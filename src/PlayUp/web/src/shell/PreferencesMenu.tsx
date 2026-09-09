import { useEffect, useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Field } from '../design-system/components/Field';
import { Select } from '../design-system/components/Select';
import { useDismissLayer } from '../design-system/useDismissLayer';
import { SettingsNavIcon } from '../design-system/icons/shellIcons';
import {
  isSupportedLocale,
  type SupportedLocale,
} from '../i18n/config';
import { setStoredLocale } from '../i18n/resolveLocale';
import { localeFlag } from './localeFlags';
import './preferences-menu.css';

type PreferencesMenuProps = {
  /** Placement variant for Accueil vs shell chrome. */
  className?: string;
};

/**
 * Global preferences entry (language now; theme later).
 * Immediate apply — no save button.
 */
export function PreferencesMenu({ className = '' }: PreferencesMenuProps) {
  const { t, i18n } = useTranslation('shell');
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const panelId = useId();
  const languageFieldId = useId();

  const locale: SupportedLocale = isSupportedLocale(i18n.language)
    ? i18n.language
    : 'fr';

  useDismissLayer(open, () => {
    setOpen(false);
  });

  useEffect(() => {
    if (!open) {
      return;
    }

    function onPointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    document.addEventListener('mousedown', onPointerDown);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
    };
  }, [open]);

  async function onLanguageChange(value: string | null) {
    if (!value || !isSupportedLocale(value)) {
      return;
    }
    setStoredLocale(value);
    await i18n.changeLanguage(value);
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

      {open ? (
        <div
          id={panelId}
          className="shell-preferences__panel"
          role="dialog"
          aria-label={t('preferences.title')}
        >
          <p className="shell-preferences__title">{t('preferences.title')}</p>

          <Field label={t('preferences.language')} htmlFor={languageFieldId}>
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
        </div>
      ) : null}
    </div>
  );
}
