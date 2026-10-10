import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const structureDir = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  '../src/pages/structure',
);
const hubPath = path.join(structureDir, 'structure-hub.css');
const lines = fs.readFileSync(hubPath, 'utf8').split('\n');

function slice(start, end) {
  return lines.slice(start - 1, end).join('\n');
}

const operationalHeader = `/*
 * Structure operational UI — phase bar, strip, tiles, occupancy meters.
 * Loaded via structure.css (after structure-hub.css).
 */

`;

const operationalBody =
  slice(579, 649) +
  '\n\n' +
  slice(1164, 1985);

fs.writeFileSync(
  path.join(structureDir, 'structure-operational.css'),
  operationalHeader + operationalBody + '\n',
);

const hubKeep =
  lines.slice(0, 578).join('\n') +
  '\n\n' +
  lines.slice(1986).join('\n');

const hubHeader = `/*
 * Structure hub — topology + phase fiche (master-detail).
 * Loaded via structure.css.
 */

`;

fs.writeFileSync(hubPath, hubHeader + hubKeep.replace(/^\/\*[\s\S]*?\*\/\s*/m, ''));

console.log('Wrote structure-operational.css and trimmed structure-hub.css');
