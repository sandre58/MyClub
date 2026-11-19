// Manual group placement dialog. Cup placement stays in StructureManualPlacementDialog.

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ApiError,
  assignEntriesToGroup,
  assignEntryToGroup,
  clearEntryFromGroup,
  replaceEntryInGroup,
} from '../../../api';
import { ChoiceTile } from '../../../design-system/components/ChoiceTile';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Dialog } from '../../../design-system/components/Dialog';
import { Select } from '../../../design-system/components/Select';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  CheckIcon,
  EmptySelectionIcon,
  ListChecksIcon,
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
import { formatStructureMutationMessage } from '../problems/formatStructureMutationMessage';
import { scheduleStructureRefresh } from '../structureInvalidation';
import {
  canBatchAssignToGroup,
  canReplaceInGroup,
  findGroupSeatByEntryId,
  groupBatchCap,
  listAvailableGroupEntries,
  listGroupManualTargets,
  orderBatchSelection,
  purgeUnavailableSelection,
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
};

type DialogMode = 'batch' | 'singleEmpty' | 'edit';

function occupantId(place: SchematicCase | null): string | null {
  if (!place) return null;
  return place.entry?.entryId ?? place.assignment?.entryId ?? null;
}

function resolveDialogMode(place: SchematicCase | null): DialogMode {
  if (place == null) return 'batch';
  if (occupantId(place) == null) return 'singleEmpty';
  return 'edit';
}

