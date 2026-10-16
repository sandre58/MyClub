# R7 — Schematic case primary label (Qual/Prog vs resolved entry)

**Status:** **Accepted** (2026-10-17).  
**Scope:** SPA presentation only — no hub, occupancy, or materialize changes.

## Decision

When a participant is **resolved** (`entry` / `assignment`), the **case primary** is always the **participant identity** (name + crest), for **all feed kinds** including Qualification and Progression.

**Provenance (Qual/Prog path):** **tooltip only** in the first implementation — no second line on the case (avoids density with long names and multi-round brackets).

| Situation | Case primary | Provenance |
|-----------|--------------|------------|
| Qual / Prog, unresolved | Path label | On case (primary) |
| Qual / Prog, resolved | Name + crest | Tooltip (path + from-phase when present) |
| Draw, resolved | Name + crest | Draw tooltip (unchanged) |
| Direct, resolved | Name + crest | Unchanged |

**Supersedes:** U4 B1 (Qual/Prog path always on case primary).

## Implementation

- [`phaseSchematic.tsx`](../../../src/PlayUp/web/src/pages/structure/phaseSchematic.tsx) — `structureCaseLabels`, `constructionPathLabel` for tooltips
- [`phaseSchematicTooltip.ts`](../../../src/PlayUp/web/src/pages/structure/phaseSchematicTooltip.ts) — structural feed + resolved: path as tip subject, not `resolvedTeam` row
- Tests: [`phaseSchematic.test.tsx`](../../../src/PlayUp/web/src/pages/structure/phaseSchematic.test.tsx)

## Recette

- `cup-qf-sf` → Demi-finale: case primary = team; hover tip shows progression path.

## References

- [cup-materialize-recette-protocol.md](./cup-materialize-recette-protocol.md)
