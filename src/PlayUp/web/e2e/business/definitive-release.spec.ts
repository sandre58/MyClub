import { expect, test } from '@playwright/test';

import {
  createDefinitiveSelectionCompetition,
  playUpApiBase,
} from './hostSeed';

/**
 * Host + PostgreSQL — grouped release of a place that can no longer be confirmed.
 * The competition is created by this test.
 */
test('Structure — libération groupée d’une sélection vide puis placement manuel', async ({
  page,
  request,
}) => {
  const seeded = await createDefinitiveSelectionCompetition(request);

  await page.goto(
    `/competitions/${seeded.competitionId}/structure?stage=${seeded.destinationId}`,
  );
  await expect(page.getByRole('heading', { name: 'Structure' })).toBeVisible();
  await expect(
    page.getByText('1 place ne pourra plus être confirmée.'),
  ).toBeVisible();
  await expect(
    page.getByRole('button', {
      name: /Non confirmée.*Ne pourra plus être confirmée/,
    }),
  ).toBeVisible();

  await page.getByRole('button', { name: 'Libérer…' }).click();
  const dialog = page.getByRole('dialog', { name: 'Libérer ces places ?' });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByText(/Ouest — S4 \(Tableau\)/)).toBeVisible();
  await expect(dialog.getByText('Aucun match ne sera modifié.')).toBeVisible();
  await expect(
    dialog.getByText(
      'Cette libération est irréversible. Les places vidées ne seront pas reprises automatiquement.',
    ),
  ).toBeVisible();

  await dialog.getByRole('button', { name: 'Libérer', exact: true }).click();

  const summary = page.getByRole('dialog', { name: 'Places libérées' });
  await expect(summary).toBeVisible();
  await expect(summary.getByText(/Ouest — S4 \(Tableau\)/)).toBeVisible();
  await expect(
    page.getByRole('button', { name: 'Libérer', exact: true }),
  ).toHaveCount(0);

  await summary.getByRole('button', { name: 'Fermer' }).click();
  await expect(summary).toHaveCount(0);
  await expect(
    page.getByText('1 place ne pourra plus être confirmée.'),
  ).toHaveCount(0);
  await expect(page.getByText('Ouest')).toHaveCount(0);
  await expect(page.getByText('Non résolue')).toBeVisible();
  await expect(page.getByText('4e du classement')).toBeVisible();

  const restored = await request.put(
    `${playUpApiBase()}/stages/${seeded.destinationId}/affectation`,
    { data: { entryIds: seeded.cupEntryIds } },
  );
  if (!restored.ok()) {
    throw new Error(
      `PUT affectation failed (${restored.status()}): ${await restored.text()}`,
    );
  }
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Structure' })).toBeVisible();
  await page.getByRole('button', { name: 'Placer manuellement' }).click();
  const manual = page.getByRole('dialog', {
    name: 'Placement manuel — Tableau',
  });
  await manual.getByRole('combobox', { name: 'Place' }).click();
  await page.getByRole('option', { name: 'S4' }).click();
  await manual.getByRole('checkbox', { name: 'Ouest' }).click();
  await manual.getByRole('button', { name: 'Enregistrer' }).click();
  await expect(manual).toHaveCount(0);
  await expect(page.getByText('Ouest').first()).toBeVisible();
});
