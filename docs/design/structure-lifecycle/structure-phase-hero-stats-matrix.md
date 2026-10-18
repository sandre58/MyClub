# Structure phase fiche — hero stats (product + data contract)

Status: **product decisions validated (2026-10-18)** · **Application + SPA hero presentation shipped** (Swiss contract reserve unchanged).

**Notion (product SoT):** task [Stats hero — réflexion par type × statut](https://app.notion.com/p/3fd75fb92d1081f0af6fc64d3476815c) · [Matrice UI Structure §4.1](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4).

---

## Architecture principle (signed)

| Layer | Responsibility |
| ----- | -------------- |
| **Backend (Application reads)** | Structural facts, business rules for their values, and when a fact is unavailable |
| **Frontend (PlayUp web)** | Presentation only: visibility (matrix + UI context such as strip duplication), ordering, labels, formatting — **no recomputation of structural values from raw schematic/domain inputs** |
| **No new HTTP surface by default** | Audit `GET` structure hub (+ schematic if needed) first; evolve `StructureStageHubSummaryDto` (or a nested DTO) only after explicit contract arbitration |

`resolveStructurePhaseHeroFacts(...)` **may** exist later as a **pure presentation selector** over facts already on the wire (or over a backend-provided `heroFacts[]` payload). It must **not** become a disguised metrics service.

**Visibility ≠ calculation:** the SPA may hide a fact; it must not derive the fact’s numeric value.

Related: [truth-sources.md](../structure-phase-refonte/truth-sources.md) (occupancy vs hero).

---

## Product decisions (A–F)

| ID | Decision | Status |
| -- | -------- | ------ |
| **A** | Hero is **strictly topological** — no `X/N` occupancy in hero; occupation belongs to the Places strip. No automatic occupancy fallback when the strip is absent without an explicit product decision. | **Validated** |
| **B** | **Cup:** rounds in hero, **not** legs. Hub cards may show more metrics; fiche hero stays silhouette. | **Validated** |
| **C** | **Groups:** group **count**, not `G×P` grid. | **Validated** |
| **D** | **Championship:** show matchdays only when count **> 0** (avoid `0` noise in Draft). Value = **configured** skeleton matchdays, not matchdays with attached matches (see § Arbitration #1). | **Validated** |
| **E** | **Never** show materialized match count in hero (including Completed without Matchs aside). | **Validated** |
| **F** | Keep **Modifier la forme** in the **header** for now; revisit B2 chrome only after render + intent review. | **Pending** |

**Guiding rule:** lifecycle status does not change the **nature** of hero facts; it only affects **visibility** when a fact is noise or production.

### Target facts by format (hero)

| Format | Facts (order) | Show conditions |
| ------ | ------------- | --------------- |
| Championship | Matchdays · Places N | Matchdays if `> 0`; Places N if `> 0` and not duplicated by Places strip |
| Cup | Rounds · Places N | Rounds if `> 0`; Places N as above |
| Groups | Groups · Places N | Groups if `> 0`; Places N as above |
| Swiss | Generated/planned rounds · Places N | Rounds row if generated `> 0` or planned `> 0`; Places N as above |

### Status overlay (visibility only)

| Status | Rule |
| ------ | ---- |
| Draft | Configured topology only — **no production** stats in hero |
| Ready | Same topology — **no** materialized match stats |
| Running | Same topology — progression/production in operational surfaces |
| Suspended | Same as Running — no hero signal that implies work to do |
| Completed | Keep topology — **no** production numbers in hero |

**Places N:** hide when the composition Places strip owns the signal (`showPlacementBlock` today — presentation flag, not lifecycle).

**Swiss (reserve):** keep generated/planned when available; in **Completed**, validate copy so it does not read as pending work (i18n/recette, not a third rule matrix).

**Hub vs fiche:** `topologyCardFacts` (Structure hub cards) = multi-metric preview; phase fiche hero = 2–3 silhouette metrics — **do not merge grammars in code**.

---

## Arbitration #1 — Championship matchdays (**validated 2026-10-18**)

Two notions must **not** be conflated on the wire:

| Notion | Meaning | Hero Championship |
| ------ | ------- | ----------------- |
| **Configured matchdays** | `Stage.Matchdays.Count` — skeleton structure, matches may be absent | **This is the hero fact** |
| **Matchdays with attached matches** | Matchdays where at least one fixture has `MatchIds` | **Not** the hero fact (`CountMatchdaysWithAttachedMatches` today) |

**Product rule:** hero shows configured count for **all** stage statuses when `> 0`; when zero configured matchdays, the stat is **hidden** (not “0 journées”).

| Situation | Expected hero |
| --------- | --------------- |
| Draft, no configured matchdays | Hidden |
| Draft, 5 configured, no matches | 5 |
| Ready / Running / Suspended / Completed, 5 configured | 5 |

**Backend obligation:** expose configured count on each `StructureStageHubSummaryDto` with **explicit** XML/docs semantics. Do **not** repurpose existing `MatchdayCount` on the hub row if it remains “with attached matches” (other consumers may rely on that meaning).

---

## Data provenance audit (current contracts)

Primary wire: **`GET` structure view** → `StructureViewDto.Stages[]` as `StructureStageHubSummaryDto` (`StructureViewAssembler.BuildStageHubSummary`).

Secondary: **`GET` stage schematic** → `StageSchematicDto` (occupancy for strip/issues — **not** hero per A).

### Hero fact → source today

| Hero fact (target) | Intended backend authority | On wire today | Frontend today | Gap / note |
| ------------------ | -------------------------- | ------------- | -------------- | ---------- |
| Group count | `Stage.Groups.Count` | `StructureStageHubSummaryDto.GroupCount` | Direct | OK |
| Cup round count | `Stage.Rounds.Count` | `RoundCount` | Direct | OK |
| Championship matchdays (topology) | `Stage.Matchdays.Count` (configured) | Hub `MatchdayCount` = **attached-match** count only — **wrong field for hero** | `stage.matchdayCount` | **Gap — Application:** add e.g. `ConfiguredMatchdayCount` on hub summary (see § Recommended Application contract) |
| Places N | Hub `CompositionCapacity` / `ResolvePlaces` | `CompositionCapacity` when set | `resolvePlacesN()` with **client fallback** | **Hero:** hub value only — **no** frontend fallback for hero |
| Swiss generated | TBD — must be guaranteed on wire | Hub `MatchdayCount` (attached-match semantics) | `swissRoundCounts().generated` ← hub | **Open (item 3):** domain Swiss logic often uses `Matchdays.Count` (e.g. `OverviewAssembler`); hub row differs — clarify or nest DTO before hero relies on it |
| Swiss planned K | `SwissSettings.RoundCount` | `SwissRoundCount` | `swissRoundCounts().planned` | OK if pairing is documented or nested DTO |
| Materialized matches | Production | `Materialize.MaterializedCount` | Shown in hero when materialize present | **Remove** (E) — presentation only |
| Occupied places | Schematic occupancy rules | Computed in Application (`StructurePlaceOccupancyIssues.CountOccupiedPlaces`) for issues; **not** on hub | `countSchematicOccupiedPlaces` on schematic cases | **Not for hero (A).** Strip/meters: **technical debt** — FE duplicates backend rules; align with backend projection over time (see truth-sources) |

### Endpoints (no new API assumed)

| Endpoint / payload | Role for hero |
| ------------------ | ------------- |
| Structure view (`stages[]` hub summary) | **Primary** source for all hero numbers after contract cleanup |
| Stage schematic | **Exclude** from hero value calculation (A); may remain for schematic UI and strip occupancy until occupancy is hub-projected |

---

## Contract arbitration backlog

| # | Topic | Status |
| - | ----- | ------ |
| 1 | Championship configured vs attached matchdays | **Closed** — configured on hub (§ Arbitration #1) |
| 2 | Places N — hero uses hub only, no FE fallback | **Closed** (product) — enforce in SPA ticket |
| 3 | Swiss generated/planned semantics on wire | **Open** — keep existing fields only if documented; else explicit DTO |
| 4 | Strip occupancy (`countSchematicOccupiedPlaces`) | **Out of scope** — [truth-sources](../structure-phase-refonte/truth-sources.md); not hero (A) |
| 5 | Optional `StructurePhaseHeroFactDto[]` on hub | **Deferred** — prefer minimal new fields unless packaging wins in Application review |

### Recommended Application contract (ticket scope)

**Implementation ticket (full text):** [structure-phase-hero-configured-matchdays-application-ticket.md](./structure-phase-hero-configured-matchdays-application-ticket.md).

**In scope (hero unblock):**

1. Add **`ConfiguredMatchdayCount`** (or equivalent name) on `StructureStageHubSummaryDto`:
   - Value: `stage.Matchdays.Count`
   - Document: configured skeleton matchdays; independent of attached matches; stable across lifecycle statuses.
2. **Leave** existing hub **`MatchdayCount`** semantics unchanged (`CountMatchdaysWithAttachedMatches`) unless a full consumer audit proves a safe migration.
3. Contract tests: Championship (and any format with matchdays on skeleton) — configured count on hub matches domain; cases with skeleton matchdays but zero attached matches still report configured count > 0.

**Places N (same ticket or immediate follow-up):** hero consumers read `CompositionCapacity` only; Application ensures capacity is present when Places N is meaningful (existing `ResolvePlaces` path).

**Out of scope:** new HTTP route; hero fact array DTO; strip occupancy projection; B2 chrome.

**Reference inconsistency (audit note):** `StructureTopologySummaryDto.MatchdayCount` on the **primary** stage already uses `primary.Matchdays.Count` (`BuildStructureSummary`), while **per-stage hub** rows use attached-match count — align hero with **per-stage configured** field, not primary-only topology.

---

## Implementation sequence

1. **Validate** Championship semantics (this document + Notion task) — **done**.
2. **Application ticket** — hub field + tests (above).
3. **SPA ticket** — `resolveStructurePhaseHeroFacts` visibility/render only; D/E/B/C/A rules; no `matches` stat; Championship uses `configuredMatchdayCount`; Places N from hub only.
4. **Design Lab** — after SPA; **B2 / F** separate.

**Commit:** when Git annex + README reflect arbitrations and working tree is reviewed (not before).

---

## Current SPA behaviour (baseline for diff)

Logic lives in `StructurePhaseFiche.tsx` (~804–871): builds `heroFacts` from hub fields + `resolvePlacesN` + `swissRoundCounts` + optional `materialize.materializedCount`. Rendered in `StructurePhaseSchematicAndAside.tsx`.

---

## Changelog

| Date | Change |
| ---- | ------ |
| 2026-10-18 | Initial matrix: product A–E, architecture principle, provenance audit, contract gaps |
| 2026-10-18 | Arbitration #1: Championship hero = configured matchdays; Application `ConfiguredMatchdayCount` recommended; backlog items 2–4 clarified |
