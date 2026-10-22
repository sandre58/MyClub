/**
 * S4: bucket CSS classes flagged by frontend-audit (no literal \bclass\b in src).
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const srcRoot = path.join(webRoot, 'src');

function walk(dir, out = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) walk(p, out);
    else out.push(p);
  }
  return out;
}

const cssFiles = walk(srcRoot).filter((f) => f.endsWith('.css'));
const allTs = walk(srcRoot).filter((f) => /\.(tsx?|html)$/.test(f));

const cssImported = new Set();
for (const f of allTs) {
  const c = fs.readFileSync(f, 'utf8');
  for (const m of c.matchAll(/import\s+['"]([^'"]+\.css)['"]/g)) {
    const rel = m[1];
    if (rel.startsWith('.')) {
      cssImported.add(path.normalize(path.join(path.dirname(f), rel)));
    }
  }
}

function resolveCssImports(file, visited = new Set()) {
  const norm = path.normalize(file);
  if (visited.has(norm)) return;
  visited.add(norm);
  cssImported.add(norm);
  if (!fs.existsSync(norm)) return;
  const c = fs.readFileSync(norm, 'utf8');
  for (const m of c.matchAll(/@import\s+['"]([^'"]+)['"]/g)) {
    resolveCssImports(path.join(path.dirname(norm), m[1]), visited);
  }
}

resolveCssImports(path.join(srcRoot, 'index.css'));
for (const f of [...cssImported]) resolveCssImports(f);

const reachableCssSet = new Set(
  [...cssImported].filter((f) => f.endsWith('.css') && fs.existsSync(f)),
);

function extractClasses(css) {
  const stripped = css.replace(/\/\*[\s\S]*?\*\//g, '');
  const classes = new Set();
  for (const m of stripped.matchAll(/\.([a-zA-Z_][\w-]*)/g)) classes.add(m[1]);
  return classes;
}

let allClasses = new Set();
const classToFiles = new Map();
for (const f of cssFiles.filter((f) => reachableCssSet.has(path.normalize(f)))) {
  for (const cls of extractClasses(fs.readFileSync(f, 'utf8'))) {
    allClasses.add(cls);
    if (!classToFiles.has(cls)) classToFiles.set(cls, []);
    classToFiles.get(cls).push(path.relative(srcRoot, f));
  }
}

let sourceText = '';
for (const f of allTs) sourceText += `${fs.readFileSync(f, 'utf8')}\n`;

const unusedClasses = [...allClasses]
  .filter((cls) => {
    const esc = cls.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    return !new RegExp(`\\b${esc}\\b`).test(sourceText);
  })
  .sort();

function bucket(cls) {
  if (cls.startsWith('dlab-')) return 'design_lab';
  const dsMod = /^ds-[\w-]+--/.test(cls);
  if (dsMod) {
    const base = cls.replace(/--[^-]+.*$/, '');
    const dynamic = new RegExp(`${base.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}--\\$\\{`).test(
      sourceText,
    );
    if (dynamic) return 'ds_dynamic_modifier';
  }
  if (/--\$\{/.test(sourceText) && sourceText.includes(cls.split('--')[0])) {
    return 'dynamic_prefix';
  }
  const prefix = cls.includes('--') ? cls.slice(0, cls.indexOf('--')) : cls;
  const templated = new RegExp(
    `${prefix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}--\\$\\{`,
  ).test(sourceText);
  if (templated) return 'dynamic_prefix';
  if (cls.startsWith('structure-') || cls.startsWith('schematic-') || cls.startsWith('regulation-')) {
    return 'page_modifier_investigate';
  }
  if (cls.startsWith('shell-')) return 'shell_modifier_investigate';
  return 'investigate';
}

const buckets = new Map();
for (const cls of unusedClasses) {
  const b = bucket(cls);
  if (!buckets.has(b)) buckets.set(b, []);
  buckets.get(b).push(cls);
}

function findDuplicateBlocks(cssPath) {
  const lines = fs.readFileSync(cssPath, 'utf8').split('\n');
  const blocks = new Map();
  let i = 0;
  while (i < lines.length) {
    const sel = lines[i].trim();
    if (sel.endsWith('{') && !sel.startsWith('@')) {
      const selector = sel.slice(0, -1).trim();
      const body = [];
      i++;
      while (i < lines.length && lines[i].trim() !== '}') {
        body.push(lines[i].trim());
        i++;
      }
      const normalized = body.filter(Boolean).join(';');
      if (normalized.length > 40) {
        if (!blocks.has(normalized)) blocks.set(normalized, []);
        blocks.get(normalized).push(selector);
      }
    }
    i++;
  }
  return [...blocks.entries()].filter(([, sels]) => sels.length > 1);
}

let dupGroups = 0;
let dupFiles = 0;
for (const f of cssFiles.filter((f) => reachableCssSet.has(path.normalize(f)))) {
  const dups = findDuplicateBlocks(f);
  if (dups.length) {
    dupFiles++;
    dupGroups += dups.length;
  }
}

console.log('=== S4 CSS class qualification ===');
console.log('Reachable CSS files:', reachableCssSet.size);
console.log('Unique classes:', allClasses.size);
console.log('No literal \\bclass\\b in TS/HTML:', unusedClasses.length);

for (const [name, items] of [...buckets.entries()].sort((a, b) => b[1].length - a[1].length)) {
  console.log(`\n--- ${name}: ${items.length} ---`);
  for (const cls of items.slice(0, 15)) {
    console.log(`  .${cls}  (${(classToFiles.get(cls) ?? []).join(', ')})`);
  }
  if (items.length > 15) console.log(`  ... +${items.length - 15} more`);
}

console.log('\n=== Duplicate declaration blocks (same body, multiple selectors) ===');
console.log('Files with ≥1 duplicate group:', dupFiles);
console.log('Duplicate groups (total):', dupGroups);
console.log('Note: shared DS modifiers / state variants often intentional — not auto-debt.');
