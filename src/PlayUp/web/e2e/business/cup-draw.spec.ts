import { test } from '@playwright/test';
import {
  expectCupDrawAppliedOnStructure,
  findCupCompositionCompleteCompetition,
  getPrimaryStageId,
  runCupSlotDrawPublishAndApply,
} from './hostSeed';

/**
 * Host + PostgreSQL E2E — critical path #3 (draw publish/apply).
 * Seed: cup-composition-complete (16/16 entries, draw rules engaged).
 */
test.describe('Play’Up business (Host + PostgreSQL)', () => {
  test('Draw — créer, générer, publier et appliquer (Coupe composition complète)', async ({
    request,
  }) => {
    const competition = await findCupCompositionCompleteCompetition(request);
    const stageId = await getPrimaryStageId(request, competition.id);
    await runCupSlotDrawPublishAndApply(request, stageId);
    await expectCupDrawAppliedOnStructure(request, competition.id);
  });
});
