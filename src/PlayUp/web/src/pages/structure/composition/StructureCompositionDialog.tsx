// -----------------------------------------------------------------------
// Composition dialog — Affectation set (Population).
// -----------------------------------------------------------------------

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Fragment, useEffect, useId, useMemo, useRef, useState } from 'react';
import { useOpenEdge, useSyncWhen } from '../../../design-system/useRenderSync';
import { useTranslation } from 'react-i18next';
import { replaceStageAffectationAuthoring } from '../../../api';
import { Alert } from '../../../design-system/components/Alert';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Dialog } from '../../../design-system/components/Dialog';
import { TextLink } from '../../../design-system/components/TextLink';
import { ToggleButtonGroup } from '../../../design-system/components/ToggleButtonGroup';
import {
  CheckIcon,
  ListChecksIcon,
  SearchIcon,
} from '../../../design-system/icons/contentIcons';
import { CloseIcon } from '../../../design-system/icons/shellIcons';
import { EntryCellWithdrawnMarker } from '../../../design-system/components/EntryCell';
import { TeamCrest } from '../../../design-system/TeamCrest';
import { notify } from '../../../design-system/toastStore';
import { useClearMutationsOnOpen } from '../../../design-system/useClearMutationsOnOpen';
import { useDiscardConfirm } from '../../../design-system/useDiscardConfirm';
import type { StructureEntry, StructureStageHubSummary } from '../../../types';
import { MutationError, PendingLabel } from '../../../ui';
import { scheduleStructureRefresh } from '../structureInvalidation';
import { OccupancyMeter } from './OccupancyMeter';
import { occupancyMeterState } from './occupancyMeterMath';
import { resolvePlacesN } from './structurePlaces';

type StructureCompositionDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  stage: StructureStageHubSummary;
  stages: StructureStageHubSummary[];
  entries: StructureEntry[];
  /**
   * Inbound Qualif/Prog expected volume (Draft). Reserves Places N so Affectation
   * cannot overfill the expected Population.
   */
  reservedFromFeeds?: number;
  /** Focus search when opened via deep-link. */
  focusSearch?: boolean;
};

type Row = {
  entryId: string;
  displayName: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  eligible: boolean;
  selected: boolean;
};

type TeamSegment = 'all' | 'selected' | 'available';

type CompositionFact = {
  key: string;
  value: string;
  label: string | null;
  primary?: boolean;
};

function sameIdSet(a: Set<string>, b: ReadonlySet<string>): boolean {
  if (a.size !== b.size) return false;
  for (const id of a) {
    if (!b.has(id)) return false;
  }
  return true;
}

function matchesQuery(name: string, query: string): boolean {
  return query.length === 0 || name.toLowerCase().includes(query);
}

type CompositionTeamRowProps = {
  row: Row;
  pending: boolean;
  locked: boolean;
  capacityHint?: string;
  withdrawnTip: string;
  onToggle: (entryId: string, selected: boolean) => void;
};

function CompositionTeamRow({
  row,
  pending,
  locked,
  capacityHint,
  withdrawnTip,
  onToggle,
}: CompositionTeamRowProps) {
  return (
    <label
      className={[
        'structure-composition__row',
        !row.eligible ? 'structure-composition__row--ineligible' : null,
        locked ? 'structure-composition__row--locked' : null,
      ]
        .filter(Boolean)
        .join(' ')}
      title={locked ? capacityHint : undefined}
    >
      <TeamCrest
        name={row.displayName}
        logoMediaId={row.logoMediaId}
        primaryColor={row.primaryColor}
        size="sm"
      />
      <span className="structure-composition__name">{row.displayName}</span>
      {!row.eligible ? (
        <EntryCellWithdrawnMarker
          tooltip={withdrawnTip}
          className="structure-composition__withdrawn-marker"
        />
      ) : null}
      <input
        type="checkbox"
        className="structure-composition__check"
        checked={row.selected}
        disabled={pending || (!row.eligible && !row.selected) || locked}
        onChange={() => onToggle(row.entryId, row.selected)}
      />
    </label>
  );
}

type CompositionSectionProps = {
  headingId: string;
  title: string;
  rows: Row[];
  emptyLabel: string;
  pending: boolean;
  atCapacity: boolean;
  affectationCap: number | null;
  capacityHint?: string;
  withdrawnTip: string;
  onToggle: (entryId: string, selected: boolean) => void;
};

