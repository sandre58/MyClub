import type { ReactElement, ReactNode } from 'react';
import { MatchRow } from '../../../design-system/components/MatchRow';
import { AlreadyPlacedStatus } from '../../../design-system/components/Status';
import { TeamCrest } from '../../../design-system/TeamCrest';
import type { SlotConfrontationRow, SlotConfrontationSide } from './drawUi';

function crestFor(side: SlotConfrontationSide): ReactElement {
  if (!side.entryId) {
    return <span aria-hidden="true" />;
  }
  return (
    <TeamCrest
      name={side.displayName}
      logoMediaId={side.logoMediaId}
      primaryColor={side.primaryColor}
      size="sm"
    />
  );
}

function sideName(side: SlotConfrontationSide): ReactNode {
  if (side.muted) {
    return <span className="ds-meta">{side.displayName}</span>;
  }
  return side.displayName;
}

function sideMark(side: SlotConfrontationSide): ReactNode {
  if (!side.alreadyPlacedLabel) {
    return null;
  }
  return <AlreadyPlacedStatus>{side.alreadyPlacedLabel}</AlreadyPlacedStatus>;
}

function rowAria(row: SlotConfrontationRow): string {
  if (row.sideA.cellState || row.sideB.cellState) {
    const home = row.sideA.ariaLabel ?? row.sideA.displayName;
    const away = row.sideB.ariaLabel ?? row.sideB.displayName;
    return `${row.key} · ${home} vs ${away}`;
  }
  return `${row.key} · ${row.sideA.slotKey} vs ${row.sideB.slotKey}`;
}

function unpairedAria(side: SlotConfrontationSide): string {
  if (side.cellState) {
    return side.ariaLabel ?? side.displayName;
  }
  return `${side.displayName} · ${side.slotKey}`;
}

function ConfrontationMatchRow({
  row,
}: {
  row: SlotConfrontationRow;
}): ReactElement {
  const aside = `${row.sideA.slotKey} vs ${row.sideB.slotKey}`;
  return (
    <li aria-label={rowAria(row)}>
      <MatchRow
        home={{
          name: sideName(row.sideA),
          mark: sideMark(row.sideA),
          crest: crestFor(row.sideA),
        }}
        away={{
          name: sideName(row.sideB),
          mark: sideMark(row.sideB),
          crest: crestFor(row.sideB),
        }}
        score={row.key}
        scoreMuted
        aside={aside}
        density="compact"
      />
    </li>
  );
}

function UnpairedMatchRow({
  side,
}: {
  side: SlotConfrontationSide;
}): ReactElement {
  return (
    <li aria-label={unpairedAria(side)}>
      <MatchRow
        home={{
          name: sideName(side),
          mark: sideMark(side),
          crest: crestFor(side),
        }}
        away={{ name: '', crest: <span aria-hidden="true" /> }}
        score={side.slotKey}
        scoreMuted
        aside={<span aria-hidden="true" />}
        density="compact"
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
  ariaLabel?: string;
  unpairedLabel?: string;
}): ReactElement {
  return (
    <div className="draw-slot-results">
      {rows.length > 0 ? (
        <ul
          className="draw-slot-rows"
          {...(ariaLabel ? { 'aria-label': ariaLabel } : {})}
        >
          {rows.map((row) => (
            <ConfrontationMatchRow key={row.key} row={row} />
          ))}
        </ul>
      ) : null}
      {unpaired.length > 0 ? (
        <ul
          className="draw-slot-rows draw-slot-rows__unpaired"
          {...((unpairedLabel ?? ariaLabel)
            ? { 'aria-label': unpairedLabel ?? ariaLabel }
            : {})}
        >
          {unpaired.map((side) => (
            <UnpairedMatchRow key={side.slotKey} side={side} />
          ))}
        </ul>
      ) : null}
    </div>
  );
}
