/**
 * Report-only: static import graph from product entry (main.tsx).
 * Does not resolve path aliases beyond relative imports or dynamic import() string literals.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const srcRoot = path.join(webRoot, 'src');

const EXT = ['.tsx', '.ts', '.jsx', '.js'];

function walk(dir, out = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) walk(p, out);
    else if (/\.(tsx?|jsx?)$/.test(ent.name)) out.push(p);
  }
  return out;
}

function resolveImport(fromFile, spec) {
  if (!spec.startsWith('.')) return null;
  const base = path.normalize(path.join(path.dirname(fromFile), spec));
  if (fs.existsSync(base) && fs.statSync(base).isFile()) return base;
  for (const ext of EXT) {
    const withExt = base + ext;
    if (fs.existsSync(withExt)) return withExt;
  }
  for (const ext of EXT) {
    const index = path.join(base, `index${ext}`);
    if (fs.existsSync(index)) return index;
  }
  return null;
}

function extractImports(content) {
  const specs = [];
  for (const m of content.matchAll(
    /(?:import|export)\s+(?:type\s+)?(?:[\w*{}\s,]+from\s+)?['"]([^'"]+)['"]/g,
  )) {
    specs.push(m[1]);
  }
  for (const m of content.matchAll(/import\s*\(\s*['"]([^'"]+)['"]\s*\)/g)) {
    specs.push(m[1]);
  }
  return specs;
}

function buildReachable(entryFiles) {
  const reachable = new Set();
  const queue = [...entryFiles];
  while (queue.length) {
    const file = queue.pop();
    const norm = path.normalize(file);
    if (!fs.existsSync(norm) || reachable.has(norm)) continue;
    reachable.add(norm);
    const content = fs.readFileSync(norm, 'utf8');
    for (const spec of extractImports(content)) {
      const resolved = resolveImport(norm, spec);
      if (resolved && resolved.startsWith(srcRoot)) queue.push(resolved);
    }
  }
  return reachable;
}

const allModules = walk(srcRoot);
const testEntries = allModules.filter((f) => f.includes('.test.'));
const reachableProduct = buildReachable([path.join(srcRoot, 'main.tsx')]);
const reachableTests = buildReachable(testEntries);

const productionModules = allModules.filter(
  (f) =>
    !f.includes('.test.') &&
    !f.includes(`${path.sep}test${path.sep}`) &&
    !f.includes(`${path.sep}e2e${path.sep}`),
);

const unreachableFromProduct = productionModules.filter(
  (f) => !reachableProduct.has(path.normalize(f)),
);

const testOnlySupport = unreachableFromProduct.filter((f) =>
  reachableTests.has(path.normalize(f)),
);

const productOrphanCandidates = unreachableFromProduct.filter(
  (f) => !reachableTests.has(path.normalize(f)),
);

/** @param {string} file */
function importersOf(file) {
  const norm = path.normalize(file);
  const rel = path.relative(srcRoot, norm).replace(/\\/g, '/');
  const base = path.basename(norm, path.extname(norm));
  const hits = [];
  for (const mod of allModules) {
    if (mod === norm) continue;
    const c = fs.readFileSync(mod, 'utf8');
    if (
      c.includes(`'./${base}'`) ||
      c.includes(`"./${base}"`) ||
      c.includes(`'${rel.replace(/\.tsx?$/, '')}'`) ||
      c.includes(`"/${rel}"`) ||
      c.includes(`from './${rel.replace(/\.tsx?$/, '')}'`)
    ) {
      hits.push(path.relative(srcRoot, mod));
    }
  }
  return hits;
}

function exportNames(file) {
  const c = fs.readFileSync(file, 'utf8');
  const names = [];
  if (/export\s+default\b/.test(c)) names.push('default');
  for (const m of c.matchAll(/export\s+(?:async\s+)?function\s+(\w+)/g)) names.push(m[1]);
  for (const m of c.matchAll(/export\s+const\s+(\w+)/g)) names.push(m[1]);
  for (const m of c.matchAll(/export\s+class\s+(\w+)/g)) names.push(m[1]);
  for (const m of c.matchAll(/export\s+type\s+(\w+)/g)) names.push(m[1]);
  for (const m of c.matchAll(/export\s+\{\s*([^}]+)\}/g)) {
    for (const part of m[1].split(',')) {
      const n = part.trim().split(/\s+as\s+/)[0].trim();
      if (n) names.push(n);
    }
  }
  return [...new Set(names)];
}

let productSource = '';
for (const f of productionModules) {
  if (reachableProduct.has(path.normalize(f))) {
    productSource += `${fs.readFileSync(f, 'utf8')}\n`;
  }
}

const exportOnlyInTests = [];
for (const f of productionModules) {
  if (!reachableProduct.has(path.normalize(f))) continue;
  const rel = path.relative(srcRoot, f);
  for (const name of exportNames(f)) {
    if (name === 'default') continue;
    const esc = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const inProduct = new RegExp(`\\b${esc}\\b`).test(productSource);
    const inTests = allModules
      .filter((m) => m.includes('.test.'))
      .some((m) => m !== f && new RegExp(`\\b${esc}\\b`).test(fs.readFileSync(m, 'utf8')));
    if (!inProduct && inTests) {
      exportOnlyInTests.push({ rel, name });
    }
  }
}

console.log('=== MODULE GRAPH (from main.tsx) ===');
console.log('Production modules:', productionModules.length);
console.log(
  'Reachable from product:',
  productionModules.filter((f) => reachableProduct.has(path.normalize(f))).length,
);
console.log('Unreachable from product:', unreachableFromProduct.length);
console.log('  Test-only support (reachable from *.test.* only):', testOnlySupport.length);
for (const f of testOnlySupport.sort()) {
  console.log('   ', path.relative(srcRoot, f));
}
console.log('  Product orphan candidates:', productOrphanCandidates.length);
for (const f of productOrphanCandidates.sort()) {
  const importers = importersOf(f);
  console.log(`   ${path.relative(srcRoot, f)}`);
  if (importers.length) console.log(`      importers: ${importers.join(', ')}`);
}

console.log('\n=== EXPORTS REFERENCED IN TESTS ONLY (not in product graph text) ===');
console.log('Count:', exportOnlyInTests.length);
for (const { rel, name } of exportOnlyInTests.slice(0, 30)) {
  console.log(`  ${name} @ ${rel}`);
}
if (exportOnlyInTests.length > 30) {
  console.log(`  ... +${exportOnlyInTests.length - 30} more`);
}
