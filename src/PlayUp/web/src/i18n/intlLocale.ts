import { DEFAULT_LOCALE } from './config';

/**
 * Maps product language codes to BCP 47 tags for Intl formatting.
 * fr → fr-FR, en → en-GB (European date order).
 */
export function toIntlLocale(language: string): string {
  const base = language.split('-')[0]?.toLowerCase() ?? DEFAULT_LOCALE;
  if (base === 'en') {
    return 'en-GB';
  }
  if (base === 'fr') {
    return 'fr-FR';
  }
  return 'fr-FR';
}
