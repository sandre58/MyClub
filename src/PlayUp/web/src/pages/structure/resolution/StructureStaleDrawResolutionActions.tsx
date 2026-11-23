import { useTranslation } from 'react-i18next';

import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  UndoIcon,
  UnlockIcon,
} from '../../../design-system/icons/contentIcons';
import type { StructureStageHubSummary, StageDraw } from '../../../types';
import { ApiError } from '../../../api/http';
import { MutationError, PendingLabel } from '../../../ui';
import {
  type DrawRedoPresentation,
  type ServerReleaseCta,
  formatReleaseDrawnTeamsLabel,
  releaseFooterMode,
  releaseBlockReason,
  redoPresentation,
} from '../draw/drawActions';
import {
  drawCreateBlockDetailNameIndex,
  formatDrawCreateBlockDetailLines,
} from '../draw/drawCreateBlockDetail';
import { StripOperationalDiagnosticMessage } from '../fiche/structureOperationalStripUi';

export type StructureStaleDrawResolutionActionsProps = {
  activeDraw: StageDraw | null;
  releaseCta: ServerReleaseCta;
  redoPending: boolean;
  releasePending: boolean;
  redoError: unknown;
  releaseError: unknown;
  confirmRedoOpen: boolean;
  confirmReleaseOpen: boolean;
  structureStages: readonly StructureStageHubSummary[];
  onOpenConfirmRedo: () => void;
  onOpenConfirmRelease: () => void;
  onCancelConfirmRedo: () => void;
  onCancelConfirmRelease: () => void;
  onConfirmRedo: () => void;
  onConfirmRelease: () => void;
  detailGroups?: readonly { id: string; name: string }[];
  detailRoster?: readonly { entryId: string; displayName?: string | null }[];
  stageDraws?: readonly StageDraw[];
};

function redoDisabledReason(
  presentation: DrawRedoPresentation,
  translate: (key: string) => string,
): string | null {
  if (presentation.kind !== 'blocked') {
    return null;
  }
  return translate(`draw:redoBlocked.${presentation.reason}`);
}

