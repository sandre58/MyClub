import i18n from '../i18n';
import { attentionTargetTypeLabel } from '../i18n/enumLabels';
import { situationTitle } from '../i18n/situationCopy';
import type { CompetitionStageSummary } from '../types';
import type { FunctionalProblemPresentation } from './functionalProblemPresentation';
import type { OverviewSituation } from '../types';

export function attentionSituationTitle(
  presentation: FunctionalProblemPresentation,
  fallbackSource: string,
  params?: Record<string, string | number | undefined>,
): string {
  const mergedParams = {
    ...presentation.descriptionParams,
    ...params,
  };
  const translated = i18n.t(presentation.titleKey, {
    ns: presentation.titleNs,
    defaultValue: '',
    ...mergedParams,
  });
  if (translated.length > 0 && translated !== presentation.titleKey) {
    return translated;
  }

  return situationTitle(fallbackSource, params);
}

/**
 * Second line under the drawer title for runtime signals that need business context.
 */
export function attentionSituationSubtitle(
  item: OverviewSituation,
): string | null {
  const { source, targetType } = item;
  if (source === 'QualificationConflict') {
    if (targetType === 'Form') {
      return i18n.t('attention.rowSubtitle.qualificationConflictForm', {
        ns: 'shell',
      });
    }
    return i18n.t('attention.rowSubtitle.qualificationConflict', {
      ns: 'shell',
    });
  }
  if (source === 'ProgressionConflict') {
    if (targetType === 'Form') {
      return i18n.t('attention.rowSubtitle.progressionConflictForm', {
        ns: 'shell',
      });
    }
    return i18n.t('attention.rowSubtitle.progressionConflict', { ns: 'shell' });
  }
  if (source === 'QualificationPending') {
    return i18n.t('attention.rowSubtitle.qualificationPending', {
      ns: 'shell',
    });
  }
  if (source === 'ProgressionPending') {
    return i18n.t('attention.rowSubtitle.progressionPending', { ns: 'shell' });
  }
  return null;
}

export function attentionSituationMeta(
  item: OverviewSituation,
  stages: readonly CompetitionStageSummary[],
): string | null {
  const stageId =
    item.params?.destinationStageId?.trim() ||
    item.params?.stageId?.trim() ||
    (item.targetType === 'Stage' ? item.targetId?.trim() : undefined);

  if (stageId) {
    const stage = stages.find((candidate) => candidate.stageId === stageId);
    if (stage?.name) {
      return stage.name;
    }
  }

  if (item.targetType === 'Competition') {
    return null;
  }

  return item.targetType ? attentionTargetTypeLabel(item.targetType) : null;
}
