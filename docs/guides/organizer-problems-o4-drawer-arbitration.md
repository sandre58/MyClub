# Organizer problems — O4 drawer scope (G3 atelier)

**Status:** **Pending product arbitration** — no Application or drawer payload change until **DECIDED** and written contract.

**Prerequisite question:** What user problem does each candidate category solve that Structure, Overview, or lifecycle surfaces do not already address?

## Four decisions (distinct)

| # | Topic | Output |
| :- | :---- | :----- |
| 1 | **Catalogue** | Which T2/T3/T4 facts become `GET /attention` items |
| 2 | **Exclusions** | Lifecycle phase, Running/Suspended rules, Overview dedup |
| 3 | **Priority / severity** | Meaning of `Severity` vs `semanticLevel` vs SPA tone — no implicit merge |
| 4 | **Line unit / dedup** | One row = one business fact vs one action vs one root cause |

**Blocker:** Decide (4) before designing any new Application collector.

## Current policy (unchanged until O4)

- T5 only: `InsufficientParticipants`, `DrawNoSolution`, `Qualification*`, `Progression*`.
- Construction denylist SPA: [`attentionPartition.ts`](../../src/PlayUp/web/src/shell/attentionPartition.ts).

## If DECIDED — implementation checklist

- Notion Décision row + update [pilot aliases](./organizer-problems-pilot-aliases.md)
- `NeedsAttentionAssembler` extension (or dedicated projector) with SoT comments
- Host `/attention` contract tests
- SPA partition/denylist mirror — not guardrail-only

## Default if no decision

**Report** O4; drawer stays T5; construction remains Structure/Overview SoT.
