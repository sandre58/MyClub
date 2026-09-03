import type { SVGProps } from 'react'
import { Icon, type IconSize } from './Icon'
import {
  AttentionIcon,
  MatchesNavIcon,
  RegulationNavIcon,
  TeamsNavIcon,
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

/** Ajout — plus (CTA Créer). */
export function PlusIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M5 12h14" />
      <path d="M12 5v14" />
    </Icon>
  )
}

/** Identité — crayon. */
export function PencilIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z" />
      <path d="m15 5 4 4" />
    </Icon>
  )
}

/** Suppression — corbeille (préparation). */
export function TrashIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M3 6h18" />
      <path d="M8 6V4h8v2" />
      <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
      <path d="M10 11v6" />
      <path d="M14 11v6" />
    </Icon>
  )
}

/** Retrait d’une équipe en saison — bouclier moins (pas une personne). */
export function WithdrawIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M20 13c0 5-3.5 7.5-8 10-4.5-2.5-8-5-8-10V6l8-4 8 4Z" />
      <path d="M9 12h6" />
    </Icon>
  )
}

/** Empty state — cadre en pointillés (rien de sélectionné). */
export function EmptySelectionIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} strokeDasharray="3 3" {...props}>
      <rect x="4" y="4" width="16" height="16" rx="2" />
    </Icon>
  )
}

/** Sélection multiple — deux calques. */
export function LayersIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="m12.83 2.18a2 2 0 0 0-1.66 0L2.6 6.08a1 1 0 0 0 0 1.83l8.58 3.91a2 2 0 0 0 1.66 0l8.58-3.91a1 1 0 0 0 0-1.83Z" />
      <path d="m22 12.67-9.17 4.16a2 2 0 0 1-1.66 0L2 12.67" />
      <path d="m22 17.67-9.17 4.16a2 2 0 0 1-1.66 0L2 17.67" />
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

/**
 * Calendrier sportif — glyphe Matches nav.
 * Used by Préparation / GeneratedCalendar overview panel.
 */
export function CalendarIcon({ size, ...props }: OverviewIconProps) {
  return <MatchesNavIcon size={size} {...props} />
}

/** Équipes — participants (même glyphe que la nav Équipes). */
export function TeamsIcon({ size, ...props }: OverviewIconProps) {
  return <TeamsNavIcon size={size} {...props} />
}

/** Règlement — document. */
export function RegulationIcon({ size, ...props }: OverviewIconProps) {
  return <RegulationNavIcon size={size} {...props} />
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

/** Personne — tête / épaules (placeholder effectif). */
export function PersonIcon({ size, ...props }: OverviewIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
      <circle cx="12" cy="7" r="4" />
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
