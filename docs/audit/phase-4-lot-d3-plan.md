# Phase 4 — Lot D3 (AUD-0004) — Réserve produit Cup C2

**Launched:** 2026-10-22 · **Mode:** **arbitrage produit** — **pas d’implémentation** (guard / domaine / HTTP) tant que le contrat n’est pas figé.

**Phase 3 (lot C3) delivered:** tests scindés A3/A5, `cas assign` exclu, registre **AUD-0004 → Vérifié** pour le périmètre technique. La **réserve produit** reste ouverte (`phase-3-closure.md`).

**Pilotage:** traiter **Q1–Q3 ensemble** (même parcours Cup C2 : motif de blocage, règle de démarrage, cas assign).

---

## 1. Arbitrage produit validé (2026-10-22)

**Fiche Notion :** [Contrat Q1–Q3](https://www.notion.com/p/40075fb92d1081979ed7c15846a62c0b).

| Question | Décision |
| --- | --- |
| **Q1** | Contrat normatif **`IncompleteEntryRound`** @ 1/2 sans assign — SPA alignée, sans substituer `UnmaterializedConfrontations`. |
| **Q2** | Règle impasse @ 2/2 mat. eligible **indépendante** des runs ; backend A5 puis SPA A4–A5. |
| **Q3** | **Option A** — assign @ 1/2 **hors contrat D3-B** (pas guard/domaine ; futur = contrat distinct). |

**Vérification backend (2026-10-22) :** `dotnet test … --filter CupC2Impasse` → **2/2** (A3 + A5). **SPA A3 / A4–A5 :** à rejouer avant go D3-B.

**Interdit dans D3-B :** modifier `StageStartReadinessGuard`, règles Cup domaine, ou couvrir **cas assign** (Q3-A).

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
- [x] Validation décideur Q1–Q2 + **Q3-A** (Notion).
- [x] Tests backend référence A3 + A5 (2026-10-22).
- [ ] Rejouage SPA RECETTE C2 A3 + A4–A5 ; écarts listés.
- [ ] **Go explicite D3-B** (décideur).
- [ ] D3-B : lot correctif borné (read-model / UX / docs / tests — **pas** guard/domaine sauf nouveau arbitrage).

---

## 6. Vérification (inchangé)

```bash
dotnet test tests/MyClub.PlayUp.Development.Tests --filter FullyQualifiedName~CupC2Impasse -c Release
```

Attendu : **2** tests verts (`C2_A3_*`, `C2_A5_*`).
