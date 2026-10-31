import { expect, test } from '@playwright/test';

import {
  correctFinishedMatchResult,
  findFirstFinishedMatchOnStage,
  findResolutionFreshnessQualSlotCompetition,
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
      await findResolutionFreshnessQualSlotCompetition(request);
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
});
