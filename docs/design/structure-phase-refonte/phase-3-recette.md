# Structure phase fiche — Phase 3 recette

Scope: **aside** (Matchs, Flux), **règlement** (bande digest), preuves automatisées + checklist manuelle.

Authority: [actions-permissions-inventory](./actions-permissions-inventory.md), [truth-sources](./truth-sources.md).

**Phase 3 status:** **closed** (2026-10-18). Sign-off : [phase-3-recette-manual-execution.md](./phase-3-recette-manual-execution.md) §7. `typecheck` / Playwright E2E : hors périmètre de cette phase (piste technique séparée).

---

## Automated coverage (Git)

| Area | Test file(s) |
|:-----|:-------------|
| Aside Matchs visibility | `structureMatchsSectionUi.test.ts` |
| Matchs aside O-MAT-VIS-1 recette (SPA render) | `phase3MatchsAsideRecette.test.tsx` |
| Operational strip / materialize | `StructurePhaseOperationalStrip.test.tsx` |
| Lifecycle chrome | `StructurePhaseMatchsLifecycleBlock.test.tsx`, `StructurePhaseDraftMatchsPrepare.test.tsx` |
| Regulation footband | `StructurePhaseRegulationStrip.test.tsx` |
| Hub integration | `StructurePage.test.tsx` |

- **Aside Matchs visibility** — `resolveAsideMatchsStripContent`, `shouldShowAsideMatchsTile`.
- **Matchs aside recette** — component render of aside strip; **not** a browser/DevRunner substitute.
- **Hub integration** — lifecycle header, cross-surface behaviour.

Do not replace these with empty snapshots; extend when adding behaviour.

---

## Manual checklist (non-régression)

**Browser execution sheet (24 controls A–E, B-SUS, parity, gap journal):** [phase-3-recette-manual-execution.md](./phase-3-recette-manual-execution.md). Use it for manual sign-off; Vitest results in this file are replay evidence only.

### Fiche header

Browser evidence : [execution sheet §3 A](./phase-3-recette-manual-execution.md).

- [x] Draft: Prepare in header toolbar; **no** aside Matchs tile.
- [x] Ready/Running: Start / Complete / Suspend / Resume labels and disabled tooltips match pre-refonte.
- [x] Block messages and remediation links under the title row.

### Aside — Matchs (non-Draft)

Authority: **O-MAT-VIS-1** (Notion Matrice UI + `structure-phase-lifecycle-ui.md` § Matchs aside tile). Tile only when useful content is **rendered**; lifecycle stays on fiche header.

#### 2026-10-17 — Matchs aside recette pass

**What this pass is**

| Layer | Replayed | Role |
|:------|:---------|:-----|
| Vitest `phase3MatchsAsideRecette.test.tsx` (8 cases) | Yes | Aside strip DOM + one header case |
| Vitest `structureMatchsSectionUi.test.ts` + `StructurePhaseOperationalStrip.test.tsx` | Yes | Tile visibility + strip production rules |
| .NET `Qual_form_champ_*` (Development.Tests, 3 tests) | Yes | Hub read model (partial / full / running seeds) |
| DevRunner + browser (Structure fiche E2E) | Yes | [Manual execution sheet](./phase-3-recette-manual-execution.md) (2026-10-18) |

**Reproducibility (Vitest)** — not a browser substitute:

```bash
cd src/PlayUp/web
npm run test -- --run src/pages/structure/phase3MatchsAsideRecette.test.tsx
npm run test -- --run src/pages/structure/structureMatchsSectionUi.test.ts src/pages/structure/StructurePhaseOperationalStrip.test.tsx src/pages/structure/phase3MatchsAsideRecette.test.tsx
```

(Verified 2026-10-17: 8/8 isolated; **48/48** with the two related structure test files.)

#### 2026-10-18 — Automated replay (session evidence)

