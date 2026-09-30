// -----------------------------------------------------------------------
// Composition dialog — Affectation set (Population).
// -----------------------------------------------------------------------

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { replaceStageAffectationAuthoring } from '../api';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
import { TextLink } from '../design-system/components/TextLink';
import { Tooltip } from '../design-system/components/Tooltip';
import {
  CheckIcon,
  ListChecksIcon,
  SearchIcon,
} from '../design-system/icons/contentIcons';
import { CloseIcon } from '../design-system/icons/shellIcons';
import { TeamCrest } from '../design-system/TeamCrest';
import { notify } from '../design-system/toastStore';
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import type { StructureEntry, StructureStageHubSummary } from '../types';
import { MutationError, PendingLabel } from '../ui';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { resolvePlacesN } from './structurePlaces';

type StructureCompositionDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  stage: StructureStageHubSummary;
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

function sameIdSet(a: Set<string>, b: ReadonlySet<string>): boolean {
  if (a.size !== b.size) return false;
  for (const id of a) {
    if (!b.has(id)) return false;
  }
  return true;
}

/**
 * Work dialog — edit the phase Population via Affectation (CompositionEntries).
 * Partial Draft sets are allowed. Selection cap accounts for inbound feed volume.
 * B2: available on non-root phases alongside inbound Qualif/Prog.
 */
export function StructureCompositionDialog({
  open,
  onClose,
  competitionId,
  stage,
  entries,
  reservedFromFeeds = 0,
  focusSearch = false,
}: StructureCompositionDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tReg } = useTranslation('regulation');
  const queryClient = useQueryClient();
  const searchRef = useRef<HTMLInputElement>(null);

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

  useEffect(() => {
    if (!open) {
      setSearch('');
      return;
    }
    const initial = new Set(stage.affectationEntryIds ?? []);
    setSelected(initial);
    setBaseline(new Set(initial));
    resetDiscard();
  }, [open, stage.affectationEntryIds, stage.stageId, resetDiscard]);

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
    return rows.filter((r) => r.displayName.toLowerCase().includes(q));
  }, [rows, search]);

  const k = selected.size;
  const populationExpected = k + reserved;
  const atCapacity =
    affectationCap != null && k >= affectationCap;
  const remaining =
    placesN != null && placesN > 0
      ? Math.max(0, placesN - populationExpected)
      : null;
  const complete =
    placesN != null && placesN > 0 && populationExpected === placesN;
  const emptyEligible =
    entries.filter((e) => e.status === 'Active').length === 0 && k === 0;

  const selectableFiltered = useMemo(
    () => filtered.filter((r) => r.eligible && !r.selected),
    [filtered],
  );
  const canSelectAll =
    selectableFiltered.length > 0 &&
    (affectationCap == null || k < affectationCap);

  const ratio =
    placesN != null && placesN > 0
      ? Math.min(1, Math.max(0, populationExpected / placesN))
      : 0;

  const saveMutation = useMutation({
    mutationFn: () =>
      replaceStageAffectationAuthoring(stage.stageId, [...selected]),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, competitionId);
      notify.success(t('composition.toastUpdated'));
      onClose();
    },
  });

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

  const counterLabel =
    placesN == null
      ? t('composition.counterUnknown', { count: populationExpected })
      : t('composition.counter', { k: populationExpected, n: placesN });

  const statusLabel =
    placesN == null
      ? null
      : complete
        ? t('composition.statusComplete')
        : remaining != null
          ? t('composition.statusRemaining', { count: remaining })
          : null;

  const teamsHref = `/competitions/${competitionId}/teams`;

  function requestClose() {
    requestDiscardClose(saveMutation.isPending);
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        size="md"
        title={t('composition.title', { phase: stage.name })}
        description={t('composition.lede')}
        closeLabel={tCommon('close')}
        closeDisabled={saveMutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        footerStatus={
          saveMutation.isError ? (
            <MutationError error={saveMutation.error} />
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
        <div className="structure-composition__toolbar">
          <div
            className={[
              'structure-composition__meter',
              complete ? 'structure-composition__meter--complete' : null,
              placesN != null &&
              placesN > 0 &&
              populationExpected < placesN
                ? 'structure-composition__meter--short'
                : null,
              placesN != null && populationExpected > placesN
                ? 'structure-composition__meter--over'
                : null,
            ]
              .filter(Boolean)
              .join(' ')}
          >
            <p className="structure-composition__counter" aria-live="polite">
              {counterLabel}
            </p>
            {placesN != null && placesN > 0 ? (
              <div
                className="structure-composition__track"
                aria-hidden="true"
              >
                <span
                  className="structure-composition__fill"
                  style={{ width: `${ratio * 100}%` }}
                />
              </div>
            ) : null}
            {statusLabel ? (
              <p className="structure-composition__status" role="status">
                {statusLabel}
              </p>
            ) : null}
          </div>

          <Tooltip content={t('composition.selectAll')}>
            <button
              type="button"
              className="ds-btn ds-btn--ghost ds-icon-button structure-composition__select-all"
              disabled={!canSelectAll || saveMutation.isPending}
              aria-label={t('composition.selectAll')}
              onClick={selectAllFiltered}
            >
              <ListChecksIcon size="sm" />
            </button>
          </Tooltip>

          <label className="structure-composition__search">
            <span className="ds-visually-hidden">{t('composition.search')}</span>
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
        </div>

        {emptyEligible ? (
          <div className="structure-composition__empty">
            <p>{t('composition.emptyEligible')}</p>
            <TextLink to={teamsHref}>{t('composition.openTeams')}</TextLink>
          </div>
        ) : (
          <ul className="structure-composition__list" aria-label={t('composition.listAria')}>
            {filtered.map((row) => {
              const locked =
                !row.selected && atCapacity && affectationCap != null;
              return (
                <li key={row.entryId}>
                  <label
                    className={[
                      'structure-composition__row',
                      !row.eligible
                        ? 'structure-composition__row--ineligible'
                        : null,
                      locked ? 'structure-composition__row--locked' : null,
                    ]
                      .filter(Boolean)
                      .join(' ')}
                    title={
                      locked
                        ? t('composition.atCapacityHint', {
                            n: affectationCap,
                          })
                        : undefined
                    }
                  >
                    <input
                      type="checkbox"
                      checked={row.selected}
                      disabled={
                        saveMutation.isPending ||
                        (!row.eligible && !row.selected) ||
                        locked
                      }
                      onChange={() => toggle(row.entryId, row.selected)}
                    />
                    <TeamCrest
                      name={row.displayName}
                      logoMediaId={row.logoMediaId}
                      primaryColor={row.primaryColor}
                      size="sm"
                    />
                    <span className="structure-composition__name">
                      {row.displayName}
                    </span>
                    {!row.eligible ? (
                      <span className="structure-composition__warn">
                        {t('composition.noLongerEligible')}
                      </span>
                    ) : null}
                  </label>
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
