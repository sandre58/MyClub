import type { OverviewSituation } from '../types';
import { structureDiagnosticMeta } from '../pages/structure/issues/structureDiagnosticPresentation';
import {
  structureDeepLink,
  type StructureFocusId,
} from '../pages/structure/structureNavigation';
import type { StructureSectionId } from '../pages/structure/structureHubSections';

/**
 * Resolve an Overview navigationHint / situation target to an existing SPA route.
 * Uses Host-provided matchId when present — no Fixture → Match join in React.
 * Lives in shell/ so chrome (attention) and pages share routes without shell→pages.
 */
export function overviewTargetHref(target: {
  targetType: string | null | undefined;
  targetId: string | null | undefined;
  matchId?: string | null;
  stageId?: string | null;
  competitionId?: string | null;
}): string | null {
  const { targetType, targetId, matchId, stageId, competitionId } = target;

  if (matchId) {
    return `/matches/${matchId}`;
  }

  if (!targetType || !targetId) {
    return null;
  }

  switch (targetType) {
    case 'Stage':
      return competitionId
        ? structureDeepLink({ competitionId, stageId: targetId })
        : null;
    case 'Slot': {
      const stageFromSlot = targetId.split(':')[0];
      return stageFromSlot && competitionId
        ? structureDeepLink({ competitionId, stageId: stageFromSlot })
        : null;
    }
    case 'Fixture':
      // Without resolved matchId, fall back to stage matches or competition hub.
      if (stageId) {
        return `/stages/${stageId}/matches`;
      }
      return competitionId ? `/competitions/${competitionId}/matches` : null;
    case 'Match':
      return `/matches/${targetId}`;
    case 'Competition':
      return `/competitions/${targetId}`;
    case 'Structure':
      return `/competitions/${targetId}/structure`;
    case 'Draw':
      if (!stageId) {
        return null;
      }
      return competitionId
        ? structureDeepLink({ competitionId, stageId })
        : null;
    default:
      return null;
  }
}

const QUAL_PROG_RUNTIME_SOURCES = new Set([
  'QualificationPending',
  'QualificationConflict',
  'ProgressionPending',
  'ProgressionConflict',
]);

/**
 * Deep-link to the qualification / progression rails on Structure when the Host
 * signal targets a destination phase (not a materialised match row).
 */
export function runtimeQualificationProgressionHref(
  situation: OverviewSituation,
  competitionId: string,
): string | null {
  const { source, targetType, targetId, params } = situation;
  if (!QUAL_PROG_RUNTIME_SOURCES.has(source)) {
    return null;
  }
  if (targetType === 'Fixture' || targetType === 'Draw') {
    return null;
  }

  const fromParams =
    params?.destinationStageId?.trim() || params?.stageId?.trim() || null;
  const colonPrefix =
    targetType === 'Slot' || targetType === 'Group'
      ? (() => {
          if (!targetId) {
            return null;
          }
          const idx = targetId.indexOf(':');
          return idx >= 0 ? targetId.slice(0, idx).trim() : null;
        })()
      : null;
  const formStageId =
    targetType === 'Form' && targetId?.trim() ? targetId.trim() : null;

  const stageId = fromParams ?? colonPrefix ?? formStageId;
  if (!stageId) {
    return null;
  }

  const section: StructureSectionId = source.startsWith('Qualification')
    ? 'qualification'
    : 'progression';
  const focus: StructureFocusId = 'rail-attribution';

  return structureDeepLink({
    competitionId,
    stageId,
    section,
    focus,
  });
}

export function situationHref(
  situation: OverviewSituation,
  competitionId: string,
): string | null {
  if (situation.source === 'InsufficientParticipants') {
    return `/competitions/${competitionId}/teams`;
  }

  const stageId =
    situation.params?.stageId?.trim() ||
    (situation.targetType === 'Stage' ? situation.targetId?.trim() : null);

  if (situation.source === 'CupBracketInvalid' && stageId) {
    return structureDeepLink({
      competitionId,
      stageId,
      section: 'construction',
    });
  }

  const isStructureCatalogueSource =
    Boolean(structureDiagnosticMeta(situation.source)) ||
    situation.source === 'CupBracketInvalid' ||
    situation.source === 'MissingPotRules';

  if (isStructureCatalogueSource) {
    return `/competitions/${competitionId}/structure${
      stageId ? `?stage=${encodeURIComponent(stageId)}` : ''
    }`;
  }

  const runtimeStructure = runtimeQualificationProgressionHref(
    situation,
    competitionId,
  );
  if (runtimeStructure) {
    return runtimeStructure;
  }

  return (
    overviewTargetHref({
      targetType: situation.targetType,
      targetId: situation.targetId,
      matchId: situation.matchId,
      competitionId,
    }) ??
    (situation.targetType === 'Fixture'
      ? `/competitions/${competitionId}/matches`
      : null)
  );
}
