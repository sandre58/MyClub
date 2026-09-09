import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it } from 'vitest';
import i18n from '../i18n';
import { LOCALE_STORAGE_KEY } from '../i18n/config';
import { PreferencesMenu } from './PreferencesMenu';
import { renderWithI18n } from '../test/renderWithI18n';

describe('PreferencesMenu', () => {
  afterEach(async () => {
    window.localStorage.removeItem(LOCALE_STORAGE_KEY);
    await i18n.changeLanguage('fr');
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
});