| Suite | Replayed | Result |
|:------|:---------|:-------|
| `phase3MatchsAsideRecette.test.tsx` | Yes | 8/8 |
| Matchs bundle (3 files, see 2026-10-17 commands) | Yes | 50/50 |
| Lifecycle + Draft + Regulation + `StructurePage` (4 files) | Yes | 54/54 |
| All seven Vitest files (see [Automated coverage](#automated-coverage-git)) | Yes | 105/105 (closure replay: **106/106**, see below) |
| .NET `Qual_form_champ_*` | Yes | 3/3 (`--no-build` if SDK file lock) |
| DevRunner + browser | Yes | [Manual execution sheet](./phase-3-recette-manual-execution.md) — A–E + B-SUS ; B6 sur `swiss-ready` |
| Playwright smoke (`npm run test:e2e`) | No | Not run (see note below) |

- **Matchs bundle count:** 50/50 on 2026-10-18 vs 48/48 on 2026-10-17 — added tests, not a failure.
- **E2E:** not run because app `npm run typecheck` fails on the working tree (`tsconfig.app`). Vitest uses `tsconfig.vitest` and passed. Unblock build/smoke in a dedicated PR when needed.

```bash
cd src/PlayUp/web
npm run test:run -- src/pages/structure/phase3MatchsAsideRecette.test.tsx
npm run test:run -- src/pages/structure/structureMatchsSectionUi.test.ts src/pages/structure/StructurePhaseOperationalStrip.test.tsx src/pages/structure/phase3MatchsAsideRecette.test.tsx
npm run test:run -- src/pages/structure/StructurePhaseMatchsLifecycleBlock.test.tsx src/pages/structure/StructurePhaseDraftMatchsPrepare.test.tsx src/pages/structure/StructurePhaseRegulationStrip.test.tsx src/pages/structure/StructurePage.test.tsx
# Or single invocation:
npm run test:run -- src/pages/structure/structureMatchsSectionUi.test.ts src/pages/structure/StructurePhaseOperationalStrip.test.tsx src/pages/structure/phase3MatchsAsideRecette.test.tsx src/pages/structure/StructurePhaseMatchsLifecycleBlock.test.tsx src/pages/structure/StructurePhaseDraftMatchsPrepare.test.tsx src/pages/structure/StructurePhaseRegulationStrip.test.tsx src/pages/structure/StructurePage.test.tsx
```

```bash
dotnet test tests/MyClub.PlayUp.Development.Tests/MyClub.PlayUp.Development.Tests.csproj --filter "FullyQualifiedName~Qual_form_champ"
```

#### 2026-10-18 — Clôture replay (gate technique)

| Suite | Commit `8873a491` | Result |
|:------|:------------------|:-------|
| Seven Vitest files (commands above) | Yes | **106/106** |
| .NET `Qual_form_champ_*` (`--no-build` if file lock) | Yes | **3/3** |

Manual recette : [execution sheet](./phase-3-recette-manual-execution.md) (A–E, B-SUS, parité §6).

**O-MAT-IMPL-1 — tile visibility (closed in SPA)**

Fiche-level tile visibility now follows **`resolveAsideMatchsStripContent`** / `shouldShowAsideMatchsTile(stage, presentation)` (shared with `MatchesColumn` aside rendering). Strip **production** rules unchanged; composition layout still uses `shouldShowMatchsSection`.

| Case | Scenario | Expected | Observed (Vitest) |
|:-----|:---------|:---------|:------------------|
| 3 | Champ Ready, placement signal, no `materialize` | Hide tile | Hidden |
| 5 | Swiss Ready (`swiss-ready`) | Hide tile (2a) | Hidden |
| 6 | Ready, lifecycle only, no production body | Hide tile | Hidden |

**Checklist items touched by this pass**

- [x] Case 1 (Draft): tile hidden; Prepare on header — Vitest.
- [x] Cases 2, 4, 7, 8: production / progression in strip; no lifecycle CTAs in aside — Vitest.
- [x] Cases 3, 5, 6: empty aside shell no longer shown at fiche — Vitest (`shouldShowAsideMatchsTile` + strip render).
- [x] Case 7 **Suspended** — navigateur B-SUS (`ctrl-o3-mat-recette` S3) ; Vitest strip : [ctrl-o3-mat-devrunner-recette.md](../structure-lifecycle/ctrl-o3-mat-devrunner-recette.md).
- [x] « Voir les matchs » link (Completed toolbar) — navigateur `d81b6474` ; unit : `structureMatchsSectionUi.test.ts`.
- [x] Full browser/DevRunner pass — **2026-10-18** ([execution sheet](./phase-3-recette-manual-execution.md)).

### Aside — Flux

Browser evidence : [execution sheet §3 C](./phase-3-recette-manual-execution.md).

- [x] Sorties: phase heads + rule labels aligned with Qualifiés (no volume chip).
- [x] Attribution: rank medal + qualifier-style rule line (no outcome chips).
- [x] Empty Sorties / Attribution: `EmptyState` **compact** + small icon; low vertical footprint (C4 N/A documenté).

### Règlement

Browser evidence : [execution sheet §3 D](./phase-3-recette-manual-execution.md).

- [x] Digest strip: match / standing / confrontation (when visible) — layout per spec.
- [x] Omit facts that do not apply (e.g. prolongation / TAB when off).
- [x] Binding badges (général / personnalisé) + edit icons in section toolbar.
- [x] Regulation link in footband (libellé UI « Règlement » — réserve documentée §7).
- [x] Narrow width: reflow + gradient separator (D6).

### Schematic

- [x] No dashed outer frame on schematic tile — [execution sheet E1](./phase-3-recette-manual-execution.md).

### Parity table

Remplie — [execution sheet § 6](./phase-3-recette-manual-execution.md#6-table-de-parité). Inventaire : [actions-permissions-inventory.md](./actions-permissions-inventory.md#phase-3-parity-table-template).
