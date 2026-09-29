// -----------------------------------------------------------------------
// Shared Who → Where flow chrome for Qual / Prog Sorties editors.
// -----------------------------------------------------------------------

import type { ReactNode } from 'react';
import { ChevronDownIcon } from '../design-system/icons/shellIcons';

export function SortiesWhoWhereFlow({
  sourceLabel,
  destinationLabel,
  feedsLabel,
  source,
  destination,
}: {
  /** Accessible name for the source block (e.g. "Who?"). */
  sourceLabel: string;
  /** Accessible name for the destination block (e.g. Where?). */
  destinationLabel: string;
  /** Visible connector label (e.g. "Feeds"). */
  feedsLabel: string;
  source: ReactNode;
  destination: ReactNode;
}) {
  return (
    <div className="structure-qualification__flow">
      <section
        className="structure-qualification__flow-block"
        data-role="source"
        aria-label={sourceLabel}
      >
        {source}
      </section>
      <div
        className="structure-qualification__flow-connector"
        role="presentation"
      >
        <span className="structure-qualification__flow-connector-label">
          {feedsLabel}
        </span>
        <ChevronDownIcon size="md" aria-hidden="true" />
      </div>
      <section
        className="structure-qualification__flow-block"
        data-role="destination"
        aria-label={destinationLabel}
      >
        {destination}
      </section>
    </div>
  );
}
