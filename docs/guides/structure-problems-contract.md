# Structure — problems messaging contract (Matchs column)

**Purpose:** Git-side reference for **centralized lifecycle problem copy and visibility** on the Structure SPA **Matchs operational column**. Host and read model remain the source of truth for business rules and authorization.

**Related:** [Structure phase lifecycle UI](./structure-phase-lifecycle-ui.md) · [Organizer problems audit (working draft)](./organizer-problems-audit.md) · Notion *Lifecycle phase — UI Structure*.

---

## Status: Paused

**G5 (2026-10-12):** No active implementation tasks — unpause requires explicit product decision. Matchs SPA changes in tree remain local module only.

**Generalization remains suspended.** Step 2 is **PARTIALLY DECIDED** (2026-10-12): semantic **principles** are recorded in [organizer-problems-audit.md](./organizer-problems-audit.md); wire shape, alias equivalence, and drawer behaviour remain **VERIFY** / **DEFERRED** as stated there. The **Step 3 documentation phase** explicitly **excludes** this Matchs local pilot. Unpause requires a **separate** product decision — not implied by Step 2 principles or Step 3 planning.

While **Paused**:

- The **Matchs column module** (`src/PlayUp/web/src/pages/structure/problems/`), if present in the tree, is a **local presentation prototype** only — not a cross-surface error framework.
- **No** extension to Overview, the Needs attention drawer, or global mutation error handling based on this document.
- **No** Application, Host, or read-model DTO work under this contract until the Matchs pilot is explicitly included in an approved implementation brief.

The sections below record **Matchs-column intent** from the 2026-10-12 discussion. They are **not** a merge gate while status is Paused.

---

## Scope (Matchs column — when resumed)

Draft Prepare empty state and Ready / Running / Suspended lifecycle block in `StructurePhaseMatchsLifecycleBlock.tsx`. **Deferred while paused:** production strip, diagnostic rails, dialogs, global `MutationError`, Overview, backend changes.

---

## Principles

1. **One i18n namespace** for Matchs lifecycle problems: `structure.problems.lifecycle.*` and `structure.problems.mutation.*`.
2. **Pure TypeScript** for wire-code normalization, message resolution, and Matchs-column visibility — no global store, no duplicated C# rules.
3. **Three concerns** (small functions, not layers): normalize source → resolve message → apply visibility for this surface.
4. **HTTP / ProblemDetails unchanged**; the SPA interprets codes for display only.
5. **API errors keep provenance** (`source: 'api'`, wire code); unknown codes use explicit fallbacks, not fake anticipated copy.

---

## Product arbitrations (Matchs column — historical 2026-10-12)

*Apply when this contract is **unpaused** after transversal semantic arbitration.*

| Topic | Rule |
| :---- | :--- |
| Draft + `ConstructionFrozen` | Keep validated Draft UX: generic draft detail in body; **no** gel tooltip or paragraph that contradicts presented state. |
| `StageNotReady` on Draft | Body uses `lifecycle.state.Draft.detail` (honest aggregate), not `problems.lifecycle.StageNotReady`. |
| Prepare checklist | When read model emits a **specific** disabled code, show the matching `problems.lifecycle.{code}` message. |
| Readiness vs rail dedup | **Deferred** (hub); when implemented, dedupe only on stable keys for the **same** business issue. |

---

## Wire sources (Matchs column)

| Source | Field | When |
| :----- | :---- | :--- |
| Anticipated transition | `lifecycle.transition.disabledReasonCode` | Primary CTA disabled (Prepare / Start / Complete) |
| Anticipated cancel-start | `lifecycle.cancelStartTransition.disabledReasonCode` | Cancel-start icon disabled (tooltip) |
| Anticipated revert | `lifecycle.revertTransition.disabledReasonCode` | Suspend / Resume / Revert-to-draft |
| POST lifecycle mutation | `ProblemDetails.code` (+ optional `reasons`) | After failed `useStructureStageLifecycleMutation` |

---

## Inventory — anticipated lifecycle codes

