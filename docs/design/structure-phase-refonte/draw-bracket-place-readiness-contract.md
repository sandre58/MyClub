# Draw bracket place readiness — contract (audit slice)

**Status:** implemented (Application classifier, `StageOverview` facts, Rerun POST guard, SPA consumes overview facts).

**Slot product path:** Cup `createAndGenerateDraw` and a slot Relancer send `DrawInputsIntent.Partial`. Only free slots are filled; occupants stay. The launch gate allows that partial grid when unplaced teams and free slots match (`NoFreeSlot`, `NotEnoughTeams`, `TooManyTeams` otherwise). Groups, `Rerun`, and Redo stay a full reshuffle and still require a vacant grid. The Cup checklist line is met when at least one place is free.  
**Audience:** Application + HTTP + SPA Tirage checklist / create gate parity.  
**Related:** [draw-blocked-baseline](./draw-blocked-baseline.md), Notion Structure page (checklist callout 2026-10-16).

---

## Problem

The composition-band **Tirage checklist** (criterion `bracketPlacesFree`) and the SPA gate reason `occupiedSlots` must reflect the same notion of “place not available for a **first** Cup/Groups slot draw on an empty grid,” without the SPA reconstructing occupation from `GET …/schematic`.

Today:

| Source | What it exposes |
|--------|-----------------|
| `StageOverview` slots | `entryId` only — qual/prog **structural** reservation has no `entryId` |
| `GET …/schematic` | `feedOrigin` per case (Qual/Prog/Direct/Draw) — used as interim SPA workaround |
| `DrawInputsFactory` (`Default`) | Fixed slots = slots with `entryId` in composition pool only |
| `createAndGenerateDraw` | Slot: `Create` + `ConfigureDrawInputs(Partial)` + `Generate` (free slots only). Group: `Create` + `ConfigureDrawInputs(Rerun)` + `Generate` |
| `CreateDraw` (Application) | No occupancy / feed preflight |
| Structure hub per stage | `directAssignmentCount`; inbound composition = **seat** resolution, not per bracket slot feed |

**Gap:** checklist/gate semantics (entry + structural feeds) ≠ overview-only counting; schematic coupling is not an acceptable SoT.

---

## Scope of the fact

**Workflow:** *Draw launch preflight* for Cup (`Slot`) or Groups (`Group`), aligned with `resolveStageDrawCreateGate` → `occupiedSlots` and checklist line 3. On the fiche, the disabled primary CTA only applies when there are no draws yet (`overviewDraws.length === 0`); the dialog uses the same gate for **Nouveau** (including after Cancel).

**Out of scope for this DTO (unchanged channels):**

- `directAssignment` — remains **stage-level** (`directAssignmentCount` on Structure hub summary); separate gate reason and copy.
- Pool / Places N, pots, group shape, minimum teams — existing checklist lines and gate reasons.
- Encoding F / `DrawInputsIntent.Default` fixed promotion — generation concern, not the same as “grid must be empty before first launch.”
- Championship / Swiss — no draw mechanism V1.

**Authority model (product):**

1. **Read model** — explicit facts for checklist display (pedagogical, not exhaustive).
2. **SPA gate** — `resolveStageDrawCreateGate` remains the UX authorization for disabling “Perform draw” (see baseline doc).
3. **POST draw pipeline** — must enforce launch invariants **independently** of the client once arbitration in § POST guard is accepted. The read model and SPA consume the **same** Application classifier; they do not replace server refusal.

---

## POST guard — Rerun draw inputs (accepted 2026-10-16)

### What the code does today

