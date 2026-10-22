# Bilan consolidé — dettes & signaux (phases 1 → 6)

**Usage:** alimenter le **cadrage phase 7** — distinguer **confirmé** (preuve / registre / décision) vs **signal** (heuristique, warning, gap de couverture).  
**Registre Notion:** [Registre des findings](https://app.notion.com/p/ec992f0e07134ac2a11a837e5539e45c).  
**Dernière mise à jour:** 2026-10-22 (post **clôture phase 7** @ `47f2b014`).

---

## Légende

| Tag | Sens |
| --- | --- |
| **Confirmé** | Finding registre, décision Notion, ou défaut reproduit / lot livré |
| **Dette acceptée** | Arbitrage explicite — pas de lot sans nouveau go |
| **Signal** | Métrique, heuristique ou gap — **pas** un défaut confirmé |
| **Reporté** | Hors clôture de la phase d’origine — arbitrage produit ou phase ultérieure |

---

## Registre & phases correctives (1 → 4)

| Id / sujet | Statut | Type | Source | Note phase 7 |
| --- | --- | --- | --- | --- |
| **AUD-0008** trajectoire C (auth / TLS / S6) | Dette acceptée (mitigation bind + Production hosts) | **Confirmé** | Lots C + C2 phase 7, Notion | **Auth/TLS/S6** — arbitrage produit avant lot |
| **AUD-0009** tests archi étendus | Dette acceptée | Confirmé | Phase 3 closure | Hors P7 par défaut |
| **AUD-0010** taille `UseCaseExecutor.Stages` | Dette acceptée | Confirmé | Phase 3 closure | **Pas** refactor opportuniste P7 |
| **AUD-0012** `StageRepository` / Include EF | Dette acceptée (+ mitigation D2-B lots 1–2) | Confirmé | Phase 4 D2 + P7 lot perf | Nouveau D2-B **uniquement** avec hypothèse + baseline |
| **AUD-0005-C** warnings .NET | Partiellement traité (D1/D1-B) | Confirmé | Phase 4, baseline CI | Garde-fou en place — pas lot ouvert |
| **AUD-0004** réserve A3 / cas assign | Reporté produit | Confirmé (écart documenté) | Phase 3–4 D3 | **Métier** — pas stream sécurité/perf P7 par défaut |
| **AUD-0016** advisories npm dev (stylelint/braces) | Dette acceptée | Confirmé | Phase 3 C2 | Signal supply-chain **faible** sauf nouvelle politique |

---

## Phase 5 → 6 (produit & statique)

| Id / sujet | Statut | Type | Source | Note phase 7 |
| --- | --- | --- | --- | --- |
| **F5** `presentedIssueCodes` / frontière BE-FE | Reporté | Reporté produit | `phase-5-closure.md` | **Arbitrage métier** — pas lot P7 automatique |
| **S4-INV-01…04** CSS ciblés | Différé | Signal → qualifié | `phase-6-s4-css-qualification.md` | **Hors P7** sauf go explicite + recette |
| **80 groupes** décl. CSS dupliquées | Dette acceptée | Signal | S4 | **Hors P7** (DRY cosmétique) |
| **~676** clés i18n « suspicious » | Dette acceptée | Signal | S3 | **Hors P7** (heuristique) |
| **Helpers / fixtures** colocalisés | Dette acceptée | Organisation | S5 | **Hors P7** |
| **Gros composants** (>1,4k lignes) | Dette acceptée | Signal lisibilité | S5 | **Hors P7** (refactor produit) |
| **Vite chunk** > 500 kB (warning) | Atténué (entry ~284 kB min) | Signal → lot livré | [`phase-7-lot-perf-vite-bundle.md`](phase-7-lot-perf-vite-bundle.md) | Routes lazy ; poids total app inchangé |
| **Heuristiques audit** (`frontend:audit`, etc.) | Politique | Confirmé (S1) | `phase-6-s1-lint-ci-arbitration.md` | **Report-only** — inchangé |
| **S6** garde-fous (coverage, audits CI) | Dette acceptée | Confirmé | `phase-6-closure.md` | Pas d’extension P7 sans arbitrage |

---

## Couverture & exploitation (signaux transverses)

| Sujet | Type | Source | Note phase 7 |
| --- | --- | --- | --- |
| Draw E2E (chemin critique #3) | Slice Host API livrée | `critical-paths.md`, `cup-draw.spec.ts` | UI Structure draw — signal résiduel |
| Materialize E2E (chemin #2) | Slice Host API livrée | `groups-materialize.spec.ts` | UI materialize — signal résiduel |
| Health / readiness Host | Livré | `phase-7-lot-ops.md` | `GET /health` |
| Phase 8 rejeu global chantier | Roadmap | Phase 7 closure | **Prochain** arbitrage chantier |

---

## Explicitement **non** candidats phase 7 par défaut

(Aligné décideur 2026-10-22.)

- S4-INV, doublons CSS, i18n mass-delete, déplacement fixtures, découpage fichiers volumineux.
- F5 et AUD-0004 sans arbitrage produit dédié.
- Refactor architecture (AUD-0010) ou CSS/DS opportuniste.

---

## Synthèse post-clôture phase 7

| Stream | Bilan |
| --- | --- |
| **Sécurité** | AUD-0008 C + C2 livrés ; auth/TLS/S6 **reportés** |
| **Perf** | AUD-0012 lot 2 + bundle lazy routes |
| **Ops** | Health, draw + materialize E2E (API) |

Voir [`phase-7-closure.md`](phase-7-closure.md).
