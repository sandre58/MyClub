import type { BackendModule } from 'i18next';
import { I18N_NAMESPACES, isSupportedLocale } from './config';

function isRegisteredNamespace(
  namespace: string,
): namespace is (typeof I18N_NAMESPACES)[number] {
  return (I18N_NAMESPACES as readonly string[]).includes(namespace);
}

/** Loads one namespace JSON for a supported locale (Vite code-splits per file). */
export const localeBackend: BackendModule = {
  type: 'backend',
  init() {},
  read(language, namespace, callback) {
    if (!isSupportedLocale(language) || !isRegisteredNamespace(namespace)) {
      callback(
        new Error(`Unknown locale bundle: ${language}/${namespace}`),
        null,
      );
      return;
    }

    void import(`./locales/${language}/${namespace}.json`)
      .then((module) => {
        callback(null, module.default as Record<string, unknown>);
      })
      .catch((error: Error) => {
        callback(error, null);
      });
  },
};