| Step | Occupancy / DA check? |
|------|------------------------|
| `CreateDraw.Execute` | **No** — only `DrawRules` engaged, operational exit guard, Domain “one non-cancelled draw”. |
| `ConfigureDrawInputs` + `DrawInputsIntent.Default` | **No** — Encoding F promotes `slot.EntryId` / group seats into Fixed* ([Encoding F decision](https://www.notion.so/3e375fb92d108102880adf41111d6a49)). |
| `ConfigureDrawInputs` + `DrawInputsIntent.Rerun` | **Yes** — vacant grid and no direct assignment. Fixed* stay empty ([Rerun decision](https://www.notion.so/3e475fb92d108141bb0cf04ccd7def06)). |
| `ConfigureDrawInputs` + `DrawInputsIntent.Partial` | **Yes** — Slot only. Unplaced teams must equal free slots (`DrawNoFreeSlot`, `DrawNotEnoughTeams`, `DrawTooManyTeams`). Occupants are not in the pool. |
| `GenerateDrawResolution` | A Slot pool that is exactly the unplaced teams is generated onto free slots only. Generate does not write occupants. |
| SPA `createAndGenerateDraw` | Slot → `Partial`. Group → `Rerun`. Launch allowed/blocked comes from hub `drawCreateLaunch`. |

`Rerun` and Redo still refuse a populated grid. `Partial` is the Cup path that fills only free slots. `Default` remains Encoding F.

### What product decisions imply

- **Gate Tirage** (M1–M7) lists Structure + Entrées + DrawRules — not an explicit “vacant grid” row, but Tirage is a distinct gate from Composition cardinality.
- **Encoding F** (*Default*) **requires** the server to accept partial occupancy and treat existing placements as Fixed*.
- **Rerun** (*Nouveau tirage*) deliberately **ignores** occupancy for Fixed* — but UX + **Libérer placements** assume the operator clears aligned occupants before a meaningful Apply; the SPA already blocks **Nouveau** while the grid is blocking.
- **Placement manuel** — “DA bloque Nouveau Tirage (gate existant)” — product invariant, today **not** enforced in `CreateDraw`.

**Conclusion:** `occupiedSlots` is **not** a `CreateDraw` Domain invariant. It is a **launch precondition** for the **Rerun product path** (full reshuffle), which must coexist with **Default** partial draws.

### Options

| Option | Description | Verdict |
|--------|-------------|---------|
| **A — Client-only** | Keep server permissive; read model is display-only. | **Reject** for this slice — leaves API bypass and allows Generate on qual-reserved slots without feed awareness. |
| **B — Block `CreateDraw`** when `BlockingPlaceCount > 0` | Simple HTTP surface. | **Reject** — would block or complicate **Encoding F** (`ConfigureDrawInputs(Default)` on a partially filled grid). |
| **C — Block launch on `ConfigureDrawInputs(Rerun)`** (recommended) | Shared `StageDrawBracketLaunchClassifier` + refuse when `BlockingPlaceCount > 0` or `DirectAssignments.Any()` for Slot/Group kinds. **Default** intent unchanged. | **Recommended** — aligns SPA `createAndGenerateDraw`, dialog **Nouveau**, and API; preserves Encoding F. |
| **D — Block `GenerateDrawResolution` only** | Infer launch mode from empty Fixed*. | **Weak** — ambiguous vs Default with empty grid; intent not persisted on `Draw`. |

### Normative rule

> For `DrawInputsIntent.Rerun`, the Application layer rejects input configuration when `BlockingPlaceCount > 0` or when at least one direct assignment exists for the stage. The rejection uses the same occupancy classifier as `StageDrawBracketLaunchFactsDto` and returns an explicit Application error mapped to HTTP `409 Conflict`.
>
> `DrawInputsIntent.Default` retains Encoding F behaviour, including partially occupied grids.
>
> **Precedence (SPA parity):** `Application.DrawLaunchDirectAssignmentPresent` before `Application.DrawLaunchGridNotVacant` when both apply.

**Hook:** `ConfigureDrawInputs.Execute` before `stage.ConfigureDrawInputs`; `UseCaseExecutor.ConfigureDrawInputsAsync` loads competition stages for a consistent snapshot.

**Codes:** `Application.DrawLaunchGridNotVacant`, `Application.DrawLaunchDirectAssignmentPresent` → HTTP 409.

---

## What counts as a bracket place state (Cup entry places)

Classification is per **entry place** (Cup: `CupSlot` schematic positions / `stage.Slots` keys that participate in draw launch; Groups: group seats — see below).

Use the same feed snapshot as schematic inbound resolution: `SlotFeedSnapshotAssembler` + `SlotFeedResolver` (not a second ruleset in TypeScript).

| Situation | Classification | Blocks first draw launch (`occupiedSlots` semantics) | Checklist line 3 |
|-----------|----------------|----------------------|------------------|
| Slot with `entryId` in composition pool | `OccupiedByEntry` | Yes | Count toward “places occupied” |
| Unique Qualification feed on slot | `ReservedByStructuralFeed` | Yes | Count |
| Unique Progression feed on slot | `ReservedByStructuralFeed` | Yes | Count |
| Unique published Draw feed on slot | `ReservedByStructuralFeed` | Yes | Count |
| Unique Direct feed on slot | `ReservedByStructuralFeed` | Yes (slot claimed); **also** stage may have `directAssignmentCount` → `directAssignment` reason | Prefer gate reason `directAssignment` when any DA exists; slot still reserved in counts |
| Feed `Missing` and no `entryId` | `VacantForDrawLaunch` | No | Vacant |
| Feed `Multiple` or `Invalid` | `FeedResolutionInvalid` | Yes (not “free”) | Count as occupied **or** expose separate invalid flag — **must not** display as “aucun” / vacant |
| Inbound qual seat “placed” without destination `entryId` | `ReservedByStructuralFeed` when resolver maps to slot feed | Yes | Same as qual feed on slot |

**Groups:** For `Group` draw kind, “place” = group seat index with an `entryId` in `StageOverview.groups` (materialized occupant). Structural feeds on group places follow schematic/group rules when implemented; V1 checklist gate today uses schematic `GroupPlace` cases when loaded — backend slice should mirror group occupancy from stage groups + feed snapshot if applicable.

---

## Proposed HTTP fact (names stable after review)

Extend **`GET /stages/{id}/overview`** (existing `StageOverviewDto`) with an optional block:

```csharp
/// <summary>
/// Bracket place facts for Tirage launch preflight (Cup/Groups with draw rules).
/// Null when not applicable (e.g. no draw mechanism).
/// </summary>
public sealed record StageDrawBracketLaunchFactsDto(
    int EntryPlaceCount,
    int OccupiedByEntryCount,
    int ReservedByStructuralFeedCount,
    int FeedResolutionInvalidCount,
    /// <summary>OccupiedByEntry + Reserved + Invalid — matches SPA occupiedSlots when > 0.</summary>
    int BlockingPlaceCount,
    int VacantForDrawLaunchCount);
```

### Counter invariants (must be tested on the classifier)

Let `P` be the finite set of entry places in launch scope, `|P| = EntryPlaceCount`.

Each place is classified into **exactly one** of:

- `OccupiedByEntry` — materialized `entryId` on slot / group seat in pool.
- `ReservedByStructuralFeed` — `SlotFeedResolver` status `Unique` (Qual / Prog / Draw / Direct on that slot).
- `FeedResolutionInvalid` — status `Multiple` or `Invalid`.
- `VacantForDrawLaunch` — no `entryId` and feed status `Missing`.

Then:

```text
OccupiedByEntryCount
  + ReservedByStructuralFeedCount
  + FeedResolutionInvalidCount
  + VacantForDrawLaunchCount
  = EntryPlaceCount

BlockingPlaceCount
  = OccupiedByEntryCount
  + ReservedByStructuralFeedCount
  + FeedResolutionInvalidCount

VacantForDrawLaunchCount
  = EntryPlaceCount - BlockingPlaceCount
```

Rules:

- No double counting — e.g. `entryId` on a slot with a Unique feed still counts as **one** place (classify as `OccupiedByEntry` **or** `ReservedByStructuralFeed` with a single documented precedence: **entry wins** if present).
- `FeedResolutionInvalid` places are **never** included in `VacantForDrawLaunchCount`.
- `directAssignmentCount` / `DirectAssignments` are **not** folded into these six counters; they remain a **separate** launch refusal (SPA reason `directAssignment`; server guard on Rerun per § POST guard).

**Derived checks (SPA after backend ships):**

- `bracketPlacesFree` ⇔ `BlockingPlaceCount == 0`
- `hasOccupiedSlots` ⇔ `BlockingPlaceCount > 0`
- If `FeedResolutionInvalidCount > 0`, checklist may still show line 3 unmet; optional future copy links to topology issues (not in checklist scope today).

**Do not** add per-slot `blocksDraw` booleans without a documented enum; prefer aggregate counts + optional future per-slot diagnostic endpoint if needed.

---

## Endpoint choice (audit conclusion)

| Option | For | Against |
|--------|-----|---------|
| **`StageOverview`** (recommended) | Already loaded for Tirage column / draw dialog; slots + draws co-located; one round-trip for gate + checklist | Slightly widens overview payload |
| **Structure hub** | Good for `directAssignmentCount` + inbound (already on hub stage summary) | Heavier graph load; inbound DTO is feed-seat oriented, not bracket launch grid; duplicate feed logic if facts only live on hub |

**Decision:** implement `StageDrawBracketLaunchFactsDto` on **`StageOverviewDto`**, assembled via the same **`StageDrawBracketLaunchClassifier`** used by the Rerun POST guard (§ POST guard).

Structure hub continues to supply `directAssignmentCount` for `directAssignment` gate reason.

---

## Shared Application rule (implementation sketch)

1. Build `SlotFeedSnapshot` for target stage (same inputs as schematic assembler).
2. For each slot key in draw launch scope, resolve feed via `SlotFeedResolver`.
3. Classify using matrix above + `slot.EntryId`.
4. Aggregate counts.

Unit tests: matrix rows (entry only, qual unique, prog unique, draw unique, direct unique, missing, invalid, multiple, entry + feed conflict).

**Remove after backend:** SPA `schematicCases` parameter to `countBracketPlacesBlockingDraw` / `resolveStageDrawCreateGate` (interim workaround).

---

## Implementation order (after POST arbitration)

1. `StageDrawBracketLaunchClassifier` + matrix unit tests (including Invalid / Multiple never vacant).
2. `StageOverviewDto` + assembler exposing the six counters.
3. Coherence tests: classifier ↔ DTO; Rerun guard ↔ `BlockingPlaceCount` / DA (if option C accepted).
4. SPA: consume DTO; remove `schematicCases` from `drawUi.ts`.
5. Regression: checklist, CTA, dialog **Nouveau**, Cup/Groups; **Default** Encoding F partial draw still works via API.

## Regression checklist (post-implementation)

- [ ] Qual-filled tableau: checklist line 3 shows count > 0 without `/schematic`.
- [ ] Overview-only manual `entryId`: unchanged behaviour.
- [ ] Invalid feed slot: not shown as “aucun”.
- [ ] `directAssignmentCount > 0`: gate `directAssignment` unchanged; checklist line 3 may still show occupancy.
- [ ] `POST …/inputs` with `Rerun` returns 409 when `BlockingPlaceCount > 0` or DA present (if option C).
- [ ] `POST …/inputs` with `Default` on partially occupied grid still succeeds (Encoding F).

---

## Notion product sentence (French)

> Occupation du tableau : la checklist consomme un fait explicite du read model Application du workflow Tirage. Elle ne déduit pas l’occupation depuis le schéma ; le gate serveur reste l’autorité pour autoriser la création.
