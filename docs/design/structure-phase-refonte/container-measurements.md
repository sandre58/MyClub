# Fiche container width — measurement baseline (Phase 1)

Hub master–detail and composition band thresholds depend on **inline size of the fiche column**, not viewport alone.

CSS sources: `structure-hub.css`, `structure-fiche.css`, `structure-phase-layout.css`.

---

## Hub grid (multi-phase)

| Viewport | Rule | Topology column | Fiche column (grid track) |
|----------|------|-----------------|---------------------------|
| `< 52rem` | Single column | full width | full width |
| `≥ 52rem` | `minmax(14rem, 0.34fr) \| minmax(0, 1fr)` | `0.34fr` (min 14rem) | `1fr` share of `(viewport − gap)` |

Gap: `var(--space-16)` (1rem at default root).

**Fiche track width (content box, before fiche padding):**

\[
W_\text{track} = \frac{1}{1.34}\,\bigl(V - 1\text{rem} - \max(14\text{rem}, 0.34 \cdot (V - 1\text{rem}))\bigr)
\]

Simplified when topology hits **14rem** minimum (typical on laptop widths):

\[
W_\text{track} \approx \frac{V - 15\text{rem}}{1.34}
\]

| Viewport `V` | Fiche track (approx.) | Inner after fiche padding `2×1.5rem` |
|--------------|----------------------|--------------------------------------|
| 52rem | ~27.6rem | ~24.6rem |
| 64rem | ~36.6rem | ~33.6rem |
| 80rem | ~48.5rem | ~45.5rem |
| 101rem | ~64.2rem | ~61.2rem |

Single-phase hub (`structure-hub__layout--single-phase`): fiche track = `V − 2×page horizontal padding` (app shell) — measure in DevTools on `.structure-fiche`.

---

## Composition band container queries

Container: `.structure-phase-composition` (`container-name: phase-composition`).

| Container width | Layout |
|-----------------|--------|
| `≥ 56rem` | 3 columns: Équipes \| Qualifiés \| Affectation |
| `34rem – 55.99rem` | 2 columns; Affectation spans full width on second row when 3 sections visible |
| `< 34rem` | Stack sections |

At **~24.6rem** inner (52rem viewport, multi-phase), expect **narrow stack** — 3-column band is **not** reachable without wider viewport or single-phase hub.

---

## How to verify in browser

1. Open Structure hub with **≥2 phases**, select a phase (N2 fiche).
2. DevTools → select `.structure-fiche` → **Computed** → `width` (content box).
3. Repeat at viewport widths 52rem, 64rem, 80rem (responsive mode).
4. Optional: select `.structure-phase-composition` and confirm which `@container phase-composition` rules apply.

Record findings below when sign-off completes:

| Date | Viewport | Hub mode | `.structure-fiche` width | `.structure-phase-composition` width | Notes |
|------|----------|----------|--------------------------|--------------------------------------|-------|
| | | | | | |

---

## Operational strip containers

Placement/draw clusters inside composition use `.structure-phase-operational-bar` (`container-name: operational-bar`):

- `≤ 42rem`: strip sections stack.
- `≥ 34rem`: placement + draw side-by-side within strip.

Aside **Matchs** tile uses the same operational-bar wrapper.
