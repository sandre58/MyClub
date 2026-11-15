import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const layoutCss = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), 'structure-phase-layout.css'),
  'utf8',
);

describe('composition qualifiers rule line (iter3 option A)', () => {
  it('stacks rule and hint with no vertical gap', () => {
    const block = layoutCss.match(
      /\.structure-composition-qualifiers__rule-line--after\s*\{[^}]+\}/,
    );
    expect(block).not.toBeNull();
    expect(block![0]).toMatch(/gap:\s*0/);
  });

  it('types the qualification rule with label size and primary color', () => {
    const block = layoutCss.match(
      /\.structure-composition-qualifiers__rule-primary\s+\.structure-feed-group__rule-label\s*\{[^}]+\}/,
    );
    expect(block).not.toBeNull();
    expect(block![0]).toMatch(/var\(--text-label\)/);
    expect(block![0]).toMatch(/var\(--leading-tight\)/);
    expect(block![0]).toMatch(/var\(--font-weight-semibold\)/);
    expect(block![0]).toMatch(/var\(--color-text-primary\)/);
  });

  it('keeps the global feed rule label on meta size for non-qualifier rails', () => {
    const block = layoutCss.match(
      /^\.structure-feed-group__rule-label\s*\{[^}]+\}/m,
    );
    expect(block).not.toBeNull();
    expect(block![0]).toMatch(/var\(--text-meta\)/);
  });
});
