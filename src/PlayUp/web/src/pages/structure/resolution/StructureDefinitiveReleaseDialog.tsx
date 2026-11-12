import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import {
  confirmDefinitiveResolutionRelease,
  confirmDeletedSourceDefinitiveRelease,
  type DefinitiveResolutionReleaseResponse,
  type ResolutionReleaseAffectedOccupant,
  type ResolutionReleaseDetachedMatch,
} from '../../../api';
import { ApiError } from '../../../api/http';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Dialog } from '../../../design-system/components/Dialog';
import { MutationError } from '../../../ui';
import { scheduleStructureRefresh } from '../structureInvalidation';
import {
  definitivePlaceText,
  definitiveReleasePlaces,
  primaryDefinitiveReleases,
} from './structureDefinitiveRelease';

type StructureDefinitiveReleaseDialogProps = {
  open: boolean;
  competitionId: string;
  sourceId: string;
  destinationStageId: string | null;
  preview: DefinitiveResolutionReleaseResponse | null;
  onClose: () => void;
  /** Places whose source phase was deleted. The route is the destination. */
  deletedSources?: boolean;
};

export function StructureDefinitiveReleaseDialog({
  open,
  competitionId,
  sourceId,
  destinationStageId,
  preview,
  onClose,
  deletedSources = false,
}: StructureDefinitiveReleaseDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tErrors } = useTranslation('errors');
  const queryClient = useQueryClient();
  const [summary, setSummary] =
    useState<DefinitiveResolutionReleaseResponse | null>(null);
  const [trackedOpen, setTrackedOpen] = useState(open);
  if (open !== trackedOpen) {
    setTrackedOpen(open);
    if (!open) {
      setSummary(null);
    }
  }

  const refresh = () => {
    scheduleStructureRefresh(
      queryClient,
      competitionId,
      ['graph', 'hubStageShell'],
      { stageId: destinationStageId ?? sourceId },
    );
  };

  const confirm = useMutation({
    mutationFn: () => {
      if (!preview) {
        throw new Error('Definitive release preview is missing.');
      }
      const places = definitiveReleasePlaces(preview);
      if (deletedSources) {
        if (!destinationStageId) {
          throw new Error('Deleted-source release needs a destination.');
        }
        return confirmDeletedSourceDefinitiveRelease(destinationStageId, {
          versionToken: preview.versionToken,
          places,
        });
      }
      return confirmDefinitiveResolutionRelease(sourceId, {
        destinationStageId,
        versionToken: preview.versionToken,
        places,
      });
    },
    onSuccess: (result) => {
      setSummary(result);
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409) {
        refresh();
      }
    },
  });

  const close = () => {
    if (confirm.isPending) {
      return;
    }
    if (summary !== null || confirm.isError) {
      refresh();
    }
    confirm.reset();
    onClose();
  };

  const body = summary ?? preview;
  const showingSummary = summary !== null;

  return (
    <>
      <Dialog
        open={open && showingSummary && body !== null}
        onClose={close}
        title={t('resolution.definitiveRelease.summaryTitle')}
        closeLabel={t('resolution.definitiveRelease.close')}
      >
        {body ? <ReleaseBody body={body} t={t} tErrors={tErrors} /> : null}
      </Dialog>
      <ConfirmDialog
        open={open && !showingSummary && preview !== null}
        title={t('resolution.definitiveRelease.confirmTitle')}
        danger
        message={
          preview ? (
            <div>
              <p>{t('resolution.definitiveRelease.warning')}</p>
              <ReleaseBody body={preview} t={t} tErrors={tErrors} />
            </div>
          ) : null
        }
        confirmLabel={t('resolution.definitiveRelease.confirm')}
        cancelLabel={t('resolution.definitiveRelease.cancel')}
        closeLabel={t('resolution.definitiveRelease.close')}
        confirmPending={confirm.isPending}
        confirmPendingLabel={t('resolution.definitiveRelease.pending')}
        confirmDisabled={confirm.isPending || confirm.isError}
        footerStatus={
          confirm.isError ? <MutationError error={confirm.error} /> : null
        }
        onConfirm={() => confirm.mutate()}
        onCancel={close}
      />
    </>
  );
}

function ReleaseBody({
  body,
  t,
  tErrors,
}: {
  body: DefinitiveResolutionReleaseResponse;
  t: (key: string, params?: Record<string, string>) => string;
  tErrors: (key: string, params?: Record<string, unknown>) => string;
}) {
  const places = primaryDefinitiveReleases(body);
  const populations = body.releases.filter((row) => row.sideEffect);
  return (
    <div>
      <ul>
        {places.map((row) => (
          <li
            key={`${row.stageId}-${row.target}-${row.entryId}-${row.slotKey}`}
          >
            {t('resolution.definitiveRelease.place', {
              team: row.entryName ?? '',
              place: definitivePlaceText(t, row),
              phase: row.stageName ?? '',
            })}
            {row.populationRemoved ? (
              <span>
                {' '}
                {t('resolution.definitiveRelease.population', {
                  team: row.entryName ?? '',
                })}
              </span>
            ) : null}
          </li>
        ))}
      </ul>
      {populations.map((row) => (
        <p key={`population-${row.stageId}-${row.entryId}`}>
          {t('resolution.definitiveRelease.population', {
            team: row.entryName ?? '',
          })}
        </p>
      ))}
      {body.detachedMatches.length === 0 ? (
        <p>{t('resolution.release.nonePlayed')}</p>
      ) : (
        <div>
          <p>{t('resolution.definitiveRelease.detachedIntro')}</p>
          <ul>
            {body.detachedMatches.map((match) => (
              <li key={`${match.matchId}-${match.legIndex}`}>
                {detachedLabel(t, match)}
              </li>
            ))}
          </ul>
        </div>
      )}
      {body.exclusions.length > 0 ? (
        <div>
          <p>{t('resolution.definitiveRelease.exclusionsIntro')}</p>
          <ul>
            {body.exclusions.map((row) => (
              <li
                key={`${row.stageId}-${row.entryId}-${row.slotKey}-${row.code}`}
              >
                {t('resolution.definitiveRelease.excluded', {
                  team: row.entryName ?? '',
                  place: definitivePlaceText(t, row),
                  reason: exclusionReason(tErrors, row.code),
                })}
              </li>
            ))}
          </ul>
        </div>
      ) : null}
      {body.affectedOccupants.length > 0 ? (
        <div>
          <p>{t('resolution.definitiveRelease.affectedIntro')}</p>
          <ul>
            {body.affectedOccupants.map((row) => (
              <li key={`${row.stageId}-${row.entryId}-${row.slotKey}`}>
                {affectedLabel(t, row)}
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}

function exclusionReason(
  t: (key: string, params?: Record<string, unknown>) => string,
  code: string,
): string {
  const label = t(code, { defaultValue: '' });
  return label.length > 0 ? label : code;
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
): string {
  return t('resolution.release.affectedOccupantOtherPhase', {
    team: row.entryName ?? '',
    place: definitivePlaceText(t, row),
    phase: row.stageName ?? '',
    before: t(`resolution.freshness.state.${row.stateBefore}`),
    after: t(`resolution.freshness.state.${row.stateAfter}`),
  });
}
