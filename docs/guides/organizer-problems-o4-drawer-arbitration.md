# Organizer problems — O4 drawer scope (G3 atelier)

**Status:** **Pending product arbitration** — no Application or drawer payload change until **DECIDED** and written contract.

**Prerequisite question:** What user problem does each candidate fact solve that Structure, Overview, or lifecycle surfaces do not already address?

Detecting a fact in the backend is **not** sufficient for a drawer line.

---

## Two exercises (do not conflate)

| Exercise | Input | Output |
| :------- | :---- | :----- |
| **A — Generic O4 principles** | Inventories in this doc (Annexes A–B), workshop proposals below | DECIDED rules for line unit, catalogue stance, exclusions, severity mapping |
| **B — Competition-specific** | Export `GET /competitions/{id}/structure` (issues per stage, blockers) | What the banner “N problems” actually is; dedup examples grounded in one org state |

Exercise **A** can proceed **without** a live export. Exercise **B** is required to interpret a concrete count (e.g. six Structure problems) and to validate dedup against real overlaps — not to decide whether the drawer should mirror the banner in general.

**Code policy until Notion `DECIDED`:** `GET /attention` stays **T5 only**; no collector or payload extension.

---

## Workshop order (recommended)

1. Validate or correct **line unit / dedup** (decision 4).
2. Fill **Annex A4** using Annex A inventory + family catalogue proposals (decision 1 — per code or per family default).
3. Write **exclusion** and **severity mapping** rules into the O4 contract (decisions 2–3).
4. Record outcomes in **Notion Décisions** with status **`DECIDED`** before any Application or SPA drawer work.

---

## Four decisions (distinct)

| # | Topic | Output |
| :- | :---- | :----- |
| 1 | **Catalogue** | Which T2/T3/T4 facts become `GET /attention` items |
| 2 | **Exclusions** | Lifecycle phase, Running/Suspended rules, Overview dedup |
| 3 | **Priority / severity** | Meaning of `Severity` vs `semanticLevel` vs SPA tone — no implicit merge |
| 4 | **Line unit / dedup** | One row = one business fact vs one action vs one root cause |

**Blocker:** Decide **(4)** before designing any new Application collector or dedup rules.

---

## Current policy (unchanged until O4)

- **T5 only** on `GET /attention`: `InsufficientParticipants`, `DrawNoSolution`, `Qualification*`, `Progression*`.
- Construction denylist SPA (guardrail): [`attentionPartition.ts`](../../src/PlayUp/web/src/shell/attentionPartition.ts).
- Structure hub diagnostics (T2/T3/T4) → Structure PageHead / fiche / banner CTAs — **not** mirrored in the drawer today.

---

## Workshop proposals (submitted for validation — not DECIDED)

These rows capture recommended **discussion defaults** for the atelier. They do **not** change code or contract until validated in Notion Décisions.

| # | Topic | Proposal | Rationale |
| :- | :---- | :------- | :-------- |
| 1 | Catalogue | Do **not** bulk-import all T2/T3/T4 facts. Evaluate **each category / code** individually. | A drawer line must add **action or information** not already clear on Structure, Overview, or lifecycle. |
| 2 | Exclusions | Exclude situations already covered by Overview or lifecycle UX. Define **explicit** rules for **Running** and **Suspended** (names alone are insufficient). | Avoid duplicate triage and actions incompatible with competition state. |
| 3 | Severity | Keep **three** layers: Host `Severity`, product `semanticLevel`, drawer visual tone. | Contract severity ≠ felt urgency for organizers. |
| 4 | Line unit / dedup | Prefer **one line per actionable business problem**; group symptoms sharing the **same correction** when dedup rules allow. | Avoid N lines for one fix; do not hide independent actions. |

**Order:** arbitrate **(4)** first — it constrains collector shape and dedup.

---

## Decision 4 — Line unit and deduplication (priority proposal)

**Principle (to validate):** one drawer line = one **actionable business problem**, not a technical code, not a CTA label alone.

| Situation | Proposal (not DECIDED) |
| :-------- | :--------------------- |
| Several codes, **same** correction | **One grouped line** when action and organizer context are genuinely identical. |
| One code, **several phases** | Group **only** if the organizer can apply one shared correction; otherwise **one line per phase** (or explicit multi-target representation — TBD). |
| Several symptoms, **different** corrections | **Several lines**, even if symptoms share a technical root cause. |

