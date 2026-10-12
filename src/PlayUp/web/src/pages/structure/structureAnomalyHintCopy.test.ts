import { describe, expect, it } from 'vitest';
import { structureAnomalyHintTitle } from './structureAnomalyHintCopy';

describe('structureAnomalyHintTitle', () => {
  const t = (key: string) =>
    key === 'readiness.anomalyHintTitle'
      ? 'Des anomalies nécessitent votre attention'
      : key;

  it('returns the single PageHead sentence', () => {
    expect(structureAnomalyHintTitle({ t })).toBe(
      'Des anomalies nécessitent votre attention',
    );
  });
});
