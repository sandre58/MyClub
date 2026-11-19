// -----------------------------------------------------------------------
// Manual Cup placement — DirectAssignment dialog (Structure).
// -----------------------------------------------------------------------

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { assignEntryToSlot, clearSlotAssignment } from '../../../api';
import { ChoiceTile } from '../../../design-system/components/ChoiceTile';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Dialog } from '../../../design-system/components/Dialog';
import { Select } from '../../../design-system/components/Select';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  CheckIcon,
  DrawPendingIcon,
  EmptySelectionIcon,
  PersonIcon,
} from '../../../design-system/icons/contentIcons';
import { CloseIcon } from '../../../design-system/icons/shellIcons';
import { TeamCrest } from '../../../design-system/TeamCrest';
import { useClearMutationsOnOpen } from '../../../design-system/useClearMutationsOnOpen';
import { useDiscardConfirm } from '../../../design-system/useDiscardConfirm';
import type {
  SchematicCase,
  StructureEntry,
  StructureStageHubSummary,
} from '../../../types';
import { EmptyState, MutationError, PendingLabel } from '../../../ui';
import {
  listCupManualPlaces,
  occupiedCupPlaceLabel,
  occupiedEntryIds,
  type ManualPlaceMode,
} from './manualPlacementUi';
import { scheduleStructureRefresh } from '../structureInvalidation';
import { StructureManualPlacementSectionHead } from './StructureManualPlacementSectionHead';

type StructureManualPlacementDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  stage: StructureStageHubSummary;
  entries: StructureEntry[];
  cases: SchematicCase[];
  structureStages: readonly StructureStageHubSummary[];
  /** Preselect from schematic click; null = CTA (pick place). */
  initialSlotKey?: string | null;
};

type TeamChoice = {
  entryId: string;
  displayName: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  placedElsewhere?: string | null;
};

function compositionPoolIds(stage: StructureStageHubSummary): string[] {
  return stage.compositionEntryIds ?? [];
}

