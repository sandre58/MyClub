# Bilan consolidé — dettes & signaux (phases 1 → 6)

**Usage:** alimenter le **cadrage phase 7** — distinguer **confirmé** (preuve / registre / décision) vs **signal** (heuristique, warning, gap de couverture).  
**Registre Notion:** [Registre des findings](https://app.notion.com/p/ec992f0e07134ac2a11a837e5539e45c).  
**Dernière mise à jour:** 2026-10-22 (post clôture phase 6 @ `940935b0`).

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
| **AUD-0008** trajectoire C (auth / TLS avant exposition Internet) | Reporté | **Confirmé** (décision A+C phase 2) | Phase 3 closure, Notion Décision | **Candidat sécurité P7** — pas d’impl sans go |
| **AUD-0009** tests archi étendus | Dette acceptée | Confirmé | Phase 3 closure | Hors P7 par défaut |
| **AUD-0010** taille `UseCaseExecutor.Stages` | Dette acceptée | Confirmé | Phase 3 closure | **Pas** refactor opportuniste P7 |
| **AUD-0012** `StageRepository` / Include EF | Dette acceptée (+ mitigation D2-B) | Confirmé | Phase 4 D2, registre | **Perf P7** seulement après **mesure** repro |
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
| **Vite chunk** > 500 kB (warning) | Signal | Signal perf | Wave 1 / F5 reconciliation | **Candidat perf P7** — vague 1 mesure |
| **Heuristiques audit** (`frontend:audit`, etc.) | Politique | Confirmé (S1) | `phase-6-s1-lint-ci-arbitration.md` | **Report-only** — inchangé |
| **S6** garde-fous (coverage, audits CI) | Dette acceptée | Confirmé | `phase-6-closure.md` | Pas d’extension P7 sans arbitrage |

---

## Couverture & exploitation (signaux transverses)

| Sujet | Type | Source | Note phase 7 |
| --- | --- | --- | --- |
| Draw E2E (chemin critique #3) | Signal gap | `critical-paths.md` | **Candidat ops/QA P7** — lot borné, pas « perf » |
| Materialize E2E (chemin #2) | Signal gap | idem | Idem |
| Phase 8 rejeu global chantier | Roadmap | Plans phase 6 | **Après** P7 — hors cadrage immédiat |

---

## Explicitement **non** candidats phase 7 par défaut

(Aligné décideur 2026-10-22.)

- S4-INV, doublons CSS, i18n mass-delete, déplacement fixtures, découpage fichiers volumineux.
- F5 et AUD-0004 sans arbitrage produit dédié.
- Refactor architecture (AUD-0010) ou CSS/DS opportuniste.

---

## Synthèse pour priorisation P7

| Stream roadmap (plan directeur) | Entrées **confirmées** plausibles | Entrées **signaux** à baseline d’abord |
| --- | --- | --- |
| **Sécurité** | AUD-0008 trajectoire C | Surface Host, secrets, headers (à inventorier vague 1) |
| **Perf** | AUD-0012 (si protocole mesure) | Bundle Vite, hot paths API (à inventorier) |
| **Exploitation** | — | Health/readiness, logs, parcours E2E métier manquants |
