import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const layoutCss = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), 'structure-phase-layout.css'),
  'utf8',
);

describe('composition qualifiers rule and hint (iter4)', () => {
  it('stacks rule and hint with no vertical gap', () => {
    const block = layoutCss.match(
      /\.structure-composition-qualifiers__rule-line--after\s*\{[^}]+\}/,
    );
    expect(block).not.toBeNull();
    expect(block![0]).toMatch(/gap:\s*0/);
  });

  it('types the qualification rule like équipes team names', () => {
    const block = layoutCss.match(
      /\.structure-composition-qualifiers__rule-primary\s+\.structure-feed-group__rule-label\s*\{[^}]+\}/,
    );
    expect(block).not.toBeNull();
    expect(block![0]).toMatch(/var\(--text-meta\)/);
    expect(block![0]).toMatch(/var\(--leading-tight\)/);
    expect(block![0]).toMatch(/var\(--font-weight-regular\)/);
    expect(block![0]).toMatch(/var\(--color-text-primary\)/);
  });

  it('types the qualifier hint like the rule row for visual balance', () => {
    const hint = layoutCss.match(
      /\.structure-composition-qualifiers__hint\s*\{[^}]+\}/,
    );
    const rule = layoutCss.match(
      /\.structure-composition-qualifiers__rule-primary\s+\.structure-feed-group__rule-label\s*\{[^}]+\}/,
    );
    expect(hint).not.toBeNull();
    expect(rule).not.toBeNull();
    expect(hint![0]).toMatch(/var\(--text-meta\)/);
    expect(rule![0]).toMatch(/var\(--text-meta\)/);
    expect(hint![0]).toMatch(/var\(--leading-tight\)/);
    expect(hint![0]).toMatch(/var\(--font-weight-regular\)/);
  });

  it('spaces qualifier hint units with space-8', () => {
    const block = layoutCss.match(
      /\.structure-composition-qualifiers__hint\s*\{[^}]+\}/,
    );
    expect(block).not.toBeNull();
    expect(block![0]).toMatch(/gap:\s*var\(--space-8\)/);
  });

  it('keeps the global feed rule label on meta size for non-qualifier rails', () => {
    const block = layoutCss.match(
      /^\.structure-feed-group__rule-label\s*\{[^}]+\}/m,
    );
    expect(block).not.toBeNull();
    expect(block![0]).toMatch(/var\(--text-meta\)/);
  });
});
