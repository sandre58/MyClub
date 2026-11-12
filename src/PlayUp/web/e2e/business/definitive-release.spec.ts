import { expect, test } from '@playwright/test';

import { createDefinitiveSelectionCompetition } from './hostSeed';

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
    page.getByText('Ne pourra plus être confirmée', { exact: true }),
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
  // The qualification path still reserves the slot, so manual placement
  // stays hidden. The seat shows the unresolved ranking instead of the team.
  await expect(page.getByText('Non résolue')).toBeVisible();
  await expect(page.getByText('4e du classement')).toBeVisible();
  await expect(
    page.getByRole('button', { name: 'Placer manuellement' }),
  ).toHaveCount(0);
});
