# Phase 6 — S2 — Dead code frontend (qualification)

**Mode:** audit only — **no** suppression, refactor produit, ni changement de contrat métier.  
**SHA:** `main` @ `aaa09642` (post S1/T1) · re-run after merge.  
**Hors périmètre S2:** classes CSS orphelines, clés i18n, blocs CSS dupliqués → **S4** / **S3** (après S2).

## Méthode

| Étape | Outil | Limite connue |
| --- | --- | --- |
| Graphe modules produit | `npm run frontend:module-graph` (`scripts/frontend-module-graph-audit.mjs`) | Imports relatifs + `import()` littéraux depuis `main.tsx` ; pas d’alias `@/` |
| Graphe test-only | Même script, entrées `*.test.*` | Fixtures hors `src/test/` mais nommées `.fixture.ts` / `*TestHelpers*` |
| Composants DS | `npm run frontend:audit` (§ DESIGN-SYSTEM COMPONENTS) | Heuristique import JSX / chemin |
| Fichiers CSS orphelins | `frontend:audit` (§ CSS IMPORT GRAPH) | — |
| Symboles inutilisés **dans** un fichier | `tsc` (`noUnusedLocals` / `noUnusedParameters`) | **Déjà** gate CI via `typecheck` |
| Exports nommés jamais importés | Non automatisé en gate | Faux positifs ré-exports / usage dynamique |

**Repro:**

```bash
cd src/PlayUp/web
npm run frontend:module-graph
npm run frontend:audit   # DS components + CSS graph only for S2 cross-check
```

---

## Synthèse chiffrée

| Métrique | Valeur |
| --- | ---: |
| Modules prod `.ts/.tsx` (hors tests) | 321 |
| Atteignables depuis `main.tsx` | 315 |
| Non atteignables depuis produit | 6 |
| Dont support **tests uniquement** | 5 |
| Dont candidat prod | 1 (`vite-env.d.ts` — types) |
| Composants DS sans import externe | **0** |
| Fichiers CSS hors graphe `index.css` | **0** |

---

## Liste qualifiée (3 catégories)

### Confirmé inutilisé (produit)

| Id | Artefact | Preuve | Action proposée (arbitrage) |
| --- | --- | --- | --- |
| **S2-001** | `pages/structure/structureOperationalStripRatioTone.ts` (`ratioToneForOperationalMeter`) | Graphe produit : **aucun** import ; seul importeur = `structureOperationalStripRatioTone.test.ts`. UI strip utilise `drawPoolToneToRatioGaugeTone` / `operationalStripPoolTone` dans `StructurePhaseOperationalStrip.tsx` et jauges associées. | **Lot optionnel** : (a) brancher la fonction si elle formalise une règle produit manquante, ou (b) retirer module + test si redondant avec le pipeline pool-tone existant — **décision produit/UX**, pas nettoyage aveugle. |

### Faux positif / usage indirect (conservé)

| Id | Artefact | Motif |
| --- | --- | --- |
| **S2-FP-01** | `pages/competition/competitionOverviewPageTestHelpers.tsx` | Helpers Vitest ; atteignable depuis `CompetitionOverviewPage.*.test.tsx` uniquement. |
| **S2-FP-02** | `pages/structure/structureMatchProductionPresentation.testFixtures.ts` | Fixtures recette / tests structure match production. |
| **S2-FP-03** | `pages/structure/structureMatchProductionPresentation.testPayloads.ts` | Payloads hub pour tests strip / page / recette. |
| **S2-FP-04** | `pages/structure/structurePresentedIssueCodes.fixture.ts` | Fixture `presentedIssueCodes` (contrats Host). |
| **S2-FP-05** | `vite-env.d.ts` | Références ambient Vite ; hors graphe ESM attendu. |
| **S2-FP-06** | Routes `/dev/foundations`, `/design-lab` | Lazy-loaded ; **dans** le graphe produit — terrains volontaires, pas dead code. |
| **S2-FP-07** | Pages legacy routées (`classements`, `stage/*`) | Toujours déclarées dans `App.tsx` — dette produit éventuelle, **pas** orphan module. |
| **S2-FP-08** | Barrel `src/ui.tsx`, `src/api.ts`, `src/types.ts` | Fortement importés ; pas de candidat DS orphelin. |

### À investiguer (aucune suppression)

| Id | Sujet | Pourquoi |
| --- | --- | --- |
| **S2-INV-01** | **S2-001** (voir ci-dessus) | Un test unitaire documente un comportement (`attention` + `filled=0` → `partial`) non branché en prod — risque soit **spec en avance**, soit **code mort** ; comparer sémantique avec `structureOperationalCopy.ts` / `operationalStripPoolTone` avant toute suppression. |
| **S2-INV-02** | Exports nommés peu référencés | Pas de scan exhaustif export-level dans S2 ; `typecheck` ne couvre pas « export jamais importé ailleurs ». Lot ultérieur seulement si douleur mesurée. |
| **S2-INV-03** | `frontend:audit` — 100 classes CSS sans match texte | **Reporté S4** — peut être DS modifiers, classes dynamiques, design-lab. |

---

## Arbitrage demandé (fin de stream S2)

1. **S2-001** — choisir **brancher** vs **retirer** vs **conserver** (spec isolée) ; si retrait, lot dédié avec relecture UX strip.
2. **Organisation fixtures** (hors S2 correctif) — option S5 : déplacer `*TestHelpers*` / `*.fixture.ts` sous `src/test/` pour clarifier le graphe — **pas** obligatoire.
3. **S3** ensuite — i18n repo (`i18n:audit:repo`) sans supprimer sur heuristique.
4. **S4** ensuite — classes CSS et blocs dupliqués.

**Critère de fin S2 (atteint pour l’audit):** liste qualifiée + actions proposées ci-dessus — **pas** de nettoyage global.

---

## Références

- Politique outils : [`phase-6-s1-lint-ci-arbitration.md`](phase-6-s1-lint-ci-arbitration.md)
- Wave 1 : [`phase-6-wave-1-static-baseline.md`](phase-6-wave-1-static-baseline.md)
- Contributor : `src/PlayUp/web/docs/conventions.md` (§ CSS tooling)
