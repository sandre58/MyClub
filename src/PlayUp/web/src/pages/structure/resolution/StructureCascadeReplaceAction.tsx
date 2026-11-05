import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useCallback, useState, type ReactNode } from 'react';
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
import { CascadeReplaceContext } from './structureCascadeReplaceContext';
import { isReplayableStale } from './structureResolutionOccupants';

type StructureCascadeReplaceActionProps = {
  stage: StructureStageHubSummary | null;
  competitionId: string;
  children: ReactNode;
};

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

export function StructureCascadeReplaceAction({
  stage,
  competitionId,
  children,
}: StructureCascadeReplaceActionProps) {
  const { t } = useTranslation('structure');
  const queryClient = useQueryClient();
  const [occupant, setOccupant] = useState<StructureResolutionOccupant | null>(
    null,
  );
  const [played, setPlayed] = useState<CascadePlayedMatch[]>([]);
  const [versionToken, setVersionToken] = useState<string | null>(null);
  const stageId = stage?.stageId ?? '';

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

  const requestReplace = useCallback(
    (target: StructureResolutionOccupant) => {
      if (!stage || !isReplayableStale(target)) {
        return;
      }
      confirm.reset();
      preview.mutate(target);
    },
    [confirm, preview, stage],
  );

  const blocked = confirm.isError;

  return (
    <CascadeReplaceContext.Provider value={{ requestReplace }}>
      {preview.isError ? <MutationError error={preview.error} /> : null}
      {children}
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
    </CascadeReplaceContext.Provider>
  );
}
