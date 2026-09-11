import {
  ArrowRight,
  ArrowDown,
  ArrowLeftRight,
  CalendarDays,
  CalendarPlus,
  Check,
  CircleAlert,
  Clipboard,
  Copy,
  Equal,
  Flag,
  GitBranch,
  GripVertical,
  LayoutGrid,
  Layers,
  ListOrdered,
  Minus,
  Network,
  Pencil,
  Pipette,
  Plus,
  Shuffle,
  Trash2,
  Trophy,
  UsersRound,
  X,
} from 'lucide-react';
import type { SVGProps } from 'react';
import { Icon, LucideIcon, type IconSize } from './Icon';
import {
  AttentionIcon,
  MatchesNavIcon,
  RegulationNavIcon,
  TeamsNavIcon,
} from './shellIcons';

type ContentIconProps = SVGProps<SVGSVGElement> & { size?: IconSize };

/**
 * Product / métier content icons (Structure, Règlement, Overview, forms…).
 * Same rules as shell chrome: stroke monocolor, currentColor, 24×24, sizes via Icon.
 * SoT: Identité §13 · foundations/icons.css · Icon.tsx
 */

/** Prochaine action — flag (en-tête générique). */
export function NextActionIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Flag} size={size} {...props} />;
}

/** Ajout — plus (CTA Créer). */
export function PlusIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Plus} size={size} {...props} />;
}

/** Stepper — moins. */
export function MinusIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Minus} size={size} {...props} />;
}

/** Drag handle — grip vertical. */
export function GripIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={GripVertical} size={size} {...props} />;
}

/** Victoire / trophée. */
export function TrophyIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Trophy} size={size} {...props} />;
}

/** Poules — grille de groupes. */
export function GroupsFormatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={LayoutGrid} size={size} {...props} />;
}

/** Championnat — classement ordonné. */
export function ChampionshipFormatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ListOrdered} size={size} {...props} />;
}

/** Coupe — arbre / bracket. */
export function CupFormatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={GitBranch} size={size} {...props} />;
}

/** Tours de coupe — jalons (distinct du type Coupe). */
export function RoundsStatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Flag} size={size} {...props} />;
}

/** Journées — calendrier. */
export function MatchdayStatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={CalendarDays} size={size} {...props} />;
}

/** Matchs attachés. */
export function MatchesStatIcon({ size, ...props }: ContentIconProps) {
  return <MatchesNavIcon size={size} {...props} />;
}

/** Aller-retour / manches. */
export function LegsStatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ArrowLeftRight} size={size} {...props} />;
}

/** Anomalie structurelle compacte (≠ tirage / ops). */
export function StructureIssueIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={CircleAlert} size={size} {...props} />;
}

/** Tirage à définir — mélange / pots (≠ anomalie). */
export function DrawPendingIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Shuffle} size={size} {...props} />;
}

/** Swiss — réseau de paires. */
export function SwissFormatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Network} size={size} {...props} />;
}

/** Nul — égalité. */
export function EqualIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Equal} size={size} {...props} />;
}

/** Défaite — croix. */
export function CrossIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={X} size={size} {...props} />;
}

/** Identité — crayon. */
export function PencilIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Pencil} size={size} {...props} />;
}

/** Suppression — corbeille (préparation). */
export function TrashIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Trash2} size={size} {...props} />;
}

/** Retrait d’une équipe en saison — bouclier moins (pas une personne). */
export function WithdrawIcon({ size, ...props }: ContentIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M20 13c0 5-3.5 7.5-8 10-4.5-2.5-8-5-8-10V6l8-4 8 4Z" />
      <path d="M9 12h6" />
    </Icon>
  );
}

/** Empty state — cadre en pointillés (rien de sélectionné). */
export function EmptySelectionIcon({ size, ...props }: ContentIconProps) {
  return (
    <Icon size={size} strokeDasharray="3 3" {...props}>
      <rect x="4" y="4" width="16" height="16" rx="2" />
    </Icon>
  );
}

/** Sélection multiple — deux calques. */
export function LayersIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Layers} size={size} {...props} />;
}

/** Créer les matchs — calendrier + ajout. */
export function CreateMatchesIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={CalendarPlus} size={size} {...props} />;
}

/** Préparation (presse-papier) — playground / surfaces futures. */
export function PreparationIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Clipboard} size={size} {...props} />;
}

/** Copier dans le presse-papiers. */
export function CopyIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Copy} size={size} {...props} />;
}

/** Pipette — échantillonner une couleur à l’écran (EyeDropper). */
export function PipetteIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Pipette} size={size} {...props} />;
}

/**
 * Calendrier sportif — glyphe Matches nav.
 * Used by Préparation / GeneratedCalendar overview panel.
 */
export function CalendarIcon({ size, ...props }: ContentIconProps) {
  return <MatchesNavIcon size={size} {...props} />;
}

/** Équipes — participants (même glyphe que la nav Équipes). */
export function TeamsIcon({ size, ...props }: ContentIconProps) {
  return <TeamsNavIcon size={size} {...props} />;
}

/** Règlement — document. */
export function RegulationIcon({ size, ...props }: ContentIconProps) {
  return <RegulationNavIcon size={size} {...props} />;
}

/** Structure — arborescence de phases / groupes. */
export function StructureIcon({ size, ...props }: ContentIconProps) {
  return (
    <Icon size={size} {...props}>
      <rect x="9" y="2" width="6" height="5" rx="1" />
      <rect x="2" y="17" width="6" height="5" rx="1" />
      <rect x="16" y="17" width="6" height="5" rx="1" />
      <path d="M12 7v4" />
      <path d="M5 17v-2a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v2" />
    </Icon>
  );
}

/** À traiter — triangle d'alerte (identique au shell). */
export function OverviewAttentionIcon({ size, ...props }: ContentIconProps) {
  return <AttentionIcon size={size} {...props} />;
}

/** Confirmation d'état — check (pastilles de statut). */
export function CheckIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Check} size={size} {...props} />;
}

/** Étape non encore atteinte — cercle en pointillés. */
export function PendingCircleIcon({ size, ...props }: ContentIconProps) {
  return (
    <Icon size={size} strokeDasharray="3 3" {...props}>
      <circle cx="12" cy="12" r="9" />
    </Icon>
  );
}

/** Personne — tête / épaules (placeholder effectif). */
export function PersonIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={UsersRound} size={size} {...props} />;
}

/** Progression neutre — flèche droite (fallback action). */
export function ArrowRightIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ArrowRight} size={size} {...props} />;
}

/** Connecteur vertical — flèche bas (topologie ordonnée). */
export function ArrowDownIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ArrowDown} size={size} {...props} />;
}
