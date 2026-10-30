import '@testing-library/jest-dom/vitest';
import { cleanup, configure } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterEach, beforeAll } from 'vitest';
import { i18nReady } from '../i18n';
import { THEME_STORAGE_KEY } from '../theme/config';
import { resetThemeState } from '../theme/setThemePreference';
import { I18nTestProvider } from './renderWithI18n';

beforeAll(async () => {
  await i18nReady;
});

if (typeof window.matchMedia !== 'function') {
  window.matchMedia = (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  });
}

// Global wrapper for plain `render()` in tests (RTL types omit `wrapper` on configure).
configure({
  wrapper: ({ children }: { children: ReactNode }) => (
    <I18nTestProvider>{children}</I18nTestProvider>
  ),
} as Parameters<typeof configure>[0]);

afterEach(() => {
  window.localStorage.removeItem(THEME_STORAGE_KEY);
  resetThemeState('system');
  document.documentElement.removeAttribute('data-theme');
  document.documentElement.style.colorScheme = '';
  cleanup();
});

if (typeof Element.prototype.scrollIntoView !== 'function') {
  Element.prototype.scrollIntoView = () => undefined;
}
