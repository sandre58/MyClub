# Composition tile — lifecycle presentation (signed)

**Status:** Product decision (presentation only).  
**Scope:** Structure fiche **Composition** band — visibility, density, copy, interactions.  
**Out of scope:** §11B authorization / mutability matrix (gates unchanged); new hub fields; schematic as SoT for qualifiers.  
**Related:** [phase-mutability-matrix.md](./phase-mutability-matrix.md), [structure-diagnostic-presentation-contract.md](./structure-diagnostic-presentation-contract.md), [places-operational-strip-occupancy-arbitration-2026-10-16.md](./places-operational-strip-occupancy-arbitration-2026-10-16.md).

---

## 1. Principle

Keep **one Composition tile** and one business narrative (**entrants → pool → structural placement / draw**). Adapt **density and interactivity** to lifecycle utility — not a different tile per status.

Presentation may change with phase status **without** changing server authorization (same separation as diagnostics §1.1).

**Primary driver:** operational usefulness of each zone at this point in the cycle, with **stage status** as the main presentation bucket (`Draft` / `Ready` vs exploitation vs `Completed`).

**Suspended:** presentation follows **Running** (regroupement per [structure-diagnostic-presentation-impact-rules.md](./structure-diagnostic-presentation-impact-rules.md) §3). Match-clock copy stays on match surfaces.

---

## 2. Zone matrix (default)

| Zone | Draft / Ready | Running / Suspended | Completed |
|------|---------------|---------------------|-----------|
| **Qualifiés** | Configure + resolve inbound feeds | Read + track entrants (resolved / placed / unresolved) | Historical consult |
| **Équipes** | Affectation authoring pool + **Entrées** gauge vs Places N (work) | **Engaged runtime pool** (see §5.1) + **Résolues** gauge (inbound seats when present, else pool vs Places N) | Final engaged state + **Résolues** gauge when total > 0 |
| **Affectation des places** (Places + Tirage strip) | Full operational strip (work zone) | **Compact operational summary** + path to detail | Final state consult |

### Rules

1. **Do not hide** the Composition tile in exploitation — it carries the structural chain.
2. **Do not keep three columns at prep density** in Running/Suspended/Completed — no disabled prep CTAs, no draw-readiness checklist tone.
3. **Read-only ≠ frozen fiction** — show real hub state (unresolved qual seats, partial occupancy, draw not applied) without re-exposing forbidden actions.
4. **Do not duplicate the schematic** — synthesis here; schematic and draw workflow remain investigation surfaces.

---

## 3. Places / Tirage summary — content modes (not status-only)

Lifecycle bucket chooses **layout**; **summary copy/severity** also depends on structural facts (existing hub + presentation policy — no new backend rules).

| Mode | When (examples) | Presentation |
|------|-----------------|--------------|
| **Normal** | Draw applied, places aligned with policy, inbound feeds resolved as expected | Neutral synthesis (counts, applied badge, short labels) |
| **Structural gap** | Unapplied draw, partial place occupancy, unresolved inbound, `SlotFeedMissing` (non-awaiting-draw silence), etc. | **Informative** signal per diagnostic contract — not prep CTAs, not “fix now” checklist |
| **Historical** | `Completed` (and stable exploitation end-state when defined) | Final snapshot; gaps shown as **archived fact**, not active prep anomaly |

**Open (recette):** Running/Suspended with a **persistent structural gap** — summary must remain visible and classified (mode 2), not collapsed to mode 1.

Signals already available for SPA (non-exhaustive): `stage.status`, `stage.actions`, `inboundComposition`, `compositionEntryCount`, draw overview (applied/published), `structureIssues` + [structureDiagnosticPresentationPolicy](../../../src/PlayUp/web/src/pages/structure/structureDiagnosticPresentationPolicy.ts), places strip occupancy arbitration.

---

## 4. Interactions (align with mutability)

| Capability | Draft / Ready (no production) | Running / Suspended | Completed |
|------------|-------------------------------|---------------------|-----------|
| Edit affectation pool | As today (action-gated) | Hidden | Hidden |
| Draw workflow CTAs | As today | Hidden; link/detail only | Hidden |
| Manual Cup placement | As today | Hidden | Hidden |
| View phase / draw detail / schematic | Allow | Allow | Allow |

