// Manual group placement dialog. Cup placement stays in StructureManualPlacementDialog.

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ApiError,
  assignEntryToGroup,
  clearEntryFromGroup,
  replaceEntryInGroup,
  setGroupEntries,
} from '../../../api';
import { ChoiceTile } from '../../../design-system/components/ChoiceTile';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Dialog } from '../../../design-system/components/Dialog';
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
import { StructureManualPlacementSectionHead } from './StructureManualPlacementSectionHead';
import {
  buildSetGroupEntriesPayload,
  canReplaceInGroup,
  canSetGroupMembership,
  findGroupSeatByEntryId,
  groupBatchCap,
  groupDialogAdditionsOnly,
  listAvailableGroupEntries,
  listGroupMemberEntries,
  listOtherGroupEntries,
  purgeUnavailableSelection,
  batchSelectionExceedsCapacity,
  memberEntryClearable,
  membershipSetsEqual,
  reseedMembershipSelection,
  listGroupSelectTargets,
  membershipAdditionLimit,
  resolveGroupDisplayLabel,
  entryGroupPlacementHint,
  type GroupStatusSuffix,
  type OtherGroupPlacementChoice,
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
  /** Target group when opened from the operational menu (batch). */
  fixedGroupId?: string | null;
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

function formatGroupPickerLabel(
  t: (key: string, options?: Record<string, unknown>) => string,
  baseLabel: string,
  suffix: GroupStatusSuffix,
): string {
  if (!suffix) {
    return baseLabel;
  }
  const suffixKey = {
    complete: 'groupPlacement.groupSuffixComplete',
    reserved: 'groupPlacement.groupSuffixReserved',
    match: 'groupPlacement.groupSuffixMatch',
    draw: 'groupPlacement.groupSuffixDraw',
  }[suffix];
  return t('groupPlacement.groupNameWithSuffix', {
    group: baseLabel,
    suffix: t(suffixKey),
  });
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
  fixedGroupId = null,
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
  const membershipBatchAllowed = canSetGroupMembership(
    actions,
    stage.placesPerGroup,
  );
  const replaceAllowed = canReplaceInGroup(actions);

  const labelForGroup = useMemo(
    () => (name: string) => t('place.group', { name }),
    [t],
  );

  const groupSelectTargets = useMemo(
    () =>
      listGroupSelectTargets(cases, stage.placesPerGroup ?? 0, labelForGroup),
    [cases, stage.placesPerGroup, labelForGroup],
  );

  const [groupId, setGroupId] = useState<string | null>(null);
  const [expectedEntryIds, setExpectedEntryIds] = useState<string[]>([]);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [liveNote, setLiveNote] = useState<string | null>(null);
  const [seatBlocked, setSeatBlocked] = useState(false);
  const [placementBlocked, setPlacementBlocked] = useState(false);
  const [refusedEntryId, setRefusedEntryId] = useState<string | null>(null);
  /** Bumps on open / mode / group change so Dialog body focus is not reset on each toggle. */
  const [focusEpoch, setFocusEpoch] = useState(0);
  const initialSelectionRef = useRef<string[] | string | null>(null);
  const openSessionRef = useRef(false);
  const membershipSeededRef = useRef(false);
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

  const activePlace = mode === 'edit' ? (subjectSeat ?? placeProp) : null;

  const occupiedId =
    mode === 'edit' ? (editSubjectEntryId ?? occupantId(activePlace)) : null;

  const available = useMemo(
    () => listAvailableGroupEntries(stage, cases, entries),
    [stage, cases, entries],
  );

  const effectiveBatch = mode === 'batch' && membershipBatchAllowed;

  const emptySeatCap = useMemo(
    () => (groupId ? groupBatchCap(cases, groupId, actions) : 0),
    [cases, groupId, actions],
  );

  const additionsOnly =
    effectiveBatch && groupId
      ? groupDialogAdditionsOnly(cases, groupId, stage.placesPerGroup ?? 0)
      : false;
  const memberEntries = useMemo(() => {
    if (!effectiveBatch || !groupId) return [];
    return listGroupMemberEntries(stage, cases, groupId, entries);
  }, [effectiveBatch, groupId, stage, cases, entries]);

  const otherGroupEntries = useMemo(() => {
    if (!effectiveBatch || !groupId) return [];
    return listOtherGroupEntries(stage, cases, groupId, entries, labelForGroup);
  }, [effectiveBatch, groupId, stage, cases, entries, labelForGroup]);

  const batchAdditionLimit = useMemo(() => {
    if (!effectiveBatch || !groupId) {
      return emptySeatCap;
    }
    return membershipAdditionLimit(
      emptySeatCap,
      cases,
      groupId,
      expectedEntryIds,
      selectedIds,
      additionsOnly,
    );
  }, [
    effectiveBatch,
    groupId,
    emptySeatCap,
    cases,
    expectedEntryIds,
    selectedIds,
    additionsOnly,
  ]);
  const cap = batchAdditionLimit;

  const multiSelect = effectiveBatch;

  const teamChoices = useMemo(() => {
    if (effectiveBatch) {
      return available;
    }
    const choices = [...available];
    for (const anchorId of [occupiedId, selectedId]) {
      if (!anchorId || choices.some((row) => row.entryId === anchorId)) {
        continue;
      }
      const entry = entries.find((row) => row.entryId === anchorId);
      const occupiedName =
        activePlace?.assignment?.displayName?.trim() ||
        activePlace?.entry?.displayName?.trim() ||
        entry?.displayName?.trim() ||
        anchorId;
      const pot =
        stage.compositionEntryPots?.find((row) => row.entryId === anchorId)
          ?.pot ?? null;
      choices.unshift({
        entryId: anchorId,
        displayName:
          entry?.displayName?.trim() ||
          entry?.shortName?.trim() ||
          occupiedName,
        logoMediaId:
          activePlace?.assignment?.logoMediaId ?? entry?.logoMediaId ?? null,
        primaryColor:
          activePlace?.assignment?.primaryColor ?? entry?.primaryColor ?? null,
        pot,
      });
    }
    return choices;
  }, [
    effectiveBatch,
    available,
    occupiedId,
    selectedId,
    activePlace,
    entries,
    stage.compositionEntryPots,
  ]);

  const compositionEmpty = (stage.compositionEntryIds ?? []).length === 0;

  const batchPlacementRows = useMemo(() => {
    if (!effectiveBatch) {
      return [];
    }
    type BatchPlacementRow =
      | { kind: 'member'; team: (typeof memberEntries)[number] }
      | { kind: 'available'; team: (typeof available)[number] }
      | { kind: 'other'; team: OtherGroupPlacementChoice };
    const rows: BatchPlacementRow[] = [
      ...memberEntries.map((team) => ({ kind: 'member' as const, team })),
      ...available.map((team) => ({ kind: 'available' as const, team })),
      ...otherGroupEntries.map((team) => ({ kind: 'other' as const, team })),
    ];
    rows.sort((left, right) =>
      left.team.displayName.localeCompare(right.team.displayName, undefined, {
        numeric: true,
        sensitivity: 'base',
      }),
    );
    return rows;
  }, [effectiveBatch, memberEntries, available, otherGroupEntries]);

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

      const payload = buildSetGroupEntriesPayload(
        cases,
        targetGroup,
        selectedIds,
        available,
      );
      if (payload.length === 0 && expectedEntryIds.length === 0) {
        throw new Error('missing selection');
      }
      await setGroupEntries(
        stage.stageId,
        targetGroup,
        payload,
        expectedEntryIds,
      );
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
        if (
          error.code === 'Application.GroupMembershipStale' &&
          effectiveBatch
        ) {
          setLiveNote(t('groupPlacement.groupChanged'));
          saveMutation.reset();
          return;
        }
        const blockCodes = [
          'Stage.StructureLocked',
          'Application.ManualGroupPlacementBlockedByActiveDraw',
          'Stage.GroupNotFound',
        ];
        if (error.code && blockCodes.includes(error.code)) {
          setPlacementBlocked(true);
          setFocusEpoch((value) => value + 1);
        }
        if (mode === 'edit' && error.code === 'Stage.EntryNotFound') {
          setLiveNote(t('groupPlacement.seatChanged'));
        }
      }
    },
  });

  useClearMutationsOnOpen(open, saveMutation);

  useEffect(() => {
    if (!open) {
      openSessionRef.current = false;
      membershipSeededRef.current = false;
      return;
    }
    if (openSessionRef.current) {
      return;
    }
    openSessionRef.current = true;
    const initialGroup =
      placeProp?.formPosition.kind === 'GroupPlace'
        ? (placeProp.formPosition.groupId?.trim() ?? null)
        : (fixedGroupId?.trim() ?? null);
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
    } else if (effectiveBatch && initialGroup) {
      const members = listGroupMemberEntries(
        stage,
        cases,
        initialGroup,
        entries,
      ).map((row) => row.entryId);
      setExpectedEntryIds(members);
      setSelectedIds(members);
      initialSelectionRef.current = members;
      membershipSeededRef.current = true;
    } else if (mode === 'batch') {
      setSelectedId(null);
      initialSelectionRef.current = null;
    }
    saveMutation.reset();
    setFocusEpoch((value) => value + 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- open edge
  }, [open, placeProp, mode, fixedGroupId]);

  const hubMemberEntryIds = useMemo(() => {
    if (!groupId) {
      return [];
    }
    return listGroupMemberEntries(stage, cases, groupId, entries).map(
      (row) => row.entryId,
    );
  }, [groupId, stage, cases, entries]);
  const lastHubMembersRef = useRef<string[] | null>(null);

  useEffect(() => {
    if (!open || !effectiveBatch || !groupId || !membershipSeededRef.current) {
      lastHubMembersRef.current = null;
      return;
    }
    const previous = lastHubMembersRef.current;
    if (previous != null && membershipSetsEqual(hubMemberEntryIds, previous)) {
      return;
    }
    lastHubMembersRef.current = hubMemberEntryIds;
    if (previous == null) {
      return;
    }
    setExpectedEntryIds(hubMemberEntryIds);
    setSelectedIds((selection) =>
      reseedMembershipSelection(hubMemberEntryIds, selection, available),
    );
    setLiveNote(t('groupPlacement.groupChanged'));
  }, [open, effectiveBatch, groupId, hubMemberEntryIds, available, t]);

  useEffect(() => {
    if (!open || mode !== 'edit') return;
    if (subjectGone) {
      setLiveNote(t('groupPlacement.seatChanged'));
      setSeatBlocked(false);
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
    if (!open || mode !== 'edit' || !occupiedId) return;
    window.requestAnimationFrame(() => {
      const root = teamsListRef.current;
      const tile = root?.querySelector<HTMLElement>(
        'button[role="radio"][aria-checked="true"], input[type="radio"]:checked',
      );
      tile?.focus();
    });
  }, [open, mode, focusEpoch, occupiedId]);

  useEffect(() => {
    if (!open) return;
    const blocked = placementBlocked || (seatBlocked && !subjectGone);
    if (!blocked) return;
    let innerFrame = 0;
    const outerFrame = window.requestAnimationFrame(() => {
      innerFrame = window.requestAnimationFrame(() => {
        blockedFocusRef.current?.focus({ preventScroll: true });
      });
    });
    return () => {
      window.cancelAnimationFrame(outerFrame);
      window.cancelAnimationFrame(innerFrame);
    };
  }, [
    open,
    placementBlocked,
    seatBlocked,
    subjectGone,
    focusEpoch,
    cases.length,
  ]);

  useEffect(() => {
    if (!open || mode !== 'batch') return;
    if (effectiveBatch) {
      const conserved = selectedIds.filter((id) =>
        expectedEntryIds.includes(id),
      );
      const additions = selectedIds.filter(
        (id) => !expectedEntryIds.includes(id),
      );
      const { ids: nextAdds, removedCount } = purgeUnavailableSelection(
        additions,
        available,
      );
      if (removedCount > 0) {
        setSelectedIds([...conserved, ...nextAdds]);
        setLiveNote(
          t('groupPlacement.selectionPurged', { count: removedCount }),
        );
      }
      return;
    }
    const { ids, removedCount } = purgeUnavailableSelection(
      selectedIds,
      available,
    );
    if (removedCount > 0) {
      setSelectedIds(ids);
      setLiveNote(t('groupPlacement.selectionPurged', { count: removedCount }));
    }
  }, [open, mode, effectiveBatch, expectedEntryIds, available, selectedIds, t]);

  const groupLabel =
    groupId != null
      ? resolveGroupDisplayLabel(cases, groupId, (name) =>
          t('place.group', { name }),
        )
      : '';

  const lockedGroupLabel = useMemo(() => {
    if (groupId == null) return groupLabel;
    const target = groupSelectTargets.find((row) => row.groupId === groupId);
    const base = target?.label ?? groupLabel;
    return formatGroupPickerLabel(t, base, target?.suffix ?? null);
  }, [groupId, groupSelectTargets, groupLabel, t]);

  const additionsCount = effectiveBatch
    ? selectedIds.filter((id) => !expectedEntryIds.includes(id)).length
    : 0;
  const selectionOverCapacity =
    effectiveBatch &&
    batchSelectionExceedsCapacity(
      stage.placesPerGroup ?? 0,
      expectedEntryIds,
      selectedIds,
    );
  const overCap =
    effectiveBatch && (additionsCount > cap || selectionOverCapacity);
  const atCap = effectiveBatch && cap > 0 && additionsCount >= cap;
  const showPlacementBlocked =
    placementBlocked || (seatBlocked && !subjectGone);

  const dirty =
    mode === 'edit'
      ? (selectedId ?? null) !== (initialSelectionRef.current ?? null)
      : multiSelect
        ? !membershipSetsEqual(expectedEntryIds, selectedIds)
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

  const membershipDirty =
    effectiveBatch && !membershipSetsEqual(expectedEntryIds, selectedIds);

  const canSaveBatch =
    effectiveBatch && membershipDirty && !compositionEmpty && !overCap;

  const canSave = canSaveEdit || canSaveSingle || canSaveBatch;

  const placementTarget =
    groupLabel.trim() !== ''
      ? groupLabel
      : (lockedGroupLabel.split(' — ')[0]?.trim() ?? lockedGroupLabel);

  const dialogTitle = t('groupPlacement.placementTitle', {
    phase: stage.name,
    target: placementTarget,
  });

  const primaryLabel =
    mode === 'edit' || mode === 'singleEmpty' || !multiSelect
      ? t('manualPlacement.save')
      : t('groupPlacement.save');

  const fillRemaining = Math.max(0, batchAdditionLimit - additionsCount);
  const canFillGroup =
    effectiveBatch && fillRemaining > 0 && available.length > 0 && !atCap;

  const fillGroup = () => {
    if (additionsOnly && cap <= 0) return;
    const next = new Set(selectedIds);
    for (const team of available) {
      const projectedAdds = [...next].filter(
        (id) => !expectedEntryIds.includes(id),
      ).length;
      if (projectedAdds >= cap) break;
      if (!next.has(team.entryId)) {
        next.add(team.entryId);
      }
    }
    setSelectedIds([...next]);
    saveMutation.reset();
  };

  const clearDecheckableSelection = () => {
    if (additionsOnly || !groupId) return;
    const removableMembers = selectedIds.filter(
      (id) =>
        expectedEntryIds.includes(id) &&
        memberEntryClearable(cases, groupId, id),
    ).length;
    const hadAdditions = selectedIds.some(
      (id) => !expectedEntryIds.includes(id),
    );
    if (removableMembers === 0 && !hadAdditions) return;
    setSelectedIds((current) =>
      current.filter((id) => {
        if (!expectedEntryIds.includes(id)) {
          return false;
        }
        return !memberEntryClearable(cases, groupId, id);
      }),
    );
    if (removableMembers > 0) {
      setLiveNote(
        t('groupPlacement.willRemoveMembers', { count: removableMembers }),
      );
    }
    saveMutation.reset();
  };

  const canRemoveMembers =
    effectiveBatch &&
    !additionsOnly &&
    groupId != null &&
    selectedIds.some((id) =>
      !expectedEntryIds.includes(id)
        ? true
        : memberEntryClearable(cases, groupId, id),
    );

  const toggleBatch = (entryId: string, next: boolean) => {
    setSelectedIds((current) => {
      if (next) {
        if (current.includes(entryId)) {
          return current;
        }
        const isAddition = !expectedEntryIds.includes(entryId);
        const additions = current.filter(
          (id) => !expectedEntryIds.includes(id),
        ).length;
        if (isAddition && additions >= cap) {
          return current;
        }
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
    (placeProp != null || groupSelectTargets.length > 0 || placementBlocked);

  const placesPerGroup = stage.placesPerGroup ?? 0;
  const checkedTeamCount = effectiveBatch ? selectedIds.length : 0;
  const batchStatsTone =
    checkedTeamCount >= placesPerGroup && placesPerGroup > 0
      ? 'success'
      : 'attention';

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

          {effectiveBatch ? null : (
            <div className="structure-manual-placement__field">
              <span className="structure-manual-placement__label">
                {t('groupPlacement.targetLabel')}
              </span>
              <p
                className="structure-manual-placement__place-locked"
                aria-label={t('groupPlacement.targetLockedAria', {
                  group: lockedGroupLabel,
                })}
              >
                {lockedGroupLabel}
              </p>
            </div>
          )}

          {effectiveBatch ? (
            <>
              <div
                className="structure-qualification__facts structure-manual-placement__facts"
                data-tone={batchStatsTone}
                aria-live="polite"
              >
                <span
                  className="structure-qualification__fact structure-qualification__fact--secondary"
                  data-fact="places"
                >
                  <span className="structure-qualification__fact-value">
                    {placesPerGroup}
                  </span>
                  <span className="structure-qualification__fact-label">
                    {t('groupPlacement.factPlaces', { count: placesPerGroup })}
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
                    {checkedTeamCount}
                  </span>
                  <span className="structure-qualification__fact-label">
                    {t('groupPlacement.factOccupied', {
                      count: checkedTeamCount,
                    })}
                  </span>
                </span>
              </div>
            </>
          ) : null}

          {atCap ? (
            <p id={capHintId} className="ds-visually-hidden">
              {t('groupPlacement.capReached', { group: groupLabel })}
            </p>
          ) : null}

          {overCap ? (
            <p className="structure-manual-placement__hint" role="status">
              {t('groupPlacement.overCap', {
                count: additionsCount - cap,
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
            (teamChoices.length > 0 ||
              memberEntries.length > 0 ||
              otherGroupEntries.length > 0 ||
              mode === 'edit') ? (
              <div className="structure-manual-placement__field structure-manual-placement__field--fill">
                <StructureManualPlacementSectionHead
                  label={
                    multiSelect
                      ? t('groupPlacement.teamLabel')
                      : t('manualPlacement.teamLabel')
                  }
                  toolbar={
                    <>
                      {effectiveBatch ? (
                        <button
                          type="button"
                          className="ds-btn ds-btn--ghost ds-btn--sm structure-composition__select-all"
                          disabled={!canFillGroup || pending}
                          onClick={fillGroup}
                        >
                          <ListChecksIcon size="sm" />
                          {t('groupPlacement.fillGroup')}
                        </button>
                      ) : null}
                      {effectiveBatch || mode === 'edit' ? (
                        <button
                          type="button"
                          className="ds-btn ds-btn--ghost ds-btn--sm structure-composition__select-all"
                          disabled={
                            pending ||
                            (effectiveBatch
                              ? !canRemoveMembers
                              : selectedId == null)
                          }
                          onClick={
                            effectiveBatch
                              ? clearDecheckableSelection
                              : onClearSeatSelection
                          }
                        >
                          <CloseIcon size="sm" />
                          {t('groupPlacement.removeAll')}
                        </button>
                      ) : null}
                    </>
                  }
                />
                <div
                  ref={teamsListRef}
                  tabIndex={-1}
                  className="structure-manual-placement__teams ds-scroll-safe"
                  role={teamGroupRole}
                  aria-label={
                    multiSelect
                      ? t('groupPlacement.teamLabel')
                      : t('manualPlacement.teamLabel')
                  }
                >
                  {effectiveBatch
                    ? batchPlacementRows.map((row) => {
                        const team = row.team;
                        if (row.kind === 'other') {
                          return (
                            <ChoiceTile
                              key={`other-${team.entryId}`}
                              label={team.displayName}
                              description={
                                entryGroupPlacementHint(
                                  cases,
                                  team.entryId,
                                  t,
                                ) ?? undefined
                              }
                              layout="inline"
                              reserveSelectionMark
                              leading={
                                <TeamCrest
                                  name={team.displayName}
                                  logoMediaId={team.logoMediaId}
                                  primaryColor={team.primaryColor}
                                  size="sm"
                                />
                              }
                              role="checkbox"
                              selected={false}
                              disabled={pending}
                              unavailable
                              onChange={() => undefined}
                            />
                          );
                        }
                        const isMember = row.kind === 'member';
                        const clearable =
                          groupId != null &&
                          (!isMember ||
                            (memberEntryClearable(
                              cases,
                              groupId,
                              team.entryId,
                            ) &&
                              !additionsOnly));
                        const protectedMember = isMember && !clearable;
                        const selected = selectedIds.includes(team.entryId);
                        const lockedAtCap = !isMember && !selected && atCap;
                        const placementHint = isMember
                          ? entryGroupPlacementHint(cases, team.entryId, t)
                          : undefined;
                        return (
                          <ChoiceTile
                            key={team.entryId}
                            label={team.displayName}
                            description={placementHint ?? undefined}
                            layout="inline"
                            reserveSelectionMark
                            leading={
                              <TeamCrest
                                name={team.displayName}
                                logoMediaId={team.logoMediaId}
                                primaryColor={team.primaryColor}
                                size="sm"
                              />
                            }
                            role="checkbox"
                            selected={selected}
                            disabled={pending}
                            unavailable={protectedMember || lockedAtCap}
                            aria-describedby={
                              refusedEntryId === team.entryId &&
                              batchErrorMessage
                                ? batchErrorId
                                : lockedAtCap
                                  ? capHintId
                                  : undefined
                            }
                            onChange={(next) => {
                              if (pending || protectedMember) return;
                              toggleBatch(team.entryId, next);
                            }}
                          />
                        );
                      })
                    : teamChoices.map((team) => {
                        const selected = selectedId === team.entryId;
                        const placementHint = entryGroupPlacementHint(
                          cases,
                          team.entryId,
                          t,
                        );
                        return (
                          <ChoiceTile
                            key={team.entryId}
                            label={team.displayName}
                            description={placementHint ?? undefined}
                            layout="inline"
                            reserveSelectionMark
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
                            disabled={pending}
                            aria-describedby={
                              refusedEntryId === team.entryId &&
                              batchErrorMessage
                                ? batchErrorId
                                : undefined
                            }
                            onChange={(next) => {
                              if (pending) return;
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
