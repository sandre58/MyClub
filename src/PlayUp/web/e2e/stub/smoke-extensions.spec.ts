import { expect, test } from '@playwright/test';
import { IDS, stubPlayUpApi } from './stubApi';

/** Stub-only extensions for shell / overview signals (paths 5–6). */
test.describe('Play’Up smoke extensions (stub API)', () => {
  test('Shell — ouvrir le tiroir À traiter', async ({ page }) => {
    await stubPlayUpApi(page, { withAttentionItem: true });
    await page.goto(`/competitions/${IDS.competitionId}`);

    await page
      .getByRole('button', { name: 'À traiter, 1 élément' })
      .click();

    await expect(
      page.getByRole('dialog', { name: /À traiter/ }),
    ).toBeVisible();
    await expect(
      page.getByRole('link', { name: /Progression en attente/i }),
    ).toBeVisible();
    await expect(
      page.locator('.shell-attention-drawer__row-meta').filter({
        hasText: 'League',
      }),
    ).toBeVisible();
  });

  test('Overview — Prochaine action (PrepareStage)', async ({ page }) => {
    await stubPlayUpApi(page, { withOverviewProgression: true });
    await page.goto(`/competitions/${IDS.competitionId}`);

    await expect(
      page.getByRole('heading', { name: 'Prochaine action' }),
    ).toBeVisible();
    await expect(
      page.getByText(
        /Finalisez la configuration de la phase avant les opérations sportives/i,
      ),
    ).toBeVisible();
  });
});