---

## 5. Reference recette cases (before SPA)

| ID | Scenario | Assert |
|----|----------|--------|
| **R-CT-1** | Cup — draw applied, confrontation engaged | Compact Places/Tirage summary; no prep checklist; qual/teams readable |
| **R-CT-2** | Groups — matches in progress | See **§5.1** (engaged pool); no compose CTA; Places summary neutral unless gap codes active |
| **R-CT-3** | Championship — `Completed` | Historical synthesis; no active prep messaging |
| **R-CT-4** | **Suspended** + unresolved inbound or draw/places gap | Summary mode **structural gap** visible; informative tone; no forbidden CTA |

### 5.1 R-CT-2 — Équipes: affectation authoring vs engaged pool (signed)

In **exploitation** (`Running` / `Suspended`), the **Équipes** zone **prioritises runtime composition** actually engaged in the structure when hub data is **available and reliable** for the format. **Affectation authoring** remains a distinct configuration source and must **never** be shown as if it were guaranteed to match the engaged pool.

If runtime engaged-pool data is **not** available or not reliable for the format, the zone must state explicitly that the **engaged pool is undetermined** — **no silent fallback** to affectation authoring.

Presentation stays **read-only**; server authorization is unchanged.

In **`Completed`**, show the **final state available** from the same priority rules. Do not imply that authoring affectation and engaged composition were necessarily identical at close.

**Keep both sources distinguishable in UI** (labels/copy), even when only one list is shown.

#### Hub signals (verify before SPA — do not invent SoT)

| Hub field | Documented meaning (Structure GET) | Typical use |
|-----------|--------------------------------------|-------------|
| `affectationEntryIds` / `affectationEntryCount` / `affectationPreviewNames` | **Authoring** population set (Population tile) | Draft / Ready **work** zone; secondary label in exploitation if contrast needed |
| `compositionEntryIds` / `compositionEntryCount` / `compositionPreviewNames` | **Runtime** composition membership (Draw / Live pool) | Exploitation **Équipes** list + engaged-pool gauge when reliable |
| `compositionIneligibleCount` | Runtime entries no longer Active | Exploitation attention (informative), not prep checklist |
| `expectedPopulationOccupancy` (SPA helper today) | Affectation + inbound **configured** volume | **Prep** meter (U1); **not** engaged-pool SoT in exploitation |

**Implementation guardrails (mandatory before merge):**

1. **No invented SoT** — map each format (`Cup`, `Groups`, `Championship`, `Swiss`) to which hub fields are authoritative for “engaged pool” in exploitation; document gaps in recette notes if a format cannot be determined.
2. **Count ≠ occupancy** — use `compositionEntryCount` (or list cardinality) for **pool membership** only when its contract matches engaged pool for that format; do **not** conflate with schematic place occupancy or Places strip k/N without checking existing arbitration ([places-operational-strip-occupancy-arbitration-2026-10-16.md](./places-operational-strip-occupancy-arbitration-2026-10-16.md)).

**SPA baseline today (to change):** `listComposedAffectationEntries` reads `affectationEntryIds` only — exploitation mode must branch per §5.1.

---

## 6. Implementation note (SPA)

Introduce a single **composition band presentation mode** derived from `stage.status` (+ optional exploitation helpers), then branch:

- strip layout: `full` vs `compactSummary` vs `historical`;
- **Équipes** data source: affectation authoring (prep) vs composition runtime (exploitation / completed per §5.1).

Reuse [structureDiagnosticPresentationPolicy](../../../src/PlayUp/web/src/pages/structure/structureDiagnosticPresentationPolicy.ts) for gap severity in summary — do not fork a parallel rules engine.

**Agent gate:** §5.1 guardrails satisfied per format in recette R-CT-1…4 before merge.

Deferred P3 “visibility matrix” experiments are superseded by this decision for lifecycle density; population-state nuances may still refine within each zone.
