import { expect, test, type Page } from '@playwright/test';

function playUpE2eArtifactsDir(): string | null {
  const dir = process.env.PLAYUP_E2E_ARTIFACTS?.trim();
  return dir && dir.length > 0 ? dir : null;
}

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

async function countPerimeBadgeControls(page: Page) {
  return (
    (await page.getByRole('img', { name: /Périmé/ }).count()) +
    (await page.getByRole('button', { name: /Périmé/ }).count()) +
    (await page.getByRole('link', { name: /Périmé/ }).count())
  );
}

/** No visible « Périmé » copy before mutation (labels live in tooltips / aria). */
async function expectFreshnessNotSurfacedAsText(page: Page) {
  await expect(page.getByText('Périmé')).toHaveCount(0);
}

async function expectNoPerimeBadgeControls(page: Page) {
  await expect(await countPerimeBadgeControls(page)).toBe(0);
}

async function expectFirstPerimeBadgeVisible(page: Page) {
  await expect(
    page
      .getByRole('button', { name: /Périmé/ })
      .or(page.getByRole('img', { name: /Périmé/ }))
      .or(page.getByRole('link', { name: /Périmé/ }))
      .first(),
  ).toBeVisible();
}

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
    await expectFreshnessNotSurfacedAsText(page);
    await expectNoPerimeBadgeControls(page);
    await correctFinishedMatchResult(request, matchId, 0, 2);

    await page.reload();
    await expectFirstPerimeBadgeVisible(page);
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
    await expectFreshnessNotSurfacedAsText(page);
    await expectNoPerimeBadgeControls(page);
    await replaceQualificationRulesForStaleBadge(request, leagueId, terminalId);

    await page.reload();
    await expectFirstPerimeBadgeVisible(page);
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
    await expect(
      place.getByRole('img', {
        name: /Périmé.*ne peut pas être remplacée automatiquement/i,
      }),
    ).toBeVisible();
    await expect(place.getByRole('button', { name: /Périmé/ })).toHaveCount(0);
    const artifactDir = playUpE2eArtifactsDir();
    if (artifactDir) {
      await place
        .getByRole('img', {
          name: /Périmé.*ne peut pas être remplacée automatiquement/i,
        })
        .hover();
      await page.waitForTimeout(450);
      await page.screenshot({
        path: `${artifactDir}/recette3-perime-non-remplacable.png`,
      });
      await page.screenshot({
        path: `${artifactDir}/recette3-tooltip-separator-perime.png`,
      });
    }
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
    const cascadeDialog = page.getByRole('dialog', {
      name: /remplacement en cascade/i,
    });
    await expect(cascadeDialog).toBeVisible();
  });

  test('Structure — capture toast 409 cascade preview', async ({
    page,
    request,
  }) => {
    const artifactDir = playUpE2eArtifactsDir();
    test.skip(!artifactDir, 'Set PLAYUP_E2E_ARTIFACTS to capture screenshots.');

    const competition = await findFreshnessActionsRecetteCompetition(request);
    const stageId = await getStageIdByName(request, competition.id, 'Tableau');
    await page.goto(
      `/competitions/${competition.id}/structure?stage=${stageId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    const replace = page
      .locator('.schematic-slot')
      .getByRole('button', { name: /Périmé/ })
      .first();
    await expect(replace).toBeVisible({ timeout: 30_000 });
    await page.route('**/resolution/cascade-preview', async (route) => {
      await route.fulfill({
        status: 409,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'Application.CascadeReplaceDirectAssignment',
          title: 'Direct assignment',
          status: 409,
        }),
      });
    });
    await replace.click();
    const toast = page.locator('.ds-toast');
    await expect(toast.first()).toBeVisible({ timeout: 15_000 });
    await page.waitForTimeout(300);
    await page.screenshot({
      path: `${artifactDir}/recette3-toast-409.png`,
    });
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
      .filter({ hasText: 'Nord' })
      .getByRole('button', {
        name: /Non confirmée — .*le match.*n'est pas joué\. Libérer la place/,
      })
      .click();
    const releaseDialog = page.getByRole('dialog', {
      name: 'Libérer cette place ?',
    });
    await expect(releaseDialog).toBeVisible();
    const artifactDir = playUpE2eArtifactsDir();
    if (artifactDir) {
      await releaseDialog.screenshot({
        path: `${artifactDir}/recette3-release-dialog-secondary.png`,
      });
    }
    await releaseDialog.getByRole('button', { name: /Jouer le match/ }).click();
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

  test('Structure — dialog égalité pour captures recette', async ({
    page,
    request,
  }) => {
    const artifactDir = playUpE2eArtifactsDir();
    test.skip(!artifactDir, 'Set PLAYUP_E2E_ARTIFACTS to capture screenshots.');

    const competition = await findFreshnessActionsRecetteCompetition(request);
    const stageId = await getStageIdByName(
      request,
      competition.id,
      'A départager',
    );
    await page.goto(
      `/competitions/${competition.id}/structure?stage=${stageId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();
    await page
      .locator('.schematic-slot')
      .filter({ hasText: 'Nord' })
      .getByRole('button', { name: /Libérer la place/ })
      .click();
    const dialog = page.getByRole('dialog', {
      name: 'Libérer cette place ?',
    });
    await expect(dialog).toBeVisible();
    await dialog.screenshot({
      path: `${artifactDir}/recette3-release-dialog-egalite.png`,
    });
    await dialog.getByRole('button', { name: 'Annuler' }).click();
  });

  test('Structure — libération depuis Qualifiés alignée sur la schématique', async ({
    page,
    request,
  }) => {
    const competition = await findFreshnessActionsRecetteCompetition(request);
    const stageId = await getStageIdByName(request, competition.id, 'A jouer');
    const team = 'Nord';

    await page.goto(
      `/competitions/${competition.id}/structure?stage=${stageId}`,
    );
    await expect(
      page.getByRole('heading', { name: 'Structure' }),
    ).toBeVisible();

    const slot = page.locator('.schematic-slot').filter({ hasText: team });
    await slot.getByRole('button', { name: /Libérer la place/ }).click();
    const dialog = page.getByRole('dialog', {
      name: 'Libérer cette place ?',
    });
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'Annuler' }).click();
    await expect(dialog).toHaveCount(0);

    const qualifiers = page.locator('.structure-composition-qualifiers');
    await qualifiers.getByRole('button', { name: /Libérer la place/ }).click();
    await expect(
      page.getByRole('dialog', { name: 'Libérer cette place ?' }),
    ).toBeVisible();
    await expect(
      page.getByText(`${team} quittera aussi la composition de cette phase.`),
    ).toBeVisible();
    await page.getByRole('button', { name: 'Libérer', exact: true }).click();
    await expect(dialog).toHaveCount(0);
    await expect(slot.getByText(team)).toHaveCount(0);
    await expect(qualifiers.getByText(team)).toHaveCount(0);
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
      page.getByText(
        new RegExp(
          `Manche 1 \\(${seeded.matchLabel.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\) : le côté domicile redevient à déterminer`,
        ),
      ),
    ).toBeVisible();
  } else {
    await expect(page.getByText('Aucun match ne sera modifié.')).toBeVisible();
  }
  await expect(
    page.getByText(
      `${seeded.team} quittera aussi la composition de cette phase.`,
    ),
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
