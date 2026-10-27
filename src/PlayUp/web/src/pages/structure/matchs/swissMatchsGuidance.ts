import type { TFunction } from 'i18next';

import type { StructureStageHubSummary } from '../../../types';
import { swissRoundCounts } from './swissRoundCounts';

export type SwissMatchsGuidanceKind = 'A' | 'B' | 'C' | 'D' | 'E' | 'neutral';

export type SwissMatchsGuidanceView = {
  kind: SwissMatchsGuidanceKind;
  messageKey: string;
  messageParams?: Record<string, unknown>;
  plannedHintKey?: string;
};

/**
 * Presentation-only Swiss sport guidance for the Structure Matchs column.
 * Does not evaluate GenerateNextRound readiness or Overview blockers.
 */
export function resolveSwissMatchsGuidance(
  stage: StructureStageHubSummary,
): SwissMatchsGuidanceView | null {
  if (stage.formatKind !== 'Swiss') {
    return null;
  }

  const { generated: n, planned: plannedNullable } = swissRoundCounts(stage);
  const K = plannedNullable ?? 0;
  const status = stage.status;

  if (status === 'Draft' || status === 'Completed') {
    return null;
  }

  if (K > 0 && n > K) {
    return { kind: 'neutral', messageKey: 'fiche.swissGuidance.neutral' };
  }

  const progress = stage.lifecycle?.matchProgress;
  const open = progress?.open ?? 0;
  const total = progress?.total ?? 0;
  const finished = progress?.finished ?? 0;

  if (status === 'Ready' && n === 0) {
    return {
      kind: 'A',
      messageKey: 'fiche.swissGuidance.readyNoRounds',
      messageParams: K > 0 ? { planned: K } : undefined,
      plannedHintKey:
        K > 0 ? 'fiche.swissGuidance.plannedRoundsHint' : undefined,
    };
  }

  if ((status === 'Running' || status === 'Suspended') && n === 0) {
    return {
      kind: 'B',
      messageKey: 'fiche.swissGuidance.runningNoRounds',
    };
  }

  if (status !== 'Running' && status !== 'Suspended') {
    return null;
  }

  if (K > 0 && n >= K) {
    return {
      kind: 'E',
      messageKey: 'fiche.swissGuidance.allRoundsGenerated',
      messageParams: { planned: K },
    };
  }

  if (n > 0 && K > 0 && n < K) {
    if (open === 0 && total > 0 && finished === 0) {
      return { kind: 'neutral', messageKey: 'fiche.swissGuidance.neutral' };
    }
    if (open > 0) {
      return {
        kind: 'C',
        messageKey: 'fiche.swissGuidance.awaitingRoundResults',
      };
    }
    if (open === 0 && total > 0) {
      return {
        kind: 'D',
        messageKey: 'fiche.swissGuidance.roundCompleteMoreToGenerate',
      };
    }
    return { kind: 'neutral', messageKey: 'fiche.swissGuidance.neutral' };
  }

  return null;
}

export function formatSwissMatchProgressCaption(
  t: TFunction<'structure'>,
  caption: SwissMatchsGuidanceView,
): string {
  let text = t(caption.messageKey, caption.messageParams);
  if (caption.plannedHintKey) {
    text = `${text} ${t(caption.plannedHintKey, caption.messageParams)}`;
  }
  return text;
}
