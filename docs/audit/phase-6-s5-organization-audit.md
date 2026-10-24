# Phase 6 — S5 — Organisation dépôt (audit)

**Mode:** audit only — **aucun** déplacement de fichier, **aucun** refactor d’arborescence.  
**Décision associée (clôture phase 6) :** S4-INV-01…04 **différés** — dette qualifiée, pas de micro-lot CSS.

---

## 1. Conventions actuelles (Play’Up web)

| Zone | Rôle | Config |
| --- | --- | --- |
| `src/**/*.test.{ts,tsx}` | Tests Vitest colocalisés | `vite.config.ts` → `setupFiles: src/test/setup.tsx` ; `exclude: e2e/**` |
| `src/test/` | Utilitaires partagés tests (`renderWithI18n`, fixtures overview/structure) | `tsconfig.vitest.json` inclut `src/test/**/*` |
| `src/pages/**/__fixtures__/*.json` | Données figées recette / assembler (import JSON) | Résolution bundler ; chemins relatifs dans tests voisins |
| `scripts/*.mjs` | Audits report-only phase 6 | `package.json` ; soumis à `oxlint` |
| `e2e/` | Playwright (hors Vitest) | Job CI séparé |

**Pas de** alias `@/test` : imports relatifs `../test/renderWithI18n` depuis pages/shell.

---

## 2. Helpers / fixtures hors `src/test/` (inventaire)

| Fichier | Consommateurs | Atteignable depuis `main.tsx` | Note S5 |
| --- | --- | ---: | --- |
| `pages/competition/competitionOverviewPageTestHelpers.tsx` | 2 tests Overview | Non | Nom explicite ; colocalisé page |
| `pages/structure/structureMatchProductionPresentation.testFixtures.ts` | 3 tests + recette | Non | |
| `pages/structure/structureMatchProductionPresentation.testPayloads.ts` | 4 tests | Non | |
| `pages/structure/structurePresentedIssueCodes.fixture.ts` | 4 tests structure | Non | Suffixe `.fixture.ts` |
| `pages/structure/__fixtures__/matchProduction/*.json` | 2 tests assembler/strip | Non | Proche du domaine match production |
| `pages/structure/__fixtures__/populationRecette/*.json` | `structurePopulationRecette.test.ts` | Non | Protocole recette U1 |
| `pages/structure/structureRegulationDigest.build.tsx` | **Produit** (`StructurePhaseRegulationStrip`, `structureRegulationDigest.tsx`) | **Oui** | **Pas** un helper test — nom trompeur pour le graphe « test-only » |

**Vérification couplage produit → test :** aucun import d’un fichier `*TestHelpers*` / `*.testFixtures` / `*.fixture.ts` depuis du TSX produit (hors tests). OK.

---

## 3. Option « tout regrouper sous `src/test/` »

| Pour | Contre |
| --- | --- |
| Graphe `frontend:module-graph` plus lisible (5 modules « orphelins » disparaissent du côté pages) | Perte de proximité domaine (structure/competition) |
| Convention unique fixtures | Déplacement = **N** imports à mettre à jour ; risque oublier JSON `__fixtures__` |
| Aligné avec `overviewFixtures.ts` / `structureTestFixtures.ts` existants | `structureRegulationDigest.build.tsx` **ne doit pas** y aller (prod) |

**Checklist lot futur (non exécuté en S5) :**

1. Lister imports (grep) pour chaque fichier déplacé.
2. Conserver les JSON à côté des builders ou sous `src/test/fixtures/structure/…` avec imports explicites.
3. Rejouer `npm run test:run` + tests concernés (Overview, Structure strip/page, recette).
4. Vérifier `typecheck:tests` / `tsconfig.vitest.json` (déjà couvre `src/test/**`).
5. Pas d’impact Playwright sauf si e2e importait ces modules (non constaté).

**Arbitrage S5 :** **ne pas déplacer** en phase 6 — bénéfice structurel modéré vs risque de churn ; dette **acceptée** (colocalisation).

---

## 4. Fichiers volumineux (signal, pas action)

Seuils indicatifs @ audit (lignes, hors tests) :

| Lignes | Fichier |
| ---: | --- |
| 1649 | `StructurePhaseOperationalStrip.tsx` |
| 1644 | `CompetitionOverviewPage.tsx` |
| 1491 | `StructurePhaseFiche.tsx` |
| 1299 | `phaseSchematic.tsx` |

Découpage = chantier produit / lisibilité — **hors** phase 6 (pas de refactor autorisé).

---

## 5. Scripts audit & documentation

- Inventaire report-only : `conventions.md` (§ CSS tooling + module graph + i18n qualify + css qualify).
- Gates CI inchangés : S1 ([`phase-6-s1-lint-ci-arbitration.md`](phase-6-s1-lint-ci-arbitration.md)).

---

## 6. Stream S6 (tests / garde-fous)

**Non approfondi** en phase 6 au-delà de la vague 1 :

- CI web : `test:run` (Vitest) ; pas de seuil `test:coverage`.
- CI .NET : `dotnet test` + warnings baseline (D1).
- Audits heuristiques **non** ajoutés à la CI.

**Dette acceptée** : pas de nouveau garde-fou automatisé sur couverture ou audits repo-wide.

---

## Critère de fin S5

- [x] Inventaire helpers/fixtures + risques couplage.
- [x] Décision explicite : **pas** de déplacement en phase 6.
- [x] S6 noté comme inchangé / accepté.
