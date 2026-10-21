import { Clock3, MapPin } from 'lucide-react';
import type { SVGProps } from 'react';
import { LucideIcon, type IconSize } from './Icon';

type MetaIconProps = SVGProps<SVGSVGElement> & { size?: IconSize };

/** Clock — time meta (Match Hero, calendar). */
export function ClockIcon({ size, ...props }: MetaIconProps) {
  return <LucideIcon icon={Clock3} size={size} {...props} />;
}

/** Place — stadium / pitch. */
export function PinIcon({ size, ...props }: MetaIconProps) {
  return <LucideIcon icon={MapPin} size={size} {...props} />;
}
