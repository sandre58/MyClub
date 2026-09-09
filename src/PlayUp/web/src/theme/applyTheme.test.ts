import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { applyTheme } from './applyTheme';

describe('applyTheme', () => {
  let root: HTMLDivElement;

  beforeEach(() => {
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.colorScheme = '';
    root = document.createElement('div');
    root.className = 'ds-root';
    document.body.appendChild(root);
  });

  afterEach(() => {
    root.remove();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.colorScheme = '';
  });

  it('sets data-theme and color-scheme on document and ds-root nodes', () => {
    applyTheme('dark');

    expect(document.documentElement.dataset.theme).toBe('dark');
    expect(document.documentElement.style.colorScheme).toBe('dark');
    expect(root.getAttribute('data-theme')).toBe('dark');
  });

  it('is idempotent when called again', () => {
    applyTheme('light');
    applyTheme('dark');

    expect(root.getAttribute('data-theme')).toBe('dark');
  });
});
