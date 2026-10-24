# Phase 8 — Cursor rules reconciliation

**Date:** 2026-10-22  
**SHA:** `8d4a8dfa` (post P8-W1)  
**Mode:** read-only review + **one** targeted rule alignment (example command block).  
**Authority order (phase 2):** Notion décisions arbitrées → architecture/conventions Git → CI → règles Cursor (renvois, pas duplication divergente).

---

## 1. Inventory (current)

| Source | Count | Notes |
| --- | ---: | --- |
| `.cursor/rules/**/*.mdc` | **22** | Unchanged set since phase 0 / C8 |
| `.cursor/commands/*.md` | **2** | `audit-before-pr.md`, `cross-design-review.md` |
| `AGENTS.md` | **1** | Root pointer — added C8 |

See [`cursor-rules-inventory.md`](cursor-rules-inventory.md) (P8 addendum below).

### alwaysApply vs scoped

| alwaysApply | Scoped |
| --- | --- |
| `architecture`, `workflow`, `general`, `documentation`, `no-cargo-cult` | `csharp` · `testing` · `playup-web-quality` · `design-system` · `loading-states` · `design/00`–`11` |

---

## 2. Cross-check vs décisions arbitrées (échantillon)

| Décision / SoT | Règle(s) | Verdict |
| --- | --- | --- |
| Modular monolith, UseCaseExecutor, Media via Host | `architecture.mdc` | **Aligné** avec code + tests isolation |
| Development.Tests → Infra OK, pas Host/DevRunner | `architecture.mdc`, `testing.mdc` + `DevelopmentTestsArchitectureIsolationTests` | **Aligné** (C8 / AUD-0011) |
| Pas de MediatR / Redux par défaut | `architecture.mdc`, `no-cargo-cult.mdc` | **Aligné** |
| Notion = produit/décisions ; Git = code/contrats | `documentation.mdc`, `workflow.mdc` | **Aligné** |
| Hiérarchie design Notion / Lab / rules / code | `design/00`, `design-system.mdc` | **Aligné** (double entrée assumée — voir §4) |
| Heuristiques audit web **hors CI** | `playup-web-quality.mdc` | **Aligné** phase 6 S1 |
| AUD-0008 auth/TLS | *Non décrit comme présent* dans rules | **OK** — pas de fausse promesse sécurité |
| AUD-0008 exposition loopback (C2) | Non dupliqué dans rules | **OK** — `docs/guides/` + code Host |

**Aucun conflit connu** entre règles Cursor et `architecture.mdc` / décisions Notion référencées dans le chantier.

---

## 3. Écarts wave 5 → post C8 → P8

| Écart (historique) | Statut P8 |
| --- | --- |
| Pas d’`AGENTS.md` | **Résolu** C8 |
| `audit-before-pr` sans `build` / `lint:css` | **Résolu** C8 |
| `playup-web-quality.mdc` bloc bash sans `build` | **Résolu** P8-Cursor micro-lot (aligné CI + command PR) |
| Fichiers `.mdc` dédiés Media / DevRunner | **Dette acceptée** C8 — pointeurs `architecture.mdc` + `docs/guides/` |
| AUD-0009 / AUD-0010 dans rules | **Non exigé** — dette registre, pas règle Cursor |
| `i18n:check` absent de CI | **Documenté** S1 / `playup-web-quality` (non présenté comme gate) — écart CI connu, pas contradiction rules |
| Plugin Mobbin (user Cursor) | Hors dépôt — hors périmètre réconciliation repo |

---

## 4. Zones de vigilance (non bloquantes clôture)

| Zone | Observation | Action |
| --- | --- | --- |
| **Double entrée design** | `design-system.mdc` + `design/00` + Notion | Conserver — agents doivent lire `00` pour hiérarchie ; pas de fusion sans arbitrage |
| **README web vs rules** | README peut mentionner CSS legacy hors stylelint | Cohérent avec `playup-web-quality` |
| **cross-design-review** | Commande PR design — complète `audit-before-pr` | Pas de conflit ; pas de mise à jour requise |
| **Règles vs P7 ops** | Health `/health`, bind guard non dans `.mdc` | Exploitation dans guides/code — optionnel lien futur dans `documentation.mdc` seulement si arbitrage |

---

## 5. Automated guardrails (rules ↔ contrôles)

| Exigence rule | Contrôle |
| --- | --- |
| Host ↛ Development/TestKit | `HostArchitectureIsolationTests` + CI publish |
| Development.Tests boundary | `DevelopmentTestsArchitectureIsolationTests` |
| Web CI stack | `ci.yml` web job ↔ `playup-web-quality` + `audit-before-pr` |
| .NET warnings policy | `verify-dotnet-warnings-baseline.sh` (post P8-W1 vert) |

---

## 6. Outcome (clôture stream P8-Cursor)

| Critère plan directeur | Statut |
| --- | --- |
| Inventaire à jour | **OK** (addendum inventory) |
| Obsolètes / contradictoires corrigés **après validation** | **OK** — micro-alignement `playup-web-quality` |
| Décisions architecture reflétées | **OK** sur périmètre audit |
| Exigences automatisables avec contrôle | **OK** (§5) |
| Exceptions / non arbitré identifiés | **OK** — Media/DevRunner mdc dédiés |
| **Aucun conflit connu** rules ↔ SoT | **OK** |

---

## 7. Related

- C8 : [`phase-3-lot-c8-plan.md`](phase-3-lot-c8-plan.md)
- Wave 5 governance : [`phase-1-wave-5.md`](phase-1-wave-5.md) §4
- Chantier closure : [`phase-8-plan.md`](phase-8-plan.md)
