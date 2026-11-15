import { expect, test, devices, type Locator } from '@playwright/test';

import {
  findFreshnessActionsRecetteCompetition,
  getStageIdByName,
} from './hostSeed';
import { expectCupSlotsTextVisible } from './cupSlotTextFit';

test.use({ ...devices['Pixel 7'] });

type FreshnessChromeSize = {
  hitW: number;
  hitH: number;
  paintW: number;
  paintH: number;
};

async function measureFreshnessChrome(
  control: Locator,
): Promise<FreshnessChromeSize> {
  return control.evaluate((el) => {
    const cs = getComputedStyle(el);
    const padX = parseFloat(cs.paddingLeft) + parseFloat(cs.paddingRight);
    const padY = parseFloat(cs.paddingTop) + parseFloat(cs.paddingBottom);
    const hit = el.getBoundingClientRect();
    return {
      hitW: hit.width,
      hitH: hit.height,
      paintW: hit.width - padX,
      paintH: hit.height - padY,
    };
  });
}

function expectPaintNear20(size: FreshnessChromeSize) {
  expect(size.paintW).toBeGreaterThanOrEqual(19);
  expect(size.paintW).toBeLessThanOrEqual(21);
  expect(size.paintH).toBeGreaterThanOrEqual(19);
  expect(size.paintH).toBeLessThanOrEqual(21);
}

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
  const qualifierChrome = await measureFreshnessChrome(qualifierBadge);
  expect(qualifierChrome.hitW).toBeGreaterThanOrEqual(28);
  expect(qualifierChrome.hitH).toBeGreaterThanOrEqual(28);
  expectPaintNear20(qualifierChrome);

  const tableauId = await getStageIdByName(request, competition.id, 'Tableau');
  const barrageId = await getStageIdByName(request, competition.id, 'Barrage');

  await page.goto(
    `/competitions/${competition.id}/structure?stage=${tableauId}`,
  );
  const teteSlot = page
    .locator('.schematic-cup .schematic-slot')
    .filter({ hasText: 'Nord' });
  await expect(
    teteSlot.getByRole('button', { name: /Périmé.*remplacer/i }),
  ).toHaveCount(1);
  await expectCupSlotsTextVisible(page);

  for (const phaseId of [tableauId, barrageId, stageId]) {
    await page.goto(
      `/competitions/${competition.id}/structure?stage=${phaseId}`,
    );
    await expect(
      page.locator('.schematic-cup .schematic-slot').first(),
    ).toBeVisible();
    await expectCupSlotsTextVisible(page);
  }

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(
    `/competitions/${competition.id}/structure?stage=${tableauId}`,
  );
  await expect(
    page.locator('.schematic-cup .schematic-slot').first(),
  ).toBeVisible();
  await expectCupSlotsTextVisible(page);
  await page.setViewportSize({ width: 412, height: 915 });

  const slot = page.locator('.schematic-slot').filter({ hasText: 'Nord' });
  await page.goto(`/competitions/${competition.id}/structure?stage=${stageId}`);
  await expect(
    slot.locator('.schematic-slot__name-row .schematic-slot__freshness'),
  ).toBeVisible();
  const slotBadge = slot.getByRole('button', { name: /Libérer la place/ });
  const slotChrome = await measureFreshnessChrome(slotBadge);
  expect(slotChrome.hitW).toBeGreaterThanOrEqual(24);
  expect(slotChrome.hitH).toBeGreaterThanOrEqual(24);
  expectPaintNear20(slotChrome);

  await slotBadge.tap();
  await expect(
    page.getByRole('dialog', { name: 'Libérer cette place ?' }),
  ).toBeVisible();
  await expect(
    page.getByRole('dialog', { name: /Placement manuel/ }),
  ).toHaveCount(0);
});
