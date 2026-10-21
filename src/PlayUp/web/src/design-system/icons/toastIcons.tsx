import { Check, CircleX, Info } from 'lucide-react';
import type { SVGProps } from 'react';
import { LucideIcon, type IconSize } from './Icon';
import { AttentionIcon } from './shellIcons';
import type { ToastTone } from '../toastStore';

type ToastIconProps = SVGProps<SVGSVGElement> & { size?: IconSize };

function ToastSuccessIcon({ size, ...props }: ToastIconProps) {
  return <LucideIcon icon={Check} size={size} {...props} />;
}

function ToastErrorIcon({ size, ...props }: ToastIconProps) {
  return <LucideIcon icon={CircleX} size={size} {...props} />;
}

function ToastInfoIcon({ size, ...props }: ToastIconProps) {
  return <LucideIcon icon={Info} size={size} {...props} />;
}

/** Tone glyph for toast — decorative; message + role carry meaning. */
export function ToastToneIcon({
  tone,
  size = 'md',
  ...props
}: ToastIconProps & { tone: ToastTone }) {
  switch (tone) {
    case 'success':
      return <ToastSuccessIcon size={size} {...props} />;
    case 'error':
      return <ToastErrorIcon size={size} {...props} />;
    case 'attention':
      return <AttentionIcon size={size} {...props} />;
    case 'info':
      return <ToastInfoIcon size={size} {...props} />;
  }
}
