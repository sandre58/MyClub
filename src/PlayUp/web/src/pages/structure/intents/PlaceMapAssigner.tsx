// -----------------------------------------------------------------------
// Shared Place map — destination holes + source popover (click, no drag).
// Qual = occurrence / rank. Prog = confrontation. Writes stay source-indexed.
// Slot: a filled hole clears its source, then the popover reassigns that hole.
// Groups: the popover adds another source; each chip has its own clear.
// -----------------------------------------------------------------------

import { useEffect, useId, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Popover } from '../../../design-system/components/Popover';
import { Tooltip } from '../../../design-system/components/Tooltip';
import { ListPlusIcon } from '../../../design-system/icons/contentIcons';
import { CloseIcon } from '../../../design-system/icons/shellIcons';
import {
  PLACE_MAP_COLUMNS,
  alignPlaceAssignments,
  assignPlaceToSource,
  canFillPlaceMap,
  chunkPlaceMapRows,
  clearSourceAssignment,
  fillPlaceMap,
  groupPlaceMapSections,
  placeMapBoardScrolls,
} from './placeMapAssignerModel';

export type PlaceMapAssignerSource = {
  key: string;
  label: string;
};

export type PlaceMapAssignerPlace = {
  id: string;
  label: string;
  sectionLabel?: string | null;
};

export function PlaceMapAssignerSkeleton({ cells }: { cells: number }) {
  const { t } = useTranslation('common');
  const count = Math.max(cells, 3);
  return (
    <div
      className="structure-place-map-assigner structure-place-map-assigner--skeleton"
      aria-busy="true"
      aria-label={t('loading')}
    >
      <div className="structure-place-map-assigner__rows" aria-hidden="true">
        {Array.from({ length: count }, (_, index) => (
          <span key={index} className="structure-place-map-assigner__skel" />
        ))}
      </div>
    </div>
  );
}

