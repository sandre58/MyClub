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

    const tableauId = await getStageIdByName(
      request,
      competition.id,
      'Tableau',
    );
    await page.goto(
      `/competitions/${competition.id}/structure?stage=${tableauId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    await page.getByRole('button', { name: /À traiter/ }).click();
    const drawer = page.getByRole('dialog', { name: /À traiter/ });
    await expect(drawer).toBeVisible();
    await expect(drawer.getByText(/Conflit de qualification/)).toHaveCount(0);
    await expect(drawer.getByText(/autre équipe/)).toBeVisible();
    await drawer.screenshot({
      path: `${artifactsDir}/m5-attention-drawer-coalesced-slot.png`,
    });
    await drawer
      .getByRole('link', { name: /autre équipe|different team/i })
      .click();
    await expect(page).toHaveURL(/focus=operational-placement/);
    await expect(page.locator('.schematic-slot').first()).toBeVisible();
    await page.screenshot({
      path: `${artifactsDir}/m5-structure-place-focus.png`,
      fullPage: false,
    });

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${aJouerId}`,
    );
    await page
      .locator('.schematic-slot')
      .filter({ hasText: 'Nord' })
      .getByRole('button', {
        name: /Non confirmée — .*le match.*n'est pas joué\. Libérer la place/,
      })
      .click();
    const releaseWithErrorDialog = page.getByRole('dialog', {
      name: 'Libérer cette place ?',
    });
    await expect(releaseWithErrorDialog).toBeVisible();
    const longMessage =
      'This release could not be completed because the upstream qualification path, bracket pairing, and match schedule are inconsistent with the current place assignment and regulation checksum.';
    await page.route('**/stages/*/resolution/release', async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({
            title: 'Conflict',
            detail: longMessage,
            status: 409,
          }),
        });
        return;
      }
      await route.continue();
    });
    await releaseWithErrorDialog
      .getByRole('button', { name: 'Libérer', exact: true })
      .click();
    await expect(releaseWithErrorDialog.getByText(longMessage)).toBeVisible();
    await releaseWithErrorDialog.screenshot({
      path: `${artifactsDir}/m5-release-dialog-long-footer-error.png`,
    });
    await releaseWithErrorDialog
      .getByRole('button', { name: 'Annuler' })
      .click();
  });
});
