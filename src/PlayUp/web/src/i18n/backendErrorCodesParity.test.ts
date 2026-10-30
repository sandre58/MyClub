import { execSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const CODE_ASSIGNMENT = /public const string \w+ =\s*\n?\s*"([^"]+)";/gs;

function repoRootFromI18nModule(): string {
  return join(dirname(fileURLToPath(import.meta.url)), '../../../../..');
}

function listErrorCodeFiles(repoRoot: string): string[] {
  const playUp = join(repoRoot, 'src/PlayUp');
  const media = join(repoRoot, 'src/Media');
  const out = execSync(
    `find "${playUp}" "${media}" \\( -name "*ErrorCodes*.cs" -o -name "ApplicationErrorCodes.cs" \\) 2>/dev/null`,
    { encoding: 'utf8' },
  );
  return out.trim().split('\n').filter(Boolean);
}

function readBackendErrorCodes(repoRoot: string): string[] {
  const codes = new Set<string>();
  for (const file of listErrorCodeFiles(repoRoot)) {
    const source = readFileSync(file, 'utf8');
    for (const match of source.matchAll(CODE_ASSIGNMENT)) {
      codes.add(match[1]);
    }
  }
  return [...codes].sort();
}

function errorsJsonLeafKeys(
  tree: Record<string, unknown>,
  prefix = '',
): string[] {
  const keys: string[] = [];
  for (const [key, value] of Object.entries(tree)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
      keys.push(...errorsJsonLeafKeys(value as Record<string, unknown>, path));
    } else if (typeof value === 'string') {
      keys.push(path);
    }
  }
  return keys;
}

function parseErrorsJson(locale: 'en' | 'fr'): Record<string, unknown> {
  const root = repoRootFromI18nModule();
  const path = join(
    root,
    'src/PlayUp/web/src/i18n/locales',
    locale,
    'errors.json',
  );
  return JSON.parse(readFileSync(path, 'utf8')) as Record<string, unknown>;
}

describe('errors.json ↔ backend error codes', () => {
  const repoRoot = repoRootFromI18nModule();
  const backendCodes = readBackendErrorCodes(repoRoot);
  const enKeys = errorsJsonLeafKeys(parseErrorsJson('en')).sort();
  const frKeys = errorsJsonLeafKeys(parseErrorsJson('fr')).sort();

  it('lists the same leaf keys in FR and EN', () => {
    expect(frKeys).toEqual(enKeys);
  });

  it('has no i18n key without a backend code', () => {
    const backend = new Set(backendCodes);
    const orphans = enKeys.filter((key) => !backend.has(key));
    expect(orphans, orphans.join(', ')).toEqual([]);
  });

  it('has no backend code without FR and EN copy', () => {
    const en = new Set(enKeys);
    const fr = new Set(frKeys);
    const missingEn = backendCodes.filter((code) => !en.has(code));
    const missingFr = backendCodes.filter((code) => !fr.has(code));
    expect(missingEn, missingEn.join(', ')).toEqual([]);
    expect(missingFr, missingFr.join(', ')).toEqual([]);
  });
});