Mapped to `structure.problems.lifecycle.{Code}` (FR/EN parity required):

| Code | Typical surface |
| :--- | :-------------- |
| `_unknown` | Fallback |
| `CompetitionClosed` | Any |
| `ConstructionFrozen` | Prepare (Draft); visibility suppressed on Matchs (see below) |
| `Places` | Prepare |
| `InitialRoundNotPlayable` | Start |
| `StageNotReady` | Prepare (non-Draft body only; Draft uses state detail) |
| `StructureGraphInvalid` | Prepare |
| `OpenMatches` | Complete (paragraph suppressed Running/Suspended — stats carry signal) |
| `ConfrontationUndecidable` | Prepare / gates |
| `OverCapacityEdge` | Prepare |
| `OverExtraction` | Prepare |
| `SlotFeedMissing` | Prepare |
| `SlotFeedInvalid` | Prepare |
| `SlotFeedMultiple` | Prepare |
| `CycleDetected` | Prepare |
| `RoutingNondeterminism` | Prepare |
| `UnmaterializedConfrontations` | Start (paragraph suppressed) |
| `ProductionPresent` | Revert / cancel-start (tooltip only) |
| `MatchLifecycleStarted` | Revert / cancel-start (tooltip only) |
| `MatchLiveInProgress` | Suspend (tooltip only) |

Legacy keys under `lifecycle.disabled.*` remain until other surfaces migrate; Matchs column resolves via `problems.*`.

---

## Mutation mapping

| Wire | Resolved message |
| :--- | :--------------- |
| `Application.StageCompletionBlocked` | Same copy as `problems.lifecycle.OpenMatches` |
| Known `errors:{code}` with SPA entry | `problems.mutation.{code}` if present, else `errors` namespace via `apiErrorLabel` |
| Unknown / missing code | `problems.mutation._unknown` |
| Non-`ApiError` with message | Raw `Error.message` (client) |

---

## Matchs column visibility

Implemented in `matchsColumnVisibility.ts` (extracted from former status-band helpers).

| Rule | Behaviour |
| :--- | :---------- |
| Primary paragraph | Hidden when code is `OpenMatches` and status is Running or Suspended |
| Primary paragraph | Hidden for `ConstructionFrozen`, `UnmaterializedConfrontations` |
| Draft Prepare tooltip | Hidden when code is `ConstructionFrozen` |
| Revert paragraph | Hidden for `ProductionPresent`, `MatchLifecycleStarted`, `MatchLiveInProgress` (tooltip on control) |
| Remediation link | Shown when href present; `ConstructionFrozen` → Overview link + `lifecycle.statusBand.viewOverview` |
| Mutation error | Always `Alert` danger under the block when POST fails |

---

## Validation gate (before P4+)

Manual checks still required in the browser; automated coverage for P0–P3:

- [ ] Five lifecycle statuses: badge unchanged; Matchs block matches status matrix in lifecycle UI guide
- [ ] Draft Prepare: enabled/disabled, tooltip rules, POST error alert
- [ ] Ready / Running / Suspended: primary, cancel-start, pause/resume, revert-to-draft
- [ ] Disabled reasons: paragraph vs tooltip per table above
- [ ] Remediation links when read model provides href / ConstructionFrozen Overview
- [ ] No duplicate OpenMatches copy vs match stats on Running/Suspended
- [x] FR/EN parity for all `problems.*` keys in inventory (`structureProblems.test.ts`)
- [x] Vitest: normalization, unknown codes, visibility helpers (`structureProblems.test.ts`, `structurePhaseStatusBandModel.test.ts`)

---

## Code map

| Area | Path |
| :--- | :--- |
| Contract (this doc) | `docs/guides/structure-problems-contract.md` |
| Normalize / resolve / visibility | `src/PlayUp/web/src/pages/structure/problems/` |
| Matchs UI | `StructurePhaseMatchsLifecycleBlock.tsx` |
| Action labels / toasts (unchanged) | `structurePhaseLifecycleActions.ts` |
| Lifecycle action codes | `structurePhaseStatusBandModel.ts` |
