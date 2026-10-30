import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { FALLBACK_LOCALE, I18N_NAMESPACES, SUPPORTED_LOCALES } from './config';
import { localeBackend } from './localeBackend';
import { resolveInitialLocale } from './resolveLocale';

function syncDocumentLang(locale: string) {
  if (typeof document !== 'undefined') {
    document.documentElement.lang = locale;
  }
}

export const i18nReady = i18n
  .use(localeBackend)
  .use(initReactI18next)
  .init({
    lng: resolveInitialLocale(),
    fallbackLng: FALLBACK_LOCALE,
    supportedLngs: [...SUPPORTED_LOCALES],
    load: 'languageOnly',
    ns: [...I18N_NAMESPACES],
    defaultNS: 'common',
    interpolation: {
      escapeValue: false,
    },
    returnNull: false,
    saveMissing: import.meta.env.DEV,
    missingKeyHandler: import.meta.env.DEV
      ? (_lngs, ns, key) => {
          console.warn(`[i18n] missing key: ${ns}:${key}`);
        }
      : undefined,
  })
  .then(() => {
    syncDocumentLang(i18n.language);
  });

i18n.on('languageChanged', syncDocumentLang);

export default i18n;
