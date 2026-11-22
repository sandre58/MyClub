import {
  expect,
  test,
  type APIRequestContext,
  type Locator,
  type Page,
} from '@playwright/test';
import {
  getPrimaryStageId,
  getStructureMaterializationState,
  playUpApiBase,
} from './hostSeed';

type HubPot = { entryId: string; pot: number };

type HubStage = {
  stageId: string;
  compositionEntryIds?: string[] | null;
  compositionPreviewNames?: string[] | null;
  compositionEntryPots?: HubPot[] | null;
};

type StageGroupWire = { id: string; name: string; entryIds: string[] };

type StageDrawWire = {
  id: string;
  status: string;
  isApplied?: boolean;
  actions?: {
    release?: { releasableCount?: number };
    redo?: { state?: string };
  };
};

type StageOverviewWire = {
  draws?: StageDrawWire[];
  groups?: StageGroupWire[];
};

async function readHubStage(
  request: APIRequestContext,
  competitionId: string,
): Promise<HubStage> {
  const stageId = await getPrimaryStageId(request, competitionId);
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
  );
  if (!response.ok()) {
    throw new Error(`GET structure failed (${response.status()})`);
  }
  const view = (await response.json()) as { stages?: HubStage[] };
  const stage = view.stages?.find((item) => item.stageId === stageId);
  if (!stage) {
    throw new Error(`Hub stage ${stageId} is missing`);
  }
  return stage;
}

async function readStageOverview(
  request: APIRequestContext,
  stageId: string,
): Promise<StageOverviewWire> {
  const response = await request.get(`${playUpApiBase()}/stages/${stageId}`);
  if (!response.ok()) {
    throw new Error(`GET stage failed (${response.status()})`);
  }
  return (await response.json()) as StageOverviewWire;
}

type RosterEntry = { entryId: string; displayName?: string | null };

async function readRoster(
  request: APIRequestContext,
  competitionId: string,
): Promise<RosterEntry[]> {
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
  );
  if (!response.ok()) {
    throw new Error(`GET structure failed (${response.status()})`);
  }
  const view = (await response.json()) as {
    participants?: { entries?: RosterEntry[] };
  };
  return view.participants?.entries ?? [];
}

function teamsOfPot(
  stage: HubStage,
  roster: readonly RosterEntry[],
  pot: number,
): { entryId: string; label: string }[] {
  const labels = new Map<string, string>();
  for (const entry of roster) {
    const name = entry.displayName?.trim();
    if (entry.entryId && name) {
      labels.set(entry.entryId, name);
    }
  }
  return (stage.compositionEntryPots ?? [])
    .filter((row) => row.pot === pot)
    .flatMap((row) => {
      const label = labels.get(row.entryId);
      return label ? [{ entryId: row.entryId, label }] : [];
    });
}

async function placeNamedTeam(
  page: Page,
  groupLabel: string,
  teamName: string,
) {
  const group = page
    .locator('.regulation-schematic__card')
    .filter({ hasText: groupLabel });
  const seat = group
    .getByRole('button', { name: /Cliquer pour placer une équipe/ })
    .first();
  await expect(seat).toBeEnabled();
  await seat.click();
  const dialog = placementDialog(page);
  await expect(dialog).toBeVisible();
  await dialog.getByRole('checkbox', { name: teamName, exact: true }).click();
  await savePlacementDialog(dialog);
  await expect(group.getByText(teamName, { exact: true })).toBeVisible();
}

