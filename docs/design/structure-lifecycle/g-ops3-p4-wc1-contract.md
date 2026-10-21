# SLICE-4 — Contrat P-4 / WC-1 (P0)

| Field | Value |
|-------|--------|
| **Parent** | [slice-4 §7](./slice-4-g-ops3-prog-impact-enf.md) · [A-3-R1 note](./g-ops3-a3-r1-technical-note.md) §7.1 |
| **Scope P0–P2** | Diff + witness read-only — **no** gate wiring, **no** SLICE-3 change |

## P-4 — Invariants

| Invariant | Rule |
|-----------|------|
| **Model** | Diff on `ProgressionRules.Paths` only (not intents). |
| **Source key** | `(SourcePairKey, Outcome)` — at most one path per key in a rules instance. |
| **Removed** | Key in `before`, absent in `after`. |
| **Replaced** | Key in both, `!beforePath.Equals(afterPath)` (destination change). |
| **Added** | Key in `after`, absent in `before`. |
| **Unchanged** | Key in both, equal `ProgressionPath`. |
| **`Touched`** | `Removed ∪ ReplacedBefore` — **before** path snapshots only (gate WC-1 input). |
| **Clear rules** | `afterRules == null` → every `before` path is **Removed**. |
| **No-op wire** | `ProgressionRules.Equals(before, after)` — `Touched` empty. |
| **Family A** | Mutations that are **Added** only → `Touched` empty (no WC-1 block). |

**Code:** `ProgressionRulesPathDiff.Compare` (Domain).

## WC-1 — Invariants (per `p` in `Touched`)

| Step | Outcome |
|------|---------|
| Rejeu `expectedEntry` | Fixture decided → `ProgressionApplier` ; indeterminate → **ConsumedAmbiguous** |
| `satBefore` | `DestinationSatisfaction.EvaluateProgression(dest, sourceId, p, expectedEntry)` |
| Not satisfied | **NotConsumed** (no block for `p`) |
| `p'` in `after` | Same `(SourcePairKey, Outcome)` |
| `p'` missing or `≠ p` | **ConsumedLoss** |
| `p'` equal to `p` | **NotConsumed** |

**Non-goals:** aggregate destination population; WC-2 / 2c production link (P3–P4).

**Code:** `ProgressionPathConsumptionWitness` (Application) — not wired to `ReplaceStageProgressionRules` until P5+.

## Errors (future gate, P5+)

| Verdict | Application code (planned) |
|---------|----------------------------|
| ConsumedLoss | `Application.ProgressionFeedConsumed` |
| ConsumedAmbiguous | Same (prudent refuse) |
