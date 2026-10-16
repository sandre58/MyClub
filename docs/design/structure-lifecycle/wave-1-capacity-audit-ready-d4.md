# Wave 1 — Audit capacité (lecture seule) — D4 / tokens Structure en `Ready`

**Status:** Audit (2026-10-16) — **aucun correctif code** tant que les écarts ci-dessous ne sont pas **validés** par le décisionnaire.

**Périmètre** — Gel **A** : cohérence **hub structure** · **SPA** (strip réglement / tokens) · **API** · **domaine** pour le gel **D4** (match / standing / confrontation) et les tokens `Replace*` / lifecycle en phase **`Ready`**, hors production attachée (D2).

**Références** — [phase-mutability-matrix.md](./phase-mutability-matrix.md) D4 · Δ-01 · C-18 / C-19 / C-20 · [StructureViewAssembler.cs](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructureViewAssembler.cs) `BuildReadyStageActions`.

---

## Synthèse

| Verdict | Détail |
|---------|--------|
| **Écart capacité métier (type C-20)** | **Aucun identifié** après C-20 (`StandingRulesLocked`) |
| **Couverture tests** | Renforcer possible (hub Ready liste explicite ; Host PUT match-rules en Ready) — **pas** un écart produit si domaine refuse déjà |
| **Action code** | **Aucune** recommandée sans validation explicite d’un écart listé en §3 |

---

## Grille D4 — `Ready`, sans matchs attachés (`attachedMatches == 0`)

| Capacité D4 | Hub `Ready` | SPA strip (`StructurePhaseFiche`) | Domaine | Application / API | Aligné Δ-01 / D4 ? |
|-------------|-------------|-----------------------------------|---------|-------------------|---------------------|
| **Match rules** | Pas `ReplaceMatchRules` | `canEditMatch` false | `ReplaceMatchRules` → `StructureLocked` ([`StageStructureLockedInReadyTests`](../../../tests/MyClub.PlayUp.Domain.Tests/Stages/StageStructureLockedInReadyTests.cs)) | `EnsureStructureMutable` via use cases | **Oui** |
| **Standing rules** | Pas `ReplaceStandingRules` | `canEditStanding` false | `ReplaceStandingRules` → `StandingRulesLocked` (**C-20**) | Host `PUT …/standing-rules` → 409 | **Oui** |
| **Tie (default / round)** | Pas tokens tie | `canEditTie` false | `ReplaceDefaultTieFormat` / `ReplaceRoundTieFormat` → `EnsureStructureMutable` → `StructureLocked` | Pas de test Host dédié Ready | **Oui** (domaine) |
| **Draw rules (P1)** | Pas `ReplaceDrawRules` | `canAuthorDrawRulesP1` = Draft only (**C-19**) | `ReplaceStageDrawRules` Ready → `StructureLocked` ([`ReplaceStageDrawRulesTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageDrawRulesTests.cs)) | — | **Oui** |
| **Forme (matchday, group, cup slots…)** | Absents | — | `AddMatchday` etc. → `StructureLocked` | — | **Oui** |
| **Rename** | `RenameStage` | Header selon hub + Δ-O4 Completed | **Autorisé** en Ready (politique §11A) | — | **Oui** (hors D4) |
| **Ops Ready (qual / prog / placement / affectation)** | Présents si format | Bandes / dialogs selon tokens | Gardes D1/D2 application | G-OPS1/2 + **G-B-3** | **Oui** (D4 n’interdit pas) |

### `Ready` avec production (`attachedMatches > 0`)

Hub : **`[RenameStage]`** seul (`BuildReadyStageActions` L827–829). Aligné D2 (pas d’ops structurelles générales via hub).

---

## Écarts proposés (à valider avant tout code)

| ID | Nature | Proposition | Décision produit requise ? |
|----|--------|-------------|----------------------------|
| **W1-T1** | **Tests** — `StructureView_exposes_operational_actions_when_stage_is_Ready_without_production` ne nie pas explicitement `ReplaceMatchRules` / `ReplaceStandingRules` / tie tokens | Renforcer assertions hub (non-régression) | **Non** — qualité ; hors gel capacité |
| **W1-T2** | **Tests Host** — pas de `PUT …/match-rules` en Ready → 409 `StructureLocked` | Ajouter test intégration symétrique C-20 standing | **Non** — preuve API ; domaine déjà OK |
| **W1-T3** | **Tests Host** — pas de test HTTP `AppliedDrawPoolChangeBlocked` (G-B-3) | Voir [wave-2-g-b-3-g-ops3-closure.md](./wave-2-g-b-3-g-ops3-closure.md) | **Non** — slice livré côté application |

**Aucun W1-*** de type **refus métier manquant** identifié au-delà de C-20 (déjà livré).

---

## Hors périmètre Wave 1 (B différé — ne pas traiter comme bug recette gel)

- G-MAT3 / G-MAT4 (gates différés signés)
- G-B-1, C-ENF-*, D1 awards étendu
- G-OPS3 hors `ReplaceStageAffectationAuthoring` + pool **EntryId** set (voir Wave 2)
- Modèle de capacités §3 matrice (« target — not implemented »)

---

## Suite

1. **Décisionnaire** — valider « zéro écart capacité » ou pointer un ID W1-* à implémenter.
2. Si zéro — **feu vert recette gel A** (avec Wave 2 doc).
3. Si W1-T* validés — chantier tests uniquement, commits séparés.
