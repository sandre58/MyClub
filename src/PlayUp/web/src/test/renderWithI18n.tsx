import { render, type RenderOptions } from '@testing-library/react';
import { I18nextProvider } from 'react-i18next';
import type { ReactElement, ReactNode } from 'react';
import i18n from '../i18n';

/** Wraps children with the same i18n instance as the app (locale `fr`). */
export function I18nTestProvider({ children }: { children: ReactNode }) {
  return <I18nextProvider i18n={i18n}>{children}</I18nextProvider>;
}

export function renderWithI18n(
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) {
  return render(ui, {
    ...options,
    wrapper: ({ children }) => <I18nTestProvider>{children}</I18nTestProvider>,
  });
}
