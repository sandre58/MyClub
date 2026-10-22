/**
 * S3 qualification buckets for i18n-key-audit heuristics (report-only).
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const srcRoot = path.join(webRoot, 'src');
const localesRoot = path.join(srcRoot, 'i18n/locales');

const DYNAMIC_KEY_PREFIXES = [
  'errors:',
  'enums:attentionSource.',
  'enums:attentionDescription.',
  'enums:attentionSeverity.',
  'enums:completionBlocker.',
  'structure:QualIncomplete',
  'structure:PlacementIncomplete',
  'structure:ProgIncomplete',
];

function walk(dir) {
  const out = [];
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) out.push(...walk(p));
    else out.push(p);
  }
  return out;
}

function leafKeys(value, prefix = '') {
  if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
    return Object.entries(value).flatMap(([key, child]) =>
      leafKeys(child, prefix ? `${prefix}.${key}` : key),
    );
  }
  return prefix ? [prefix] : [];
}

let sourceText = '';
for (const f of walk(srcRoot).filter((p) => /\.(tsx?|json)$/.test(p))) {
  if (f.includes(`${path.sep}locales${path.sep}`)) continue;
  sourceText += `${fs.readFileSync(f, 'utf8')}\n`;
}

const suspicious = [];
for (const locale of ['fr', 'en']) {
  const dir = path.join(localesRoot, locale);
  for (const file of fs.readdirSync(dir).filter((n) => n.endsWith('.json'))) {
    const ns = file.replace('.json', '');
    const keys = leafKeys(JSON.parse(fs.readFileSync(path.join(dir, file), 'utf8')));
    for (const key of keys) {
      const full = `${ns}:${key}`;
      if (DYNAMIC_KEY_PREFIXES.some((p) => full.startsWith(p))) continue;
      const leaf = key.split('.').pop() ?? key;
      const hit =
        sourceText.includes(full) ||
        sourceText.includes(`'${key}'`) ||
        sourceText.includes(`"${key}"`) ||
        sourceText.includes(`\`${key}\``) ||
        sourceText.includes(`'${leaf}'`) ||
        sourceText.includes(`"${leaf}"`);
      if (!hit) suspicious.push({ locale, ns, key, full: `${locale}:${full}` });
    }
  }
}

const PLURAL_LEAF = /_(one|other|zero|two|few|many)$/;

function bucket(entry) {
  const leaf = entry.key.split('.').pop() ?? entry.key;
  if (PLURAL_LEAF.test(leaf)) return 'plural_suffix';
  if (entry.ns === 'enums') return 'enums_dynamic_or_catalog';
  if (sourceText.includes(`t('${entry.key.split('.')[0]}`)) return 'namespace_root_only';
  if (entry.key.includes('{{') || entry.key.includes('count')) return 'plural_or_icu_path';
  return 'investigate';
}

const buckets = new Map();
for (const s of suspicious) {
  const b = bucket(s);
  if (!buckets.has(b)) buckets.set(b, []);
  buckets.get(b).push(s);
}

console.log('=== S3 i18n qualification (heuristic buckets) ===');
console.log('Suspicious rows (fr+en):', suspicious.length);
console.log('Unique logical keys (ns:key, any locale):', new Set(suspicious.map((s) => `${s.ns}:${s.key}`)).size);

for (const [name, items] of [...buckets.entries()].sort((a, b) => b[1].length - a[1].length)) {
  console.log(`\n--- ${name}: ${items.length} ---`);
  const sample = items.slice(0, 12).map((i) => i.full);
  for (const line of sample) console.log(`  ${line}`);
  if (items.length > 12) console.log(`  ... +${items.length - 12} more`);
}

console.log('\n=== Parity note ===');
console.log('i18n:check (localeParity) is independent — green does not imply zero suspicious keys.');
