# Phase 3 — Lot C8 plan (Cursor governance)

**Status:** Implemented on `feature/new_version` (rules/commands/docs; no product code).

**Authorization:** User go 2026-10-21 — closes arbitrage 7 **gouvernance Cursor** (opportunistic lot; not a full rules rewrite).

**Findings addressed:** governance gaps from `phase-1-wave-5.md` §4; **AUD-0011** (Development.Tests → Infrastructure pattern documented + test).

---

## 1. Scope (in)

| Item | Deliverable |
| --- | --- |
| **PR prep command** | `audit-before-pr.md` — web checks match CI `web` including `lint:css` and `npm run build` |
| **Architecture / testing rules** | `Development.Tests` exception; Media/DevRunner doc pointers in `architecture.mdc`; recette section in `testing.mdc` |
| **Design entry points** | Cross-link `design-system.mdc` ↔ `design/00-design-principles.mdc` |
| **AGENTS.md** | Root pointer to `.cursor/rules/` and inventory |
| **AUD-0011** | `DevelopmentTestsArchitectureIsolationTests` — Infra allowed, Host/DevRunner forbidden |
| **Inventory** | `cursor-rules-inventory.md` C8 addendum |

---

## 2. Explicit exclusions (debt accepted in phase 2)

| Finding | Treatment |
| --- | --- |
| **AUD-0009** | Optional extra arch tests — **not** added (csproj + existing isolation tests suffice) |
| **AUD-0010** | `UseCaseExecutor` size — **no** refactor without measured pain |
| **Dedicated Media / DevRunner `.mdc` files** | Pointers in `architecture.mdc` + Git guides only |
| **Full rules rewrite** | Out of scope — targeted deltas only |

---

## 3. Validation

```bash
dotnet test tests/MyClub.PlayUp.Development.Tests --filter FullyQualifiedName~DevelopmentTestsArchitecture -c Release
dotnet test tests/MyClub.PlayUp.Development.Tests --filter FullyQualifiedName~DevelopmentArchitecture -c Release
```

Manual: read `AGENTS.md`, run suggested web command block from `audit-before-pr.md` when touching `src/PlayUp/web`.

---

## 4. Acceptance criteria

- [x] `audit-before-pr` no longer suggests web checks that skip `build` / `lint:css`.
- [x] Development.Tests boundary documented and guarded by test.
- [x] `AGENTS.md` + inventory updated.
- [ ] Notion governance / registre rows (post-merge) if tracked separately from AUD-0011.

---

## 5. Related docs

- `docs/audit/cursor-rules-inventory.md`
- `docs/audit/phase-1-wave-5.md` §4
