# Structure diagnostics — presentation contract (signed)

**Status:** Product contract signed (presentation only).  
**Scope:** Structure hub diagnostics (`structureIssues`), related attention items, mutation copy on structure surfaces.  
**Out of scope:** §11B authorization gates (Prepare, Materialize, Start, G-OPS, G-MAT); sport Overview; live matches; teams (except echoes).  
**Annexes:**  
- Step 1 audit — [structure-diagnostic-presentation-technical-audit.md](./structure-diagnostic-presentation-technical-audit.md)  
- **Step 2 (signed)** — [structure-diagnostic-presentation-impact-rules.md](./structure-diagnostic-presentation-impact-rules.md)

---

## 1. Decision model

### 1.1 Three independent dimensions

| Dimension | Responsibility | Source of truth |
|-----------|----------------|-----------------|
| **Diagnostic** | Describes a structural or operational problem | Existing analyzer and domain rules |
| **Presentation** | Visibility, severity, tone, message | Common product policy (this contract) |
| **Authorization** | Whether an operation is allowed | Existing business gates and server |

Presentation may change with phase status **without** changing server authorization.

### 1.2 Contract values

| Severity | UI tone | Meaning |
|----------|---------|---------|
| `hidden` | — | Not shown in the current context |
| `warning` | `attention` | Monitor; no established blocking impact |
| `warning` | `progress` | Expected incompleteness during construction |
| `blocking` | `danger` | Priority fix or established exploitation impact per applicable rule |

`blocking` / `danger` alone **never** imply server authorization.

### 1.3 Signed arbitration (summary)

1. **AuthoredAwardUnreachable** — keep D5 / G-UI1: `danger` on Cup/KO; `attention` on classifying formats.
2. **Promotion to danger** — only when an explicit, testable business rule establishes exploitation impact; unknown impact stays `warning`.
3. **PopulationShortfall** / **SourcePopulationShortfall** — visual `danger` in **Ready**; server gates unchanged.
4. **NonRootUnfed** — `progress` in **Draft** only when expected incompleteness is established from reliable signals; otherwise `attention`.
5. **Surfaces** — fiche, attention drawer, topology, and related UI use the same presentation policy.

---

## 2. Reference matrix (presentation)

Not a gate specification. Status columns: **Draft**, **Ready**, **Running**.

| Diagnostic | Draft | Ready | Running |
|------------|-------|-------|---------|
| `CycleDetected` | danger | danger | danger |
| `RoutingNondeterminism` | danger | danger | danger |
| `OverExtraction` | danger | danger | danger |
| `Dangling*` / `Missing*` | danger | danger | danger |
| `SlotFeedMissing` | danger¹ | danger¹ | danger¹ |
| `OverCapacityEdge` | attention | attention² | attention² |
| `PopulationShortfall` | attention | **danger (visual)** | attention² |
| `SourcePopulationShortfall` | attention | **danger (visual)** | attention² |
| `NonRootUnfed` | progress³ or attention | attention / danger² | attention / danger² |
| `AuthoredAwardUnreachable` — Cup/KO | danger | danger | danger |
| `AuthoredAwardUnreachable` — classifying | attention | attention | attention |
| `ExpectedRankUnreachable` | attention | attention / danger² | attention / danger² |
| `ConfrontationUndecidable` | danger | danger | danger |

¹ Keep existing **hide** rules when engaged draw makes the diagnostic obsolete (e.g. `SlotFeedMissing` awaiting draw).  
² `danger` only if a defined, testable exploitation-impact rule applies; otherwise stay `attention`.  
³ `progress` only when expected incompleteness is established; otherwise `attention`.

### Reading rules

- Phase status does **not** auto-promote all warnings.
- Per-diagnostic policy overrides generic status rules.
- Unlisted diagnostics keep current behaviour until explicitly classified; no invented business severity in fallbacks.
- Obsolete diagnostics are **hidden** by validated contextual rules, not by visual approximation.

### Diagnostics not in the matrix

Until classified, keep current presentation (typically blocking / danger): e.g. `SlotFeedInvalid`, `SlotFeedMissing` siblings, readiness blockers (`CupBracketInvalid`, `StructureGraphInvalid`, …), attention runtime sources (`QualificationPending`, …).

---

## 3. Technical contract (target)

### 3.1 Single entry point

SPA SoT: extend/replace `structureDiagnosticSeverity.ts` with a shared **presentation** function; Host mirrors with parity tests.

Conceptual signature:

```text
structureDiagnosticPresentation(code, context) → {
  severity: hidden | warning | blocking,
  tone: progress | attention | danger | null,
  titleKeyShort,
  titleKeyLong,
}
```

**Context (minimum):**

- `formatKind`
- `stageStatus`: `Draft` | `Ready` | `Running` (map `Suspended` / `Completed` per audit)
- `incompletenessExpected` when known (NonRootUnfed progress)
- `operationalImpact`: established | absent | unknown (footnote ² promotions)
- Masking inputs (draw engaged, presented projection, U1 synthesis, etc.)

### 3.2 Impact policy

Promotion to `blocking` requires: identified diagnostic, defined impact rule, reliable evaluation data, reference test.  
**Unknown impact ⇒ warning.**  
Do not infer impact from Ready/Running alone.

### 3.3 Host / SPA parity

Same severity for equivalent context in `StructurePresentedAttentionCollector` and `functionalProblemPresentation` (must receive `formatKind` and `stageStatus` when required).

---

## 4. UI surfaces

| Surface | Expected behaviour |
|---------|-------------------|
| Fiche diagnostics | Policy tone + copy |
| Attention drawer | Same classification and editorial keys as fiche (short variant) |
| Topology | Worst **presented** severity, not always red |
| PageHead | Negative signal when a presented diagnostic is `danger`; generic copy OK |
| Capacity gauge | Tone aligned with population shortfall diagnostic |
| Match progress gauge | Unchanged (operational progression) |
| Prepare / Materialize / Start | Existing gates; messages explain refusals |
| Mutations | Localized actionable copy; no raw HTTP or wire codes |

---

## 5. Editorial contract

One catalogue; per diagnostic **short** (badge, drawer) and **long** (fiche, detail).  
No wire codes, raw i18n keys, or internal type names as user-visible fallbacks.  
Mutations: map used `Application.*` codes; drop organizer-facing HTTP suffixes (e.g. `(403)`).

---

## 6. Acceptance checklist (recipe)

Use for manual QA and automated coverage planning (21 items in signed French checklist: policy A, surfaces B, copy C, non-regression D).  
Checking boxes in a tool is not proof of test execution.

---

## 7. Protections

Implementation must **not**: change §11B gates; rewrite readiness to match colours; refactor Overview/sport/teams DS globally; promote to danger without testable impact; hide structural anomalies solely because phase is Draft.

---

## 8. Recommended implementation order

1. Technical audit — codes, context, masking (audit doc).  
2. Formalize exploitation-impact rules + tests for footnote ² rows.  
3. Shared presentation + Host/SPA parity tests.  
4. Wire UI surfaces.  
5. Unify copy + run recipe checklist.
