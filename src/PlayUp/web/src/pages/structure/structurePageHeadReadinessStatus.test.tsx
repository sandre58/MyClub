import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { StructurePageHeadReadinessStatus } from './structurePageHeadReadinessStatus';
import { InfoIcon } from '../../design-system/icons/contentIcons';

describe('StructurePageHeadReadinessStatus', () => {
  it('renders outline pill Status without multiline modifiers', () => {
    const { container } = render(
      <StructurePageHeadReadinessStatus tone="info" icon={<InfoIcon size="sm" />}>
        Structure prête
      </StructurePageHeadReadinessStatus>,
    );

    const status = container.querySelector('.ds-status');
    expect(status).toHaveClass('ds-status--pill');
    expect(status).toHaveClass('ds-status--outline');
    expect(status).not.toHaveClass('ds-status--multiline');
    expect(screen.getByRole('status')).toHaveTextContent('Structure prête');
  });
});
