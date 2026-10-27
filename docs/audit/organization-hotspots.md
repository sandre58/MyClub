# Organisation hotspots

Short map of where a flat directory is a navigation problem. Counts are **direct** files (not recursive) at tip `28bfa4a597cd6357f0fe262e94d5f9afbb82ab56`, excluding `node_modules`, `bin`, and `obj`. Play’Up web at that tip: 617 files.

- **A** — a targeted move or a file split improves navigation.
- **B** — natural volume, or debt already accepted.
- **C** — generated output or assets.

## Map (pre-move)

| Hotspot | Direct files | Class | Reading |
| --- | ---: | --- | --- |
| `src/PlayUp/web/src/pages/structure/` | 218 | A | Only flat directory where thematic folders match the product. |
| `MyClub.PlayUp.Application/Reads/` | 83 | A | Subgroups are possible. Import churn for a moderate gain — deferred. |
| Large SPA files (structure / overview) | 1.3k–2.1k lines | A | File splits (F-02), separate from folder moves. |
| `docs/design/structure-lifecycle/` | 70 | B | Design history. |
| `web/src/design-system/components/` | 63 | B | Component catalogue. |
| `tests/.../Application.Tests/Stages/` | 62 | B | Mirrors stage use cases. |
| `docs/audit/` | 57 | B | Audit archive. |
| `tests/.../Host.Tests/` | 52 | B | Host test surface. |
| `Host/Contracts/` | 50 | B | One request file per endpoint. |
| `Development/Scenarios/` | 47 | B | Seed scenarios. |
| `web/src/shell/` | 47 | B | Shell catalogue. |
| Structure fixtures and helpers | — | B | Colocalized (phase 6 S5). Not under `src/test/`. |
| Domain `Stages/` and `Stage.cs` (3400 lines) | — | B | Folders already exist. The aggregate file is F-01. |
| `Application/Stages/` | — | B | Use-case folders already. |
| Migrations, package-lock, seed logos | — | C | Generated or assets. |

Large files at the same tip (signal only): `ScenarioOrchestration.cs` 3723, `Stage.cs` 3400, `StructurePage.test.tsx` 2170, `OverviewAssembler.cs` 2062, `StructurePhaseOperationalStrip.tsx` 1735, `CompetitionOverviewPage.tsx` 1725, `StructurePhaseFiche.tsx` 1524, `StructureViewAssembler.cs` 1494, `phaseSchematic.tsx` 1381, `UseCaseExecutor.Stages.cs` 1130, `structure.json` (fr and en) 1155 each.

## This lot

Flat files under `pages/structure/` moved into thematic folders. Import paths only: no symbol renames, no component or CSS rewrites. Stylesheets stay at `structure/*.css`. `__fixtures__/` is unchanged. The existing `problems/` module (mutation messages and Matchs column visibility) stays; new `issues/` is diagnostics, readiness, and presented codes.

Page shell stays at the folder root: `StructurePage*`, page-head readiness, navigation, hub section ids, invalidation, and query cache.

| Folder | Files |
| --- | ---: |
| `structure/` (root) | 24 |
| `composition/` | 39 |
| `issues/` | 31 |
| `fiche/` | 28 |
| `schematic/` | 28 |
| `intents/` | 27 |
| `draw/` | 18 |
| `matchs/` | 17 |
| `problems/` | 10 |
| `regulation/` | 6 |

## Still open

- F-02 — split `StructurePhaseOperationalStrip`, `StructurePhaseFiche`, `phaseSchematic`, `CompetitionOverviewPage`.
- `Application/Reads/` subgroups.
- F-01 — `Stage.cs`, `OverviewAssembler`, `StructureViewAssembler`, `UseCaseExecutor.Stages`.
- S5 — Structure fixtures stay colocalized under `pages/structure/`.
- Host/Contracts, design-system components, shell, `docs/design` — unchanged.
