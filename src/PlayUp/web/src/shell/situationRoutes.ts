import type { OverviewSituation } from '../types';
import { structureDiagnosticMeta } from '../pages/structure/issues/structureDiagnosticPresentation';
import { structureDeepLink } from '../pages/structure/structureNavigation';

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
      return `/stages/${targetId}`;
    case 'Slot': {
      const stageFromSlot = targetId.split(':')[0];
      return stageFromSlot ? `/stages/${stageFromSlot}` : null;
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
        ? `/competitions/${competitionId}/structure?stage=${encodeURIComponent(stageId)}`
        : `/stages/${stageId}`;
    default:
      return null;
  }
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
