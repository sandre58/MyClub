# C-20 — `ReplaceStandingRules` en phase `Ready` — audit lecture seule

**Status:** Audit (2026-10-16) — **aucune implémentation** ; **recommandation décisionnelle H1** (2026-10-16) — **go implémentation distinct**.

**Contexte** — **Δ-01** (strip réglement Prêt) et **D4** (matrice §3 `gameRegulation`) : match / standing / confrontation = **lecture seule** en `Ready`. Le **hub** structure et la **SPA** n’exposent pas `ReplaceStandingRules` en `Ready` (aligné Δ-01). Le **domaine** et l’**API** acceptent encore `ReplaceStandingRules` en `Ready` pour une phase classifiante — écart **capacité** (même famille que **C-18** / C-19).

**Références** — [phase-mutability-matrix.md](./phase-mutability-matrix.md) (D4, ligne `ReplaceStageStandingRulesAsync`) · Notion [Matrice UI Structure](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4) (C-20).

---

## Synthèse (4 couches)

| Couche | Comportement actuel (`Ready`, phase classifiante) | Aligné D4 / Δ-01 ? |
|--------|---------------------------------------------------|---------------------|
| **SPA** | Strip réglement : `canEditStanding` = `actions.includes('ReplaceStandingRules')` → **false** en Prêt (pas de crayon) | **Oui** |
| **Hub** | `BuildReadyStageActions` — **pas** de `ReplaceStandingRules` (seulement ops : qual/prog/affectation/slots Cup…) | **Oui** |
| **API** | `PUT /stages/{id}/standing-rules` → **204** si domaine accepte | **Non** — voie directe |
| **Domaine** | `EnsureStandingRulesMutable()` refuse seulement **`Completed`** ; `Ready` / `Running` / `Draft` / `Suspended` → **autorisé** | **Non** vs D4 gel |

**Exception documentée (matrice)** — En **Running** (et **Suspended** via exploitation), standing replace reste **autorisé** domaine + hub + crayon strip (classifiant) — **R-Stand1** / §8. **C-20** porte sur **`Ready` uniquement**, pas sur l’exception Running.

---

## D4 et décisions déjà actées

| Artefact | Contenu pertinent |
|----------|-------------------|
| **D4** | `gameRegulation` : Allow Draft · **Deny Ready** · Deny si production · exception standing **après Start** (Running) |
| **Δ-01 fermée** | Strip Prêt digest sans crayons ; hub sans tokens match/standing/tie — **pas** de chantier SPA imposé si déjà conforme |
| **C-20 (registre C)** | Dette **enforcement métier** distincte de Δ-01 UI — explicite dans fiche Δ-01 / matrice Notion |

**Arbitrage** — Recommandation **H1** (refus domaine/API en `Ready`, symétrie C-18 / Δ-01 / D4). **H2** (D4 = contrainte UI seule) reste défendable uniquement si le produit le confirme explicitement — **non** retenu dans la recommandation actuelle. **Go C-20** = décision séparée avant code.

---

## Hub — `StructureViewAssembler`

**Fichier** — `StructureViewAssembler.cs`

| Statut | `ReplaceStandingRules` au hub ? |
|--------|--------------------------------|
| **Draft** | Oui si classifiant + standing bound (`BuildDraftStageActions`) |
| **Ready** | **Non** (`BuildReadyStageActions` L824–852) |
| **Running / Suspended** | Oui si classifiant + `StandingRules` présentes (`BuildExploitationStageActions` L855–865) |
| **Completed** | Non (exploitation / draft builders non utilisés ; actions `[]`) |

**Tests** — `StructureView_exposes_operational_actions_when_stage_is_Ready_without_production` : ne contient **pas** `ReplaceStandingRules`. Running : `StructureView_exposes_rename_and_standing_when_stage_is_Running_classifying` contient le token.

---

## API — contrat HTTP