function CompositionSection({
  headingId,
  title,
  rows,
  emptyLabel,
  pending,
  atCapacity,
  affectationCap,
  capacityHint,
  withdrawnTip,
  onToggle,
}: CompositionSectionProps) {
  return (
    <section
      className="structure-composition__section"
      aria-labelledby={headingId}
    >
      <h4 id={headingId} className="structure-composition__section-title">
        {title}
      </h4>
      {rows.length === 0 ? (
        <p className="structure-composition__section-empty">{emptyLabel}</p>
      ) : (
        <ul className="structure-composition__list">
          {rows.map((row) => {
            const locked =
              !row.selected && atCapacity && affectationCap != null;
            return (
              <li key={row.entryId}>
                <CompositionTeamRow
                  row={row}
                  pending={pending}
                  locked={locked}
                  capacityHint={capacityHint}
                  withdrawnTip={withdrawnTip}
                  onToggle={onToggle}
                />
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}

/**
 * Work dialog — edit the phase Population via Affectation (CompositionEntries).
 * Partial Draft sets are allowed. Selection cap accounts for inbound feed volume.
 * Available on non-root phases alongside inbound Qualif/Prog.
 */
export function StructureCompositionDialog({
  open,
  onClose,
  competitionId,
  stage,
  stages,
  entries,
  reservedFromFeeds = 0,
  focusSearch = false,
}: StructureCompositionDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tReg } = useTranslation('regulation');
  const queryClient = useQueryClient();
  const searchRef = useRef<HTMLInputElement>(null);
  const selectedHeadingId = useId();
  const availableHeadingId = useId();

  const placesN = resolvePlacesN(stage);
  const reserved = Math.max(0, reservedFromFeeds);
  /** Max teams Affectation may hold without exceeding Places N given reserved feeds. */
  const affectationCap =
    placesN != null ? Math.max(0, placesN - reserved) : null;

  const [selected, setSelected] = useState<Set<string>>(() => new Set());
  const [baseline, setBaseline] = useState<Set<string>>(() => new Set());
  const [search, setSearch] = useState('');
  const [segment, setSegment] = useState<TeamSegment>('all');

  const dirty = !sameIdSet(selected, baseline);
  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(dirty, onClose);

  useOpenEdge(open, {
    onClose: () => {
      setSearch('');
      setSegment('all');
    },
  });
  useSyncWhen(
    open,
    `${stage.stageId}:${(stage.affectationEntryIds ?? []).join(',')}`,
    () => {
      const initial = new Set(stage.affectationEntryIds ?? []);
      setSelected(initial);
      setBaseline(new Set(initial));
      resetDiscard();
    },
  );

  useEffect(() => {
    if (!open || !focusSearch) return;
    const timer = window.setTimeout(() => searchRef.current?.focus(), 50);
    return () => window.clearTimeout(timer);
  }, [open, focusSearch]);

  const byId = useMemo(
    () => new Map(entries.map((entry) => [entry.entryId, entry])),
    [entries],
  );

  const rows: Row[] = useMemo(() => {
    const activeIds = new Set(
      entries.filter((e) => e.status === 'Active').map((e) => e.entryId),
    );

    const allIds = new Set<string>([
      ...entries.filter((e) => e.status === 'Active').map((e) => e.entryId),
      ...selected,
    ]);

    return [...allIds]
      .map((entryId) => {
        const entry = byId.get(entryId);
        return {
          entryId,
          displayName: entry?.displayName ?? entryId,
          logoMediaId: entry?.logoMediaId,
          primaryColor: entry?.primaryColor,
          eligible: activeIds.has(entryId),
          selected: selected.has(entryId),
        };
      })
      .sort((a, b) =>
        a.displayName.localeCompare(b.displayName, undefined, {
          sensitivity: 'base',
        }),
      );
  }, [entries, selected, byId]);

  const query = search.trim().toLowerCase();
  const searchActive = query.length > 0;
  const selectedInScope = segment !== 'available';
  const availableInScope = segment !== 'selected';

  // Disponibles are active and unselected. Withdrawn entries stay in Sélectionnées only.
  const selectedMatches = selectedInScope
    ? rows.filter((row) => row.selected && matchesQuery(row.displayName, query))
    : [];
  const availableMatches = availableInScope
    ? rows.filter(
        (row) =>
          row.eligible && !row.selected && matchesQuery(row.displayName, query),
      )
    : [];

  const k = selected.size;
  const populationExpected = k + reserved;
  const atCapacity = affectationCap != null && k >= affectationCap;
  const remaining =
    placesN != null && placesN > 0
      ? Math.max(0, placesN - populationExpected)
      : null;
  const complete =
    placesN != null && placesN > 0 && populationExpected === placesN;
  const emptyEligible =
    entries.filter((e) => e.status === 'Active').length === 0 && k === 0;

  const canSelectAll =
    availableMatches.length > 0 &&
    (affectationCap == null || k < affectationCap);

  const meterState =
    placesN != null && placesN > 0
      ? occupancyMeterState(populationExpected, placesN)
      : null;
  const meterLabel =
    placesN != null && placesN > 0
      ? t('composition.counter', { k: populationExpected, n: placesN })
      : null;

  const scopedHasMatch =
    selectedMatches.length > 0 || availableMatches.length > 0;
  const showEmptySearch = searchActive && !scopedHasMatch;
  const showEmptySelected = !searchActive && segment === 'selected' && k === 0;
  const showEmptyEligible =
    !searchActive && emptyEligible && segment !== 'selected';

  const showSelectedSection =
    !showEmptySearch &&
    !showEmptyEligible &&
    !showEmptySelected &&
    selectedInScope &&
    k > 0 &&
    (!searchActive || selectedMatches.length > 0);
  const showAvailableSection =
    !showEmptySearch &&
    !showEmptyEligible &&
    !showEmptySelected &&
    availableInScope &&
    (!searchActive || availableMatches.length > 0);

  const facts: CompositionFact[] = [
    {
      key: 'selected',
      value: String(k),
      label: t('composition.factSelected', { count: k }),
      primary: true,
    },
  ];
  if (placesN != null && placesN > 0) {
    facts.push({
      key: 'places',
      value: String(placesN),
      label: t('composition.factPlaces', { count: placesN }),
    });
  }
  if (reserved > 0) {
    facts.push({
      key: 'reserved',
      value: String(reserved),
      label: t('composition.factReserved', { count: reserved }),
    });
  }
  if (remaining != null) {
    facts.push(
      complete
        ? {
            key: 'status',
            value: t('composition.statusComplete'),
            label: null,
          }
        : {
            key: 'remaining',
            value: String(remaining),
            label: t('composition.factRemaining', { count: remaining }),
          },
    );
  }

  const capacityHint =
    affectationCap != null
      ? t('composition.atCapacityHint', { n: affectationCap })
      : undefined;
  const withdrawnTip = t('composition.withdrawnFromCompetitionTip');

  const saveMutation = useMutation({
    mutationFn: () =>
      replaceStageAffectationAuthoring(stage.stageId, [...selected]),
    onSuccess: () => {
      notify.success(t('composition.toastUpdated'));
      onClose();
      scheduleStructureRefresh(queryClient, competitionId, ['graph']);
    },
  });

  useClearMutationsOnOpen(open, saveMutation);

  const toggle = (entryId: string, currentlySelected: boolean) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (currentlySelected) {
        next.delete(entryId);
        return next;
      }
      if (affectationCap != null && next.size >= affectationCap) {
        return prev;
      }
      next.add(entryId);
      return next;
    });
  };

  const selectAllFiltered = () => {
    setSelected((prev) => {
      const next = new Set(prev);
      const room =
        affectationCap != null
          ? Math.max(0, affectationCap - next.size)
          : Number.POSITIVE_INFINITY;
      if (room === 0) return prev;
      let added = 0;
      for (const row of availableMatches) {
        if (added >= room) break;
        if (!next.has(row.entryId)) {
          next.add(row.entryId);
          added += 1;
        }
      }
      return added === 0 ? prev : next;
    });
  };

  const teamsHref = `/competitions/${competitionId}/teams`;

  function requestClose() {
    requestDiscardClose(saveMutation.isPending);
  }

  const sectionProps = {
    pending: saveMutation.isPending,
    atCapacity,
    affectationCap,
    capacityHint,
    withdrawnTip,
    onToggle: toggle,
  };

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        size="lg"
        title={t('composition.title', { phase: stage.name })}
        description={t('composition.lede')}
        closeLabel={tCommon('close')}
        closeDisabled={saveMutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        footerStatus={
          saveMutation.isError ? (
            <MutationError
              error={saveMutation.error}
              structureStages={stages}
            />
          ) : null
        }
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--secondary"
              disabled={saveMutation.isPending || discardOpen}
              onClick={requestClose}
            >
              <CloseIcon size="sm" />
              {tCommon('cancel')}
            </button>
            <button
              type="button"
              className="ds-btn ds-btn--primary"
              disabled={saveMutation.isPending || !dirty}
              onClick={() => saveMutation.mutate()}
            >
              {saveMutation.isPending ? (
                <PendingLabel>{t('composition.saving')}</PendingLabel>
              ) : (
                <>
                  <CheckIcon size="sm" />
                  {t('composition.save')}
                </>
              )}
            </button>
          </>
        }
      >
        <div className="structure-composition">
          <div className="structure-composition__chrome">
            <div
              className="structure-qualification__facts structure-composition__facts"
              aria-live="polite"
            >
              {facts.map((fact, index) => (
                <Fragment key={fact.key}>
                  {index > 0 ? (
                    <span
                      className="structure-qualification__fact-rule"
                      aria-hidden="true"
                    />
                  ) : null}
                  <span
                    className={[
                      'structure-qualification__fact',
                      fact.primary
                        ? 'structure-qualification__fact--primary'
                        : 'structure-qualification__fact--secondary',
                    ].join(' ')}
                  >
                    <span className="structure-qualification__fact-value">
                      {fact.value}
                    </span>
                    {fact.label != null ? (
                      <span className="structure-qualification__fact-label">
                        {fact.label}
                      </span>
                    ) : null}
                  </span>
                </Fragment>
              ))}
            </div>

            {meterState != null && meterLabel != null && placesN != null ? (
              <OccupancyMeter
                current={populationExpected}
                capacity={placesN}
                state={meterState}
                fillVariant="brand"
                ariaLabel={meterLabel}
                className="structure-composition__meter"
              />
            ) : null}

            {atCapacity || reserved > 0 ? (
              <Alert tone={atCapacity ? 'warning' : 'info'} role="status">
                <div className="structure-composition__guidance">
                  {atCapacity && capacityHint != null ? (
                    <p>{capacityHint}</p>
                  ) : null}
                  {reserved > 0 ? (
                    <p>
                      {t('composition.reservedBySources', { count: reserved })}
                    </p>
                  ) : null}
                </div>
              </Alert>
            ) : null}
          </div>

          <div className="structure-composition__controls">
            <ToggleButtonGroup
              className="structure-composition__segments"
              aria-label={t('composition.segmentAria')}
              value={segment}
              onChange={setSegment}
              options={[
                { value: 'all', label: t('composition.segmentAll') },
                {
                  value: 'selected',
                  label: t('composition.segmentSelected'),
                },
                {
                  value: 'available',
                  label: t('composition.segmentAvailable'),
                },
              ]}
            />
            <div className="structure-composition__search-row">
              <label className="structure-composition__search">
                <span className="ds-visually-hidden">
                  {t('composition.search')}
                </span>
                <span className="ds-input">
                  <span className="ds-input__leading" aria-hidden="true">
                    <SearchIcon size="sm" />
                  </span>
                  <input
                    ref={searchRef}
                    type="search"
                    className="ds-input__control"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder={t('composition.searchPlaceholder')}
                    autoComplete="off"
                  />
                </span>
              </label>
              <button
                type="button"
                className="ds-btn ds-btn--ghost structure-composition__select-all"
                disabled={!canSelectAll || saveMutation.isPending}
                onClick={selectAllFiltered}
              >
                <ListChecksIcon size="sm" />
                {t('composition.selectAll')}
              </button>
            </div>
          </div>

          {showEmptyEligible ? (
            <div className="structure-composition__empty">
              <p>{t('composition.emptyEligible')}</p>
              <TextLink to={teamsHref}>{t('composition.openTeams')}</TextLink>
            </div>
          ) : showEmptySearch ? (
            <div className="structure-composition__empty">
              <p>{t('composition.emptySearch')}</p>
            </div>
          ) : showEmptySelected ? (
            <div className="structure-composition__empty">
              <p>{t('composition.emptySelected')}</p>
            </div>
          ) : (
            <div className="structure-composition__sections">
              {showSelectedSection ? (
                <CompositionSection
                  headingId={selectedHeadingId}
                  title={t('composition.segmentSelected')}
                  rows={selectedMatches}
                  emptyLabel={t('composition.emptySelected')}
                  {...sectionProps}
                />
              ) : null}
              {showAvailableSection ? (
                <CompositionSection
                  headingId={availableHeadingId}
                  title={t('composition.segmentAvailable')}
                  rows={availableMatches}
                  emptyLabel={t('composition.emptyAvailable')}
                  {...sectionProps}
                />
              ) : null}
            </div>
          )}
        </div>
      </Dialog>

      <ConfirmDialog
        open={discardOpen}
        title={tReg('editor.discardTitle')}
        message={tReg('editor.discardMessage')}
        confirmLabel={tReg('editor.discardConfirm')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        danger
        onCancel={cancelDiscard}
        onConfirm={confirmDiscard}
      />
    </>
  );
}
