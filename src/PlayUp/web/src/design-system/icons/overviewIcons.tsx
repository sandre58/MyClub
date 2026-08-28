import type { SVGProps } from 'react'
import { Icon, type IconSize } from './Icon'
import {
  AttentionIcon,
  MatchesNavIcon,
  OrganisationNavIcon,
} from './shellIcons'

type OverviewIconProps = SVGProps<SVGSVGElement> & { size?: IconSize }

/**
 * Vue d'ensemble content icons — same rules as shell chrome:
 * stroke monocolor, currentColor, 24×24 Lucide-compatible, sizes via Icon.
 * SoT: Identité §13 · foundations/icons.css · Icon.tsx
 */

/** Prochaine action — flag (en-tête générique). */
export function NextActionIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z" />
      <path d="M4 22v-7" />
    </Icon>
  )
}

/** Créer les matchs — calendrier + ajout. */
export function CreateMatchesIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M8 2v4" />
      <path d="M16 2v4" />
      <rect x="3" y="4" width="18" height="18" rx="2" />
      <path d="M3 10h18" />
      <path d="M12 14v4" />
      <path d="M10 16h4" />
    </Icon>
  )
}

/** Préparation (presse-papier) — playground / surfaces futures. */
export function PreparationIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <rect x="8" y="2" width="8" height="4" rx="1" />
      <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" />
      <path d="M9 12h6" />
      <path d="M9 16h4" />
    </Icon>
  )
}

/** En cours (play). */
export function InProgressIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <circle cx="12" cy="12" r="10" />
      <path d="m10 8 6 4-6 4z" />
    </Icon>
  )
}

/**
 * Calendrier — glyphe Matches nav.
 * Conservé pour le futur signal cycle Calendrier (OPEN) — pas d'usage Cockpit V1.
 */
export function CalendarIcon({ size, ...props }: OverviewIconProps) {
  return <MatchesNavIcon size={size} {...props} />
}

/** Équipes — participants (même glyphe que la nav Organisation). */
export function TeamsIcon({ size, ...props }: OverviewIconProps) {
  return <OrganisationNavIcon size={size} {...props} />
}

/** Règlement — document. */
export function RegulationIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7z" />
      <path d="M14 2v5h6" />
      <path d="M9 13h6" />
      <path d="M9 17h4" />
    </Icon>
  )
}

/** Structure — arborescence de phases / groupes. */
export function StructureIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <rect x="9" y="2" width="6" height="5" rx="1" />
      <rect x="2" y="17" width="6" height="5" rx="1" />
      <rect x="16" y="17" width="6" height="5" rx="1" />
      <path d="M12 7v4" />
      <path d="M5 17v-2a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v2" />
    </Icon>
  )
}

/** À traiter — triangle d'alerte (identique au shell). */
export function OverviewAttentionIcon({ size, ...props }: OverviewIconProps) {
  return <AttentionIcon size={size} {...props} />
}

/** Confirmation d'état — check (pastilles de statut). */
export function CheckIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M20 6 9 17l-5-5" />
    </Icon>
  )
}

/** Étape non encore atteinte — cercle en pointillés. */
export function PendingCircleIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} strokeDasharray="3 3" {...props}>
      <circle cx="12" cy="12" r="9" />
    </Icon>
  )
}

/** Progression neutre — flèche droite (fallback action). */
export function ArrowRightIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M5 12h14" />
      <path d="m12 5 7 7-7 7" />
    </Icon>
  )
}
