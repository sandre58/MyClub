# SLICE-4 — G-OPS3-PROG-IMPACT-ENF

| Field | Value |
|-------|--------|
| **Status** | **Closed** (DM 2026-10-16) — P0–P6 livrés · garde `StructureProgressionImpactGate` · proxy Applied progression retiré (P5–P6) · [arbitrage](./g-ops3-progression-impact-arbitration.md) |
| **Slice ID** | `SLICE-4-G-OPS3-PROG-IMPACT-ENF` |
| **Allowed backlog** | G-OPS3 reliquat — `ReplaceStageProgressionRules` only |
| **Contract SoT** | [g-ops3-progression-impact-arbitration.md](./g-ops3-progression-impact-arbitration.md) (arbitrage métier **validé** DM 2026-10-16) |
| **§12 G-OPS3** | Reste **Partial** après livraison (qual SLICE-3, draw-rules reliquat) |

**Governance:** [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4 · arbitrage parent [g-ops3-progression-impact-arbitration.md](./g-ops3-progression-impact-arbitration.md).

**Distinct slices:** [SLICE-3-G-OPS3-QUAL-PROG-ENF](./slice-3-g-ops3-qual-prog-enf.md) (gate Applied **qualification**) · [SLICE-1-G-B-3-ENF](./slice-1-g-b-3-enf.md) (pool).

---

## 1. Périmètre produit

| In scope | Out of scope |
|----------|----------------|
| `StructureProgressionImpactGate` — diff P-4 + G-OPS2 + WC-1/WC-2 (**2c**) | `ReplaceStageQualificationRules` — gate Applied SLICE-3 (**qual**) |
| Retrait proxy Applied **progression** (P5–P6) | G-B-3, B1, affaiblissement D2 |
| `Application.ProgressionFeedConsumed` + i18n + Host 409 | Promotion §12 G-OPS3 → **Done** |
| Tests **R-Prog-OPS3-*** (A1, A2, B1, B2, C1, C2) + preuve Host | Hub diagnostic persistant |

---

## 2. Préconditions d’implémentation (stop line)

| ID | Condition | Statut |
|----|-----------|--------|
| **GOV-1** | Arbitrage métier A-3 / A-4 validé | **Done** (DM 2026-10-16) |
| **GOV-2** | Slice enregistrée §11C.4 (cette fiche) | **Done** |
| **TECH-1** | [A-3-R1](./g-ops3-progression-impact-arbitration.md#a-3-r1--réserve-technique-attribution-du-rejeu) — **WC-1** + caractérisation · [g-ops3-a3-r1-technical-note.md](./g-ops3-a3-r1-technical-note.md) §7.1 | **Closed** (DM 2026-10-16) |

**PR produit garde :** autorisées (contrat **WC-1** + tests witness ; **WC-2** / 2c dans le même garde, spec A-4).

**Post-P5 :** le use case progression n’appelle plus le proxy Applied SLICE-3 ; **qualification** conserve `StructureAppliedDrawFeedRulesChangeGate`. P6 = nettoyage du code mort (voir §7).

---

## 3. Guard order (inchangé vs aujourd’hui + insertion)

1. No-op (`Equals` `ProgressionRules`)  
2. `StructureOperationalExitEditGuard` (D2)  
3. `StructureReadyConsumerFeedGate` (W-OPS3-1 / G-OPS2)  
4. **Nouveau** garde impact progression (W-OPS3-2 + 2c)  
5. `ReplaceProgressionRulesOperationally`  

**Ne pas** permuter D2 et G-OPS2 (priorités d’erreur existantes).

---

## 4. Référence cas (extrait)

Voir matrice et **R-Prog-OPS3-*** dans [g-ops3-progression-impact-arbitration.md](./g-ops3-progression-impact-arbitration.md).

---

## 5. Critères de clôture slice

**Closed** (DM 2026-10-16) :

1. TECH-1 **Closed** — **Done**.  
2. Garde impact livré ; proxy Applied **progression** retiré — **Done** (P5–P6).  
3. Tests Application + Host progression (204 A1, 409 B2, 409 2c) — **Done** (§7).  
4. Qualification : refus Applied via SLICE-3 — **Done** (non-régression P6).  
5. P6 nettoyage technique — **Done** (DM 2026-10-16).  
6. §12 **G-OPS3** reste **Partial** (draw-rules et autres reliquats hors cette slice).

---

## 6. Séquencing

1. **TECH-1 / A-3-R1** — **Closed** (WC-1).  
2. **Impl garde** — voir §7 (WC-1 + WC-2 / 2c + W-OPS3-1 inchangé).  
3. **Retrait** proxy Applied sur progression dans `ReplaceStageProgressionRules` — **Done** (P5).  
4. Qualification : gate Applied SLICE-3 **maintenu**.  
5. **P6** — suppression références / tests du proxy mort (`EnsureProgressionReplaceAllowed` progression) sans changement métier.

---

## 7. Plan d’implémentation du garde (contrat)

| Phase | Livrable | Référence |
|-------|----------|-----------|
| **P0** | Contrat P-4 / WC-1 figé | [g-ops3-p4-wc1-contract.md](./g-ops3-p4-wc1-contract.md) |
| **P1** | `ProgressionRulesPathDiff` (Domain) + tests diff | **Done** |
| **P2** | `ProgressionPathConsumptionWitness` + tests | **Done** |
| **P3** | Inventaire lien chemin → production aval | [wc2 inventory](./g-ops3-wc2-production-link-inventory.md) **Done** |
| **P4** | `ProgressionPathProductionParticipationWitness` + form/slot + composition WC-1/2 | [wc2 contract](./g-ops3-wc2-contract.md) **Done** (réserve form/slot **levée**) |
| **P5** | Garde branché : WC-1 + WC-2, ordre §3, erreurs distinctes, **R-Prog-OPS3-***, preuves Host — [critères DM](./g-ops3-wc2-contract.md#conditions-dacceptation-p5-dm) | **Done** (DM clôture 2026-10-16) |
| **P6** | Nettoyage : code mort proxy Applied **progression** (`StructureAppliedDrawFeedRulesChangeGate.EnsureProgressionReplaceAllowed` supprimé ; qual inchangée) | **Done** (DM 2026-10-16) |

### Preuves P5 (DM 2026-10-16)

**Application (Release)** — [`ReplaceStageProgressionRulesGOps3ImpactTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageProgressionRulesGOps3ImpactTests.cs) : A1, A2, NOOP, B1, B2, C1, C2, D2, **2c** (`ProgressionDownstreamProductionBlocked`), **B1neg** ; non-régression POOL [`ReplaceStageAffectationAuthoringGOps3Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageAffectationAuthoringGOps3Tests.cs) **5/5**.

**Host (Postgres)** — [`ReplaceStageProgressionRulesGOps3EndpointTests`](../../../tests/MyClub.PlayUp.Host.Tests/ReplaceStageProgressionRulesGOps3EndpointTests.cs) :

| Cas | HTTP | Code `ProblemDetails` |
|-----|------|------------------------|
| A1 — premier chemin, draw appliqué, aval brouillon | **204** | — |
| B2 — consommation WC-1 (`ApplyProgressionOutcome`) | **409** | `Application.ProgressionFeedConsumed` |
| 2c — production aval participante sans consommation WC-1 | **409** | `Application.ProgressionDownstreamProductionBlocked` |

Affectation Host POOL : [`ReplaceStageAffectationAuthoringEndpointTests`](../../../tests/MyClub.PlayUp.Host.Tests/ReplaceStageAffectationAuthoringEndpointTests.cs) **1/1** (non-régression).

### P6 — nettoyage (DM 2026-10-16)

- Supprimé : `StructureAppliedDrawFeedRulesChangeGate.EnsureProgressionReplaceAllowed` (aucun appelant ; progression déjà sur `StructureProgressionImpactGate` depuis P5).
- Conservé : `EnsureQualificationReplaceAllowed`, code `Application.AppliedDrawFeedRulesChangeBlocked`, mapping Host 409, `lifecycleMutationMessageKey.ts`.
- Conservé (nom distinct) : `StructureReadyConsumerFeedGate.EnsureProgressionReplaceAllowed` — **G-OPS2**, pas le proxy SLICE-3.
- i18n : libellés `AppliedDrawFeedRulesChangeBlocked` limités à la **qualification** (seul usage restant du code).
- Commentaires witnesses WC-1/WC-2 alignés sur le branchement P5.
- **Tests Release P6 :** Application **40/40**, Host **5/5** (filtre mandat).

**Tests contrat WC-1 :** conserver / étendre [`ProgressionPathConsumptionWitnessA3R1Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ProgressionPathConsumptionWitnessA3R1Tests.cs) ; garde couvert par **R-Prog-OPS3-B*** et cas allow/refus arbitrage.

**Qualification** reste sur SLICE-3. Améliorations hors périmètre (docs historiques, tests additionnels) → backlog séparé, sans rouvrir cette slice par défaut.
