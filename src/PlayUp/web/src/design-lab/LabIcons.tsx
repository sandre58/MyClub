import type { SVGProps } from 'react'
import { Icon, type IconSize } from '../design-system/icons/Icon'

type LabIconProps = SVGProps<SVGSVGElement> & { size?: IconSize }

/** Horloge — méta horaire (Match Hero, calendrier). */
export function ClockIcon({ size, ...props }: LabIconProps) {
  return (
    <Icon size={size} {...props}>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </Icon>
  )
}

/** Lieu — stade / terrain. */
export function PinIcon({ size, ...props }: LabIconProps) {
  return (
    <Icon size={size} {...props}>
      <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z" />
      <circle cx="12" cy="10" r="3" />
    </Icon>
  )
}
