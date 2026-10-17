# Cup lifecycle — Start, mutability, recovery (product arbitration)

**Status:** **Pending** product arbitration (no implementation until Notion decision is validated).  
**Product SoT:** [Notion — [Play'Up] Cup — Start, mutabilité (D2) et récupération](https://app.notion.com/p/3fc75fb92d1081d587e9ed0b5b59e4a2) (`💭 Proposée`).  
**Notion link:** https://app.notion.com/p/3fc75fb92d1081d587e9ed0b5b59e4a2

**Reference rules (remain in force until this decision overrides them):**

- **§ H Option A** — materialize all **determined eligible** confrontations before `StartStage` ([lifecycle UI guide](../../guides/structure-phase-lifecycle-ui.md) § H; Notion § H decision).
- **D2** — operational/structural edits blocked when match production is attached ([phase-mutability-matrix.md](./phase-mutability-matrix.md) §1 D2).

**Related Git (behaviour today, not product verdict):** `StageStartReadinessGuard`, `StageConfrontationMaterializationState.CountCupEligible`, `StructureOperationalExitEditGuard`, [cup-materialize-recette-protocol.md](./cup-materialize-recette-protocol.md).

---

## Arbitration sequence (validated framing)

1. **Start + R0 complete (business definition)** — BYE, non-power-of-two field sizes, **non-playable** fixtures vs pairs that must be populated before exploitation. Do **not** equate “R0 complete” with “every structural pair has two participants” without those rules.
2. **Recovery** — supported paths after premature materialize; do **not** assume D2 exceptions. **Note:** tightening Start (1a) does **not** alone fix freeze-on-first-match in **Ready**; recovery must be decided if partial materialize remains allowed.
3. **Preventive UX** — warn before **Create matches** that structure may freeze (including in Ready), **independent** of the Start verdict.
4. **Tests / recette** — promoted to demonstrated invariants only after **reference seed** is fixed and behaviour verified (scenario table = hypotheses until then).

---

## Three decision questions (no default on 1a/1b or 2a/2b)

| # | Question |
|---|----------|
| **Q1 Start** | May Cup enter **Running** with a **structurally incomplete** initial round while § H Option A and current guards pass? |
| **Q2 Mutability** | Confirm freeze when **any** match is attached, **including Ready**, or define a scoped exception (not assumed). |
| **Q3 Recovery** | Dematerialize, cancel start, revert draft — which paths are **supported** vs discouraged vs out of scope? |

Full option matrix lives in [Notion](https://app.notion.com/p/3fc75fb92d1081d587e9ed0b5b59e4a2) (section **Résolution des 4 points** — analysis, not accepted).

### Reference seeds (impasse — verify manually before tests)

| DevRunner row (`structure-materialize-recette`) | Role |
|-------------------------------------------------|------|
| **RECETTE C1** — Cup 8, one bracket pair placed | Before first materialize (0/1 style) |
| **RECETTE C2** — Cup 8, two pairs placed, one materialized | **Impasse-type** (D2 + Start after Option A) |
| **RECETTE C3** — Cup 8, fully materialized | Positive control |

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios structure-materialize-recette
```

Cup **BYE** is **not** represented in domain code today (Swiss only) — see Notion analysis; do not assume BYE in R0 definition.

---

## After decision

- Update Notion Décision status and link above.
- Short pointer only in [structure-phase-lifecycle-ui.md](../../guides/structure-phase-lifecycle-ui.md) (link to Notion + this file) — **do not** duplicate the full matrix in the guide.
- Implementation, `disabledReasonCode` registry, and automated cases **after** validation only.
