import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { SlaCell } from './SlaCell';
import type { SlaStatus } from '../../../types/api';

describe('SLA state indicator', () => {
  it.each<[SlaStatus, string]>([
    ['NotApplicable', 'No SLA'], ['WithinSla', 'Within SLA'],
    ['AtRisk', 'At risk'], ['Breached', 'Breached'], ['Met', 'Met'],
  ])('%s has a readable text label', (status, label) => {
    render(<SlaCell dueAtUtc={null} slaStatus={status} />);
    expect(screen.getByText(label)).toBeInTheDocument();
  });
});
