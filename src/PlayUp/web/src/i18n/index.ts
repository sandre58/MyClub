import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { FALLBACK_LOCALE, I18N_NAMESPACES } from './config';
import { resolveInitialLocale } from './resolveLocale';
import actionsEn from './locales/en/actions.json';
import actionsFr from './locales/fr/actions.json';
import classementsEn from './locales/en/classements.json';
import classementsFr from './locales/fr/classements.json';
import overviewEn from './locales/en/overview.json';
import overviewFr from './locales/fr/overview.json';
import commonEn from './locales/en/common.json';
import commonFr from './locales/fr/common.json';
import competitionsEn from './locales/en/competitions.json';
import competitionsFr from './locales/fr/competitions.json';
import drawEn from './locales/en/draw.json';
import drawFr from './locales/fr/draw.json';
import enumsEn from './locales/en/enums.json';
import enumsFr from './locales/fr/enums.json';
import errorsEn from './locales/en/errors.json';
import errorsFr from './locales/fr/errors.json';
import homeEn from './locales/en/home.json';
import homeFr from './locales/fr/home.json';
import matchesEn from './locales/en/matches.json';
import matchesFr from './locales/fr/matches.json';
import organisationEn from './locales/en/organisation.json';
import organisationFr from './locales/fr/organisation.json';
import shellEn from './locales/en/shell.json';
import shellFr from './locales/fr/shell.json';
import teamsEn from './locales/en/teams.json';
import teamsFr from './locales/fr/teams.json';
import regulationEn from './locales/en/regulation.json';
import regulationFr from './locales/fr/regulation.json';
import stageEn from './locales/en/stage.json';
import stageFr from './locales/fr/stage.json';

function syncDocumentLang(locale: string) {
  if (typeof document !== 'undefined') {
    document.documentElement.lang = locale;
  }
}

void i18n.use(initReactI18next).init({
  lng: resolveInitialLocale(),
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
    en: {
      common: commonEn,
      shell: shellEn,
      enums: enumsEn,
      errors: errorsEn,
      actions: actionsEn,
      overview: overviewEn,
      matches: matchesEn,
      draw: drawEn,
      organisation: organisationEn,
      teams: teamsEn,
      regulation: regulationEn,
      stage: stageEn,
      home: homeEn,
      competitions: competitionsEn,
      classements: classementsEn,
    },
  },
  interpolation: {
    escapeValue: false,
  },
  returnNull: false,
  // Surface missing keys during development without crashing the UI.
  saveMissing: import.meta.env.DEV,
  missingKeyHandler: import.meta.env.DEV
    ? (_lngs, ns, key) => {
        console.warn(`[i18n] missing key: ${ns}:${key}`);
      }
    : undefined,
});

syncDocumentLang(i18n.language);
i18n.on('languageChanged', syncDocumentLang);

export default i18n;
