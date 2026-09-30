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
  Dices,
  Equal,
  Flag,
  GitBranch,
  Goal,
  GripVertical,
  LayoutGrid,
  Layers,
  ListChecks,
  ListMinus,
  ListOrdered,
  ListPlus,
  Minus,
  Medal,
  Network,
  Pencil,
  Pipette,
  PlayingCardsFan,
  Plus,
  Podium,
  Search,
  ShieldBan,
  Shuffle,
  Sigma,
  SquareOff,
  Timer,
  Trash2,
  Trophy,
  Unlock,
  UsersRound,
  Volleyball,
  X,
  Handshake,
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
 * Product / domain content icons (Structure, Regulation, Overview, forms…).
 * Same rules as shell chrome: stroke monocolor, currentColor, 24×24, sizes via Icon.
 * See foundations/icons.css · Icon.tsx
 */

/** Next action — flag (generic header). */
export function NextActionIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Flag} size={size} {...props} />;
}

/** Add — plus (Create CTA). */
export function PlusIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Plus} size={size} {...props} />;
}

/** Stepper — minus. */
export function MinusIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Minus} size={size} {...props} />;
}

/** Drag handle — grip vertical. */
export function GripIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={GripVertical} size={size} {...props} />;
}

/** Win / trophy. */
export function TrophyIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Trophy} size={size} {...props} />;
}

/** Placement awards. */
export function AttributionIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Medal} size={size} {...props} />;
}

/** Groups — group grid. */
export function GroupsFormatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={LayoutGrid} size={size} {...props} />;
}

/** Championship — ordered ranking. */
export function ChampionshipFormatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ListOrdered} size={size} {...props} />;
}

/** Cup — tree / bracket. */
export function CupFormatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={GitBranch} size={size} {...props} />;
}

/** Cup rounds — milestones (distinct from Cup format). */
export function RoundsStatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Flag} size={size} {...props} />;
}

/** Matchdays — calendar. */
export function MatchdayStatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={CalendarDays} size={size} {...props} />;
}

/** Attached matches. */
export function MatchesStatIcon({ size, ...props }: ContentIconProps) {
  return <MatchesNavIcon size={size} {...props} />;
}

/** Match rules (framework / phase) — aligned with Regulation hub. */
export function MatchRulesIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Volleyball} size={size} {...props} />;
}

/** Standing — podium (aligned with Regulation hub). */
export function StandingRulesIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Podium} size={size} {...props} />;
}

/** Confrontations */
export function ConfrontationIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Handshake} size={size} {...props} />;
}

/** Two legs / legs. */
export function LegsStatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ArrowLeftRight} size={size} {...props} />;
}

/** Compact structural anomaly (≠ draw / ops). */
export function StructureIssueIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={CircleAlert} size={size} {...props} />;
}

/** Draw pending — shuffle / pots (≠ anomaly). */
export function DrawPendingIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Shuffle} size={size} {...props} />;
}

/** Draw — configuration (aligned with Regulation Shuffle). */
export function DrawConfigIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Shuffle} size={size} {...props} />;
}

/** Random draw. */
export function RandomIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Dices} size={size} {...props} />;
}

/** Swiss — pairing network. */
export function SwissFormatIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Network} size={size} {...props} />;
}

/** Draw — equality. */
export function EqualIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Equal} size={size} {...props} />;
}

/** Loss — cross. */
export function CrossIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={X} size={size} {...props} />;
}

/** Identity — pencil. */
export function PencilIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Pencil} size={size} {...props} />;
}

/** Delete — trash (preparation). */
export function TrashIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Trash2} size={size} {...props} />;
}

/** Release placements / unlock a grid — not a delete. */
export function UnlockIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Unlock} size={size} {...props} />;
}

/** Withdraw a team mid-season — shield minus (not a person). */
export function WithdrawIcon({ size, ...props }: ContentIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M20 13c0 5-3.5 7.5-8 10-4.5-2.5-8-5-8-10V6l8-4 8 4Z" />
      <path d="M9 12h6" />
    </Icon>
  );
}

/** Empty state — dashed frame (nothing selected). */
export function EmptySelectionIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={SquareOff} size={size} {...props} />;
}

/** Multi-select — two layers. */
export function LayersIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Layers} size={size} {...props} />;
}

/** Create matches — calendar + add. */
export function CreateMatchesIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={CalendarPlus} size={size} {...props} />;
}

/** Preparation (clipboard) — playground / future surfaces. */
export function PreparationIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Clipboard} size={size} {...props} />;
}

/** Copy to clipboard. */
export function CopyIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Copy} size={size} {...props} />;
}

/** Eyedropper — sample a color on screen (EyeDropper). */
export function PipetteIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Pipette} size={size} {...props} />;
}

/**
 * Sport calendar — Matches nav glyph.
 * Used by Preparation / GeneratedCalendar overview panel.
 */
export function CalendarIcon({ size, ...props }: ContentIconProps) {
  return <MatchesNavIcon size={size} {...props} />;
}

/** Teams — participants (same glyph as Teams nav). */
export function TeamsIcon({ size, ...props }: ContentIconProps) {
  return <TeamsNavIcon size={size} {...props} />;
}

/** Regulation — document. */
export function RegulationIcon({ size, ...props }: ContentIconProps) {
  return <RegulationNavIcon size={size} {...props} />;
}

/** Structure — stage / group tree. */
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

/** Attention — alert triangle (same as shell). */
export function OverviewAttentionIcon({ size, ...props }: ContentIconProps) {
  return <AttentionIcon size={size} {...props} />;
}

/** State confirmation — check (status pills). */
export function CheckIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Check} size={size} {...props} />;
}

/** Step not yet reached — dashed circle. */
export function PendingCircleIcon({ size, ...props }: ContentIconProps) {
  return (
    <Icon size={size} strokeDasharray="3 3" {...props}>
      <circle cx="12" cy="12" r="9" />
    </Icon>
  );
}

/** Person — head / shoulders (roster placeholder). */
export function PersonIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={UsersRound} size={size} {...props} />;
}

/** Neutral progression — right arrow (fallback action). */
export function ArrowRightIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ArrowRight} size={size} {...props} />;
}

/** Vertical connector — down arrow (ordered topology). */
export function ArrowDownIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ArrowDown} size={size} {...props} />;
}

/** Aggregate score — sigma. */
export function AggregateIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Sigma} size={size} {...props} />;
}

/** Match clock / period duration. */
export function TimerIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Timer} size={size} {...props} />;
}

/** Penalties / goal — shootout token. */
export function PenaltiesIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Goal} size={size} {...props} />;
}

/** Disciplinary cards family. */
export function DisciplineIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={PlayingCardsFan} size={size} {...props} />;
}

/** Administrative forfeit result. */
export function ForfeitIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ShieldBan} size={size} {...props} />;
}

/** Add row / append list item. */
export function ListPlusIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ListPlus} size={size} {...props} />;
}

/** Remove row / shrink list. */
export function ListMinusIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ListMinus} size={size} {...props} />;
}

/** Confirm multi-select / checklist. */
export function ListChecksIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={ListChecks} size={size} {...props} />;
}

/** Search field chrome. */
export function SearchIcon({ size, ...props }: ContentIconProps) {
  return <LucideIcon icon={Search} size={size} {...props} />;
}
