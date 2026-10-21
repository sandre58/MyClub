# Cup lifecycle — Start, mutability, recovery (product arbitration)

**Status:** **✅ Acceptée** (2026-10-17, décideur).  
**Product SoT:** [Notion — [Play'Up] Cup — Start, mutabilité (D2) et récupération](https://app.notion.com/p/3fc75fb92d1081d587e9ed0b5b59e4a2)  
**Notion link:** https://app.notion.com/p/3fc75fb92d1081d587e9ed0b5b59e4a2

**Accepted verdict (mono-tour canonical Cup only):**

| Topic | Decision |
|-------|----------|
| **R0 complete** | Every canonical entry-round pair P* has two distinct `EntryId` (family A). Family B empty (no Cup BYE in scope). Family C for progression-only downstream, not entry-round gaps on upstream mono-tour. |
| **Q1 Start** | **1a** — refuse `StartStage` while R0 is incomplete per above. |
| **Q2 Mutability** | **2a** — confirm D2 (freeze when any match attached, including Ready). |
| **Q3 Recovery** | Nominal: do not materialize before R0 ready. **Revert to Ready** supported (does not alone restore placement). **Dematerialize in Ready** supported (C2 backend + SPA validated). Revert to Draft not nominal recovery. |
| **UX** | Warn before materialize + signal incomplete R0 (independent of Q1). |
| **BYE** | Out of functional scope for this decision; no implicit BYE; future Cup BYE = separate arbitration. |

**Evidence (not the decision):** `CupC2ImpasseRecetteProtocolTests`, [cup-c2-spa-recette-checklist.md](./cup-c2-spa-recette-checklist.md). Demat CTA discoverability (icon-only) = UX implementation topic.

**Reference rules (unchanged unless superseded):**

- **§ H Option A** — materialize all **determined eligible** confrontations before `StartStage` ([lifecycle UI guide](../../guides/structure-phase-lifecycle-ui.md) § H; Notion § H decision).
- **D2** — operational/structural edits blocked when match production is attached ([phase-mutability-matrix.md](./phase-mutability-matrix.md) §1 D2).

**Related Git (pre-implementation behaviour):** `StageStartReadinessGuard`, `StageConfrontationMaterializationState.CountCupEligible`, `StructureOperationalExitEditGuard`, [cup-materialize-recette-protocol.md](./cup-materialize-recette-protocol.md).

---

## Implementation (after acceptance)

| Tranche | Status |
|---------|--------|
| **1 — Start guard 1a** | **Done** — `CupCanonicalEntryRoundStartGate`, `Application.StartBlockedIncompleteEntryRound`, read `IncompleteEntryRound`, SPA i18n |
| **2 — Preventive UX** | **Done** — read `canonicalEntryPairCount` / `undeterminedCanonicalEntryPairCount`, aside advisory + confirm before partial cup materialize |
| **3 — Demat discoverability** | **Done** — visible label on dematerialize CTA in aside Matchs (icon + text) |

### Reference seeds

| DevRunner row (`structure-materialize-recette`) | Role |
|-------------------------------------------------|------|
| **RECETTE C1** — Cup 8, one bracket pair placed | Before first materialize (0/1 style) |
| **RECETTE C2** — Cup 8, two pairs placed, one materialized | Impasse after 2/2 eligible materialized (2/4 bracket pairs determined) |
| **RECETTE C3** — Cup 8, fully materialized | Positive control |

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios structure-materialize-recette
```

---

## After decision (maintenance)

- Keep this file as Git pointer + accepted summary; full narrative stays on Notion.
- One-line link in [structure-phase-lifecycle-ui.md](../../guides/structure-phase-lifecycle-ui.md) when implementation lands (no parallel SoT).
