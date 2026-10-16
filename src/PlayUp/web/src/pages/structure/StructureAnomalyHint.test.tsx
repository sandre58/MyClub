import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StructureReadinessHint } from './StructureReadinessHint';

describe('StructureReadinessHint', () => {
  it('renders the compact error title only', () => {
    render(<StructureReadinessHint severity="error" />);

    expect(
      screen.getByText(/Des anomalies nécessitent votre attention/i),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Corriger/i }),
    ).not.toBeInTheDocument();
  });

  it('renders the warning title with attention styling', () => {
    render(<StructureReadinessHint severity="warning" />);

    expect(
      screen.getByText(/La structure reste à finaliser/i),
    ).toBeInTheDocument();
  });
});
