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
    name: /Libérer la place/,
  });
  const qualifierBox = await qualifierBadge.boundingBox();
  expect(qualifierBox).not.toBeNull();
  expect(qualifierBox!.width).toBeGreaterThanOrEqual(28);
  expect(qualifierBox!.height).toBeGreaterThanOrEqual(28);

  const slot = page.locator('.schematic-slot').filter({ hasText: 'Nord' });
  const slotBadge = slot.getByRole('button', { name: /Libérer la place/ });
  const slotBox = await slotBadge.boundingBox();
  expect(slotBox).not.toBeNull();
  expect(slotBox!.width).toBeGreaterThanOrEqual(24);
  expect(slotBox!.height).toBeGreaterThanOrEqual(24);

  await slotBadge.tap();
  await expect(
    page.getByRole('dialog', { name: 'Libérer cette place ?' }),
  ).toBeVisible();
  await expect(
    page.getByRole('dialog', { name: /Placement manuel/ }),
  ).toHaveCount(0);
});
