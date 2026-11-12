import { useQueries } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { previewDefinitiveResolutionRelease } from '../../../api';
import type { StructureStageHubSummary } from '../../../types';
import { queryKeys } from '../../../queryKeys';
import { StructureDefinitiveReleaseDialog } from './StructureDefinitiveReleaseDialog';
import { primaryDefinitiveReleases } from './structureDefinitiveRelease';

type StructureDefinitiveReleaseBannerProps = {
  stage: StructureStageHubSummary;
  stages: readonly StructureStageHubSummary[];
  competitionId: string;
};

export function StructureDefinitiveReleaseBanner({
  stage,
  stages,
  competitionId,
}: StructureDefinitiveReleaseBannerProps) {
  const { t } = useTranslation('structure');
  const { t: tErrors } = useTranslation('errors');
  const known = new Set(stages.map((item) => item.stageId));
  const sourceIds = [
    ...new Set(
      (stage.resolutionOccupants ?? [])
        .filter(
          (occupant) =>
            occupant.definitive === true &&
            occupant.upstreamStageId != null &&
            known.has(occupant.upstreamStageId),
        )
        .map((occupant) => occupant.upstreamStageId as string),
    ),
  ];
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
  const [openSourceId, setOpenSourceId] = useState<string | null>(null);
  const openIndex = sourceIds.indexOf(openSourceId ?? '');
  const openPreview =
    openIndex >= 0 ? (previews[openIndex]?.data ?? null) : null;

  const rows = sourceIds.flatMap((sourceId, index) => {
    const preview = previews[index]?.data;
    if (!preview) {
      return [];
    }
    const count = primaryDefinitiveReleases(preview).length;
    if (count === 0 && preview.exclusions.length === 0) {
      return [];
    }
    return [{ sourceId, preview, count }];
  });

  if (rows.length === 0) {
    return null;
  }

  return (
    <div className="structure-definitive-release">
      {rows.map((row) => (
        <section
          key={row.sourceId}
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
                      place:
                        exclusion.placeLabel ??
                        exclusion.slotKey ??
                        exclusion.target,
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
              onClick={() => setOpenSourceId(row.sourceId)}
            >
              {t('resolution.definitiveRelease.release')}
            </button>
          ) : null}
        </section>
      ))}
      <StructureDefinitiveReleaseDialog
        open={openPreview !== null}
        competitionId={competitionId}
        sourceId={openSourceId ?? ''}
        destinationStageId={stage.stageId}
        preview={openPreview}
        onClose={() => setOpenSourceId(null)}
      />
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
