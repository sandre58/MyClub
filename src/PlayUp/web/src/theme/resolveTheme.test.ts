import { afterEach, describe, expect, it, vi } from 'vitest';
import { resolveTheme } from './resolveTheme';

describe('resolveTheme', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('maps light and dark preferences directly', () => {
    expect(resolveTheme('light')).toBe('light');
    expect(resolveTheme('dark')).toBe('dark');
  });

  it('maps system to dark when OS prefers dark', () => {
    vi.spyOn(window, 'matchMedia').mockReturnValue({
      matches: true,
      media: '(prefers-color-scheme: dark)',
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    } as MediaQueryList);

    expect(resolveTheme('system')).toBe('dark');
  });

  it('maps system to light when OS prefers light', () => {
    vi.spyOn(window, 'matchMedia').mockReturnValue({
      matches: false,
      media: '(prefers-color-scheme: dark)',
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    } as MediaQueryList);

    expect(resolveTheme('system')).toBe('light');
  });
});
