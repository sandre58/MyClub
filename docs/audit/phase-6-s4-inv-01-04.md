# Phase 6 — S4-INV-01 + S4-INV-04 (F-07 partial)

**Date:** 2026-10-27  
**Tip:** `d5f4c2f4e30e28b2c6b4d0e1e47be4790020d2c6` (main post-#104)  
**Scope:** dead selector drift only. S4-INV-02, S4-INV-03, the 80 duplicate declaration groups, theme/tokens, and a `frontend:audit` CI gate stay out.

Qualification source: [`phase-6-s4-css-qualification.md`](phase-6-s4-css-qualification.md).

## Hypothesis

`ShellHeader` and the Design Lab hide the competition period with `shell-header__period`. The phone rule in `shell.css` also listed `.ds-shell-header__period`, which no TSX applies. Structure empty flux rails still render, but through the element `structure-phase-rail__empty` inside `FluxRail` (`contentEmpty`), not the modifier `structure-phase-rail--empty`. That modifier never matched, so its rules (and the `:not(--empty)` exclusion) can go without changing the flex of the rails the page actually mounts. Phone period hiding stays on the live class.

## S4-INV-01 — choice

**Remove the unused DS selector.** Do not rename the markup to `ds-shell-header__period`.

The period is product shell chrome (`shell-header.css` + `ShellHeader.tsx` + Design Lab). The phone hide already targets `.shell-header__period` in the same rule as `.shell-header__meta-separator`. Renaming would touch markup and the stylesheet that owns the period’s type, for a class the design system never styled on its own.

Phone rule after the change:

```css
[data-shell-vp='phone'] .shell-header__period,
[data-shell-vp='phone'] .shell-header__meta-separator {
  display: none;
}
```

## S4-INV-04 — choice

**Empty state stays. Dead modifier rules go.**

`FluxRail` still swaps children when `contentEmpty` is set (`StructurePhaseSchematicAndAside`: Sorties / Attribution). The wrapper is `structure-phase-rail__empty`. Kept: `.structure-phase-rail__empty .ds-empty--compact` in `structure-phase-layout.css`.

Removed, because `--empty` is never set on the `<aside>`:

- `structure-fiche.css` — `.structure-phase-hero__out > .structure-phase-rail--empty`
- `structure-phase-layout.css` — `.structure-phase-tile--flux .structure-phase-rail--empty .structure-flux-rail__head`

The live companion used `:not(.structure-phase-rail--empty)`. That exclusion never failed, so every direct-child rail (including an empty flux rail) already received `flex: 1 1 auto; min-height: 0`. The selector is now `.structure-phase-hero__out > .structure-phase-rail` with those same declarations.

## Grep

Commands (repo root):

```bash
rg -n "ds-shell-header__period|shell-header__period" --glob '*.{css,tsx,ts}'
rg -n "structure-phase-rail--empty|structure-phase-rail__empty" --glob '*.{css,tsx,ts}'
```

### Before (`d5f4c2f4`)

| Class | CSS | TSX |
| --- | --- | --- |
| `ds-shell-header__period` | `shell.css` phone rule only | none |
| `shell-header__period` | `shell.css` phone rule, `shell-header.css` | `ShellHeader.tsx`, `DesignLabPage.tsx` |
| `structure-phase-rail--empty` | `structure-fiche.css` (2), `structure-phase-layout.css` (1) | none |
| `structure-phase-rail__empty` | `structure-phase-layout.css` | `structureFluxRails.tsx` |

### After

| Class | CSS | TSX |
| --- | --- | --- |
| `ds-shell-header__period` | none | none |
| `shell-header__period` | `shell.css` phone rule, `shell-header.css` | unchanged |
| `structure-phase-rail--empty` | none | none |
| `structure-phase-rail__empty` | unchanged | unchanged |

## Visual recipe (3A)

No layout declaration on a matched selector changed. Check these surfaces anyway:

1. **Shell, phone** (`<48rem`, `data-shell-vp="phone"`), competition with a period: the date range and the `·` separator are hidden; crest, name, and status stay. Desktop and tablet: the period stays next to the status.
2. **Structure phase fiche, flux tile** (Sorties / Attribution): a filled rail and an empty rail (`EmptyState` inside `.structure-phase-rail__empty`) keep the same rail column (`flex: 1 1 auto; min-height: 0` under `.structure-phase-hero__out`). Hub spacing around the flux tile does not jump.

## Gates

- `npm run lint:css` is the CSS merge gate.
- `frontend:audit` stays report-only (S1). Do not add it to CI in this lot.

## Still open on F-07

- S4-INV-02 (`.ds-notice__body` / `__icon`) — needs a Notice component arbitration.
- S4-INV-03 (`structure-regulation-digest__*` residual) — separate Structure visual lot.
- 80 duplicate declaration groups — cosmetic, not this PR.
