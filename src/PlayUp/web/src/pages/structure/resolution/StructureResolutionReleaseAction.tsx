import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useCallback, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import {
  confirmResolutionRelease,
  previewResolutionRelease,
  type ResolutionReleaseAffectedOccupant,
  type ResolutionReleaseDetachedMatch,
  type ResolutionReleaseRequest,
  type ResolutionReleaseResponse,
} from '../../../api';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { UnlockIcon } from '../../../design-system/icons/contentIcons';
import { ChevronRightIcon } from '../../../design-system/icons/shellIcons';
import type {
  StructureResolutionOccupant,
  StructureStageHubSummary,
} from '../../../types';
import { MutationError } from '../../../ui';
import { scheduleStructureRefresh } from '../structureInvalidation';
import { freshnessAction } from './freshnessGuidance';
import { ResolutionReleaseContext } from './structureResolutionReleaseContext';

type StructureResolutionReleaseActionProps = {
  stage: StructureStageHubSummary | null;
  competitionId: string;
  children: ReactNode;
};

function toRequest(
  occupant: StructureResolutionOccupant,
): ResolutionReleaseRequest {
  return {
    target: occupant.target,
    entryId: occupant.entryId,
    slotKey: occupant.slotKey ?? null,
    groupId: occupant.groupId ?? null,
  };
}