export function StructureStaleDrawResolutionActions({
  activeDraw,
  releaseCta,
  redoPending,
  releasePending,
  redoError,
  releaseError,
  confirmRedoOpen,
  confirmReleaseOpen,
  structureStages,
  onOpenConfirmRedo,
  onOpenConfirmRelease,
  onCancelConfirmRedo,
  onCancelConfirmRelease,
  onConfirmRedo,
  onConfirmRelease,
  detailGroups,
  detailRoster,
  stageDraws = [],
}: StructureStaleDrawResolutionActionsProps) {
  const { t } = useTranslation(['structure', 'draw', 'common']);
  const redo =
    activeDraw != null
      ? redoPresentation(activeDraw)
      : { kind: 'hidden' as const };
  const canRedo = redo.kind === 'available';
  const redoReason = redoDisabledReason(redo, t);
  const detailNames = drawCreateBlockDetailNameIndex({
    groups: detailGroups,
    roster: detailRoster ?? [],
  });
  const redoNamedLines =
    activeDraw != null && redo.kind === 'blocked'
      ? formatDrawCreateBlockDetailLines(
          activeDraw.actions?.redo.blockDetail,
          detailNames,
          t,
        )
      : [];
  const redoErrorLines = formatDrawCreateBlockDetailLines(
    redoError instanceof ApiError ? redoError.reasons : undefined,
    detailNames,
    t,
  );

  const appliedReleaseMode =
    activeDraw != null ? releaseFooterMode(activeDraw) : 'hidden';
  const appliedReleaseDisabledReason =
    activeDraw != null ? releaseBlockReason(activeDraw) : null;

  const useAppliedRelease =
    appliedReleaseMode !== 'hidden' && activeDraw != null;
  const staleServerMode =
    releaseCta.draw != null
      ? releaseFooterMode(releaseCta.draw)
      : ('hidden' as const);
  const releaseMode = useAppliedRelease ? appliedReleaseMode : staleServerMode;
  const releaseDraw = useAppliedRelease ? activeDraw : releaseCta.draw;
  const releaseDisabled = releaseMode === 'disabled';
  const releaseDisabledReason = useAppliedRelease
    ? appliedReleaseDisabledReason
    : releaseCta.disabledReason;

  const showRelease = releaseMode !== 'hidden' && releaseDraw != null;
  const releaseLabel =
    releaseDraw != null
      ? formatReleaseDrawnTeamsLabel(t, releaseDraw, stageDraws)
      : t('structure:resolution.staleDraw.release');

  const actionRow = (
    <div className="structure-stale-draw-actions__buttons">
      {redo.kind !== 'hidden' ? (
        redoReason != null && !canRedo ? (
          <Tooltip content={redoReason}>
            <button
              type="button"
              className="ds-btn ds-btn--secondary"
              disabled
              aria-label={t('structure:resolution.staleDraw.redo')}
            >
              <UndoIcon size="sm" />
              {t('structure:resolution.staleDraw.redo')}
            </button>
          </Tooltip>
        ) : (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={!canRedo || redoPending || releasePending}
            onClick={onOpenConfirmRedo}
          >
            {redoPending ? (
              <PendingLabel>{t('draw:redoing')}</PendingLabel>
            ) : (
              <>
                <UndoIcon size="sm" />
                {t('structure:resolution.staleDraw.redo')}
              </>
            )}
          </button>
        )
      ) : null}
      {showRelease ? (
        releaseDisabled && releaseDisabledReason ? (
          <Tooltip content={releaseDisabledReason}>
            <button
              type="button"
              className="ds-btn ds-btn--secondary"
              disabled
              aria-label={releaseLabel}
            >
              <UnlockIcon size="sm" />
              {releaseLabel}
            </button>
          </Tooltip>
        ) : (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={releaseDisabled || releasePending || redoPending}
            onClick={onOpenConfirmRelease}
          >
            {releasePending ? (
              <PendingLabel>{t('draw:releasingPlacements')}</PendingLabel>
            ) : (
              <>
                <UnlockIcon size="sm" />
                {releaseLabel}
              </>
            )}
          </button>
        )
      ) : null}
    </div>
  );

  return (
    <>
      <div className="structure-stale-draw-actions">
        <StripOperationalDiagnosticMessage tone="attention">
          {t('structure:resolution.staleDraw.message')}
        </StripOperationalDiagnosticMessage>
        {actionRow}
        {redoNamedLines.map((line, index) => (
          <p key={index} className="ds-body">
            {line}
          </p>
        ))}
      </div>
      <ConfirmDialog
        open={confirmRedoOpen}
        title={t('structure:resolution.staleDraw.confirmRedoTitle')}
        message={
          activeDraw?.kind === 'Group'
            ? t('draw:confirmRedoGroups')
            : activeDraw?.inputsIntent === 'Partial'
              ? t('draw:confirmRedoCup')
              : t('draw:confirmRedo')
        }
        confirmLabel={t('structure:resolution.staleDraw.redo')}
        cancelLabel={t('common:close')}
        closeLabel={t('common:close')}
        confirmPending={redoPending}
        confirmPendingLabel={t('draw:redoing')}
        footerStatus={
          redoError ? (
            <>
              <MutationError
                error={redoError}
                structureStages={structureStages}
              />
              {redoErrorLines.map((line, index) => (
                <p key={index} className="ds-body">
                  {line}
                </p>
              ))}
            </>
          ) : null
        }
        onCancel={onCancelConfirmRedo}
        onConfirm={onConfirmRedo}
      />
      <ConfirmDialog
        open={confirmReleaseOpen}
        title={t('structure:resolution.staleDraw.confirmReleaseTitle')}
        message={t('draw:confirmRelease')}
        confirmLabel={
          releaseDraw != null
            ? formatReleaseDrawnTeamsLabel(t, releaseDraw, stageDraws)
            : t('draw:releasePlacements')
        }
        confirmIcon={<UnlockIcon size="sm" />}
        cancelLabel={t('common:close')}
        closeLabel={t('common:close')}
        confirmPending={releasePending}
        confirmPendingLabel={t('draw:releasingPlacements')}
        footerStatus={
          releaseError ? (
            <MutationError
              error={releaseError}
              structureStages={structureStages}
            />
          ) : null
        }
        onCancel={onCancelConfirmRelease}
        onConfirm={onConfirmRelease}
      />
    </>
  );
}
