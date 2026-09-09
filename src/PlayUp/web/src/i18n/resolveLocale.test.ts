import { afterEach, describe, expect, it } from 'vitest';
import {
  DEFAULT_LOCALE,
  LOCALE_STORAGE_KEY,
  type SupportedLocale,
} from './config';
import { toIntlLocale } from './intlLocale';
import {
  getStoredLocale,
  resolveInitialLocale,
  setStoredLocale,
} from './resolveLocale';

describe('toIntlLocale', () => {
  it('maps fr to fr-FR and en to en-GB', () => {
    expect(toIntlLocale('fr')).toBe('fr-FR');
    expect(toIntlLocale('en')).toBe('en-GB');
    expect(toIntlLocale('en-US')).toBe('en-GB');
  });

  it('falls back unknown languages to fr-FR', () => {
    expect(toIntlLocale('es')).toBe('fr-FR');
  });
});

describe('resolveInitialLocale', () => {
  afterEach(() => {
    window.localStorage.removeItem(LOCALE_STORAGE_KEY);
  });

  it('uses stored locale when valid', () => {
    setStoredLocale('en');
    expect(resolveInitialLocale()).toBe('en');
    expect(getStoredLocale()).toBe('en');
  });

  it('defaults to fr in test mode without storage', () => {
    expect(resolveInitialLocale()).toBe(DEFAULT_LOCALE);
  });

  it('ignores invalid stored values', () => {
    window.localStorage.setItem(LOCALE_STORAGE_KEY, 'de');
    expect(resolveInitialLocale()).toBe(DEFAULT_LOCALE);
  });
});

describe('setStoredLocale', () => {
  afterEach(() => {
    window.localStorage.removeItem(LOCALE_STORAGE_KEY);
  });

  it('persists supported locales', () => {
    const locale: SupportedLocale = 'en';
    setStoredLocale(locale);
    expect(window.localStorage.getItem(LOCALE_STORAGE_KEY)).toBe('en');
  });
});
