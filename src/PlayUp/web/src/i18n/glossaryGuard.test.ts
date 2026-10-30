import { readFileSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const localesRoot = join(dirname(fileURLToPath(import.meta.url)), 'locales');

function parseJson(path: string): unknown {
  let text = readFileSync(path, 'utf8');
  if (text.charCodeAt(0) === 0xfeff) {
    text = text.slice(1);
  }
  return JSON.parse(text) as unknown;
}

function collectStringLeaves(value: unknown, out: string[]): void {
  if (typeof value === 'string') {
    out.push(value);
    return;
  }
  if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
    for (const child of Object.values(value as Record<string, unknown>)) {
      collectStringLeaves(child, out);
    }
  }
}

function localeJsonFiles(locale: 'fr' | 'en'): string[] {
  return readdirSync(join(localesRoot, locale))
    .filter((name) => name.endsWith('.json'))
    .map((name) => join(localesRoot, locale, name));
}

describe('i18n glossary guard', () => {
  it('FR locale values do not expose bare “Swiss”', () => {
    const violations: string[] = [];
    for (const file of localeJsonFiles('fr')) {
      const leaves: string[] = [];
      collectStringLeaves(parseJson(file), leaves);
      for (const text of leaves) {
        if (/\bSwiss\b/.test(text)) {
          violations.push(`${file}: ${text}`);
        }
      }
    }
    expect(violations).toEqual([]);
  });

  it('FR locale values do not use “ronde” (prefer “tour”)', () => {
    const violations: string[] = [];
    for (const file of localeJsonFiles('fr')) {
      const leaves: string[] = [];
      collectStringLeaves(parseJson(file), leaves);
      for (const text of leaves) {
        if (/\brondes?\b/i.test(text)) {
          violations.push(`${file}: ${text}`);
        }
      }
    }
    expect(violations).toEqual([]);
  });

  it('FR and EN locale values do not expose “Domain”', () => {
    const violations: string[] = [];
    for (const locale of ['fr', 'en'] as const) {
      for (const file of localeJsonFiles(locale)) {
        const leaves: string[] = [];
        collectStringLeaves(parseJson(file), leaves);
        for (const text of leaves) {
          if (/\bDomain\b/.test(text)) {
            violations.push(`${file}: ${text}`);
          }
        }
      }
    }
    expect(violations).toEqual([]);
  });
});
