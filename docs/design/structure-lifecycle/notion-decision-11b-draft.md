# Notion — Décision §11B (draft — copy/paste)

**Sibling decision:** [§11A mutabilité Ready](https://app.notion.com/p/3fa75fb92d108172b028d6858d0ea45b?pvs=204) (Acceptée 2026-10-15)

**Annexe Git:** [section-11b-signoff-preparation.md](./section-11b-signoff-preparation.md) · [phase-mutability-matrix.md](./phase-mutability-matrix.md)

---

## Métadonnées suggérées

| Champ | Valeur |
|-------|--------|
| **Titre** | [Play'Up] Structure phase lifecycle — contrat d'implémentation (§11B) |
| **Type** | Décision technique |
| **Statut** | Acceptée (Git mirror 2026-10-15) |
| **Date** | 2026-10-15 |
| **Projet** | MyClub / Play'Up |

---

## Texte décision

Le décideur signe le **contrat d'implémentation** complémentaire à la politique §11A déjà acceptée.

### Périmètre livré (§12 Done)

- **G-UI1 / G-UI2** — sévérités drawer alignées matrice  
- **G-MAT1 / G-MAT2** — gates materialize Cup (undecidable, attribution)  
- **G-OPS1 / G-OPS2** — mutations opérationnelles Ready (qual, prog, affectation) ; D2 et D1 avec `ReadyConsumerWouldBreak` + noms de phases en SPA  

### Périmètre différé (réserve écrite)

| ID | Réserve | Réouverture |
|----|---------|-------------|
| **G-MAT3** | Pas de gate `NonRootUnfed` ; test **R-Mat-M2b** (A5) | [Arbitrage Git 2026-10-15](./g-mat3-arbitration-session.md) |
| **G-MAT4** | Pas de gate shortfall source ; **R-Mat-M4b** + diagnostic analyzer | [Arbitrage Git 2026-10-15](./g-mat4-arbitration-session.md) |
| **G-OPS3** | Pas de refuse commande draw/applied ; **R-Draw1-N*** | **D3-1** — [Arbitrage Git 2026-10-15](./g-ops3-arbitration-session.md) |

### Contrainte

- **G-SWISS** — pas de garde production globale ; APIs progressives §8 matrice  

### Autorisation équipe / Agent

- Corriger les régressions sur les lignes **Done**  
- **Ne pas** implémenter les gates différés sans nouvelle décision  
- Extensions OPS (autres commandes §9), hub D1 persistant, tiers mutabilité domaine → **Task Notion** dédiée  

### Réserve A5

Inchangée : diagnostic hub ≠ veto materialize automatique.