async function discardDraws(request: APIRequestContext, stageId: string) {
  const before = await readStageOverview(request, stageId);
  for (const draw of before.draws ?? []) {
    if (draw.status === 'Cancelled') continue;
    const cancel = await request.post(
      `${playUpApiBase()}/stages/${stageId}/draws/${draw.id}/cancel`,
    );
    if (!cancel.ok() && cancel.status() !== 409) {
      throw new Error(
        `Cancel draw failed (${cancel.status()}): ${await cancel.text()}`,
      );
    }
  }
  const after = await readStageOverview(request, stageId);
  for (const draw of after.draws ?? []) {
    if (draw.status !== 'Cancelled') continue;
    const release = await request.post(
      `${playUpApiBase()}/stages/${stageId}/draws/${draw.id}/release-aligned-placements`,
    );
    if (!release.ok() && release.status() !== 409 && release.status() !== 404) {
      throw new Error(
        `Release draw failed (${release.status()}): ${await release.text()}`,
      );
    }
  }
}

async function dismissStatus(page: Page, text: string) {
  const status = page.getByRole('status').filter({ hasText: text });
  await expect(status).toBeVisible();
  await status.getByRole('button', { name: 'Fermer', exact: true }).click();
  await expect(status).toBeHidden();
}

async function launchAndValidateDraw(page: Page) {
  await page
    .getByRole('button', { name: 'Lancer le tirage', exact: true })
    .click();
  const dialog = page.getByRole('dialog', { name: /Exécutions de tirage/ });
  const posted = page.waitForRequest(
    (req) => req.method() === 'POST' && req.url().endsWith('/draws'),
  );
  await expect(dialog).toBeVisible();
  await dialog
    .getByRole('button', { name: 'Lancer le tirage', exact: true })
    .click();
  const createBody = (await posted).postDataJSON() as { intent?: string };
  expect(createBody.intent).toBe('Default');
  // The success toast pauses under the pointer and covers the footer.
  await dismissStatus(page, 'Nouvelle exécution créée.');
  const validate = dialog.getByRole('button', {
    name: 'Valider le tirage',
    exact: true,
  });
  await expect(validate).toBeEnabled();
  await validate.click();
  await dismissStatus(page, 'Tirage publié et appliqué.');
  await expect(dialog).toBeHidden();
}

async function reopenDrawDialog(page: Page) {
  await page
    .getByRole('button', { name: 'Voir le tirage', exact: true })
    .click();
  const dialog = page.getByRole('dialog', { name: /Exécutions de tirage/ });
  await expect(dialog).toBeVisible();
  return dialog;
}

const MANUAL = 'Placement groupes — manuel';
const MANUAL_DRAW = 'Placement groupes — tirage';
const READY_MATCH = 'Placement groupes — match';

const placeButtons = (page: Page) =>
  page.getByRole('button', {
    name: /Groupe [A-Z] · \d+ — Cliquer pour placer une équipe/,
  });

function placementDialog(page: Page) {
  return page.getByRole('dialog', {
    name: /Placement — .* —/,
  });
}

function selectableUncheckedTeams(dialog: Locator) {
  return dialog.locator(
    'button[role="checkbox"][aria-checked="false"]:not([aria-disabled="true"])',
  );
}

async function teamNameOnSchematicSeat(
  group: Locator,
  seatIndex1Based: number,
): Promise<string> {
  const seat = group.locator('.schematic-slot').nth(seatIndex1Based - 1);
  await expect(seat).toBeVisible();
  const nameRow = seat.locator('.schematic-slot__name-row');
  await expect(nameRow).toBeVisible();
  return (await nameRow.innerText()).trim();
}

async function titleFromTeamCheckbox(tile: Locator): Promise<string> {
  const title = tile.locator('.ds-choice-tile__title');
  await expect(title).toBeVisible();
  return (await title.innerText()).trim();
}

/** Team name for an unchecked selectable tile ≠ `excludeName` (resolve before unchecking). */
async function replacementTeamName(
  dialog: Locator,
  excludeName: string,
): Promise<string> {
  const candidates = selectableUncheckedTeams(dialog);
  const count = await candidates.count();
  for (let i = 0; i < count; i += 1) {
    const tile = candidates.nth(i);
    const name = await titleFromTeamCheckbox(tile);
    if (name !== excludeName) {
      return name;
    }
  }
  throw new Error(
    `No unchecked replacement team found besides "${excludeName}"`,
  );
}

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
  await discardDraws(request, stageId);
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

