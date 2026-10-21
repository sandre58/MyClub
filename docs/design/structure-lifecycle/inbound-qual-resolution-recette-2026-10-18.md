# Recette — « Non résolue » après qualification terminée (lot E)

**Status:** **Shipped** (2026-10-18). Read-model + apply witness fix; SPA copy unchanged.

**Goal:** Exploitation / historical Composition band shows **resolved** team names when upstream qual phase is **Completed**, rankings exist, and qualification has been **applied** to the model feeding inbound seats.

**Related code:** `StructureInboundCompositionAssembler.cs` · `ApplyQualification.cs` · `FormPathResolutionKey.cs` · SPA `compositionTeamsInboundRows.ts`.

---

## Root cause (classified)

| Layer | Finding |
|-------|---------|
| **Data / apply** | `ApplyQualification` recorded `FormPathResolution` only for **ForForm** destinations, not **population** targets. After apply, composition was filled but inbound seats had no per-path witness. |
| **Assembler** | `ResolveQualificationSeat` looked up `FormPathResolution` only for `TargetsForm`, so population paths always returned `null` → SPA showed « Non résolue » despite applied qual. |
| **SPA** | No change required when hub `inboundComposition.rules[].seats[].entryId` is populated (`mapSeat` → `resolved: true`). |

**Fix:** Record path fingerprint on apply for `TargetsPopulation` and `TargetsForm`; resolve inbound seats via `FormPathResolutionKey.FromQualification` for both; relax key guard to allow population destinations.

---

## Scenarios

| Id | Arrange | Inspect | Pass criteria | Automated |
|----|---------|---------|---------------|-----------|
| **E-A** | Dest **Running**, qual applied, ForForm paths | `inboundComposition` seats | `entryId` + display names | `StructureInboundCompositionAssemblerTests.Assemble_resolves_for_form_seats_after_apply_qualification` |
| **E-A′** | Population (non-ForForm) qual applied | Same | Resolved seats | `Assemble_resolves_population_target_seats_from_composition_after_apply` |
| **E-B** | Qual **not applied** | Attention vs Structure | Pending / unresolved consistent | Manual / existing attention tests |
| **E-C** | `TargetsGroup` | Assembler | Stays unresolved until group merge (by design) | Documented — no change |
| **E-D** | `entryId` in seat, missing from `participants.entries` | SPA rows | Unresolved until entry in payload | `compositionTeamsInboundRows` branch (unchanged) |
| **E-E** | Historical mode | Parity with exploitation | Same hub contract | Manual DevRunner |

---

## Investigation checklist (closed)

1. Diff `inboundComposition` vs composition — **aligned after apply witness**.
2. Source stage status + apply events — **use `FormPathResolutions` count**, not standings DTO alone.
3. `listCompositionTeamRows` — **resolved when `hubSeats[].resolved`**.
