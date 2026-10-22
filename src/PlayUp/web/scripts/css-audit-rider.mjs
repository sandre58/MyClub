/**
 * Rider CSS helpers: report unknown custom properties; --fix joins broken selector lines only.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const srcRoot = path.join(webRoot, 'src');
const fix = process.argv.includes('--fix');

function walk(dir, out = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) walk(p, out);
    else if (ent.name.endsWith('.css')) out.push(p);
  }
  return out;
}

function isCommentLine(line) {
  const t = line.trim();
  if (!t) return true;
  if (t.startsWith('/*')) return true;
  if (t.startsWith('*') && !t.startsWith('*/')) return true;
  if (t.endsWith('*/')) return true;
  return false;
}

function looksLikeSelectorContinuation(prev, next) {
  const p = prev.trimEnd();
  const n = next.trimStart();
  if (!p || !n) return false;
  if (isCommentLine(prev) || isCommentLine(next)) return false;
  if (p.endsWith('{') || p.endsWith(',') || p.endsWith(';')) return false;
  if (/^@(import|charset|keyframes|media|container)/i.test(p)) return false;
  const combinatorContinuation = /^[+~>]/.test(n);
  if (!/^[.#\[>:]/.test(n) && !combinatorContinuation) return false;
  if (p.includes('{')) return false;
  if (combinatorContinuation) {
    return p.endsWith(']') || p.endsWith(')') || /[.#\[:[]/.test(p);
  }
  return /[.#\[:[]/.test(p) || p.endsWith(')') || p.endsWith(']');
}

/** Join Rider-invalid selector wraps (does not touch comments or declaration blocks). */
function joinBrokenSelectorLines(content) {
  const lines = content.split('\n');
  const out = [];
  for (let i = 0; i < lines.length; i++) {
    let line = lines[i];
    while (i + 1 < lines.length && looksLikeSelectorContinuation(line, lines[i + 1])) {
      line = `${line.trimEnd()} ${lines[i + 1].trimStart()}`;
      i++;
    }
    out.push(line);
  }
  return out.join('\n');
}

const DEF_RE = /(?:^|[\s{;])(--[\w-]+)\s*:/gm;
const VAR_RE = /var\(\s*(--[\w-]+)/g;

const files = walk(srcRoot);

function report() {
  const defined = new Set();
  for (const f of files) {
    for (const m of fs.readFileSync(f, 'utf8').matchAll(DEF_RE)) defined.add(m[1]);
  }

  const unknownVars = new Map();
  let brokenLineCount = 0;

  for (const f of files) {
    const rel = path.relative(srcRoot, f);
    const content = fs.readFileSync(f, 'utf8');
    const lines = content.split('\n');
    for (let i = 0; i < lines.length - 1; i++) {
      if (looksLikeSelectorContinuation(lines[i], lines[i + 1])) {
        brokenLineCount++;
        if (brokenLineCount <= 25) {
          console.log(`  ${rel}:${i + 1}`);
        }
      }
    }
    for (const m of content.matchAll(VAR_RE)) {
      const name = m[1];
      if (!defined.has(name)) {
        const line = content.slice(0, m.index).split('\n').length;
        if (!unknownVars.has(name)) unknownVars.set(name, []);
        unknownVars.get(name).push({ file: rel, line });
      }
    }
  }

  console.log('=== Rider-style broken selector wraps ===');
  console.log(`Count: ${brokenLineCount}`);

  console.log('\n=== var(--*) with no --* definition in src CSS ===');
  for (const [name, hits] of [...unknownVars.entries()].sort()) {
    console.log(`  ${name} — ${hits[0].file}:${hits[0].line}`);
  }
}

report();

if (fix) {
  let n = 0;
  for (const f of files) {
    const raw = fs.readFileSync(f, 'utf8');
    const next = joinBrokenSelectorLines(raw);
    if (next !== raw) {
      fs.writeFileSync(f, next);
      n++;
      console.log(`\nFixed: ${path.relative(srcRoot, f)}`);
    }
  }
  console.log(`\n=== --fix: ${n} file(s) ===`);
  report();
}