async function savePlacementDialog(dialog: Locator) {
  const save = dialog.getByRole('button', { name: 'Enregistrer', exact: true });
  await expect(save).toBeVisible();
  await expect(save).toBeEnabled();
  await save.click();
  await expect(dialog).toBeHidden();
}

async function placeOneTeamFromDialog(page: Page) {
  const dialog = placementDialog(page);
  await expect(dialog).toBeVisible();
  await expect(dialog.getByText(/^Pot /)).toHaveCount(0);
  const teamPicker = selectableUncheckedTeams(dialog);
  await expect(teamPicker.first()).toBeVisible();
  await teamPicker.first().click();
  await savePlacementDialog(dialog);
}

/** Continues from whatever seats are still empty, so a CI retry can finish a partial grid. */
async function fillRemainingSeats(page: Page) {
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
    const openSeat = placeButtons(page).first();
    await expect(openSeat).toBeVisible();
    await expect(openSeat).toBeEnabled();
    await openSeat.click();
    await placeOneTeamFromDialog(page);
    await expect(placeButtons(page)).toHaveCount(remaining - 1);
    remaining -= 1;
  }
}

async function fillGroupSeats(page: Page, groupLabel: string, count = 4) {
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
    const seat = localPlace.first();
    await expect(seat).toBeVisible();
    await expect(seat).toBeEnabled();
    await seat.click();
    await placeOneTeamFromDialog(page);
  }
  await expect(group.getByText(`${count}/${count}`)).toBeVisible();
}

async function produceMatchesIfNeeded(page: Page) {
  const prepare = page.getByRole('button', { name: 'Marquer comme prête' });
  if (await prepare.isVisible()) {
    await expect(prepare).toBeEnabled();
    await prepare.click();
  }
  const matches = page.getByRole('region', { name: 'Matchs' });
  const created = matches.getByText('Tous les matchs prévus sont créés.');
  if (!(await created.isVisible())) {
    await expect(
      matches.getByText(/Il reste \d+ matchs à créer\./),
    ).toBeVisible();
    const create = matches.getByRole('button', {
      name: 'Créer les matchs',
      exact: true,
    });
    await expect(create).toBeEnabled();
    await create.click();
  }
  await expect(created).toBeVisible();
}

