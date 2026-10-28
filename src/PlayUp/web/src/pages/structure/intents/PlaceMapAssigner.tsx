// -----------------------------------------------------------------------
// Shared Place map — destination cells + source tray (click, no drag).
// Qual = occurrence / rank. Prog = confrontation. Writes stay source-indexed.
// -----------------------------------------------------------------------

import { useId, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Tooltip } from '../../../design-system/components/Tooltip';
import { useDismissLayer } from '../../../design-system/useDismissLayer';
import { ListPlusIcon } from '../../../design-system/icons/contentIcons';
import { CloseIcon } from '../../../design-system/icons/shellIcons';
import { ToastToneIcon } from '../../../design-system/icons/toastIcons';
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
  const trayLabelId = `${baseId}-tray`;
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [activeSource, setActiveSource] = useState(0);
  const [activePlace, setActivePlace] = useState(0);
  const sourceRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const placeRefs = useRef<Array<HTMLButtonElement | null>>([]);

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
  const selected =
    unplaced.find((row) => row.source.key === selectedKey) ?? null;
  const filled = aligned.filter((key) => key.trim()).length;
  const placeIds = places.map((place) => place.id);
  const fillDisabled = !canFillPlaceMap(aligned, placeIds, uniquePlaces);

  useDismissLayer(selected != null, () => setSelectedKey(null));

  function occupantsFor(placeId: string) {
    return sources.flatMap((source, index) =>
      aligned[index]?.trim() === placeId ? [{ source, index }] : [],
    );
  }

  function focusSource(index: number) {
    if (unplaced.length === 0) return;
    const next = Math.max(0, Math.min(unplaced.length - 1, index));
    setActiveSource(next);
    sourceRefs.current[next]?.focus();
  }

  function focusPlace(index: number) {
    if (visualPlaces.length === 0) return;
    const next = Math.max(0, Math.min(visualPlaces.length - 1, index));
    setActivePlace(next);
    placeRefs.current[next]?.focus();
  }

  function onTrayKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    const target = event.target;
    if (!(target instanceof HTMLElement)) return;
    const raw = target.dataset.trayIndex;
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

  function onPlaceClick(placeId: string, blocked: boolean) {
    if (!selected || blocked) return;
    const next = assignPlaceToSource(
      assignments,
      sources.length,
      selected.index,
      placeId,
      uniquePlaces,
    );
    if (!next) return;
    onAssignmentsChange(next);
    setSelectedKey(null);
    const visualIndex = visualPlaces.findIndex((place) => place.id === placeId);
    if (visualIndex >= 0) focusPlace(visualIndex);
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
                setSelectedKey(null);
              }}
            >
              <ListPlusIcon size="sm" />
              {fillLabel}
            </button>
          </Tooltip>
        </span>
      </div>

      <div className="structure-place-map-assigner__tray">
        <p
          id={trayLabelId}
          className="structure-place-map-assigner__tray-label"
        >
          {t('placeMap.trayLabel')}
        </p>
        {unplaced.length === 0 ? (
          <p className="structure-place-map-assigner__empty-tray" role="status">
            {t('placeMap.emptyTray')}
          </p>
        ) : (
          <div
            role="listbox"
            aria-labelledby={trayLabelId}
            aria-orientation="horizontal"
            className="structure-place-map-assigner__sources"
            onKeyDown={onTrayKeyDown}
          >
            {unplaced.map((row, trayIndex) => {
              const isSelected = selectedKey === row.source.key;
              return (
                <button
                  key={row.source.key}
                  ref={(node) => {
                    sourceRefs.current[trayIndex] = node;
                  }}
                  id={`${baseId}-source-${trayIndex}`}
                  type="button"
                  role="option"
                  data-tray-index={trayIndex}
                  tabIndex={
                    trayIndex ===
                    Math.min(activeSource, Math.max(unplaced.length - 1, 0))
                      ? 0
                      : -1
                  }
                  aria-selected={isSelected}
                  aria-invalid="true"
                  data-invalid="true"
                  className="structure-place-map-assigner__source"
                  aria-label={t('placeMap.sourceOption', {
                    source: row.source.label,
                  })}
                  onClick={() => {
                    setActiveSource(trayIndex);
                    setSelectedKey((current) =>
                      current === row.source.key ? null : row.source.key,
                    );
                  }}
                >
                  <ToastToneIcon tone="attention" size="sm" />
                  <span>{row.source.label}</span>
                </button>
              );
            })}
          </div>
        )}
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
                        const blocked =
                          uniquePlaces &&
                          occupants.length > 0 &&
                          selected != null;
                        const armed = selected != null && !blocked;
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
                            data-armed={armed ? 'true' : 'false'}
                            data-blocked={blocked ? 'true' : 'false'}
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
                              aria-disabled={blocked || undefined}
                              onClick={() =>
                                onPlaceClick(cell.place.id, blocked)
                              }
                            >
                              <span className="structure-place-map-assigner__place-label">
                                {cell.place.label}
                              </span>
                              {occupants.length > 0 ? (
                                <span className="structure-place-map-assigner__occupant">
                                  {occupantLabel}
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

      <p className="ds-visually-hidden" role="status" aria-live="polite">
        {t('placeMap.counterAria', { filled, total: sources.length })}
      </p>
      <p className="ds-visually-hidden" role="status" aria-live="polite">
        {selected
          ? t('placeMap.selectionStatus', { source: selected.source.label })
          : ''}
      </p>
    </div>
  );
}
