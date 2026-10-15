import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { StructureOperationalIssueBanner } from './StructureOperationalIssueBanner';

describe('StructureOperationalIssueBanner', () => {
  it('uses paused copy when diagnostic context is Suspended', () => {
    render(
      <StructureOperationalIssueBanner
        codes={['SlotFeedMissing']}
        diagnosticContext={{ stageStatus: 'Suspended' }}
      />,
    );
    expect(screen.getByText(/constat en pause/i)).toBeTruthy();
  });
});