test.describe('manual group placement', () => {
  test('places three teams from the CTA in one batch then produces matches', async ({
    page,
    request,
  }) => {
    // Batch path: CTA + partial fill + remaining seats + match production.
    test.setTimeout(90_000);
    const competition = await prepareCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);

    const manualCta = page.getByRole('button', { name: 'Placer manuellement' });
    await expect(manualCta).toBeVisible();
    await expect(manualCta).toBeEnabled();
    await manualCta.click();
    const menuItem = page.getByRole('menuitem', {
      name: /Groupe A, 0 équipes sur 4/,
    });
    await expect(menuItem).toBeVisible();
    await menuItem.click();
    const dialog = placementDialog(page);
    await expect(dialog).toBeVisible();

    const teams = selectableUncheckedTeams(dialog);
    await expect(teams.first()).toBeVisible();
    const available = await teams.count();
    expect(available).toBeGreaterThanOrEqual(3);
    const toPick = 3;
    for (let i = 0; i < toPick; i += 1) {
      const tile = teams.nth(i);
      await expect(tile).toBeVisible();
      await tile.click();
    }
    await savePlacementDialog(dialog);

    await fillRemainingSeats(page);
    const pools = page.getByRole('group', { name: '2 groupes' });
    await expect(pools.getByText('4/4')).toHaveCount(2);
    await produceMatchesIfNeeded(page);
  });

  test('replaces a team on a full group seat', async ({ page, request }) => {
    const competition = await prepareCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillGroupSeats(page, 'Groupe A');

    const groupA = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe A' });
    const originalName = await teamNameOnSchematicSeat(groupA, 1);
    const editSeat = groupA
      .getByRole('button', {
        name: /Groupe A · 1 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await expect(editSeat).toBeEnabled();
    await editSeat.click();
    const dialog = placementDialog(page);
    await expect(dialog).toBeVisible();
    const occupant = dialog.getByRole('checkbox', {
      name: originalName,
      exact: true,
      checked: true,
    });
    await expect(occupant).toBeVisible();
    const replacedName = await replacementTeamName(dialog, originalName);
    await occupant.click();
    const replacee = dialog.getByRole('checkbox', {
      name: replacedName,
      exact: true,
    });
    await expect(replacee).toBeVisible();
    await replacee.click();
    await savePlacementDialog(dialog);
    await expect(groupA.getByText('4/4')).toBeVisible();
    // Batch replace keeps remaining seats; newcomer may not land on seat 1 (SetGroupEntries).
    await expect(groupA.getByText(replacedName, { exact: true })).toBeVisible();
    await expect(groupA.getByText(originalName, { exact: true })).toHaveCount(
      0,
    );
  });

  test('removes a team with clear then save', async ({ page, request }) => {
    const competition = await prepareCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    const groupB = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe B' });
    await fillGroupSeats(page, 'Groupe B');

    const occupantName = await teamNameOnSchematicSeat(groupB, 1);
    const editSeat = groupB
      .getByRole('button', {
        name: /Groupe B · 1 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await expect(editSeat).toBeEnabled();
    await editSeat.click();
    const dialog = placementDialog(page);
    await expect(dialog).toBeVisible();
    const occupant = dialog.getByRole('checkbox', {
      name: occupantName,
      exact: true,
      checked: true,
    });
    await expect(occupant).toBeVisible();
    await occupant.click();
    await savePlacementDialog(dialog);

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
    const competition = await prepareCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillRemainingSeats(page);

    const pools = page.getByRole('group', { name: '2 groupes' });
    await expect(pools.getByText('4/4')).toHaveCount(2);
    await expect(page.getByText('Non requis — grille complète')).toHaveCount(0);
  });

  test('fills both pools, shows that the draw is not required, and produces matches', async ({
    page,
    request,
  }) => {
    const competition = await prepareCompetition(request, MANUAL_DRAW);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillRemainingSeats(page);

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

  test('compacts group seats after withdrawal and opens seat 3 on the shifted occupant', async ({
    page,
    request,
  }) => {
    const competition = await prepareCompetition(request, MANUAL);
    await page.goto(`/competitions/${competition.id}/structure`);
    await fillGroupSeats(page, 'Groupe A');

    const groupA = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe A' });
    const shiftedTeamName = await teamNameOnSchematicSeat(groupA, 4);
    const seatOneName = await teamNameOnSchematicSeat(groupA, 1);
    const editSeat = groupA
      .getByRole('button', {
        name: /Groupe A · 1 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await expect(editSeat).toBeEnabled();
    await editSeat.click();
    const dialog = placementDialog(page);
    await expect(dialog).toBeVisible();
    const seatOne = dialog.getByRole('checkbox', {
      name: seatOneName,
      exact: true,
      checked: true,
    });
    await expect(seatOne).toBeVisible();
    await seatOne.click();
    await savePlacementDialog(dialog);
    await expect(groupA.getByText('3/4')).toBeVisible();

    const seatThree = groupA
      .getByRole('button', {
        name: /Groupe A · 3 — Cliquer pour modifier ou retirer/,
      })
      .first();
    await expect(seatThree).toBeEnabled();
    await seatThree.click();
    const replaceDialog = placementDialog(page);
    await expect(replaceDialog).toBeVisible();
    await expect(
      replaceDialog.getByRole('checkbox', {
        name: shiftedTeamName,
        exact: true,
        checked: true,
      }),
    ).toBeVisible();
    await replaceDialog.getByRole('button', { name: 'Annuler' }).click();
  });

  test('disables the draw when two teams of the same pot share a group', async ({
    page,
    request,
  }) => {
    test.setTimeout(90_000);
    const competition = await prepareCompetition(request, MANUAL_DRAW);
    const stageId = await getPrimaryStageId(request, competition.id);
    await discardDraws(request, stageId);
    await page.goto(`/competitions/${competition.id}/structure`);

    const stage = await readHubStage(request, competition.id);
    const roster = await readRoster(request, competition.id);
    const samePot = teamsOfPot(stage, roster, 1);
    expect(samePot.length).toBeGreaterThanOrEqual(2);
    await placeNamedTeam(page, 'Groupe A', samePot[0].label);
    await placeNamedTeam(page, 'Groupe A', samePot[1].label);

    const launch = page.getByRole('button', { name: 'Lancer le tirage' });
    await expect(launch).toBeDisabled();
    await expect(
      page.getByText('Déplacez une équipe du même pot avant de tirer.'),
    ).toBeVisible();
    const named = page.getByText(/Groupe A, pot 1 :/);
    await expect(named).toBeVisible();
    await expect(named).toContainText(samePot[0].label);
    await expect(named).toContainText(samePot[1].label);

    const overview = await readStageOverview(request, stageId);
    expect(
      (overview.draws ?? []).every((draw) => draw.status === 'Cancelled'),
    ).toBe(true);
  });

  test('draws the remaining group places and keeps the hand-placed teams', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await prepareCompetition(request, MANUAL_DRAW);
    const stageId = await getPrimaryStageId(request, competition.id);
    await discardDraws(request, stageId);
    await page.goto(`/competitions/${competition.id}/structure`);

    const stage = await readHubStage(request, competition.id);
    const roster = await readRoster(request, competition.id);
    const pot1 = teamsOfPot(stage, roster, 1);
    const pot2 = teamsOfPot(stage, roster, 2);
    expect(pot1.length).toBeGreaterThan(0);
    expect(pot2.length).toBeGreaterThan(0);
    const kept = [pot1[0], pot2[0]];
    await placeNamedTeam(page, 'Groupe A', kept[0].label);
    await placeNamedTeam(page, 'Groupe A', kept[1].label);

    await launchAndValidateDraw(page);
    const dialog = await reopenDrawDialog(page);
    await expect(
      dialog.getByRole('button', { name: 'Refaire le tirage' }),
    ).toBeVisible();
    await dialog.getByRole('button', { name: 'Fermer', exact: true }).click();
    await expect(dialog).toBeHidden();

    const groupA = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe A' });
    await expect(groupA.getByText('4/4')).toBeVisible();
    await expect(
      groupA.getByText(kept[0].label, { exact: true }),
    ).toBeVisible();
    await expect(
      groupA.getByText(kept[1].label, { exact: true }),
    ).toBeVisible();
    const pools = page.getByRole('group', { name: '2 groupes' });
    await expect(pools.getByText('4/4')).toHaveCount(2);

    const keptIds = kept.map((team) => team.entryId);
    await expect
      .poll(async () => {
        const overview = await readStageOverview(request, stageId);
        const groupAWire = overview.groups?.find((group) => group.name === 'A');
        const ids = new Set(groupAWire?.entryIds ?? []);
        return keptIds.every((id) => ids.has(id));
      })
      .toBe(true);
    const overview = await readStageOverview(request, stageId);
    const potByEntry = new Map(
      (stage.compositionEntryPots ?? []).map((row) => [row.entryId, row.pot]),
    );
    for (const group of overview.groups ?? []) {
      const pots = group.entryIds.map((id) => potByEntry.get(id));
      expect(new Set(pots)).toEqual(new Set([1, 2, 3, 4]));
    }

    await produceMatchesIfNeeded(page);
  });

  test('redoes a groups draw and keeps the hand-placed teams', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await prepareCompetition(request, MANUAL_DRAW);
    const stageId = await getPrimaryStageId(request, competition.id);
    await discardDraws(request, stageId);
    await page.goto(`/competitions/${competition.id}/structure`);

    const stage = await readHubStage(request, competition.id);
    const roster = await readRoster(request, competition.id);
    const kept = [
      teamsOfPot(stage, roster, 1)[0],
      teamsOfPot(stage, roster, 2)[0],
    ];
    await placeNamedTeam(page, 'Groupe A', kept[0].label);
    await placeNamedTeam(page, 'Groupe A', kept[1].label);
    await launchAndValidateDraw(page);
    const dialog = await reopenDrawDialog(page);

    await dialog.getByRole('button', { name: 'Refaire le tirage' }).click();
    const confirm = page.getByRole('dialog', { name: 'Refaire le tirage ?' });
    await expect(confirm).toContainText(
      'retire seulement les équipes placées par le tirage',
    );
    await confirm.getByRole('button', { name: 'Refaire le tirage' }).click();
    await dismissStatus(page, 'Nouvelle exécution créée.');
    await expect(
      dialog.getByRole('heading', { name: /Exécution #/ }).first(),
    ).toBeVisible();

    const overview = await readStageOverview(request, stageId);
    expect(overview.draws?.some((draw) => draw.status === 'Draft')).toBe(true);
    const groupA = overview.groups?.find((group) => group.name === 'A');
    const keptIds = kept.map((team) => team.entryId);
    expect(groupA?.entryIds).toEqual(expect.arrayContaining(keptIds));
    await dialog.getByRole('button', { name: 'Fermer', exact: true }).click();
    await expect(dialog).toBeHidden();
    const groupACard = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe A' });
    const seats = groupACard.locator('.schematic-slot__name-row');
    await expect(seats.getByText(kept[0].label, { exact: true })).toBeVisible();
    await expect(seats.getByText(kept[1].label, { exact: true })).toBeVisible();
  });

  test('cancels an applied groups draw and offers release of the drawn teams only', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await prepareCompetition(request, MANUAL_DRAW);
    const stageId = await getPrimaryStageId(request, competition.id);
    await discardDraws(request, stageId);
    await page.goto(`/competitions/${competition.id}/structure`);

    const stage = await readHubStage(request, competition.id);
    const roster = await readRoster(request, competition.id);
    const kept = [
      teamsOfPot(stage, roster, 1)[0],
      teamsOfPot(stage, roster, 2)[0],
    ];
    await placeNamedTeam(page, 'Groupe A', kept[0].label);
    await placeNamedTeam(page, 'Groupe A', kept[1].label);
    await launchAndValidateDraw(page);
    const dialog = await reopenDrawDialog(page);

    const cancelApplied = dialog.getByRole('button', {
      name: 'Annuler le tirage',
    });
    const redo = dialog.getByRole('button', { name: 'Refaire le tirage' });
    await expect(cancelApplied).toBeVisible();
    await expect(redo).toBeVisible();
    const order = await cancelApplied.evaluate((cancel, redoText) => {
      const footer = cancel.parentElement;
      const redoButton = [...(footer?.querySelectorAll('button') ?? [])].find(
        (button) => button.textContent?.includes(redoText),
      );
      if (!redoButton) return false;
      return (
        (cancel.compareDocumentPosition(redoButton) &
          Node.DOCUMENT_POSITION_FOLLOWING) !==
        0
      );
    }, 'Refaire le tirage');
    expect(order).toBe(true);

    await cancelApplied.click();
    const confirm = page.getByRole('dialog', { name: 'Annuler le tirage ?' });
    await expect(confirm).toContainText(
      'Les équipes restent à leur place et ne sont plus protégées.',
    );
    await expect(confirm).not.toContainText('Des matchs sont générés');
    await confirm.getByRole('button', { name: 'Annuler le tirage' }).click();

    await expect(
      dialog.getByRole('button', { name: /Libérer #/ }),
    ).toBeVisible();
    const overview = await readStageOverview(request, stageId);
    const groupA = overview.groups?.find((group) => group.name === 'A');
    const keptIds = kept.map((team) => team.entryId);
    expect(groupA?.entryIds).toEqual(expect.arrayContaining(keptIds));
    const cancelled = overview.draws?.find(
      (draw) => draw.status === 'Cancelled',
    );
    expect(cancelled?.actions?.release?.releasableCount).toBeGreaterThan(0);
    expect(cancelled?.actions?.release?.releasableCount).toBeLessThan(
      (overview.groups ?? []).reduce(
        (sum, group) => sum + group.entryIds.length,
        0,
      ),
    );
  });

  test('clears a fixed team after apply and keeps a drawn team protected', async ({
    page,
    request,
  }) => {
    test.setTimeout(120_000);
    const competition = await prepareCompetition(request, MANUAL_DRAW);
    const stageId = await getPrimaryStageId(request, competition.id);
    await discardDraws(request, stageId);
    await page.goto(`/competitions/${competition.id}/structure`);

    const stage = await readHubStage(request, competition.id);
    const roster = await readRoster(request, competition.id);
    const kept = [
      teamsOfPot(stage, roster, 1)[0],
      teamsOfPot(stage, roster, 2)[0],
    ];
    await placeNamedTeam(page, 'Groupe A', kept[0].label);
    await placeNamedTeam(page, 'Groupe A', kept[1].label);
    await launchAndValidateDraw(page);

    const groupACard = page
      .locator('.regulation-schematic__card')
      .filter({ hasText: 'Groupe A' });
    const before = await readStageOverview(request, stageId);
    const groupA = before.groups?.find((group) => group.name === 'A');
    const keptIds = kept.map((team) => team.entryId);
    const drawnId = groupA?.entryIds.find((id) => !keptIds.includes(id));
    expect(drawnId).toBeTruthy();
    const drawnName = roster.find(
      (entry) => entry.entryId === drawnId,
    )?.displayName;
    expect(drawnName).toBeTruthy();

    const drawnSlot = groupACard
      .locator('.schematic-slot')
      .filter({ hasText: drawnName! });
    await expect(
      drawnSlot.getByRole('button', { name: /modifier ou retirer/ }),
    ).toHaveCount(0);
    const drawnReasonId = await drawnSlot.getAttribute('aria-describedby');
    expect(drawnReasonId).toBeTruthy();
    await expect(page.locator(`[id="${drawnReasonId}"]`)).toContainText(
      'placée par le tirage appliqué',
    );

    const fixedSlot = groupACard
      .locator('.schematic-slot')
      .filter({ hasText: kept[0].label });
    await expect(fixedSlot).not.toContainText('placée par le tirage appliqué');
    await fixedSlot
      .getByRole('button', { name: /modifier ou retirer/ })
      .click();
    const placement = placementDialog(page);
    await expect(placement).toBeVisible();
    await placement
      .getByRole('checkbox', {
        name: kept[0].label,
        exact: true,
        checked: true,
      })
      .click();
    await savePlacementDialog(placement);
    await expect(
      groupACard.getByText(kept[0].label, { exact: true }),
    ).toHaveCount(0);

    const after = await readStageOverview(request, stageId);
    const applied = after.draws?.find((draw) => draw.status === 'Published');
    expect(applied?.isApplied).toBe(true);
    expect(applied?.actions?.redo?.state).toBe('Available');
    const stillDrawn = groupACard
      .locator('.schematic-slot')
      .filter({ hasText: drawnName! });
    await expect(
      stillDrawn.getByRole('button', { name: /modifier ou retirer/ }),
    ).toHaveCount(0);
    const stillDrawnReasonId =
      await stillDrawn.getAttribute('aria-describedby');
    expect(stillDrawnReasonId).toBeTruthy();
    await expect(page.locator(`[id="${stillDrawnReasonId}"]`)).toContainText(
      'placée par le tirage appliqué',
    );
  });
});
