# Phase 3 — Lot C3 plan (AUD-0004)

**Status:** Implemented on `feature/new_version` (split tests + protocol docs; no domain change).

**Authorization:** User go 2026-10-21 — aligns with phase 2 **option 2 (scinder)** ([Décision](https://www.notion.com/p/40075fb92d108156a85df51720fefbbe), [Arbitrage 3](https://www.notion.com/p/40075fb92d1081cdb0f8d655d2e93c68)).

**Finding:** AUD-0004 — `CupC2ImpasseRecetteProtocolTests` mixed **cas assign** with checklist A3/A5; assertion drift vs `StageStartReadinessGuard` order.

---

## 1. Scope (in)

| Item | Deliverable |
| --- | --- |
| **Split tests** | `C2_A3_*` @ 1/2 without manual assign; `C2_A5_*` after 2/2 eligible materialized — both expect `StartBlockedIncompleteEntryRound` |
| **Exclude cas assign** | No `AssignEntryToSlot` before first Start in protocol tests; documented as future qualification |
| **Docs** | `cup-c2-spa-recette-checklist.md`, `cup-materialize-recette-protocol.md` aligned with backend protocol (SPA vs guard drift noted at A3) |

**No change** to `StageStartReadinessGuard`, HTTP contracts, or Cup domain rules.

---

## 2. Explicit exclusions

| Item | Reason |
| --- | --- |
| **Cas assign** (manual slot before Start @ 1/2) | Separate product contract per arbitrage — no test until qualified |
| **Aligning domain to legacy `StartBlockedUnmaterialized` at A3** | Rejected in option 2 when it conflicts with guard 1a on RECETTE C2 seed |
| **SPA-only recette re-sign-off** | Checklist track B / Q3 unchanged |
| **C8, C7, C1** | Other lots |

---

## 3. Validation

```bash
dotnet test tests/MyClub.PlayUp.Development.Tests --filter FullyQualifiedName~CupC2Impasse -c Release
```

Expect **2** passing tests.

---

## 4. Acceptance criteria

- [x] `CupC2Impasse` integration tests green on branch.
- [x] Protocol docs describe A3/A5 without obsolete single-chain / `StartBlockedUnmaterialized` @ 1/2.
- [x] Cas assign explicitly deferred.
- [x] Registre Notion **AUD-0004** → vérifié / clôture lot (post-merge @ `bbb76630`).

---

## 5. Commits

- `15772006` — initial split (remove cas assign from chain).
- *(this change)* — named tests A3/A5 + doc sync + `phase-3-lot-c3-plan.md`.
