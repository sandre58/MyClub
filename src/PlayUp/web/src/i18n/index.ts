import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import {
  FALLBACK_LOCALE,
  I18N_NAMESPACES,
  SUPPORTED_LOCALES,
  type SupportedLocale,
} from './config';
import { localeBackend } from './localeBackend';
import { resolveInitialLocale } from './resolveLocale';

function syncDocumentLang(locale: string) {
  if (typeof document !== 'undefined') {
    document.documentElement.lang = locale;
  }
}

function inactiveLocale(language: string): SupportedLocale {
  return language === 'fr' ? 'en' : 'fr';
}

/** Captured once after init — false when only the boot locale was loaded. */
let inactiveLocaleLoadedAtInitialBoot: boolean | null = null;

export function wasInactiveLocaleLoadedAtInitialBoot(): boolean {
  if (inactiveLocaleLoadedAtInitialBoot === null) {
    throw new Error('i18n init has not finished');
  }
  return inactiveLocaleLoadedAtInitialBoot;
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
    const other = inactiveLocale(i18n.language);
    inactiveLocaleLoadedAtInitialBoot = I18N_NAMESPACES.some((ns) =>
      i18n.hasResourceBundle(other, ns),
    );
    syncDocumentLang(i18n.language);
  });

i18n.on('languageChanged', syncDocumentLang);

export default i18n;
