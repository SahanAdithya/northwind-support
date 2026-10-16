import { useCallback, useEffect, useState } from 'react';
import { ApiError, toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { formatDateTime } from '../../../lib/format';
import type { TicketDetail, TicketEscalation } from '../../../types/api';

interface Props { ticket: TicketDetail; onUpdated: (ticket: TicketDetail) => void; }

/** Client validation mirrors the API, but server validation remains authoritative. */
export function EscalationPanel({ ticket, onUpdated }: Props) {
  const [reason, setReason] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [history, setHistory] = useState<TicketEscalation[]>([]);
  const [historyError, setHistoryError] = useState('');

  const loadHistory = useCallback(async () => {
    try {
      setHistory(await ticketsApi.getEscalations(ticket.id));
      setHistoryError('');
    } catch (e) { setHistoryError(toErrorMessage(e, 'Unable to load escalation history')); }
  }, [ticket.id]);

  useEffect(() => {
    // Network responses update React state asynchronously; ignore stale responses.
    let active = true;
    void ticketsApi.getEscalations(ticket.id)
      .then((entries) => {
        if (!active) return;
        setHistory(entries);
        setHistoryError('');
      })
      .catch((error: unknown) => {
        if (active) setHistoryError(toErrorMessage(error, 'Unable to load escalation history'));
      });
    return () => { active = false; };
  }, [ticket.id]);

  const disabled = ticket.priority === 'Critical' ||
    ticket.status === 'Resolved' || ticket.status === 'Closed';
  const invalidReason = reason.trim().length < 5 || reason.trim().length > 500;

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (disabled || invalidReason || submitting) return;
    setSubmitting(true);
    setError('');
    try {
      const result = await ticketsApi.escalateTicket(ticket.id, reason.trim());
      onUpdated(result.ticket);
      setReason('');
      await loadHistory();
    } catch (e) {
      setError(e instanceof ApiError && e.status === 409
        ? `Escalation conflict: ${e.message}` : toErrorMessage(e, 'Escalation failed'));
    } finally { setSubmitting(false); }
  };

  return <div className="card">
    <h2>Escalation</h2>
    {disabled ? <p>This ticket cannot be escalated in its current state.</p> : <form onSubmit={(e) => void submit(e)}>
      <div className="field">
        <label className="field__label" htmlFor="escalation-reason">Reason (5–500 characters)</label>
        <textarea id="escalation-reason" rows={3} maxLength={500} value={reason}
          onChange={(e) => setReason(e.target.value)} required aria-describedby="escalation-reason-help" />
        <small id="escalation-reason-help">Explain why this ticket needs a higher priority.</small>
      </div>
      {error && <p role="alert">{error}</p>}
      <button type="submit" className="button button--primary"
        disabled={invalidReason || submitting}>{submitting ? 'Escalating…' : 'Escalate priority'}</button>
    </form>}
    <h3>Escalation history</h3>
    {historyError && <p role="alert">{historyError}</p>}
    {history.length === 0 && !historyError && <p>No escalations yet.</p>}
    <ol>{history.map((entry) => <li key={entry.id}>
      <strong>{entry.fromPriority} → {entry.toPriority}</strong> on {formatDateTime(entry.escalatedAtUtc)}
      <p>By {entry.escalatedBy}: {entry.reason}</p>
      <p>Agent: {entry.fromAgentId ?? 'Unassigned'} → {entry.toAgentId ?? 'Unassigned'}</p>
      <p>Due: {formatDateTime(entry.fromDueAtUtc)} → {formatDateTime(entry.toDueAtUtc)}</p>
    </li>)}</ol>
  </div>;
}
