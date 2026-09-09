import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import i18n from '../i18n';
import { LOCALE_STORAGE_KEY } from '../i18n/config';
import { THEME_STORAGE_KEY } from '../theme/config';
import { resetThemeState, setThemePreference } from '../theme/setThemePreference';
import { PreferencesMenu } from './PreferencesMenu';
import { renderWithI18n } from '../test/renderWithI18n';

describe('PreferencesMenu', () => {
  afterEach(async () => {
    window.localStorage.removeItem(LOCALE_STORAGE_KEY);
    window.localStorage.removeItem(THEME_STORAGE_KEY);
    resetThemeState('system');
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.colorScheme = '';
    await i18n.changeLanguage('fr');
    vi.restoreAllMocks();
  });

  it('opens preferences and switches language to English', async () => {
    const user = userEvent.setup();
    renderWithI18n(<PreferencesMenu />);

    await user.click(
      screen.getByRole('button', { name: 'Ouvrir les préférences' }),
    );

    const dialog = screen.getByRole('dialog', { name: 'Préférences' });
    expect(within(dialog).getByText('Langue')).toBeInTheDocument();

    await user.click(within(dialog).getByRole('combobox'));
    await user.click(screen.getByRole('option', { name: 'English' }));

    expect(i18n.language).toBe('en');
    expect(document.documentElement.lang).toBe('en');
    expect(window.localStorage.getItem(LOCALE_STORAGE_KEY)).toBe('en');
  });

  it('switches theme to dark via preference listbox', async () => {
    const user = userEvent.setup();
    renderWithI18n(
      <div className="ds-root">
        <PreferencesMenu />
      </div>,
    );

    await user.click(
      screen.getByRole('button', { name: 'Ouvrir les préférences' }),
    );

    const dialog = screen.getByRole('dialog', { name: 'Préférences' });
    await user.click(within(dialog).getByRole('radio', { name: 'Sombre' }));

    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
    expect(document.documentElement.dataset.theme).toBe('dark');
  });

  it('keeps system selected when OS is dark, not dark option', async () => {
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

    setThemePreference('system');
    const user = userEvent.setup();
    renderWithI18n(
      <div className="ds-root">
        <PreferencesMenu />
      </div>,
    );

    await user.click(
      screen.getByRole('button', { name: 'Ouvrir les préférences' }),
    );

    const dialog = screen.getByRole('dialog', { name: 'Préférences' });
    expect(
      within(dialog).getByRole('radio', { name: 'Système' }),
    ).toHaveAttribute('aria-checked', 'true');
    expect(
      within(dialog).getByRole('radio', { name: 'Sombre' }),
    ).toHaveAttribute('aria-checked', 'false');
    expect(document.documentElement.dataset.theme).toBe('dark');
  });
});
