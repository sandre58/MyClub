import { useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Field } from '../design-system/components/Field';
import { Popover } from '../design-system/components/Popover';
import { Select } from '../design-system/components/Select';
import { SettingsNavIcon } from '../design-system/icons/shellIcons';
import {
  isSupportedLocale,
  type SupportedLocale,
} from '../i18n/config';
import { setStoredLocale } from '../i18n/resolveLocale';
import type { ThemePreference } from '../theme/config';
import {
  getThemePreference,
  setThemePreference,
} from '../theme/setThemePreference';
import { localeFlag } from './localeFlags';
import { ThemePreferenceListbox } from './ThemePreferenceListbox';
import './preferences-menu.css';

type PreferencesMenuProps = {
  /** Placement variant for Home vs shell chrome. */
  className?: string;
};

/**
 * Global preferences entry — language and theme.
 * Immediate apply — no save button.
 */
export function PreferencesMenu({ className = '' }: PreferencesMenuProps) {
  const { t, i18n } = useTranslation('shell');
  const [open, setOpen] = useState(false);
  const [themePreference, setThemePreferenceState] =
    useState<ThemePreference>(getThemePreference);
  const rootRef = useRef<HTMLDivElement>(null);
  const panelId = useId();
  const languageFieldId = useId();

  const locale: SupportedLocale = isSupportedLocale(i18n.language)
    ? i18n.language
    : 'fr';

  async function onLanguageChange(value: string | null) {
    if (!value || !isSupportedLocale(value)) {
      return;
    }
    setStoredLocale(value);
    await i18n.changeLanguage(value);
  }

  function onThemeChange(preference: ThemePreference) {
    setThemePreferenceState(preference);
    setThemePreference(preference);
  }

  const rootClass = ['shell-preferences', className].filter(Boolean).join(' ');

  return (
    <div className={rootClass} ref={rootRef}>
      <button
        type="button"
        className="ds-btn ds-btn--ghost ds-icon-button shell-preferences__trigger"
        aria-expanded={open}
        aria-controls={panelId}
        aria-haspopup="dialog"
        aria-label={t('preferences.open')}
        onClick={() => setOpen((current) => !current)}
      >
        <SettingsNavIcon size="sm" aria-hidden="true" />
      </button>

      <Popover
        open={open}
        onOpenChange={setOpen}
        anchorRef={rootRef}
        id={panelId}
        className="shell-preferences__panel"
        aria-label={t('preferences.title')}
        align="end"
        width={22.5 * 16}
      >
        <p className="shell-preferences__title">{t('preferences.title')}</p>

        <Field width="md" label={t('preferences.language')} htmlFor={languageFieldId}>
          <Select
            id={languageFieldId}
            aria-label={t('preferences.language')}
            value={locale}
            allowClear={false}
            onChange={onLanguageChange}
            options={[
              {
                value: 'fr',
                label: t('preferences.locale.fr'),
                leading: localeFlag('fr'),
              },
              {
                value: 'en',
                label: t('preferences.locale.en'),
                leading: localeFlag('en'),
              },
            ]}
          />
        </Field>

        <Field
          label={t('preferences.theme.label')}
          className="shell-preferences__theme-field"
        >
          <ThemePreferenceListbox
            value={themePreference}
            onChange={onThemeChange}
            aria-label={t('preferences.theme.label')}
          />
        </Field>
      </Popover>
    </div>
  );
}
