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
    const timeout = window.setTimeout(() => setDebouncedSearch(filters.search), DEBOUNCE_MS);
    return () => window.clearTimeout(timeout);
  }, [filters.search]);

  // The original effect watched only search/page/pageSize; dropdown changes never fetched.
  // Keep search out of these dependencies so typing doesn't trigger an immediate request.
  const requestFilters = useMemo<TicketFilters>(() => ({ ...filters, search: debouncedSearch }), [
    debouncedSearch,
    filters.status, filters.priority, filters.categoryId, filters.customerId,
    filters.assignedAgentId, filters.unassignedOnly, filters.page, filters.pageSize,
    filters.sortBy, filters.sortDirection,
  ]);

  const load = useCallback(async () => {
    const request = ++requestNumber.current;
    setState((current) => ({ ...current, isLoading: true, error: undefined }));
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
    void load();
    return () => { requestNumber.current += 1; };
  }, [load]);

  const updateFilters = (patch: Partial<TicketFilters>) =>
    setFilters((current) => ({ ...current, ...patch, page: patch.page ?? 1 }));
  const resetFilters = () => setFilters(emptyFilters);
  return { filters, updateFilters, resetFilters, reload: load, ...state };
}
