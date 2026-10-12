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

export function attentionSituationMeta(
  item: OverviewSituation,
  stages: readonly CompetitionStageSummary[],
): string | null {
  const stageId =
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