| Élément | Détail |
|---------|--------|
| Route | `PUT /stages/{stageId:guid}/standing-rules` |
| Corps | `ReplaceStageStandingRulesRequest` (points + critères) |
| Executor | `ReplaceStageStandingRulesAsync` → `ReplaceStageStandingRules.Execute` → `stage.ReplaceStandingRules` |
| Garde application | Aucune garde lifecycle **Ready**-spécifique (contrairement à structure match rules via `EnsureStructureMutable` sur d’autres ops) |

**Tests Host** — `ReplaceStandingRulesEndpointTests` : **Running** 204, Cup invariant, missing 404 ; **pas** de cas **Ready** documenté.

---

## Domaine — `Stage.ReplaceStandingRules`

**Fichier** — `Stage.cs` (L317–334, `EnsureStandingRulesMutable` L2764–2771)

- `EnsureStandingRulesMutable` : bloque **`Completed`** uniquement (`StageErrorCodes.InvalidTransition`).
- **`Ready`** : mutation **autorisée** si phase classifiante et `StandingRules` présentes.
- Commentaire domaine : « Allowed after Start » sur la méthode — **ne mentionne pas** l’interdiction Ready de D4.

**Tests application** — `ReplaceStageStandingRulesTests` : Running OK, Completed rejeté, Cup non classifiant rejeté ; **aucun** test Ready allow/deny.

---

## SPA (hors strip structure — point de vigilance)

| Surface | Comportement |
|---------|----------------|
| `StructurePhaseFiche` + strip | Prêt : pas de token → pas de crayon standing (Δ-01) |
| `StageMatchStandingDialogs` | `canReplace` = `stage.actions.includes('ReplaceStandingRules')` — dépend du **hub stage** chargé pour la page match ; en pratique exploitation si token présent |

Pas d’écart Δ-01 identifié sur la fiche structure **Ready** ; C-20 ne vise pas à rouvrir le strip Prêt.

---

## Options d’alignement

| Option | Domaine / API | Hub | SPA | Note |
|--------|---------------|-----|-----|------|
| **H1 — Refus serveur en Ready** *(recommandé)* | `EnsureStandingRulesMutable` refuse **`Ready`** (+ `Completed` existant) | Déjà OK | Inchangé | Cohérent D4 ; protège API directe |
| **H2 — Statu quo capacité** | Ready autorisé | OK | OK | D4 = cible UI seulement ; dette assumée |
| **P — Assouplir D4** | Garder Ready | Exposer token + crayons | Rouvrir Δ-01 | **Rejet** par gel Δ-01 / D4 |

### Garde-fous H1 (à confirmer au go implémentation)

1. **Périmètre** — blocage **uniquement** en `Ready` (pas Draft ; pas Running / Suspended).
2. **Exception Running** — inchangée (replace autorisé classifiant, matrice §8 / C-13).
3. **HTTP** — `409 Conflict` + code métier **stable** (proposition : `Stage.StandingRulesLocked`, parallèle `Stage.DisplayNameLocked` / C-18 — **à valider** au go ; éviter `InvalidTransition` générique pour `Completed` seul).
4. **Tests** — domaine + application + Host : `Ready` → refus ; `Running` / `Suspended` / `Draft` (selon cas existants) → comportement actuel conservé.
5. **Présentation** — **aucun** changement strip / SPA / hub attendu si l’audit reste valide (tokens déjà absents en Prêt).

---

## Périmètre chantier proposé (après **go C-20** explicite)

1. Valider code erreur (`Stage.StandingRulesLocked` ou équivalent) + message domaine.
2. Domaine : étendre `EnsureStandingRulesMutable` (ou garde dédiée) — **Ready** refusé.
3. Tests domaine / application / Host (voir garde-fous § ci-dessus).
4. Matrice § `ReplaceStageStandingRulesAsync` + registre Notion **C-20** Closed.
5. **Hors scope** — exception Running/Suspended ; match/tie rules ; Δ-01 strip ; hub/SPA.

---

## Clôture audit

- **Livrable** — cette fiche ; pas de code.
- **Recommandation** — **H1** (2026-10-16).
- **Suite** — validation code erreur + **go implémentation** distinct ; sinon clôture documentée « H2 assumé ».
