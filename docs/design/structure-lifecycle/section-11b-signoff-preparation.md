# §11B — Sign-off preparation (maintainer pack)

**Date:** 2026-10-15  
**Git annex:** [phase-mutability-matrix.md](./phase-mutability-matrix.md)  
**§11A:** signed 2026-10-15  
**§11B:** **signed 2026-10-15** (Git mirror; canonical row in Notion Décisions)

This document satisfies the **maintainer** checklist before the product publishes a **Décision §11B** in Notion. Signing §11B authorizes implementation **only** for §12 rows in **Done** or explicit **post-sign reopen** of **Deferred** items — not automatic delivery of deferred gates.

---

## 1. Criteria §11B (product)

| ID | Criterion | Preparation status | Evidence |
|----|-----------|-------------------|----------|
| **B1** | Every §12 row is **Done**, **Deferred**, or **Constraint** — no orphan **Litigious** | **Ready** | §2 table below |
| **B2** | Reference cases §10 mapped; materialize deferred paths have **signed A5** contract tests | **Ready** | §3 table; gaps **R-Draw1+** documented |
| **B3** | Notion **Décision §11B** published (canonical) | **Draft ready** | §5 copy block |
| **B4** | Post-sign implementation scope explicit | **Ready** | [implementation-plan-post-11b.md](./implementation-plan-post-11b.md) |

---

## 2. B1 — §12 classification (§11B buckets)

| ID | §11B status | Shipped / reserved | Written reserve (Deferred) |
|----|-------------|--------------------|----------------------------|
| **G-UI1** | **Done** | 2026-10-15 | — |
| **G-UI2** | **Done** | 2026-10-15 | — |
| **G-MAT1** | **Done** | Cup materialize gate | — |
| **G-MAT2** | **Done** | Cup materialize gate (option B) | — |
| **G-MAT3** | **Deferred** | Hub warning only; **R-Mat-M2b** test | [g-mat3-arbitration-session.md](./g-mat3-arbitration-session.md) — no gate; M2a unattainable without contract revision |
| **G-MAT4** | **Deferred** | Analyzer + **R-Mat-M4b**; roster `MaterializationFailure` on target | [g-mat4-arbitration-session.md](./g-mat4-arbitration-session.md) — M4a redundant with roster path |
| **G-OPS1** | **Done** | qual / prog / affectation Ready+D2 | Further commands §9 → separate increments |
| **G-OPS2** | **Done** | D1 on same three commands | — |
| **G-OPS3** | **Deferred** | UI partial; **R-Draw1-N1..N3** | [g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md) — **D3-1** blocks gate |
| **G-SWISS** | **Constraint** | No global production guard | §8 capability grid |

**Litigious:** none remaining in §12.

---

## 3. B2 — §10 reference cases (verification)

| ID | Expected | Status | Tests / note |
|----|----------|--------|----------------|
| R-D1a / R-D1b | OPS + D1 | **Covered** | `*GOpsTests` |
| R-Draw1 | Refuse (target) | **Deferred** | Positif gap; [GOps3Tests](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageAffectationAuthoringGOps3Tests.cs) négatifs |
| R-Draw2 | Prepare vs drawer | **Covered** | `StructurePresentedAttentionCollectorTests` |
| R-Mat1 | Roster incomplete | **Covered** | `StructureMaterializeAssemblerTests` |
| R-Mat2 | D2 after materialize | **Covered** | `ReplaceStage*GOpsTests` D2 (qual/prog/affectation) |
| R-Mat-M1 | Undecidable materialize | **Covered** | `MaterializeCupFromOccupiedSlotsGMat1Tests` |
| R-Mat-M2 | NonRootUnfed | **Deferred A5** | `MaterializeCupFromOccupiedSlotsRMatM2bTests` |
| R-Mat-M4b | SourcePopulationShortfall foreign | **Deferred A5** | `MaterializeMatchesRMatM4bTests` |
| R-Swiss1/2 | Progressive APIs | **Covered** | `GenerateNextRoundTests`, `DematerializeSwissLastRoundTests` |
| R-UI1 | Drawer severity | **Covered** | G-UI1 shipped (host + web) |

**Agreed deferred positives (not blocking §11B):** R-Draw1 refuse command, G-MAT3 M2a, G-MAT4 M4a — each has reopen conditions in arbitration decisions.

---

## 4. Vocabulary (§11A vs §11B)

| Term | Meaning |
|------|---------|
| **Done** | Policy row implemented and tested per §12 notes |
| **Deferred** | Product **defers** gate or scope; **reserve écrite** + reopen conditions; hub/diagnostics may still warn |
| **Constraint** | Ongoing rule (G-SWISS) — not a backlog ticket |
| **Spec draft** | Git design artefact; for §11B counts as **Deferred** when gate deferred decision signed |

---

## 5. Notion — Décision §11B (copy block)

**Title:** [Play'Up] Structure phase lifecycle — contrat d'implémentation (§11B)

**Statut:** À valider · 2026-10-15 · Projet MyClub

**Lien annexe Git:** `docs/design/structure-lifecycle/phase-mutability-matrix.md` + `section-11b-signoff-preparation.md`

**Décision**

Le décideur signe le **contrat d'implémentation** suivant §11A (déjà acceptée) :

1. **Backlog §12** — livré : G-UI1/2, G-MAT1/2, G-OPS1/2. **Différé** avec réserve écrite : G-MAT3, G-MAT4, G-OPS3 (décisions d'arbitrage Git du 2026-10-15). **Contrainte** : G-SWISS (§8 matrice).
2. **Tests de non-régression A5** signés pour les gates différés : R-Mat-M2b, R-Mat-M4b, R-Draw1-N1..N3 — sans obligation d'implémenter les refus positifs tant que les conditions de réouverture ne sont pas remplies.
3. **Hors périmètre §11B** : extension G-OPS aux autres commandes §9, remédiation hub D1 au-delà du footer mutation, tiers de mutabilité domaine — sauf tâche Notion dédiée.
4. **Autorisation Agent** : implémenter uniquement les lignes §12 **Done** restantes si écart code↔doc découvert ; rouvrir un **Deferred** seulement après nouvelle décision produit (D3-1, M3/M4 reopen, etc.).

**Réserve A5** (§11A) : inchangée — un diagnostic hub n'est pas un veto materialize automatique.

---

## 6. Maintainer checklist (before Notion publish)

- [x] B1 table complete — no Litigious
- [x] B2 §10 aligned with tests
- [x] Arbitration decisions linked (G-MAT3, G-MAT4, G-OPS3)
- [x] Post-11B implementation plan file
- [x] Decision maker signs Notion §11B
- [x] Update Notion §11A page cross-link to §11B row
- [x] After Notion sign: tick B1–B4 in matrix §11B and set version note on matrix

---

## 7. After §11B signature (process)

1. Mark §11B checkboxes in [phase-mutability-matrix.md](./phase-mutability-matrix.md).
2. Do **not** auto-start deferred gates — track via Notion Task + reopen arbitration doc.
3. Use [implementation-plan-post-11b.md](./implementation-plan-post-11b.md) for allowed follow-up work.