export function StructureResolutionReleaseAction({
  stage,
  competitionId,
  children,
}: StructureResolutionReleaseActionProps) {
  const { t } = useTranslation('structure');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [occupant, setOccupant] = useState<StructureResolutionOccupant | null>(
    null,
  );
  const [previewBody, setPreviewBody] =
    useState<ResolutionReleaseResponse | null>(null);
  const stageId = stage?.stageId ?? '';

  const refresh = useCallback(() => {
    scheduleStructureRefresh(
      queryClient,
      competitionId,
      ['graph', 'hubStageShell'],
      { stageId },
    );
  }, [competitionId, queryClient, stageId]);

  const preview = useMutation({
    mutationFn: (target: StructureResolutionOccupant) =>
      previewResolutionRelease(stageId, toRequest(target)),
    onSuccess: (result, target) => {
      setOccupant(target);
      setPreviewBody(result);
    },
    onError: (_error, target) => {
      setOccupant(target);
      setPreviewBody(null);
      refresh();
    },
  });

  const confirm = useMutation({
    mutationFn: () => {
      if (!occupant || !previewBody) {
        throw new Error('Release preview is missing.');
      }
      return confirmResolutionRelease(stageId, {
        ...toRequest(occupant),
        versionToken: previewBody.versionToken,
      });
    },
    onSuccess: () => {
      setOccupant(null);
      setPreviewBody(null);
      refresh();
    },
    onError: () => {
      refresh();
    },
  });

  const requestRelease = useCallback(
    (target: StructureResolutionOccupant) => {
      if (!stage || !target.releasable || preview.isPending) {
        return;
      }
      confirm.reset();
      preview.mutate(target);
    },
    [confirm, preview, stage],
  );

  const blocked = confirm.isError || preview.isError || previewBody === null;
  const detached = previewBody?.detachedMatches ?? [];
  const affected = previewBody?.affectedOccupants ?? [];
  const populationDropped = (previewBody?.releases ?? []).filter(
    (row) => row.target === 'Population' && occupant?.target !== 'Population',
  );
  const navigateAction =
    occupant === null
      ? null
      : freshnessAction(occupant, competitionId, {
          canReplace: false,
          canRelease: false,
        });
  const causeKey =
    occupant?.indeterminacyCause != null
      ? occupant.indeterminacyCause === 'MatchNonJoue' &&
        occupant.actionMatchLabel?.trim()
        ? ('resolution.release.cause.MatchNonJoueNamed' as const)
        : (`resolution.release.cause.${occupant.indeterminacyCause}` as const)
      : null;

  const closeWithoutRelease = () => {
    if (confirm.isPending) {
      return;
    }
    if (confirm.isError || preview.isError) {
      refresh();
    }
    setOccupant(null);
    setPreviewBody(null);
    preview.reset();
    confirm.reset();
  };

  const secondaryNavigate =
    navigateAction?.kind === 'navigate' ? navigateAction : null;

  return (
    <ResolutionReleaseContext.Provider value={{ requestRelease }}>
      {children}
      <ConfirmDialog
        open={occupant !== null && (previewBody !== null || preview.isError)}
        title={t('resolution.release.confirmTitle')}
        message={
          <div>
            {causeKey ? (
              <p>
                {t(
                  causeKey,
                  occupant?.indeterminacyCause === 'MatchNonJoue' &&
                    occupant.actionMatchLabel?.trim()
                    ? { match: occupant.actionMatchLabel.trim() }
                    : undefined,
                )}
              </p>
            ) : null}
            {previewBody === null ? null : (
              <>
                {populationDropped.map((row) => (
                  <p key={`${row.stageId}-${row.entryId}`}>
                    {t('resolution.release.populationDropped', {
                      team: row.entryName ?? '',
                    })}
                  </p>
                ))}
                {detached.length === 0 ? (
                  <p>{t('resolution.release.nonePlayed')}</p>
                ) : (
                  <div>
                    <p>{t('resolution.release.detachedIntro')}</p>
                    <ul>
                      {detached.map((match) => (
                        <li key={`${match.matchId}-${match.legIndex}`}>
                          {detachedLabel(t, match)}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
                {affected.length > 0 ? (
                  <div>
                    <p>{t('resolution.release.affectedIntro')}</p>
                    <ul>
                      {affected.map((row) => (
                        <li
                          key={`${row.stageId}-${row.entryId}-${row.slotKey}-${row.groupId}`}
                        >
                          {affectedLabel(t, row, stageId)}
                        </li>
                      ))}
                    </ul>
                  </div>
                ) : null}
              </>
            )}
          </div>
        }
        confirmLabel={t('resolution.release.confirm')}
        cancelLabel={t('resolution.release.cancel')}
        confirmPending={confirm.isPending}
        confirmPendingLabel={t('resolution.release.pending')}
        confirmDisabled={blocked || confirm.isPending}
        confirmIcon={<UnlockIcon size="sm" aria-hidden="true" />}
        secondaryLabel={
          secondaryNavigate
            ? t(secondaryNavigate.labelKey, secondaryNavigate.labelParams)
            : undefined
        }
        secondaryIcon={
          secondaryNavigate ? (
            <ChevronRightIcon size="sm" aria-hidden="true" />
          ) : undefined
        }
        onSecondary={
          secondaryNavigate
            ? () => {
                closeWithoutRelease();
                navigate(secondaryNavigate.href);
              }
            : undefined
        }
        footerStatus={
          preview.isError ? (
            <MutationError error={preview.error} />
          ) : confirm.isError ? (
            <MutationError error={confirm.error} />
          ) : null
        }
        onConfirm={() => confirm.mutate()}
        onCancel={closeWithoutRelease}
      />
    </ResolutionReleaseContext.Provider>
  );
}

function detachedLabel(
  t: (key: string, params?: Record<string, string>) => string,
  match: ResolutionReleaseDetachedMatch,
): string {
  const home = match.homeName?.trim();
  const away = match.awayName?.trim();
  const teams = home && away ? `${home} – ${away}` : (home ?? away ?? '');
  return t(
    teams
      ? 'resolution.release.detachedMatchNamed'
      : 'resolution.release.detachedMatch',
    {
      leg: String(match.legIndex),
      side: t(`resolution.release.side.${match.side}`),
      match: teams,
    },
  );
}

function affectedLabel(
  t: (key: string, params?: Record<string, string>) => string,
  row: ResolutionReleaseAffectedOccupant,
  stageId: string,
): string {
  const otherPhase = row.stageId !== stageId && Boolean(row.stageName);
  return t(
    otherPhase
      ? 'resolution.release.affectedOccupantOtherPhase'
      : 'resolution.release.affectedOccupant',
    {
      team: row.entryName ?? '',
      place:
        row.placeLabel ??
        row.slotKey ??
        t(`resolution.release.target.${row.target}`),
      phase: row.stageName ?? '',
      before: t(`resolution.freshness.state.${row.stateBefore}`),
      after: t(`resolution.freshness.state.${row.stateAfter}`),
    },
  );
}
