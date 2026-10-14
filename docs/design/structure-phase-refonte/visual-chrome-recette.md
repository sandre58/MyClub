# Structure phase fiche — visual chrome (band-3)

Validated **2026-10-14**: hub-aligned fiche column with **inset** inner zones.

Related: [truth sources](./truth-sources.md), [container measurements](./container-measurements.md).

---

## Product chrome

| Level | Implementation |
|-------|----------------|
| **Fiche column** | Base `.structure-fiche` in `structure-fiche.css` — `surface`, border, `shadow-page`, padding `24` (visual parity with Topology `FormSection`) |
| **Band-3 layout** | `.structure-fiche--flat` in `structure-phase-layout.css` |
| **Composition / Matchs / Flux** | Inset tiles: secondary mix background, hairline border (~42% border mix), `radius-control`, no shadow |
| **Schematic** | Dashed frame, transparent viewport on fiche surface |
| **Regulation** | Hairline top separator on fiche foot — strip behaviour unchanged |

Phase header lives inside the fiche shell (identity bar, not a separate hub card).

---

## Schematic layout

- Vertical stretch beside aside (CSS on `.structure-fiche--flat .structure-phase-main`).
- Full-width schematic with aside below: not adopted.
