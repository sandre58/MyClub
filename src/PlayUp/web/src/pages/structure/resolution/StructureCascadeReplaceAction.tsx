import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import {
  confirmCascadeReplace,
  previewCascadeReplace,
  type CascadePlayedMatch,
  type CascadeReplaceRequest,
} from '../../../api';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import type {
  StructureResolutionOccupant,
  StructureStageHubSummary,
} from '../../../types';
import { MutationError } from '../../../ui';
import { scheduleStructureRefresh } from '../structureInvalidation';

type StructureCascadeReplaceActionProps = {
  stage: StructureStageHubSummary;
  competitionId: string;
};

function isReplayableStale(occupant: StructureResolutionOccupant): boolean {
  return (
    occupant.state === 'Perime' &&
    (occupant.provenanceKind === 'Qualification' ||
      occupant.provenanceKind === 'Progression')
  );
}

function toRequest(
  occupant: StructureResolutionOccupant,
): CascadeReplaceRequest {
  return {
    target: occupant.target,
    entryId: occupant.entryId,
    slotKey: occupant.slotKey ?? null,
    groupId: occupant.groupId ?? null,
  };
}

function placeLabel(occupant: StructureResolutionOccupant): string {
  if (occupant.slotKey) {
    return occupant.slotKey;
  }
  if (occupant.groupId) {
    return occupant.groupId;
  }
  return occupant.entryId;
}

export function StructureCascadeReplaceAction({
  stage,
  competitionId,
}: StructureCascadeReplaceActionProps) {
  const { t } = useTranslation('structure');
  const queryClient = useQueryClient();
  const [occupant, setOccupant] = useState<StructureResolutionOccupant | null>(
    null,
  );
  const [played, setPlayed] = useState<CascadePlayedMatch[]>([]);
  const [versionToken, setVersionToken] = useState<string | null>(null);
  const stageId = stage.stageId;
  const stale = (stage.resolutionOccupants ?? []).filter(isReplayableStale);

  const preview = useMutation({
    mutationFn: (target: StructureResolutionOccupant) =>
      previewCascadeReplace(stageId, toRequest(target)),
    onSuccess: (result, target) => {
      setOccupant(target);
      setPlayed(result.playedMatches);
      setVersionToken(result.versionToken);
    },
  });

  const confirm = useMutation({
    mutationFn: () => {
      if (!occupant || !versionToken) {
        throw new Error('Cascade preview is missing.');
      }
      return confirmCascadeReplace(stageId, {
        ...toRequest(occupant),
        versionToken,
      });
    },
    onSuccess: () => {
      setOccupant(null);
      setVersionToken(null);
      scheduleStructureRefresh(
        queryClient,
        competitionId,
        ['graph', 'hubStageShell'],
        {
          stageId,
        },
      );
    },
  });

  if (stale.length === 0) {
    return null;
  }

  const blocked = confirm.isError;

  return (
    <div>
      {stale.map((target) => (
        <button
          key={`${target.target}-${target.slotKey ?? ''}-${target.groupId ?? ''}-${target.entryId}`}
          type="button"
          className="ds-btn ds-btn--secondary"
          onClick={() => {
            confirm.reset();
            preview.mutate(target);
          }}
        >
          {t('resolution.cascade.actionNamed', { place: placeLabel(target) })}
        </button>
      ))}
      {preview.isError ? <MutationError error={preview.error} /> : null}
      <ConfirmDialog
        open={occupant !== null && versionToken !== null}
        title={t('resolution.cascade.confirmTitle')}
        message={
          played.length === 0 ? (
            <p>{t('resolution.cascade.nonePlayed')}</p>
          ) : (
            <div>
              <p>{t('resolution.cascade.playedIntro')}</p>
              <ul>
                {played.map((match) => (
                  <li key={match.matchId}>
                    {t('resolution.cascade.playedMatch', {
                      matchId: match.matchId,
                    })}
                  </li>
                ))}
              </ul>
            </div>
          )
        }
        confirmLabel={t('resolution.cascade.confirm')}
        cancelLabel={t('resolution.cascade.cancel')}
        confirmPending={confirm.isPending}
        confirmPendingLabel={t('resolution.cascade.pending')}
        confirmDisabled={blocked}
        danger={played.length > 0}
        footerStatus={blocked ? <MutationError error={confirm.error} /> : null}
        onConfirm={() => confirm.mutate()}
        onCancel={() => {
          if (!confirm.isPending) {
            setOccupant(null);
            setVersionToken(null);
          }
        }}
      />
    </div>
  );
}
