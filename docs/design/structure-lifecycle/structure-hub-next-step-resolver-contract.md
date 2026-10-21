# Structure hub next step — resolver contract (step 2)

Status: **shipped** — `StructureViewDto.HubNextStep` on `GET /structure`; contract tests green; SPA consumes DTO only (see truth matrix §8).

## Input snapshot

Resolver consumes the same snapshot as `StructureViewAssembler.Assemble` output **plus** competition regulation facts already on `StructureViewDto`:

- `stages[]` in competition order (id, name, status, lifecycle, materialize, draw badge, composition fields, …)
- `readiness` (primary-stage schedule/Swiss hints — **competition scope only**)
- `format.primaryStageId` / `primaryStageName`
- competition `status` (open vs Completed/Archived)
- **Not** in resolver: SPA `/attention` (PageHead gates positives after attention; contract tests document “resolver output” only)

## Output (logical)

```text
HubNextStepDecision?
  = null  → no info pill from hub step (calm or ambiguous)
  | { Code, StageId, StageName }  → info pill
```

`success` (calm) is **not** produced by the resolver — PageHead derives it when `null` + calm rules (§5 in truth matrix).

## Priority (signed principles — not numeric P1–P8)

Per stage, evaluate **activatable** steps. Pick **at most one** hub winner:

1. If multiple stages yield candidates **without** dominance → `null`.
2. Dominance uses **signed principles** (truth matrix §4), not field order on DTO.

**Per-stage tier (first match wins on that stage):**

| Tier | Codes | Activatable when |
| ---- | ----- | ---------------- |
| T1 | `ResumeStage` | `lifecycle.revertTransition`: `ResumeStage`, `enabled` |
| T2 | `PrepareStage` | `lifecycle.transition`: `PrepareStage`, `enabled` |
| T3 | `ApplyDraw` | Apply authorization (Overview `ActionApplyDraw` parity) |
| T4 | `RunDraw` | `DrawCreateLaunchAllowed` (see draw launch gate doc) |
| T5 | `MaterializeMatches`, `MaterializeFromOccupiedSlots` | `materialize.commandEnabled` |
| T6 | `StartStage` | `lifecycle.transition`: `StartStage`, `enabled`, and **no** higher tier on same stage |
| T7 | `CompleteStage` | `lifecycle.transition`: `CompleteStage`, `enabled`, and **no** higher tier on same stage |

**Out of scope for `hubNextStep`:** `BuildSchedule`, `RunSwissRounds` (Structure PageHead is structure-only; hints may remain on `readiness` for other surfaces).

**Cross-stage (Q1 signed):** collect the **best (lowest) tier** per stage; global winner = the **single** stage at the minimum tier. If **two or more** stages share that minimum tier → `null`. **Stage order does not break ties.**

**Disabled transitions never emit** (S12).

## Scenario matrix S1–S14

Tests live in `StructureHubNextStepResolverContractTests.cs`. Expected **resolver output** (PageHead layers error/warning/attention applied separately in SPA).

| ID | Scenario | Expected resolver | Notes |
| -- | -------- | ----------------- | ----- |
| S1 | Single Draft, Prepare enabled | `PrepareStage` · phase | |
| S2 | Two Draft, both Prepare enabled | `null` | Ambiguous |
| S3 | Suspended, Resume enabled | `ResumeStage` · phase | |
| S4 | ToLaunch + launch gate OK | `RunDraw` · phase | Badge alone insufficient |
| S5 | ToApply + apply authorized | `ApplyDraw` · phase | Badge without auth → `null` |
| S6 | `materialize.commandEnabled` | `Materialize*` · phase | `commandEnabled` false → never |
| S7 | Running, Complete enabled, no construction left on stage | `CompleteStage` · phase | If mat/draw/apply still required → those codes |
| S8 | Competition Running + lone Draft frozen | `null` or N/A | PageHead **warning/error**; resolver may still emit Prepare on **other** stages — PageHead negative/warning wins |
| S9 | All ≥ Ready, not all Completed, no activatable step | `null` | Calm → PageHead `success` |
| S10 | (attention) | `null` | SPA gate — not resolver input |
| S11 | Competition Completed/Archived | `null` | No positive pill |
| S12 | Prepare projected but `enabled: false` | `null` | No action pill |
| S13 | Schedule/Swiss hint only (`readyForNextSlice` + hint) | `null` | Never emitted on Structure hub |
| S14 | Structure problems + Prepare enabled | `PrepareStage` at resolver | PageHead shows **error**; positive suppressed in SPA |

## Conflict scenarios (priority regression)

| ID | Setup | Expected resolver | Principle |
| -- | ----- | ----------------- | --------- |
| C1 | Ready: materialize + Start both enabled | `Materialize*` on that stage | Materialize before Start |
| C2 | Draft: Prepare enabled + ToLaunch (gate OK) | `PrepareStage` | Prepare before materialize/draw |
| C3 | Same stage: ToApply + ToLaunch (edge) | `ApplyDraw` | Apply before Run |
| C4 | Stage A Apply, Stage B Prepare (order A before B) | `ApplyDraw` · A | Cross-stage: earlier stage wins if same tier; Apply is T3 vs Prepare T2 → **Prepare on B first if B before A** — **see open Q1** |
| C5 | Two stages both `ApplyDraw` authorized | `null` | Ambiguous |
| C6 | Complete enabled + materialize still required on same Ready phase | `Materialize*` | Complete not default |
| C7 | Start enabled + RunDraw gate OK (draw required format) | `RunDraw` | Draw before Start when both activatable |
| C8 | Badge ToLaunch, `BlockingPlaceCount > 0` | `null` for RunDraw; may be Prepare/Materialize/other | Gate fail-closed |

### Q1 (signed)

Minimum tier wins globally; equal minimum tier across multiple stages → `null`. Covered by `S2`, `Q1_lower_tier_wins_across_stages_without_using_stage_order`.

## ApplyDraw authorization (shared predicate)

Extract (step 3) from same logic as `OverviewAssembler` draw actions:

- Non-cancelled draw
- `Status == Published`
- `Resolution.State == Resolved`
- Kind Slot or Group
- `!DrawAppliedState.IsApplied(draw, stage)`

Badge `ToApply` is necessary but tests must assert **resolved** published draw (S5 negative case).

## Test execution model

| Category | Command | Expected |
| -------- | ------- | -------- |
| Precursor / gate | `dotnet test --filter FullyQualifiedName~StructureDrawCreateLaunchGateContractTests` | **Pass** (step 2) |
| Resolver contract | `dotnet test --filter FullyQualifiedName~StructureHubNextStepResolverContractTests` | **Green** |
| Gate precursors | `StructureDrawCreateLaunchGateContractTests` | **Green** |

## Review checklist (product sign-off before DTO)

- [ ] S1–S14 expectations match truth matrix
- [ ] C1–C8 encode signed priority principles without P1–P8 table
- [ ] RunDraw never emitted from badge alone (C8 + gate doc)
- [ ] `success` never returned from resolver
- [ ] Open Q1 cross-stage rule accepted

## Related

- `structure-pagehead-truth-matrix.md`
- `structure-draw-create-launch-gate.md`
