/**
 * Thin presentation mapping for functional problems (Phase 1 contrat).
 * Reuses existing i18n — no parallel catalogue, no Error Framework.
 *
 * Identity aligns with Overview DeduplicateSituations: source + targetType + targetId.
 *
 * Structure construction surfaces: detection, diagnosis, and correction stay distinct.
 * Diagnostic taxonomy → presentation via structureDiagnosticPresentation (family / tile / CTA).
 * Population shortfall may also come from the volume helper (never Host blockers alone).
 * MissingPotRules = only when draw engaged (HasDrawRules); PageHead = synthesis counts
 * AttentionDrawer = T5 + Structure catalogue; birth denylist in attentionPartition.
 */

import { situationHref } from './situationRoutes';
import { organizerProblemPresentationCode } from './organizerProblemRead';
import type {
  CompetitionStageSummary,
  OrganizerProblemRead,
  OverviewSituation,
} from '../types';
import {
  structureDiagnosticMeta,
  structureDiagnosticTone,
  type StructureDiagnosticTone,
} from '../pages/structure/issues/structureDiagnosticPresentation';
import type { StructureDiagnosticSeverityContext } from '../pages/structure/issues/structureDiagnosticSeverity';
import { resolveStructureDiagnosticIssueTitleKey } from '../pages/structure/issues/structurePausedDiagnosticCopy';

/** Orthogonal signal tone (DS Alert / Status / toast mapping — design §6.3). */
export type FunctionalProblemTone = 'danger' | 'warning' | 'info' | 'attention';

function mapStructureTone(
  tone: StructureDiagnosticTone,
): FunctionalProblemTone {
  if (tone === 'progress') return 'info';
  return tone;
}
/**
 * Canal / surface SoT or echo — documents responsibility, not a router.
 * AttentionDrawer presents runtime only; construction stays Structure/Équipes.
 */
export type FunctionalProblemCanal =
  | 'structure-pagehead'
  | 'structure-topology'
  | 'structure-dialog'
  | 'overview-echo'
  | 'attention-drawer'
  | 'mutation'
  | 'draw-gate'
  | 'teams-plateau';

export type FunctionalProblemIdentity = {
  source: string;
  targetType?: string | null;
  targetId?: string | null;
  /** Host-resolved Match id when target is Fixture (Overview / Attention). */
  matchId?: string | null;
  competitionId: string;
  params?: Record<string, string> | null;
  /** Host Overview actionCode when known. */
  actionCode?: string | null;
  /** Optional stage for Structure deep-links (Qual/Prog editor). */
  stageId?: string | null;
  /** structureIssues child code when mapping Topology rows. */
  structureIssueCode?: string | null;
  /**
   * SlotFeedMissing Host context: draw engaged → silence UX (not shown via this helper
   * when callers filter). Without draw → danger.
   */
  awaitingDraw?: boolean;
  /** When `attention-drawer`, structure issues use short triage copy (enums). */
  canal?: FunctionalProblemCanal;
  structureDiagnosticContext?: StructureDiagnosticSeverityContext;
};

export type FunctionalProblemCta = {
  actionCode: string | null;
  /** Prefer Host actionCode; SPA may navigate via sotHref. */
};

export type FunctionalProblemPresentation = {
  tone: FunctionalProblemTone;
  /** i18next key (with `titleNs`). */
  titleKey: string;
  titleNs: string;
  descriptionParams?: Record<string, string | number>;
  sotHref: string | null;
  cta: FunctionalProblemCta | null;
  canal: FunctionalProblemCanal;
};

const CONSTRUCTION_BLOCKERS = new Set([
  'MissingStage',
  'MissingStructure',
  'MissingPotRules',
  'CupBracketInvalid',
  'ConfrontationUndecidable',
  'OverCapacityEdge',
  'OverExtraction',
]);

const RUNTIME_ATTENTION = new Set([
  'DrawNoSolution',
  'QualificationPending',
  'QualificationConflict',
  'ProgressionPending',
  'ProgressionConflict',
]);

const DRAW_CREATE_BLOCKS = new Set([
  'unsupported',
  'active',
  'emptyPool',
  'emptyPoolUpstream',
  'belowMinimumTeams',
  'missingPots',
  'groupShape',
  'countMismatch',
  'directAssignment',
  'occupiedSlots',
  'noFreeSlot',
  'notEnoughTeams',
  'tooManyTeams',
]);

function structureHref(competitionId: string, stageId?: string | null): string {
  if (stageId) {
    return `/competitions/${competitionId}/structure?stage=${encodeURIComponent(stageId)}`;
  }
  return `/competitions/${competitionId}/structure`;
}

function teamsHref(competitionId: string): string {
  return `/competitions/${competitionId}/teams`;
}

