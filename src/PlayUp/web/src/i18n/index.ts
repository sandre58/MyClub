import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import {
  DEFAULT_LOCALE,
  FALLBACK_LOCALE,
  I18N_NAMESPACES,
} from './config'
import commonFr from './locales/fr/common.json'
import enumsFr from './locales/fr/enums.json'
import shellFr from './locales/fr/shell.json'

function syncDocumentLang(locale: string) {
  if (typeof document !== 'undefined') {
    document.documentElement.lang = locale
  }
}

void i18n.use(initReactI18next).init({
  lng: DEFAULT_LOCALE,
  fallbackLng: FALLBACK_LOCALE,
  ns: [...I18N_NAMESPACES],
  defaultNS: 'common',
  resources: {
    fr: {
      common: commonFr,
      shell: shellFr,
      enums: enumsFr,
    },
  },
  interpolation: {
    escapeValue: false,
  },
  returnNull: false,
  // Surface missing keys during development without crashing the UI.
  saveMissing: import.meta.env.DEV,
})

syncDocumentLang(i18n.language)
i18n.on('languageChanged', syncDocumentLang)

export default i18n
