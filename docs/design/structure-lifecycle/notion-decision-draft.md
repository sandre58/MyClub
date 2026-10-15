# Notion — Décision (signed content)

**Private Notion draft:** [Décision — Structure phase mutabilité opérationnelle en Ready (draft)](https://app.notion.com/p/3fa75fb92d108153b80ff35858ea0912?pvs=204) — sync body from below; move to Décisions database when ready.

**Technical annex (Git SoT):** [phase-mutability-matrix.md](./phase-mutability-matrix.md)

---

## Status

| Layer | State |
|-------|--------|
| **§11A (A1–A6)** | **SIGNED** 2026-10-15 — reserve on A5: diagnostics are not automatic vetoes; §5.2 is directional |
| **§11B** | **NOT SIGNED** — G-MAT1–G-MAT4 remain litigious |
| **G-UI1 / G-UI2** | Implemented in Git (2026-10-15) |

---

## Title

Structure phase — mutabilité opérationnelle en Ready

## Decision (product rule)

Une phase **Prête** a une **forme** et un **règlement de jeu** (match, classement, confrontation) **figés**, mais peut encore recevoir des **ajustements opérationnels contrôlés** : entrées, sorties, attributions, placement, tirage.

Ces ajustements ne doivent :

1. Ni invalider silencieusement une phase consommatrice déjà Prête (**D1**).
2. Ni désynchroniser structure et production dès qu’au moins un match est matérialisé (**D2**), sauf **capacités Swiss explicites**.
3. **Prepare** = intégrité structurelle ; **Materialize** = production réalisable — sans confondre avec « tout diagnostic I bloque Prepare / tout C bloque Materialize » (**réserve A5**).
4. Tirage opérationnel ; règlement de jeu figé en Prêt (**D4**).

## Arbitrages fermés

| # | Decision |
|---|----------|
| **#1 `SlotFeedMissing`** | **A — intégrité**, Prepare bloquant ; masquage drawer ≠ assouplissement métier |
| **#2 Diagnostics M** | Politique §5.2 ; drawer aligné Prepare pour `AuthoredAwardUnreachable` (Cup = erreur) |
| **#3 Swiss** | Capacités explicites ; pas de guard global `HasAttachedMatches ⇒ deny all` |

## D1–D4

Voir matrice Git §1 et §11A.

## Implementation scope (without §11B)

- Done: G-UI1, G-UI2
- Deferred: G-OPS1–3, G-MAT1–4 (spec + command tests first)