export function PlaceMapAssigner({
  sources,
  places,
  assignments,
  onAssignmentsChange,
  uniquePlaces,
  heading,
  boardAriaLabel,
  fillLabel,
  fillHint,
}: {
  sources: readonly PlaceMapAssignerSource[];
  places: readonly PlaceMapAssignerPlace[];
  /** Parallel to `sources`. Empty string = unmapped. */
  assignments: readonly string[];
  onAssignmentsChange: (next: string[]) => void;
  /** Cup/slot: one source per place. Groups: reuse allowed. */
  uniquePlaces: boolean;
  heading?: string;
  boardAriaLabel: string;
  fillLabel: string;
  fillHint: string;
}) {
  const { t } = useTranslation('structure');
  const baseId = useId();
  const fillHintId = useId();
  const popoverId = `${baseId}-sources`;
  const anchorRef = useRef<HTMLElement | null>(null);
  const placeRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const sourceRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const [openPlaceId, setOpenPlaceId] = useState<string | null>(null);
  const [activePlace, setActivePlace] = useState(0);
  const [activeSource, setActiveSource] = useState(0);

  const aligned = useMemo(
    () => alignPlaceAssignments(assignments, sources.length),
    [assignments, sources.length],
  );
  const sections = useMemo(
    () => groupPlaceMapSections(places, t('placeMap.sectionOther')),
    [places, t],
  );
  const sectioned = sections.some((section) => section.label);
  const scroll = placeMapBoardScrolls(places.length);
  const visualPlaces = useMemo(
    () => sections.flatMap((section) => section.places),
    [sections],
  );
  const boardSections = useMemo(() => {
    const laidOut: {
      label: string | null;
      sectionIndex: number;
      rows: {
        rowNumber: number;
        cells: {
          place: PlaceMapAssignerPlace;
          columnIndex: number;
          visualIndex: number;
        }[];
      }[];
    }[] = [];
    let visualIndex = 0;
    let rowNumber = 0;
    for (
      let sectionIndex = 0;
      sectionIndex < sections.length;
      sectionIndex += 1
    ) {
      const section = sections[sectionIndex];
      if (!section) continue;
      const rows: (typeof laidOut)[number]['rows'] = [];
      for (const chunk of chunkPlaceMapRows(
        section.places,
        PLACE_MAP_COLUMNS,
      )) {
        rowNumber += 1;
        const cells: (typeof rows)[number]['cells'] = [];
        for (
          let columnIndex = 0;
          columnIndex < chunk.length;
          columnIndex += 1
        ) {
          const place = chunk[columnIndex];
          if (!place) continue;
          cells.push({ place, columnIndex, visualIndex });
          visualIndex += 1;
        }
        rows.push({ rowNumber, cells });
      }
      laidOut.push({
        label: section.label,
        sectionIndex,
        rows,
      });
    }
    return laidOut;
  }, [sections]);
  const rowCount = boardSections.reduce(
    (sum, section) => sum + section.rows.length,
    0,
  );

  const unplaced = sources.flatMap((source, index) =>
    aligned[index]?.trim() ? [] : [{ source, index }],
  );
  const filled = aligned.filter((key) => key.trim()).length;
  const placeIds = places.map((place) => place.id);
  const fillDisabled = !canFillPlaceMap(aligned, placeIds, uniquePlaces);
  const safeActiveSource =
    unplaced.length === 0 ? 0 : Math.min(activeSource, unplaced.length - 1);

  useEffect(() => {
    if (!openPlaceId) return;
    sourceRefs.current[safeActiveSource]?.focus();
  }, [openPlaceId, safeActiveSource]);

  function occupantsFor(placeId: string) {
    return sources.flatMap((source, index) =>
      aligned[index]?.trim() === placeId ? [{ source, index }] : [],
    );
  }

  function focusPlace(index: number) {
    if (visualPlaces.length === 0) return;
    const next = Math.max(0, Math.min(visualPlaces.length - 1, index));
    setActivePlace(next);
    placeRefs.current[next]?.focus();
  }

  function focusSource(index: number) {
    if (unplaced.length === 0) return;
    const next = Math.max(0, Math.min(unplaced.length - 1, index));
    setActiveSource(next);
    sourceRefs.current[next]?.focus();
  }

  function closePopover() {
    setOpenPlaceId(null);
  }

  function openPopover(placeId: string, anchor: HTMLElement) {
    anchorRef.current = anchor;
    setActiveSource(0);
    setOpenPlaceId(placeId);
  }

  function onCellClick(placeId: string, anchor: HTMLButtonElement) {
    if (openPlaceId === placeId) {
      closePopover();
      return;
    }
    const occupants = occupantsFor(placeId);
    if (uniquePlaces && occupants[0]) {
      onAssignmentsChange(
        clearSourceAssignment(assignments, sources.length, occupants[0].index),
      );
    }
    openPopover(placeId, anchor);
  }

  function chooseSource(sourceIndex: number) {
    if (!openPlaceId) return;
    const next = assignPlaceToSource(
      assignments,
      sources.length,
      sourceIndex,
      openPlaceId,
      uniquePlaces,
    );
    if (!next) return;
    onAssignmentsChange(next);
    closePopover();
    const visualIndex = visualPlaces.findIndex(
      (place) => place.id === openPlaceId,
    );
    if (visualIndex >= 0) focusPlace(visualIndex);
  }

  function onPopoverKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    const target = event.target;
    if (!(target instanceof HTMLElement)) return;
    const raw = target.dataset.sourceIndex;
    if (raw == null) return;
    const index = Number(raw);
    if (!Number.isInteger(index)) return;
    let next: number | null = null;
    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        next = index + 1;
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        next = index - 1;
        break;
      case 'Home':
        next = 0;
        break;
      case 'End':
        next = unplaced.length - 1;
        break;
      default:
        return;
    }
    event.preventDefault();
    focusSource(next);
  }

  function onGridKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    const target = event.target;
    if (!(target instanceof HTMLElement)) return;
    if (target.dataset.placeClear != null) return;
    const raw = target.dataset.placeIndex;
    if (raw == null) return;
    const index = Number(raw);
    if (!Number.isInteger(index)) return;
    if (event.key === 'Delete' || event.key === 'Backspace') {
      event.preventDefault();
      const place = visualPlaces[index];
      const first = place ? occupantsFor(place.id)[0] : undefined;
      if (!first) return;
      onAssignmentsChange(
        clearSourceAssignment(assignments, sources.length, first.index),
      );
      return;
    }
    let next: number | null = null;
    switch (event.key) {
      case 'ArrowRight':
        next = index + 1;
        break;
      case 'ArrowLeft':
        next = index - 1;
        break;
      case 'ArrowDown':
        next = index + PLACE_MAP_COLUMNS;
        break;
      case 'ArrowUp':
        next = index - PLACE_MAP_COLUMNS;
        break;
      case 'Home':
        next = 0;
        break;
      case 'End':
        next = visualPlaces.length - 1;
        break;
      default:
        return;
    }
    event.preventDefault();
    focusPlace(next);
  }

  return (
    <div className="structure-place-map-assigner">
      <div className="structure-place-map-assigner__toolbar">
        <div className="structure-place-map-assigner__title">
          {heading ? (
            <p className="structure-place-map-assigner__heading">{heading}</p>
          ) : null}
          <p className="structure-place-map-assigner__counter ds-num">
            {t('placeMap.counter', { filled, total: sources.length })}
          </p>
        </div>
        <span className="structure-place-map-assigner__fill">
          <span id={fillHintId} className="ds-visually-hidden">
            {fillHint}
          </span>
          <Tooltip content={fillHint}>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              aria-describedby={fillHintId}
              disabled={fillDisabled}
              onClick={() => {
                onAssignmentsChange(
                  fillPlaceMap(aligned, placeIds, uniquePlaces),
                );
                closePopover();
              }}
            >
              <ListPlusIcon size="sm" />
              {fillLabel}
            </button>
          </Tooltip>
        </span>
      </div>

      <div
        className="structure-place-map-assigner__board"
        data-scroll={scroll ? 'true' : 'false'}
      >
        <div
          role="grid"
          aria-label={boardAriaLabel}
          aria-rowcount={rowCount}
          aria-colcount={PLACE_MAP_COLUMNS}
          data-sectioned={sectioned ? 'true' : 'false'}
          className="structure-place-map-assigner__sections"
          onKeyDown={onGridKeyDown}
        >
          {boardSections.map((section) => {
            const sectionId = `${baseId}-section-${section.sectionIndex}`;
            return (
              <div
                key={section.label ?? `flat-${section.sectionIndex}`}
                role="none"
              >
                {section.label ? (
                  <p
                    id={sectionId}
                    className="structure-place-map-assigner__section-label"
                  >
                    {section.label}
                  </p>
                ) : null}
                <div
                  role="rowgroup"
                  aria-labelledby={section.label ? sectionId : undefined}
                >
                  {section.rows.map((row) => (
                    <div
                      key={`${section.label ?? 'flat'}-${row.rowNumber}`}
                      role="row"
                      className="structure-place-map-assigner__rows"
                      aria-rowindex={row.rowNumber}
                    >
                      {row.cells.map((cell) => {
                        const occupants = occupantsFor(cell.place.id);
                        const occupantLabel = occupants
                          .map((occupant) => occupant.source.label)
                          .join(', ');
                        const cellLabel =
                          occupants.length > 0
                            ? t('placeMap.cellFilled', {
                                place: cell.place.label,
                                source: occupantLabel,
                              })
                            : t('placeMap.cellEmpty', {
                                place: cell.place.label,
                              });
                        const open = openPlaceId === cell.place.id;
                        return (
                          <div
                            key={cell.place.id}
                            role="gridcell"
                            aria-rowindex={row.rowNumber}
                            aria-colindex={cell.columnIndex + 1}
                            className="structure-place-map-assigner__cell"
                            data-filled={
                              occupants.length > 0 ? 'true' : 'false'
                            }
                            data-open={open ? 'true' : 'false'}
                          >
                            <button
                              ref={(node) => {
                                placeRefs.current[cell.visualIndex] = node;
                              }}
                              type="button"
                              data-place-index={cell.visualIndex}
                              tabIndex={
                                cell.visualIndex ===
                                Math.min(
                                  activePlace,
                                  Math.max(visualPlaces.length - 1, 0),
                                )
                                  ? 0
                                  : -1
                              }
                              className="structure-place-map-assigner__cell-hit"
                              aria-label={cellLabel}
                              aria-haspopup="listbox"
                              aria-expanded={open}
                              aria-controls={open ? popoverId : undefined}
                              onClick={(event) =>
                                onCellClick(cell.place.id, event.currentTarget)
                              }
                            >
                              <span className="structure-place-map-assigner__place-label">
                                {cell.place.label}
                              </span>
                              {occupants.length > 0 ? (
                                <span className="structure-place-map-assigner__chips">
                                  {occupants.map((occupant) => (
                                    <span
                                      key={occupant.source.key}
                                      className="structure-place-map-assigner__chip"
                                    >
                                      {occupant.source.label}
                                    </span>
                                  ))}
                                </span>
                              ) : null}
                            </button>
                            {occupants.length > 0 ? (
                              <span className="structure-place-map-assigner__clears">
                                {occupants.map((occupant) => (
                                  <button
                                    key={occupant.source.key}
                                    type="button"
                                    data-place-clear=""
                                    className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
                                    tabIndex={-1}
                                    aria-label={t('placeMap.clear', {
                                      source: occupant.source.label,
                                      place: cell.place.label,
                                    })}
                                    onClick={() =>
                                      onAssignmentsChange(
                                        clearSourceAssignment(
                                          assignments,
                                          sources.length,
                                          occupant.index,
                                        ),
                                      )
                                    }
                                  >
                                    <CloseIcon size="sm" aria-hidden="true" />
                                  </button>
                                ))}
                              </span>
                            ) : null}
                          </div>
                        );
                      })}
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      <Popover
        key={openPlaceId ?? 'closed'}
        id={popoverId}
        open={openPlaceId != null}
        onOpenChange={(next) => {
          if (!next) closePopover();
        }}
        anchorRef={anchorRef}
        role="listbox"
        aria-label={t('placeMap.popoverLabel')}
        width={280}
        flipThreshold={160}
        className="structure-place-map-assigner__popover"
      >
        {unplaced.length === 0 ? (
          <p
            className="structure-place-map-assigner__popover-empty"
            role="status"
          >
            {t('placeMap.popoverEmpty')}
          </p>
        ) : (
          <div
            className="structure-place-map-assigner__sources"
            onKeyDown={onPopoverKeyDown}
          >
            {unplaced.map((row, trayIndex) => (
              <button
                key={row.source.key}
                ref={(node) => {
                  sourceRefs.current[trayIndex] = node;
                }}
                type="button"
                role="option"
                data-source-index={trayIndex}
                tabIndex={trayIndex === safeActiveSource ? 0 : -1}
                aria-selected={trayIndex === safeActiveSource}
                className="structure-place-map-assigner__source"
                aria-label={t('placeMap.chooseSource', {
                  source: row.source.label,
                })}
                onClick={() => chooseSource(row.index)}
              >
                {row.source.label}
              </button>
            ))}
          </div>
        )}
      </Popover>

      <p className="ds-visually-hidden" role="status" aria-live="polite">
        {t('placeMap.counterAria', { filled, total: sources.length })}
      </p>
    </div>
  );
}
