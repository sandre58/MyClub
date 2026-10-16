# C-18 — `RenameStage` en phase `Completed` — audit lecture seule

**Status:** Audit (2026-10-16) — **aucune implémentation** ; arbitrage métier requis avant chantier.

**Contexte** — O-04 livré en SPA : crayon rename masqué en `Completed` (Δ-O4-1, `68d0f50a`). Le hub structure et l’API acceptent encore le renommage. **C-18** = alignement **capacité** hub/API (et éventuellement domaine) vs cible produit « titre lecture seule » en Terminée.

**Références** — [delta-o4-completed-fiche.md](./delta-o4-completed-fiche.md) · Notion [Matrice UI Structure](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4) (C-18, §4.3 header) · `phase-mutability-matrix.md` ligne `RenameStageAsync`.

---

## Synthèse (3 couches)

| Couche | Comportement actuel (`Completed`) | Aligné O-04 UI cible ? |
|--------|-----------------------------------|-------------------------|
| **SPA** | `canRename={actions.includes('RenameStage') && !isCompletedStage}` | **Oui** — pas d’affordance rename |
| **Hub** (`stage.actions`) | `RenameStage` **seul** (phase Completed) | **Non** — fausse capacité côté client |
| **API / domaine** | `POST /stages/{id}/rename` → **204** ; pas de garde statut phase | **Non** — mutation toujours possible (curl, futur client) |

**Écart type** — même famille que C-19 (hub P1 Ready) : **projection hub ≠ UI cible**, avec en plus une **voie HTTP non gardée** si l’on vise l’interdiction métier.

---

## Hub — `StructureViewAssembler.BuildStageActions`

**Fichier** — `src/PlayUp/MyClub.PlayUp.Application/Reads/StructureViewAssembler.cs`

| Condition | `actions` retournées |
|-----------|----------------------|
| Compétition `Completed` ou `Archived` | `[RenameStage]` uniquement |
| Phase `StageStatus.Completed` | `[RenameStage]` uniquement |
| Phase `Draft` / `Ready` / `Running` / `Suspended` | listes dédiées (souvent incluent `RenameStage` + ops) |

Constante : `ActionRenameStage = "RenameStage"` (L74). Branche Completed : L728.

**Tests** — aucun test automatisé sur hub **phase Completed** ou **compétition clôturée** ; seuls Running/Ready couverts (`StructureUseCaseTests.cs`).

---

## API — contrat HTTP

| Élément | Détail |
|---------|--------|
| Route | `POST /stages/{stageId:guid}/rename` |
| Corps | `RenameStageRequest { Name }` |
| Réponse succès | `204 No Content` |
| Auth / policy | *(inchangé par C-18 — hors périmètre audit)* |

**Fichiers** — `StageEndpoints.cs` (L93–106) · `UseCaseExecutor.RenameStageAsync` (L548–556) · `RenameStage.Execute` (Application).

**Garde-fous executor** — `RenameStageAsync` **n’appelle pas** `EnsureCompetitionAllowsLifecycleMutation` (contrairement à Prepare/Start/Complete, placement, etc.). Une compétition **Completed/Archived** peut donc encore renommer une phase via API tant que le stage existe.

**Tests Host** — **aucun** test d’intégration sur `/stages/.../rename` repéré dans `MyClub.PlayUp.Host.Tests`.

---

## Domaine — `Stage.Rename`

**Fichier** — `src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs` (L958–973)

- Documentation domaine : renommage autorisé **à tout statut lifecycle** — « display name is not structure-critical ».
- Validation : `StageName` uniquement ; no-op si nom inchangé.
- **Aucun** `EnsureStructureMutable` / garde `Completed`.

**Tests domaine / application** — `Rename_is_allowed_while_Running` (`LocaleStructureTests.cs`) ; **pas** de test « refusé en Completed » (car refus inexistant).

**Inventaire matrice** — `phase-mutability-matrix.md` : `RenameStageAsync` · garde « — » · hub `RenameStage` · famille **Meta**.

---

## SPA — consommation du token

| Surface | Usage |
|---------|--------|
| `StructurePhaseFiche.tsx` | `canRename = actions.includes('RenameStage')` puis gate `!isCompletedStage` vers le header |
| `StructurePhaseFicheHeader.tsx` | Affiche le bouton rename si `canRename` (prop parent) |
| `api/stages.ts` | `renameStage` → POST rename (appelé par mutation si UI ouverte) |

Aucun autre écran web ne consomme `RenameStage` pour l’instant (grep codebase web).

---

## Scénarios de capacité (à trancher en produit)

1. **Phase `Completed`, compétition encore `Running`** — cas `championship:finished` (recette O-04) : hub expose `RenameStage`, API rename **réussit**, SPA **masque** le crayon.
2. **Compétition `Completed` / `Archived`** — hub = `[RenameStage]` seul ; API rename **toujours** sans garde compétition ; SPA selon statut phase (si phase Completed → pas de crayon).
3. **Client non-SPA** — toute intégration fondée sur `stage.actions` ou POST rename peut encore modifier le libellé en Terminée.

---

## Options d’alignement (non arbitrées)

| Option | Hub | Application / API | Domaine | Effort / risque |
|--------|-----|-------------------|---------|------------------|
| **H1 — Hub seul** | Retirer `RenameStage` si `stage.Status == Completed` (et ? compétition clôturée) | Inchangé | Inchangé | Faible ; **capacité HTTP résiduelle** |
| **H2 — Application** | Aligné sur refus executor | `RenameStageAsync` refuse si `Completed` (+ ? compétition clôturée) | Inchangé ou garde légère | Moyen ; contrat erreur à définir (`ApplicationErrorCodes`) |
| **D — Domaine** | Aligné | Délègue au domaine | `Rename` refuse `Completed` | Plus fort ; remet en cause doc « locale at any status » |
| **P — Produit inverse** | Garder hub + API | Garder | Garder | Réouvre O-04 UI (non retenu aujourd’hui) |

**Points à décider explicitement** (arbitrage métier — **pas** tranchés ici) :

1. Le renommage en phase **Terminée** doit-il être **interdit côté serveur**, ou seulement **non exposé** (hub) / **non affiché** (SPA déjà fait) ?
2. Même question pour **compétition** `Completed` / `Archived` : le seul token hub `RenameStage` est-il voulu (métadonnée d’archive) ?
3. Si refus serveur : **code d’erreur**, message UX (mutation copy), mise à jour **matrice** § Meta et registre Notion C-18.

---

## Périmètre chantier suggéré (après décision)

- **Probable** — `StructureViewAssembler.BuildStageActions` · tests `StructureUseCaseTests` (Completed + compétition clôturée).
- **Si H2+** — `UseCaseExecutor.RenameStageAsync` · test application · test Host rename 4xx.
- **Si D** — `Stage.Rename` + tests domaine · sync `phase-mutability-matrix.md`.
- **Hors scope C-18** — C-20 standing Ready ; autres tokens hub ; i18n SPA (déjà OK).

---

## Vérification manuelle rapide (post-décision)

1. Seed `championship:finished` → GET structure → `actions` contient ou non `RenameStage` selon cible.
2. `POST /stages/{id}/rename` sur phase Completed → 204 vs 4xx selon cible.
3. SPA : pas de crayon (régression Δ-O4-1).

---

## Clôture audit

- **Livrable** — cette fiche uniquement (lecture seule).
- **Suite** — décision produit sur H1 / H2 / D (ou variante) → fiche d’implémentation ou tâche Notion **C-18** avant code.
