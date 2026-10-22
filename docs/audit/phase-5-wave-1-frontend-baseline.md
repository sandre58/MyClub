# Phase 5 — Wave 1 — Frontend baseline

**Launched:** 2026-10-22  
**Mode:** investigation only — **no application or script changes** in this wave.

## Reference (immutable)

| Field | Value |
| --- | --- |
| Git ref | `main` |
| SHA | `d14391dd46170243056408d42df6d22d1627213bd` (merge PR #71) |
| Working tree | `src/PlayUp/web` **identical** to this SHA on the measurement branch (no web diff vs `d14391dd`) |

**Pre-flight:** `git rev-parse main` and `git rev-parse d14391dd` both returned `d14391dd46170243056408d42df6d22d1627213bd`.

## Environment (execution context)

| Item | Observed |
| --- | --- |
| OS | Windows (agent runner) |
| Node | v24.18.0 |
| npm | 11.16.0 |
| Dependencies | `node_modules` present under `src/PlayUp/web` (not reinstalled via `npm ci` for this run) |
| npm noise | Repeated warning: `Unknown env config "devdir"` (local npm config — **not** a product defect on the SHA) |

Results below describe behaviour **on the reference SHA** in this environment. A Linux CI runner may differ only where noted (not re-run here).

---

## 1. Mandatory controls

All commands from `src/PlayUp/web`.

| Command | Exit | Result (reference SHA) |
| --- | ---: | --- |
| `npm run build` | **0** | `tsc -b` + `vite build` OK (~5.6s). Vite **warning** (not failure): main chunk `index-*.js` ≈ 1 165 kB gzip ≈ 302 kB — suggests code-splitting (`chunkSizeWarningLimit`). |
| `npm run i18n:check` | **0** | Vitest `localeParity.test.ts` — **16/16** passed (~4.4s). |
| `npm run lint:css` | **0** | Stylelint on `src/**/*.css` — **no violations** (contrast: phase 1 wave 3 reported AUD-0003 duplicate selector on `structure-fiche.css`). |

**Interpretation:** Core build and parity gates are **green** on `d14391dd`. The chunk-size line is an **informational** Vite reporter warning, not a failed build.

---

## 2. Audit scripts (AUD-0001 scope)

### 2.1 `package.json` mapping @ `d14391dd`

| npm script | Target on disk | Runs? |
| --- | --- | ---: |
| `css:audit` | `scripts/audit-structure-fiche-css.mjs` | **Yes** (0) |
| `i18n:audit` | `scripts/audit-structure-i18n.mjs` | **Yes** (0) |
| `tile:check` | `scripts/tile-family-check.mjs` | **Yes** (0) |

**Not present:** `scripts/frontend-audit.mjs`, `scripts/i18n-key-audit.mjs` (removed 2026-10-12 per phase 1 wave 3). Current scripts are **structure-scoped** replacements, not drop-in global audits.

### 2.2 Script output (this run)

| Command | Exit | stdout summary |
| --- | ---: | --- |
| `npm run css:audit` | **0** | `structure-fiche.css: 0 unused / 118 unique tokens` |
| `npm run i18n:audit` | **0** | `structure.json (en): 0 suspect / 983 leaf keys` (+ script disclaimer on false negatives) |
| `npm run tile:check` | **0** | `Tile family B … OK` |

### 2.3 AUD-0001 status vs phase 1 wave 3

| Phase 1 finding | Wave 1 @ `d14391dd` |
| --- | --- |
| `css:audit` / `i18n:audit` / `tile:check` pointed at **missing** files | **Resolved** on tree — npm entries target existing `scripts/*.mjs` |
| Contributor docs referenced `frontend-audit.mjs` | **Web docs** (`src/PlayUp/web/docs/*`) no longer grep-match `frontend-audit` @ this SHA |
| `docs/audit/baseline.md`, `phase-1-wave-3.md` still describe scripts as **broken** | **Documentation drift** (Git audit docs stale vs repo) — not re-measured product behaviour |

**CI:** `.github/workflows` does **not** invoke `css:audit`, `i18n:audit`, or `tile:check` (unchanged from wave 3).

---

## 3. Register cross-check (phase 1 → wave 1)

| ID | Wave 3 snapshot (SHA `93b09f16…`) | Wave 1 @ `d14391dd` | Notes for F-lots |
| --- | --- | --- | --- |
| **AUD-0001** | Broken npm → missing global scripts | Scripts **executable**; global audit **still absent** | F1 → arbitrage: close as “repointed + scoped”, update audit Git docs, and/or restore global tool |
| **AUD-0003** | `lint:css` duplicate `.structure-fiche__header-lifecycle-toolbar` | **`lint:css` clean** | F2 — re-verify finding still exists before any lot |
| **AUD-0013** | Heuristic orphans on `structure-fiche.css` | Heuristic reports **0 unused** / 118 tokens | F3 — finding may be obsolete or heuristic changed; do not assume deletion lot without diff vs wave 3 |
| **AUD-0014** | Suspect leaf keys in `structure.json` | **0 suspect** / 983 leaves | F4 — same caution as F3 |

---

## 4. Limits of this investigation

- **No** `npm ci` fresh install log — used existing `node_modules`.
- **No** `npm run lint` (oxlint), `typecheck`, or `test:run` — out of wave 1 mandatory list (optional for a later wave).
- **No** repo-wide dead-code / unused-export scan (still no `frontend-audit.mjs`).
- Scoped audit scripts use **text search heuristics** — false negatives possible (dynamic class names, i18n keys via indirection); script output includes i18n disclaimer.
- **No** corrective changes — baseline only.

---

## 5. Reproducibility

```bash
git checkout d14391dd46170243056408d42df6d22d1627213bd
cd src/PlayUp/web
npm ci   # recommended for strict repro
npm run build
npm run i18n:check
npm run lint:css
npm run css:audit
npm run i18n:audit
npm run tile:check
```

Local logs (this run): `.local/phase5-wave1/*.log` (gitignored).

---

## 6. Wave 1 conclusion

On **`d14391dd`**, mandatory frontend gates (**build**, **i18n:check**, **lint:css**) pass. AUD-0001 **npm scripts execute** against structure-scoped tools; the **global** audit tooling remains absent. Several phase 1 register items (**AUD-0003**, **AUD-0013**, **AUD-0014**) **do not reproduce** on this SHA with current commands — F2–F4 require **re-arbitration** (confirm obsolete vs re-measure with wave 3 protocol) before any corrective lot.

**F1–F4 remain unauthorized** until explicit Notion lot go.
