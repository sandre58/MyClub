# Programme B — arbitrage décisionnaire (2026-10-16)

**Statut** — Enregistrement **§11C.4** (Git). Ne remplace pas une page Notion Décision si le processus produit l’exige ; à recouper avec la SoT Notion.

**Principe** — Pas de ratification en bloc du diff local Programme B. Trois décisions distinctes + C-ENF découpé.

---

## Décisions

| Sujet | Décision | §11C.4 / fiche |
|-------|----------|----------------|
| **G-B-1-ENF** | Ratifier **uniquement** B-Q3a (≤1 tirage non annulé ; refus `CreateDraw` si autre tirage non `Cancelled`). | **SLICE-2-G-B-1-ENF** — **Closed** (`c1a12ec4`) ; réserve SPA · §12 **G-B-1 Deferred** — [fiche](./slice-2-g-b-1-enf.md) |
| **G-OPS3 qual/prog** | Ratifier le refus de changement des règles **qualification / progression** lorsqu’un tirage est **Applied** (même entrées logiques que G-B-3 : mutation refusée, pas obsolescence C). | **SLICE-3-G-OPS3-QUAL-PROG-ENF** — **Authorized** ; **G-OPS3** reste **Partial** §12. |
| **C-ENF** | Bundle interdit. Detect + Apply + Mat-Start **Closed** (`279b6235`, `85360d3`, `ee0fc2e6`). | **G-C-INV** §12 **Done** (consumption gates) ; limite Host Start HTTP documentée en fiche Mat-Start. |

---

## C-ENF — découpage

| Slice ID | Backlog | Périmètre cible | Statut |
|----------|---------|-----------------|--------|
| **SLICE-C-ENF-DETECT** | **C-ENF-Detect** (G-C-1) | Prédicat `IsObsoleteForConsumption` (W-inputs, W-pool ; W-rules **exclu**) + lecture DTO/SPA uniquement. | **Closed** (2026-10-16) — `279b6235` · [fiche](./slice-c-enf-detect.md) |
| **SLICE-C-ENF-APPLY** | **C-ENF-Apply** (G-C-2) | Refus `ApplyDraw` si tirage cible obsolète (prédicat Detect) ; wire 409 + i18n. | **Closed** (2026-10-16) — `85360d3` · [fiche](./slice-c-enf-apply.md) |
| **SLICE-C-ENF-MAT-START** | **C-ENF-Mat** + **C-ENF-Deps** (G-C-3, G-C-4) | Refus **Materialize** (2 commandes) + **Start** ; parité lecture Materialize/Start. | **Closed** (2026-10-16) — `ee0fc2e6` · [fiche](./slice-c-enf-mat-start.md) |

**WIP** — Stash `c-enf-apply-mat-gates-wip` / `.c-enf-gates-wip/` : prototype obsolète ; livraison C-ENF complète en Git — stash conservé, non requis pour **G-C-INV**.

### Arbitrage Detect (2026-10-16) — synthèse

- **Pool effectif** : intersection affectation ∩ composition ; pool vide autorisé ; résolution non vide ⇒ obsolete si hors pool.
- **W-applied** : Applied ⇒ non obsolete ; **Published non Resolved** / Draft / Cancelled / NoSolution ⇒ `false`.
- **W-rules** : hors Detect (exclusion explicite).
- **Lecture** : `isObsoleteForConsumption` dérivé ; pas de badge, drawer, ni gate d’action dans ce slice.
- **Tests** : neuf scénarios listés dans la fiche Detect ; G-B-3 reste hors preuve Detect.

### Arbitrage C-ENF Apply / Mat-Start (2026-10-16)

| # | Sujet | Décision |
|---|--------|----------|
| 1 | **SLICE-C-ENF-APPLY** | **Authorized** — refus `ApplyDraw` via `IsObsoleteForConsumption` ; `PublishAndApplyDraw` via `ApplyDraw.Execute` ; `Application.DrawObsoleteForConsumption` ; 409 + i18n ; tests R-Inv-1 (Apply) ; **ne pas** modifier le prédicat Detect |
| 2 | **Ordre** | Apply implémenté et **Closed** avant autorisation Mat-Start |
| 3 | **Mat-Start** | Un seul slice (G-C-3 + G-C-4) ; **parité lecture** Materialize + Start **in scope** quand autorisé |
| 4 | **G-C-INV §12** | **Done** seulement après clôture formelle **des deux** slices + preuves + parité ; sinon **Deferred** |
| 5 | **Gouvernance** | Fiches + §11C.4 **Authorized** avant reprise WIP ; jamais **Closed** sans commit dédié |

**Réserve Apply :** contrôle au bon moment sur l’état courant — pas de faux blocage d’un tirage encore consommable.

### Arbitrage C-ENF Mat-Start (2026-10-16)

| # | Sujet | Décision |
|---|--------|----------|
| 1 | **Autorisation** | **Authorized** après clôture Apply — implémentation distincte, pas de bundle |
| 2 | **Materialize** | `EnsureNoObsoleteDrawsOnStage` sur `MaterializeMatches` + `MaterializeCupFromOccupiedSlots` |
| 3 | **Start** | Blocage commande + **`TryGetDisabledReasonCode`** aligné |
| 4 | **Parité lecture** | `StructureMaterializeAssembler` + hub Start — codes disabled existants ; pas de drawer |
| 5 | **Tests** | Domain/Application/Host + assemblers ; R-Inv-1 jambes Mat/Start ; **R-Mat-block** |
| 6 | **WIP** | Reprise **sélective** du stash — **pas** `stash pop` global |
| 7 | **G-C-INV** | **Done** seulement après **Closed** Mat-Start + preuves parité + §12 ; sinon **Deferred** |

**Hors périmètre :** prédicat Detect, W-rules, Apply, drawer diagnostics.

---

## Ordre d’exécution (post-arbitrage)

1. Fiches slices + §11C.4 (ce document + [phase-mutability-matrix.md](./phase-mutability-matrix.md)).
2. Aucune fiche **Closed** sans protocole slice-1 (autorisation → implémentation → preuves → revue).
3. Matrice §12 : **G-OPS3** = **Partial** ; **G-B-1** **Deferred** ; **G-C-INV** **Done** après clôture C-ENF (2026-10-16).
4. Tests ciblés **Release** + audit diff par lot.
5. Commits **séparés** (G-B-1 ; G-OPS3 ; C-ENF uniquement après autorisation slice par slice).

---

## Références

[wave-b-programme-after-gel-a.md](./wave-b-programme-after-gel-a.md) · [slice-2-g-b-1-enf.md](./slice-2-g-b-1-enf.md) · [slice-3-g-ops3-qual-prog-enf.md](./slice-3-g-ops3-qual-prog-enf.md) · [slice-c-enf-detect.md](./slice-c-enf-detect.md) · [slice-c-enf-apply.md](./slice-c-enf-apply.md) · [slice-c-enf-mat-start.md](./slice-c-enf-mat-start.md)
