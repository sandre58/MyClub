# Phase 6 — T1 — Application test failure (resolved 2026-10-22)

## Symptom

`StructurePresentedAttentionCollectorTests.Collect_emits_incomplete_entry_round_when_primary_start_blocker_is_unmaterialized` failed on `main` @ `4b6b3fe7` / post phase 6 wave 1.

**Assertion:** `/attention` collector should emit both `IncompleteEntryRound` and `UnmaterializedConfrontations` (blocking).

**Actual:** `IncompleteEntryRound` present; `UnmaterializedConfrontations` absent (also `SlotFeedMissing`, `PopulationShortfall` from readiness blockers).

## Investigation

| Check | Result |
| --- | --- |
| Lifecycle transition | `disabledReasonCode = IncompleteEntryRound` — matches test preconditions |
| `presentedIssueCodes` on hub | Same as collector input — **no** `UnmaterializedConfrontations` |
| Product change | Commit `6aa23992` — **D3-B**: suppress duplicate `UnmaterializedConfrontations` when Start is already explained by `IncompleteEntryRound` |
| Parallel test (read-model) | `StructurePresentedStageDiagnosticsTests.Assemble_cup_partial_round_materialization_surfaces_incomplete_entry_round_without_unmaterialized_duplicate` documents the contract |
| Recette | `CupC2ImpasseRecetteProtocolTests` updated in same D3-B commit |

**Conclusion:** **Test was stale** — not a product regression. The collector correctly mirrors `StructurePresentedStageDiagnostics.CollectPresentedIssueCodes`; D3-B intentionally removed parallel Start attention on materialization gap when entry round is the primary Start blocker.

## Action taken

- Renamed test to `Collect_cup_incomplete_entry_round_does_not_duplicate_unmaterialized_attention`.
- Assert `IncompleteEntryRound` blocking item **and** `NotContain` `UnmaterializedConfrontations`.

No change to `StructurePresentedAttentionCollector` or diagnostics logic.

## Re-verify

```bash
dotnet test tests/MyClub.PlayUp.Application.Tests -c Release --filter "FullyQualifiedName~Collect_cup_incomplete_entry_round_does_not_duplicate"
```
