import { expect, type Page, test } from '@playwright/test';

/**
 * Visual baselines for the design surfaces (no Host).
 * Linux snapshots only — same Chromium as CI (`playwright install --with-deps chromium`).
 * Regenerate: docs/audit/critical-paths.md
 */

async function settleVisualSurface(page: Page): Promise<void> {
  await page.addStyleTag({
    content: `
      *, *::before, *::after {
        animation: none !important;
        transition: none !important;
        caret-color: transparent !important;
        scroll-behavior: auto !important;
      }
      * { scrollbar-width: none !important; }
      *::-webkit-scrollbar { display: none !important; }
    `,
  });

  await page.evaluate(async () => {
    await document.fonts.ready;
    await document.fonts.load('400 16px "IBM Plex Sans"');
    await document.fonts.load('600 16px "IBM Plex Sans"');
    await document.fonts.load('700 16px "IBM Plex Sans"');
    await Promise.all(
      [...document.images].map(async (img) => {
        if (img.complete && img.naturalWidth > 0) {
          return;
        }
        try {
          await img.decode();
        } catch {
          // Broken or empty images stay as painted.
        }
      }),
    );
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }
  });
}

test.describe('Visual baselines', () => {
  test('design-lab', async ({ page }) => {
    await page.goto('/design-lab');
    await expect(page.getByText('Design Lab', { exact: true })).toBeVisible();
    await expect(page.locator('[data-nav-ready="true"]')).toBeVisible();
    await expect(page.getByText('Où en est-on ?')).toBeVisible();
    await settleVisualSurface(page);

    await expect(page).toHaveScreenshot('design-lab.png', {
      mask: [page.locator('.ds-live-dot')],
    });
  });

  test('foundations', async ({ page }) => {
    await page.goto('/dev/foundations');
    await expect(
      page.getByRole('heading', { name: /lockup Accueil/ }),
    ).toBeVisible();
    await settleVisualSurface(page);

    await expect(page).toHaveScreenshot('foundations.png', {
      mask: [page.locator('.ds-live-dot')],
    });
  });
});
