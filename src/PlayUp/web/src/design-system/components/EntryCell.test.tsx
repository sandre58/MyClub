import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { EntryCell } from './EntryCell';

describe('EntryCell', () => {
  it('shows withdrawn icon with accessible name instead of inline warn copy', () => {
    render(
      <ul>
        <li className="structure-entries__team">
          <EntryCell
            displayName="Alpha FC"
            ineligible
            ineligibleTooltip="Team withdrawn from the competition"
          />
        </li>
      </ul>,
    );
    expect(screen.queryByText(/Plus éligible/i)).not.toBeInTheDocument();
    expect(
      screen.getByRole('img', {
        name: 'Team withdrawn from the competition',
      }),
    ).toBeInTheDocument();
  });

  it('keeps legacy ineligible label when no tooltip is provided', () => {
    render(
      <ul>
        <li className="structure-entries__team">
          <EntryCell
            displayName="Beta FC"
            ineligible
            ineligibleLabel="No longer eligible"
          />
        </li>
      </ul>,
    );
    expect(screen.getByText('No longer eligible')).toBeInTheDocument();
  });
});
