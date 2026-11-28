import { expect, test } from '@playwright/test';
import {
  applyCupTwoLegJourneyProgression,
  createCupTwoLegJourneyShell,
  playUpApiBase,
  prepareCupTwoLegJourneyForPlay,
  readFinalSlotSEntryName,
} from './hostSeed';

/**
 * Host + PostgreSQL E2E — parcours Coupe 2 manches (une demi, progression vers la finale).
 */
test.describe('Play’Up business (Host + PostgreSQL)', () => {
  test('Coupe aller-retour — format, deux matchs, progression', async ({
    page,
    request,
  }) => {
    const shell = await createCupTwoLegJourneyShell(request);

    await page.goto(`/competitions/${shell.competitionId}/structure`);

    const confrontationPanel = page.locator('#tile-confrontation');
    await expect(
      confrontationPanel.getByRole('heading', {
        name: 'Format de confrontation',
      }),
    ).toBeVisible();
    await confrontationPanel.getByRole('button', { name: /Modifier/i }).click();

    const dialog = page.getByRole('dialog', {
      name: /Format de confrontation/,
    });
    await expect(dialog).toBeVisible();
    await dialog
      .getByRole('radiogroup', { name: 'Nombre de manches' })
      .getByRole('radio', { name: '2 manches' })
      .click();
    const extraTime = dialog.getByRole('switch', {
      name: 'Prolongations de confrontation',
    });
    if (!(await extraTime.isChecked())) {
      await extraTime.click();
    }
    const penalties = dialog.getByRole('switch', {
      name: 'Tirs au but de confrontation',
    });
    if (!(await penalties.isChecked())) {
      await penalties.click();
    }
    await dialog
      .getByRole('button', { name: 'Enregistrer', exact: true })
      .click();

    const saved = page
      .getByRole('status')
      .filter({ hasText: 'Format de confrontation enregistré' });
    if (await saved.isVisible().catch(() => false)) {
      await saved.getByRole('button', { name: 'Fermer', exact: true }).click();
    }
    await expect(dialog).toBeHidden();

    const structure = await request.get(
      `${playUpApiBase()}/competitions/${shell.competitionId}/structure`,
    );
    const view = (await structure.json()) as {
      stages: {
        stageId: string;
        defaultTieFormat?: { numberOfLegs: number } | null;
        confrontationSegments?:
          | {
              rounds: { inheritsDefaultTieFormat: boolean }[];
            }[]
          | null;
      }[];
    };
    const semi = view.stages.find((s) => s.stageId === shell.semiStageId);
    expect(semi?.defaultTieFormat?.numberOfLegs).toBe(2);
    const round = semi?.confrontationSegments
      ?.flatMap((segment) => segment.rounds)
      .find((roundRef) => roundRef);
    expect(round?.inheritsDefaultTieFormat).toBe(true);

    const ready = await prepareCupTwoLegJourneyForPlay(request, shell);

    const finishMatchWithScore = async (
      homeName: string,
      awayName: string,
      homeGoals: string,
      awayGoals: string,
    ) => {
      await page.getByRole('button', { name: 'Démarrer le match' }).click();
      await expect(page.getByText('En direct')).toBeVisible();
      await page
        .getByLabel(new RegExp(`${homeName} — résultat`, 'i'))
        .fill(homeGoals);
      await page
        .getByLabel(new RegExp(`${awayName} — résultat`, 'i'))
        .fill(awayGoals);
      await page.getByRole('button', { name: 'Terminer le match' }).click();
      await expect(page.getByText('Terminé')).toBeVisible();
    };

    await page.goto(`/matches/${ready.p1Leg1MatchId}`);
    await finishMatchWithScore(
      ready.leg1HomeName,
      ready.leg1AwayName,
      '2',
      '0',
    );

    await page.goto(`/matches/${ready.p1Leg2MatchId}`);
    await expect(page.getByText('Manche 2')).toBeVisible();
    await finishMatchWithScore(
      ready.leg2HomeName,
      ready.leg2AwayName,
      '0',
      '1',
    );

    await page.goto(`/competitions/${shell.competitionId}`);
    await expect(
      page.getByRole('link', { name: /Progression en attente/i }),
    ).toBeVisible();
    // Read overview : « Appliquer la progression » n’est pas le CTA principal tant
    // que la phase finale projette « Créer les matchs » (MaterializeFromOccupiedSlots).
    await applyCupTwoLegJourneyProgression(request, shell.semiStageId);

    const qualifiedName = await readFinalSlotSEntryName(
      request,
      shell.finalStageId,
    );
    expect(qualifiedName).toBe(ready.leg1HomeName);

    await page.goto(
      `/competitions/${shell.competitionId}/structure?stage=${shell.finalStageId}`,
    );
    await expect(page.getByText(ready.leg1HomeName)).toBeVisible();
  });
});
