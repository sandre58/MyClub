import i18n from './index';
import { ApiError } from '../api';

/**
 * Maps ProblemDetails `code` (wire) → organizer copy in the `errors` namespace.
 * Falls back to undefined so callers can use English diagnostic detail.
 */
export function apiErrorLabel(error: ApiError): string | undefined {
  if (!error.code) {
    return undefined;
  }

  return apiErrorCodeLabel(error.code);
}

export function apiErrorCodeLabel(code: string): string | undefined {
  const label = i18n.t(`errors:${code}`, { defaultValue: '' });
  if (label.length > 0) {
    return label;
  }
  if (code.includes('.')) {
    return undefined;
  }
  const stageScoped = i18n.t(`errors:Stage.${code}`, {
    defaultValue: '',
  });
  return stageScoped.length > 0 ? stageScoped : undefined;
}

const TRANSPORT_ERROR_NAMES = new Set(['AbortError', 'TimeoutError']);

/**
 * Timeout, browser offline, and HTTP 5xx share one organizer message.
 * Business 4xx keep their wire code. fetch() rejects with TypeError
 * (“Failed to fetch” / “Load failed” / “NetworkError…”).
 */
export function isGenericNetworkFailure(error: unknown): boolean {
  if (error instanceof ApiError) {
    return error.status >= 500 || error.status === 408;
  }

  const name = readErrorName(error);
  if (name !== undefined && TRANSPORT_ERROR_NAMES.has(name)) {
    return true;
  }

  return isOfflineFetchError(error);
}

/** Shared organizer copy for {@link isGenericNetworkFailure}. */
export function genericNetworkLabel(): string {
  return i18n.t('common:networkError');
}

function readErrorName(error: unknown): string | undefined {
  if (typeof error !== 'object' || error === null || !('name' in error)) {
    return undefined;
  }
  const name = (error as { name: unknown }).name;
  return typeof name === 'string' ? name : undefined;
}

function isOfflineFetchError(error: unknown): boolean {
  if (!(error instanceof TypeError)) {
    return false;
  }
  const message = error.message.toLowerCase();
  return (
    message.includes('failed to fetch') ||
    message.includes('networkerror') ||
    message.includes('load failed') ||
    message.includes('network request failed')
  );
}