**Dedup key:** the **business action** the organizer can take — not wire code, not copy alone, not an assumed root cause.

**Open design points (require DECIDED answers before collector design):**

- How to represent **multiple phases** on a single line (params, primary stage, plural labels).
- How to choose the **primary CTA** when grouping codes with the same intent but different deep links today on Structure.

---

## Decision 1 — Catalogue: initial family recommendations (drawer value — not inclusion)

Conservative stance: the drawer is **not** a mirror of the Structure banner. A family enters `GET /attention` only if the organizer gains **new information or action**, especially when triaging from **another page** (Matches, Teams, Overview).

These rows are **initial workshop recommendations**, not decisions to extend `/attention`.

| Problem family | Initial recommendation | Rationale |
| :------------- | :--------------------- | :-------- |
| `MissingStage`, `MissingStructure` | Keep creation paths + Overview SoT | Initial configuration, not runtime triage |
| `MissingPotRules` | Keep Structure SoT | Banner + navigation to draw section already contextualize action |
| `CupBracketInvalid` | Keep Structure SoT | Competition construction; dedicated surface |
| `StructureGraphInvalid` (aggregate) | **No extra drawer line** if detailed child issues are shown elsewhere | Avoid global blocker + causes duplication |
| `OverCapacityEdge`, `OverExtraction`, `ConfrontationUndecidable` | Keep Structure SoT by default; **individual** review if drawer adds cross-page action | Different contexts and corrections |
| `CycleDetected`, `RoutingNondeterminism`, missing qual/prog destinations | **No bulk import**; per-code review vs qual/prog/exits overlap | Related but not necessarily equivalent actions |
| `SlotFeedMissing`, `SlotFeedInvalid`, `SlotFeedMultiple` | Keep Structure SoT | Places and feed configuration |
| `PopulationShortfall`, `SourcePopulationShortfall`, `NonRootUnfed` | Review **together** without assuming merge | Same theme (promised/available teams) but corrections may differ |
| `AuthoredAwardUnreachable`, `ExpectedRankUnreachable` | Keep Structure SoT | Attribution / expected ranks; specific actions |

