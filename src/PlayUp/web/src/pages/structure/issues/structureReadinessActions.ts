import type { StructureStageHubSummary, StructureView } from '../../../types';
import type { StructureSectionId } from '../structureHubSections';
import {
  structureDiagnosticMeta,
  structureDiagnosticTone,
  type StructureDiagnosticCta,
  type StructureDiagnosticTile,
  type StructureDiagnosticTone,
} from './structureDiagnosticPresentation';
import { structureDiagnosticContextFromStage } from './structureDiagnosticContext';
import { stagePresentedIssueCodes } from './structureStagePresentedIssues';
import { blockerDetailsForStage } from './structureReadinessBlockerDetails';
import {
  structureDeepLink,
  type StructureFocusId,
} from '../structureNavigation';

export type StructureReadinessActionItem = {
  id: string;
  stageId: string;
  stageName: string;
  code: string;
  tone: StructureDiagnosticTone;
  titleKey: string;
  cta: StructureDiagnosticCta;
  /** Overrides generic readiness.action.{cta} when navigation is contextual. */
  actionLabelKey?: string;
  href: string;
};

function resolvePlacesActionLabelKey(stage: StructureStageHubSummary): string {
  if (stage.hasDrawRules) {
    return 'readiness.action.placesOpenDraw';
  }
  return 'readiness.action.placesManual';
}

const TONE_RANK: Record<StructureDiagnosticTone, number> = {
  danger: 0,
  attention: 1,
  progress: 2,
};

function ctaToSection(cta: StructureDiagnosticCta): StructureSectionId | null {
  switch (cta) {
    case 'qualification':
      return 'qualification';
    case 'progression':
      return 'progression';
    case 'tieFormat':
      return 'confrontation';
    case 'standing':
      return 'classement';
    case 'structure':
      return 'construction';
    default:
      return null;
  }
}

function ctaToFocus(
  cta: StructureDiagnosticCta,
  tile: StructureDiagnosticTile | null,
): StructureFocusId | null {
  if (cta === 'sorties') {
    return 'rail-exits';
  }
  if (cta === 'attribution') {
    return 'rail-attribution';
  }
  if (cta === 'affectation' && tile === 'capacity') {
    return 'rail-entries';
  }
  return null;
}

export function resolveStructureDiagnosticNavigation(args: {
  competitionId: string;
  stageId: string;
  stage?: StructureStageHubSummary;
  code: string;
  cta: StructureDiagnosticCta;
}): string {
  if (args.code === 'MissingPotRules') {
    return structureDeepLink({
      competitionId: args.competitionId,
      stageId: args.stageId,
      section: 'tirage',
    });
  }
  if (args.code === 'CupBracketInvalid') {
    return structureDeepLink({
      competitionId: args.competitionId,
      stageId: args.stageId,
      section: 'construction',
    });
  }

  if (args.cta === 'places') {
    if (args.stage?.hasDrawRules) {
      return structureDeepLink({
        competitionId: args.competitionId,
        stageId: args.stageId,
        openDrawWorkflow: true,
      });
    }
    return structureDeepLink({
      competitionId: args.competitionId,
      stageId: args.stageId,
      manual: true,
    });
  }

  const tile = structureDiagnosticMeta(args.code)?.tile ?? null;
  const section = ctaToSection(args.cta);
  const focus = ctaToFocus(args.cta, tile);
  const compose = args.cta === 'affectation' && focus === 'rail-entries';

  if (section) {
    return structureDeepLink({
      competitionId: args.competitionId,
      stageId: args.stageId,
      section,
    });
  }

  return structureDeepLink({
    competitionId: args.competitionId,
    stageId: args.stageId,
    compose: compose || undefined,
    focus: focus ?? undefined,
  });
}

function pushIssueItem(
  items: StructureReadinessActionItem[],
  args: {
    data: StructureView;
    competitionId: string;
    stage: StructureStageHubSummary;
    code: string;
  },
): void {
  const meta = structureDiagnosticMeta(args.code);
  if (!meta) {
    return;
  }
  const tone = structureDiagnosticTone(
    args.code,
    structureDiagnosticContextFromStage(args.data, args.stage),
  );
  items.push({
    id: `${args.stage.stageId}:${args.code}`,
    stageId: args.stage.stageId,
    stageName: args.stage.name,
    code: args.code,
    tone,
    titleKey: meta.titleKey,
    cta: meta.cta,
    actionLabelKey:
      meta.cta === 'places'
        ? resolvePlacesActionLabelKey(args.stage)
        : undefined,
    href: resolveStructureDiagnosticNavigation({
      competitionId: args.competitionId,
      stageId: args.stage.stageId,
      stage: args.stage,
      code: args.code,
      cta: meta.cta,
    }),
  });
}

function pushSyntheticItem(
  items: StructureReadinessActionItem[],
  args: {
    competitionId: string;
    stage: StructureStageHubSummary;
    id: string;
    code: string;
    tone: StructureDiagnosticTone;
    titleKey: string;
    cta: StructureDiagnosticCta;
  },
): void {
  items.push({
    id: args.id,
    stageId: args.stage.stageId,
    stageName: args.stage.name,
    code: args.code,
    tone: args.tone,
    titleKey: args.titleKey,
    cta: args.cta,
    href: resolveStructureDiagnosticNavigation({
      competitionId: args.competitionId,
      stageId: args.stage.stageId,
      stage: args.stage,
      code: args.code,
      cta: args.cta,
    }),
  });
}

/**
 * All banner-eligible correction entries (sorted, not capped).
 * Uses the same stage issue projection as readiness summary.
 */
export function collectAllStructureReadinessActionItems(
  data: StructureView,
  stages: StructureStageHubSummary[],
): StructureReadinessActionItem[] {
  if (stages.length === 0) {
    return [];
  }

  const items: StructureReadinessActionItem[] = [];
  const competitionId = data.competitionId;

  for (const stage of stages) {
    const presented = stagePresentedIssueCodes(data, stage);
    for (const code of presented) {
      pushIssueItem(items, { data, competitionId, stage, code });
    }
  }

  for (const stage of stages) {
    for (const _entry of blockerDetailsForStage(
      data,
      stage.stageId,
      'CupBracketInvalid',
    )) {
      pushSyntheticItem(items, {
        competitionId,
        stage,
        id: `${stage.stageId}:CupBracketInvalid`,
        code: 'CupBracketInvalid',
        tone: 'danger',
        titleKey: 'readiness.statShape',
        cta: 'structure',
      });
    }
  }

  items.sort((a, b) => {
    const toneDiff = TONE_RANK[a.tone] - TONE_RANK[b.tone];
    if (toneDiff !== 0) {
      return toneDiff;
    }
    const stageDiff = a.stageId.localeCompare(b.stageId);
    if (stageDiff !== 0) {
      return stageDiff;
    }
    return a.code.localeCompare(b.code);
  });

  return items;
}
