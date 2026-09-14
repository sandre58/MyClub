import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { replaceStageCompositionEntries } from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { TextLink } from '../design-system/components/TextLink';
import type { StructureEntry, StructureStageHubSummary } from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { resolvePlacesN } from './structurePlaces';
import { MutationError, PendingLabel } from '../ui';

type StructureCompositionDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  stage: StructureStageHubSummary;
  entries: StructureEntry[];
  /** Focus search when opened via deep-link. */
  focusSearch?: boolean;
};

type Row = {
  entryId: string;
  displayName: string;
  eligible: boolean;
  selected: boolean;
};

/**
 * Work dialog — edit the root composition entry set (Affectation).
 * Partial Draft sets are allowed; k = N disables further growth.
 */
export function StructureCompositionDialog({
  open,
  onClose,
  competitionId,
  stage,
  entries,
  focusSearch = false,
}: StructureCompositionDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const searchRef = useRef<HTMLInputElement>(null);

  const capacity = resolvePlacesN(stage);
  const [selected, setSelected] = useState<Set<string>>(() => new Set());
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (!open) {
      setSearch('');
      return;
    }
    setSelected(new Set(stage.compositionEntryIds ?? []));
  }, [open, stage.compositionEntryIds, stage.stageId]);

  useEffect(() => {
    if (!open || !focusSearch) return;
    const timer = window.setTimeout(() => searchRef.current?.focus(), 50);
    return () => window.clearTimeout(timer);
  }, [open, focusSearch]);

  const rows: Row[] = useMemo(() => {
    const activeIds = new Set(
      entries.filter((e) => e.status === 'Active').map((e) => e.entryId),
    );
    const byId = new Map(entries.map((e) => [e.entryId, e]));

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
          eligible: activeIds.has(entryId),
          selected: selected.has(entryId),
        };
      })
      .sort((a, b) =>
        a.displayName.localeCompare(b.displayName, undefined, {
          sensitivity: 'base',
        }),
      );
  }, [entries, selected]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter((r) => r.displayName.toLowerCase().includes(q));
  }, [rows, search]);

  const k = selected.size;
  const atCapacity = capacity != null && k >= capacity;
  const emptyEligible =
    entries.filter((e) => e.status === 'Active').length === 0 && k === 0;

  const saveMutation = useMutation({
    mutationFn: () =>
      replaceStageCompositionEntries(stage.stageId, [...selected]),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, competitionId);
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
      if (capacity != null && next.size >= capacity) {
        return prev;
      }
      next.add(entryId);
      return next;
    });
  };

  const counterLabel =
    capacity == null
      ? t('composition.counterUnknown', { count: k })
      : t('composition.counter', { k, n: capacity });

  const teamsHref = `/competitions/${competitionId}/teams`;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title={t('composition.title', { phase: stage.name })}
      description={counterLabel}
      closeLabel={tCommon('close')}
      closeDisabled={saveMutation.isPending}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={saveMutation.isPending}
            onClick={onClose}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={saveMutation.isPending}
            onClick={() => saveMutation.mutate()}
          >
            {saveMutation.isPending ? (
              <PendingLabel>{t('composition.saving')}</PendingLabel>
            ) : (
              t('composition.save')
            )}
          </button>
        </>
      }
    >
      <div className="structure-composition">
        <MutationError error={saveMutation.error} />
        <label className="structure-composition__search">
          <span className="ds-visually-hidden">{t('composition.search')}</span>
          <input
            ref={searchRef}
            type="search"
            className="ds-input"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('composition.searchPlaceholder')}
            autoComplete="off"
          />
        </label>

        {emptyEligible ? (
          <div className="structure-composition__empty" role="status">
            <p>{t('composition.emptyEligible')}</p>
            <TextLink to={teamsHref}>{t('composition.openTeams')}</TextLink>
          </div>
        ) : (
          <ul
            className="structure-composition__list"
            role="listbox"
            aria-multiselectable="true"
          >
            {filtered.map((row) => {
              const disabledGrow =
                !row.selected && atCapacity && capacity != null;
              return (
                <li key={row.entryId}>
                  <label
                    className={[
                      'structure-composition__row',
                      !row.eligible
                        ? 'structure-composition__row--ineligible'
                        : null,
                      disabledGrow
                        ? 'structure-composition__row--locked'
                        : null,
                    ]
                      .filter(Boolean)
                      .join(' ')}
                  >
                    <input
                      type="checkbox"
                      checked={row.selected}
                      disabled={disabledGrow}
                      onChange={() => toggle(row.entryId, row.selected)}
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
  );
}
