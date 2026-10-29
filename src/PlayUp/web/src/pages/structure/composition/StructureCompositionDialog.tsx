// -----------------------------------------------------------------------
// Composition dialog — Affectation set (Population).
// -----------------------------------------------------------------------

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Fragment, useEffect, useId, useMemo, useRef, useState } from 'react';
import { useOpenEdge, useSyncWhen } from '../../../design-system/useRenderSync';
import { useTranslation } from 'react-i18next';
import { replaceStageAffectationAuthoring } from '../../../api';
import { Chip } from '../../../design-system/components/Chip';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { ChoiceTile } from '../../../design-system/components/ChoiceTile';
import { Dialog } from '../../../design-system/components/Dialog';
import { TextLink } from '../../../design-system/components/TextLink';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  CheckIcon,
  ListChecksIcon,
  SearchIcon,
} from '../../../design-system/icons/contentIcons';
import { CloseIcon } from '../../../design-system/icons/shellIcons';
import { TeamCrest } from '../../../design-system/TeamCrest';
import { notify } from '../../../design-system/toastStore';
import { useClearMutationsOnOpen } from '../../../design-system/useClearMutationsOnOpen';
import { useDiscardConfirm } from '../../../design-system/useDiscardConfirm';
import type { StructureEntry, StructureStageHubSummary } from '../../../types';
import { MutationError, PendingLabel } from '../../../ui';
import { scheduleStructureRefresh } from '../structureInvalidation';
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

type CompositionFactKey = 'places' | 'selected' | 'remaining' | 'reserved';

type CompositionFact = {
  key: CompositionFactKey;
  value: string;
  label: string;
};

/** Restantes tone. Zero stays secondary; any other count is attention. */
type CompositionRemainingTone = 'attention' | 'secondary';

function compositionRemainingTone(remaining: number): CompositionRemainingTone {
  return remaining === 0 ? 'secondary' : 'attention';
}

function sameIdSet(a: Set<string>, b: ReadonlySet<string>): boolean {
  if (a.size !== b.size) return false;
  for (const id of a) {
    if (!b.has(id)) return false;
  }
  return true;
}

type CompositionTeamRowProps = {
  row: Row;
  pending: boolean;
  locked: boolean;
  capacityHintId?: string;
  withdrawnLabel: string;
  withdrawnTip: string;
  onToggle: (entryId: string, selected: boolean) => void;
};

function CompositionTeamRow({
  row,
  pending,
  locked,
  capacityHintId,
  withdrawnLabel,
  withdrawnTip,
  onToggle,
}: CompositionTeamRowProps) {
  const disabled = pending || (!row.eligible && !row.selected) || locked;

  return (
    <ChoiceTile
      label={row.displayName}
      leading={
        <TeamCrest
          name={row.displayName}
          logoMediaId={row.logoMediaId}
          primaryColor={row.primaryColor}
          size="sm"
        />
      }
      description={
        !row.eligible ? (
          <Tooltip content={withdrawnTip}>
            <Chip tone="neutral">{withdrawnLabel}</Chip>
          </Tooltip>
        ) : undefined
      }
      selected={row.selected}
      layout="inline"
      disabled={disabled}
      aria-describedby={locked ? capacityHintId : undefined}
      onChange={() => onToggle(row.entryId, row.selected)}
    />
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
  const capacityHintId = useId();

  const placesN = resolvePlacesN(stage);
  const reserved = Math.max(0, reservedFromFeeds);
  /** Max teams Affectation may hold without exceeding Places N given reserved feeds. */
  const affectationCap =
    placesN != null ? Math.max(0, placesN - reserved) : null;

  const [selected, setSelected] = useState<Set<string>>(() => new Set());
  const [baseline, setBaseline] = useState<Set<string>>(() => new Set());
  const [search, setSearch] = useState('');

  const dirty = !sameIdSet(selected, baseline);
  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(dirty, onClose);

  useOpenEdge(open, { onClose: () => setSearch('') });
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

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter((row) => row.displayName.toLowerCase().includes(q));
  }, [rows, search]);

  const k = selected.size;
  /** Open affectation places. Not Places N minus k. */
  const remaining = affectationCap != null ? affectationCap - k : null;
  const tone = remaining != null ? compositionRemainingTone(remaining) : null;
  const atCapacity = affectationCap != null && k >= affectationCap;
  const emptyEligible =
    entries.filter((e) => e.status === 'Active').length === 0 && k === 0;
  const searchActive = search.trim().length > 0;

  const selectableFiltered = useMemo(
    () => filtered.filter((row) => row.eligible && !row.selected),
    [filtered],
  );
  const canSelectAll =
    selectableFiltered.length > 0 &&
    (affectationCap == null || k < affectationCap);

  const facts: CompositionFact[] = [];
  if (placesN != null && placesN > 0) {
    facts.push({
      key: 'places',
      value: String(placesN),
      label: t('composition.factPlaces', { count: placesN }),
    });
  }
  facts.push({
    key: 'selected',
    value: String(k),
    label: t('composition.factSelected', { count: k }),
  });
  if (remaining != null) {
    facts.push({
      key: 'remaining',
      value: String(remaining),
      label: t('composition.factRemaining', { count: remaining }),
    });
  }
  if (reserved > 0) {
    facts.push({
      key: 'reserved',
      value: String(reserved),
      label: t('composition.factReserved', { count: reserved }),
    });
  }

  const capacityHint =
    affectationCap != null
      ? t('composition.atCapacityHint', { n: affectationCap })
      : undefined;
  const withdrawnLabel = t('composition.noLongerEligible');
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
      for (const row of selectableFiltered) {
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

  const showEmptySearch = searchActive && filtered.length === 0;
  const showEmptyEligible = !searchActive && emptyEligible;

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
              data-tone={tone ?? undefined}
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
                      fact.key === 'selected'
                        ? 'structure-qualification__fact--primary'
                        : 'structure-qualification__fact--secondary',
                    ].join(' ')}
                    data-fact={fact.key}
                  >
                    <span
                      className="structure-qualification__fact-value"
                      data-zero={
                        fact.key === 'selected' && k === 0 ? 'true' : undefined
                      }
                    >
                      {fact.value}
                    </span>
                    <span className="structure-qualification__fact-label">
                      {fact.label}
                    </span>
                  </span>
                </Fragment>
              ))}
            </div>

            {atCapacity && capacityHint != null ? (
              <p id={capacityHintId} className="ds-visually-hidden">
                {capacityHint}
              </p>
            ) : null}

            <div className="structure-composition__toolbar">
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
          ) : (
            <ul
              className="structure-composition__list"
              aria-label={t('composition.listAria')}
            >
              {filtered.map((row) => {
                const locked =
                  !row.selected && atCapacity && affectationCap != null;
                return (
                  <li key={row.entryId}>
                    <CompositionTeamRow
                      row={row}
                      pending={saveMutation.isPending}
                      locked={locked}
                      capacityHintId={
                        locked && atCapacity ? capacityHintId : undefined
                      }
                      withdrawnLabel={withdrawnLabel}
                      withdrawnTip={withdrawnTip}
                      onToggle={toggle}
                    />
                  </li>
                );
              })}
            </ul>
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
