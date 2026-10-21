# Recette — Retrait équipe sur bande Composition (lot B — présentation)

**Status:** **Shipped** (2026-10-18) — `EntryCellWithdrawnMarker`, bande Composition + dialog. **Depends on** [composition-teams-resolution-vs-eligibility-arbitration-2026-10-18.md](./composition-teams-resolution-vs-eligibility-arbitration-2026-10-18.md) §2–3 (dual counter still OPEN).

**Goal:** Withdrawn entry in composition shows **scannable state** (icon + tooltip = retrait), not ambiguous « Plus éligible » as forfeit match.

---

## Scenarios

| Id | Setup | Expected |
|----|--------|----------|
| **B-W-1** | `WithdrawEntry` on composed team | Icon + tooltip (FR/EN) on row **end**; accessible name |
| **B-W-2** | Match forfeit, entry still Active | **No** withdrawal icon on Structure band |
| **B-W-3** | Foundations / EntryCell specimen | Matches product strip |

**Touchpoints:** `EntryCell.tsx` · `structurePhaseEntriesRail.tsx` · i18n `enumLabels.entryStatus` or dedicated `composition.withdrawnTip`.
