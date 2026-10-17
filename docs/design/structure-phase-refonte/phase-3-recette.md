# Structure phase fiche — Phase 3 recette

Scope: **aside** (Matchs, Flux), **règlement** (bande digest), preuves automatisées + checklist manuelle.

Authority: [actions-permissions-inventory](./actions-permissions-inventory.md), [truth-sources](./truth-sources.md).

---

## Automated coverage (Git)

| Area | Tests |
|------|--------|
| Aside Matchs visibility | `structureMatchsSectionUi.test.ts` (`shouldShowAsideMatchsTile`) |
| Matchs aside O-MAT-VIS-1 recette (SPA render) | `phase3MatchsAsideRecette.test.tsx` — **not** a browser/DevRunner substitute |
| Operational strip / materialize | `StructurePhaseOperationalStrip.test.tsx` |
| Lifecycle chrome | `StructurePhaseMatchsLifecycleBlock.test.tsx`, `StructurePhaseDraftMatchsPrepare.test.tsx` |
| Regulation footband | `StructurePhaseRegulationStrip.test.tsx` |
| Hub integration | `StructurePage.test.tsx` (lifecycle header, cross-surface) |

Do not replace these with empty snapshots; extend when adding behaviour.

---

## Manual checklist (non-régression)

### Fiche header

- [ ] Draft: Prepare in header toolbar; **no** aside Matchs tile.
- [ ] Ready/Running: Start / Complete / Suspend / Resume labels and disabled tooltips match pre-refonte.
- [ ] Block messages and remediation links under the title row.

### Aside — Matchs (non-Draft)

Authority: **O-MAT-VIS-1** (Notion Matrice UI + `structure-phase-lifecycle-ui.md` § Matchs aside tile). Tile only when useful content is **rendered**; lifecycle stays on fiche header.

#### 2026-10-17 — Matchs aside recette pass (working tree; not committed)

**What this pass is**

| Layer | Replayed in this pass? | Role |
|-------|------------------------|------|
| Vitest `phase3MatchsAsideRecette.test.tsx` (8 cases) | **Yes** | SPA **component render** of aside strip + one header case; verdict on **DOM presented**, not `shouldShowAsideMatchsTile` alone |
| Vitest `StructurePhaseOperationalStrip.test.tsx` (Suspended / materialize) | **No** (existing suite; not re-run as part of sign-off narrative) | Prior coverage for production rules on strip |
| .NET `Qual_form_champ_*` Development.Tests | **Yes** (3 tests) | Hub read model for seeds partial / full / running |
| DevRunner + browser (Structure fiche end-to-end) | **No** | **Distinct** manual pass — still required for full Phase 3 sign-off |

**Reproducibility (Vitest)**

```bash
cd src/PlayUp/web
npm run test -- --run src/pages/structure/phase3MatchsAsideRecette.test.tsx
npm run test -- --run src/pages/structure/structureMatchsSectionUi.test.ts src/pages/structure/StructurePhaseOperationalStrip.test.tsx src/pages/structure/phase3MatchsAsideRecette.test.tsx
```

(Verified 2026-10-17: 8/8 isolated; 46/46 with the two related structure test files — no ordering dependency.)

**O-MAT-IMPL-1 — confirmed visibility gaps (product contract unchanged)**

Strip production rules are **not** implicated; gaps are **fiche-level tile visibility** vs useful content presented in aside.

| Case | Scenario | Product (O-MAT-VIS-1) | Observed | Tracking |
|------|----------|----------------------|----------|----------|
| 3 | Champ Ready, `placesN` / placement signal, no `materialize` | Hide tile | Strip body empty; `shouldShowAsideMatchsTile` can still be true | **O-MAT-IMPL-1** |
| 5 | Swiss Ready (`swiss-ready`) | Hide tile (2a) | Strip body empty; tile flag can still be true at fiche | **O-MAT-IMPL-1** |
| 6 | Ready, lifecycle/composition signal only | Hide tile | Strip body empty; tile flag true via lifecycle Ready | **O-MAT-IMPL-1** |

No SPA functional change in this recette artefact; alignment of `shouldShowAsideMatchsTile` is a **separate** implementation track after scope validation.

**Checklist items touched by this pass**

- [x] Case 1 (Draft): tile hidden; Prepare on header — Vitest + `shouldShowAsideMatchsTile`.
- [x] Cases 2, 4, 7, 8: production / progression in strip; no lifecycle CTAs in aside — Vitest.
- [ ] Case 7 **Suspended**: not replayed here — use `ctrl-o3-mat-recette` + `StructurePhaseOperationalStrip.test.tsx` / [ctrl-o3-mat-devrunner-recette.md](../structure-lifecycle/ctrl-o3-mat-devrunner-recette.md).
- [ ] « Voir les matchs » link (Completed toolbar) — **not** exercised in `phase3MatchsAsideRecette.test.tsx`.
- [ ] Full browser/DevRunner pass — **outstanding** for formal Phase 3 closure.

### Aside — Flux

- [ ] Sorties: phase heads + rule labels aligned with Qualifiés (no volume chip).
- [ ] Attribution: rank medal + qualifier-style rule line (no outcome chips).
- [ ] Empty Sorties / Attribution: `EmptyState` **compact** + small icon; low vertical footprint.

### Règlement

- [ ] Digest strip: match / standing / confrontation (when visible) — large brand icon left (vertically centered), title + binding badge + pencil toolbar edit, facts line (brand icon + secondary value, wide gaps); vertical gradient separators when inline.
- [ ] Omit facts that do not apply (e.g. prolongation / TAB when off).
- [ ] Binding badges (général / personnalisé) + edit icons in section toolbar.
- [ ] « Ouvrir le règlement » link in footband.
- [ ] Narrow width: digest sections wrap (flex, large gap); fact icons secondary, values primary; gradient separator above footband.

### Schematic

- [ ] No dashed outer frame on schematic tile.

### Parity table

Fill [Phase 3 parity table](./actions-permissions-inventory.md#phase-3-parity-table-template) during recette.
