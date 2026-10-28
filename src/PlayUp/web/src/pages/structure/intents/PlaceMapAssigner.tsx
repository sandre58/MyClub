// -----------------------------------------------------------------------
// Shared Place map — one source row, compact place menu (click, no drag).
// Qual = occurrence / rank. Prog = confrontation. Writes stay source-indexed.
// Cup/slot: a place can belong to one source. Groups: a pool may be reused.
// -----------------------------------------------------------------------

import {
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
} from 'react';
import { useTranslation } from 'react-i18next';
import { Chip } from '../../../design-system/components/Chip';
import { Popover } from '../../../design-system/components/Popover';
import { Tooltip } from '../../../design-system/components/Tooltip';
import { ListPlusIcon } from '../../../design-system/icons/contentIcons';
import { ChevronDownIcon } from '../../../design-system/icons/shellIcons';
import { ToastToneIcon } from '../../../design-system/icons/toastIcons';
import {
  alignPlaceAssignments,
  assignPlaceToSource,
  canFillPlaceMap,
  clearSourceAssignment,
  fillPlaceMap,
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

type MenuEntry = {
  id: string;
  label: string;
  disabled: boolean;
  clear: boolean;
  section: string | null;
};

function firstEnabledIndex(items: readonly MenuEntry[]) {
  return items.findIndex((item) => !item.disabled);
}

function stepEnabledIndex(
  items: readonly MenuEntry[],
  from: number,
  direction: 1 | -1,
) {
  const enabled = items.flatMap((item, index) =>
    item.disabled ? [] : [index],
  );
  if (enabled.length === 0) return from;
  const position = enabled.indexOf(from);
  if (position < 0) return enabled[0] ?? from;
  const next = position + direction;
  if (next < 0 || next >= enabled.length) return from;
  return enabled[next] ?? from;
}

function menuBlocks(items: readonly MenuEntry[]) {
  const blocks: {
    key: string;
    section: string | null;
    entries: { item: MenuEntry; index: number }[];
  }[] = [];
  items.forEach((item, index) => {
    const key = item.clear ? 'clear' : (item.section ?? '');
    const last = blocks[blocks.length - 1];
    if (!last || last.key !== key) {
      blocks.push({
        key,
        section: item.clear ? null : item.section,
        entries: [{ item, index }],
      });
      return;
    }
    last.entries.push({ item, index });
  });
  return blocks;
}

export function PlaceMapAssignerSkeleton({ cells }: { cells: number }) {
  const { t } = useTranslation('common');
  const count = Math.max(cells, 1);
  return (
    <div
      className="structure-place-map-assigner structure-place-map-assigner--skeleton"
      aria-busy="true"
      aria-label={t('loading')}
    >
      <ul className="structure-place-map-assigner__list" aria-hidden="true">
        {Array.from({ length: count }, (_, index) => (
          <li key={index} className="structure-place-map-assigner__row">
            <span className="structure-place-map-assigner__skel structure-place-map-assigner__skel--source" />
            <span className="structure-place-map-assigner__skel structure-place-map-assigner__skel--pill" />
          </li>
        ))}
      </ul>
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
  listAriaLabel,
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
  listAriaLabel: string;
  fillLabel: string;
  fillHint: string;
}) {
  const { t } = useTranslation('structure');
  const baseId = useId();
  const fillHintId = useId();
  const menuId = `${baseId}-places`;
  const anchorRef = useRef<HTMLButtonElement | null>(null);
  const pillRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const optionRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const restorePill = useRef<number | null>(null);
  const [openSource, setOpenSource] = useState<number | null>(null);
  const [activeOption, setActiveOption] = useState(0);

  const aligned = useMemo(
    () => alignPlaceAssignments(assignments, sources.length),
    [assignments, sources.length],
  );
  const placeIds = useMemo(() => places.map((place) => place.id), [places]);
  const anySection = places.some((place) => place.sectionLabel?.trim());
  const filled = aligned.filter((key) => key.trim()).length;
  const fillDisabled = !canFillPlaceMap(aligned, placeIds, uniquePlaces);

  const menuItems = useMemo(() => {
    if (openSource == null) return [];
    const current = aligned[openSource]?.trim() ?? '';
    const source = sources[openSource];
    const items: MenuEntry[] = [];
    if (current && source) {
      items.push({
        id: '',
        label: t('placeMap.clear', { source: source.label }),
        disabled: false,
        clear: true,
        section: null,
      });
    }
    for (const place of places) {
      const taken =
        uniquePlaces &&
        aligned.some(
          (key, index) => index !== openSource && key.trim() === place.id,
        );
      const section = anySection
        ? place.sectionLabel?.trim() || t('placeMap.sectionOther')
        : null;
      items.push({
        id: place.id,
        label: place.label,
        disabled: taken,
        clear: false,
        section,
      });
    }
    return items;
  }, [aligned, anySection, openSource, places, sources, t, uniquePlaces]);

  const blocks = useMemo(() => menuBlocks(menuItems), [menuItems]);
  const safeActive = (() => {
    if (menuItems.length === 0) return 0;
    if (!menuItems[activeOption]?.disabled) {
      return Math.min(activeOption, menuItems.length - 1);
    }
    const fallback = firstEnabledIndex(menuItems);
    return fallback >= 0 ? fallback : 0;
  })();

  useEffect(() => {
    if (openSource != null) return;
    const index = restorePill.current;
    restorePill.current = null;
    if (index != null) pillRefs.current[index]?.focus();
  }, [openSource]);

  function labelFor(placeId: string) {
    const trimmed = placeId.trim();
    if (!trimmed) return '';
    return places.find((place) => place.id === trimmed)?.label ?? trimmed;
  }

  function closeMenu() {
    setOpenSource((current) => {
      if (current != null) restorePill.current = current;
      return null;
    });
  }

  function openMenu(sourceIndex: number, anchor: HTMLButtonElement) {
    anchorRef.current = anchor;
    const current = aligned[sourceIndex]?.trim() ?? '';
    const selected = places.findIndex((place) => place.id === current);
    const start = current ? (selected >= 0 ? selected + 1 : 0) : 0;
    setActiveOption(start);
    setOpenSource(sourceIndex);
  }

  function toggleMenu(sourceIndex: number, anchor: HTMLButtonElement) {
    if (openSource === sourceIndex) {
      closeMenu();
      return;
    }
    openMenu(sourceIndex, anchor);
  }

  function choose(item: MenuEntry) {
    if (openSource == null || item.disabled) return;
    const next = item.clear
      ? clearSourceAssignment(assignments, sources.length, openSource)
      : assignPlaceToSource(
          assignments,
          sources.length,
          openSource,
          item.id,
          uniquePlaces,
        );
    if (!next) return;
    onAssignmentsChange(next);
    closeMenu();
  }

  function onMenuKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const target = event.target;
    if (!(target instanceof HTMLElement)) return;
    if (target.dataset.optionIndex == null) return;
    let next: number | null = null;
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowRight':
        next = stepEnabledIndex(menuItems, safeActive, 1);
        break;
      case 'ArrowUp':
      case 'ArrowLeft':
        next = stepEnabledIndex(menuItems, safeActive, -1);
        break;
      case 'Home':
        next = Math.max(firstEnabledIndex(menuItems), 0);
        break;
      case 'End': {
        const enabled = menuItems.flatMap((item, index) =>
          item.disabled ? [] : [index],
        );
        next = enabled[enabled.length - 1] ?? safeActive;
        break;
      }
      default:
        return;
    }
    event.preventDefault();
    setActiveOption(next);
  }

  const openLabel = openSource == null ? '' : sources[openSource]?.label;

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
                closeMenu();
              }}
            >
              <ListPlusIcon size="sm" />
              {fillLabel}
            </button>
          </Tooltip>
        </span>
      </div>

      <ul
        className="structure-place-map-assigner__list"
        aria-label={listAriaLabel}
      >
        {sources.map((source, index) => {
          const placeId = aligned[index]?.trim() ?? '';
          const placeLabel = labelFor(placeId);
          const empty = !placeId;
          const open = openSource === index;
          const pillLabel = empty
            ? t('placeMap.rowEmpty', { source: source.label })
            : t('placeMap.rowFilled', {
                source: source.label,
                place: placeLabel,
              });
          return (
            <li
              key={source.key}
              className="structure-place-map-assigner__row"
              data-invalid={empty ? 'true' : 'false'}
            >
              <Chip className="structure-place-map-assigner__source">
                {source.label}
              </Chip>
              <span className="structure-place-map-assigner__place">
                <button
                  ref={(node) => {
                    pillRefs.current[index] = node;
                  }}
                  type="button"
                  className="structure-place-map-assigner__pill"
                  data-empty={empty ? 'true' : 'false'}
                  aria-label={pillLabel}
                  aria-invalid={empty || undefined}
                  aria-haspopup="listbox"
                  aria-expanded={open}
                  aria-controls={open ? menuId : undefined}
                  onClick={(event) => toggleMenu(index, event.currentTarget)}
                  onKeyDown={(event) => {
                    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                      event.preventDefault();
                      openMenu(index, event.currentTarget);
                      return;
                    }
                    if (
                      (event.key === 'Delete' || event.key === 'Backspace') &&
                      placeId
                    ) {
                      event.preventDefault();
                      onAssignmentsChange(
                        clearSourceAssignment(
                          assignments,
                          sources.length,
                          index,
                        ),
                      );
                    }
                  }}
                >
                  <span className="structure-place-map-assigner__pill-label">
                    {empty ? t('placeMap.placeholder') : placeLabel}
                  </span>
                  <span
                    className="structure-place-map-assigner__chevron"
                    data-open={open ? 'true' : 'false'}
                    aria-hidden="true"
                  >
                    <ChevronDownIcon size="sm" />
                  </span>
                </button>
                {empty ? (
                  <span
                    className="structure-place-map-assigner__pending"
                    aria-hidden="true"
                  >
                    <ToastToneIcon tone="attention" size="sm" />
                  </span>
                ) : null}
              </span>
            </li>
          );
        })}
      </ul>

      <Popover
        id={menuId}
        open={openSource != null}
        onOpenChange={(next) => {
          if (!next) closeMenu();
        }}
        anchorRef={anchorRef}
        role="listbox"
        aria-label={t('placeMap.menuLabel', { source: openLabel ?? '' })}
        align="end"
        width={280}
        flipThreshold={160}
        className="structure-place-map-assigner__menu"
      >
        <div
          className="structure-place-map-assigner__options"
          onKeyDown={onMenuKeyDown}
        >
          {blocks.map((block) => {
            const options = block.entries.map(
              ({ item, index: optionIndex }) => (
                <button
                  key={item.clear ? 'clear' : item.id}
                  ref={(node) => {
                    optionRefs.current[optionIndex] = node;
                    if (node && optionIndex === safeActive) node.focus();
                  }}
                  type="button"
                  role="option"
                  className="structure-place-map-assigner__option"
                  data-option-index={optionIndex}
                  data-active={optionIndex === safeActive ? 'true' : 'false'}
                  data-selected={
                    !item.clear &&
                    openSource != null &&
                    aligned[openSource]?.trim() === item.id
                      ? 'true'
                      : 'false'
                  }
                  aria-selected={
                    !item.clear &&
                    openSource != null &&
                    aligned[openSource]?.trim() === item.id
                  }
                  disabled={item.disabled}
                  tabIndex={optionIndex === safeActive ? 0 : -1}
                  onClick={() => choose(item)}
                >
                  {item.label}
                </button>
              ),
            );
            if (!block.section) {
              return (
                <div
                  key={block.key}
                  className="structure-place-map-assigner__group"
                >
                  {options}
                </div>
              );
            }
            return (
              <div
                key={block.key}
                role="group"
                aria-label={block.section}
                className="structure-place-map-assigner__group"
              >
                <p className="structure-place-map-assigner__menu-section">
                  {block.section}
                </p>
                {options}
              </div>
            );
          })}
        </div>
      </Popover>

      <p className="ds-visually-hidden" role="status" aria-live="polite">
        {t('placeMap.counterAria', { filled, total: sources.length })}
      </p>
    </div>
  );
}
