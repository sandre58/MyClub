import { afterEach, describe, expect, it, vi } from 'vitest';
import { THEME_STORAGE_KEY } from './config';
import {
  getThemePreference,
  resetThemeState,
  setThemePreference,
} from './setThemePreference';

describe('setThemePreference', () => {
  afterEach(() => {
    window.localStorage.removeItem(THEME_STORAGE_KEY);
    resetThemeState('system');
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.colorScheme = '';
    vi.restoreAllMocks();
  });

  it('applies dark theme and stores preference', () => {
    setThemePreference('dark');

    expect(getThemePreference()).toBe('dark');
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
    expect(document.documentElement.dataset.theme).toBe('dark');
  });

  it('reacts to OS changes only when preference is system', () => {
    let changeHandler: (() => void) | null = null;

    vi.spyOn(window, 'matchMedia').mockReturnValue({
      matches: false,
      media: '(prefers-color-scheme: dark)',
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: (_event, handler) => {
        changeHandler = handler as () => void;
      },
      removeEventListener: () => {
        changeHandler = null;
      },
      dispatchEvent: () => false,
    } as MediaQueryList);

    setThemePreference('system');
    expect(document.documentElement.dataset.theme).toBe('light');

    vi.spyOn(window, 'matchMedia').mockReturnValue({
      matches: true,
      media: '(prefers-color-scheme: dark)',
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: (_event, handler) => {
        changeHandler = handler as () => void;
      },
      removeEventListener: () => {
        changeHandler = null;
      },
      dispatchEvent: () => false,
    } as MediaQueryList);

    changeHandler?.();
    expect(document.documentElement.dataset.theme).toBe('dark');

    setThemePreference('light');
    changeHandler?.();
    expect(document.documentElement.dataset.theme).toBe('light');
  });
});
