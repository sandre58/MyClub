import {
  expect,
  test,
  type APIRequestContext,
  type Page,
} from '@playwright/test';
import {
  getPrimaryStageId,
  getStructureMaterializationState,
  playUpApiBase,
} from './hostSeed';

const MANUAL = 'Placement groupes — manuel';
const MANUAL_DRAW = 'Placement groupes — tirage';
const READY_MATCH = 'Placement groupes — match';

const placeButtons = (page: Page) =>
  page.getByRole('button', {
    name: /Groupe [A-Z] · \d+ — Cliquer pour placer une équipe/,
  });

type SchematicCaseWire = {
  formPosition?: { kind?: string; groupId?: string };
  entry?: { entryId?: string };
  assignment?: { entryId?: string };
  canClear?: boolean;
};

async function clearGroupPlacements(
  request: APIRequestContext,
  competitionId: string,
) {
  const stageId = await getPrimaryStageId(request, competitionId);
  const schematic = await request.get(
    `${playUpApiBase()}/stages/${stageId}/schematic`,
  );
  if (!schematic.ok()) {
    throw new Error(`GET schematic failed (${schematic.status()})`);
  }
  const body = (await schematic.json()) as { cases?: SchematicCaseWire[] };
  for (const place of body.cases ?? []) {
    if (place.formPosition?.kind !== 'GroupPlace') continue;
    const entryId = place.entry?.entryId ?? place.assignment?.entryId;
    const groupId = place.formPosition.groupId;
    if (!entryId || !groupId || !place.canClear) continue;
    const response = await request.delete(
      `${playUpApiBase()}/stages/${stageId}/groups/${groupId}/entries/${entryId}`,
    );
    if (!response.ok() && response.status() !== 404) {
      throw new Error(
        `DELETE group entry failed (${response.status()}): ${await response.text()}`,
      );
    }
  }
}

async function prepareCompetition(
  request: APIRequestContext,
  name: string,
): Promise<{ id: string; name: string }> {
  const competition = await findCompetition(request, name);
  const { attachedMatchCount, stageId } =
    await getStructureMaterializationState(request, competition.id);
  if (attachedMatchCount > 0) {
    const response = await request.post(
      `${playUpApiBase()}/stages/${stageId}/matches/dematerialize`,
    );
    if (!response.ok()) {
      throw new Error(
        `Dematerialize failed (${response.status()}): ${await response.text()}`,
      );
    }
  }
  await clearGroupPlacements(request, competition.id);
  return competition;
}

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

async function placeOneTeamFromDialog(page: Page, expectPot: boolean) {
  const dialog = page.getByRole('dialog', {
    name: /Placement — .* —/,
  });
  await expect(dialog).toBeVisible();
  const pot = dialog.getByText(/^Pot /);
  if (expectPot) {
    await expect(pot.first()).toBeVisible();
  } else {
    await expect(pot).toHaveCount(0);
  }
  const teamPicker = dialog.getByRole('checkbox', { checked: false });
  await expect(teamPicker.first()).toBeVisible();
  await teamPicker.first().click();
  await dialog
    .getByRole('button', { name: 'Enregistrer', exact: true })
    .click();
  await expect(dialog).toBeHidden();
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
    await placeOneTeamFromDialog(page, expectPot);
    await expect(placeButtons(page)).toHaveCount(remaining - 1);
    remaining -= 1;
  }
}

async function fillGroupSeats(
  page: Page,
  groupLabel: string,
  expectPot: boolean,
  count = 4,
) {
  const group = page
    .locator('.regulation-schematic__card')
    .filter({ hasText: groupLabel });
  await expect(group).toBeVisible();
  const localPlace = group.getByRole('button', {
    name: /Cliquer pour placer une équipe/,
  });
  for (let placed = 0; placed < count; placed += 1) {
    const open = await localPlace.count();
    if (open === 0) break;
    await localPlace.first().click();
    await placeOneTeamFromDialog(page, expectPot);
  }
  await expect(group.getByText(`${count}/${count}`)).toBeVisible();
}

async function produceMatchesIfNeeded(page: Page) {
  const prepare = page.getByRole('button', { name: 'Marquer comme prête' });
  if (await prepare.isVisible()) {
    await prepare.click();
  }
  const matches = page.getByRole('region', { name: 'Matchs' });
  const created = matches.getByText('Tous les matchs prévus sont créés.');
  if (!(await created.isVisible())) {
    await expect(
      matches.getByText(/Il reste \d+ matchs à créer\./),
    ).toBeVisible();
    await matches
      .getByRole('button', { name: 'Créer les matchs', exact: true })
      .click();
  }
  await expect(created).toBeVisible();
}

