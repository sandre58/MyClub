import type { SVGProps } from 'react';
import type { SupportedLocale } from '../i18n/config';

type FlagProps = SVGProps<SVGSVGElement>;

/** Decorative FR flag — 3:2, for Select leading only. */
function FranceFlagIcon(props: FlagProps) {
  return (
    <svg viewBox="0 0 3 2" focusable="false" {...props}>
      <rect width="1" height="2" fill="#002654" />
      <rect x="1" width="1" height="2" fill="#fff" />
      <rect x="2" width="1" height="2" fill="#ce1126" />
    </svg>
  );
}

/** Decorative GB flag — aligned with en-GB product locale. */
function GreatBritainFlagIcon(props: FlagProps) {
  return (
    <svg viewBox="0 0 60 40" focusable="false" {...props}>
      <rect width="60" height="40" fill="#012169" />
      <path d="M0 0 L60 40 M60 0 L0 40" stroke="#fff" strokeWidth="8" />
      <path d="M0 0 L60 40 M60 0 L0 40" stroke="#C8102E" strokeWidth="5" />
      <path d="M30 0 V40 M0 20 H60" stroke="#fff" strokeWidth="14" />
      <path d="M30 0 V40 M0 20 H60" stroke="#C8102E" strokeWidth="8" />
    </svg>
  );
}

/** Locale leading icon for preferences Select. */
export function LocaleFlag({ locale }: { locale: SupportedLocale }) {
  if (locale === 'en') {
    return <GreatBritainFlagIcon />;
  }
  return <FranceFlagIcon />;
}
