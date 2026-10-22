# Phase 7 — Sécurité, performance & exploitation (cadrage)

**Status:** preparation — **pas de go implémentation** (2026-10-22)  
**Roadmap (plan directeur):** sécurité · performance · exploitation (distinct de l’assainissement statique phase 6).

**Reprise technique:** `main` @ `940935b0` (post clôture phase 6).  
**Bilan dettes:** [`phase-7-debts-consolidated.md`](phase-7-debts-consolidated.md).  
**Phase 6 close:** [`phase-6-closure.md`](phase-6-closure.md).  
**Pilotage Notion:** [Phase 7 — Sécurité, perf & exploitation (préparation)](https://www.notion.com/p/40175fb92d1081509b4fdd89b6490867).

---

## 0. Séparation cadrage / exécution

| Étape | Autorisé maintenant | Interdit sans go explicite |
| --- | --- | --- |
| **Cadrage** (ce document + Notion + bilan dettes) | Docs Git, fiche Notion, priorisation | Code produit, config prod, refactor |
| **Vague 1** (post go phase 7) | Investigation, baseline, inventaires | Lots correctifs |
| **Lots P7-x** | — | Un lot = périmètre + recette + critères d’arrêt **validés** |

---

## 1. Périmètre indicatif (à valider en arbitrage)

### In scope (candidats — **pas** engagés)

| Stream | Intention | Entrées registre / bilan |
| --- | --- | --- |
| **P7-Sec** | Exposition Host, auth/TLS, secrets, durcissement | **AUD-0008** trajectoire C |
| **P7-Perf** | Mesures puis optimisations **prouvées** | **AUD-0012** (si repro), bundle Vite (signal) |
| **P7-Ops** | Exploitation locale → cible déploiement, observabilité, parcours critiques | Gaps `critical-paths.md` (draw / materialize E2E) |

### Out of scope (sauf nouvel arbitrage)

| Exclusion | Raison |
| --- | --- |
| S4-INV, doublons CSS, i18n heuristique, fixtures, gros TSX | Dettes phase 6 **différées** — pas automatiques P7 |
| F5, AUD-0004 réserves produit | Métier / UX — phase ou décision dédiée |
| AUD-0009, AUD-0010 | Dette acceptée phase 3 |
| Phase 8 rejeu global | Après P7 |
| Gates heuristiques (`frontend:audit`, …) en CI | Politique phase 6 S1 |

---

## 2. Priorisation (grille — à arbitrer)

Pour chaque candidat retenu en vague 1, noter : **risque**, **impact utilisateur**, **coût**, **preuves**.

| Priorité suggérée | Sujet | Risque | Preuve actuelle |
| --- | --- | --- | --- |
| **P0** (cadrage) | Inventaire sécurité Host (AUD-0008 C) | Élevé si exposition publique | Décision Notion + phase 1 wave 5 |
| **P1** | Protocole mesure AUD-0012 | Moyen (latence DB) | Investigation D2 livrée, dette maintenue |
| **P2** | Baseline perf web (bundle, LCP proxy) | Moyen | Warning Vite seulement |
| **P2** | E2E métier draw / materialize | Moyen (régression) | Matrice `critical-paths.md` |
| **—** | CSS / orga / F5 | Faible ou hors axe | Clôture phase 5–6 |

**Décision attendue:** ordre des streams P7-Sec / Perf / Ops et **quel** sujet ouvre le **premier lot** (un seul).

---

## 3. Critères d’acceptation phase 7 (draft — à valider)

- Vague 1 baseline publiée (sécurité + perf + ops **mesurés**, pas supposés).
- Chaque stream : **livré**, **dette acceptée**, ou **reporté** (Notion + Git).
- Lots correctifs : **un par go** ; recette + critère d’arrêt documentés.
- Aucune extension des audits heuristiques phase 6 en gate CI sans arbitrage S1-like.
- CI verte sur SHA de clôture phase 7.

---

## 4. Plan Git (préparation — sans branche obligatoire)

| Livrable | Chemin | État |
| --- | --- | --- |
| Bilan dettes | `phase-7-debts-consolidated.md` | ✅ |
| Plan cadrage | `phase-7-plan.md` (this file) | ✅ |
| Vague 1 | `phase-7-wave-1-security-perf-ops-baseline.md` | ⏳ post go |
| Lots | `phase-7-lot-*.md` | ⏳ post arbitrage |

**Branche suggérée** au go phase 7 : `feature/audit-phase-7-launch` (docs + vague 1 uniquement jusqu’à arbitrage lot).

---

## 5. Dépendances

| Dépendance | Impact |
| --- | --- |
| Décision **AUD-0008** (Notion) | Cadre tout stream P7-Sec |
| Produit / hébergement cible | Périmètre TLS, auth, réseau |
| Postgres + DevRunner + Host | Baseline perf / E2E métier |
| Registre findings | Mise à jour statuts post-lots |

---

## 6. Progress

- [x] Bilan consolidé dettes phases 1–6.
- [x] Plan Git cadrage (sans code produit).
- [ ] Fiche Notion phase 7 validée (périmètre + exclusions).
- [ ] **Go décideur phase 7** (cadrage → vague 1).
- [ ] Arbitrage premier lot borné.
- [ ] Vague 1 baseline.
- [ ] Clôture phase 7.

---

## 7. Prochaine action recommandée

1. Arbitrer sur Notion : périmètre P7-Sec / Perf / Ops et **exclusions** ci-dessus.  
2. **Go phase 7** = autoriser **vague 1** investigation uniquement.  
3. Après vague 1 : soumettre **un** lot prioritaire (ex. AUD-0008 périmètre A, ou protocole AUD-0012, ou baseline bundle) — pas de lot parallèle sans décision.
