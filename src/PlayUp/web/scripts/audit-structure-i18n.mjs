import fs from 'node:fs';
import path from 'node:path';

function flatten(obj, prefix = '', out = []) {
  for (const [key, value] of Object.entries(obj)) {
    const full = prefix ? `${prefix}.${key}` : key;
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      flatten(value, full, out);
    } else {
      out.push(full);
    }
  }
  return out;
}

function walk(dir, files = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (entry.name === 'node_modules') continue;
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(full, files);
    else if (/\.(tsx?|jsx?)$/.test(entry.name)) files.push(full);
  }
  return files;
}

function pluralBase(key) {
  return key.replace(/_(zero|one|two|few|many|other)$/, '');
}

/** Prefixes used in template literals: t(`prefix.${var}`) */
const DYNAMIC_PREFIXES = [
  'hub.drawExecution.',
  'graph.issues.',
  'graph.issuesShort.',
  'readiness.nextStep.',
  'lifecycle.disabled.',
  'lifecycle.state.',
  'lifecycle.mutation.',
  'problems.lifecycle.',
  'fiche.materialize.disabledReason.',
  'fiche.dematerialize.disabledReason.',
  'fiche.drawWorkflow.createBlocked.short.',
  'fiche.drawWorkflow.createBlocked.detail.',
  'diagnostics.',
];

function isReferenced(key, corpus) {
  const candidates = new Set([key, pluralBase(key)]);
  for (const candidate of candidates) {
    if (
      corpus.includes(`'${candidate}'`) ||
      corpus.includes(`"${candidate}"`) ||
      corpus.includes(`structure:${candidate}`) ||
      corpus.includes(`\`${candidate}`) ||
      corpus.includes(`${candidate}.`) ||
      corpus.includes(`${candidate}\``)
    ) {
      return true;
    }
  }
  if (DYNAMIC_PREFIXES.some((prefix) => key.startsWith(prefix))) {
    return true;
  }
  const segments = key.split('.');
  if (segments.length > 1) {
    const parent = segments.slice(0, -1).join('.');
    if (corpus.includes(`\`${parent}.`) || corpus.includes(`'${parent}.`)) {
      return true;
    }
  }
  return false;
}

const keys = flatten(
  JSON.parse(fs.readFileSync('src/i18n/locales/en/structure.json', 'utf8')),
);
const corpus = walk('src')
  .map((f) => fs.readFileSync(f, 'utf8'))
  .join('\n');

const suspect = keys.filter((key) => !isReferenced(key, corpus));

console.log(
  `structure.json (en): ${suspect.length} suspect / ${keys.length} leaf keys`,
);
console.log(
  'Suspect keys may still be used via backend, tests-only FR parity, or undocumented templates — verify before deleting.',
);
for (const key of suspect) {
  console.log(`  ${key}`);
}
