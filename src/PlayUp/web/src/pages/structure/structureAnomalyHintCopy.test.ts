import { describe, expect, it } from 'vitest';
import { structureReadinessHintTitle } from './structureAnomalyHintCopy';

describe('structureReadinessHintTitle', () => {
  const t = (key: string) => key;

  it('maps error and warning to PageHead status keys', () => {
    expect(structureReadinessHintTitle({ t, severity: 'error' })).toBe(
      'readiness.anomalyHintTitle',
    );
    expect(structureReadinessHintTitle({ t, severity: 'warning' })).toBe(
      'readiness.warningHintTitle',
    );
  });
});
