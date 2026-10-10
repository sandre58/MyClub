import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const webRoot = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const srcRoot = path.join(webRoot, 'src');

function walk(dir) {
  const out = [];
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) out.push(...walk(p));
    else out.push(p);
  }
  return out;
}

const allTsFiles = walk(srcRoot).filter((f) => /\.tsx?$/.test(f));
const cssFiles = walk(srcRoot).filter((f) => f.endsWith('.css'));

const cssImported = new Set();
for (const f of allTsFiles) {
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

const orphanCss = cssFiles.filter((f) => !reachableCssSet.has(path.normalize(f)));

console.log('=== CSS IMPORT GRAPH ===');
console.log('Total CSS files:', cssFiles.length);
console.log('Reachable from product entry:', reachableCssSet.size);
console.log(
  'Orphan:',
  orphanCss.length ? orphanCss.map((f) => path.relative(srcRoot, f)).join(', ') : 'none',
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

const allSource = walk(srcRoot).filter((f) => /\.(tsx?|html)$/.test(f));
let sourceText = '';
for (const f of allSource) sourceText += `${fs.readFileSync(f, 'utf8')}\n`;

const unusedClasses = [...allClasses]
  .filter((cls) => {
    const esc = cls.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    return !new RegExp(`\\b${esc}\\b`).test(sourceText);
  })
  .sort();

console.log('\n=== CSS CLASSES (heuristic — verify before delete) ===');
console.log('Unique classes in reachable CSS:', allClasses.size);
console.log('No textual match in src:', unusedClasses.length);
if (unusedClasses.length) {
  console.log('By file count (top orphans):');
  const scored = unusedClasses.map((cls) => ({
    cls,
    files: classToFiles.get(cls)?.length ?? 0,
  }));
  scored.sort((a, b) => b.files - a.files);
  for (const { cls } of scored.slice(0, 60)) {
    console.log(`  .${cls}  (${(classToFiles.get(cls) ?? []).join(', ')})`);
  }
}

const dsDir = path.join(srcRoot, 'design-system/components');
const unusedComponents = [];
for (const comp of fs.readdirSync(dsDir).filter(
  (f) => f.endsWith('.tsx') && !f.includes('.test.'),
)) {
  const name = comp.replace('.tsx', '');
  let used = false;
  for (const f of allTsFiles) {
    if (f.includes(`components${path.sep}${comp}`)) continue;
    const c = fs.readFileSync(f, 'utf8');
    if (
      c.includes(`/${name}'`) ||
      c.includes(`/${name}"`) ||
      c.includes(`<${name} `) ||
      c.includes(`<${name}>`) ||
      c.includes(`<${name}\n`)
    ) {
      used = true;
      break;
    }
  }
  if (!used) unusedComponents.push(name);
}

console.log('\n=== DESIGN-SYSTEM COMPONENTS (no import outside file) ===');
console.log(unusedComponents.length ? unusedComponents.join(', ') : 'none');

const localeDir = path.join(srcRoot, 'i18n/locales/fr');

function flattenKeys(obj, prefix = '') {
  const keys = [];
  for (const [k, v] of Object.entries(obj)) {
    const p = prefix ? `${prefix}.${k}` : k;
    if (v && typeof v === 'object' && !Array.isArray(v)) keys.push(...flattenKeys(v, p));
    else keys.push(p);
  }
  return keys;
}

let totalKeys = 0;
const unusedI18n = [];
const maybeUsedI18n = [];

for (const nsFile of fs.readdirSync(localeDir).filter((f) => f.endsWith('.json'))) {
  const nsName = nsFile.replace('.json', '');
  const keys = flattenKeys(JSON.parse(fs.readFileSync(path.join(localeDir, nsFile), 'utf8')));
  totalKeys += keys.length;
  for (const key of keys) {
    const full = `${nsName}:${key}`;
    const parts = key.split('.');
    const leaf = parts[parts.length - 1];
    const hit =
      sourceText.includes(full) ||
      sourceText.includes(`'${key}'`) ||
      sourceText.includes(`"${key}"`) ||
      sourceText.includes(`\`${key}\``) ||
      sourceText.includes(`'${leaf}'`) ||
      sourceText.includes(`"${leaf}"`);
    if (!hit) unusedI18n.push(full);
    else maybeUsedI18n.push(full);
  }
}

console.log('\n=== I18N FR KEYS (string absent from src — false positives possible) ===');
console.log(`Flagged unused: ${unusedI18n.length} / ${totalKeys}`);
for (const k of unusedI18n.slice(0, 50)) console.log(`  ${k}`);
if (unusedI18n.length > 50) console.log(`  ... +${unusedI18n.length - 50} more`);

// Duplicate CSS property blocks (rough)
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
        const sig = normalized;
        if (!blocks.has(sig)) blocks.set(sig, []);
        blocks.get(sig).push(selector);
      }
    }
    i++;
  }
  const dups = [...blocks.entries()].filter(([, sels]) => sels.length > 1);
  return dups;
}

console.log('\n=== CSS DUPLICATE DECL BLOCKS (same declarations, multiple selectors) ===');
let dupCount = 0;
for (const f of cssFiles.filter((f) => reachableCssSet.has(path.normalize(f)))) {
  const dups = findDuplicateBlocks(f);
  if (dups.length) {
    console.log(`\n${path.relative(srcRoot, f)}:`);
    for (const [sig, sels] of dups.slice(0, 5)) {
      dupCount++;
      console.log(`  ${sels.join(' | ')}`);
      console.log(`    ${sig.slice(0, 120)}${sig.length > 120 ? '…' : ''}`);
    }
    if (dups.length > 5) console.log(`  ... +${dups.length - 5} more in file`);
  }
}
console.log(`\nDuplicate-block groups (sampled): ${dupCount}+`);

// Structure tsx file count
const structureFiles = walk(path.join(srcRoot, 'pages/structure'));
console.log('\n=== STRUCTURE MODULE ===');
console.log('Files:', structureFiles.length);
console.log(
  'TSX:',
  structureFiles.filter((f) => f.endsWith('.tsx') && !f.includes('.test.')).length,
);
console.log(
  'Tests:',
  structureFiles.filter((f) => f.includes('.test.')).length,
);
console.log(
  'CSS lines:',
  structureFiles
    .filter((f) => f.endsWith('.css'))
    .reduce((n, f) => n + fs.readFileSync(f, 'utf8').split('\n').length, 0),
);

let cssLines = 0;
let tsLines = 0;
for (const f of cssFiles) cssLines += fs.readFileSync(f, 'utf8').split('\n').length;
for (const f of allTsFiles.filter((f) => !f.includes('.test.')))
  tsLines += fs.readFileSync(f, 'utf8').split('\n').length;

console.log('\n=== TOTALS ===');
console.log('CSS lines (all files):', cssLines);
console.log('TS/TSX lines (non-test):', tsLines);

// index.css vs design-system token duplication
console.log('\n=== DUAL TOKEN PIPELINE ===');
console.log(
  'index.css imports palettes+tokens; design-system/index.css re-imports palettes+tokens for .ds-root',
);