function structureDiagnosticPresentationFields(
  code: string,
  canal: FunctionalProblemCanal | undefined,
  context?: StructureDiagnosticSeverityContext,
): { tone: FunctionalProblemTone; titleKey: string; titleNs: string } {
  const tone = mapStructureTone(structureDiagnosticTone(code, context));
  const stageStatus = context?.stageStatus;
  const copyContext = {
    compositionEntryCount: context?.compositionEntryCount,
    compositionCapacity: context?.compositionCapacity,
  };
  const variant = canal === 'attention-drawer' ? 'short' : 'long';
  const titleKey = resolveStructureDiagnosticIssueTitleKey(
    code,
    stageStatus,
    copyContext,
    variant,
  );
  return { tone, titleKey, titleNs: 'structure' };
}

/**
 * Map a functional problem identity to presentation fields.
 * Callers compose DS Alert/Status/toast — this helper does not render.
 */
export function functionalProblemPresentation(
  identity: FunctionalProblemIdentity,
): FunctionalProblemPresentation {
  const {
    source,
    competitionId,
    params,
    actionCode,
    stageId,
    structureIssueCode,
    targetType,
    targetId,
    matchId,
    canal,
    structureDiagnosticContext,
  } = identity;

  const descriptionParams = params
    ? Object.fromEntries(
        Object.entries(params).map(([key, value]) => [key, value]),
      )
    : undefined;

  const stageFromTarget =
    stageId ?? (targetType === 'Stage' && targetId ? targetId : null);

  if (!structureIssueCode && structureDiagnosticMeta(source)) {
    return functionalProblemPresentation({
      source,
      targetType,
      targetId,
      matchId,
      competitionId,
      params,
      actionCode,
      stageId: stageFromTarget,
      structureIssueCode: source,
      canal,
      structureDiagnosticContext,
    });
  }

  // Topology / fiche child issue (structureIssues code)
  if (structureIssueCode) {
    const meta = structureDiagnosticMeta(structureIssueCode);
    if (meta) {
      const issueTitle = structureDiagnosticPresentationFields(
        structureIssueCode,
        canal,
        structureDiagnosticContext,
      );
      return {
        tone: issueTitle.tone,
        titleKey: issueTitle.titleKey,
        titleNs: issueTitle.titleNs,
        descriptionParams,
        sotHref: structureHref(competitionId, stageId),
        cta: { actionCode: actionCode ?? 'ConfigureStructure' },
        canal: canal ?? 'structure-topology',
      };
    }
  }

  if (canal === 'attention-drawer' && structureDiagnosticMeta(source)) {
    const fields = structureDiagnosticPresentationFields(
      source,
      canal,
      structureDiagnosticContext,
    );
    return {
      tone: fields.tone,
      titleKey: fields.titleKey,
      titleNs: fields.titleNs,
      descriptionParams,
      sotHref: structureHref(competitionId, stageFromTarget),
      cta: { actionCode: actionCode ?? 'ConfigureStructure' },
      canal: 'attention-drawer',
    };
  }

  if (source === 'StructureGraphInvalid') {
    return {
      tone: 'danger',
      titleKey: 'attentionSource.StructureGraphInvalid',
      titleNs: 'enums',
      descriptionParams,
      sotHref: structureHref(competitionId),
      cta: { actionCode: actionCode ?? 'ConfigureStructure' },
      canal: 'overview-echo',
    };
  }

  if (source === 'InsufficientParticipants') {
    return {
      tone: 'attention',
      titleKey: 'attentionSource.InsufficientParticipants',
      titleNs: 'enums',
      descriptionParams,
      sotHref: teamsHref(competitionId),
      cta: { actionCode: actionCode ?? 'AddEntry' },
      canal: 'teams-plateau',
    };
  }

  if (CONSTRUCTION_BLOCKERS.has(source)) {
    return {
      tone: structureDiagnosticMeta(source)
        ? mapStructureTone(structureDiagnosticTone(source))
        : 'danger',
      titleKey: `attentionSource.${source}`,
      titleNs: 'enums',
      descriptionParams,
      sotHref: structureHref(competitionId, stageId),
      cta: { actionCode: actionCode ?? 'ConfigureStructure' },
      canal: 'structure-pagehead',
    };
  }

  if (RUNTIME_ATTENTION.has(source)) {
    return {
      tone: source === 'DrawNoSolution' ? 'warning' : 'attention',
      titleKey: `attentionSource.${source}`,
      titleNs: 'enums',
      descriptionParams,
      sotHref: situationHref(
        {
          source,
          nature: 'Blocking',
          targetType: targetType ?? null,
          targetId: targetId ?? null,
          matchId: matchId ?? null,
          actionable: actionCode != null,
          actionCode: actionCode ?? null,
          impactCode: null,
          params: params ?? {},
        } satisfies OverviewSituation,
        competitionId,
      ),
      cta: actionCode ? { actionCode } : null,
      canal: 'attention-drawer',
    };
  }

  if (DRAW_CREATE_BLOCKS.has(source)) {
    // drawUi never-danger: journey gates stay info|warning.
    const warningReasons = new Set([
      'countMismatch',
      'belowMinimumTeams',
      'groupShape',
      'missingPots',
      'occupiedSlots',
      'directAssignment',
      'noFreeSlot',
      'notEnoughTeams',
      'tooManyTeams',
    ]);
    // Reuse Structure fiche i18n (no parallel draw.createBlock catalogue).
    const configEcho =
      source === 'missingPots'
        ? { actionCode: 'ConfigureDrawParams' as const }
        : source === 'belowMinimumTeams' || source === 'notEnoughTeams'
          ? { actionCode: 'AddEntry' as const }
          : null;
    return {
      tone: warningReasons.has(source) ? 'warning' : 'info',
      titleKey: `fiche.drawWorkflow.createBlocked.short.${source}`,
      titleNs: 'structure',
      descriptionParams,
      sotHref:
        source === 'belowMinimumTeams' || source === 'notEnoughTeams'
          ? teamsHref(competitionId)
          : structureHref(competitionId, stageId),
      cta: configEcho,
      canal: 'draw-gate',
    };
  }

  if (
    source.startsWith('QualIncomplete') ||
    source.startsWith('ProgIncomplete')
  ) {
    return {
      tone: 'warning',
      titleKey: source,
      titleNs: 'structure',
      descriptionParams,
      sotHref: null,
      cta: null,
      canal: 'structure-dialog',
    };
  }

  if (source.startsWith('PlacementIncomplete')) {
    return {
      tone: 'warning',
      titleKey: source,
      titleNs: 'structure',
      descriptionParams,
      sotHref: null,
      cta: null,
      canal: 'structure-dialog',
    };
  }

  // Fallback: mutation / unknown — prefer enums attentionSource when present.
  return {
    tone: 'danger',
    titleKey: `attentionSource.${source}`,
    titleNs: 'enums',
    descriptionParams,
    sotHref: situationHref(
      {
        source,
        nature: 'Blocking',
        targetType: targetType ?? null,
        targetId: targetId ?? null,
        matchId: matchId ?? null,
        actionable: actionCode != null,
        actionCode: actionCode ?? null,
        impactCode: null,
        params: params ?? {},
      } satisfies OverviewSituation,
      competitionId,
    ),
    cta: actionCode ? { actionCode } : null,
    canal: 'mutation',
  };
}

