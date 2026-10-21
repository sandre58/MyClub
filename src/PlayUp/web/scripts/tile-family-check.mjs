/**
 * Phase 4 guard — family B root tiles must compose `.ds-selectable-tile`.
 * Flags className values ending in `__tile` (not `__tile-head`, etc.) without
 * SelectableTile / ds-selectable-tile nearby.
 */
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

const rootTileClass = /className=["'`][^"'`]*__tile["'`]/;
const innerTileClass = /__tile-/;
const familyBMarker = /SelectableTile|ds-selectable-tile/;

const allowlist = [/structure-attribution__tile/];

const tsxFiles = walk(srcRoot).filter((f) => f.endsWith('.tsx'));
const violations = [];

for (const file of tsxFiles) {
  const rel = path.relative(srcRoot, file);
  const lines = fs.readFileSync(file, 'utf8').split('\n');
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (!rootTileClass.test(line) || innerTileClass.test(line)) continue;
    if (allowlist.some((re) => re.test(line))) continue;
    const window = lines.slice(Math.max(0, i - 6), i + 10).join('\n');
    if (familyBMarker.test(window)) continue;
    if (/['"]structure-qualification__tile['"]/.test(line)) {
      if (familyBMarker.test(window)) continue;
    }
    violations.push(`${rel}:${i + 1}: ${line.trim()}`);
  }
}

console.log('=== Tile family B (root *__tile + ds-selectable-tile) ===');
if (violations.length === 0) {
  console.log('OK');
} else {
  console.log(`Gaps (${violations.length}):`);
  for (const v of violations) console.log(`  ${v}`);
}

process.exit(violations.length > 0 ? 1 : 0);
