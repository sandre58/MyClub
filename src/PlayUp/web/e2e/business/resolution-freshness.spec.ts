import { expect, test } from '@playwright/test';

import {
  correctFinishedMatchResult,
  findFirstFinishedMatchOnStage,
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
      .getByRole('link', {
        name: /Indéterminé.*n'est pas joué.*Jouer le match/,
      })
      .click();
    await expect(page).toHaveURL(/\/matches\/[0-9a-f-]+/);
    await expect(page.getByRole('heading').first()).toBeVisible();
  });
});
