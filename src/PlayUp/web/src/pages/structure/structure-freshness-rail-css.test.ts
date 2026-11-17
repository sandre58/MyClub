import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const structureFicheCss = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), 'structure-fiche.css'),
  'utf8',
);

describe('structure-fiche freshness actions', () => {
  it('does not ship removed trailing-rail overrides', () => {
    expect(structureFicheCss).not.toContain('structure-entries__cell-trailing');
  });

  it('keeps a 28px min hit area on team-line actions', () => {
    const actionRule = structureFicheCss.match(
      /\.structure-entries__action\s*\{[^}]+\}/,
    );
    expect(actionRule).not.toBeNull();
    expect(actionRule![0]).toMatch(/min-height:\s*28px/);
    expect(actionRule![0]).toMatch(/min-width:\s*28px/);
  });
});
