import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const webRoot = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const srcRoot = path.join(webRoot, 'src');
const localesRoot = path.join(srcRoot, 'i18n/locales');

/** Wire codes resolved at runtime — do not flag as unused when only matched dynamically. */
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
      if (!hit) suspicious.push(`${locale}:${full}`);
    }
  }
}

console.log(`Suspicious unused keys (heuristic): ${suspicious.length}`);
for (const k of suspicious.slice(0, 80)) console.log(`  ${k}`);
if (suspicious.length > 80) console.log(`  ... +${suspicious.length - 80} more`);
