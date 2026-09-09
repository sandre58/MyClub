import i18n from './index';

/** Wire action / progression code → organizer label (`actions` namespace). */
export function actionLabel(
  code: string,
  params?: Record<string, string | number | undefined>,
): string {
  return i18n.t(code, {
    ns: 'actions',
    defaultValue: i18n.t('fallback', { ns: 'actions', code }),
    ...params,
  });
}
