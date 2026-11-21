import { expect, test } from '@playwright/test';
import {
  ensureCupCompositionDrawPending,
  expectCupDrawAppliedOnStructure,
  findCupCompositionCompleteCompetition,
  getPrimaryStageId,
  runCupSlotDrawPublishAndApply,
} from './hostSeed';
import { applyCupPartialDrawAfterDirectAssignment } from './cup-partial-redo-helpers';

/**
 * Host + PostgreSQL E2E — critical path #3 (draw publish/apply).
 * Seed: cup-composition-complete (16/16 entries, draw rules engaged).
 * API and UI specs share that competition; each one restores the pending draw first.
 */
test.describe('Play’Up business (Host + PostgreSQL)', () => {
  test.describe.configure({ mode: 'serial' });

  test.beforeEach(async ({ request }) => {
    await ensureCupCompositionDrawPending(request);
  });

  test('Draw — créer, générer, publier et appliquer (Coupe composition complète)', async ({
    request,
  }) => {
    const competition = await findCupCompositionCompleteCompetition(request);
    const stageId = await getPrimaryStageId(request, competition.id);
    await runCupSlotDrawPublishAndApply(request, stageId);
    await expectCupDrawAppliedOnStructure(request, competition.id);
  });

  test('Draw — fenêtre, lancement et application (Coupe composition complète)', async ({
    page,
    request,
  }) => {
    const competition = await findCupCompositionCompleteCompetition(request);
    await page.goto(`/competitions/${competition.id}/structure`);

    await page
      .getByRole('button', { name: 'Lancer le tirage', exact: true })
      .click();

    const dialog = page.getByRole('dialog', { name: /Exécutions de tirage/ });
    await expect(dialog).toBeVisible();
    await dialog
      .getByRole('button', { name: 'Lancer le tirage', exact: true })
      .click();

    // Success toast pauses while the pointer rests on it and covers the footer.
    const created = page
      .getByRole('status')
      .filter({ hasText: 'Nouvelle exécution créée.' });
    await expect(created).toBeVisible();
    await created.getByRole('button', { name: 'Fermer', exact: true }).click();

    await dialog
      .getByRole('button', { name: 'Valider le tirage', exact: true })
      .click();

    await expect(dialog.getByRole('list', { name: 'Résultat' })).toBeVisible();
    await expect(
      dialog.getByRole('listitem', { name: /vs/ }).first(),
    ).toBeVisible();

    const published = page
      .getByRole('status')
      .filter({ hasText: 'Tirage publié et appliqué.' });
    await expect(published).toBeVisible();
    await published
      .getByRole('button', { name: 'Fermer', exact: true })
      .click();

    await expect(
      dialog.getByRole('button', { name: 'Annuler le tirage' }),
    ).toBeVisible();
    await expect(
      dialog.getByRole('button', { name: 'Refaire le tirage' }),
    ).toBeVisible();

    await dialog.getByRole('button', { name: 'Fermer', exact: true }).click();
    await expect(dialog).toBeHidden();
    await expect(
      page.getByText('Un tirage au sort a été appliqué pour cette phase.'),
    ).toBeVisible();
    await expect(
      page.getByRole('button', { name: 'Voir le tirage', exact: true }),
    ).toBeVisible();
  });

  test('Draw — Partial appliqué avec DA, Refaire au clavier et confirmRedoCup', async ({
    page,
    request,
  }) => {
    const competition = await findCupCompositionCompleteCompetition(request);
    const { manualSlotKey } = await applyCupPartialDrawAfterDirectAssignment(
      request,
      competition.id,
    );

    await page.goto(`/competitions/${competition.id}/structure`);
    await page
      .getByRole('button', { name: 'Voir le tirage', exact: true })
      .click();
    const dialog = page.getByRole('dialog', { name: /Exécutions de tirage/ });
    await expect(dialog).toBeVisible();

    const redo = dialog.getByRole('button', { name: 'Refaire le tirage' });
    const cancel = dialog.getByRole('button', { name: 'Annuler le tirage' });
    await expect(cancel).toBeVisible();
    await expect(redo).toBeVisible();
    await cancel.focus();
    await page.keyboard.press('Tab');
    await expect(redo).toBeFocused();

    await redo.click();
    const confirm = page.getByRole('dialog', { name: 'Refaire le tirage ?' });
    await expect(confirm).toContainText(
      'libère seulement les emplacements placés par le tirage',
    );
    await confirm.getByRole('button', { name: 'Refaire le tirage' }).click();
    await expect(confirm).toBeHidden();
    await expect(dialog.getByRole('list', { name: 'Résultat' })).toBeVisible();

    await dialog.getByRole('button', { name: 'Fermer', exact: true }).click();
    await expect(dialog).toBeHidden();
    await expect(
      page.locator(`[data-slot-key="${manualSlotKey}"]`),
    ).toBeVisible();
  });
});
