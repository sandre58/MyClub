import { describe, expect, it, vi } from 'vitest';
import { mountAfterI18nReady } from './appStartup';

describe('mountAfterI18nReady', () => {
  it('renders after i18nReady resolves', async () => {
    const render = vi.fn();
    await mountAfterI18nReady({
      i18nReady: Promise.resolve(),
      changeLanguage: vi.fn(),
      fallbackLocale: 'fr',
      render,
      logError: vi.fn(),
    });
    expect(render).toHaveBeenCalledOnce();
  });

  it('falls back and still renders when i18nReady rejects', async () => {
    const render = vi.fn();
    const changeLanguage = vi.fn().mockResolvedValue(undefined);
    const logError = vi.fn();
    const initError = new Error('chunk failed');

    await mountAfterI18nReady({
      i18nReady: Promise.reject(initError),
      changeLanguage,
      fallbackLocale: 'fr',
      render,
      logError,
    });

    expect(logError).toHaveBeenCalledWith(
      '[i18n] Initial locale failed to load; mounting with fallback.',
      initError,
    );
    expect(changeLanguage).toHaveBeenCalledWith('fr');
    expect(render).toHaveBeenCalledOnce();
  });

  it('still renders when fallback changeLanguage also fails', async () => {
    const render = vi.fn();
    const fallbackError = new Error('fallback failed');
    const changeLanguage = vi.fn().mockRejectedValue(fallbackError);
    const logError = vi.fn();

    await mountAfterI18nReady({
      i18nReady: Promise.reject(new Error('init failed')),
      changeLanguage,
      fallbackLocale: 'fr',
      render,
      logError,
    });

    expect(logError).toHaveBeenCalledWith(
      '[i18n] Fallback locale failed to load.',
      fallbackError,
    );
    expect(render).toHaveBeenCalledOnce();
  });
});
