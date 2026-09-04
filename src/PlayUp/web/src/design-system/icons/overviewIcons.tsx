import {
    ArrowRight,
    CalendarPlus,
    Check,
    Clipboard,
    Copy,
    Flag,
    Layers,
    Pencil,
    Pipette,
    Plus,
    Trash2,
    UsersRound,
} from 'lucide-react'
import type { SVGProps } from 'react'
import { Icon, LucideIcon, type IconSize } from './Icon'
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
  return <LucideIcon icon={Flag} size={size} {...props} />
}

/** Ajout — plus (CTA Créer). */
export function PlusIcon({ size, ...props }: OverviewIconProps) {
  return <LucideIcon icon={Plus} size={size} {...props} />
}

/** Identité — crayon. */
export function PencilIcon({ size, ...props }: OverviewIconProps) {
  return <LucideIcon icon={Pencil} size={size} {...props} />
}

/** Suppression — corbeille (préparation). */
export function TrashIcon({ size, ...props }: OverviewIconProps) {
  return <LucideIcon icon={Trash2} size={size} {...props} />
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
  return <LucideIcon icon={Layers} size={size} {...props} />
}

/** Créer les matchs — calendrier + ajout. */
export function CreateMatchesIcon({ size, ...props }: OverviewIconProps) {
  return <LucideIcon icon={CalendarPlus} size={size} {...props} />
}

/** Préparation (presse-papier) — playground / surfaces futures. */
export function PreparationIcon({ size, ...props }: OverviewIconProps) {
  return <LucideIcon icon={Clipboard} size={size} {...props} />
}

/** Copier dans le presse-papiers. */
export function CopyIcon({ size, ...props }: OverviewIconProps) {
  return <LucideIcon icon={Copy} size={size} {...props} />
}

/** Pipette — échantillonner une couleur à l’écran (EyeDropper). */
export function PipetteIcon({ size, ...props }: OverviewIconProps) {
  return <LucideIcon icon={Pipette} size={size} {...props} />
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
  return <LucideIcon icon={Check} size={size} {...props} />
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
  return <LucideIcon icon={UsersRound} size={size} {...props} />
}

/** Progression neutre — flèche droite (fallback action). */
export function ArrowRightIcon({ size, ...props }: OverviewIconProps) {
  return <LucideIcon icon={ArrowRight} size={size} {...props} />
}
