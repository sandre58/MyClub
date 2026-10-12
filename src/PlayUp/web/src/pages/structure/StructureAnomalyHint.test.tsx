import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StructureAnomalyHint } from './StructureAnomalyHint';

describe('StructureAnomalyHint', () => {
  it('renders the compact negative title only', () => {
    render(<StructureAnomalyHint />);

    expect(
      screen.getByText(/Des anomalies nécessitent votre attention/i),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Corriger/i }),
    ).not.toBeInTheDocument();
  });
});
