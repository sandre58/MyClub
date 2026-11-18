import {
  expect,
  test,
  type APIRequestContext,
  type Page,
} from '@playwright/test';

const MANUAL = 'Placement groupes — manuel';
const MANUAL_DRAW = 'Placement groupes — tirage';
const READY_MATCH = 'Placement groupes — match';

const placeButtons = (page: Page) =>
  page.getByRole('button', {
    name: /Groupe [A-Z] · \d+ — Cliquer pour placer une équipe/,
  });

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

/** Continues from whatever seats are still empty, so a CI retry can finish a partial grid. */
async function fillRemainingSeats(page: Page, expectPot: boolean) {
  const pools = page.getByRole('group', { name: '2 groupes' });
  await expect(pools).toBeVisible();
  await expect
    .poll(async () => {
      const open = await placeButtons(page).count();
      const full = await pools.getByText('4/4').count();
      return open + full;
    })
    .toBeGreaterThan(0);

  let remaining = await placeButtons(page).count();
  while (remaining > 0) {
    await placeButtons(page).first().click();
    const dialog = page.getByRole('dialog', {
      name: /Placement manuel —/,
    });
    await expect(dialog).toBeVisible();
    const pot = dialog.getByText(/^Pot /);
    if (expectPot) {
      await expect(pot.first()).toBeVisible();
    } else {
      await expect(pot).toHaveCount(0);
    }
    const teamPicker = dialog.getByRole('radio');
    await expect(teamPicker.first()).toBeVisible();
    await teamPicker.first().click();
    await dialog
      .getByRole('button', { name: 'Enregistrer', exact: true })
      .click();
    await expect(dialog).toBeHidden();
    await expect(placeButtons(page)).toHaveCount(remaining - 1);
    remaining -= 1;
  }
}

test.describe('manual group placement', () => {
  test('fills the manual pools without showing a pot', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await findCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillRemainingSeats(page, false);

    const pools = page.getByRole('group', { name: '2 groupes' });
    await expect(pools.getByText('4/4')).toHaveCount(2);
    await expect(page.getByText('Non requis — grille complète')).toHaveCount(0);
  });

  test('fills both pools, shows that the draw is not required, and produces matches', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await findCompetition(request, MANUAL_DRAW);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillRemainingSeats(page, true);

    const pools = page.getByRole('group', { name: '2 groupes' });
    await expect(pools.getByText('4/4')).toHaveCount(2);
    await expect(page.getByText('Non requis — grille complète')).toBeVisible();

    // Match production is offered once the phase is Ready. Draft hides it.
    const prepare = page.getByRole('button', { name: 'Marquer comme prête' });
    if (await prepare.isVisible()) {
      await prepare.click();
    }

    const matches = page.getByRole('region', { name: 'Matchs' });
    const created = matches.getByText('Tous les matchs prévus sont créés.');
    if (!(await created.isVisible())) {
      await expect(
        matches.getByText('Il reste 12 matchs à créer.'),
      ).toBeVisible();
      await matches
        .getByRole('button', { name: 'Créer les matchs', exact: true })
        .click();
    }
    await expect(created).toBeVisible();
  });

  test('places three teams from the CTA in one batch then produces matches', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await findCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);

    await page.getByRole('button', { name: 'Placer manuellement' }).click();
    const dialog = page.getByRole('dialog', {
      name: /Placement manuel —/,
    });
    await expect(dialog).toBeVisible();

    const teams = dialog.getByRole('checkbox');
    await teams.nth(0).click();
    await teams.nth(1).click();
    await teams.nth(2).click();
    await dialog.getByRole('button', { name: /Placer 3 équipes/ }).click();
    await expect(dialog).toBeHidden();

    const pools = page.getByRole('group', { name: '2 groupes' });
    await expect(pools.getByText('3/4')).toHaveCount(1);

    const prepare = page.getByRole('button', { name: 'Marquer comme prête' });
    if (await prepare.isVisible()) {
      await prepare.click();
    }

    const matches = page.getByRole('region', { name: 'Matchs' });
    const created = matches.getByText('Tous les matchs prévus sont créés.');
    if (!(await created.isVisible())) {
      await matches
        .getByRole('button', { name: 'Créer les matchs', exact: true })
        .click();
    }
    await expect(created).toBeVisible();
  });

  test('replaces a team on a full group seat', async ({ page, request }) => {
    test.setTimeout(120_000);
    const competition = await findCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillRemainingSeats(page, false);

    const editSeat = page
      .getByRole('button', {
        name: /Groupe A · 1 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await editSeat.click();
    const dialog = page.getByRole('dialog', {
      name: /Modifier la place — Groupe A/,
    });
    const radios = dialog.getByRole('radio');
    const current = await radios
      .filter({ hasText: /Équipe générée 1|Alpha/ })
      .count();
    const pick = current > 0 ? radios.nth(1) : radios.last();
    await pick.click();
    await dialog
      .getByRole('button', { name: 'Enregistrer', exact: true })
      .click();
    await expect(dialog).toBeHidden();
    await expect(editSeat).toBeVisible();
  });

  test('removes a team with clear then save', async ({ page, request }) => {
    test.setTimeout(120_000);
    const competition = await findCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillRemainingSeats(page, false);

    const editSeat = page
      .getByRole('button', {
        name: /Groupe B · 1 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await editSeat.click();
    const dialog = page.getByRole('dialog', {
      name: /Modifier la place — Groupe B/,
    });
    await dialog
      .getByRole('button', { name: /Retirer l'équipe de la place/ })
      .click();
    await dialog
      .getByRole('button', { name: 'Enregistrer', exact: true })
      .click();
    await expect(dialog).toBeHidden();
    await expect(
      page.getByRole('button', {
        name: /Groupe B · 1 — Cliquer pour placer une équipe/,
      }),
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
      groupA.getByRole('button', { name: /Cliquer pour placer une équipe/ }),
    ).toHaveCount(0);

    const emptyReasonId = await empty.getAttribute('aria-describedby');
    expect(emptyReasonId).toBeTruthy();
    await expect(page.locator(`[id="${emptyReasonId}"]`)).toContainText(
      'Ce groupe a déjà un match',
    );

    const occupied = groupA
      .locator('.schematic-slot:not(.schematic-slot--empty)')
      .first();
    const occupiedReasonId = await occupied.getAttribute('aria-describedby');
    expect(occupiedReasonId).toBeTruthy();
    await expect(page.locator(`[id="${occupiedReasonId}"]`)).toContainText(
      'ne peut pas être retirée',
    );

    await empty.hover();
    await expect(page.getByRole('tooltip')).toContainText(
      'Ce groupe a déjà un match',
    );

    await occupied.hover();
    await expect(page.getByRole('tooltip')).toContainText(
      'ne peut pas être retirée',
    );
  });
});
