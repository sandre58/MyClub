import type { UseMutationResult } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import { TextInput } from '../../../design-system/components/TextInput';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  ChampionshipFormatIcon,
  CheckIcon,
  CupFormatIcon,
  GroupsFormatIcon,
  PencilIcon,
  StructureIcon,
  SwissFormatIcon,
} from '../../../design-system/icons/contentIcons';
import { CloseIcon } from '../../../design-system/icons/shellIcons';
import { structureFormatKindLabel } from '../../../i18n/enumLabels';
import type {
  CompetitionStatus,
  StructureFormatKind,
  StructureStageHubSummary,
} from '../../../types';
import { MutationError, StageStatusBadge } from '../../../ui';
import { shouldShowMatchsLifecycle } from '../problems';
import { StructurePhaseHeaderLifecycle } from './StructurePhaseHeaderLifecycle';

function FormatGlyph({
  kind,
  size = 'md',
}: {
  kind?: StructureFormatKind | null;
  size?: 'sm' | 'md' | 'lg';
}) {
  switch (kind) {
    case 'Cup':
      return <CupFormatIcon size={size} aria-hidden="true" />;
    case 'Championship':
      return <ChampionshipFormatIcon size={size} aria-hidden="true" />;
    case 'Groups':
      return <GroupsFormatIcon size={size} aria-hidden="true" />;
    case 'Swiss':
      return <SwissFormatIcon size={size} aria-hidden="true" />;
    default:
      return <StructureIcon size={size} aria-hidden="true" />;
  }
}

export function StructurePhaseFicheHeader({
  stage,
  competitionId,
  competitionStatus = null,
  structureStages,
  formatLabel,
  editingName,
  nameDraft,
  renameMutation,
  canRename,
  compactIcon,
  onNameDraftChange,
  onBeginRename,
  onCancelRename,
  onCommitRename,
}: {
  stage: StructureStageHubSummary;
  competitionId: string;
  competitionStatus?: CompetitionStatus | null;
  structureStages: readonly StructureStageHubSummary[];
  formatLabel: string | null;
  editingName: boolean;
  nameDraft: string;
  renameMutation: UseMutationResult<void, Error, string, unknown>;
  canRename: boolean;
  compactIcon: string;
  onNameDraftChange: (value: string) => void;
  onBeginRename: () => void;
  onCancelRename: () => void;
  onCommitRename: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');

  const resolvedFormatLabel =
    formatLabel ??
    (stage.formatKind ? structureFormatKindLabel(stage.formatKind) : null);

  const showLifecycleRow = !editingName && shouldShowMatchsLifecycle(stage);

  return (
    <header className="structure-fiche__header structure-fiche__header--two-rows">
      <div className="structure-fiche__header-row structure-fiche__header-row--identity">
        <div className="structure-fiche__header-identity">
          {resolvedFormatLabel ? (
            <Tooltip content={resolvedFormatLabel}>
              <span className="structure-fiche__format-icon">
                <FormatGlyph kind={stage.formatKind} size="lg" />
              </span>
            </Tooltip>
          ) : (
            <span className="structure-fiche__format-icon" aria-hidden="true">
              <FormatGlyph kind={stage.formatKind} size="lg" />
            </span>
          )}
          {editingName ? (
            <form
              className="structure-fiche__title-edit"
              onSubmit={(event) => {
                event.preventDefault();
                onCommitRename();
              }}
            >
              <div className="structure-fiche__title-edit-field">
                <label
                  className="ds-visually-hidden"
                  htmlFor="phase-rename-input"
                >
                  {t('fiche.renamePhase')}
                </label>
                <TextInput
                  id="phase-rename-input"
                  value={nameDraft}
                  onChange={(event) => onNameDraftChange(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === 'Escape') {
                      event.preventDefault();
                      onCancelRename();
                    }
                  }}
                  disabled={renameMutation.isPending}
                  maxLength={100}
                  required
                  autoFocus
                  invalid={renameMutation.isError}
                />
              </div>
              <div className="ds-icon-toolbar">
                <Tooltip content={t('fiche.confirmRename')}>
                  <button
                    type="submit"
                    className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact ds-icon-button--affirm"
                    disabled={
                      renameMutation.isPending || nameDraft.trim().length === 0
                    }
                    aria-label={t('fiche.confirmRename')}
                  >
                    {renameMutation.isPending ? (
                      <span className="ds-spinner" aria-hidden="true" />
                    ) : (
                      <CheckIcon size="sm" />
                    )}
                  </button>
                </Tooltip>
                <Tooltip content={tCommon('cancel')}>
                  <button
                    type="button"
                    className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact ds-icon-button--dismiss"
                    disabled={renameMutation.isPending}
                    aria-label={tCommon('cancel')}
                    onClick={onCancelRename}
                  >
                    <CloseIcon size="sm" />
                  </button>
                </Tooltip>
              </div>
              {renameMutation.isError ? (
                <MutationError
                  error={renameMutation.error}
                  structureStages={structureStages}
                />
              ) : null}
            </form>
          ) : (
            <>
              <h2
                id="phase-overview-heading"
                className="structure-fiche__title"
              >
                {stage.name}
              </h2>
              {canRename ? (
                <Tooltip content={t('fiche.editPhase')}>
                  <button
                    type="button"
                    className={compactIcon}
                    aria-label={t('fiche.editPhase')}
                    onClick={onBeginRename}
                  >
                    <PencilIcon size="sm" />
                  </button>
                </Tooltip>
              ) : null}
            </>
          )}
        </div>
        {!editingName ? (
          <div className="structure-fiche__header-row-end">
            <StageStatusBadge status={stage.status} />
          </div>
        ) : null}
      </div>
      {showLifecycleRow ? (
        <div className="structure-fiche__header-row structure-fiche__header-row--lifecycle">
          <div className="structure-fiche__header-lifecycle-cluster">
            <StructurePhaseHeaderLifecycle
              competitionId={competitionId}
              stage={stage}
              competitionStatus={competitionStatus}
            />
          </div>
        </div>
      ) : null}
    </header>
  );
}

export type StructurePhaseHeroFact = {
  icon: ReactNode;
  value: string | number;
  label: string;
  statKey: string;
};
