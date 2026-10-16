# Cup materialize — recette protocol (hub + SPA)

**Status:** Reference gate — automated integration + manual SPA checklist (2026-10-17).  
**Scope:** Cup `MaterializeFromOccupiedSlots` read model, command gates, and Structure **Aside Matchs** production surface.

## Product contracts (frozen for this gate)

| Layer | Contract |
|-------|----------|
| **Case** | Provenance + optional resolved participant; does not guarantee production. |
| **Fil (P1 / blue)** | Structural eligibility from hub `materialize.confrontations` (`determined` / `eligible` / `materialized`); not a promise that G-MAT1/2 pass at execute time. |
| **CTA** | `materialize.commandEnabled`; success = HTTP + domain effect. |
| **Gauge** | `materializedCount` / `determinedCount` count **confrontations**, not necessarily calendar **matches** (e.g. two legs → `materializedCount` +1, `createdCount` +2). |

## Automated evidence

**Project:** `tests/MyClub.PlayUp.Development.Tests/CupMaterializeRecetteProtocolTests.cs`  
**Collection:** `DevelopmentPostgres` (Testcontainers).

| ID | What it proves |
|----|----------------|
| **R1** | Undetermined rows when slots lack `EntryId` (`structure-materialize-recette` C1). |
| **R2** | Eligible pair materializes; SingleLeg recette → one created match. |
| **R3** | Idempotence: second call `alreadyComplete`, no new matches (C3). |
| **R5a** | Partial materialize when P1 award unreachable (G-MAT2) — **preserve** behaviour. |
| **R5b** | Harness: `AttributionUnreachable` on targeted FINAL. |
| **R6** | `commandEnabled` false + `disabledReason` `DrawObsoleteForConsumption`; command throws same code. |
| **R7** | Seed `cup-qf-sf`: semi slots have `EntryId`; schematic feed Progression (not React primary label). |
| **R8** | Groups Running → `StructureNotMutable`, not Cup fil. |
| **R4** | **Snapshot only:** Dev seed `invariants-exec-undecidable` does **not** reproduce documented EXEC-001 Prepare gate — track separately ([g-mat1](./g-mat1-confrontation-undecidable-at-materialize.md) § EXEC-001). |

**HTTP (related, not duplicated in Cup recette tests):** `CompetitionSlice3EndpointTests.Cup_slot_draw_apply_and_materialize_creates_matchesAsync` — `POST …/matches/materialize-from-slots` → 200.

```bash
dotnet test tests/MyClub.PlayUp.Development.Tests --filter "FullyQualifiedName~CupMaterializeRecetteProtocolTests"
```

## DevRunner seeds

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios structure-materialize-recette,cup-qf-sf
```

| Seed | Use |
|------|-----|
| `structure-materialize-recette` | RECETTE C1–C3, G*, R* materialize hub states |
| `cup-qf-sf` | R7 — semi phase with progression-fed occupied slots |
| `regulation-tie-homogeneous` | Two-leg format labels (SPA jauge / copy spot-check) |

## Manual SPA checklist

**Prerequisites:** Host `http://localhost:5287`, `npm run dev` in `src/PlayUp/web`, workspace DB seeded.

Deep-link shape: `/competitions/{competitionId}/structure?stage={stageId}&section=matchs`  
Organizer surface for production on **Ready** Cup: **Aside Matchs** (`#aside-matchs-heading`, strip `data-layout-variant="aside-matchs"`). Draft hides this tile — do not sign off R6 on Draft layout alone.

### R6 — P0 (obsolete Cup draw)

Conclusive only if all three align:

1. **Data:** `materialize.commandEnabled === false`, `materialize.disabledReason === "DrawObsoleteForConsumption"`.
2. **Presentation:** Aside **Matchs** states production is blocked with the draw-obsolete copy (`fiche.materialize.disabledReason.DrawObsoleteForConsumption` / EN equivalent).
3. **Action:** No enabled **Create / Complete matches** control in that cluster.

**API recipe (local QA):** Cup bracket 4 → slot draw **published, not applied** → shrink affectation pool → manual slot assignment → `prepare` → verify hub then SPA.

**2026-10-17 pass:** Conform on Aside Matchs (message present, no materialize CTA). Same copy may also appear on Places / Qualified strips.

### R7 — P1 (resolved participant vs primary label)

Open `cup-qf-sf` → **Demi-finale**. Case primary = **resolved team**; Qual/Prog path in **tooltip** ([R7](./r7-schematic-case-primary-arbitration.md) **Accepted**).

### Jauge — copy

On partial production (e.g. RECETTE C2, Ready, single leg): gauge `materializedCount/determinedCount` + “match(s) left to create” — aligned when one confrontation remains.

On **two legs**, verify separately: confrontations determined / materialized vs match legs created. If copy says “matches” while the gauge counts confrontations, prefer **i18n copy** fix only (no hub calculation change) once confirmed in UI.

## Exit criteria

| Outcome | Next step |
|---------|-----------|
| R6 SPA conform | Keep this doc + integration tests; PR when ready. |
| R6 SPA gap | Classify: Host read model vs SPA binding (`classifyMaterializePresentation`, `materializeDisabledCaption`) vs wrong surface. |
| R7 | Product decision before code. |
| R4 / EXEC-001 | Separate QA seed vs doc task — not a read-model fix from R4 alone. |
