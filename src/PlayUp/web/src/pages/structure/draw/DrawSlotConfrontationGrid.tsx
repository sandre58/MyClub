import type { ReactElement } from 'react';
import { TeamCrest } from '../../../design-system/TeamCrest';
import type { SlotConfrontationRow, SlotConfrontationSide } from './drawUi';

function ConfrontationSide({
  side,
}: {
  side: SlotConfrontationSide;
}): ReactElement {
  return (
    <span className="draw-confrontation__side">
      <TeamCrest
        name={side.displayName}
        logoMediaId={side.logoMediaId}
        primaryColor={side.primaryColor}
        size="sm"
      />
      <span className="draw-confrontation__name">{side.displayName}</span>
    </span>
  );
}

/** Cup draw product: one card per bracket pair, label `P{n} · S1/S2`. */
export function DrawSlotConfrontationGrid({
  rows,
  vsLabel,
  ariaLabel,
}: {
  rows: readonly SlotConfrontationRow[];
  vsLabel: string;
  ariaLabel: string;
}): ReactElement {
  return (
    <ul className="draw-confrontation-grid" aria-label={ariaLabel}>
      {rows.map((row) => {
        const label = `${row.key} · ${row.sideA.slotKey}/${row.sideB.slotKey}`;
        return (
          <li key={row.key}>
            <article className="draw-confrontation" aria-label={label}>
              <h4 className="draw-confrontation__label">{label}</h4>
              <ConfrontationSide side={row.sideA} />
              <p className="draw-confrontation__vs">{vsLabel}</p>
              <ConfrontationSide side={row.sideB} />
            </article>
          </li>
        );
      })}
    </ul>
  );
}
