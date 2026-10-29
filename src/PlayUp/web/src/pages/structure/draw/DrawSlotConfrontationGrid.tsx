import type { ReactElement } from 'react';
import { MatchRow } from '../../../design-system/components/MatchRow';
import { TeamCrest } from '../../../design-system/TeamCrest';
import type { SlotConfrontationRow, SlotConfrontationSide } from './drawUi';

function crestFor(side: SlotConfrontationSide): ReactElement {
  return (
    <TeamCrest
      name={side.displayName}
      logoMediaId={side.logoMediaId}
      primaryColor={side.primaryColor}
      size="sm"
    />
  );
}

function ConfrontationMatchRow({
  row,
}: {
  row: SlotConfrontationRow;
}): ReactElement {
  const aside = `${row.sideA.slotKey} vs ${row.sideB.slotKey}`;
  const label = `${row.key} · ${aside}`;
  return (
    <li aria-label={label}>
      <MatchRow
        home={{ name: row.sideA.displayName, crest: crestFor(row.sideA) }}
        away={{ name: row.sideB.displayName, crest: crestFor(row.sideB) }}
        score={row.key}
        scoreMuted
        aside={aside}
      />
    </li>
  );
}

function UnpairedMatchRow({
  side,
}: {
  side: SlotConfrontationSide;
}): ReactElement {
  const label = `${side.slotKey} · ${side.displayName}`;
  return (
    <li aria-label={label}>
      <MatchRow
        home={{ name: side.displayName, crest: crestFor(side) }}
        away={{ name: '—', crest: <span aria-hidden="true" /> }}
        score={side.slotKey}
        scoreMuted
        aside={side.slotKey}
      />
    </li>
  );
}

/** Cup draw product: one MatchRow per bracket pair, aside `S1 vs S2`. */
export function DrawSlotConfrontationGrid({
  rows,
  unpaired = [],
  ariaLabel,
  unpairedLabel,
}: {
  rows: readonly SlotConfrontationRow[];
  unpaired?: readonly SlotConfrontationSide[];
  ariaLabel: string;
  unpairedLabel?: string;
}): ReactElement {
  return (
    <div className="draw-slot-results">
      {rows.length > 0 ? (
        <ul className="draw-slot-rows" aria-label={ariaLabel}>
          {rows.map((row) => (
            <ConfrontationMatchRow key={row.key} row={row} />
          ))}
        </ul>
      ) : null}
      {unpaired.length > 0 ? (
        <ul className="draw-slot-rows" aria-label={unpairedLabel ?? ariaLabel}>
          {unpaired.map((side) => (
            <UnpairedMatchRow key={side.slotKey} side={side} />
          ))}
        </ul>
      ) : null}
    </div>
  );
}
