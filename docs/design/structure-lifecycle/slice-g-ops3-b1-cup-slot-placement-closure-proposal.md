# PROPOSITION — Clôture gouvernance G-OPS3 B1 (Cup slot placement)

> **DRAFT — NON APPROUVÉ.** Ce document **ne modifie pas** la fiche [`slice-g-ops3-b1-cup-slot-placement.md`](./slice-g-ops3-b1-cup-slot-placement.md) (reste **Draft**), **ne met pas à jour** [§11C.4](./phase-mutability-matrix.md#11c4-slice-records) ni le statut §12 **G-OPS3** tant qu’un DM n’a pas validé l’application de cette proposition.

| Field | Value (proposé après validation DM) |
|-------|----------------------------------------|
| **Status** | **Closed** (date DM) — livraison [`c9029ba0`](https://github.com/sandre58/MyClub/commit/c9029ba0) |
| **Slice ID** | `SLICE-G-OPS3-B1-CUP-SLOT-PLACEMENT` |
| **Allowed backlog** | Reliquat **G-OPS3** — Cup `AssignEntryToSlot` / `ClearSlotAssignment` uniquement |
| **Contract** | Refus des mutations **conflictuelles** sur paires `(SlotKey, EntryId)` protégées par un tirage **Applied** (`DrawAppliedState` + `Resolution.SlotResults`) |

**Gouvernance inchangée jusqu’à commit DM :** fiche **Draft** · pas de ligne §11C.4 · §12 **G-OPS3** = **Partial**.

**Voir aussi :** [proposition de ligne §11C.4](./slice-g-ops3-b1-11c4-row-proposal.md) · [wave B programme](./wave-b-programme-after-gel-a.md).

---

## Périmètre livré (`c9029ba0`)

| In scope (livré) | Out of scope (inchangé) |
|------------------|-------------------------|
| `StructureAppliedDrawSlotPlacementGate` sur Assign / Clear | `ReplaceStageAffectationAuthoring` (**G-B-3** / SLICE-1) |
| Wire `Application.AppliedDrawSlotPlacementProtected` · HTTP **409** | Qual/prog / draw-rules (**SLICE-3** et reliquats G-OPS3) |
| Host mapping [`PlayUpExceptionHandler`](../../../src/PlayUp/MyClub.PlayUp.Host/PlayUpExceptionHandler.cs) | Promotion §12 **G-OPS3** → **Done** |
| SPA : clé `lifecycleMutationMessageKey` + EN/FR `structure.json` | Parité SPA « bouton désactivé » obligatoire (hors slice) |
| Tests Application + Host (voir preuves) | Groups placement (arbitrage séparé) |

**Ordre des gardes (livré) :** D2 → **B1** → domaine (Assign / Clear). **Pas** d’ordonnancement imposé avec G-B-3 (commandes disjointes).

---

## Preuves d’exécution (Release — rapportées à la livraison)

### Application — [`CupSlotPlacementGOps3B1Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/CupSlotPlacementGOps3B1Tests.cs) (9)

| ID fiche | Scénario (résumé) | Résultat attendu |
|----------|-------------------|------------------|
| **T1** | Applied · `ClearSlotAssignment` sur slot ∈ `SlotResults` | Refus · `AppliedDrawSlotPlacementProtected` |
| **T2** | Applied · `AssignEntryToSlot` change l’entrée protégée | Refus · même code |
| **T3** | Applied · même `EntryId` sur même slot | Autorisé |
| **T4** | Applied · slot hors `SlotResults` | B1 n’applique pas (autorisé côté B1) |
| **T5–T6** | Non Applied / drift `IsApplied` false | B1 n’applique pas (clear/assign autorisés côté B1) |
| **T7** | Applied ailleurs · F1 sur slot non protégé | **F1** refuse indépendamment (`MaterializedOpponentConflict`) |
| — | Clé slot avec espaces (canonicalisation) | Alignement gate / `FindSlot` |

### Host — [`CupSlotPlacementGOps3B1EndpointTests`](../../../tests/MyClub.PlayUp.Host.Tests/CupSlotPlacementGOps3B1EndpointTests.cs) (2)

| Scénario | HTTP | Code |
|----------|------|------|
| `DELETE` assignment · slot protégé Applied | **409** | `Application.AppliedDrawSlotPlacementProtected` |
| `PUT` assignment · changement entrée protégée | **409** | idem |

**Message utilisateur (FR, arbitré) :** *« Cette affectation est protégée par le tirage appliqué. Elle ne peut pas être modifiée directement. »*

---

## Réserves acceptées à la clôture (DM 2026-10-16 — livraison)

| Réserve | Contenu | Impact clôture |
|---------|---------|----------------|
| **R4** | Branche défensive gate : candidats ambigus → pas de refus B1 « fourre-tout » | Documentée comme **non atteignable** avec données domaine valides ; **pas** de test reflection dédié |
| **T7 (limite de preuve)** | Le test nommé T7 en livraison prouve **F1 après B1** quand B1 ne s’applique pas ; **ne** prouve pas le scénario « Applied ailleurs + conflit F1 sur slot protégé » en une seule mutation | Acceptée pour clôture slice B1 ; ne généralise pas la couverture F1×Applied |

**Non revendiqué dans B1 :** test Host pour chaque variante Application ; parité hub SPA complète ; recovery produit (annuler tirage, etc.).

---

## Critères de clôture gouvernance (checklist DM)

À marquer **Done** uniquement après validation explicite :

1. Diff livré = périmètre B1 uniquement (`c9029ba0`). — **Done** (code).
2. T1–T7 Application + preuve Host Assign/Clear refuse. — **Done** (avec réserves ci-dessus).
3. Clear refuse **avant** no-op domaine sur slot protégé Applied. — **Done**.
4. Fiche B1 → **Closed** + ligne §11C.4 appliquée ([proposition](./slice-g-ops3-b1-11c4-row-proposal.md)) + index / wave B si DM le demande. — **En attente DM**.
5. §12 **G-OPS3** : reste **Partial** (draw-rules, autres reliquats). — **Proposé** (pas de promotion Done).
6. Pas de confusion avec `AppliedDrawPoolChangeBlocked` (G-B-3) ni C-ENF. — **Done**.

---

## Application proposée (après « go » DM)

1. Mettre à jour l’en-tête de [`slice-g-ops3-b1-cup-slot-placement.md`](./slice-g-ops3-b1-cup-slot-placement.md) (statut **Closed**, lien commit, réserves).
2. Insérer la ligne §11C.4 validée dans [`phase-mutability-matrix.md`](./phase-mutability-matrix.md).
3. Ajuster la note §12 **G-OPS3** pour référencer B1 **Closed** (slice) tout en gardant **Partial** (backlog).
4. Commit gouvernance dédié (séparé de `c9029ba0` si pas déjà fusionné).

**Ne pas faire sans DM :** changer §11C.4, statut fiche, ou G-OPS3 global dans le même commit que une ré-ouverture de périmètre.
