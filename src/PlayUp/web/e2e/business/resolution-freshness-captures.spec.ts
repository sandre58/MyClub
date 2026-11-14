import { expect, test } from '@playwright/test';

import {
  findFreshnessActionsRecetteCompetition,
  getStageIdByName,
} from './hostSeed';

const out = '/opt/cursor/artifacts';

test.describe('Recette 4 captures (local only)', () => {
  test.skip(!!process.env.CI, 'Skipped in CI');

  test('recette4 screenshots', async ({ page, request }) => {
    const competition = await findFreshnessActionsRecetteCompetition(request);
    const avalId = await getStageIdByName(request, competition.id, 'Aval');
    const poulesId = await getStageIdByName(request, competition.id, 'Poules');
    const aJouerId = await getStageIdByName(request, competition.id, 'A jouer');

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${avalId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    const upstream = page.locator('.schematic-slot').getByRole('link', {
      name: /Périmé.*Phase amont périmée.*Aller à la phase amont/,
    });
    await expect(upstream.locator('.lucide-corner-left-up')).toBeVisible();
    await upstream.hover();
    await page.waitForTimeout(350);
    await page.screenshot({
      path: `${out}/recette4-upstream-icon-tooltip.png`,
    });

    await page.goto(
      `/competitions/${competition.id}/classements?stage=${poulesId}`,
    );
    await expect(page.getByTestId('standings-tables')).toBeVisible();
    await expect(
      page.locator(`[id="standings-panel-${poulesId}:Overall"]`),
    ).toHaveClass(/classements-panel--focus/);
    await page.screenshot({
      path: `${out}/recette4-classements-stage-focus.png`,
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
    const dialog = page.getByRole('dialog', {
      name: 'Libérer cette place ?',
    });
    await expect(dialog).toBeVisible();
    await dialog.screenshot({
      path: `${out}/recette4-release-dialog-three-buttons.png`,
    });
  });
});
