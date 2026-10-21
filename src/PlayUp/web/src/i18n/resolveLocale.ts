import {
  DEFAULT_LOCALE,
  LOCALE_STORAGE_KEY,
  isSupportedLocale,
  type SupportedLocale,
} from './config';

function readStoredLocale(): SupportedLocale | null {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    const stored = window.localStorage.getItem(LOCALE_STORAGE_KEY);
    if (stored && isSupportedLocale(stored)) {
      return stored;
    }
  } catch {
    // private mode / blocked storage — fall through
  }

  return null;
}

function localeFromNavigator(): SupportedLocale {
  if (typeof navigator === 'undefined') {
    return DEFAULT_LOCALE;
  }

  const candidates = [
    navigator.language,
    ...(navigator.languages ?? []),
  ].filter(Boolean);

  for (const candidate of candidates) {
    const base = candidate.split('-')[0]?.toLowerCase();
    if (base === 'en') {
      return 'en';
    }
  }

  return DEFAULT_LOCALE;
}

/**
 * Boot order: valid localStorage → navigator en-* → fr.
 * In Vitest, skip navigator so the suite stays on the product default (fr)
 * unless a test explicitly sets playup:locale.
 */
export function resolveInitialLocale(): SupportedLocale {
  const stored = readStoredLocale();
  if (stored) {
    return stored;
  }

  if (import.meta.env.MODE === 'test') {
    return DEFAULT_LOCALE;
  }

  return localeFromNavigator();
}

export function setStoredLocale(locale: SupportedLocale): void {
  if (typeof window === 'undefined') {
    return;
  }

  try {
    window.localStorage.setItem(LOCALE_STORAGE_KEY, locale);
  } catch {
    // ignore quota / private mode
  }
}

export function getStoredLocale(): SupportedLocale | null {
  return readStoredLocale();
}
