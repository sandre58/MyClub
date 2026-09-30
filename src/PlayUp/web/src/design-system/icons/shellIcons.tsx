import {
  Bell,
  CalendarDays,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ChevronUp,
  FileText,
  Gauge,
  Settings,
  Shield,
  TriangleAlert,
  X,
} from 'lucide-react';
import type { SVGProps } from 'react';
import { Icon, LucideIcon, type IconSize } from './Icon';

type ShellIconProps = SVGProps<SVGSVGElement> & { size?: IconSize };

/** Overview — gauge (Shell A). */
export function OverviewNavIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={Gauge} size={size} {...props} />;
}

/** Structure — org tree. */
export function StructureNavIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <rect x="9" y="2" width="6" height="6" rx="1" />
      <rect x="2" y="16" width="6" height="6" rx="1" />
      <rect x="16" y="16" width="6" height="6" rx="1" />
      <path d="M12 8v4" />
      <path d="M5 16v-3a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v3" />
    </Icon>
  );
}

/** Teams — shield. */
export function TeamsNavIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={Shield} size={size} {...props} />;
}

/** Regulation — document. */
export function RegulationNavIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={FileText} size={size} {...props} />;
}

/** Matches — calendar / operational hub. */
export function MatchesNavIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={CalendarDays} size={size} {...props} />;
}

/** Standings — podium. */
export function ClassementsNavIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6" />
      <path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18" />
      <path d="M4 22h16" />
      <path d="M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22" />
      <path d="M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22" />
      <path d="M18 2H6v7a6 6 0 0 0 12 0V2Z" />
    </Icon>
  );
}

/** Settings — gear (outline). */
export function SettingsNavIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={Settings} size={size} {...props} />;
}

/** Rail collapse — chevron toward sidebar. */
export function SidebarCollapseIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={ChevronLeft} size={size} {...props} />;
}

/** Rail expand — chevron away from sidebar. */
export function SidebarExpandIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={ChevronRight} size={size} {...props} />;
}

/** Attention trigger — bell (header chrome). */
export function AttentionBellIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={Bell} size={size} {...props} />;
}

/** Attention alert — triangle (drawer / situation rows). */
export function AttentionIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={TriangleAlert} size={size} {...props} />;
}

/** Competition swap — arrows left/right (header chrome). */
export function SwapIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="m16 3 4 4-4 4" />
      <path d="M20 7H4" />
      <path d="m8 21-4-4 4-4" />
      <path d="M4 17h16" />
    </Icon>
  );
}

/** Overlay close — X (drawer chrome). */
export function CloseIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={X} size={size} {...props} />;
}

/** Back / previous step — chevron left. */
export function ChevronLeftIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={ChevronLeft} size={size} {...props} />;
}

/** Navigate / open — chevron right. */
export function ChevronRightIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={ChevronRight} size={size} {...props} />;
}

/** Split / menu — chevron down. */
export function ChevronDownIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={ChevronDown} size={size} {...props} />;
}

/** Stepper / expand — chevron up. */
export function ChevronUpIcon({ size, ...props }: ShellIconProps) {
  return <LucideIcon icon={ChevronUp} size={size} {...props} />;
}

/** Situation mark — alert triangle (attention drawer rows). */
export function AttentionMarkIcon({ size, ...props }: ShellIconProps) {
  return AttentionIcon({ size, ...props });
}
