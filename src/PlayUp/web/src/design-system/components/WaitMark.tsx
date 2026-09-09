import type { ReactNode } from 'react';
import { PlayUpLockupMark } from '../PlayUpLockupMark';

export type WaitSize = 'home' | 'page' | 'region';

/**
 * Page / region wait — mark Accueil + orbiting ring, label centered under the animation.
 * Arbitrated 2026-09-03 (Lab C). Not a watermark: the mark is the wait signature.
 * Buttons keep `PendingLabel` (spinner): the PNG does not scale into a control.
 *
 * Sizes: `home` (Accueil, largest) · `page` (workspace) · `region` (panel).
 */
export function WaitMark({
  children,
  size = 'page',
}: {
  children: ReactNode;
  size?: WaitSize;
}) {
  return (
    <p
      className={['ds-wait', `ds-wait--${size}`].join(' ')}
      role="status"
      aria-live="polite"
    >
      <span className="ds-wait__orbit">
        <span className="ds-wait__ring" aria-hidden="true" />
        <PlayUpLockupMark className="ds-wait__mark" />
      </span>
      <span className="ds-wait__label">{children}</span>
    </p>
  );
}
