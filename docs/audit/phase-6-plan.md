# Phase 6 — Qualité statique et organisation du dépôt (launch)

**Roadmap (plan directeur):** warnings et contrôles retenus · **code mort** backend/frontend · références périmées · **contrôles automatisables** (dead code, i18n, CSS) · arborescence · commentaires · tests.

**Reprise:** `main` @ `4b6b3fe7` (merge PR #72, post phase 5).

**Pilotage Notion:** [Phase 6 — Qualité statique et organisation du dépôt](https://www.notion.com/p/40175fb92d108127a553e39280cb77ab).

**Go décideur:** 2026-10-22.

**Phase 5 close:** [`phase-5-closure.md`](phase-5-closure.md).

---

## 1. Scope

### In scope (phase 6)

| Stream | Sujet | Mode |
| --- | --- | --- |
| **Vague 1** | Baseline contrôles .NET + web + inventaire scripts audit / warnings | Investigation — **no app code** |
| **S1** | Politique warnings — suites retenues vs dettes (suite D1, oxlint, stylelint, chunk Vite) | Arbitrage **puis** lots ciblés |
| **S2** | Code mort / unused — backend (analyseurs, grep) + frontend (`frontend:audit`, oxlint) | Investigation **puis** lots validés |
| **S3** | i18n repo-wide — `i18n:audit:repo`, parity, clés dynamiques | Investigation |
| **S4** | CSS / DS — `frontend:audit`, `css:audit:rider`, stylelint | Investigation |
| **S5** | Organisation dépôt — arborescence, fichiers volumineux, commentaires pilotage | Investigation |
| **S6** | Tests / garde-fous — couverture parcours, scripts CI manquants | Investigation |

### Out of scope (unless new arbitration)

| Item | Reason |
| --- | --- |
| Domaine métier / persistance | Phase 4 close |
| Refonte produit Structure | Hors audit correctif |
| Phase 7 sécurité / perf / exploitation | Roadmap phase 7 |
| Phase 8 rejeu global | Clôture chantier |
| F5 phase 5 (`presentedIssueCodes`) | Reporté — lot dédié si arbitrage produit |

---

## 2. Lots

**Aucun lot correctif autorisé** tant qu’il n’est pas listé sur la fiche Notion phase 6 après vague 1 / arbitrage.

| Lot | Prérequis | Livrable attendu |
| --- | --- | --- |
| **Vague 1** | Go phase 6 | `phase-6-wave-1-static-baseline.md` |
| **S1** | Arbitrage lint CI | [`phase-6-s1-lint-ci-arbitration.md`](phase-6-s1-lint-ci-arbitration.md) |
| **T1** | Test Application rouge (hors stream S2–S6) | [`phase-6-t1-application-test-findings.md`](phase-6-t1-application-test-findings.md) |
| **S2** | Dead code FE (qualification) | [`phase-6-s2-dead-code-fe.md`](phase-6-s2-dead-code-fe.md) |
| **S3** | i18n repo (qualification) | [`phase-6-s3-i18n-qualification.md`](phase-6-s3-i18n-qualification.md) |
| **S4+** | CSS repo, autres streams | TBD |

---

## 3. Critères de fin phase 6 (pilotage)

- Vague 1 baseline publiée.
- Chaque stream S1–S6 : **livré**, **dette acceptée**, ou **reporté** (registre / Notion).
- Contrôles retenus documentés ; pas de suppression code/i18n/CSS sans preuve.
- CI verte sur SHA de clôture.

---

## 4. Progress

- [x] Go phase 6 (décideur, 2026-10-22).
- [x] Plan Git (this file).
- [x] Vague 1 — [`phase-6-wave-1-static-baseline.md`](phase-6-wave-1-static-baseline.md) @ `4b6b3fe7`.
- [x] **S1** — arbitrage lint CI + fix oxlint on `css-audit-rider.mjs`.
- [x] **T1** — test collector aligné D3-B (`6aa23992`), pas de changement produit.
- [x] **S2** — qualification dead code FE ; **S2-001** retiré (module + test).
- [x] **S3** — qualification i18n repo (buckets, pas de suppression).
- [ ] Arbitrage S3-INV (5 clés regulation) / streams S4–S6.

---

## 5. Vérification rapide (référence)

```bash
# .NET
dotnet build -c Release
dotnet test -c Release --no-build

# Web
cd src/PlayUp/web
npm run build
npm run lint
npm run lint:css
npm run i18n:check
npm run frontend:audit
npm run css:audit:rider
```
