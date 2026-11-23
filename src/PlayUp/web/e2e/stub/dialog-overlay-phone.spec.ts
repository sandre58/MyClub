import { expect, test } from '@playwright/test';
import { IDS, stubPlayUpApi } from './stubApi';

async function expectDialogAboveHeader(page: import('@playwright/test').Page) {
  const header = page.locator('.ds-shell-header');
  await expect(header).toBeVisible();

  const hit = await header.evaluate((el) => {
    const rect = el.getBoundingClientRect();
    const x = rect.left + rect.width / 2;
    const y = rect.top + rect.height / 2;
    const target = document.elementFromPoint(x, y);
    if (!target) {
      return { ok: false, reason: 'no element' };
    }
    const dialogHit =
      target.closest('.ds-dialog__backdrop') != null ||
      target.closest('.ds-dialog__panel') != null ||
      target.closest('.ds-dialog') != null;
    const chromeHit =
      target.closest('.ds-shell-header') != null ||
      target.closest('.ds-shell-rail') != null ||
      target.closest('.shell-nav-backdrop') != null;
    return {
      ok: dialogHit && !chromeHit,
      tag: target.tagName,
      className: target.className,
    };
  });

  expect(hit.ok, JSON.stringify(hit)).toBe(true);
}

async function openTeamsAddDialog(page: import('@playwright/test').Page) {
  await page.goto(`/competitions/${IDS.competitionId}/teams`);
  await expect(page.locator('.shell[data-shell-vp="phone"]')).toBeVisible({
    timeout: 10_000,
  });
  await page.getByRole('button', { name: /Ajouter une équipe/i }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
}

test.describe('Dialog overlay above shell chrome (stub)', () => {
  test.beforeEach(async ({ page }) => {
    await stubPlayUpApi(page);
    await page.setViewportSize({ width: 390, height: 844 });
  });

  test('phone — dialog paints above the header', async ({ page }) => {
    await openTeamsAddDialog(page);
    await expectDialogAboveHeader(page);

    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();

    const opener = page.getByRole('button', { name: /Ajouter une équipe/i });
    await page.keyboard.press('Escape');
    await expect(dialog).not.toBeVisible();
    await expect(opener).toBeFocused();
  });

  test('phone — dialog above header and rail after the menu was opened', async ({
    page,
  }) => {
    await page.goto(`/competitions/${IDS.competitionId}/teams`);
    await expect(page.locator('.shell[data-shell-vp="phone"]')).toBeVisible();

    await page
      .getByRole('button', { name: /Ouvrir le menu de navigation/i })
      .click();
    await expect(page.locator('.shell[data-nav-open="true"]')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.locator('.shell[data-nav-open="false"]')).toBeVisible();

    await page.getByRole('button', { name: /Ajouter une équipe/i }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await expectDialogAboveHeader(page);

    const railZoneHit = await page.evaluate(() => {
      const target = document.elementFromPoint(12, window.innerHeight / 2);
      return (
        target?.closest(
          '.ds-dialog__backdrop, .ds-dialog__panel, .ds-dialog',
        ) != null && target?.closest('.ds-shell-rail') == null
      );
    });
    expect(railZoneHit).toBe(true);
  });

  test('phone — Tab stays inside the dialog', async ({ page }) => {
    await openTeamsAddDialog(page);
    const dialog = page.getByRole('dialog');
    const nameField = dialog.getByRole('textbox').first();
    await expect(nameField).toBeVisible();
    await nameField.click();

    for (let i = 0; i < 12; i++) {
      await page.keyboard.press('Tab');
      const inside = await page.evaluate(() => {
        const active = document.activeElement;
        return active?.closest('.ds-dialog__panel') != null;
      });
      expect(inside).toBe(true);
    }
  });

  test('desktop — dialog paints above the header', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto(`/competitions/${IDS.competitionId}/teams`);
    await expect(page.locator('.shell[data-shell-vp="desktop"]')).toBeVisible();
    await page.getByRole('button', { name: /Ajouter une équipe/i }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await expectDialogAboveHeader(page);
  });

  test('Accueil create dialog is portaled to body', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('button', { name: /Créer une compétition/i }).click();
    await expect(page.getByRole('dialog')).toBeVisible();

    const portaled = await page.evaluate(() => {
      const root = document.querySelector('.ds-dialog');
      return root?.parentElement === document.body;
    });
    expect(portaled).toBe(true);
  });
});
