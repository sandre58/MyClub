import { useQuery, useQueries } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import {
  previewDefinitiveResolutionRelease,
  previewDeletedSourceDefinitiveRelease,
  type DefinitiveResolutionReleaseResponse,
} from '../../../api';
import type { StructureStageHubSummary } from '../../../types';
import { queryKeys } from '../../../queryKeys';
import { StructureDefinitiveReleaseDialog } from './StructureDefinitiveReleaseDialog';
import {
  definitivePlaceText,
  primaryDefinitiveReleases,
} from './structureDefinitiveRelease';

type StructureDefinitiveReleaseBannerProps = {
  stage: StructureStageHubSummary;
  stages: readonly StructureStageHubSummary[];
  competitionId: string;
};

type ReleaseSession = {
  sourceId: string | null;
  preview: DefinitiveResolutionReleaseResponse;
};

export function StructureDefinitiveReleaseBanner({
  stage,
  stages,
  competitionId,
}: StructureDefinitiveReleaseBannerProps) {
  const { t } = useTranslation('structure');
  const { t: tErrors } = useTranslation('errors');
  const known = new Set(stages.map((item) => item.stageId));
  const occupants = stage.resolutionOccupants ?? [];
  const sourceIds = [
    ...new Set(
      occupants
        .filter(
          (occupant) =>
            occupant.definitive === true &&
            occupant.upstreamStageId != null &&
            known.has(occupant.upstreamStageId),
        )
        .map((occupant) => occupant.upstreamStageId as string),
    ),
  ];
  const hasDeletedSource = occupants.some(
    (occupant) =>
      occupant.definitive === true &&
      (occupant.upstreamStageId == null ||
        !known.has(occupant.upstreamStageId)),
  );
  const previews = useQueries({
    queries: sourceIds.map((sourceId) => ({
      queryKey: queryKeys.stages.definitiveReleasePreview(
        sourceId,
        stage.stageId,
      ),
      queryFn: () =>
        previewDefinitiveResolutionRelease(sourceId, {
          destinationStageId: stage.stageId,
        }),
    })),
  });
  const deletedPreview = useQuery({
    queryKey: queryKeys.stages.definitiveDeletedReleasePreview(stage.stageId),
    queryFn: () => previewDeletedSourceDefinitiveRelease(stage.stageId),
    enabled: hasDeletedSource,
  });
  const [session, setSession] = useState<ReleaseSession | null>(null);

  const rows = sourceIds.flatMap((sourceId, index) => {
    const preview = previews[index]?.data;
    return preview ? bannerRow(sourceId, preview) : [];
  });
  const deletedRow = deletedPreview.data
    ? bannerRow(null, deletedPreview.data)
    : [];

  if (rows.length === 0 && deletedRow.length === 0 && session === null) {
    return null;
  }

  return (
    <div className="structure-definitive-release">
      <div>
        {[...rows, ...deletedRow].map((row) => (
          <section
            key={row.sourceId ?? 'deleted'}
            className="structure-definitive-release__source"
          >
            {row.count > 0 ? (
              <p>
                {t('resolution.definitiveRelease.banner', { count: row.count })}
              </p>
            ) : null}
            {row.preview.exclusions.length > 0 ? (
              <div>
                <p>{t('resolution.definitiveRelease.excludedIntro')}</p>
                <ul className="structure-definitive-release__exclusions">
                  {row.preview.exclusions.map((exclusion) => (
                    <li
                      key={`${exclusion.entryId}-${exclusion.slotKey}-${exclusion.code}`}
                    >
                      {t('resolution.definitiveRelease.excluded', {
                        team: exclusion.entryName ?? '',
                        place: definitivePlaceText(t, exclusion),
                        reason: exclusionReason(tErrors, exclusion.code),
                      })}
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
            {row.count > 0 ? (
              <button
                type="button"
                className="ds-btn ds-btn--destructive ds-btn--sm"
                onClick={() =>
                  setSession({ sourceId: row.sourceId, preview: row.preview })
                }
              >
                {t('resolution.definitiveRelease.release')}
              </button>
            ) : null}
          </section>
        ))}
      </div>
      <StructureDefinitiveReleaseDialog
        open={session !== null}
        competitionId={competitionId}
        sourceId={session?.sourceId ?? stage.stageId}
        destinationStageId={stage.stageId}
        preview={session?.preview ?? null}
        deletedSources={session?.sourceId == null && session !== null}
        onClose={() => setSession(null)}
      />
    </div>
  );
}

function bannerRow(
  sourceId: string | null,
  preview: DefinitiveResolutionReleaseResponse,
) {
  const count = primaryDefinitiveReleases(preview).length;
  if (count === 0 && preview.exclusions.length === 0) {
    return [];
  }
  return [{ sourceId, preview, count }];
}

function exclusionReason(
  t: (key: string, params?: Record<string, unknown>) => string,
  code: string,
): string {
  const label = t(code, { defaultValue: '' });
  return label.length > 0 ? label : code;
}
