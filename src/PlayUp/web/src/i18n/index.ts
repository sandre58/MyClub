import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import {
  DEFAULT_LOCALE,
  FALLBACK_LOCALE,
  I18N_NAMESPACES,
} from './config'
import actionsFr from './locales/fr/actions.json'
import classementsFr from './locales/fr/classements.json'
import overviewFr from './locales/fr/overview.json'
import commonFr from './locales/fr/common.json'
import competitionsFr from './locales/fr/competitions.json'
import drawFr from './locales/fr/draw.json'
import enumsFr from './locales/fr/enums.json'
import errorsFr from './locales/fr/errors.json'
import homeFr from './locales/fr/home.json'
import matchesFr from './locales/fr/matches.json'
import organisationFr from './locales/fr/organisation.json'
import shellFr from './locales/fr/shell.json'
import teamsFr from './locales/fr/teams.json'
import regulationFr from './locales/fr/regulation.json'
import stageFr from './locales/fr/stage.json'

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
      errors: errorsFr,
      actions: actionsFr,
      overview: overviewFr,
      matches: matchesFr,
      draw: drawFr,
      organisation: organisationFr,
      teams: teamsFr,
      regulation: regulationFr,
      stage: stageFr,
      home: homeFr,
      competitions: competitionsFr,
      classements: classementsFr,
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
