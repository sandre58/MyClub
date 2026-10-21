# G-MAT4 — `SourcePopulationShortfall` at materialize (spec draft)

| Field | Value |
|-------|--------|
| **Backlog ID** | G-MAT4 |
| **Reference cases** | **R-Mat-M4a** (refuse when production needs missing extraction), **R-Mat-M4b** (A5 — foreign diagnostic) |
| **Contract status** | **Spec draft (§12)** — gate **deferred** 2026-10-15 ([decision](./g-mat4-arbitration-session.md)); M4b signed; not §11B; **not** Ready |
| **Scope** | G-MAT4 only — separate from G-MAT3 (topology `NonRootUnfed`) |

**Product policy (§5.2 / §11A, reserve A5):** `SourcePopulationShortfall` is a **capacity signal** in the hub (warning). **Materialize** must block only when the **command’s production path** requires qualification **extraction volume** from a source stage that cannot be satisfied — not when the diagnostic appears on an unrelated stage.

---

## Diagnostic today (read model)

[`AnalyzeCapacity`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs) on source stage `U`:

| Step | Condition |
|------|-----------|
| Outbound | `StructurePopulationVolume.OutboundQualificationVolume(U) > 0` (sum of qual path volumes) |
| Not `OverExtraction` | `outbound <= ResolvePlacesCapacity(U)` when capacity is known |
| Shortfall | `outbound > available` where `available = max(CompositionEntries, AffectationAuthoring)` (or `activeEntryCount` when both zero) capped by capacity |

Distinct codes:

| Code | Meaning |
|------|---------|
| `OverExtraction` | Outbound qual volume **exceeds structural places** on `U` |
| `SourcePopulationShortfall` | Outbound **fits places** but exceeds **available population** on `U` |
| `PopulationShortfall` | **Destination** expected occupancy &lt; places (different axis) |

Hub severity: **warning** ([`structureDiagnosticSeverity.ts`](../../../src/PlayUp/web/src/pages/structure/structureDiagnosticSeverity.ts)). **Not** a Prepare gate today (`IsPrepareGateIssue` → false).

**Analyzer contract test:** [`AnalyzeCapacity_emits_SourcePopulationShortfall_when_outbound_exceeds_available`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/CompetitionFlowAnalyzerTests.cs).

---

## Materialize today (gap vs policy)

| Command | Population / extraction check |
|---------|------------------------------|
| [`MaterializeMatches`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeMatches/MaterializeMatches.cs) (Ch/Groups) | **Target stage** only: `ChampionshipSwissPhasePlaces.IsRosterComplete` + composition roster — **`MaterializationFailure`** if incomplete |
| Cup | [`MaterializeCupFromOccupiedSlots`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeCupFromOccupiedSlots/MaterializeCupFromOccupiedSlots.cs) — slot occupancy; **no** outbound qual volume |
| Swiss | `GenerateNextRound` — out of G-MAT4 scope |

**Δ (matrix §5.2):** Analyzer reasons about **source extraction**; Ch materialize reasons about **target roster completeness**. Same product intent (enough teams to play) but **different code paths** — unify or document explicitly before a gate.

---

## Three states (A5 — same vocabulary as G-MAT3)

| State | Question | Materialize veto? |
|-------|----------|-------------------|
| Diagnostic present | Hub shows `SourcePopulationShortfall` on stage `U`? | **No** alone |
| Production needs extraction | The batch on target `S` **depends** on qual extraction from `U` that materialize will consume or assume? | N/A alone |
| Extraction impossible | Required volume from `U` cannot be met at command time? | **Yes** if production need proven |

---

## Question to decide (candidate contract)

**When must source population shortfall block match production?**

**Proposed direction (Championship first):**

Refuse `MaterializeMatches` on stage `S` when:

1. The command’s determined pair set (or roster expansion) **requires** entries that would be supplied only via qualification **from** source stage `U`; and  
2. `outboundQual(U) > availablePopulation(U)` under the same rules as `AnalyzeCapacity` (not merely `OverExtraction`); and  
3. The shortfall is **not** already expressed by target `IsRosterComplete` / `MaterializationFailure` with equivalent organizer meaning.

**Cup:** defer — production is slot-driven; link to G-MAT3 / slot feed decisions if ever needed.

---

## Open arbitrations (decision maker)

| # | Topic | Options |
|---|--------|---------|
| **M4-1** | Wire code | Dedicated `Application.SourcePopulationShortfallAtMaterialize` (409) vs extend `MaterializationFailure` with reason |
| **M4-2** | Format v1 | Championship only vs Groups vs both Ch/Groups in one adapter |
| **M4-3** | Overlap `IsRosterComplete` | Gate only when **strictly stronger** than target roster check; or document dual refusal order |
| **M4-4** | `OverExtraction` | Never map to G-MAT4 — stays separate diagnostic / future gate |

---

## R-Mat-M4b — signed A5 non-regression (implemented)

**Given:** stage `T` emits `SourcePopulationShortfall`; stage `S` (Championship) has complete roster and eligible materialize batch; no production dependency on `T`’s promised extraction.

**When:** `MaterializeMatches` on `S`.

**Then:** success on `S`; diagnostic on `T` does not veto.

**Test:** [`MaterializeMatchesRMatM4bTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeMatchesRMatM4bTests.cs).

---

## R-Mat-M4a — positive refuse (**deferred** — not signed)

**Decision 2026-10-15:** not attainable without duplicating [`MaterializationFailure`](../../../src/PlayUp/MyClub.PlayUp.Application/Common/ApplicationErrorCodes.cs) on target roster (`IsRosterComplete`). Overlap documented: [`Execute_on_qual_fed_championship_with_incomplete_roster_uses_MaterializationFailure`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeMatchesRMatM4bTests.cs).

No G-MAT4 materialize gate until **reopen conditions** in [arbitration decision](./g-mat4-arbitration-session.md).

---

## Decision (2026-10-15)

Signed: **M4b** A5 non-regression; gate deferred; M4-1 not retained; M4-2..4 as in arbitration session. Full text: [g-mat4-arbitration-session.md](./g-mat4-arbitration-session.md).

---

## Conditions de réouverture (gate implementation)

1. **M4a** attainable with refusal **distinct** from `MaterializationFailure`; or  
2. Explicit product choice to **align** analyzer extraction vs roster materialize (Δ closure).

---

## Related

- [phase-mutability-matrix.md](./phase-mutability-matrix.md) §5.2 `SourcePopulationShortfall`, §12 G-MAT4  
- [g-mat3-arbitration-session.md](./g-mat3-arbitration-session.md) — deferred gate pattern + M2b  
- [g-mat4-arbitration-session.md](./g-mat4-arbitration-session.md) — ordre du jour arbitrage M4-1..4
