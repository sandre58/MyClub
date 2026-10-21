import i18n from './index';

const MATERIALIZE_ACTION_CODES = new Set([
  'MaterializeMatches',
  'MaterializeFromOccupiedSlots',
]);

/** Wire action / progression code → organizer label (`actions` namespace). */
export function actionLabel(
  code: string,
  params?: Record<string, string | number | undefined>,
): string {
  const key =
    MATERIALIZE_ACTION_CODES.has(code) && params?.labelVariant === 'complete'
      ? `${code}Complete`
      : code;
  return i18n.t(key, {
    ns: 'actions',
    defaultValue: i18n.t('fallback', { ns: 'actions', code }),
    ...params,
  });
}
