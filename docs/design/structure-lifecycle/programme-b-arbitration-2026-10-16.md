# Programme B — arbitrage décisionnaire (2026-10-16)

**Statut** — Enregistrement **§11C.4** (Git). Ne remplace pas une page Notion Décision si le processus produit l’exige ; à recouper avec la SoT Notion.

**Principe** — Pas de ratification en bloc du diff local Programme B. Trois décisions distinctes + C-ENF découpé.

---

## Décisions

| Sujet | Décision | §11C.4 / fiche |
|-------|----------|----------------|
| **G-B-1-ENF** | Ratifier **uniquement** B-Q3a (≤1 tirage non annulé ; refus `CreateDraw` si autre tirage non `Cancelled`). | **SLICE-2-G-B-1-ENF** — **Authorized** ; **pas Closed** avant vérification conformité + commits dédiés. |
| **G-OPS3 qual/prog** | Ratifier le refus de changement des règles **qualification / progression** lorsqu’un tirage est **Applied** (même entrées logiques que G-B-3 : mutation refusée, pas obsolescence C). | **SLICE-3-G-OPS3-QUAL-PROG-ENF** — **Authorized** ; **G-OPS3** reste **Partial** §12. |
| **C-ENF** | **Ne pas** ratifier le bundle Apply + Mat + Start avec Detect. Detect : périmètre produit **Authorized** (voir § Detect). | **SLICE-C-ENF-DETECT** **Authorized** ; Apply / Mat-Start **Pending** ; commits C-ENF **un slice à la fois**. |

---

## C-ENF — découpage

| Slice ID | Backlog | Périmètre cible | Statut |
|----------|---------|-----------------|--------|
| **SLICE-C-ENF-DETECT** | **C-ENF-Detect** (G-C-1) | Prédicat `IsObsoleteForConsumption` (W-inputs, W-pool ; W-rules **exclu**) + lecture DTO/SPA uniquement. | **Authorized** (2026-10-16) — **pas Closed** avant commit dédié · [fiche](./slice-c-enf-detect.md) |
| **SLICE-C-ENF-APPLY** | **C-ENF-Apply** (G-C-2) | Refus `ApplyDraw` si tirage obsolète (après Detect). | **Pending** — non autorisé |
| **SLICE-C-ENF-MAT-START** | **C-ENF-Mat** + **C-ENF-Deps** (G-C-3, G-C-4) | Refus **Materialize** et **Start** lorsque la voie consomme un tirage obsolète. | **Pending** — non autorisé |

**Code local non commité** — Prototype `DrawConsumptionConformance` + gates Apply/Mat/Start : **non ratifié** ; écart documenté dans [slice-c-enf-detect.md](./slice-c-enf-detect.md) § prototype vs contrat. Livraison **Detect** = commit isolé après alignement sur la fiche.

### Arbitrage Detect (2026-10-16) — synthèse

- **Pool effectif** : intersection affectation ∩ composition ; pool vide autorisé ; résolution non vide ⇒ obsolete si hors pool.
- **W-applied** : Applied ⇒ non obsolete ; **Published non Resolved** / Draft / Cancelled / NoSolution ⇒ `false`.
- **W-rules** : hors Detect (exclusion explicite).
- **Lecture** : `isObsoleteForConsumption` dérivé ; pas de badge, drawer, ni gate d’action dans ce slice.
- **Tests** : neuf scénarios listés dans la fiche Detect ; G-B-3 reste hors preuve Detect.

---

## Ordre d’exécution (post-arbitrage)

1. Fiches slices + §11C.4 (ce document + [phase-mutability-matrix.md](./phase-mutability-matrix.md)).
2. Aucune fiche **Closed** sans protocole slice-1 (autorisation → implémentation → preuves → revue).
3. Matrice §12 : **G-OPS3** = **Partial** ; **G-B-1** / **G-C-INV** mis à jour sans prétendre **Done** pour C-ENF.
4. Tests ciblés **Release** + audit diff par lot.
5. Commits **séparés** (G-B-1 ; G-OPS3 ; C-ENF uniquement après autorisation slice par slice).

---

## Références

[wave-b-programme-after-gel-a.md](./wave-b-programme-after-gel-a.md) · [slice-2-g-b-1-enf.md](./slice-2-g-b-1-enf.md) · [slice-3-g-ops3-qual-prog-enf.md](./slice-3-g-ops3-qual-prog-enf.md) · [slice-c-enf-detect.md](./slice-c-enf-detect.md)
