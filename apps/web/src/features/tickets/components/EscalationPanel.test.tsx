import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ticketsApi } from '../../../api/tickets';
import { ApiError } from '../../../api/client';
import { ticketDetail } from '../../../test/fixtures';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: { escalateTicket: vi.fn(), getEscalations: vi.fn() },
}));
const escalate = vi.mocked(ticketsApi.escalateTicket);
const history = vi.mocked(ticketsApi.getEscalations);

describe('EscalationPanel', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    history.mockResolvedValue([]);
  });

  it('rejects a short reason without calling the API', async () => {
    render(<EscalationPanel ticket={ticketDetail()} onUpdated={vi.fn()} />);
    fireEvent.change(screen.getByLabelText(/Reason/), { target: { value: 'bad' } });
    expect(screen.getByRole('button', { name: /Escalate priority/ })).toBeDisabled();
    expect(escalate).not.toHaveBeenCalled();
  });

  it('shows conflict responses without updating the ticket', async () => {
    const onUpdated = vi.fn();
    escalate.mockRejectedValue(new ApiError(409, 'Critical tickets cannot be escalated.'));
    render(<EscalationPanel ticket={ticketDetail()} onUpdated={onUpdated} />);
    fireEvent.change(screen.getByLabelText(/Reason/), { target: { value: 'Multiple impacted customers' } });
    await act(async () => { fireEvent.click(screen.getByRole('button', { name: /Escalate priority/ })); });
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent(/Critical tickets/));
    expect(onUpdated).not.toHaveBeenCalled();
  });
});
