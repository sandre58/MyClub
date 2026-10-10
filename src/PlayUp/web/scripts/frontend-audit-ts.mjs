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

const allModules = walk(srcRoot).filter(
  (f) =>
    /\.tsx?$/.test(f) &&
    !f.includes('.test.') &&
    !f.endsWith('.d.ts') &&
    !f.includes(`${path.sep}i18n${path.sep}locales${path.sep}`),
);

function resolveImport(fromFile, spec) {
  if (!spec.startsWith('.')) return null;
  const base = path.join(path.dirname(fromFile), spec);
  const candidates = [
    base,
    `${base}.tsx`,
    `${base}.ts`,
    path.join(base, 'index.tsx'),
    path.join(base, 'index.ts'),
  ];
  for (const c of candidates) {
    if (fs.existsSync(c) && fs.statSync(c).isFile()) return path.normalize(c);
  }
  return null;
}

const reachable = new Set();
const queue = [path.join(srcRoot, 'main.tsx')];

while (queue.length) {
  const file = queue.shift();
  if (reachable.has(file)) continue;
  if (!fs.existsSync(file)) continue;
  reachable.add(file);
  const content = fs.readFileSync(file, 'utf8');
  for (const m of content.matchAll(/from\s+['"]([^'"]+)['"]/g)) {
    const resolved = resolveImport(file, m[1]);
    if (resolved) queue.push(resolved);
  }
  for (const m of content.matchAll(/import\s*\(\s*['"]([^'"]+)['"]\s*\)/g)) {
    const resolved = resolveImport(file, m[1]);
    if (resolved) queue.push(resolved);
  }
}

const orphanModules = allModules.filter((f) => !reachable.has(path.normalize(f)));

console.log('=== TS MODULE REACHABILITY (from main.tsx, static + lazy) ===');
console.log('Total non-test modules:', allModules.length);
console.log('Reachable:', reachable.size);
console.log('Orphan modules:', orphanModules.length);
for (const f of orphanModules.sort()) {
  console.log(' ', path.relative(srcRoot, f));
}

// structure-specific: files only imported by tests?
const testFiles = walk(srcRoot).filter((f) => f.includes('.test.'));
const importedByProd = new Set(reachable);
const onlyTestImported = [];
for (const mod of allModules) {
  if (reachable.has(path.normalize(mod))) continue;
  let inTest = false;
  for (const tf of testFiles) {
    const c = fs.readFileSync(tf, 'utf8');
    const rel = path.relative(path.dirname(tf), mod).replace(/\\/g, '/').replace(/\.tsx?$/, '');
    if (c.includes(rel) || c.includes(path.basename(mod, path.extname(mod)))) inTest = true;
  }
  if (inTest) onlyTestImported.push(path.relative(srcRoot, mod));
}
console.log('\nOrphan but referenced in tests:', onlyTestImported.length);
