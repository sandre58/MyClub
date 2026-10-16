# Programme B — arbitrage décisionnaire (2026-10-16)

**Statut** — Enregistrement **§11C.4** (Git). Ne remplace pas une page Notion Décision si le processus produit l’exige ; à recouper avec la SoT Notion.

**Principe** — Pas de ratification en bloc du diff local Programme B. Trois décisions distinctes + C-ENF découpé.

---

## Décisions

| Sujet | Décision | §11C.4 / fiche |
|-------|----------|----------------|
| **G-B-1-ENF** | Ratifier **uniquement** B-Q3a (≤1 tirage non annulé ; refus `CreateDraw` si autre tirage non `Cancelled`). | **SLICE-2-G-B-1-ENF** — **Authorized** ; **pas Closed** avant vérification conformité + commits dédiés. |
| **G-OPS3 qual/prog** | Ratifier le refus de changement des règles **qualification / progression** lorsqu’un tirage est **Applied** (même entrées logiques que G-B-3 : mutation refusée, pas obsolescence C). | **SLICE-3-G-OPS3-QUAL-PROG-ENF** — **Authorized** ; **G-OPS3** reste **Partial** §12. |
| **C-ENF** | **Ne pas** ratifier rétroactivement le bundle Detect + Apply + Mat + Start. | Trois slices **Pending** (voir ci-dessous) ; **aucun commit** C-ENF tant qu’ils ne sont pas autorisés un par un. |

---

## C-ENF — découpage (en attente d’autorisation)

| Slice ID | Backlog | Périmètre cible | Statut |
|----------|---------|-----------------|--------|
| **SLICE-C-ENF-DETECT** | **C-ENF-Detect** (G-C-1) | Prédicat de conformité / obsolescence **pour consommation** ; surfacing lecture (`StageDrawDto`, etc.). | **Pending** — DM n’a pas encore autorisé l’implémentation. |
| **SLICE-C-ENF-APPLY** | **C-ENF-Apply** (G-C-2) | Refus `ApplyDraw` si tirage obsolète (après Detect). | **Pending** |
| **SLICE-C-ENF-MAT-START** | **C-ENF-Mat** + **C-ENF-Deps** (G-C-3, G-C-4) | Refus **Materialize** et **Start** lorsque la voie consomme un tirage obsolète. | **Pending** |

**Code local non commité** — Les fichiers `DrawConsumptionConformance`, `StructureDrawConsumptionGate`, et raccordements Apply/Mat/Start constituent une **proposition technique** alignée sur ce découpage ; ils **ne sont pas** livrés tant que les slices C-ENF correspondants ne sont pas **Authorized** puis vérifiés.

---

## Ordre d’exécution (post-arbitrage)

1. Fiches slices + §11C.4 (ce document + [phase-mutability-matrix.md](./phase-mutability-matrix.md)).
2. Aucune fiche **Closed** sans protocole slice-1 (autorisation → implémentation → preuves → revue).
3. Matrice §12 : **G-OPS3** = **Partial** ; **G-B-1** / **G-C-INV** mis à jour sans prétendre **Done** pour C-ENF.
4. Tests ciblés **Release** + audit diff par lot.
5. Commits **séparés** (G-B-1 ; G-OPS3 ; C-ENF uniquement après autorisation slice par slice).

---

## Références

[wave-b-programme-after-gel-a.md](./wave-b-programme-after-gel-a.md) · [slice-2-g-b-1-enf.md](./slice-2-g-b-1-enf.md) · [slice-3-g-ops3-qual-prog-enf.md](./slice-3-g-ops3-qual-prog-enf.md)
