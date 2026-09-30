import { expect, test } from '@playwright/test';
import { IDS, stubPlayUpApi } from './stubApi';

/**
 * Phase F — minimal smoke suite (FR labels / roles).
 * API stubbed — see stubApi.ts. Host optional for local product smoke only.
 */
test.describe('Play’Up smoke', () => {
  test.beforeEach(async ({ page }) => {
    await stubPlayUpApi(page);
  });

  test('Accueil affiche la marque et la liste des compétitions', async ({
    page,
  }) => {
    await page.goto('/');

    await expect(
      page.getByRole('heading', { name: /Play’Up|Play'Up/i }),
    ).toBeVisible();
    await expect(
      page.getByRole('button', { name: /Créer une compétition/i }),
    ).toBeVisible();
    await expect(
      page.getByRole('list', { name: /Compétitions/i }),
    ).toBeVisible();
    await expect(page.getByRole('link', { name: /Spring Cup/i })).toBeVisible();
  });

  test('Overview — navigation depuis Accueil (pas de refonte)', async ({
    page,
  }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /Spring Cup/i }).click();

    await expect(page).toHaveURL(
      new RegExp(`/competitions/${IDS.competitionId}$`),
    );
    await expect(
      page.getByRole('navigation', { name: /Navigation principale/i }),
    ).toBeVisible();
    await expect(
      page.getByRole('link', { name: /Vue d'ensemble/i }),
    ).toBeVisible();
    // Region present — Overview UX stays pre-refonte; assert shell reachability only.
    await expect(page.locator('.overview')).toBeVisible();
  });

  test('Structure — ouvrir une fiche phase', async ({ page }) => {
    await page.goto(`/competitions/${IDS.competitionId}/structure`);

    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();

    const phase = page.getByRole('button', { name: /League/i });
    await expect(phase).toBeVisible();
    await phase.click();

    await expect(page.getByRole('heading', { name: 'League' })).toBeVisible();
    await expect(page.getByRole('heading', { name: /^Match$/i })).toBeVisible();
  });

  test('Match — démarrer puis terminer', async ({ page }) => {
    await page.goto(`/matches/${IDS.matchId}`);

    await expect(page.getByText('Planifié')).toBeVisible();
    await page.getByRole('button', { name: 'Démarrer le match' }).click();
    await expect(page.getByText('En direct')).toBeVisible();

    await page.getByLabel(/Alpha — résultat/i).fill('2');
    await page.getByLabel(/Beta — résultat/i).fill('1');
    await page.getByRole('button', { name: 'Terminer le match' }).click();

    await expect(page.getByText('Terminé')).toBeVisible();
  });
});
