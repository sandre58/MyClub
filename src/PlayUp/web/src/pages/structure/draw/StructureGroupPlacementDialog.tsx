// Manual group placement dialog. Cup placement stays in StructureManualPlacementDialog.

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { assignEntryToGroup, clearEntryFromGroup } from '../../../api';
import { ChoiceTile } from '../../../design-system/components/ChoiceTile';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Dialog } from '../../../design-system/components/Dialog';
import { Select } from '../../../design-system/components/Select';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  CheckIcon,
  EmptySelectionIcon,
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
import { scheduleStructureRefresh } from '../structureInvalidation';
import {
  groupManualPlaceStillAllowed,
  listAvailableGroupEntries,
  listGroupManualTargets,
  resolveGroupDisplayLabel,
  resolveGroupManualPlace,
} from './manualGroupPlacementUi';

type StructureGroupPlacementDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  stage: StructureStageHubSummary;
  structureStages: readonly StructureStageHubSummary[];
  entries: StructureEntry[];
  cases: SchematicCase[];
  /** Schematic seat, or null when opened from the phase sheet CTA. */
  place: SchematicCase | null;
  /** `remove` opens modify (clear then place); `place` is a straight assignment. */
  intent: 'place' | 'remove';
};

function occupantId(place: SchematicCase | null): string | null {
  if (!place) return null;
  return place.entry?.entryId ?? place.assignment?.entryId ?? null;
}

function casesHubEpoch(cases: readonly SchematicCase[]): string {
  return cases
    .map((place) => {
      if (place.formPosition.kind !== 'GroupPlace') return '';
      const id = place.formPosition.groupId ?? '';
      const occ = place.entry?.entryId ?? place.assignment?.entryId ?? '';
      return `${id}:${occ}:${place.seatMode ?? ''}:${place.formPosition.index ?? ''}`;
    })
    .join('|');
}

