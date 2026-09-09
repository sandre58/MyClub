import { Clock3, MapPin } from 'lucide-react';
import type { SVGProps } from 'react';
import { LucideIcon, type IconSize } from './Icon';

type MetaIconProps = SVGProps<SVGSVGElement> & { size?: IconSize };

/** Horloge — méta horaire (Match Hero, calendrier). */
export function ClockIcon({ size, ...props }: MetaIconProps) {
  return <LucideIcon icon={Clock3} size={size} {...props} />;
}

/** Lieu — stade / terrain. */
export function PinIcon({ size, ...props }: MetaIconProps) {
  return <LucideIcon icon={MapPin} size={size} {...props} />;
}
