# Organizer problems — P2B Structure semantics audit

**Date:** 2026-10-12. **Scope:** `structureIssueSemantics[]` on hub — when SPA may use `findStructureIssueProblem`.

## Observed model

- Hub lists **canon** codes in `structureIssues[]`.
- Application emits **optional** `structureIssueSemantics` for **pilot** codes only (`OrganizerProblemSemanticsResolver`).
- Lifecycle may show **aggregate** disabled codes (O1) — not replaced by hub issue strings.

## Safe SPA use

| Use | Rule |
| :-- | :--- |
| Dialog echoes (qual/progression dangling targets) | Pass `problem` into `structureIssuePresentation` when semantics entry matches `issueCode` |
| Fiche tiles | Keep `structureDiagnosticPresentation` — do not replace with universal canon helper |
| Drawer | **No** structure issues in drawer (Step 2) |

## Multi-cause / aggregate

- One hub may list multiple issues; semantics array may be partial (pilot only).
- **Do not** infer canon from aggregate readiness blockers on Structure page.

## Integration done (targeted)

- [`StructureQualificationDialog.tsx`](../../src/PlayUp/web/src/pages/structure/StructureQualificationDialog.tsx)
- [`StructureProgressionDialog.tsx`](../../src/PlayUp/web/src/pages/structure/StructureProgressionDialog.tsx)

Further fiche integration requires per-tile audit.
