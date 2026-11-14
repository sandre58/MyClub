import { expect, test, devices } from '@playwright/test';

import {
  findFreshnessActionsRecetteCompetition,
  getStageIdByName,
} from './hostSeed';

test.use({ ...devices['Pixel 7'] });

/**
 * Host + PostgreSQL — tactile hit target for freshness badges.
 */
test('Structure — zone de tap du badge sans activer la case', async ({
  page,
  request,
}) => {
  const competition = await findFreshnessActionsRecetteCompetition(request);
  const stageId = await getStageIdByName(request, competition.id, 'A jouer');

  await page.goto(`/competitions/${competition.id}/structure?stage=${stageId}`);
  await expect(page.getByRole('heading', { name: 'Structure' })).toBeVisible();

  const qualifiers = page.locator('.structure-composition-qualifiers');
  const qualifierBadge = qualifiers.getByRole('button', {
    name: /Nord.*Libérer la place/,
  });
  await expect(qualifierBadge).toHaveCount(1);
  const qualifierHitBox = await qualifierBadge.boundingBox();
  expect(qualifierHitBox).not.toBeNull();
  expect(qualifierHitBox!.width).toBeGreaterThanOrEqual(28);
  expect(qualifierHitBox!.height).toBeGreaterThanOrEqual(28);

  const qualifierVisual = qualifierBadge.locator('svg').first();
  const qualifierVisualBox = await qualifierVisual.boundingBox();
  expect(qualifierVisualBox).not.toBeNull();
  expect(qualifierVisualBox!.width).toBeLessThanOrEqual(20);
  expect(qualifierVisualBox!.height).toBeLessThanOrEqual(20);

  const slot = page.locator('.schematic-slot').filter({ hasText: 'Nord' });
  await expect(
    slot.locator('.schematic-slot__name-row .schematic-slot__freshness'),
  ).toBeVisible();
  const slotBadge = slot.getByRole('button', { name: /Libérer la place/ });
  const slotHitBox = await slotBadge.boundingBox();
  expect(slotHitBox).not.toBeNull();
  expect(slotHitBox!.width).toBeGreaterThanOrEqual(24);
  expect(slotHitBox!.height).toBeGreaterThanOrEqual(24);

  const slotVisual = slotBadge.locator('svg').first();
  const slotVisualBox = await slotVisual.boundingBox();
  expect(slotVisualBox).not.toBeNull();
  expect(slotVisualBox!.width).toBeLessThanOrEqual(20);
  expect(slotVisualBox!.height).toBeLessThanOrEqual(20);

  await slotBadge.tap();
  await expect(
    page.getByRole('dialog', { name: 'Libérer cette place ?' }),
  ).toBeVisible();
  await expect(
    page.getByRole('dialog', { name: /Placement manuel/ }),
  ).toHaveCount(0);

  const tableauId = await getStageIdByName(request, competition.id, 'Tableau');
  await page.goto(
    `/competitions/${competition.id}/structure?stage=${tableauId}`,
  );
  const cupSlot = page
    .locator('.schematic-cup .schematic-slot')
    .filter({ has: page.locator('.schematic-slot__address') })
    .first();
  await expect(cupSlot).toBeVisible();
  const addressFitsSlot = await cupSlot.evaluate((el) => {
    const address = el.querySelector('.schematic-slot__address');
    if (!address) {
      return true;
    }
    const slotRect = el.getBoundingClientRect();
    const addressRect = address.getBoundingClientRect();
    return (
      addressRect.top >= slotRect.top - 1 &&
      addressRect.bottom <= slotRect.bottom + 1
    );
  });
  expect(addressFitsSlot).toBe(true);
});