export function StructureGroupPlacementDialog({
  open,
  onClose,
  competitionId,
  stage,
  structureStages,
  entries,
  cases,
  place,
  intent,
}: StructureGroupPlacementDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tReg } = useTranslation('regulation');
  const queryClient = useQueryClient();
  const actions = useMemo(() => stage.actions ?? [], [stage.actions]);
  const hubEpochAtRemove = useRef<string | null>(null);

  const groupTargets = useMemo(
    () =>
      listGroupManualTargets(cases, actions, (name) =>
        t('place.group', { name }),
      ),
    [cases, actions, t],
  );

  const placeLockedFromSchematic =
    place != null &&
    place.formPosition.kind === 'GroupPlace' &&
    place.formPosition.groupId?.trim() != null;

  const [groupId, setGroupId] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [modifyMode, setModifyMode] = useState(false);
  const [seatBlockedAfterRemove, setSeatBlockedAfterRemove] = useState(false);
  const [afterRemove, setAfterRemove] = useState(false);
  const [removeConfirmOpen, setRemoveConfirmOpen] = useState(false);

  const activeTarget =
    groupTargets.find((target) => target.groupId === groupId) ?? null;
  const activePlace =
    (placeLockedFromSchematic && place?.formPosition.groupId === groupId
      ? place
      : null) ??
    activeTarget?.place ??
    resolveGroupManualPlace(cases, groupId ?? '', actions);

  const groupLabel =
    groupId != null
      ? resolveGroupDisplayLabel(cases, groupId, (name) =>
          t('place.group', { name }),
        )
      : '';
  const occupiedId = modifyMode ? occupantId(activePlace) : null;
  const available = useMemo(
    () => listAvailableGroupEntries(stage, cases, entries),
    [stage, cases, entries],
  );
  const compositionEmpty = (stage.compositionEntryIds ?? []).length === 0;

  const teamChoices = useMemo(() => {
    const choices = [...available];
    if (occupiedId && !choices.some((row) => row.entryId === occupiedId)) {
      const entry = entries.find((row) => row.entryId === occupiedId);
      const occupiedName =
        activePlace?.assignment?.displayName?.trim() ||
        activePlace?.entry?.displayName?.trim() ||
        entry?.displayName?.trim() ||
        occupiedId;
      const pot =
        stage.compositionEntryPots?.find((row) => row.entryId === occupiedId)
          ?.pot ?? null;
      choices.unshift({
        entryId: occupiedId,
        displayName: occupiedName,
        logoMediaId:
          activePlace?.assignment?.logoMediaId ?? entry?.logoMediaId ?? null,
        primaryColor:
          activePlace?.assignment?.primaryColor ?? entry?.primaryColor ?? null,
        pot,
      });
    }
    return choices;
  }, [available, occupiedId, activePlace, entries, stage.compositionEntryPots]);

  const refresh = () => {
    scheduleStructureRefresh(queryClient, competitionId, ['hubStage'], {
      stageId: stage.stageId,
    });
  };

  const placeMutation = useMutation({
    mutationFn: async () => {
      const targetGroup = groupId ?? activePlace?.formPosition.groupId ?? '';
      if (!targetGroup || !selectedId) {
        throw new Error('missing selection');
      }
      await assignEntryToGroup(stage.stageId, targetGroup, selectedId);
    },
    onSuccess: () => {
      refresh();
      onClose();
    },
    onError: () => {
      refresh();
    },
  });

  const removeMutation = useMutation({
    mutationFn: async () => {
      const targetGroup = groupId ?? activePlace?.formPosition.groupId ?? '';
      const entry = occupantId(activePlace);
      if (!targetGroup || !entry) {
        throw new Error('missing selection');
      }
      await clearEntryFromGroup(stage.stageId, targetGroup, entry);
    },
    onSuccess: () => {
      setRemoveConfirmOpen(false);
      refresh();
      hubEpochAtRemove.current = casesHubEpoch(cases);
      setModifyMode(false);
      setSelectedId(null);
      setAfterRemove(true);
      setSeatBlockedAfterRemove(false);
      placeMutation.reset();
    },
    onError: () => {
      setRemoveConfirmOpen(false);
      refresh();
    },
  });

  useClearMutationsOnOpen(open, placeMutation, removeMutation);

  useEffect(() => {
    if (!open || !afterRemove || !groupId) return;
    const epoch = casesHubEpoch(cases);
    if (hubEpochAtRemove.current === epoch) {
      setSeatBlockedAfterRemove(false);
      return;
    }
    hubEpochAtRemove.current = null;
    setAfterRemove(false);
    if (groupManualPlaceStillAllowed(cases, groupId, actions)) {
      setSeatBlockedAfterRemove(false);
    } else {
      setSeatBlockedAfterRemove(true);
    }
  }, [open, afterRemove, groupId, cases, actions]);

  useEffect(() => {
    if (!open) return;
    const initialGroup =
      place?.formPosition.kind === 'GroupPlace'
        ? (place.formPosition.groupId?.trim() ?? null)
        : (groupTargets[0]?.groupId ?? null);
    setGroupId(initialGroup);
    const occupied = occupantId(place);
    const editing = intent === 'remove' && occupied != null;
    setModifyMode(editing);
    setSelectedId(editing ? occupied : null);
    setSeatBlockedAfterRemove(false);
    setAfterRemove(false);
    hubEpochAtRemove.current = null;
    setRemoveConfirmOpen(false);
    placeMutation.reset();
    removeMutation.reset();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- open edge
  }, [open, place, intent]);

  const pending = placeMutation.isPending || removeMutation.isPending;
  const mutationError = placeMutation.error ?? removeMutation.error;

  const dirty =
    !modifyMode &&
    !seatBlockedAfterRemove &&
    selectedId != null &&
    !compositionEmpty;

  const {
    requestClose: requestDiscardClose,
    discardOpen,
    confirmDiscard,
    cancelDiscard,
  } = useDiscardConfirm(dirty, onClose);

  function requestClose() {
    requestDiscardClose(pending);
  }

  const groupOptions = useMemo(
    () =>
      groupTargets.map((target) => ({
        value: target.groupId,
        label: target.label,
      })),
    [groupTargets],
  );

  const onGroupChange = (next: string | null) => {
    if (placeLockedFromSchematic) return;
    setGroupId(next);
    setSelectedId(null);
    setModifyMode(false);
    setSeatBlockedAfterRemove(false);
    setAfterRemove(false);
    hubEpochAtRemove.current = null;
    placeMutation.reset();
  };

  const occupiedName =
    teamChoices.find((row) => row.entryId === occupiedId)?.displayName ?? '';
  const awaitingHubRefresh = afterRemove && !seatBlockedAfterRemove;
  const showTeamSection =
    !seatBlockedAfterRemove &&
    !compositionEmpty &&
    groupId != null &&
    (teamChoices.length > 0 || awaitingHubRefresh);
  const canSave =
    !modifyMode &&
    !seatBlockedAfterRemove &&
    selectedId != null &&
    selectedId !== occupiedId &&
    !compositionEmpty;
  const canClearOccupant =
    modifyMode && occupiedId != null && !pending && !removeConfirmOpen;

  const dialogOpen =
    open && (place != null || groupTargets.length > 0) && groupId != null;

  const blockedEmpty = seatBlockedAfterRemove ? (
    <EmptyState
      variant="idle"
      icon={<EmptySelectionIcon size="lg" />}
      title={t('groupPlacement.blockedAfterRemoveTitle')}
    >
      {t('groupPlacement.blockedAfterRemove')}
    </EmptyState>
  ) : null;

  return (
    <>
      <Dialog
        open={dialogOpen}
        onClose={requestClose}
        title={t('manualPlacement.title', { phase: stage.name })}
        description={t('manualPlacement.lead')}
        size="md"
        closeLabel={tCommon('close')}
        closeDisabled={pending || discardOpen || removeConfirmOpen}
        trapFocus={!discardOpen && !removeConfirmOpen}
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
              disabled={pending || discardOpen || removeConfirmOpen}
              onClick={requestClose}
            >
              <CloseIcon size="sm" />
              {tCommon('cancel')}
            </button>
            {showTeamSection && !modifyMode ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={pending || !canSave}
                onClick={() => placeMutation.mutate()}
              >
                {placeMutation.isPending ? (
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
          <div className="structure-manual-placement__field">
            <span className="structure-manual-placement__label">
              {t('manualPlacement.placeLabel')}
            </span>
            {placeLockedFromSchematic || groupTargets.length <= 1 ? (
              <p
                className="structure-manual-placement__place-locked"
                aria-label={t('manualPlacement.placeLockedAria', {
                  place: groupLabel,
                })}
              >
                {groupLabel}
              </p>
            ) : (
              <Select
                options={groupOptions}
                value={groupId}
                onChange={onGroupChange}
                placeholder={t('groupPlacement.groupPlaceholder')}
                aria-label={t('manualPlacement.placeLabel')}
                disabled={pending || groupOptions.length === 0}
              />
            )}
          </div>

          <div className="structure-manual-placement__body">
            {compositionEmpty ? (
              <EmptyState
                variant="idle"
                title={t('groupPlacement.emptyCompositionTitle')}
              >
                {t('groupPlacement.emptyComposition')}
              </EmptyState>
            ) : null}
            {!compositionEmpty &&
            teamChoices.length === 0 &&
            !modifyMode &&
            !awaitingHubRefresh ? (
              <EmptyState
                variant="idle"
                title={t('groupPlacement.allPlacedTitle')}
              >
                {t('groupPlacement.allPlaced')}
              </EmptyState>
            ) : null}
            {blockedEmpty}
            {showTeamSection ? (
              <div className="structure-manual-placement__field structure-manual-placement__field--fill">
                <div className="structure-manual-placement__section-head">
                  <span className="structure-manual-placement__label">
                    {t('manualPlacement.teamLabel')}
                  </span>
                  {canClearOccupant ? (
                    <div className="ds-icon-toolbar">
                      <Tooltip content={t('groupPlacement.clearOccupant')}>
                        <button
                          type="button"
                          className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact ds-icon-button--dismiss"
                          disabled={pending}
                          aria-label={t('groupPlacement.clearOccupant')}
                          onClick={() => setRemoveConfirmOpen(true)}
                        >
                          <CloseIcon size="sm" />
                        </button>
                      </Tooltip>
                    </div>
                  ) : null}
                </div>
                <div
                  className="structure-manual-placement__teams"
                  role="group"
                  aria-label={t('manualPlacement.teamLabel')}
                >
                  {teamChoices.map((team) => (
                    <ChoiceTile
                      key={team.entryId}
                      label={team.displayName}
                      description={
                        team.pot == null
                          ? undefined
                          : t('groupPlacement.pot', { pot: team.pot })
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
                      selected={
                        modifyMode
                          ? team.entryId === occupiedId
                          : selectedId === team.entryId
                      }
                      disabled={pending || modifyMode}
                      onChange={(next) => {
                        if (pending || modifyMode) return;
                        setSelectedId(next ? team.entryId : null);
                        placeMutation.reset();
                      }}
                    />
                  ))}
                </div>
              </div>
            ) : null}
          </div>
        </div>
      </Dialog>
      <ConfirmDialog
        open={removeConfirmOpen}
        title={t('groupPlacement.removeConfirmTitle')}
        message={t('groupPlacement.removeConfirm', {
          team: occupiedName,
          group: groupLabel,
        })}
        confirmLabel={t('groupPlacement.remove')}
        confirmPendingLabel={t('groupPlacement.removing')}
        confirmPending={removeMutation.isPending}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        danger
        onConfirm={() => removeMutation.mutate()}
        onCancel={() => {
          if (removeMutation.isPending) return;
          setRemoveConfirmOpen(false);
        }}
      />
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
