import { expect, test } from '@playwright/test';
import {
  findChampionshipReadyCompetition,
  findFirstScheduledMatch,
  openSportiveExploitation,
  playUpApiBase,
} from './hostSeed';

/**
 * Host + PostgreSQL E2E — real Read/Command path (championship-ready seed).
 * CI: e2e-integration job seeds DB and starts Host before Playwright.
 */
test.describe('Play’Up business (Host + PostgreSQL)', () => {
  test('Match — démarrer puis terminer (Championnat Ready)', async ({
    page,
    request,
  }) => {
    const competition = await findChampionshipReadyCompetition(request);
    const { matchId, stageId } = await findFirstScheduledMatch(
      request,
      competition.id,
    );
    await openSportiveExploitation(request, competition.id, stageId);

    const matchResponse = await request.get(
      `${playUpApiBase()}/matches/${matchId}`,
    );
    if (!matchResponse.ok()) {
      throw new Error(
        `GET /matches/${matchId} failed (${matchResponse.status()})`,
      );
    }
    const match = (await matchResponse.json()) as {
      home: { displayName?: string | null };
      away: { displayName?: string | null };
    };
    const homeName = match.home.displayName?.trim() || 'Domicile';
    const awayName = match.away.displayName?.trim() || 'Extérieur';

    await page.goto(`/matches/${matchId}`);

    await expect(page.getByText('Planifié')).toBeVisible();
    await page.getByRole('button', { name: 'Démarrer le match' }).click();
    await expect(page.getByText('En direct')).toBeVisible();

    await page.getByLabel(new RegExp(`${homeName} — résultat`, 'i')).fill('2');
    await page.getByLabel(new RegExp(`${awayName} — résultat`, 'i')).fill('1');
    await page.getByRole('button', { name: 'Terminer le match' }).click();

    await expect(page.getByText('Terminé')).toBeVisible();
  });
});
