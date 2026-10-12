# Organizer problems — G0 audit report (2026-10-12)

**HEAD at audit start:** `b583c214` (O3 arbitration). **Scope:** Step 3 pilote + SPA Phase 2 working tree.

## Working tree (pre-commit)

| Area | Files | In pilote scope? |
| :--- | :--- | :--- |
| SPA shell | `organizerProblemRead.ts`, `needsAttentionToSituation.ts`, tests | Yes — attention + shared helper |
| SPA Matchs | `StructurePhaseMatchsLifecycleBlock.tsx`, `problems/*` | Matchs local module (contract **Paused** — lifecycle `problem` only) |
| SPA presentation | `functionalProblemPresentation.ts` | Yes — optional `problem` on structure issue helper |
| Docs | `organizer-problems-audit.md`, `step3-brief.md` | Yes — Phase 2 status lines |

**No** Application/Host changes in working tree. **No** mixed unrelated Matchs P0–P3 contract unpause.

## Alignment vs O1–O3

| Decision | WT code |
| :--- | :--- |
| O1 layered lifecycle | `normalizeLifecycleAnticipated` + `organizerProblemPresentationCode` |
| O2 Attention remediation | `needsAttentionToSituation` `suggestedActionCode` |
| O3 wire shape | Types already on `NeedsAttentionItem` / lifecycle DTO (committed) |
| Drawer policy | No `NeedsAttentionAssembler` change |

## Gaps vs revised plan (P1A remaining at audit)

- Drawer row did not yet pass `problem` to presentation layer (mapper dropped field).
- `OverviewSituation.problem` not added (deferred per plan — attention-only field on shared TS type).
- Load error / retry not implemented (P1B).

## VERIFY / DEFERRED still open

- Full alias registry (DEFERRED).
- O4 drawer catalogue expansion (G3 — not DECIDED).
- D5 implementation proposal (**DEFERRED** — see brief §7).
- `Severity` Warning/Info on wire (GAP audit).
- POST/read equivalence beyond pilot trio (VERIFY per code).

## Test record (2026-10-12)

| Command | Result |
| :--- | :--- |
| `dotnet test tests/MyClub.PlayUp.Application.Tests --filter "OrganizerProblemSemantics\|StructureStageLifecycleAssembler"` | **Pass** (20 tests) |
| `dotnet test tests/MyClub.PlayUp.Host.Tests --filter ReadSurfaceEndpointTests --no-build` | **Pass** (11 tests; rebuild blocked while Host PID running) |
| `npm test --run organizerProblemRead attentionLoadError needsAttentionToSituation attentionPartition AttentionDrawer structureProblems` | **Pass** (43 tests) |

**Pilot exit:** `pilot closed` for Application trio + O1–O3; post-pilot = drawer P1A/P1B and G2+.
