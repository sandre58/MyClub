# Structure phase fiche — Phase 3 recette

Scope: **aside** (Matchs, Flux), **règlement** (bande digest), preuves automatisées + checklist manuelle.

Authority: [actions-permissions-inventory](./actions-permissions-inventory.md), [truth-sources](./truth-sources.md).

---

## Automated coverage (Git)

| Area | Tests |
|------|--------|
| Aside Matchs visibility | `structureMatchsSectionUi.test.ts` (`shouldShowAsideMatchsTile`) |
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

- [ ] Production gauge, materialize/dematerialize, match progress only in tile body.
- [ ] « Voir les matchs » link when applicable.

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
