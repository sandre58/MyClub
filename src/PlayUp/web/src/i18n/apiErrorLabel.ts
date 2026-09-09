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
  return label.length > 0 ? label : undefined;
}
