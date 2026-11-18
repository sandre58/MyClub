import { readdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const webSrc = join(dirname(fileURLToPath(import.meta.url)), '../..');

const foundationsDir = join(dirname(fileURLToPath(import.meta.url)), '.');

const focusCss = readFileSync(join(foundationsDir, 'focus.css'), 'utf8');
const geometryCss = readFileSync(
  join(webSrc, 'design-system/tokens/geometry.css'),
  'utf8',
);
const formsCss = readFileSync(join(foundationsDir, 'forms.css'), 'utf8');
const dialogCss = readFileSync(join(foundationsDir, 'dialog.css'), 'utf8');

const GLOBAL_FOCUS_SELECTOR =
  '.ds-root :is(a, button, input, select, textarea, summary, [tabindex]):focus-visible';

function listCssFiles(dir: string): string[] {
  const entries = readdirSync(dir, { withFileTypes: true });
  const files: string[] = [];
  for (const entry of entries) {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...listCssFiles(path));
    } else if (entry.name.endsWith('.css')) {
      files.push(path);
    }
  }
  return files;
}

function collectTokenDefinitions(globDir: string): Set<string> {
  const names = new Set<string>();
  for (const file of listCssFiles(globDir)) {
    const content = readFileSync(file, 'utf8');
    for (const match of content.matchAll(/^\s*(--[\w-]+)\s*:/gm)) {
      names.add(match[1]);
    }
  }
  return names;
}

const DEFINED_FOCUS_TOKENS = new Set([
  ...collectTokenDefinitions(join(webSrc, 'design-system/tokens')),
  ...collectTokenDefinitions(join(webSrc, 'design-system/palettes')),
]);
for (const match of focusCss.matchAll(/^\s*(--[\w-]+)\s*:/gm)) {
  DEFINED_FOCUS_TOKENS.add(match[1]);
}
for (const match of geometryCss.matchAll(/^\s*(--[\w-]+)\s*:/gm)) {
  DEFINED_FOCUS_TOKENS.add(match[1]);
}

describe('focus foundation contract', () => {
  it('keeps the global focus-visible selector unchanged', () => {
    expect(focusCss).toMatch(
      new RegExp(GLOBAL_FOCUS_SELECTOR.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')),
    );
  });

  it('does not use --focus-halo on the default .ds-input focus shell rule', () => {
    expect(formsCss).not.toMatch(
      /\.ds-input:focus-within,\s*\n\.ds-input:focus-visible\s*\{[^}]*--focus-halo/,
    );
  });

  it('defines --focus-ring-extent in geometry tokens', () => {
    expect(geometryCss).toMatch(/--focus-ring-extent:\s*calc\(/);
  });

  it('declares focus halo and inset aliases on :root and .ds-root (no --focus-field)', () => {
    expect(focusCss).toMatch(/:root,\s*\n\.ds-root\s*\{[\s\S]*--focus-halo:/);
    expect(focusCss).toMatch(/--focus-inset:/);
    expect(focusCss).not.toMatch(/--focus-field:/);
  });

  it('uses brand border only on edit fields without extra box-shadow', () => {
    expect(focusCss).toMatch(
      /\.ds-root \.ds-input:is\(:focus[\s\S]*border-color:\s*var\(--color-brand\)/,
    );
    expect(focusCss).toMatch(
      /\.ds-root \.ds-input:is\(:focus[\s\S]*box-shadow:\s*none/,
    );
    expect(formsCss).not.toMatch(/--focus-field/);
  });

  it('applies scroll-safe spacing from --focus-ring-extent on dialog body and utility', () => {
    expect(dialogCss).toMatch(
      /\.ds-dialog__body\s*\{[\s\S]*--focus-ring-extent/,
    );
    expect(dialogCss).toMatch(
      /\.ds-scroll-safe\s*\{[\s\S]*--focus-ring-extent/,
    );
  });

  it('includes a forced-colors focus fallback', () => {
    expect(focusCss).toMatch(/@media\s*\(forced-colors:\s*active\)/);
    expect(focusCss).toMatch(/outline:\s*2px solid CanvasText/);
    expect(focusCss).toMatch(
      /\[data-nav='keyboard'\]\s*>\s*li\s*>\s*\[data-active='true'\][\s\S]*outline:\s*2px solid CanvasText/,
    );
  });

  it('keeps page-local scroll-safe hosts from resetting ds-scroll-safe margin and padding', () => {
    const structureDialogs = readFileSync(
      join(webSrc, 'pages/structure/structure-dialogs.css'),
      'utf8',
    );
    const structureComposition = readFileSync(
      join(webSrc, 'pages/structure/structure-composition.css'),
      'utf8',
    );

    for (const selector of [
      'structure-qualification__list',
      'structure-qualification__place-map',
    ]) {
      expect(structureDialogs).toMatch(
        new RegExp(
          `\\.${selector}\\.ds-scroll-safe\\s*\\{[^}]*margin:\\s*calc\\(-1 \\* var\\(--focus-ring-extent\\)\\)`,
        ),
      );
      expect(structureDialogs).toMatch(
        new RegExp(
          `\\.${selector}\\.ds-scroll-safe\\s*\\{[^}]*padding:\\s*var\\(--focus-ring-extent\\)`,
        ),
      );
    }

    expect(structureComposition).toMatch(
      /\.structure-composition__list\.ds-scroll-safe\s*\{[\s\S]*--focus-ring-extent/,
    );

    const teamsCss = readFileSync(
      join(webSrc, 'pages/teams/teams.css'),
      'utf8',
    );
    expect(teamsCss).not.toMatch(
      /\.teams-drawer\s*\{[^}]*padding:\s*var\(--focus-ring-extent\)/,
    );
    expect(teamsCss).toMatch(
      /\.teams-drawer__body\.ds-scroll-safe\s*\{[^}]*margin:\s*calc\(-1 \* var\(--focus-ring-extent\)\)/,
    );
  });

  it('applies portal edit-field border after portal halo so Popover comboboxes avoid outer halo', () => {
    const portalHalo = focusCss.indexOf(
      '.ds-popover :is(a, button, input, select, textarea, summary, [tabindex]):focus-visible',
    );
    const portalField = focusCss.indexOf('.ds-popover .ds-input:is(:focus');
    expect(portalHalo).toBeGreaterThan(-1);
    expect(portalField).toBeGreaterThan(-1);
    expect(portalField).toBeGreaterThan(portalHalo);
    const fieldBlock = focusCss.slice(
      portalField,
      focusCss.indexOf('}', portalField) + 1,
    );
    expect(fieldBlock).toMatch(/border-color:\s*var\(--color-brand\)/);
    expect(fieldBlock).toMatch(/box-shadow:\s*none/);
  });

  it('does not reference undefined focus-related custom properties in web CSS', () => {
    const cssRoot = join(webSrc);
    const files = listCssFiles(cssRoot);
    const used = new Set<string>();
    for (const file of files) {
      const content = readFileSync(file, 'utf8');
      for (const match of content.matchAll(/var\(\s*(--[^),\s]+)/g)) {
        const name = match[1];
        if (name.includes('focus')) {
          used.add(name);
        }
      }
    }

    for (const token of used) {
      expect(DEFINED_FOCUS_TOKENS.has(token)).toBe(true);
    }
  });
});
