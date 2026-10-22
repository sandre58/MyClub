# Phase 7 — Sécurité, performance & exploitation (closure)

**Status:** closed (pilotage) — 2026-10-22  
**Go phase 7:** vague 1 investigation — 2026-10-22  
**Go clôture:** 2026-10-22  

| Field | Value |
| --- | --- |
| **Baseline vague 1** | [`phase-7-wave-1-security-perf-ops-baseline.md`](phase-7-wave-1-security-perf-ops-baseline.md) @ `a6764084` |
| **Livrables (closure SHA)** | `main` @ `e096b386` (pilotage + lots livrés ; code technique jusqu’à `59506db2`) |
| **Plan** | [`phase-7-plan.md`](phase-7-plan.md) |
| **Notion** | [Phase 7 — Sécurité, perf & exploitation](https://www.notion.com/p/40175fb92d1081509b4fdd89b6490867) |

---

## Streams — bilan

| Stream | Résultat | Livrable |
| --- | --- | --- |
| **Vague 1** | Baseline Sec / Perf / Ops (mesures, pas correctifs) | [`phase-7-wave-1-security-perf-ops-baseline.md`](phase-7-wave-1-security-perf-ops-baseline.md) |
| **P7-Sec** | AUD-0008 trajectoire C **partielle** (C1 déjà phase 3 + lot C + **C2**) | [`phase-7-lot-sec-aud-0008-c.md`](phase-7-lot-sec-aud-0008-c.md), [`phase-7-lot-sec-aud-0008-c2.md`](phase-7-lot-sec-aud-0008-c2.md) |
| **P7-Perf** | AUD-0012 D2-B lot 2 ; bundle Vite (lazy routes) | [`phase-7-lot-perf-aud-0012.md`](phase-7-lot-perf-aud-0012.md), [`phase-7-lot-perf-vite-bundle.md`](phase-7-lot-perf-vite-bundle.md) |
| **P7-Ops** | Health `/health` ; Host E2E draw + materialize (API) | [`phase-7-lot-ops.md`](phase-7-lot-ops.md), [`phase-7-lot-ops-materialize-e2e.md`](phase-7-lot-ops-materialize-e2e.md) |

---

## Critères d’acceptation phase 7 (§3 plan) — statut

| Critère | Statut |
| --- | --- |
| Vague 1 baseline publiée | ✅ |
| Streams livrés / dette / report documentés | ✅ (ci-dessous) |
| Lots un par go + recette / tests | ✅ |
| Pas de gate heuristique phase 6 ajoutée en CI | ✅ |
| CI verte sur SHA de clôture | ⚠️ **À vérifier** sur `main` (push / CI distante) |

---

## Décisions & dettes maintenues

| Id / sujet | Statut post-P7 |
| --- | --- |
| **AUD-0008** auth + TLS + S6 (hébergement) | **Dette acceptée** — décision Notion inchangée ; C/C2 = bind + `AllowedHosts` Production + doc |
| **AUD-0012** | **Dette acceptée** — mitigations D2-B (attachment batch + multi-stage hydration) ; pas de clôture registre |
| **AUD-0009**, **AUD-0010** | Dette acceptée (inchangé) |
| **F5**, **AUD-0004**, **S4-INV**, CSS/i18n mass | Hors P7 (inchangé phase 6) |
| **Draw / materialize UI E2E** | Signal résiduel — slices API Host livrées ; profondeur UI non couverte |

---

## Lots correctifs exécutés (référence Git)

| Lot | Commit indicatif |
| --- | --- |
| AUD-0008-C (bind warn, doc, T1–T5) | `5dc2bd1f` … `3470deeb` |
| AUD-0012 D2-B lot 2 | `bafa6cd4` |
| P7-Ops (health, draw E2E) | `46b17644` |
| Bundle Vite | `310adf98` |
| Materialize E2E | `13e732f4` |
| AUD-0008-C2 | `59506db2` |

---

## Suite chantier

- **Phase 8** (rejeu global audit) ou chantier produit — arbitrage roadmap.
- **AUD-0008** : lot auth/TLS/S6 **uniquement** après arbitrage produit (identité, topologie proxy, Media).
- **AUD-0012** : lots D2-B futurs **uniquement** avec hypothèse + `PerformanceBaseline`.
- **E2E** : extension UI draw/materialize si priorité QA.

Bilan dettes consolidé mis à jour : [`phase-7-debts-consolidated.md`](phase-7-debts-consolidated.md).
