# C-18 — `RenameStage` en phase `Completed` — audit lecture seule

**Status:** **Closed** (2026-10-16) — commits `59d20b0a` (+ hub régression Draft/Suspended) — H1 + R1/R2 : domaine `CanRenameDisplayName` / `DisplayNameLocked`, hub `BuildStageActions`, tests hub + Host.

**Couverture hub `RenameStage` (post-C-18)** — `StructureUseCaseTests` : Draft · Ready · Running · Suspended → exposé ; Completed → absent ; R2 compétition clôturée + phase Running → exposé.

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

## Options d’alignement (terminologie produit)

| Option | Avantage | Limite |
|--------|----------|--------|
| **H1 — Interdiction serveur** | Contrat cohérent hub + API ; protège les appels directs | Définir les statuts bloquants ; tests Host ; erreur métier + HTTP |
| **H2 — Retrait hub seul** | Diff minimal côté projection | API accepte encore le rename → capacité résiduelle vs phase figée (O-04) |
| **D — Maintien volontaire** | Correction administrative post-clôture possible | Capacité **métier assumée**, pas écart UI ; réouvre la cible O-04 |

**Recommandation décisionnaire (2026-10-16, non = go impl.)** — **H1** : refuser le renommage **côté serveur** pour une phase `Completed`, avec **même règle** dans `BuildStageActions` et sur `POST …/rename` (erreur explicite). Aligné avec O-04 si « Terminée » = figée.

**Deux règles métier distinctes** (ne pas les fusionner en une seule condition implicite) :

| Règle | Cible recommandée (phase Completed) | Statut arbitrage |
|-------|--------------------------------------|------------------|
| **R1 — Phase `Completed`** | Refus domaine/API ; hub **sans** `RenameStage` | **Recommandation H1** — à confirmer au **go C-18** |
| **R2 — Compétition `Completed` / `Archived`** | **Refus rename uniquement via R1** (statut phase) — pas de gel additionnel au niveau compétition | **Arbitré 2026-10-16** — seul `Stage.Completed` bloque ; phase `Running`/`Ready` sous compétition clôturée : rename **autorisé** (hub/API/domaine alignés sur le statut phase) |

**R2** — Une compétition `Completed`/`Archived` ne doit **pas** à elle seule retirer `RenameStage` du hub pour une phase non `Completed` (ex. multi-phase partiellement terminée). Le hub actuel qui renvoie `[RenameStage]` seul pour toute compétition clôturée (L716–718) devra être **resserré** sous C-18 : projection selon **statut de phase**, pas seulement statut compétition.

**Détails techniques si H1 validé** (à préciser à l’impl.) :

- Couche de refus : domaine (`Stage.Rename`) vs application (`RenameStageAsync`) — préférer **une** source de vérité alignée matrice § Meta.
- Code `ApplicationErrorCodes` + mapping HTTP (4xx) ; pas de changement SPA attendu (Δ-O4-1).
- Mise à jour `phase-mutability-matrix.md` · registre Notion C-18.

---

## Périmètre chantier proposé (après go C-18 + R2 tranché)

1. Règles **R1** (+ **R2** selon arbitrage) sur `Stage.Completed` et `Competition.Completed` / `Archived`.
2. Même règle dans `BuildStageActions` et chemin `RenameStageAsync` / domaine.
3. Tests hub (`StructureUseCaseTests`) + Host/API (rename autorisé vs refusé).
4. Régression : Draft, Ready, Running, Suspended inchangés.
5. **Hors scope** — C-20 ; i18n SPA (déjà OK).

---

## Vérification manuelle rapide (post-décision)

1. Seed `championship:finished` → GET structure → `actions` contient ou non `RenameStage` selon cible.
2. `POST /stages/{id}/rename` sur phase Completed → 204 vs 4xx selon cible.
3. SPA : pas de crayon (régression Δ-O4-1).

---

## Clôture audit / suite

- **Livrable audit** — constats + options (commit `c067a40e`).
- **Implémentation** — **bloquée** jusqu’au **go C-18** explicite (H1 + R1 + R2 arbitrés 2026-10-16).
