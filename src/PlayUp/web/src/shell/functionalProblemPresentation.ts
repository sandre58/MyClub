/**
 * Thin presentation mapping for functional problems (Phase 1 contrat).
 * Reuses existing i18n — no parallel catalogue, no Error Framework.
 *
 * Identity aligns with Overview DeduplicateSituations: source + targetType + targetId.
 */

import { situationHref } from './situationRoutes';
import type { OverviewSituation } from '../types';

/** Orthogonal signal tone (DS Alert / Status / toast mapping — design §6.3). */
export type FunctionalProblemTone = 'danger' | 'warning' | 'info' | 'attention';

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
]);

const STRUCTURE_GRAPH_ISSUES = new Set([
  'DanglingQualificationTarget',
  'DanglingProgressionTarget',
  'MissingQualificationDestinationSlot',
  'MissingQualificationDestinationGroup',
  'MissingProgressionDestinationSlot',
  'MissingProgressionDestinationGroup',
  'SlotFeedsInvalid',
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
  } = identity;

  const descriptionParams = params
    ? Object.fromEntries(
        Object.entries(params).map(([key, value]) => [key, value]),
      )
    : undefined;

  // Topology child issue (structureIssues code)
  if (structureIssueCode && STRUCTURE_GRAPH_ISSUES.has(structureIssueCode)) {
    return {
      tone: 'danger',
      titleKey: `graph.issues.${structureIssueCode}`,
      titleNs: 'structure',
      descriptionParams,
      sotHref: structureHref(competitionId, stageId),
      cta: { actionCode: actionCode ?? 'ConfigureStructure' },
      canal: 'structure-topology',
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
      tone: 'attention',
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
    ]);
    // Reuse Structure fiche i18n (no parallel draw.createBlock catalogue).
    const configEcho =
      source === 'missingPots'
        ? { actionCode: 'ConfigureDrawParams' as const }
        : source === 'belowMinimumTeams'
          ? { actionCode: 'AddEntry' as const }
          : null;
    return {
      tone: warningReasons.has(source) ? 'warning' : 'info',
      titleKey: `fiche.drawWorkflow.createBlocked.short.${source}`,
      titleNs: 'structure',
      descriptionParams,
      sotHref:
        source === 'belowMinimumTeams'
          ? teamsHref(competitionId)
          : structureHref(competitionId, stageId),
      cta: configEcho,
      canal: 'draw-gate',
    };
  }

  if (source.startsWith('QualIncomplete') || source.startsWith('ProgIncomplete')) {
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

/** Convenience: Overview / Attention situation → presentation. */
export function situationPresentation(
  situation: OverviewSituation,
  competitionId: string,
): FunctionalProblemPresentation {
  return functionalProblemPresentation({
    source: situation.source,
    targetType: situation.targetType,
    targetId: situation.targetId,
    matchId: situation.matchId,
    competitionId,
    params: situation.params,
    actionCode: situation.actionCode,
  });
}

/** Convenience: Topology structureIssues row → presentation. */
export function structureIssuePresentation(input: {
  code: string;
  competitionId: string;
  stageId: string;
}): FunctionalProblemPresentation {
  return functionalProblemPresentation({
    source: 'StructureGraphInvalid',
    structureIssueCode: input.code,
    competitionId: input.competitionId,
    stageId: input.stageId,
    actionCode: 'ConfigureStructure',
  });
}
