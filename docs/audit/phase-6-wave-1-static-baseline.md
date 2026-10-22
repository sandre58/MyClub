# Phase 6 — Wave 1 — Static quality baseline

**Launched:** 2026-10-22  
**Mode:** investigation only — **no application or script changes** in this wave.

## Reference (immutable)

| Field | Value |
| --- | --- |
| Git ref | `main` |
| SHA | `4b6b3fe72bdb18b0391ccc3873495f8e7dc4f834` (merge PR #72, post phase 5) |
| Pre-flight | `git rev-parse main` → `4b6b3fe7…` |

## Environment (execution context)

| Item | Observed |
| --- | --- |
| OS | Windows (agent runner) |
| Node | v24.18.0 |
| npm | 11.16.0 |
| .NET | Release build OK — **0 warnings** (local, no baseline script run) |
| npm noise | `Unknown env config "devdir"` (local npm config — not a product defect) |

Results describe behaviour **on the reference SHA** in this environment. Linux CI may differ only where noted.

---

## 1. Mandatory controls (.NET)

| Command | Exit | Result |
| --- | ---: | --- |
| `dotnet build -c Release` | **0** | Build succeeded — **0 warning(s)**, 0 error(s) (~52s) |
| `dotnet test -c Release --no-build` | **1** | **1 failure** in `MyClub.PlayUp.Application.Tests`: `StructurePresentedAttentionCollectorTests.Collect_emits_incomplete_entry_round_when_primary_start_blocker_is_unmaterialized` — other suites green (Host, Infra, Development, etc.) |

**CI note:** `.github/workflows/ci.yml` runs full `dotnet test` — this failure is a **red gate** unless CI is green on another SHA or environment (not verified here; `gh` unavailable locally).

**Warnings policy:** CI uses `verify-dotnet-warnings-baseline.sh` on build log (`docs/audit/dotnet-warnings-baseline.json`) — not re-run in this wave.

---

## 2. Mandatory controls (Play’Up web)

Commands from `src/PlayUp/web`.

| Command | Exit | Result |
| --- | ---: | --- |
| `npm run build` | **0** | `tsc -b` + Vite OK. **Warning:** main chunk ~1 165 kB / gzip ~302 kB (`chunkSizeWarningLimit`). |
| `npm run typecheck` | **0** | `tsconfig.app` + `tsconfig.node` — OK |
| `npm run test:run` | **0** | **141** files, **1132** tests passed (~68s) |
| `npm run i18n:check` | **0** | Locale parity — **16/16** |
| `npm run lint:css` | **0** | Stylelint — no violations |
| `npm run lint` | **1** | Oxlint `--deny-warnings`: **3×** `no-useless-escape` in `scripts/css-audit-rider.mjs` (lines 38, 41, 43) |

**CI alignment:** Web job runs `format:check`, `lint`, `lint:css`, `typecheck`, `test:run`, `build` — **`npm run lint` is expected to fail** on this SHA until `css-audit-rider.mjs` escapes are fixed or script is excluded (arbitrage S1).

`format:check` — **not run** in this wave (optional follow-up).

---

## 3. Audit / report scripts (inventory)

| npm script | Role | Exit (this run) | Headline output |
| --- | --- | ---: | --- |
| `css:audit` | Structure-fiche CSS heuristic | **0** | (structure-scoped; same family as phase 5) |
| `i18n:audit` | Structure i18n heuristic | **0** | structure.json leaf keys — 0 suspect (scoped) |
| `tile:check` | Tile family B | *(not re-run)* | Green in phase 5 @ `d14391dd` |
| `frontend:audit` | Repo-wide CSS graph + orphans + scale | **0** | 69 CSS reachable; **100** classes with no textual match in `src`; duplicate-block groups **60+** (sampled) |
| `i18n:audit:repo` | Repo-wide key usage heuristic | **0** | **686** suspicious unused keys (many `*_one` / `*_other` plural forms — likely false positives) |
| `css:audit:rider` | Rider-style selector / token hygiene | **0** | Broken selector wraps **0**; undefined `var(--*)` section empty |

**Not in CI** (report-only unless later arbitration): `frontend:audit`, `i18n:audit:repo`, `css:audit:rider`, `css:audit`, `i18n:audit`, `tile:check`.

---

## 4. Stream pointers (post–wave 1 arbitration)

| Stream | Wave 1 signal | Next step |
| --- | --- | --- |
| **S1** Warnings | Vite chunk size; oxlint on `css-audit-rider.mjs`; npm `devdir` noise | Arbitrage: fix script vs oxlint override vs CI scope |
| **S2** Dead code | `frontend:audit` 100 CSS class orphans; no automated BE dead-code scan in wave 1 | Investigation + validated lots only |
| **S3** i18n | `i18n:audit:repo` 686 suspects vs `i18n:check` green | Refine heuristic / plural keys before any deletion |
| **S4** CSS | `lint:css` green; Rider audit 0 wraps; duplicate blocks in `frontend:audit` | DS / structure CSS debt sizing |
| **S5** Organisation | Totals: ~16 191 CSS lines, ~61 663 TS/TSX (non-test); dual token pipeline noted by script | TBD |
| **S6** Tests | Web vitest green; **1** Application test red locally | Reproduce on CI / fix or quarantine (product, not audit lot by default) |

---

## 5. Limits

- No `npm ci` fresh install.
- No `dotnet` warnings baseline script execution on Windows log encoding.
- No backend static analyser sweep (e.g. unused public API) — phase 6 S2.
- Heuristic audits — false positives/negatives; **no deletions** from wave 1 alone.

---

## 6. Reproducibility

```bash
git checkout 4b6b3fe72bdb18b0391ccc3873495f8e7dc4f834
dotnet build -c Release
dotnet test -c Release --no-build
cd src/PlayUp/web
npm run build && npm run lint && npm run lint:css && npm run typecheck && npm run test:run && npm run i18n:check
npm run frontend:audit && npm run i18n:audit:repo && npm run css:audit:rider
```
