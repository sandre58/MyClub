import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const css = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), '../structure-fiche.css'),
  'utf8',
);

describe('structure freshness tooltip layout', () => {
  it('stretches the action row with the tooltip column', () => {
    expect(css).toMatch(
      /\.structure-freshness-replace__tip\s*\{[^}]*align-items:\s*stretch/s,
    );
    expect(css).not.toMatch(
      /\.structure-freshness-replace__action\s*\{[^}]*width:\s*100%/s,
    );
  });
});
