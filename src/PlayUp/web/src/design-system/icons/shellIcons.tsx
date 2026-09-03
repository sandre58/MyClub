import type { SVGProps } from 'react'
import { Icon, type IconSize } from './Icon'

type ShellIconProps = SVGProps<SVGSVGElement> & { size?: IconSize }

/** Overview — gauge (Shell A). */
export function OverviewNavIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="m12 14 4-4" />
      <path d="M3.34 19a10 10 0 1 1 17.32 0" />
    </Icon>
  )
}

/** Structure — org tree. */
export function OrganisationNavIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <rect x="9" y="2" width="6" height="6" rx="1" />
      <rect x="2" y="16" width="6" height="6" rx="1" />
      <rect x="16" y="16" width="6" height="6" rx="1" />
      <path d="M12 8v4" />
      <path d="M5 16v-3a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v3" />
    </Icon>
  )
}

/** Équipes — shield. */
export function TeamsNavIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M20 13c0 5-3.5 7.5-8 9-4.5-1.5-8-4-8-9V6l8-3 8 3Z" />
    </Icon>
  )
}

/** Stades — venue / stand. */
export function VenuesNavIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M4 20V10l8-6 8 6v10" />
      <path d="M9 20v-6h6v6" />
    </Icon>
  )
}

/** Règlement — document. */
export function RegulationNavIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z" />
      <path d="M14 2v4a2 2 0 0 0 2 2h4" />
      <path d="M10 13H8" />
      <path d="M16 13H8" />
      <path d="M16 17H8" />
    </Icon>
  )
}

/** Matchs — calendrier / hub opérationnel. */
export function MatchesNavIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M8 2v4" />
      <path d="M16 2v4" />
      <rect x="3" y="4" width="18" height="18" rx="2" />
      <path d="M3 10h18" />
      <path d="M8 14h.01" />
      <path d="M12 14h.01" />
      <path d="M16 14h.01" />
      <path d="M8 18h.01" />
      <path d="M12 18h.01" />
    </Icon>
  )
}

/** Classements — podium. */
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
  )
}

/** Paramètres — gear (outline). */
export function SettingsNavIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
      <circle cx="12" cy="12" r="3" />
    </Icon>
  )
}

/** Rail collapse — chevron toward sidebar. */
export function SidebarCollapseIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="m15 18-6-6 6-6" />
    </Icon>
  )
}

/** Rail expand — chevron away from sidebar. */
export function SidebarExpandIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="m9 18 6-6-6-6" />
    </Icon>
  )
}

/** Attention trigger — bell (header chrome). */
export function AttentionBellIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
      <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
    </Icon>
  )
}

/** Attention alert — triangle (drawer / situation rows). */
export function AttentionIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3" />
      <path d="M12 9v4" />
      <path d="M12 17h.01" />
    </Icon>
  )
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
  )
}

/** Overlay close — X (drawer chrome). */
export function CloseIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M18 6 6 18" />
      <path d="m6 6 12 12" />
    </Icon>
  )
}

/** Navigate / open — chevron right. */
export function ChevronRightIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="m9 18 6-6-6-6" />
    </Icon>
  )
}

/** Split / menu — chevron down. */
export function ChevronDownIcon({ size, ...props }: ShellIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="m6 9 6 6 6-6" />
    </Icon>
  )
}

/** Situation mark — alert triangle (attention drawer rows). */
export function AttentionMarkIcon({ size, ...props }: ShellIconProps) {
  return AttentionIcon({ size, ...props })
}
