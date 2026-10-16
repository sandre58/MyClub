import { describe, expect, it } from 'vitest';
import { structureReadinessHintTitle } from './structureAnomalyHintCopy';

describe('structureReadinessHintTitle', () => {
  const t = (key: string) => key;

  it('uses default keys when pausedStructureCopy is false', () => {
    expect(
      structureReadinessHintTitle({ t, severity: 'warning' }),
    ).toBe('readiness.warningHintTitle');
  });

  it('uses paused keys when pausedStructureCopy is true', () => {
    expect(
      structureReadinessHintTitle({
        t,
        severity: 'warning',
        pausedStructureCopy: true,
      }),
    ).toBe('readiness.warningHintTitlePaused');
  });
});