Map codes → family via [Annex A](#annex-a--candidate-facts-inventory-from-git-for-individual-o4-rows).

---

## Decisions 2–3 — Exclusions and severity (proposal)

### Exclusions (formalize in O4 contract — not inferred from code)

Product rules to write explicitly:

- Structure (and related) diagnostics while competition is **`Running`**
- Same while **`Suspended`**
- Facts already in **Overview** `attentionSummary` / situations (no second triage)
- **Aggregate vs detail** (e.g. `StructureGraphInvalid` vs child issue codes)

[Annex B](#annex-b--running--suspended-facts-for-exclusion-rules--not-decisions) lists **current code behaviour** as input only.

### Severity (keep three layers)

| Layer | Role |
| :---- | :--- |
| Host `Severity` | Wire / contract on `NeedsAttentionItemDto` |
| `semanticLevel` | Organizer-facing meaning (Gate / Signal / Attention / …) |
| SPA drawer tone | Presentation only |

Define an **explicit mapping** where needed; do **not** derive drawer colour or urgency directly from Host `Severity`.

---

## Per-candidate decision rule (checklist)

For each fact proposed for the drawer, answer:

1. What **concrete problem** does the organizer face?
2. Where is it **already visible**? (Structure banner/fiche, Overview, lifecycle, current drawer)
3. What **extra action or signal** does the drawer add?
4. Does it share a **root cause** with another candidate line (dedup under decision 4)?
5. In which **competition / stage statuses** is the line valid?

If the drawer adds neither new information nor useful action → keep SoT on Structure or Overview.

---

## What “6 problems” on Structure means (code — not your competition’s exact list)

The PageHead banner count is **`presentedProblemCount`** ([`structureReadinessSummary.ts`](../../src/PlayUp/web/src/pages/structure/structureReadinessSummary.ts)):

- Sum of **projected** `structureIssues[]` per stage (after SPA rules, e.g. `SlotFeedMissing` silenced while draw awaiting publish).
- **Plus** `+1` per phase with engaged draw and **missing pot rules** (`MissingPotRules` synthetic — E1/E6).
- **Plus** `+1` when readiness blocker **`CupBracketInvalid`** (competition-level shape).

So **6 ≠ automatically six distinct codes** — it can be several issues across phases plus pot gaps plus bracket.

**To fill the workshop table for a real competition:** export `GET /competitions/{id}/structure` and list, per stage, `structureIssues` + whether `MissingPotRules` / `CupBracketInvalid` apply. Match banner rows via [`collectAllStructureReadinessActionItems`](../../src/PlayUp/web/src/pages/structure/structureReadinessActions.ts) (CTA + deep link).

---

## Annex A — Candidate facts (inventory from Git, for individual O4 rows)

Use columns **Proposal** / **Decision (validated)** during the atelier. French labels below from [`structure.json`](../../src/PlayUp/web/src/i18n/locales/fr/structure.json) (`graph.issues.*` / readiness copy).

### A1 — Already T5 on `GET /attention` (reference — not O4 expansion by default)

| Source (wire) | FR label (short) | Typical SoT today | Drawer today |
| :------------ | :----------------- | :---------------- | :----------- |
| `InsufficientParticipants` | (Teams / enums) | Teams + Overview when Draft/Ready; **not** Structure count when phases exist | Yes (Draft/Ready only) |
| `DrawNoSolution` | Tirage sans solution | Structure draw fiche + toast | Yes |
| `QualificationPending` / `Conflict` | Qualification… | Structure / Apply flows | Yes |
| `ProgressionPending` / `Conflict` | Progression… | Structure / Match hub | Yes |

### A2 — Readiness blockers (competition / hub — mostly **not** in `presentedProblemCount` per issue code)

| Code | Role | Structure UX | In Overview `attentionSummary` today |
| :--- | :--- | :----------- | :------------------------------------- |
| `MissingStage` | Birth | Empty state | No (excluded) |
| `MissingStructure` | Birth | Empty / configure | No |
| `MissingPotRules` | Groups + draw engaged | Banner synthetic row → tirage section | No |
| `CupBracketInvalid` | Cup shape | Banner + construction | No |
| `StructureGraphInvalid` | Prepare gate aggregate | Lifecycle + issues children | No |
| `ConfrontationUndecidable` | Prepare gate | Fiche confrontation | No |
| `OverCapacityEdge` | Prepare gate | Capacity / graph | No |
| `OverExtraction` | Prepare gate | Exits / qual | No |

### A3 — Stage `structureIssues[]` (T2/T3/T4 — usual content of the “N problems” banner)

| Code | FR label (graph.issues) | CTA family (SPA) | Tile |
| :--- | :---------------------- | :--------------- | :--- |
| `CycleDetected` | Liaison circulaire entre phases | sorties | exits |
| `RoutingNondeterminism` | Même sélection → plusieurs destinations | qualification | exits |
| `DanglingQualificationTarget` | Destination de qualification introuvable | qualification | exits |
| `DanglingProgressionTarget` | Destination de progression introuvable | progression | exits |
| `MissingQualificationDestinationSlot` | Place de qualification manquante | qualification | exits |
| `MissingQualificationDestinationGroup` | Groupe de qualification manquant | qualification | exits |
| `MissingProgressionDestinationSlot` | Place de progression manquante | progression | exits |
| `MissingProgressionDestinationGroup` | Groupe de progression manquant | progression | exits |
| `SlotFeedMissing` | Place pas encore alimentée | places | places |
| `SlotFeedInvalid` | Plusieurs sources du même type… | places | places |
| `SlotFeedMultiple` | Plusieurs sources différentes… | places | places |
| `NonRootUnfed` | Aucune équipe n’alimente encore cette phase | affectation | capacity |
| `OverCapacityEdge` | Plus d’équipes promises que de places | structure | capacity |
| `OverExtraction` | Sortie demande plus d’équipes que… | qualification | exits |
| `PopulationShortfall` | Pas assez d’équipes promises pour cette phase | affectation | capacity |
| `SourcePopulationShortfall` | Pas assez d’équipes dans la phase source | affectation | capacity |
| `ConfrontationUndecidable` | Confrontation qui ne peut pas toujours départager | tieFormat | confrontation |
| `AuthoredAwardUnreachable` | Paire source introuvable | attribution | attribution |
| `ExpectedRankUnreachable` | Rang ne peut pas être produit… | attribution | attribution |

CTA navigation: [`resolveStructureDiagnosticNavigation`](../../src/PlayUp/web/src/pages/structure/structureReadinessActions.ts) → deep link Structure (`section`, `focus`, dialog).

### A4 — Per-code rows (generic **Proposal** from family table — **Decision** empty until atelier)

Fill columns (1)–(5) in the atelier; **Decision** only after Notion `DECIDED`. For exercise **B**, add a companion sheet keyed by your export’s actual codes and phases.

| Code | Family default (Proposal) | Decision |
| :--- | :------------------------ | :------- |
| `MissingStage` | Keep Overview / birth SoT — not drawer | |
| `MissingStructure` | Keep Overview / birth SoT — not drawer | |
| `MissingPotRules` | Keep Structure SoT — not drawer | |
| `CupBracketInvalid` | Keep Structure SoT — not drawer | |
| `StructureGraphInvalid` | No aggregate drawer line; detail elsewhere | |
| `ConfrontationUndecidable` | Keep Structure; individual drawer review only | |
| `OverCapacityEdge` | Keep Structure; individual drawer review only | |
| `OverExtraction` | Keep Structure; individual drawer review only | |
| `CycleDetected` | No bulk import — per-code review | |
| `RoutingNondeterminism` | No bulk import — per-code review | |
| `DanglingQualificationTarget` | No bulk import — per-code review | |
| `DanglingProgressionTarget` | No bulk import — per-code review | |
| `MissingQualificationDestinationSlot` | No bulk import — per-code review | |
| `MissingQualificationDestinationGroup` | No bulk import — per-code review | |
| `MissingProgressionDestinationSlot` | No bulk import — per-code review | |
| `MissingProgressionDestinationGroup` | No bulk import — per-code review | |
| `SlotFeedMissing` | Keep Structure SoT — not drawer | |
| `SlotFeedInvalid` | Keep Structure SoT — not drawer | |
| `SlotFeedMultiple` | Keep Structure SoT — not drawer | |
| `NonRootUnfed` | Review with population family — not presuming merge | |
| `PopulationShortfall` | Review with population family — not presuming merge | |
| `SourcePopulationShortfall` | Review with population family — not presuming merge | |
| `AuthoredAwardUnreachable` | Keep Structure SoT — not drawer | |
| `ExpectedRankUnreachable` | Keep Structure SoT — not drawer | |

Extended template (optional per row):

| Code | User problem (1) | Already visible (2) | Drawer adds (3) | Dedup group (4) | Phase/competition rules (5) | Proposal | Decision |
| :--- | :----------------- | :------------------ | :-------------- | :-------------- | :-------------------------- | :------- | :------- |

---

## Annex B — Running / Suspended (facts for exclusion rules — not decisions)

Document exact product rules in O4 outcome; below is **code behaviour today** (input to discussion):

| Topic | Behaviour today |
| :---- | :-------------- |
| `InsufficientParticipants` | Emitted on `/attention` only when competition is **Draft** or **Ready** ([`NeedsAttentionAssembler`](../../src/PlayUp/MyClub.PlayUp.Application/Reads/NeedsAttentionAssembler.cs)). |
| Qual / prog / draw attentions | Evaluated from domain state; **not** gated on Running vs Ready in the same way as insufficient participants. |
| Overview `CompetitionSuspended` | Informational **situation** on Overview; **not** in `attentionSummary` / `/attention` ensemble ([Host test `Get_overview_suspended_…`](../../tests/MyClub.PlayUp.Host.Tests/ReadSurfaceEndpointTests.cs)). |
| Structure diagnostics | Shown from Structure read regardless of Running/Suspended unless UX hides sections (verify per surface in atelier). |

---

## If DECIDED — implementation checklist

- Notion Décision row + update [pilot aliases](./organizer-problems-pilot-aliases.md)
- `NeedsAttentionAssembler` extension (or dedicated projector) with SoT comments
- Host `/attention` contract tests (parity with Overview `attentionSummary` policy)
- SPA partition/denylist mirror — not guardrail-only
- Revisit [`functionalProblemPresentation.ts`](../../src/PlayUp/web/src/shell/functionalProblemPresentation.ts) canal rules

## Default if no decision

Drawer stays **T5**; construction remains Structure / Overview SoT.
