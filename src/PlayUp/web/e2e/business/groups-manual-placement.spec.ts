import { expect, test, type APIRequestContext } from '@playwright/test';

const MANUAL_DRAW = 'Placement groupes — tirage';
const READY_MATCH = 'Placement groupes — match';

async function findCompetition(request: APIRequestContext, name: string) {
  const response = await request.get('http://127.0.0.1:5287/competitions');
  if (!response.ok()) {
    throw new Error(
      `GET /competitions failed (${response.status()}). Is Host running at http://127.0.0.1:5287?`,
    );
  }
  const list = (await response.json()) as { id: string; name: string }[];
  const hit = list.find((competition) => competition.name === name);
  if (!hit) {
    throw new Error(
      `Competition "${name}" not found. Seed with DevRunner: --reset --scenarios groups-manual-placement`,
    );
  }
  return hit;
}

test.describe('manual group placement', () => {
  test('fills both pools, shows that the draw is not required, and produces matches', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await findCompetition(request, MANUAL_DRAW);
    await page.goto(`/competitions/${competition.id}/structure`);

    const placeButtons = () =>
      page.getByRole('button', { name: 'Cliquer pour placer une équipe' });

    for (let placed = 0; placed < 8; placed++) {
      const remaining = 8 - placed;
      await expect(placeButtons()).toHaveCount(remaining);
      await placeButtons().first().click();
      const dialog = page.getByRole('dialog', { name: /Placer une équipe/ });
      await expect(dialog).toBeVisible();
      if (placed === 0) {
        await expect(dialog.getByText(/^Pot /).first()).toBeVisible();
      }
      await dialog.getByRole('checkbox').first().click();
      await dialog
        .getByRole('button', { name: 'Enregistrer', exact: true })
        .click();
      await expect(dialog).toBeHidden();
    }

    const pools = page.getByRole('group', { name: '2 groupes' });
    await expect(pools.getByText('4/4')).toHaveCount(2);
    await expect(page.getByText('Non requis — grille complète')).toBeVisible();

    // Match production is offered once the phase is Ready. Draft hides it.
    await page.getByRole('button', { name: 'Marquer comme prête' }).click();

    const matches = page.getByRole('region', { name: 'Matchs' });
    await expect(
      matches.getByText('Il reste 12 matchs à créer.'),
    ).toBeVisible();
    await matches
      .getByRole('button', { name: 'Créer les matchs', exact: true })
      .click();
    await expect(
      matches.getByText('Tous les matchs prévus sont créés.'),
    ).toBeVisible();
  });

  test('shows a match lock and a withdrawal refusal before any click', async ({
    page,
    request,
  }) => {
    const competition = await findCompetition(request, READY_MATCH);
    await page.goto(`/competitions/${competition.id}/structure`);

    const groupA = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe A' });
    const empty = groupA.locator('.schematic-slot--empty');
    await expect(empty).toHaveCount(1);
    await expect(
      groupA.getByRole('button', { name: 'Cliquer pour placer une équipe' }),
    ).toHaveCount(0);

    await empty.hover();
    await expect(page.getByRole('tooltip')).toContainText(
      'Ce groupe a déjà un match',
    );

    await groupA
      .locator('.schematic-slot:not(.schematic-slot--empty)')
      .first()
      .hover();
    await expect(page.getByRole('tooltip')).toContainText(
      'ne peut pas être retirée',
    );
  });
});
