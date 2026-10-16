import type { PagedResult, TicketListItem, TicketDetail } from '../types/api';

export function ticketDetail(overrides: Partial<TicketDetail> = {}): TicketDetail {
  return {
    ...ticketListItem(), description: 'A ticket with a description.',
    customer: { id: 2, name: 'Fabrikam Inc', tier: 'Standard', email: 'hello@example.com', phone: null },
    category: { id: 2, name: 'Billing', requiresSpecialist: true },
    ...overrides,
  };
}

/** A list row with sensible defaults, so tests only state what they care about. */
export function ticketListItem(overrides: Partial<TicketListItem> = {}): TicketListItem {
  return {
    id: 12,
    reference: 'TCK-0012',
    title: 'Invoice total does not match the order',
    status: 'InProgress',
    priority: 'High',
    customer: { id: 2, name: 'Fabrikam Inc', tier: 'Standard' },
    category: { id: 2, name: 'Billing' },
    assignedAgent: { id: 4, fullName: 'Sara Lindqvist' },
    createdAtUtc: '2026-09-24T08:12:00Z',
    updatedAtUtc: '2026-09-25T10:03:00Z',
    dueAtUtc: '2026-09-24T16:12:00Z',
    resolvedAtUtc: null,
    slaStatus: 'Breached',
    ...overrides,
  };
}

export function page(items: TicketListItem[]): PagedResult<TicketListItem> {
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: 1,
  };
}
