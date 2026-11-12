import { expect, test, type Page } from '@playwright/test';

import {
  correctFinishedMatchResult,
  findFirstFinishedMatchOnStage,
  createIsolatedReleaseCompetition,
  findFreshnessActionsRecetteCompetition,
  findResolutionFreshnessQualSlotCompetition,
  findResolutionFreshnessScoreUpstreamCompetition,
  getStageIdByName,
  replaceQualificationRulesForStaleBadge,
} from './hostSeed';

/**
 * Host + PostgreSQL — resolution freshness badges on Structure.
 */
test.describe('Play’Up business (resolution freshness)', () => {
  test('Structure — badge Périmé after upstream score correction', async ({
    page,
    request,
  }) => {
    const competition =
      await findResolutionFreshnessScoreUpstreamCompetition(request);
    const leagueId = await getStageIdByName(request, competition.id, 'League');
    const terminalId = await getStageIdByName(
      request,
      competition.id,
      'Terminal',
    );
    const matchId = await findFirstFinishedMatchOnStage(request, leagueId);

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${terminalId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    await expect(page.getByText('Périmé')).toHaveCount(0);

    await correctFinishedMatchResult(request, matchId, 0, 2);

    await page.reload();
    await expect(page.getByText('Périmé').first()).toBeVisible();
  });

  test('Structure — badge Périmé after qualification rule replace', async ({
    page,
    request,
  }) => {
    const competition =
      await findResolutionFreshnessQualSlotCompetition(request);
    const leagueId = await getStageIdByName(request, competition.id, 'League');
    const terminalId = await getStageIdByName(
      request,
      competition.id,
      'Terminal',
    );

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${terminalId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    await expect(page.getByText('Périmé')).toHaveCount(0);

    await replaceQualificationRulesForStaleBadge(request, leagueId, terminalId);

    await page.reload();
    await expect(page.getByText('Périmé').first()).toBeVisible();
  });

  test('Structure — clic Périmé rejouable et place éditable', async ({
    page,
    request,
  }) => {
    const competition = await findFreshnessActionsRecetteCompetition(request);
    const stageId = await getStageIdByName(request, competition.id, 'Manuel');

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${stageId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();

    const place = page.locator('.schematic-slot').filter({
      has: page.getByRole('button', { name: 'Rejouable', exact: true }),
    });
    // Editable because the feed is a direct assignment. CascadeReplace
    // refuses that assignment, so the badge reaches the preview and the
    // case still opens placement. The two controls are siblings.
    await place.getByRole('button', { name: /Périmé/ }).click();
    await expect(
      page.getByText('Une affectation directe n’est jamais remplacée.'),
    ).toBeVisible();
    await expect(
      page.getByRole('dialog', { name: /Placement manuel/ }),
    ).toHaveCount(0);

    await place.getByRole('button', { name: 'Rejouable', exact: true }).click();
    await expect(
      page.getByRole('dialog', { name: /Placement manuel/ }),
    ).toBeVisible();
  });

  test('Structure — clic Périmé rejouable ouvre Remplacer', async ({
    page,
    request,
  }) => {
    const competition = await findFreshnessActionsRecetteCompetition(request);
    const stageId = await getStageIdByName(request, competition.id, 'Tableau');

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${stageId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    await page
      .locator('.schematic-slot')
      .getByRole('button', { name: /Périmé/ })
      .click();
    await expect(
      page.getByRole('dialog', { name: /remplacement en cascade/i }),
    ).toBeVisible();
  });

  test('Structure — clic Amont périmé ouvre la phase amont', async ({
    page,
    request,
  }) => {
    const competition = await findFreshnessActionsRecetteCompetition(request);
    const avalId = await getStageIdByName(request, competition.id, 'Aval');
    const tableauId = await getStageIdByName(
      request,
      competition.id,
      'Tableau',
    );

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${avalId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    await page
      .locator('.schematic-slot')
      .getByRole('link', {
        name: /Périmé.*Phase amont périmée.*Aller à la phase amont/,
      })
      .click();
    await expect(page).toHaveURL(new RegExp(`stage=${tableauId}`));
  });

  test('Structure — clic match non joué ouvre le match', async ({
    page,
    request,
  }) => {
    const competition = await findFreshnessActionsRecetteCompetition(request);
    const stageId = await getStageIdByName(request, competition.id, 'A jouer');

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${stageId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    await page
      .locator('.schematic-slot')
      .getByRole('button', {
        name: /Non confirmée.*n'est pas joué.*Libérer la place/,
      })
      .click();
    await page
      .getByRole('dialog', { name: 'Libérer cette place ?' })
      .getByRole('link', { name: /Jouer le match/ })
      .click();
    await expect(page).toHaveURL(/\/matches\/[0-9a-f-]+/);
    await expect(page.getByRole('heading').first()).toBeVisible();
  });

  test('Structure — libérer une place vide puis un match non joué', async ({
    page,
    request,
  }) => {
    const empty = await createIsolatedReleaseCompetition(request, false);
    await releasePlace(page, empty, false);

    const generated = await createIsolatedReleaseCompetition(request, true);
    await releasePlace(page, generated, true);
  });
});

async function releasePlace(
  page: Page,
  seeded: Awaited<ReturnType<typeof createIsolatedReleaseCompetition>>,
  withMatch: boolean,
) {
  await page.goto(
    `/competitions/${seeded.competitionId}/structure?stage=${seeded.stageId}`,
  );
  await expect(page.getByRole('heading', { name: 'Structure' })).toBeVisible();
  const stageName = withMatch ? 'Généré' : 'Vide';
  await expectAttentionRow(page, stageName, true);

  const place = page.locator('.schematic-slot').filter({
    hasText: seeded.team,
  });
  await place.getByRole('button', { name: /Libérer la place/ }).click();
  await expect(
    page.getByRole('dialog', { name: 'Libérer cette place ?' }),
  ).toBeVisible();
  if (withMatch) {
    await expect(
      page.getByText(`Manche 1 — domicile — ${seeded.matchLabel}`),
    ).toBeVisible();
  } else {
    await expect(page.getByText('Aucun match ne sera modifié.')).toBeVisible();
  }
  await expect(
    page.getByText(`${seeded.team} quitte aussi la composition.`),
  ).toBeVisible();
  await page.getByRole('button', { name: 'Libérer', exact: true }).click();
  await expect(
    page.getByRole('dialog', { name: 'Libérer cette place ?' }),
  ).toHaveCount(0);
  await expect(place.getByText(seeded.team)).toHaveCount(0);
  await expect(
    place.getByRole('button', { name: /Libérer la place/ }),
  ).toHaveCount(0);
  await expectAttentionRow(page, stageName, false);
}

async function expectAttentionRow(
  page: Page,
  stageName: string,
  visible: boolean,
) {
  const open = page.getByRole('button', { name: /À traiter, \d+ éléments?/ });
  // ResolutionIndetermine is the only line this release removes. When that
  // was the last item, the trigger stays visible and disabled.
  const cleared = page.getByRole('button', {
    name: 'À traiter, aucun élément',
  });
  if (!visible) {
    await expect(cleared.or(open)).toBeVisible();
    if (await cleared.isVisible()) {
      await expect(cleared).toBeDisabled();
      return;
    }
  }
  await open.click();
  const row = page.getByRole('link', {
    name: new RegExp(`non confirmée.*${stageName}`),
  });
  if (visible) {
    await expect(row).toBeVisible();
  } else {
    await expect(row).toHaveCount(0);
  }
  await page.getByRole('button', { name: 'Fermer À traiter' }).click();
}
