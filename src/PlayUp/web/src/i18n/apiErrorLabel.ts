import i18n from './index';
import type { ApiError } from '../api';

/**
 * Maps ProblemDetails `code` (wire) → organizer copy in the `errors` namespace.
 * Falls back to undefined so callers can use English diagnostic detail.
 */
export function apiErrorLabel(error: ApiError): string | undefined {
  if (!error.code) {
    return undefined;
  }

  const label = i18n.t(`errors:${error.code}`, { defaultValue: '' });
  if (label.length > 0) {
    return label;
  }
  if (error.code.includes('.')) {
    return undefined;
  }
  const stageScoped = i18n.t(`errors:Stage.${error.code}`, {
    defaultValue: '',
  });
  return stageScoped.length > 0 ? stageScoped : undefined;
}
