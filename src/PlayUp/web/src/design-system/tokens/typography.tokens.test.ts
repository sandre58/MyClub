import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const typographyTokens = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), 'typography.css'),
  'utf8',
);

describe('typography tokens', () => {
  it('defines caption size at 11px (0.6875rem) with matching leading', () => {
    expect(typographyTokens).toMatch(/--text-caption:\s*0\.6875rem/);
    expect(typographyTokens).toMatch(/--leading-caption:\s*1\.2/);
  });
});
