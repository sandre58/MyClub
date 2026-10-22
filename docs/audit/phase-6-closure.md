# Phase 6 — Qualité statique et organisation du dépôt (closure)

**Status:** closed (pilotage) — 2026-10-22  
**Go phase 6:** 2026-10-22  
**Go clôture:** 2026-10-22 (S5 audit + dettes S4-INV / S6 acceptées)

| Field | Value |
| --- | --- |
| **Baseline** | Vague 1 @ `4b6b3fe7` (main post PR #72) |
| **Livrables** | `main` @ `6df21d3d` |
| **Notion** | [Phase 6 — Qualité statique](https://www.notion.com/p/40175fb92d108127a553e39280cb77ab) |

---

## Streams — bilan

| Stream | Résultat | Livrable |
| --- | --- | --- |
| Vague 1 | Baseline .NET + web + scripts | [`phase-6-wave-1-static-baseline.md`](phase-6-wave-1-static-baseline.md) |
| **S1** | Arbitrage lint CI ; fix oxlint `css-audit-rider.mjs` | [`phase-6-s1-lint-ci-arbitration.md`](phase-6-s1-lint-ci-arbitration.md) |
| **T1** | Test Application stale vs D3-B ; alignement test | [`phase-6-t1-application-test-findings.md`](phase-6-t1-application-test-findings.md) |
| **S2** | Qualification dead code FE ; retrait **S2-001** | [`phase-6-s2-dead-code-fe.md`](phase-6-s2-dead-code-fe.md) |
| **S3** | Qualification i18n ; lot **S3-INV** (5 clés regulation) | [`phase-6-s3-i18n-qualification.md`](phase-6-s3-i18n-qualification.md), [`phase-6-s3-inv-arbitration.md`](phase-6-s3-inv-arbitration.md) |
| **S4** | Qualification CSS (100 classes, 80 doublons) | [`phase-6-s4-css-qualification.md`](phase-6-s4-css-qualification.md) |
| **S5** | Audit organisation ; **pas** de déplacement fichiers | [`phase-6-s5-organization-audit.md`](phase-6-s5-organization-audit.md) |
| **S6** | **Dette acceptée** — pas de nouveau garde-fou (voir S5 §6) | — |

---

## Décisions & dettes acceptées

| Id | Décision |
| --- | --- |
| **Outils** | Heuristiques (`frontend:audit`, `i18n:audit:repo`, `frontend:module-graph`, `frontend:css:qualify`, …) **report-only** — pas de gate CI par défaut (S1). |
| **S4-INV-01…04** | **Différés** — dette qualifiée ; pas de suppression CSS ni refactor global ; absence de littéral TSX ≠ preuve d’inutilité. |
| **80 groupes CSS dupliqués** | Dette DRY **optionnelle** — pas de lot implicite. |
| **i18n ~676 suspicious** | Majorité faux positifs (pluriels, chemins relatifs) — pas de nettoyage de masse. |
| **Helpers colocalisés** | Conserver sous `pages/**` ; déplacement vers `src/test/` **non** fait (S5). |
| **Vite chunk warning** | Informatif — pas de gate. |
| **F5 phase 5** | Toujours hors phase 6 (`presentedIssueCodes` / frontière BE-FE). |

---

## Lots correctifs exécutés en phase 6

- S1 : oxlint sur `css-audit-rider.mjs`.
- T1 : test `StructurePresentedAttentionCollector` (D3-B).
- S2-001 : retrait `structureOperationalStripRatioTone` + test ; garde-fou `structureOperationalCopy.test.ts`.
- S3-INV : 5 clés `regulation` orphelines supprimées (fr+en).

**Pas** de lot CSS global, **pas** de refactor arborescence, **pas** de gate audit heuristique.

---

## Outils npm (référence)

```bash
cd src/PlayUp/web
npm run frontend:module-graph
npm run frontend:css:qualify
npm run i18n:audit:qualify
npm run i18n:audit:repo    # report-only
npm run frontend:audit
npm run css:audit:rider
```

Contributor : `src/PlayUp/web/docs/conventions.md`.

---

## Lots futurs (hors clôture — arbitrage séparé)

- S4-INV-01…04 (CSS ciblé + recette visuelle).
- Déplacement optionnel fixtures → `src/test/` (checklist S5).
- Renommage `structureRegulationDigest.build.tsx` (clarifier prod vs test).
- Phase 7+ roadmap (sécurité, perf, exploitation).

---

## Suite chantier

**Phase 7** — cadrage (préparation, sans go impl) : [`phase-7-plan.md`](phase-7-plan.md) · [`phase-7-debts-consolidated.md`](phase-7-debts-consolidated.md) · [Notion](https://www.notion.com/p/40175fb92d1081509b4fdd89b6490867).
