import { useTranslation } from 'react-i18next';

import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  UndoIcon,
  UnlockIcon,
} from '../../../design-system/icons/contentIcons';
import type { StructureStageHubSummary, StageDraw } from '../../../types';
import { MutationError, PendingLabel } from '../../../ui';
import {
  type DrawRedoPresentation,
  type ServerReleaseCta,
  releaseFooterMode,
  releaseBlockReason,
  redoPresentation,
} from '../draw/drawActions';
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
}: StructureStaleDrawResolutionActionsProps) {
  const { t } = useTranslation(['structure', 'draw', 'common']);
  const redo =
    activeDraw != null
      ? redoPresentation(activeDraw)
      : { kind: 'hidden' as const };
  const canRedo = redo.kind === 'available';
  const redoReason = redoDisabledReason(redo, t);

  const appliedReleaseMode =
    activeDraw != null ? releaseFooterMode(activeDraw) : 'hidden';
  const appliedReleaseDisabledReason =
    activeDraw != null ? releaseBlockReason(activeDraw) : null;

  const useAppliedRelease =
    appliedReleaseMode !== 'hidden' && activeDraw != null;
  const releaseDraw = useAppliedRelease ? activeDraw : releaseCta.draw;
  const releaseAligned = useAppliedRelease
    ? (drawActionsReleaseCount(activeDraw!) ?? 0)
    : releaseCta.aligned;
  const releaseDisabled = useAppliedRelease
    ? appliedReleaseMode === 'disabled'
    : releaseCta.disabled || releaseCta.draw == null;
  const releaseDisabledReason = useAppliedRelease
    ? appliedReleaseDisabledReason
    : releaseCta.disabledReason;

  const showRelease =
    releaseDraw != null && (useAppliedRelease || releaseCta.aligned > 0);

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
              aria-label={t('structure:resolution.staleDraw.release')}
            >
              <UnlockIcon size="sm" />
              {t('structure:resolution.staleDraw.release')}
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
                {t('structure:resolution.staleDraw.release')}
                {releaseAligned > 0 ? ` (${releaseAligned})` : null}
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
      </div>
      <ConfirmDialog
        open={confirmRedoOpen}
        title={t('structure:resolution.staleDraw.confirmRedoTitle')}
        message={t('draw:confirmRedo')}
        confirmLabel={t('structure:resolution.staleDraw.redo')}
        cancelLabel={t('common:close')}
        closeLabel={t('common:close')}
        confirmPending={redoPending}
        confirmPendingLabel={t('draw:redoing')}
        footerStatus={
          redoError ? (
            <MutationError
              error={redoError}
              structureStages={structureStages}
            />
          ) : null
        }
        onCancel={onCancelConfirmRedo}
        onConfirm={onConfirmRedo}
      />
      <ConfirmDialog
        open={confirmReleaseOpen}
        title={t('structure:resolution.staleDraw.confirmReleaseTitle')}
        message={t('draw:confirmRelease')}
        confirmLabel={t('draw:releasePlacements')}
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

function drawActionsReleaseCount(draw: StageDraw): number | null {
  const release = draw.actions?.release;
  if (release == null || release.releasableCount <= 0) {
    return null;
  }
  return release.releasableCount;
}
