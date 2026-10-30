import type { i18n as I18nInstance } from 'i18next';

type ChangeLanguage = I18nInstance['changeLanguage'];

export async function mountAfterI18nReady(options: {
  i18nReady: Promise<void>;
  changeLanguage: ChangeLanguage;
  fallbackLocale: string;
  render: () => void;
  logError: (message: string, error: unknown) => void;
}): Promise<void> {
  const { i18nReady, changeLanguage, fallbackLocale, render, logError } =
    options;

  try {
    await i18nReady;
  } catch (error) {
    logError(
      '[i18n] Initial locale failed to load; mounting with fallback.',
      error,
    );
    try {
      await changeLanguage(fallbackLocale);
    } catch (fallbackError) {
      logError('[i18n] Fallback locale failed to load.', fallbackError);
    }
  }

  render();
}
