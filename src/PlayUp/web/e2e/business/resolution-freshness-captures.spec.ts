import { expect, test } from '@playwright/test';

import {
  findFreshnessActionsRecetteCompetition,
  getStageIdByName,
} from './hostSeed';

const artifactsDir = process.env.PLAYUP_E2E_ARTIFACTS?.trim();

test.describe('Recette captures (opt-in)', () => {
  test.skip(!artifactsDir, 'Set PLAYUP_E2E_ARTIFACTS to enable capture specs');

  test('recette5 screenshots', async ({ page, request }) => {
    const competition = await findFreshnessActionsRecetteCompetition(request);
    const avalId = await getStageIdByName(request, competition.id, 'Aval');
    const poulesId = await getStageIdByName(request, competition.id, 'Poules');
    const aJouerId = await getStageIdByName(request, competition.id, 'A jouer');
    await page.goto(
      `/competitions/${competition.id}/structure?stage=${aJouerId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    await page
      .locator('.schematic-slot')
      .filter({ hasText: 'Nord' })
      .getByRole('button', {
        name: /Non confirmée — .*le match.*n'est pas joué\. Libérer la place/,
      })
      .click();
    const releaseDialog = page.getByRole('dialog', {
      name: 'Libérer cette place ?',
    });
    await expect(releaseDialog).toBeVisible();
    await releaseDialog.screenshot({
      path: `${artifactsDir}/recette5-release-dialog-one-row.png`,
    });

    const manuelId = await getStageIdByName(request, competition.id, 'Manuel');
    await page.goto(
      `/competitions/${competition.id}/structure?stage=${manuelId}`,
    );
    await page.getByRole('button', { name: 'Placer manuellement' }).click();
    const placementDialog = page.getByRole('dialog', {
      name: /Placement manuel/,
    });
    await expect(placementDialog).toBeVisible();
    await placementDialog.screenshot({
      path: `${artifactsDir}/recette5-placement-dialog-cancel-row.png`,
    });
    await placementDialog.getByRole('button', { name: 'Annuler' }).click();

    await page.goto(
      `/competitions/${competition.id}/classements?stage=${poulesId}`,
    );
    await expect(page.getByTestId('standings-tables')).toBeVisible();
    await expect(
      page.locator(`[id="standings-panel-${poulesId}:Overall"]`),
    ).toHaveClass(/classements-panel--focus/);
    await page.waitForTimeout(2_800);
    await expect(
      page.locator(`[id="standings-panel-${poulesId}:Overall"]`),
    ).not.toHaveClass(/classements-panel--focus/);
    await page.screenshot({
      path: `${artifactsDir}/recette5-classements-after-focus.png`,
      fullPage: false,
    });

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${avalId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    await page.getByRole('button', { name: /À traiter/ }).click();
    const drawer = page.getByRole('dialog', { name: /À traiter/ });
    await expect(drawer).toBeVisible();
    await expect(drawer.getByText(/Conflit de qualification/)).toHaveCount(0);
    await drawer.screenshot({
      path: `${artifactsDir}/recette5-attention-drawer-coalesced.png`,
    });
    await page.getByRole('button', { name: 'Fermer À traiter' }).click();
  });
});
