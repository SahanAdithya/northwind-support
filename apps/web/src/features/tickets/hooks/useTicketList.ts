import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { PagedResult, TicketFilters, TicketListItem } from '../../../types/api';

const DEBOUNCE_MS = 300;

export const emptyFilters: TicketFilters = {
  search: '',
  status: undefined,
  priority: undefined,
  categoryId: undefined,
  customerId: undefined,
  assignedAgentId: undefined,
  unassignedOnly: false,
  slaStatus: undefined,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
};

interface State {
  isLoading: boolean;
  data?: PagedResult<TicketListItem>;
  error?: string;
}

/** Every non-search filter runs immediately; only search is debounced. */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [debouncedSearch, setDebouncedSearch] = useState(filters.search);
  const [state, setState] = useState<State>({ isLoading: true });
  const requestNumber = useRef(0);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setDebouncedSearch(filters.search);
      setFilters((current) => current.page === 1 ? current : { ...current, page: 1 });
    }, DEBOUNCE_MS);
    return () => window.clearTimeout(timeout);
  }, [filters.search]);

  // The original effect watched only search/page/pageSize; dropdown changes never fetched.
  // Keep search out of these dependencies so typing doesn't trigger an immediate request.
  const { status, priority, categoryId, customerId, assignedAgentId, unassignedOnly,
    slaStatus, page, pageSize, sortBy, sortDirection } = filters;
  const requestFilters = useMemo<TicketFilters>(() => ({
    search: debouncedSearch, status, priority, categoryId, customerId, assignedAgentId,
    unassignedOnly, slaStatus, page, pageSize, sortBy, sortDirection,
  }), [debouncedSearch, status, priority, categoryId, customerId, assignedAgentId,
    unassignedOnly, slaStatus, page, pageSize, sortBy, sortDirection]);

  const load = useCallback(async () => {
    const request = ++requestNumber.current;
    try {
      const data = await ticketsApi.getTickets(requestFilters);
      if (request === requestNumber.current) setState({ isLoading: false, data });
    } catch (error) {
      if (request === requestNumber.current) {
        const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
        setState((current) => ({ ...current, isLoading: false, error: message }));
      }
    }
  }, [requestFilters]);

  useEffect(() => {
    const request = ++requestNumber.current;
    let active = true;
    void ticketsApi.getTickets(requestFilters)
      .then((data) => {
        if (active && request === requestNumber.current) {
          setState({ isLoading: false, data });
        }
      })
      .catch((error: unknown) => {
        if (active && request === requestNumber.current) {
          const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
          setState((current) => ({ ...current, isLoading: false, error: message }));
        }
      });
    return () => { active = false; requestNumber.current += 1; };
  }, [requestFilters]);

  // Mark user-initiated changes as loading immediately. Requests triggered by the
  // effect below only update state after their asynchronous response arrives.
  const markLoading = () =>
    setState((current) => ({ ...current, isLoading: true, error: undefined }));
  const updateFilters = (patch: Partial<TicketFilters>) => {
    markLoading();
    // A search edit must not reset page until its debounce expires, otherwise
    // the page change causes an immediate request with the *old* search term.
    const searchOnly = Object.keys(patch).length === 1 && 'search' in patch;
    setFilters((current) => ({
      ...current, ...patch, page: patch.page ?? (searchOnly ? current.page : 1),
    }));
  };
  const resetFilters = () => {
    markLoading();
    setDebouncedSearch('');
    setFilters(emptyFilters);
  };
  const reload = () => {
    markLoading();
    return load();
  };
  return { filters, updateFilters, resetFilters, reload, ...state };
}
