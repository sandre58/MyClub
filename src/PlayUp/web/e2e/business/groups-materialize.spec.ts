import { test } from '@playwright/test';
import {
  findRecetteG1GroupsCompetition,
  getStructureMaterializationState,
  runGroupsMaterializeMatches,
} from './hostSeed';

/**
 * Host + PostgreSQL E2E — critical path #2 (Prepare / materialize).
 * Seed: structure-materialize-recette (RECETTE G1 — 0/6 matches attached).
 */
test.describe('Play’Up business (Host + PostgreSQL)', () => {
  test('Materialize — groupes RECETTE G1 (0 → 6 matchs)', async ({ request }) => {
    const competition = await findRecetteG1GroupsCompetition(request);
    const before = await getStructureMaterializationState(
      request,
      competition.id,
    );

    if (before.attachedMatchCount !== 0) {
      throw new Error(
        `Expected 0 attached matches before materialize (RECETTE G1), got ${before.attachedMatchCount}`,
      );
    }

    const result = await runGroupsMaterializeMatches(request, before.stageId);
    if (result.createdCount <= 0 || result.alreadyComplete) {
      throw new Error(
        `Materialize should create matches (createdCount=${result.createdCount}, alreadyComplete=${result.alreadyComplete})`,
      );
    }

    const after = await getStructureMaterializationState(
      request,
      competition.id,
    );
    if (after.attachedMatchCount !== 6) {
      throw new Error(
        `Expected 6 attached matches after full G1 materialize, got ${after.attachedMatchCount}`,
      );
    }
  });
});