function structureDiagnosticContextFromStageSummary(
  stageId: string | null,
  stages?: readonly CompetitionStageSummary[],
): StructureDiagnosticSeverityContext | undefined {
  if (!stageId || !stages?.length) {
    return undefined;
  }
  const stage = stages.find((candidate) => candidate.stageId === stageId);
  if (!stage) {
    return undefined;
  }
  return { stageStatus: stage.status };
}

/** Convenience: Overview / Attention situation → presentation. */
export function situationPresentation(
  situation: OverviewSituation,
  competitionId: string,
  stages?: readonly CompetitionStageSummary[],
): FunctionalProblemPresentation {
  const stageIdFromParams =
    situation.params?.stageId?.trim() ||
    situation.params?.destinationStageId?.trim() ||
    (situation.targetType === 'Stage'
      ? situation.targetId?.trim()
      : undefined) ||
    null;
  return functionalProblemPresentation({
    source: situation.source,
    targetType: situation.targetType,
    targetId: situation.targetId,
    matchId: situation.matchId,
    competitionId,
    params: situation.params,
    actionCode: situation.actionCode,
    stageId: stageIdFromParams,
    canal: 'attention-drawer',
    structureDiagnosticContext: structureDiagnosticContextFromStageSummary(
      stageIdFromParams,
      stages,
    ),
  });
}

/** Convenience: Topology / fiche structureIssues row → presentation. */
export function structureIssuePresentation(input: {
  code: string;
  competitionId: string;
  stageId: string;
  awaitingDraw?: boolean;
  problem?: OrganizerProblemRead | null;
  structureDiagnosticContext?: StructureDiagnosticSeverityContext;
}): FunctionalProblemPresentation {
  const issueCode = organizerProblemPresentationCode(input.problem, input.code);
  return functionalProblemPresentation({
    source: 'StructureGraphInvalid',
    structureIssueCode: issueCode,
    competitionId: input.competitionId,
    stageId: input.stageId,
    awaitingDraw: input.awaitingDraw,
    actionCode: 'ConfigureStructure',
    structureDiagnosticContext: input.structureDiagnosticContext,
  });
}
