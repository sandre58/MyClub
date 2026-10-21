import type { TFunction } from 'i18next';

import { ApiError } from '../api';

export function attentionLoadErrorMessage(
  t: TFunction<'shell'>,
  error: unknown,
): string {
  if (error instanceof ApiError) {
    if (error.status === 401 || error.status === 403) {
      return t('attention.loadErrorAuth');
    }
    if (error.status >= 500) {
      return t('attention.loadErrorServer');
    }
  }
  return t('attention.loadError');
}
