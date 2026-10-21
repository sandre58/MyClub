# Phase 4 — Lot D3 (AUD-0004) — Réserve produit Cup C2

**Launched:** 2026-10-22 · **Mode:** **arbitrage produit** — **pas d’implémentation** (guard / domaine / HTTP) tant que le contrat n’est pas figé.

**Phase 3 (lot C3) delivered:** tests scindés A3/A5, `cas assign` exclu, registre **AUD-0004 → Vérifié** pour le périmètre technique. La **réserve produit** reste ouverte (`phase-3-closure.md`).

**Pilotage:** traiter **Q1–Q3 ensemble** (même parcours Cup C2 : motif de blocage, règle de démarrage, cas assign).

---

## 1. Recommandation d’arbitrage produit (2026-10-22)

Orientation proposée par le décideur — **à figer sur fiche Notion** avant autorisation **D3-B**.

| Question | Décision recommandée | Justification |
| --- | --- | --- |
| **Q1 — A3 : motif de blocage** | Aligner read-model et UX sur un **motif métier explicite**, sans masquer la distinction entre états backend. | L’utilisateur doit comprendre pourquoi Start est bloqué ; la SPA ne doit pas interpréter un code backend comme un autre sans contrat explicite. |
| **Q2 — A4/A5 : Start @ 2/2 mat.** | Définir un **contrat produit unique**, puis **confronter** le run SPA (2026-10-17) et le protocole backend (A5) à ce contrat — **ne pas** déclarer l’une des preuves vraie par défaut. | Un run SPA isolé ne suffit pas ; un test backend seul ne prouve pas le parcours utilisateur. |
| **Q3 — assign @ 1/2** | **Hors lot D3-B** tant que le statut produit n’est pas établi : qualifier explicitement scénario **supporté** vs **hors contrat**. | Éviter d’élargir le périmètre ou d’ajouter un comportement implicite au domaine. |

**Interdit à ce stade :** modifier `StageStartReadinessGuard`, règles Cup domaine, ou contrats HTTP.

---

## 2. Conditions de passage à D3-B

Pour **chaque** question (Q1–Q3), la fiche Notion doit figer avant tout code :

| Champ | Contenu |
| --- | --- |
| Règle métier attendue | … |
| Comportement API / read-model | … |
| Résultat visible SPA | … |
| Test de référence + critère de réussite | … |
| Cas explicitement hors périmètre | … |

**D3-B** corrige **uniquement les écarts confirmés** — pas de refonte tirage / matérialisation.

---

## 3. État des lieux (référence)

| # | Sujet | Constat technique (sans trancher produit) |
| --- | --- | --- |
| Q1 | A3 SPA vs backend | SPA peut montrer `UnmaterializedConfrontations` ; protocole @ 1/2 sans assign → `StartBlockedIncompleteEntryRound` (guard 1a). |
| Q2 | A4/A5 | Run SPA 2026-10-17 : Start enabled à 2/2 mat. ; protocole post-C3 + `CupC2ImpasseRecetteProtocolTests` A5 : impasse `IncompleteEntryRound` après tout eligible mat. |
| Q3 | Cas assign | Hors protocole C3 ; qualification produit en attente. |

---

## 4. Sources (Git)

| Artefact | Rôle |
| --- | --- |
| `docs/design/structure-lifecycle/cup-c2-spa-recette-checklist.md` | Track A/B SPA + sign-off |
| `docs/design/structure-lifecycle/cup-materialize-recette-protocol.md` | Protocole backend |
| `tests/.../CupC2ImpasseRecetteProtocolTests.cs` | Preuves A3 / A5 |
| `docs/audit/phase-3-lot-c3-plan.md` | Périmètre C3 |

**Notion :** [Arbitrage 3 — AUD-0004](https://www.notion.com/p/40075fb92d1081cdb0f8d655d2e93c68) · **[Contrat produit Q1–Q3 (proposition)](https://www.notion.com/p/40075fb92d1081979ed7c15846a62c0b)** · [Décision Cup C2 scinder](https://www.notion.com/p/40075fb92d108156a85df51720fefbbe).

---

## 5. Livrables D3

- [x] Plan d’arbitrage + recommandation Q1–Q3 (this file).
- [x] Proposition de contrat Notion (5 champs × Q1–Q3) — [fiche](https://www.notion.com/p/40075fb92d1081979ed7c15846a62c0b).
- [ ] Validation décideur Q1–Q2 + **tranche Q3** (supporté vs hors contrat).
- [ ] Contrat figé = prérequis D3-B.
- [ ] **Autorisation explicite D3-B** (décideur).
- [ ] D3-B : lot correctif borné (read-model / UX / docs / tests — **pas** guard/domaine sauf nouveau arbitrage).

---

## 6. Vérification (inchangé)

```bash
dotnet test tests/MyClub.PlayUp.Development.Tests --filter FullyQualifiedName~CupC2Impasse -c Release
```

Attendu : **2** tests verts (`C2_A3_*`, `C2_A5_*`).
