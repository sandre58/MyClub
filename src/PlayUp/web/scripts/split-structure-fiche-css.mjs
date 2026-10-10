import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const dir = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  '../src/pages/structure',
);
const hubPath = path.join(dir, 'structure-hub.css');
const lines = fs.readFileSync(hubPath, 'utf8').split('\n');

const ficheRanges = [
  [35, 86],
  [344, 1084],
];

const ficheBody = ficheRanges
  .map(([start, end]) => lines.slice(start - 1, end).join('\n'))
  .join('\n\n');

const ficheHeader = `/*
 * Structure phase fiche — detail card, flux rails, schematic hero.
 * Loaded via structure.css (after structure-hub.css).
 */

`;

fs.writeFileSync(path.join(dir, 'structure-fiche.css'), ficheHeader + ficheBody + '\n');

const hubKeep = [
  ...lines.slice(0, 34),
  ...lines.slice(87, 343),
].join('\n');

const hubHeader = `/*
 * Structure hub — topology master list (master-detail layout).
 * Loaded via structure.css.
 */

`;

fs.writeFileSync(hubPath, hubHeader + hubKeep.replace(/^\/\*[\s\S]*?\*\/\s*/m, '') + '\n');

const structureCssPath = path.join(dir, 'structure.css');
let structureCss = fs.readFileSync(structureCssPath, 'utf8');
if (!structureCss.includes('structure-fiche.css')) {
  structureCss = structureCss.replace(
    "@import './structure-hub.css';",
    "@import './structure-hub.css';\n@import './structure-fiche.css';",
  );
  fs.writeFileSync(structureCssPath, structureCss);
}

console.log('Wrote structure-fiche.css and trimmed structure-hub.css');
