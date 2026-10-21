# Phase 4 — Lot D3 (AUD-0004) — Réserve produit Cup C2

**Launched:** 2026-10-22 · **Mode:** **arbitrage produit** avant tout changement domaine / read-model / protocole.

**Phase 3 (lot C3) delivered:** tests scindés A3/A5, `cas assign` exclu, registre **AUD-0004 → Vérifié** pour le périmètre technique. La **réserve produit** reste ouverte (voir `phase-3-closure.md`).

---

## 1. Questions ouvertes (décision métier requise)

| # | Sujet | État actuel | Décision attendue |
| --- | --- | --- | --- |
| **Q1** | **Checklist SPA A3** vs **backend** | SPA peut exposer `UnmaterializedConfrontations` sur le lifecycle ; protocole backend @ 1/2 sans assign manuel → `StartBlockedIncompleteEntryRound` (guard 1a). | Aligner **copy UX** uniquement, **read-model** (codes / intents), ou **accepter l’écart** documenté ? |
| **Q2** | **Parcours SPA A4 historique** | Run SPA 2026-10-17 : Start **enabled** à 2/2 mat. ; checklist / backend post-C3 : impasse **A5** (`IncompleteEntryRound` après tout eligible mat.). | Quelle vérité produit pour « peut-on démarrer quand le bracket n’est pas complet » ? |
| **Q3** | **Cas assign** | Assign manuel avant premier Start @ 1/2 — **hors** tests protocole ; qualifié « future decision » (C3, Arbitrage 3). | Qualifier le contrat (oui/non/partiel) avant test dédié et éventuelle évolution guard / domaine ? |

**Hors D3 sans nouveau arbitrage :** modifier `StageStartReadinessGuard`, règles Cup domaine, ou contrats HTTP.

---

## 2. Sources de vérité (Git)

| Artefact | Rôle |
| --- | --- |
| `docs/design/structure-lifecycle/cup-c2-spa-recette-checklist.md` | Track A/B SPA + sign-off |
| `docs/design/structure-lifecycle/cup-materialize-recette-protocol.md` | Protocole backend |
| `tests/.../CupC2ImpasseRecetteProtocolTests.cs` | Preuves exécutables A3 / A5 |
| `docs/audit/phase-3-lot-c3-plan.md` | Périmètre C3 / exclusions |

**Notion :** [Arbitrage 3 — AUD-0004](https://www.notion.com/p/40075fb92d1081cdb0f8d655d2e93c68) · [Décision Cup C2 scinder](https://www.notion.com/p/40075fb92d108156a85df51720fefbbe).

---

## 3. Options indicatives (non décidées ici)

| Option | Q1–Q2 | Q3 cas assign |
| --- | --- | --- |
| **A — Statu quo documenté** | Garder écart SPA/backend explicite dans checklist ; pas de changement code | Cas assign reste hors scope |
| **B — Alignement read-model** | Harmoniser codes / disabled reasons côté structure read pour coller au guard | Inchangé jusqu’à décision assign |
| **C — Évolution métier** | Revoir règles Start / materialize / bracket | Définir protocole assign @ 1/2 + tests |

Choisir **une** orientation par question ; D3-B = lot correctif **après** validation écrite (Notion Décision ou ligne registre).

---

## 4. Livrables D3

- [x] Plan d’arbitrage (this file) + pilotage Notion phase 4.
- [ ] **Décision décideur** sur Q1–Q3 (Notion).
- [ ] D3-B (si autorisé) : alignement checklist / read-model / tests — **un** lot, périmètre borné par la décision.

---

## 5. Vérification (état actuel, sans changement)

```bash
dotnet test tests/MyClub.PlayUp.Development.Tests --filter FullyQualifiedName~CupC2Impasse -c Release
```

Attendu : **2** tests verts (`C2_A3_*`, `C2_A5_*`).