export function StructureGroupPlacementDialog({
  open,
  onClose,
  competitionId,
  stage,
  structureStages,
  entries,
  cases,
  place: placeProp,
}: StructureGroupPlacementDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tReg } = useTranslation('regulation');
  const queryClient = useQueryClient();
  const capHintId = useId();
  const liveAnnouncerId = useId();
  const batchErrorId = useId();
  const actions = useMemo(() => stage.actions ?? [], [stage.actions]);
  const mode = resolveDialogMode(placeProp);
  const batchAllowed = canBatchAssignToGroup(actions, stage.placesPerGroup);
  const replaceAllowed = canReplaceInGroup(actions);

  const groupTargets = useMemo(
    () =>
      listGroupManualTargets(cases, actions, (name) =>
        t('place.group', { name }),
      ),
    [cases, actions, t],
  );

  const placeLockedFromSchematic =
    placeProp != null &&
    placeProp.formPosition.kind === 'GroupPlace' &&
    placeProp.formPosition.groupId?.trim() != null;

  const [groupId, setGroupId] = useState<string | null>(null);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [liveNote, setLiveNote] = useState<string | null>(null);
  const [seatBlocked, setSeatBlocked] = useState(false);
  const [placementBlocked, setPlacementBlocked] = useState(false);
  const [refusedEntryId, setRefusedEntryId] = useState<string | null>(null);
  /** Bumps on open / mode / group change so Dialog body focus is not reset on each toggle. */
  const [focusEpoch, setFocusEpoch] = useState(0);
  const initialSelectionRef = useRef<string[] | string | null>(null);
  const [editSubjectEntryId, setEditSubjectEntryId] = useState<string | null>(
    null,
  );
  const teamsListRef = useRef<HTMLDivElement>(null);
  const blockedFocusRef = useRef<HTMLDivElement>(null);

  const editGroupId =
    mode === 'edit' && placeProp?.formPosition.kind === 'GroupPlace'
      ? (placeProp.formPosition.groupId?.trim() ?? null)
      : null;

  const subjectSeat = useMemo(() => {
    if (mode !== 'edit' || !editGroupId || !editSubjectEntryId) return null;
    return findGroupSeatByEntryId(cases, editGroupId, editSubjectEntryId);
  }, [mode, editGroupId, editSubjectEntryId, cases]);

  const subjectGone =
    mode === 'edit' &&
    editSubjectEntryId != null &&
    subjectSeat == null &&
    cases.length > 0;

  const activePlace =
    mode === 'edit'
      ? (subjectSeat ?? placeProp)
      : placeLockedFromSchematic && placeProp?.formPosition.groupId === groupId
        ? placeProp
        : (groupTargets.find((target) => target.groupId === groupId)?.place ??
          (groupId ? resolveGroupManualPlace(cases, groupId, actions) : null));

  const occupiedId =
    mode === 'edit' ? (editSubjectEntryId ?? occupantId(activePlace)) : null;

  const available = useMemo(
    () => listAvailableGroupEntries(stage, cases, entries),
    [stage, cases, entries],
  );

  const cap = useMemo(
    () => (groupId ? groupBatchCap(cases, groupId, actions) : 0),
    [cases, groupId, actions],
  );

  const effectiveBatch = mode === 'batch' && batchAllowed;
  const multiSelect = effectiveBatch;
  const selection = effectiveBatch
    ? selectedIds
    : selectedId
      ? [selectedId]
      : [];

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

  const compositionEmpty = (stage.compositionEntryIds ?? []).length === 0;

  const refresh = () => {
    scheduleStructureRefresh(queryClient, competitionId, ['hubStage'], {
      stageId: stage.stageId,
    });
  };

  const saveMutation = useMutation({
    mutationFn: async () => {
      const targetGroup =
        groupId ?? activePlace?.formPosition.groupId?.trim() ?? '';
      if (!targetGroup) throw new Error('missing group');

      if (mode === 'edit') {
        const subjectId = editSubjectEntryId;
        if (!subjectId) throw new Error('missing subject');
        if (selectedId == null) {
          await clearEntryFromGroup(stage.stageId, targetGroup, subjectId);
          return;
        }
        if (selectedId === subjectId) throw new Error('unchanged');
        if (!replaceAllowed) throw new Error('replace not allowed');
        await replaceEntryInGroup(
          stage.stageId,
          targetGroup,
          subjectId,
          selectedId,
        );
        return;
      }

      if (mode === 'singleEmpty' || !effectiveBatch) {
        if (!selectedId) throw new Error('missing selection');
        await assignEntryToGroup(stage.stageId, targetGroup, selectedId);
        return;
      }

      const ordered = orderBatchSelection(available, selectedIds);
      if (ordered.length === 0) throw new Error('missing selection');
      await assignEntriesToGroup(stage.stageId, targetGroup, ordered);
    },
    onSuccess: () => {
      refresh();
      onClose();
    },
    onError: (error) => {
      refresh();
      if (error instanceof ApiError && error.entryId) {
        setRefusedEntryId(error.entryId);
      }
      if (error instanceof ApiError) {
        const blockCodes = [
          'Stage.StructureLocked',
          'Application.ManualGroupPlacementBlockedByActiveDraw',
          'Stage.GroupNotFound',
        ];
        if (error.code && blockCodes.includes(error.code)) {
          setPlacementBlocked(true);
        }
        if (mode === 'edit' && error.code === 'Stage.EntryNotFound') {
          setLiveNote(t('groupPlacement.seatChanged'));
        }
      }
    },
  });

  useClearMutationsOnOpen(open, saveMutation);

  useEffect(() => {
    if (!open) return;
    const initialGroup =
      placeProp?.formPosition.kind === 'GroupPlace'
        ? (placeProp.formPosition.groupId?.trim() ?? null)
        : (groupTargets[0]?.groupId ?? null);
    setGroupId(initialGroup);
    setSeatBlocked(false);
    setPlacementBlocked(false);
    setRefusedEntryId(null);
    setLiveNote(null);

    if (mode === 'edit') {
      const subjectId = occupantId(placeProp);
      setEditSubjectEntryId(subjectId);
      setSelectedId(subjectId);
      initialSelectionRef.current = subjectId;
    } else if (mode === 'singleEmpty') {
      setSelectedId(null);
      initialSelectionRef.current = null;
    } else if (effectiveBatch) {
      setSelectedIds([]);
      initialSelectionRef.current = [];
    } else if (mode === 'batch') {
      setSelectedId(null);
      initialSelectionRef.current = null;
    }
    saveMutation.reset();
    setFocusEpoch((value) => value + 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- open edge
  }, [open, placeProp, mode]);

  useEffect(() => {
    if (!open || placeProp != null || groupId != null) return;
    const next = groupTargets[0]?.groupId?.trim() ?? null;
    if (next) {
      setGroupId(next);
    }
  }, [open, placeProp, groupId, groupTargets]);

  useEffect(() => {
    if (!open || mode !== 'edit') return;
    if (subjectGone) {
      setLiveNote(t('groupPlacement.seatChanged'));
      setSeatBlocked(true);
      return;
    }
    setSeatBlocked(
      subjectSeat != null &&
        (subjectSeat.seatMode !== 'Editable' || subjectSeat.canClear !== true),
    );
    setLiveNote((current) =>
      current === t('groupPlacement.seatChanged') ? null : current,
    );
  }, [open, mode, subjectGone, subjectSeat, t]);

  useEffect(() => {
    if (!open || mode !== 'batch') return;
    const { ids, removedCount } = purgeUnavailableSelection(
      selectedIds,
      available,
    );
    if (removedCount > 0) {
      setSelectedIds(ids);
      setLiveNote(t('groupPlacement.selectionPurged', { count: removedCount }));
    }
  }, [open, mode, available, selectedIds, t]);

  useEffect(() => {
    if (!open || placeProp != null || !saveMutation.error) return;
    if (groupTargets.length === 0) {
      setPlacementBlocked(true);
    }
  }, [open, placeProp, saveMutation.error, groupTargets.length]);

  const groupLabel =
    groupId != null
      ? resolveGroupDisplayLabel(cases, groupId, (name) =>
          t('place.group', { name }),
        )
      : '';

  const groupOptions = useMemo(
    () =>
      groupTargets.map((target) => {
        const free = groupBatchCap(cases, target.groupId, actions);
        return {
          value: target.groupId,
          label: t('groupPlacement.groupOption', {
            group: target.label,
            count: free,
          }),
        };
      }),
    [groupTargets, cases, actions, t],
  );

  const overCap = effectiveBatch && selection.length > cap;
  const atCap = effectiveBatch && cap > 0 && selection.length >= cap;
  const showPlacementBlocked = seatBlocked || placementBlocked;

  const dirty =
    mode === 'edit'
      ? (selectedId ?? null) !== (initialSelectionRef.current ?? null)
      : multiSelect
        ? selectedIds.length > 0
        : selectedId != null;

  const {
    requestClose: requestDiscardClose,
    discardOpen,
    confirmDiscard,
    cancelDiscard,
  } = useDiscardConfirm(dirty, onClose);

  function requestClose() {
    requestDiscardClose(saveMutation.isPending);
  }

  function onClearSeatSelection() {
    setSelectedId(null);
    setLiveNote(t('groupPlacement.selectionCleared'));
    saveMutation.reset();
    window.requestAnimationFrame(() => {
      if (showPlacementBlocked) {
        blockedFocusRef.current?.focus();
        return;
      }
      const root = teamsListRef.current;
      const tile = root?.querySelector<HTMLElement>(
        'button[role="radio"], input[type="radio"]',
      );
      if (tile) {
        tile.focus();
        return;
      }
      root?.focus();
    });
  }

  const onGroupChange = (next: string | null) => {
    if (placeLockedFromSchematic) return;
    setGroupId(next);
    if (effectiveBatch) {
      setSelectedIds([]);
    } else {
      setSelectedId(null);
    }
    setRefusedEntryId(null);
    saveMutation.reset();
    setFocusEpoch((value) => value + 1);
  };

  const pending = saveMutation.isPending;
  const mutationError = saveMutation.error;

  const batchErrorMessage =
    mutationError instanceof ApiError && mutationError.entryId
      ? (() => {
          const team =
            teamChoices.find((row) => row.entryId === mutationError.entryId)
              ?.displayName ?? mutationError.entryId;
          const message = formatStructureMutationMessage(
            t,
            mutationError,
            structureStages,
          );
          return t('groupPlacement.batchRefused', { team, message });
        })()
      : null;

  const canSaveEdit =
    mode === 'edit' &&
    !seatBlocked &&
    !subjectGone &&
    !compositionEmpty &&
    ((selectedId == null && editSubjectEntryId != null) ||
      (selectedId != null &&
        selectedId !== editSubjectEntryId &&
        replaceAllowed));

  const canSaveSingle =
    (mode === 'singleEmpty' || (mode === 'batch' && !multiSelect)) &&
    selectedId != null &&
    !compositionEmpty;

  const canSaveBatch =
    effectiveBatch &&
    selectedIds.length > 0 &&
    selectedIds.length <= cap &&
    !overCap &&
    !compositionEmpty;

  const canSave = canSaveEdit || canSaveSingle || canSaveBatch;

  const dialogTitle =
    mode === 'edit'
      ? t('groupPlacement.editTitle', { group: groupLabel })
      : t('manualPlacement.title', { phase: stage.name });

  const primaryLabel =
    mode === 'edit' || mode === 'singleEmpty' || !multiSelect
      ? t('manualPlacement.save')
      : t('groupPlacement.saveBatch', { count: selectedIds.length });

  const fillRemaining = Math.max(0, cap - selectedIds.length);
  const canFillGroup =
    effectiveBatch && fillRemaining > 0 && teamChoices.length > 0 && !atCap;

  const fillGroup = () => {
    const next = new Set(selectedIds);
    for (const team of teamChoices) {
      if (next.size >= cap) break;
      next.add(team.entryId);
    }
    setSelectedIds([...next]);
    saveMutation.reset();
  };

  const toggleBatch = (entryId: string, next: boolean) => {
    setSelectedIds((current) => {
      if (next) {
        if (current.includes(entryId) || current.length >= cap) return current;
        return [...current, entryId];
      }
      return current.filter((id) => id !== entryId);
    });
    setRefusedEntryId(null);
    saveMutation.reset();
  };

  const dialogOpen =
    open &&
    groupId != null &&
    (placeProp != null || groupTargets.length > 0 || placementBlocked);

  const tileRole = multiSelect ? 'checkbox' : 'radio';
  const teamGroupRole = tileRole === 'radio' ? 'radiogroup' : 'group';

  return (
    <>
      <Dialog
        open={dialogOpen}
        onClose={requestClose}
        title={dialogTitle}
        description={t('groupPlacement.placeLead')}
        size="md"
        closeLabel={tCommon('close')}
        closeDisabled={pending || discardOpen}
        trapFocus={!discardOpen}
        focusKey={`${mode}:${groupId ?? ''}:${focusEpoch}`}
        footerStatus={
          mutationError ? (
            batchErrorMessage ? (
              <div
                id={batchErrorId}
                role="alert"
                className="ds-alert ds-alert--danger"
              >
                <span>{batchErrorMessage}</span>
              </div>
            ) : (
              <MutationError
                error={mutationError}
                structureStages={structureStages}
              />
            )
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
            {!showPlacementBlocked &&
            !compositionEmpty &&
            (mode === 'edit' || teamChoices.length > 0) ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={pending || !canSave}
                onClick={() => saveMutation.mutate()}
              >
                {pending ? (
                  <PendingLabel>{t('manualPlacement.saving')}</PendingLabel>
                ) : (
                  <>
                    <CheckIcon size="sm" />
                    {primaryLabel}
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

          <div className="structure-manual-placement__field">
            <span className="structure-manual-placement__label">
              {t('groupPlacement.targetLabel')}
            </span>
            {placeLockedFromSchematic || groupTargets.length <= 1 ? (
              <p
                className="structure-manual-placement__place-locked"
                aria-label={t('groupPlacement.targetLockedAria', {
                  group:
                    mode === 'batch' && groupId
                      ? t('groupPlacement.groupOption', {
                          group: groupLabel,
                          count: cap,
                        })
                      : groupLabel,
                })}
              >
                {mode === 'batch' && groupId
                  ? t('groupPlacement.groupOption', {
                      group: groupLabel,
                      count: cap,
                    })
                  : groupLabel}
              </p>
            ) : (
              <Select
                options={groupOptions}
                value={groupId}
                onChange={onGroupChange}
                placeholder={t('groupPlacement.groupPlaceholder')}
                aria-label={t('groupPlacement.targetLabel')}
                disabled={pending || groupOptions.length === 0}
              />
            )}
          </div>

          {effectiveBatch ? (
            <div
              className="structure-qualification__facts structure-composition__facts"
              aria-live="polite"
            >
              <span
                className="structure-qualification__fact structure-qualification__fact--secondary"
                data-fact="free"
              >
                <span className="structure-qualification__fact-value">
                  {cap}
                </span>
                <span className="structure-qualification__fact-label">
                  {t('groupPlacement.factFree')}
                </span>
              </span>
              <span
                className="structure-qualification__fact-rule"
                aria-hidden="true"
              />
              <span
                className="structure-qualification__fact structure-qualification__fact--primary"
                data-fact="selected"
              >
                <span className="structure-qualification__fact-value">
                  {selectedIds.length}
                </span>
                <span className="structure-qualification__fact-label">
                  {t('groupPlacement.factSelected')}
                </span>
              </span>
              <span
                className="structure-qualification__fact-rule"
                aria-hidden="true"
              />
              <span
                className="structure-qualification__fact structure-qualification__fact--secondary"
                data-fact="remaining"
              >
                <span className="structure-qualification__fact-value">
                  {Math.max(0, cap - selectedIds.length)}
                </span>
                <span className="structure-qualification__fact-label">
                  {t('groupPlacement.factRemaining')}
                </span>
              </span>
            </div>
          ) : null}

          {atCap ? (
            <p id={capHintId} className="ds-visually-hidden">
              {t('groupPlacement.capReached', { group: groupLabel })}
            </p>
          ) : null}

          {overCap ? (
            <p className="structure-manual-placement__hint" role="status">
              {t('groupPlacement.overCap', {
                count: selection.length - cap,
              })}
            </p>
          ) : null}

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
            mode !== 'edit' ? (
              <EmptyState
                variant="idle"
                title={t('groupPlacement.allPlacedTitle')}
              >
                {t('groupPlacement.allPlaced')}
              </EmptyState>
            ) : null}
            {showPlacementBlocked ? (
              <div ref={blockedFocusRef} tabIndex={-1}>
                <EmptyState
                  variant="idle"
                  icon={<EmptySelectionIcon size="lg" />}
                  title={t('groupPlacement.blockedAfterRemoveTitle')}
                >
                  {t('groupPlacement.blockedAfterRemove')}
                </EmptyState>
              </div>
            ) : null}
            {!compositionEmpty &&
            !showPlacementBlocked &&
            teamChoices.length > 0 ? (
              <div className="structure-manual-placement__field structure-manual-placement__field--fill">
                <div className="structure-manual-placement__section-head">
                  <span className="structure-manual-placement__label">
                    {multiSelect
                      ? t('groupPlacement.teamLabel')
                      : t('manualPlacement.teamLabel')}
                  </span>
                  <div className="ds-icon-toolbar">
                    {effectiveBatch ? (
                      <button
                        type="button"
                        className="ds-btn ds-btn--ghost structure-composition__select-all"
                        disabled={!canFillGroup || pending}
                        onClick={fillGroup}
                      >
                        <ListChecksIcon size="sm" />
                        {t('groupPlacement.fillGroup')}
                      </button>
                    ) : null}
                    {mode === 'edit' && selectedId != null && !pending ? (
                      <Tooltip content={t('groupPlacement.clearSeat')}>
                        <button
                          type="button"
                          className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact ds-icon-button--dismiss"
                          disabled={pending}
                          aria-label={t('groupPlacement.clearSeat')}
                          onClick={onClearSeatSelection}
                        >
                          <CloseIcon size="sm" />
                        </button>
                      </Tooltip>
                    ) : null}
                  </div>
                </div>
                <div
                  ref={teamsListRef}
                  className="structure-manual-placement__teams ds-scroll-safe"
                  role={teamGroupRole}
                  aria-label={
                    multiSelect
                      ? t('groupPlacement.teamLabel')
                      : t('manualPlacement.teamLabel')
                  }
                >
                  {teamChoices.map((team) => {
                    const selected = multiSelect
                      ? selectedIds.includes(team.entryId)
                      : selectedId === team.entryId;
                    const lockedAtCap =
                      effectiveBatch &&
                      !selected &&
                      (atCap || selectedIds.length >= cap);
                    const tileDisabled = pending;
                    return (
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
                        role={tileRole}
                        selected={selected}
                        disabled={tileDisabled}
                        unavailable={lockedAtCap}
                        aria-describedby={
                          refusedEntryId === team.entryId && batchErrorMessage
                            ? batchErrorId
                            : lockedAtCap
                              ? capHintId
                              : undefined
                        }
                        onChange={(next) => {
                          if (pending) return;
                          if (multiSelect) {
                            toggleBatch(team.entryId, next);
                            return;
                          }
                          if (next) {
                            setSelectedId(team.entryId);
                            saveMutation.reset();
                          }
                        }}
                      />
                    );
                  })}
                </div>
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
