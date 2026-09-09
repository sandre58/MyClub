import '@testing-library/jest-dom/vitest';
import { cleanup, configure } from '@testing-library/react';
import { afterEach } from 'vitest';
import '../i18n';
import { THEME_STORAGE_KEY } from '../theme/config';
import { resetThemeState } from '../theme/setThemePreference';
import { I18nTestProvider } from './renderWithI18n';

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

configure({
  wrapper: ({ children }) => <I18nTestProvider>{children}</I18nTestProvider>,
});

afterEach(() => {
  window.localStorage.removeItem(THEME_STORAGE_KEY);
  resetThemeState('system');
  document.documentElement.removeAttribute('data-theme');
  document.documentElement.style.colorScheme = '';
  cleanup();
});
