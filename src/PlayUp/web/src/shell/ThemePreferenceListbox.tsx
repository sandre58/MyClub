import { useTranslation } from 'react-i18next';
import { ToggleButtonGroup } from '../design-system/components/ToggleButtonGroup';
import {
  ThemeDarkIcon,
  ThemeLightIcon,
  ThemeSystemIcon,
} from '../design-system/icons/shellIcons';
import type { ThemePreference } from '../theme';

type ThemePreferenceListboxProps = {
  value: ThemePreference;
  onChange: (preference: ThemePreference) => void;
  'aria-label'?: string;
};

/**
 * Theme preference control — binds to preference, not resolved theme.
 */
export function ThemePreferenceListbox({
  value,
  onChange,
  'aria-label': ariaLabel,
}: ThemePreferenceListboxProps) {
  const { t } = useTranslation('shell');

  return (
    <ToggleButtonGroup
      value={value}
      onChange={onChange}
      aria-label={ariaLabel}
      options={[
        {
          value: 'system',
          label: t('preferences.theme.system'),
          leading: <ThemeSystemIcon size="sm" aria-hidden="true" />,
        },
        {
          value: 'light',
          label: t('preferences.theme.light'),
          leading: <ThemeLightIcon size="sm" aria-hidden="true" />,
        },
        {
          value: 'dark',
          label: t('preferences.theme.dark'),
          leading: <ThemeDarkIcon size="sm" aria-hidden="true" />,
        },
      ]}
    />
  );
}
