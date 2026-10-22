# Phase 6 — S4 — CSS repo (qualification)

**Mode:** audit only — **aucune** suppression de règles CSS sans arbitrage ciblé (type S3-INV / S4-INV).  
**SHA:** `6640c881` (post S3-INV) · re-run après merge.  
**Hors périmètre:** i18n (S3 clos), dead code TS (S2).

## Outils

| Commande | Rôle |
| --- | --- |
| `npm run lint:css` | **Gate CI** — Stylelint `src/**/*.css` |
| `npm run css:audit:rider` | Hygiène sélecteurs / `var(--*)` (report) |
| `npm run frontend:audit` | Graphe CSS, classes sans match texte, échantillon doublons |
| `npm run frontend:css:qualify` | Buckets S4 sur les 100 classes « orphelines » textuelles |

```bash
cd src/PlayUp/web
npm run lint:css
npm run css:audit:rider
npm run frontend:audit
npm run frontend:css:qualify
```

---

## Synthèse

| Signal | Valeur | Interprétation |
| --- | ---: | --- |
| Fichiers CSS atteignables (`index.css`) | 69 | **0** fichier orphelin |
| Classes uniques (CSS reachable) | 1501 | — |
| Sans littéral `\bclass\b` dans TS/HTML | **100** | Heuristique — **≠** 100 suppressions |
| Composants DS sans import | 0 | — |
| `lint:css` | Vert | Norme merge |
| `css:audit:rider` (wraps / vars) | 0 wraps @ wave 1 | — |
| Groupes « même bloc de déclarations, plusieurs sélecteurs » | **80** dans **19** fichiers | Dette / factorisation **optionnelle** — souvent variantes DS |

Les **100** classes ne sont pas 100 défauts : la majorité est utilisée via **`className={\`…--${…}\`}`** (non détecté par recherche textuelle du nom complet).

---

## Buckets classes (100 « orphelines » textuelles)

| Bucket | Count | Qualification | Suppression auto |
| --- | ---: | --- | --- |
| **dynamic_prefix** | 46 | **Faux positif** — modificateur via template (`regulation-card-token--${type}`, `structure-occupancy-meter--${state}`, `schematic-slot--…`, shell drawer counts, etc.). | **Non** |
| **ds_dynamic_modifier** | 28 | **Faux positif** — DS : `ds-alert--${tone}`, `ds-chip--${tone}`, `ds-status--tone-*`, etc. (`Alert.tsx`, `Chip.tsx`, …). | **Non** |
| **design_lab** | 4 | **Faux positif** — `dlab-wait--${kind}` / `--${scale}` (`LabWait.tsx`) ; styles aussi référencés en CSS composé. | **Non** |
| **page_modifier_investigate** | 19 | **À investiguer** — aucune occurrence TS des noms complets ; souvent **CSS résiduel** post-refonte (ex. sous-arbre `structure-regulation-digest__{body,header,title,actions,hero-icon}` non monté en TSX ; `structure-composition__warn`, blocs dual-gauge layout). | **Non** sans lot S4-INV |
| **investigate** | 3 | **À investiguer** — voir S4-INV ci-dessous. | **Non** |

**Confirmé inutilisé (produit)** en S4 : **aucun** lot global — preuve au cas par cas uniquement.

---

## Doublons de blocs de déclarations

- **80** groupes dans **19** fichiers (algorithme naïf : corps identique > 40 car., sélecteurs multiples).
- Exemples typiques : listes de modificateurs DS (`ds-chip--win | ds-chip--loss`), états focus/form, grilles structure.
- **Décision S4 :** considérer comme **dette cosmétique / DRY CSS**, pas comme incident qualité ; pas de gate CI ; lot futur seulement si douleur maintenance mesurée.

---

## S4-INV — candidats pour arbitrage ciblé (optionnel)

| Id | Sujet | Preuve | Piste |
| --- | --- | --- | --- |
| **S4-INV-01** | `.ds-shell-header__period` | TS utilise `shell-header__period` (`ShellHeader.tsx`) ; règle DS `[data-shell-vp='phone'] .ds-shell-header__period` sans classe appliquée en TS. | Supprimer règle DS morte **ou** aligner naming — micro-lot. |
| **S4-INV-02** | `.ds-notice__body`, `.ds-notice__icon` | Markup produit = `<p class="ds-notice ds-notice--…">` sans enfants body/icon ; styles pour composition non utilisée. | Retirer sous-arbre CSS **ou** introduire composant Notice — arbitrage UX. |
| **S4-INV-03** | Lot `structure-regulation-digest__*` (6+ classes) + voisins layout | CSS dans `structure-phase-layout.css` ; TSX actif utilise `__section`, `__sep`, `__facts`, `__chips` — pas `__body` / `__header` / `__title` / `__actions` / `__hero-icon`. | Lot CSS structure après recette visuelle. |
| **S4-INV-04** | `.structure-phase-rail--empty` | Présent en sélecteurs CSS ; **jamais** posé en TSX (grep). | Vérifier si état vide encore requis ; sinon retirer règles associées. |

Les **15** autres entrées `page_modifier_investigate` suivent la même logique : grep TS → recette → lot unique.

---

## Critère de fin S4 (audit)

- [x] Buckets documentés + script `frontend:css:qualify`.
- [x] Doublons quantifiés, non convertis en gate.
- [x] **S4-INV-01…04 différés** (clôture phase 6) — dette qualifiée, **aucune** suppression ni refactor CSS en phase 6.
- [x] Pas de nettoyage des 100 classes ni des 80 groupes doublons.

---

## Références

- S1 lint/CSS gates : [`phase-6-s1-lint-ci-arbitration.md`](phase-6-s1-lint-ci-arbitration.md)
- Wave 1 : [`phase-6-wave-1-static-baseline.md`](phase-6-wave-1-static-baseline.md)
- Rider / conventions : `src/PlayUp/web/docs/conventions.md`
