# Phase 4 — Lot D2-B — Batch fixture attachment hydration

**Hypothesis (AUD-0012):** `HydrateOrderedCollectionsAsync` issued one `FixtureAttachmentRef` query per round and per matchday. Batching all stage fixtures into a **single** attachment query preserves ordering behaviour and reduces SQL round-trips on Structure/Full loads.

**Scope:** `StageRepository.HydrateOrderedCollectionsAsync` only — **no** `Include` graph changes.

## Acceptance criteria

| Criterion | Result |
| --- | --- |
| Same scenarios (`Category=PerformanceBaseline`) | ✅ Re-run 2026-10-22 |
| SQL count ↓ on match-heavy stages | ✅ `groups:running` / `champions-league:running` list matches **20 → 18** |
| cup-qf-sf isolated Full load | Unchanged **17** (already one effective attachment round-trip on that seed) |
| No functional regression | CI `Development.Tests` + PerformanceBaseline filter |
| Behaviour | Same `HydrateAttachmentsAsync` projection per fixture |

## Before / after (local Testcontainers, 2026-10-22)

| Scenario | Metric | Before D2-B | After D2-B |
| --- | --- | ---: | ---: |
| cup-qf-sf | `GetByIdReadOnly(Full)` | 17 | 17 |
| cup-qf-sf | `GetByIdForUpdate(Full)` | 17 | 17 |
| cup-qf-sf | `GetByIdsReadOnly(Full)` N=2 | 18 | 18 |
| groups:running | `GET /stages/{id}/matches` | 20 | **18** |
| champions-league:running | `GET /stages/{id}/matches` | 20 | **18** |

Regression guards: `ReadPerformanceBaselineTests` ceilings at **18** SQL for those list endpoints.

## Registre

AUD-0012 remains **dette acceptée** — partial mitigation only. Further D2-B lots (if any) need separate hypotheses; **no** `Include` narrowing without measurement.
