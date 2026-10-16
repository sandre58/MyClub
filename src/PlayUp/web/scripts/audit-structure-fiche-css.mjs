import fs from 'node:fs';
import path from 'node:path';

const cssPath = 'src/pages/structure/structure-fiche.css';
const css = fs.readFileSync(cssPath, 'utf8');
const classes = [...css.matchAll(/\.([a-zA-Z0-9_-]+)/g)].map((m) => m[1]);
const unique = [...new Set(classes)];

/** Class stems referenced via template literals (e.g. `foo--${tone}`). */
const DYNAMIC_STEMS = [
  'structure-place-rank--',
  'structure-phase-draw-hint--',
  'structure-phase-rail--',
];

function walk(dir, files = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (entry.name === 'node_modules') continue;
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(full, files);
    else if (/\.(tsx?|jsx?|css|html|md)$/.test(entry.name)) files.push(full);
  }
  return files;
}

const files = walk('src').filter((f) => !f.includes('structure-fiche.css'));
const dead = [];

for (const cls of unique.sort()) {
  if (files.some((file) => fs.readFileSync(file, 'utf8').includes(cls))) {
    continue;
  }
  if (DYNAMIC_STEMS.some((stem) => cls.startsWith(stem))) {
    continue;
  }
  dead.push(cls);
}

console.log(`structure-fiche.css: ${dead.length} unused / ${unique.length} unique tokens`);
for (const cls of dead) {
  console.log(`  ${cls}`);
}

process.exit(dead.length > 0 ? 0 : 0);
