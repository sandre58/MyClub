import { readFileSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import { I18N_NAMESPACES } from './config';

const localesRoot = join(dirname(fileURLToPath(import.meta.url)), 'locales');

function parseJson(path: string): unknown {
  let text = readFileSync(path, 'utf8');
  if (text.charCodeAt(0) === 0xfeff) {
    text = text.slice(1);
  }
  return JSON.parse(text) as unknown;
}

function leafKeys(value: unknown, prefix = ''): string[] {
  if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
    return Object.entries(value as Record<string, unknown>).flatMap(
      ([key, child]) =>
        leafKeys(child, prefix ? `${prefix}.${key}` : key),
    );
  }
  return prefix ? [prefix] : [];
}

describe('i18n locale key parity', () => {
  const namespaces = readdirSync(join(localesRoot, 'fr'))
    .filter((name) => name.endsWith('.json'))
    .map((name) => name.replace(/\.json$/, ''))
    .sort();

  it('registers every FR namespace in I18N_NAMESPACES', () => {
    expect([...I18N_NAMESPACES].sort()).toEqual(namespaces);
  });

  it.each(namespaces)('%s FR keys match EN keys', (namespace) => {
    const fr = leafKeys(
      parseJson(join(localesRoot, 'fr', `${namespace}.json`)),
    ).sort();
    const en = leafKeys(
      parseJson(join(localesRoot, 'en', `${namespace}.json`)),
    ).sort();
    expect(en).toEqual(fr);
  });
});
