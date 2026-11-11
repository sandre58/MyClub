// Manual group placement dialog. Cup placement stays in StructureManualPlacementDialog.

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { assignEntryToGroup, clearEntryFromGroup } from '../../../api';
import { ChoiceTile } from '../../../design-system/components/ChoiceTile';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Dialog } from '../../../design-system/components/Dialog';
import { TeamCrest } from '../../../design-system/TeamCrest';
import { CloseIcon } from '../../../design-system/icons/shellIcons';
import { useClearMutationsOnOpen } from '../../../design-system/useClearMutationsOnOpen';
import { useDiscardConfirm } from '../../../design-system/useDiscardConfirm';
import type {
  SchematicCase,
  StructureEntry,
  StructureStageHubSummary,
} from '../../../types';
import { EmptyState, MutationError, PendingLabel } from '../../../ui';
import { scheduleStructureRefresh } from '../structureInvalidation';
import { listAvailableGroupEntries } from './manualGroupPlacementUi';

type StructureGroupPlacementDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  stage: StructureStageHubSummary;
  structureStages: readonly StructureStageHubSummary[];
  entries: StructureEntry[];
  cases: SchematicCase[];
  place: SchematicCase | null;
  intent: 'place' | 'remove';
};

function occupantId(place: SchematicCase | null): string | null {
  return place?.entry?.entryId ?? place?.assignment?.entryId ?? null;
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
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [removeConfirmOpen, setRemoveConfirmOpen] = useState(false);

  const groupId = place?.formPosition.groupId ?? '';
  const groupLabel = t('place.group', {
    name: place?.formPosition.groupName ?? '',
  });
  const available = useMemo(
    () => listAvailableGroupEntries(stage, cases, entries),
    [stage, cases, entries],
  );
  const compositionEmpty = (stage.compositionEntryIds ?? []).length === 0;
  const occupiedId = occupantId(place);
  const occupiedEntry = entries.find((entry) => entry.entryId === occupiedId);
  const occupiedName =
    place?.assignment?.displayName?.trim() ||
    place?.entry?.displayName?.trim() ||
    occupiedEntry?.displayName?.trim() ||
    '';
  const occupiedPot =
    stage.compositionEntryPots?.find((row) => row.entryId === occupiedId)
      ?.pot ?? null;
  const dirty = intent === 'place' && selectedId != null;

  const refresh = () => {
    scheduleStructureRefresh(queryClient, competitionId, ['hubStage'], {
      stageId: stage.stageId,
    });
  };

  const placeMutation = useMutation({
    mutationFn: async () => {
      if (!groupId || !selectedId) {
        throw new Error('missing selection');
      }
      await assignEntryToGroup(stage.stageId, groupId, selectedId);
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
      if (!groupId || !occupiedId) {
        throw new Error('missing selection');
      }
      await clearEntryFromGroup(stage.stageId, groupId, occupiedId);
    },
    onSuccess: () => {
      setRemoveConfirmOpen(false);
      refresh();
      onClose();
    },
    onError: () => {
      setRemoveConfirmOpen(false);
      refresh();
    },
  });

  useClearMutationsOnOpen(open, placeMutation, removeMutation);

  const pending = placeMutation.isPending || removeMutation.isPending;
  const mutationError = placeMutation.error ?? removeMutation.error;
  const {
    requestClose: requestDiscardClose,
    discardOpen,
    confirmDiscard,
    cancelDiscard,
  } = useDiscardConfirm(dirty, onClose);

  function requestClose() {
    requestDiscardClose(pending);
  }

  const canSave = intent === 'place' && selectedId != null && !compositionEmpty;
  const showPlaceList =
    intent === 'place' && !compositionEmpty && available.length > 0;

  return (
    <>
      <Dialog
        open={open && place != null}
        onClose={requestClose}
        title={t(
          intent === 'remove'
            ? 'groupPlacement.removeTitle'
            : 'groupPlacement.title',
          { group: groupLabel },
        )}
        description={t(
          intent === 'remove'
            ? 'groupPlacement.removeLead'
            : 'groupPlacement.placeLead',
        )}
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
            {showPlaceList ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={pending || !canSave}
                onClick={() => placeMutation.mutate()}
              >
                {placeMutation.isPending ? (
                  <PendingLabel>{t('groupPlacement.saving')}</PendingLabel>
                ) : (
                  t('groupPlacement.save')
                )}
              </button>
            ) : null}
            {intent === 'remove' && occupiedId ? (
              <button
                type="button"
                className="ds-btn ds-btn--destructive"
                disabled={pending || removeConfirmOpen}
                onClick={() => setRemoveConfirmOpen(true)}
              >
                {removeMutation.isPending ? (
                  <PendingLabel>{t('groupPlacement.removing')}</PendingLabel>
                ) : (
                  t('groupPlacement.remove')
                )}
              </button>
            ) : null}
          </>
        }
      >
        <div className="structure-manual-placement">
          {intent === 'place' && compositionEmpty ? (
            <EmptyState
              variant="idle"
              title={t('groupPlacement.emptyCompositionTitle')}
            >
              {t('groupPlacement.emptyComposition')}
            </EmptyState>
          ) : null}
          {intent === 'place' && !compositionEmpty && available.length === 0 ? (
            <EmptyState
              variant="idle"
              title={t('groupPlacement.allPlacedTitle')}
            >
              {t('groupPlacement.allPlaced')}
            </EmptyState>
          ) : null}
          {showPlaceList ? (
            <div
              className="structure-manual-placement__teams"
              role="group"
              aria-label={t('groupPlacement.teamLabel')}
            >
              {available.map((team) => (
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
                  selected={selectedId === team.entryId}
                  disabled={pending}
                  onChange={(next) => {
                    if (pending) return;
                    setSelectedId(next ? team.entryId : null);
                    placeMutation.reset();
                  }}
                />
              ))}
            </div>
          ) : null}
          {intent === 'remove' && occupiedId ? (
            <div className="structure-manual-placement__place-locked">
              <TeamCrest
                name={occupiedName}
                logoMediaId={
                  place?.assignment?.logoMediaId ?? occupiedEntry?.logoMediaId
                }
                primaryColor={
                  place?.assignment?.primaryColor ?? occupiedEntry?.primaryColor
                }
                size="sm"
              />
              <span>{occupiedName}</span>
              {occupiedPot != null ? (
                <span>{t('groupPlacement.pot', { pot: occupiedPot })}</span>
              ) : null}
              {place?.excess ? <span>{t('groupPlacement.excess')}</span> : null}
              {place?.outsideComposition ? (
                <span>{t('groupPlacement.outsideComposition')}</span>
              ) : null}
            </div>
          ) : null}
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
