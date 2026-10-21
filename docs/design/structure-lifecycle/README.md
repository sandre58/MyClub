# Structure phase lifecycle — design artefacts

Product and technical references for **stage lifecycle** (Draft / Ready / Running), **mutability** (structure vs operational vs production), and **diagnostic policy** (Prepare, Materialize, Start, drawer).

## Documents

| Document | Purpose |
|----------|---------|
| [phase-mutability-matrix.md](./phase-mutability-matrix.md) | Exhaustive inventory (diagnostics + `UseCaseExecutor.Stages` commands), current vs target policy, reference cases, open arbitrations |
| [ctrl-o3-mat-devrunner-recette.md](./ctrl-o3-mat-devrunner-recette.md) | **CTRL-O3-MAT** gate — DevRunner `ctrl-o3-mat-recette` + automated read/SPA assertions (Suspended / production UI); includes **Δ-drawer-1** drawer recette |
| [cup-materialize-recette-protocol.md](./cup-materialize-recette-protocol.md) | **Cup materialize** — `CupMaterializeRecetteProtocolTests` + SPA checklist (R6–R8, jauge copy); DevRunner `structure-materialize-recette` / `cup-qf-sf` |
| [cup-lifecycle-start-mutability-arbitration.md](./cup-lifecycle-start-mutability-arbitration.md) | **Accepted** — Cup Start vs incomplete R0, D2 freeze, recovery; Notion Décisions SoT |
| [start-readiness-exploitable-arbitration.md](./start-readiness-exploitable-arbitration.md) | **Shipped** — Ch/G `NoExploitableInitialConfrontations` Start gate; pointer to § H |
| [r7-schematic-case-primary-arbitration.md](./r7-schematic-case-primary-arbitration.md) | **R7 Accepted** — schematic case primary: resolved participant; Qual/Prog path in tooltip |
| [p3-composition-band-visibility-matrix-v0.md](./p3-composition-band-visibility-matrix-v0.md) | **P3 draft v0** — contextual Composition band visibility (lifecycle + population state); no implementation |
| [p3-composition-band-recette-u1-s1-s3.md](./p3-composition-band-recette-u1-s1-s3.md) | **P3 recette** — manual U1 / dual projection worksheet (S1–S3); gate before matrix validation |
| [delta-03-hub-p1-fiche.md](./delta-03-hub-p1-fiche.md) | **Δ-03** closed — hub P1 draw rules Draft-only in Ready (C-19 / B-Q1b); manual recette + tests |
| [delta-o4-completed-fiche.md](./delta-o4-completed-fiche.md) | **O-04** validé — **Δ-O4-2** `ec45ed60` + **Δ-O4-1** `68d0f50a` ; recette comparative Championnat OK |
| [c-18-rename-stage-completed-audit.md](./c-18-rename-stage-completed-audit.md) | **C-18 Closed** — `RenameStage` refusé en `Completed` (`59d20b0a`, PR #54) |
| [c-20-standing-rules-ready-audit.md](./c-20-standing-rules-ready-audit.md) | **C-20 Closed** — `ReplaceStandingRules` refusé en `Ready` (`Stage.StandingRulesLocked`) |
| [wave-1-capacity-audit-ready-d4.md](./wave-1-capacity-audit-ready-d4.md) | **Wave 1** — audit capacité D4 / `Ready` (lecture seule ; zéro écart métier identifié post-C-20) |
| [wave-2-g-b-3-g-ops3-closure.md](./wave-2-g-b-3-g-ops3-closure.md) | **Wave 2** — preuves G-B-3 ; **G-OPS3** reste **Partial** |
| [recette-gel-a-closure.md](./recette-gel-a-closure.md) | **Recette gel A** — clôture 2026-10-16 (aucune anomalie) |
| [wave-b-programme-after-gel-a.md](./wave-b-programme-after-gel-a.md) | **Programme B** — ordre slices post-gel A |
| [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md) | **Arbitrage DM** — §11C.4 (pas de ratification bloc C-ENF) |
| [slice-2-g-b-1-enf.md](./slice-2-g-b-1-enf.md) | **SLICE-2** — G-B-1-ENF **Closed** (`c1a12ec4`) · §12 **G-B-1 Deferred** (SPA reserve) |
| [slice-3-g-ops3-qual-prog-enf.md](./slice-3-g-ops3-qual-prog-enf.md) | **SLICE-3** — G-OPS3 qual/prog **Closed** (`472546bb`) · §12 **G-OPS3 Partial** (proof reserves) |
| [g-ops3-progression-impact-arbitration.md](./g-ops3-progression-impact-arbitration.md) | **G-OPS3** — arbitrage **validé** · **SLICE-4 Closed** (progression impact) |
| [g-ops3-a3-r1-technical-note.md](./g-ops3-a3-r1-technical-note.md) | **A-3-R1 / TECH-1** — WC-1 contrat + confirmations DM §7.1 |
| [g-ops3-wc2-contract.md](./g-ops3-wc2-contract.md) | **WC-2** — production aval (2c), read-only P4 |
| [g-ops3-wc2-production-link-inventory.md](./g-ops3-wc2-production-link-inventory.md) | **P3** — inventaire lien chemin ↔ production |
| [slice-4-g-ops3-prog-impact-enf.md](./slice-4-g-ops3-prog-impact-enf.md) | **SLICE-4** — G-OPS3 progression impact **Closed** (P0–P6, DM 2026-10-16) |
| [slice-c-enf-detect.md](./slice-c-enf-detect.md) · [apply](./slice-c-enf-apply.md) · [mat-start](./slice-c-enf-mat-start.md) | **C-ENF** — three slices **Closed** · **G-C-INV** §12 **Done** |
| **Notion — Matrice UI Structure (présentation par statut)** | [Gel produit 2026-10-15](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4) — **Δ-O3-1/2** · **Δ-drawer-1** · **Δ-03** · **Δ-O4-2** · **Δ-O4-1** · **C-18** · **C-20** closed. Post-gel capacity backlog: CTRL-O3-MAT, D1 awards, ENF slices sur go. |
| [structure-diagnostic-presentation-contract.md](./structure-diagnostic-presentation-contract.md) | **Signed** — UI severity/tone/copy for structure diagnostics (presentation only) |
| [places-operational-strip-occupancy-arbitration-2026-10-16.md](./places-operational-strip-occupancy-arbitration-2026-10-16.md) | **Arbitration (validated)** — Places strip vs `SlotFeedMissing` vs badges; `createGate.ok` (§9 shipped); **§10 (2026-10-18)** 0/N lançable → **brand** + `RunDraw` copy — shipped |
| [composition-teams-resolution-vs-eligibility-arbitration-2026-10-18.md](./composition-teams-resolution-vs-eligibility-arbitration-2026-10-18.md) | **Signed principle** — distinct labels resolution vs `Active` eligibility; UI shape OPEN |
| [start-readiness-production-gap-arbitration-2026-10-18.md](./start-readiness-production-gap-arbitration-2026-10-18.md) | **Lot C shipped** — distinct Start codes (`PhaseRosterIncomplete`, `GroupsNotReady`) |
| [start-readiness-attention-recette-2026-10-18.md](./start-readiness-attention-recette-2026-10-18.md) | **Lot C** recette scenarios (C-A…D) |
| [regulation-standing-running-audit-recette-2026-10-18.md](./regulation-standing-running-audit-recette-2026-10-18.md) | **Lot D shipped** — Règlement standing vs phase (UI audit, no gap) |
| [inbound-qual-resolution-recette-2026-10-18.md](./inbound-qual-resolution-recette-2026-10-18.md) | **Lot E shipped** — inbound qual seats after `ApplyQualification` (population + ForForm) |
| [composition-teams-withdrawal-presentation-recette-2026-10-18.md](./composition-teams-withdrawal-presentation-recette-2026-10-18.md) | **Lot B (presentation)** — icône retrait — **shipped** |
| [structure-phase-hero-stats-matrix.md](./structure-phase-hero-stats-matrix.md) | Hero stats product matrix (A–F), backend-first architecture, provenance audit; Championship + SPA baseline shipped |
| [structure-phase-hero-post-arbitrations-2026-10-18.md](./structure-phase-hero-post-arbitrations-2026-10-18.md) | **Shipped** — Swiss Completed, B2 F2, O3 A/B |
| [structure-place-occupancy-parity-vectors.md](./structure-place-occupancy-parity-vectors.md) | O3-A FE ↔ Application occupancy parity vectors |
| [structure-phase-hero-configured-matchdays-application-ticket.md](./structure-phase-hero-configured-matchdays-application-ticket.md) | **Application ticket (ready)** — `ConfiguredMatchdayCount` on hub stage summary + tests; SPA/Swiss out of scope |
| [composition-tile-lifecycle-presentation-2026-10-16.md](./composition-tile-lifecycle-presentation-2026-10-16.md) | **Signed (presentation)** — Composition tile zone density by lifecycle; compact Places/Tirage summary; recette R-CT-1…4 |
| [structure-diagnostic-presentation-technical-audit.md](./structure-diagnostic-presentation-technical-audit.md) | Pre-implementation audit — codes, gaps, parity plan |
| [structure-diagnostic-presentation-impact-rules.md](./structure-diagnostic-presentation-impact-rules.md) | Impact ², `R-Pres-*`, mutation inventory — **steps 3–5 implemented** (policy, surfaces, i18n short + mutation copy) |
| [notion-decision-draft.md](./notion-decision-draft.md) | Short **Décision** text for Notion (copy/paste); links to Git annex |
| [implementation-plan-placeholder.md](./implementation-plan-placeholder.md) | Pre-§11B placeholder (historical) |
| [section-11b-signoff-preparation.md](./section-11b-signoff-preparation.md) | **§11B** maintainer pack (signed 2026-10-15) |
| [section-11c-addendum-clarification-arbitration.md](./section-11c-addendum-clarification-arbitration.md) | **§11C signed (Git)** 2026-10-15 — workbook + matrix (état au sign-off ; slices actuelles → [programme-b](./programme-b-arbitration-2026-10-16.md)) |
| [section-11c-e5-matrix-diff-proposal.md](./section-11c-e5-matrix-diff-proposal.md) | **E5** — applied matrix diff record (§9–§11) |
| [notion-decision-11c-draft.md](./notion-decision-11c-draft.md) | Notion copy block §11C |
| [implementation-plan-post-11c.md](./implementation-plan-post-11c.md) | **Historical** (2026-10-15) — catalogue pré–Programme B ; SoT opérationnelle : [programme-b](./programme-b-arbitration-2026-10-16.md) + matrice §11C.4 |
| [slice-1-g-b-3-enf.md](./slice-1-g-b-3-enf.md) | **Closed** `SLICE-1-G-B-3-ENF` — `G-B-3-ENF` (§12 **Done**) |
| [implementation-plan-post-11b.md](./implementation-plan-post-11b.md) | Scope **after** §11B signature |
| [notion-decision-11b-draft.md](./notion-decision-11b-draft.md) | Notion copy block §11B |
| [g-mat1-confrontation-undecidable-at-materialize.md](./g-mat1-confrontation-undecidable-at-materialize.md) | **G-MAT1** — Done (Cup gate) |
| [g-mat2-authored-award-unreachable-at-materialize.md](./g-mat2-authored-award-unreachable-at-materialize.md) | **G-MAT2** — Done (Cup gate, option B) |
| [g-ops-ready-operational-mutations-and-d1.md](./g-ops-ready-operational-mutations-and-d1.md) | **G-OPS1 + G-OPS2** — **Done** (qual/prog/affectation; R-D1a/b) |
| [g-mat3-non-root-unfed-at-materialize.md](./g-mat3-non-root-unfed-at-materialize.md) | **G-MAT3** — spec draft (R-Mat-M2) |
| [g-mat3-arbitration-session.md](./g-mat3-arbitration-session.md) | **G-MAT3** — décision signée (gate deferred, M2b) |
| [g-mat4-source-population-shortfall-at-materialize.md](./g-mat4-source-population-shortfall-at-materialize.md) | **G-MAT4** — spec draft |
| [g-mat4-arbitration-session.md](./g-mat4-arbitration-session.md) | **G-MAT4** — décision signée (gate deferred, M4b) |
| [g-ops3-draw-refuse-on-composition-change.md](./g-ops3-draw-refuse-on-composition-change.md) | **G-OPS3** — spec draft |
| [g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md) | **G-OPS3** — décision signée (gate deferred, R-Draw1 neg) |
| [g-ops3-progression-impact-arbitration.md](./g-ops3-progression-impact-arbitration.md) | **G-OPS3** — arbitrage draft : progression par impact (W-OPS3-1..3) vs SLICE-3 Applied |

## Related Git docs

- [Structure phase fiche truth sources](../structure-phase-refonte/truth-sources.md)
- [Actions & permissions inventory](../structure-phase-refonte/actions-permissions-inventory.md)

## Status

**Signed policy (2026-10-15):** §11A in [phase-mutability-matrix.md](./phase-mutability-matrix.md); arbitrations #1–#3 closed §5.2–§8; G-UI1/G-UI2 shipped.  
**Notion:** [Décision §11A](https://app.notion.com/p/3fa75fb92d108172b028d6858d0ea45b?pvs=204) (Acceptée 2026-10-15).  
**G-MAT3 (2026-10-15):** gate deferred — [decision](./g-mat3-arbitration-session.md), **R-Mat-M2b** test.  
**G-MAT4 (2026-10-15):** gate deferred — [decision](./g-mat4-arbitration-session.md), **R-Mat-M4b** + analyzer + overlap tests.  
**G-OPS3 (2026-10-15):** gate deferred — [decision](./g-ops3-arbitration-session.md).  
**§11B (2026-10-15):** **signed** — [signoff pack](./section-11b-signoff-preparation.md) · [Notion copy](./notion-decision-11b-draft.md) · matrix §11B ticked. Post-sign work: [implementation-plan-post-11b.md](./implementation-plan-post-11b.md).  
**§11C (2026-10-15):** [addendum](./section-11c-addendum-clarification-arbitration.md) + [matrix §11](./phase-mutability-matrix.md#11-sign-off-scope-three-layers) — full Git sign. **Programme B (2026-10-16):** slices arbitrées **Closed** (SLICE-1..3 + C-ENF) · **G-C-INV** **Done** · §12 **G-B-1 Deferred** · **G-OPS3 Partial** — [wave-b](./wave-b-programme-after-gel-a.md).
