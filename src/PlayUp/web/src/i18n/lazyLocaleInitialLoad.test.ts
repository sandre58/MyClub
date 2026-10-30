import { describe, expect, it } from 'vitest';
import i18n, { i18nReady, wasInactiveLocaleLoadedAtInitialBoot } from './index';
import { I18N_NAMESPACES } from './config';

describe('i18n lazy locale loading', () => {
  it('loads only the active locale on initial boot', async () => {
    await i18nReady;
    expect(i18n.language).toBe('fr');
    expect(wasInactiveLocaleLoadedAtInitialBoot()).toBe(false);
    for (const ns of I18N_NAMESPACES) {
      expect(i18n.hasResourceBundle('fr', ns)).toBe(true);
    }
  });
});
