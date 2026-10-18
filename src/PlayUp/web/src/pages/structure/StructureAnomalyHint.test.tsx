import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StructureReadinessHint } from './StructureReadinessHint';

describe('StructureReadinessHint', () => {
  it('renders the compact error title only', () => {
    render(<StructureReadinessHint severity="error" />);

    expect(
      screen.getByText(/Anomalies à traiter/i),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Corriger/i }),
    ).not.toBeInTheDocument();
  });

  it('renders the warning title with attention styling', () => {
    render(<StructureReadinessHint severity="warning" />);

    expect(
      screen.getByText(/Structure incomplète/i),
    ).toBeInTheDocument();
  });
});
