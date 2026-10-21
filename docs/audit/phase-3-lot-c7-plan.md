# Phase 3 — Lot C7 plan (web hygiene)

**Status:** Implemented on `feature/new_version` (commits `2bd16efc`, `374aa492`; prerequisite **C2** on same branch).

**Authorization:** Phase 3 corrective lot — hygiène web / CI job `web` (arbitrage 7, findings AUD-0001, AUD-0003, AUD-0006, AUD-0007). No separate Notion authorization page at C1/C2 level; execution aligned with phase 2 classification **LC → lot C7**.

**Prerequisite:** **C2** (`fed7ab07`) — `npm run build` must pass before C7 gates are meaningful.

---

## 1. Scope (in)

| Finding | Deliverable | Evidence |
| --- | --- | --- |
| **AUD-0001** | `package.json` scripts point to existing tools; contributor docs updated | `css:audit` → `audit-structure-fiche-css.mjs`; `i18n:audit` → `audit-structure-i18n.mjs`; `tile:check` → `tile-family-check.mjs`; `conventions.md`, `page-migration.md` |
| **AUD-0003** | Stylelint clean — duplicate `.structure-fiche__header-lifecycle-toolbar` resolved | `structure-fiche.css` cascade merge; `npm run lint:css` |
| **AUD-0006** | Oxlint `--deny-warnings` clean | `.oxlintrc.json` overrides; hook/export refactors in Structure + DS (`2bd16efc`) |
| **AUD-0007** | Prettier `format:check` clean | `374aa492` + ongoing formatting discipline |

**CI job `web` steps satisfied:** `format:check` → `lint` → `lint:css` → `typecheck` → `test:run` → `build` (Playwright smoke unchanged).

---

## 2. Explicit exclusions (C7)

| Item | Reason |
| --- | --- |
| **AUD-0013** (unused Structure fiche CSS) | **IC** — `css:audit` reports candidates; no class removal without browser proof |
| **AUD-0014** (suspect i18n keys) | **IC** — `i18n:audit` reports; no key deletion without product confirmation |
| **AUD-0004 / C3** | Cup C2 protocol backend tests — separate lot |
| **C8** | Cursor rule rewrite, optional arch tests |
| **npm dev advisories** (stylelint/braces, etc.) | Noted on C6 fiche; no `npm audit fix --force` |
| **Global `frontend-audit.mjs`** | Not restored; structure-scoped audits only (phase 2 decision) |

---

## 3. Local validation (from `src/PlayUp/web`)

```bash
npm run format:check
npm run lint
npm run lint:css
npm run typecheck
npm run test:run
npm run build
```

Optional hygiene (not in CI `web` job):

```bash
npm run css:audit
npm run i18n:audit
npm run tile:check
```

---

## 4. Acceptance criteria

- [x] CI `web` quality steps green on branch (format, oxlint, stylelint, typecheck, unit tests, build).
- [x] AUD-0001 — npm scripts executable; no doc pointers to removed `frontend-audit.mjs` / `i18n-key-audit.mjs` in web contributor docs.
- [x] AUD-0003 — `lint:css` passes.
- [x] AUD-0006 — `lint` passes with `--deny-warnings`.
- [x] AUD-0007 — `format:check` passes.
- [ ] Registre Notion — AUD-0001, 0003, 0006, 0007 → correction / vérifié (post-merge).
- [ ] AUD-0013 / 0014 remain **open IC** until dedicated decision.

---

## 5. Expected diff (when documenting closure)

```
docs/audit/phase-3-lot-c7-plan.md
.cursor/rules/playup-web-quality.mdc   (lint:css scope note)
```

Implementation commits already on branch: `2bd16efc`, `374aa492`.
