import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const structureFicheCss = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), 'structure-fiche.css'),
  'utf8',
);

describe('structure-fiche freshness rail spacing', () => {
  it('resets inline-start margin on trailing xs actions after the base xs rule', () => {
    const baseRule =
      '.structure-freshness-badge__action.ds-btn.ds-icon-button--xs';
    const railRule =
      '.structure-entries__cell-trailing .structure-freshness-badge__action.ds-btn.ds-icon-button--xs';
    const railIdx = structureFicheCss.indexOf(railRule);
    const baseIdx = structureFicheCss.indexOf(baseRule);

    expect(railIdx).toBeGreaterThanOrEqual(0);
    expect(baseIdx).toBeGreaterThanOrEqual(0);
    expect(railIdx).toBeGreaterThan(baseIdx);

    const railBlock = structureFicheCss.slice(
      railIdx,
      structureFicheCss.indexOf('}', railIdx) + 1,
    );
    expect(railBlock).toMatch(/margin-inline-start:\s*0/);
  });
});
