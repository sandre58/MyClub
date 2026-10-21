# PROPOSITION — Ligne §11C.4 G-OPS3 B1 (à arbitrer)

> **DRAFT — NON APPROUVÉ.** Ne **pas** coller cette ligne dans [phase-mutability-matrix.md §11C.4](./phase-mutability-matrix.md#11c4-slice-records) sans décision DM explicite. La fiche B1 reste **Draft** jusqu’à application coordonnée avec [la proposition de clôture](./slice-g-ops3-b1-cup-slot-placement-closure-proposal.md).

---

## Contexte

- Contrat produit B1 arbitré (DM 2026-10-16) · livraison code **`c9029ba0`**.
- §12 **G-OPS3** doit rester **Partial** après clôture slice (draw-rules et autres reliquats).
- Distinct de **SLICE-1** (G-B-3 pool), **SLICE-3** (qual/prog Applied).

---

## Ligne proposée (table §11C.4)

| Slice ID | Backlog | Status | Fiche |
|----------|---------|--------|-------|
| **SLICE-G-OPS3-B1-CUP-SLOT-PLACEMENT** | G-OPS3 B1 — Cup `AssignEntryToSlot` / `ClearSlotAssignment` only | **Closed** (`c9029ba0` ; réserves R4/T7 · §12 **G-OPS3 Partial**) | [slice-g-ops3-b1-cup-slot-placement.md](./slice-g-ops3-b1-cup-slot-placement.md) |

**Note sous-table (optionnelle, si DM souhaite symétrie SLICE-3) :**

> **SLICE-G-OPS3-B1** — Réserves : branche défensive R4 (non testée reflection) ; T7 livraison = F1 indépendant, pas scénario combiné Applied+F1 sur slot protégé. Pas de parité SPA disable obligatoire.

---

## Points à arbitrer avant application

| # | Question | Option recommandée (documentaire) |
|---|----------|-----------------------------------|
| A | **Slice ID** final : `SLICE-G-OPS3-B1-CUP-SLOT-PLACEMENT` vs raccourci `SLICE-G-OPS3-B1` | Garder le nom long (aligné fiche draft) ou tronquer pour cohérence tableau |
| B | Date de clôture dans la cellule Status | Date du commit gouvernance, pas seulement `c9029ba0` |
| C | Mise à jour note §12 **G-OPS3** | Remplacer « B1 contract fiche draft (no §11C.4) » par « B1 **Closed** (`c9029ba0`) » tout en laissant **Partial** |
| D | Autorisation d’implémentation future | **Aucune** — ligne **Closed** uniquement ; pas de nouvelle autorisation |

---

## Ce que cette ligne n’autorise pas

- Réouverture du périmètre B1 (Groups, affectation replace, draw-rules).
- Changement du statut §12 **G-OPS3** vers **Done**.
- Implémentation SPA ou Host hors régression / doc sans nouvelle slice §11C.4.