test.describe('manual group placement', () => {
  test('places three teams from the CTA in one batch then produces matches', async ({
    page,
    request,
  }) => {
    test.setTimeout(180_000);
    const competition = await prepareCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);

    await page.getByRole('button', { name: 'Placer manuellement' }).click();
    await page
      .getByRole('menuitem', { name: /Groupe A, 0 équipes sur 4/ })
      .click();
    const dialog = page.getByRole('dialog', {
      name: /Placement — .* — Groupe A/,
    });
    await expect(dialog).toBeVisible();

    const teams = dialog.getByRole('checkbox');
    await teams.nth(0).click();
    await teams.nth(1).click();
    await teams.nth(2).click();
    await dialog.getByRole('button', { name: 'Enregistrer' }).click();
    await expect(dialog).toBeHidden();

    await fillRemainingSeats(page, false);
    const pools = page.getByRole('group', { name: '2 groupes' });
    await expect(pools.getByText('4/4')).toHaveCount(2);
    await produceMatchesIfNeeded(page);
  });

  test('replaces a team on a full group seat', async ({ page, request }) => {
    test.setTimeout(120_000);
    const competition = await prepareCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillGroupSeats(page, 'Groupe A', false);

    const groupA = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe A' });
    const editSeat = groupA
      .getByRole('button', {
        name: /Groupe A · 1 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await editSeat.click();
    const dialog = page.getByRole('dialog', {
      name: /Placement — .* — Groupe A/,
    });
    const occupant = dialog.getByRole('checkbox', { checked: true }).first();
    const originalName = (await occupant.innerText()).trim().split('\n')[0];
    await dialog
      .getByRole('checkbox', { name: originalName, checked: true })
      .click();
    const replacee = dialog
      .getByRole('checkbox', { checked: false })
      .filter({ hasText: /Bravo|Charlie/ })
      .first();
    const replacedName = (await replacee.innerText()).trim().split('\n')[0];
    await replacee.click();
    await dialog
      .getByRole('button', { name: 'Enregistrer', exact: true })
      .click();
    await expect(dialog).toBeHidden();
    await expect(groupA.getByText('4/4')).toBeVisible();
    const seatOne = groupA.locator('.schematic-slot').first();
    await expect(seatOne).toContainText(replacedName);
    await expect(seatOne).not.toContainText(originalName);
  });

  test('removes a team with clear then save', async ({ page, request }) => {
    test.setTimeout(120_000);
    const competition = await prepareCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    const groupB = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe B' });
    await fillGroupSeats(page, 'Groupe B', false);

    const editSeat = groupB
      .getByRole('button', {
        name: /Groupe B · 1 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await editSeat.click();
    const dialog = page.getByRole('dialog', {
      name: /Placement — .* — Groupe B/,
    });
    const occupantName = (
      await dialog.getByRole('checkbox', { checked: true }).first().innerText()
    )
      .trim()
      .split('\n')[0];
    await dialog
      .getByRole('checkbox', { name: occupantName, checked: true })
      .click();
    await dialog
      .getByRole('button', { name: 'Enregistrer', exact: true })
      .click();
    await expect(dialog).toBeHidden();

    await expect(groupB.getByText('3/4')).toBeVisible();
    await expect(
      groupB.getByRole('button', {
        name: /Groupe B · 4 — Cliquer pour placer une équipe/,
      }),
    ).toBeVisible();
    await expect(
      groupB.getByRole('button', {
        name: /Groupe B · 1 — Cliquer pour placer une équipe/,
      }),
    ).toHaveCount(0);
  });

  test('fills the manual pools without showing a pot', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await prepareCompetition(request, MANUAL);
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
    const competition = await prepareCompetition(request, MANUAL_DRAW);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillRemainingSeats(page, true);

    const pools = page.getByRole('group', { name: '2 groupes' });
    await expect(pools.getByText('4/4')).toHaveCount(2);
    await expect(page.getByText('Non requis — grille complète')).toBeVisible();
    await produceMatchesIfNeeded(page);
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

  test('compacts group seats after withdrawal and replace targets the shifted occupant', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await prepareCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillGroupSeats(page, 'Groupe A', false);

    const groupA = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe A' });
    await groupA
      .getByRole('button', {
        name: /Groupe A · 4 — Cliquer pour modifier ou retirer/,
      })
      .first()
      .click();
    const seatFourDialog = page.getByRole('dialog', {
      name: /Placement — .* — Groupe A/,
    });
    const shiftedTeamName = (
      await seatFourDialog
        .getByRole('checkbox', { checked: true })
        .first()
        .locator('.ds-choice-tile__title')
        .innerText()
    ).trim();
    await seatFourDialog.getByRole('button', { name: 'Annuler' }).click();

    const editSeat = groupA
      .getByRole('button', {
        name: /Groupe A · 1 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await editSeat.click();
    const dialog = page.getByRole('dialog', {
      name: /Placement — .* — Groupe A/,
    });
    const seatOneName = (
      await dialog.getByRole('checkbox', { checked: true }).first().innerText()
    )
      .trim()
      .split('\n')[0];
    await dialog
      .getByRole('checkbox', { name: seatOneName, checked: true })
      .click();
    await dialog
      .getByRole('button', { name: 'Enregistrer', exact: true })
      .click();
    await expect(dialog).toBeHidden();
    await expect(groupA.getByText('3/4')).toBeVisible();

    const seatThree = groupA
      .getByRole('button', {
        name: /Groupe A · 3 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await seatThree.click();
    const replaceDialog = page.getByRole('dialog', {
      name: /Placement — .* — Groupe A/,
    });
    await expect(
      replaceDialog.getByRole('checkbox', {
        name: shiftedTeamName,
        checked: true,
      }),
    ).toBeVisible();
    await replaceDialog.getByRole('button', { name: 'Annuler' }).click();
  });
});
