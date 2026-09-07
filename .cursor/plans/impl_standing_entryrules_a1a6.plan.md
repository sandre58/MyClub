---
name: Impl Standing EntryRules A1-A6
overview: "Plan technique validé (clarifications 2026-09-07) : prédicat Domain pour construire/valider ; StandingRules != null = signal runtime canonique ; ReplaceStandingRules rejet explicite ; nullable = deux états métier A5. Lots Domain → Runtime → Contrats → UX."
todos:
  - id: lot1-domain
    content: "Lot 1 Domain : prédicat isClassifyingPhase + StandingRules? + invariant A5 + MaterializeFrom + ReplaceStandingRules rejet + tests"
    status: pending
  - id: lot2-runtime
    content: "Lot 2 Runtime : Calculator/Qualif/Swiss exigent Standing ; Consultation Applicable via StandingRules != null"
    status: pending
  - id: lot3-contrats
    content: "Lot 3 Contrats/bootstrap : API/DTO nullable + recipes/dev fixtures"
    status: pending
  - id: lot4-ux
    content: "Lot 4 UX : hub Règlement defaults ; Structure capacité ; readiness inscrits/max + Structure N"
    status: pending
isProject: false
---

# Plan technique — EntryRules & StandingRules (A1–A6)

**Décision :** [EntryRules & StandingRules A1–A6](https://app.notion.com/p/3d475fb92d10811c91fde97676bfeb9e)  
**SoT :** [Regulation & Rules](https://app.notion.com/p/3b575fb92d108130bf8aff59d86ce9da)  
**Tâche :** [Domain — StandingRules conditionnel](https://app.notion.com/p/3d475fb92d10811895d2c18c0f58005b)

**Statut :** plan **validé** avec clarifications (2026-09-07) — prêt à implémenter.

**Hors scope :** capacité Domain dédiée ; gate Prepare/Start sur `MinimumTeams` ; rename Domain obligatoire `StandingDefaults`.

---

## Clarifications figées (avant implémentation)

### C1 — Un seul signal runtime

Le prédicat `isClassifyingPhase` sert à **construire / valider** l’état (A5) :

```text
phase classante     → StandingRules obligatoire (non-null)
phase non classante → StandingRules absent (null)
```

Après matérialisation, **reads et runtime** ne recalculent **pas** le prédicat. Signal canonique :

```text
Stage.StandingRules != null  →  standings applicables / calcul possible
Stage.StandingRules == null  →  pas de standing
```

Évite des définitions concurrentes de « classante » dans ConsultationAssembler et ailleurs.

### C2 — ReplaceStandingRules : rejet explicite

- Stage **non classante** + `ReplaceStandingRules(...)` → **rejet Domain explicite** (pas de no-op silencieux)
- Stage **classante** + `StandingRules == null` → **état impossible** (invariant visible à la construction / ReplaceRegulation)

### C3 — Nullable sémantique (pas « null partout »)

`StandingRules?` sur `StageRegulation` représente **exactement** les deux états métier valides sous A5 — **pas** une permission générale de laisser Standing null sur une phase classante.

---

## Objectif

- Construction size = **Structure**
- `EntryRules` = readiness (min) + plafond (max) — **peut différer** de la capacité Structure (**pas** d’erreur Domain)
- `Competition.StandingRules` = **defaults de seed** (sémantique produit)
- `Stage.StandingRules?` = runtime ssi phase classante (invariant A5)

---

## Lot 1 — Domain

1. Formaliser le prédicat Domain `isClassifyingPhase` (formats/règles ; **pas** `QualificationRules` seul). Groups sans Qualif → classante. Cup + Progression seule → non classante.
2. `StageRegulation.StandingRules?` — nullable sémantique (C3).
3. Invariant A5 à l’écriture / MaterializeFrom / ReplaceRegulation de phase.
4. `MaterializeFrom` :
   - phase classante **sans** Standing explicite → seed depuis `Competition.Regulation.StandingRules` (defaults A4)
   - phase non classante → Standing **null** (ignore defaults)
5. `ReplaceStandingRules` → rejet explicite si non classante (C2).
6. **Tests Domain immédiatement** autour de ces invariants.

Fichiers centraux : [`StageRegulation.cs`](src/PlayUp/MyClub.PlayUp.Domain/Rules/StageRegulation.cs), [`Regulation.cs`](src/PlayUp/MyClub.PlayUp.Domain/Rules/Regulation.cs), [`Stage.cs`](src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs).

`EntryRules` : **aucun** nouvel invariant Domain (min soft UX ; max AddEntry App).

### Tests Domain clés

| Cas | Attendu |
|-----|---------|
| Cup : MaterializeFrom | `StandingRules == null` |
| Phase classante sans Standing explicite | reçoit defaults Competition (miroir A4) |
| Championship / Groups / Swiss | Standing non-null |
| Groups sans Qualif | Standing requis |
| ReplaceStandingRules sur Cup | **rejet** Domain |
| EntryRules 16–16 + bracket 32 | pas d’erreur Domain |

---

## Lot 2 — Runtime / Reads

7. `StandingCalculator`, Qualification, Swiss : exiger Standing **non-null** quand leur exécution le nécessite (fail explicite sinon).
8. [`ConsultationAssembler`](src/PlayUp/MyClub.PlayUp.Application/Reads/ConsultationAssembler.cs) : `Applicable` dérivé de **`Stage.StandingRules != null`** — **pas** d’un second prédicat `IsClassifyingPhase` / plus de cache-misère `CupFormat` seul comme vérité.

---

## Lot 3 — Contrats / bootstrap

9. API/DTO si le nullable traverse les contrats Host.
10. Recipes / bootstrap / fixtures Dev (Cup sans Standing forcé).

---

## Lot 4 — UX

11. Hub Règlement : Standing Competition = **defaults de classement** ; Entrées = readiness / plafond — **pas** « construit pour N ».
12. Structure : capacité / topologie explicite.
13. Readiness : inscrits / max + éventuellement « Structure : N places » (sans forcer égalité Domain).

---

## Critères de done

- [ ] Domain conforme A5/A6 + C1–C3 ; SoT « écart code temporaire » retiré
- [ ] Aucune capacité Domain dédiée
- [ ] Max ≠ Structure capacity ≠ erreur Domain
- [ ] Reads consomment `StandingRules != null` uniquement
- [ ] ReplaceStandingRules rejette les phases non classantes
- [ ] Tests miroir MaterializeFrom (Cup null / classante seed) verts
- [ ] Hub Règlement : defaults, pas classement global
