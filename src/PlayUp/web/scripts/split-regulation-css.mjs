import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const regDir = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  '../src/pages/regulation',
);
const regPath = path.join(regDir, 'regulation.css');
const lines = fs.readFileSync(regPath, 'utf8').split('\n');

function slice(start, end) {
  return lines.slice(start - 1, end).join('\n');
}

const sharedHeader = `/*
 * Regulation previews + editors — shared across Règlement page, Structure fiche, Stage dialogs.
 * Import this instead of regulation.css when page shell / phase list chrome is not needed.
 */

`;

const sharedBody = slice(181, 803) + '\n\n' + slice(1065, lines.length);

fs.writeFileSync(path.join(regDir, 'regulation-shared.css'), sharedHeader + sharedBody + '\n');

const pageHeader = `/* Regulation hub — page shell, cadre frame, phase list. */

@import './regulation-shared.css';

`;

const pageBody = slice(1, 180) + '\n\n' + slice(804, 1064);

fs.writeFileSync(regPath, pageHeader + pageBody + '\n');

console.log('Wrote regulation-shared.css and trimmed regulation.css');
