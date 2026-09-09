import { afterEach, describe, expect, it } from 'vitest';
import { DEFAULT_THEME_PREFERENCE, THEME_STORAGE_KEY } from './config';
import {
  getStoredTheme,
  resolvePreference,
  setStoredTheme,
} from './storage';

describe('theme storage', () => {
  afterEach(() => {
    window.localStorage.removeItem(THEME_STORAGE_KEY);
  });

  it('returns null when nothing is stored', () => {
    expect(getStoredTheme()).toBeNull();
  });

  it('persists supported preferences', () => {
    setStoredTheme('dark');
    expect(getStoredTheme()).toBe('dark');
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
  });

  it('resolvePreference falls back to system default', () => {
    expect(resolvePreference()).toBe(DEFAULT_THEME_PREFERENCE);
  });

  it('resolvePreference uses stored value when valid', () => {
    setStoredTheme('light');
    expect(resolvePreference()).toBe('light');
  });

  it('ignores invalid stored values', () => {
    window.localStorage.setItem(THEME_STORAGE_KEY, 'sepia');
    expect(getStoredTheme()).toBeNull();
    expect(resolvePreference()).toBe(DEFAULT_THEME_PREFERENCE);
  });
});
