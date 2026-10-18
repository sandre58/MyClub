import type { StructureStageHubSummary, StructureView } from '../../types';
import {
  structureDiagnosticMeta,
  structureDiagnosticTone,
  type StructureDiagnosticTone,
} from './structureDiagnosticPresentation';
import { structureDiagnosticContextFromStage } from './structureDiagnosticContext';
import { stagePresentedIssueCodes } from './structureStagePresentedIssues';

function toneRank(tone: StructureDiagnosticTone): number {
  switch (tone) {
    case 'danger':
      return 3;
    case 'attention':
      return 2;
    case 'progress':
      return 1;
  }
}

/** Worst presented diagnostic tone on a topology tile (presentation contract §4). */
export function stageWorstPresentedDiagnosticTone(
  data: StructureView,
  stage: StructureStageHubSummary,
): StructureDiagnosticTone | null {
  if (stage.status === 'Completed') {
    return null;
  }

  const context = structureDiagnosticContextFromStage(data, stage);
  let worst: StructureDiagnosticTone | null = null;
  let rank = 0;

  for (const code of stagePresentedIssueCodes(data, stage)) {
    if (!structureDiagnosticMeta(code)) {
      continue;
    }
    const tone = structureDiagnosticTone(code, context);
    const nextRank = toneRank(tone);
    if (nextRank > rank) {
      rank = nextRank;
      worst = tone;
    }
  }

  return worst;
}
