import type {
  CompetitionStageSummary,
  OverviewSituation,
  OverviewSituationNature,
  StageStatus,
} from '../types';
import { structureDiagnosticMeta } from '../pages/structure/structureDiagnosticPresentation';
import { structureDiagnosticPresentation } from '../pages/structure/structureDiagnosticPresentationPolicy';

const EXTRA_STRUCTURE_ATTENTION_SOURCES = new Set([
  'CupBracketInvalid',
  'MissingPotRules',
]);

export function isStructureAttentionSource(source: string): boolean {
  return (
    structureDiagnosticMeta(source) != null ||
    EXTRA_STRUCTURE_ATTENTION_SOURCES.has(source)
  );
}

export function structureAttentionStageId(
  item: OverviewSituation,
): string | null {
  const fromParams = item.params?.stageId?.trim();
  if (fromParams) {
    return fromParams;
  }
  if (item.targetType === 'Stage' && item.targetId?.trim()) {
    return item.targetId.trim();
  }
  return null;
}

export function structureAttentionStageStatus(
  item: OverviewSituation,
  stages: readonly CompetitionStageSummary[],
): StageStatus | null {
  const stageId = structureAttentionStageId(item);
  if (!stageId) {
    return null;
  }
  return stages.find((stage) => stage.stageId === stageId)?.status ?? null;
}

function wireNatureToPresentationNature(
  nature: OverviewSituation['nature'],
): OverviewSituationNature {
  return nature === 'Blocking' ? 'Blocking' : 'Informational';
}

/** Drawer / shell chrome: D5 policy overrides wire severity for structure catalogue rows. */
export function presentedStructureAttentionNature(
  item: OverviewSituation,
  stages: readonly CompetitionStageSummary[],
): OverviewSituationNature {
  if (!isStructureAttentionSource(item.source)) {
    return wireNatureToPresentationNature(item.nature);
  }

  const stageStatus = structureAttentionStageStatus(item, stages);
  const { severity } = structureDiagnosticPresentation(item.source, {
    stageStatus,
  });

  if (severity === 'hidden') {
    return 'Informational';
  }
  return severity === 'blocking' ? 'Blocking' : 'Informational';
}

export function shouldPresentStructureAttentionItem(
  item: OverviewSituation,
  stages: readonly CompetitionStageSummary[],
): boolean {
  if (!isStructureAttentionSource(item.source)) {
    return true;
  }
  const stageStatus = structureAttentionStageStatus(item, stages);
  if (stageStatus === 'Completed') {
    return false;
  }
  const { severity } = structureDiagnosticPresentation(item.source, {
    stageStatus,
  });
  return severity !== 'hidden';
}

export function applyStructureAttentionPresentation(
  situations: OverviewSituation[],
  stages: readonly CompetitionStageSummary[],
): OverviewSituation[] {
  return situations
    .filter((item) => shouldPresentStructureAttentionItem(item, stages))
    .map((item) => ({
      ...item,
      nature: presentedStructureAttentionNature(item, stages),
    }));
}
