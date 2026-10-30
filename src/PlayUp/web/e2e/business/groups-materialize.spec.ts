import { expect, test } from '@playwright/test';
import {
  ensureGroupsMaterializePending,
  findRecetteG1GroupsCompetition,
  getStructureMaterializationState,
  runGroupsMaterializeMatches,
} from './hostSeed';

/**
 * Host + PostgreSQL E2E — critical path #2 (Prepare / materialize).
 * Seed: structure-materialize-recette (RECETTE G1 — 0/6 matches attached).
 * API and UI specs share that competition; each one restores 0 attached matches first.
 */
test.describe('Play’Up business (Host + PostgreSQL)', () => {
  test.describe.configure({ mode: 'serial' });

  test.beforeEach(async ({ request }) => {
    await ensureGroupsMaterializePending(request);
  });

  test('Materialize — groupes RECETTE G1 (0 → 6 matchs)', async ({
    request,
  }) => {
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

  test('Materialize — groupes RECETTE G1 depuis l’interface (0 → 6 matchs)', async ({
    page,
    request,
  }) => {
    const competition = await findRecetteG1GroupsCompetition(request);
    await page.goto(`/competitions/${competition.id}/structure`);

    const matches = page.getByRole('region', { name: 'Matchs' });
    await expect(matches.getByText('Il reste 6 matchs à créer.')).toBeVisible();
    await matches
      .getByRole('button', { name: 'Créer les matchs', exact: true })
      .click();

    await expect(
      matches.getByText('Tous les matchs prévus sont créés.'),
    ).toBeVisible();
    await expect(
      matches.locator('.structure-operational-strip__ratio-filled'),
    ).toHaveText('6');
    await expect(
      matches.locator('.structure-operational-strip__ratio-cap'),
    ).toHaveText('6');
  });
});