export function StructureManualPlacementDialog({
  open,
  onClose,
  competitionId,
  stage,
  entries,
  cases,
  structureStages,
  initialSlotKey = null,
}: StructureManualPlacementDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tReg } = useTranslation('regulation');
  const queryClient = useQueryClient();
  const liveAnnouncerId = useId();
  const teamsListRef = useRef<HTMLDivElement>(null);

  const places = useMemo(
    () => listCupManualPlaces(cases, structureStages),
    [cases, structureStages],
  );
  const editablePlaces = useMemo(
    () => places.filter((p) => p.mode === 'editable'),
    [places],
  );

  const [slotKey, setSlotKey] = useState<string | null>(null);
  const [entryId, setEntryId] = useState<string | null>(null);
  const [liveNote, setLiveNote] = useState<string | null>(null);
  /** Schematic click locks the place for the whole session. */
  const [placeLocked, setPlaceLocked] = useState(false);

  const selectedPlace = places.find((p) => p.slotKey === slotKey) ?? null;
  const mode: ManualPlaceMode | null = selectedPlace?.mode ?? null;
  const isBlocked =
    mode === 'qualProg' || mode === 'draw' || mode === 'unavailable';
  const canEdit = mode === 'editable';

  const baselineEntryId =
    selectedPlace?.mode === 'editable' ? selectedPlace.entryId : null;
  const dirty =
    canEdit &&
    slotKey != null &&
    (entryId ?? null) !== (baselineEntryId ?? null);

  const {
    requestClose: requestDiscardClose,
    discardOpen,
    confirmDiscard,
    cancelDiscard,
  } = useDiscardConfirm(dirty, onClose);

  const refresh = () => {
    scheduleStructureRefresh(queryClient, competitionId, ['hubStage'], {
      stageId: stage.stageId,
    });
  };

  useEffect(() => {
    if (!open) return;
    const locked = initialSlotKey != null && initialSlotKey.trim() !== '';
    setPlaceLocked(locked);
    const preferred = locked
      ? initialSlotKey
      : (editablePlaces[0]?.slotKey ?? places[0]?.slotKey ?? null);
    setSlotKey(preferred);
    const place = places.find((p) => p.slotKey === preferred);
    setEntryId(place?.mode === 'editable' ? place.entryId : null);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- open edge: seed from props once
  }, [open, initialSlotKey]);

  const occupied = useMemo(
    () => occupiedEntryIds(places, slotKey),
    [places, slotKey],
  );

  const entryById = useMemo(() => {
    const map = new Map<string, StructureEntry>();
    for (const entry of entries) {
      map.set(entry.entryId, entry);
    }
    return map;
  }, [entries]);

  const teamChoices = useMemo((): TeamChoice[] => {
    const pool = new Set(compositionPoolIds(stage));
    const choices: TeamChoice[] = [];
    for (const id of pool) {
      const elsewhere = occupiedCupPlaceLabel(places, id, slotKey);
      if (elsewhere) {
        const entry = entryById.get(id);
        choices.push({
          entryId: id,
          displayName:
            entry?.displayName?.trim() ||
            entry?.shortName?.trim() ||
            t('manualPlacement.unknownTeam'),
          logoMediaId: entry?.logoMediaId,
          primaryColor: entry?.primaryColor,
          placedElsewhere: elsewhere,
        });
        continue;
      }
      if (occupied.has(id) && id !== entryId) continue;
      const entry = entryById.get(id);
      choices.push({
        entryId: id,
        displayName:
          entry?.displayName?.trim() ||
          entry?.shortName?.trim() ||
          t('manualPlacement.unknownTeam'),
        logoMediaId: entry?.logoMediaId,
        primaryColor: entry?.primaryColor,
      });
    }
    if (entryId && !choices.some((c) => c.entryId === entryId)) {
      const entry = entryById.get(entryId);
      choices.unshift({
        entryId,
        displayName:
          entry?.displayName?.trim() || t('manualPlacement.unknownTeam'),
        logoMediaId: entry?.logoMediaId,
        primaryColor: entry?.primaryColor,
      });
    }
    choices.sort((a, b) =>
      a.displayName.localeCompare(b.displayName, undefined, {
        numeric: true,
        sensitivity: 'base',
      }),
    );
    return choices;
  }, [stage, occupied, entryId, entryById, places, slotKey, t]);

  const placeOptions = useMemo(
    () =>
      places.map((p) => {
        const teamName =
          p.entryId != null
            ? (entryById.get(p.entryId)?.displayName?.trim() ??
              t('manualPlacement.unknownTeam'))
            : null;
        return {
          value: p.slotKey,
          label:
            teamName != null
              ? t('manualPlacement.placeOccupied', {
                  place: p.label,
                  team: teamName,
                })
              : t('manualPlacement.placeFree', { place: p.label }),
          disabled: p.mode !== 'editable' && p.slotKey !== slotKey,
        };
      }),
    [places, slotKey, entryById, t],
  );

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!slotKey) {
        throw new Error('missing selection');
      }
      if (entryId) {
        await assignEntryToSlot(stage.stageId, slotKey, entryId);
        return;
      }
      await clearSlotAssignment(stage.stageId, slotKey);
    },
    onSuccess: () => {
      onClose();
      refresh();
    },
    onError: () => {
      refresh();
    },
  });

  useClearMutationsOnOpen(open, saveMutation);

  const pending = saveMutation.isPending;
  const mutationError = saveMutation.error;

  function requestClose() {
    requestDiscardClose(pending);
  }

  function focusTeamList() {
    window.requestAnimationFrame(() => {
      const root = teamsListRef.current;
      const tile = root?.querySelector<HTMLElement>(
        'button[role="radio"]:not([aria-disabled="true"]), input[type="radio"]:not(:disabled)',
      );
      if (tile) {
        tile.focus();
        return;
      }
      root?.focus();
    });
  }

  const blockedEmpty =
    mode === 'qualProg'
      ? {
          title: t('manualPlacement.blockedQualProgTitle'),
          body: t('manualPlacement.blockedQualProg'),
          icon: <PersonIcon size="lg" />,
        }
      : mode === 'draw'
        ? {
            title: t('manualPlacement.blockedDrawTitle'),
            body: t('manualPlacement.blockedDraw'),
            icon: <DrawPendingIcon size="lg" />,
          }
        : mode === 'unavailable'
          ? {
              title: t('manualPlacement.blockedUnavailableTitle'),
              body: t('manualPlacement.blockedUnavailable'),
              icon: <EmptySelectionIcon size="lg" />,
            }
          : null;

  const onPlaceChange = (next: string | null) => {
    if (placeLocked) return;
    setSlotKey(next);
    const place = places.find((p) => p.slotKey === next);
    setEntryId(place?.mode === 'editable' ? place.entryId : null);
    saveMutation.reset();
  };

  const canSave = canEdit && dirty;
  const canClearSelection = canEdit && entryId != null && !pending;
  const cupTargetLabel = selectedPlace?.label ?? slotKey ?? '';

  const cupCheckedCount = canEdit && entryId != null ? 1 : 0;
  const cupStatsTone = cupCheckedCount === 1 ? 'success' : 'attention';

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('manualPlacement.placementTitle', {
          phase: stage.name,
          target: cupTargetLabel,
        })}
        description={t('manualPlacement.lead')}
        size="md"
        closeLabel={tCommon('close')}
        closeDisabled={pending || discardOpen}
        trapFocus={!discardOpen}
        footerStatus={
          mutationError ? (
            <MutationError
              error={mutationError}
              structureStages={structureStages}
            />
          ) : null
        }
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--secondary"
              disabled={pending || discardOpen}
              onClick={requestClose}
            >
              <CloseIcon size="sm" />
              {tCommon('cancel')}
            </button>
            {canEdit ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={pending || !canSave}
                onClick={() => saveMutation.mutate()}
              >
                {saveMutation.isPending ? (
                  <PendingLabel>{t('manualPlacement.saving')}</PendingLabel>
                ) : (
                  <>
                    <CheckIcon size="sm" />
                    {t('manualPlacement.save')}
                  </>
                )}
              </button>
            ) : null}
          </>
        }
      >
        <div className="structure-manual-placement">
          <p
            id={liveAnnouncerId}
            className="ds-visually-hidden"
            aria-live="polite"
          >
            {liveNote}
          </p>
          {!placeLocked ? (
            <div className="structure-manual-placement__field">
              <span className="structure-manual-placement__label">
                {t('manualPlacement.placeLabel')}
              </span>
              <Select
                options={placeOptions}
                value={slotKey}
                onChange={onPlaceChange}
                placeholder={t('manualPlacement.placePlaceholder')}
                aria-label={t('manualPlacement.placeLabel')}
                disabled={pending || places.length === 0}
              />
            </div>
          ) : null}

          {canEdit ? (
            <div
              className="structure-qualification__facts structure-manual-placement__facts"
              data-tone={cupStatsTone}
              aria-live="polite"
            >
              <span
                className="structure-qualification__fact structure-qualification__fact--secondary"
                data-fact="places"
              >
                <span className="structure-qualification__fact-value">1</span>
                <span className="structure-qualification__fact-label">
                  {t('groupPlacement.factPlaces', { count: 1 })}
                </span>
              </span>
              <span
                className="structure-qualification__fact-rule"
                aria-hidden="true"
              />
              <span
                className="structure-qualification__fact structure-qualification__fact--primary"
                data-fact="occupied"
              >
                <span className="structure-qualification__fact-value">
                  {cupCheckedCount}
                </span>
                <span className="structure-qualification__fact-label">
                  {t('groupPlacement.factOccupied', { count: cupCheckedCount })}
                </span>
              </span>
            </div>
          ) : null}

          <div className="structure-manual-placement__body">
            {isBlocked && blockedEmpty ? (
              <EmptyState
                variant="idle"
                icon={blockedEmpty.icon}
                title={blockedEmpty.title}
              >
                {blockedEmpty.body}
              </EmptyState>
            ) : canEdit ? (
              <div className="structure-manual-placement__field structure-manual-placement__field--fill">
                <StructureManualPlacementSectionHead
                  label={t('manualPlacement.teamLabel')}
                  toolbar={
                    canClearSelection ? (
                      <Tooltip content={t('manualPlacement.clearSelection')}>
                        <button
                          type="button"
                          className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact ds-icon-button--dismiss"
                          disabled={pending}
                          aria-label={t('manualPlacement.clearSelection')}
                          onClick={() => {
                            setEntryId(null);
                            setLiveNote(t('manualPlacement.selectionCleared'));
                            saveMutation.reset();
                            focusTeamList();
                          }}
                        >
                          <CloseIcon size="sm" />
                        </button>
                      </Tooltip>
                    ) : null
                  }
                />
                {teamChoices.length === 0 && !baselineEntryId ? (
                  <EmptyState
                    variant="idle"
                    icon={<EmptySelectionIcon size="lg" />}
                    title={t('manualPlacement.emptyPoolTitle')}
                  >
                    {t('manualPlacement.emptyPool')}
                  </EmptyState>
                ) : (
                  <div
                    ref={teamsListRef}
                    tabIndex={-1}
                    className="structure-manual-placement__teams ds-scroll-safe"
                    role="radiogroup"
                    aria-label={t('manualPlacement.teamLabel')}
                  >
                    {teamChoices.map((team) => {
                      const selected = entryId === team.entryId;
                      const elsewhere = team.placedElsewhere;
                      return (
                        <ChoiceTile
                          key={team.entryId}
                          label={team.displayName}
                          description={
                            elsewhere
                              ? t('manualPlacement.placedOnCupHint', {
                                  place: elsewhere,
                                })
                              : undefined
                          }
                          layout="inline"
                          leading={
                            <TeamCrest
                              name={team.displayName}
                              logoMediaId={team.logoMediaId}
                              primaryColor={team.primaryColor}
                              size="sm"
                            />
                          }
                          role="radio"
                          selected={selected}
                          disabled={pending}
                          unavailable={elsewhere != null}
                          onChange={(next) => {
                            if (pending || !canEdit || elsewhere) return;
                            setEntryId(next ? team.entryId : null);
                            saveMutation.reset();
                          }}
                        />
                      );
                    })}
                  </div>
                )}
              </div>
            ) : null}
          </div>
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
        onConfirm={confirmDiscard}
        onCancel={cancelDiscard}
      />
    </>
  );
}
