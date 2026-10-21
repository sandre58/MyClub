# Phase 1 — Wave 5: Security, CI/CD, dependencies, Cursor governance, quality controls

**Launched:** 2026-10-21  
**Closed:** 2026-10-21  
**Branch:** `feature/new_version`  
**SHA vague:** `93b09f161739f47b768caad63aed90d0d3c1469f`

No code, configuration, workflow, or Cursor-rule changes in this wave.

## Classification key

| Label | Meaning |
| --- | --- |
| **Vulnérabilité démontrée** | Exploit path or advisory directly applicable to shipped/runtime surface |
| **Risque à qualifier** | Depends on deployment model, exposure, or product intent |
| **Écart de gouvernance** | Process / CI / rules drift without proven runtime defect |
| **Amélioration** | Hardening or hygiene worth tracking (P3) |

---

## 1. Security

### 1.1 Authentication and authorization (AUD-0008 — extended)

**Evidence @ SHA**

```text
rg "AddAuthentication|UseAuthorization|UseAuthentication|\\[Authorize\\]" src/PlayUp --glob *.cs
# → no ASP.NET auth pipeline (only unrelated XML doc on StructureViewDto)
```

`Program.cs` maps all Play’Up and Media minimal APIs with **no** `AddAuthentication`, `UseAuthorization`, or endpoint metadata.

**Exposure factors (risque à qualifier, not automatic CVE)**

| Factor | Observation | Qualification |
| --- | --- | --- |
| API surface | All `MapCompetition*`, `MapStructure*`, `MapStage*`, `MapMatch*`, `MapMedia*` routes are **anonymous** if Host is reachable | **Risque à qualifier** — critical only when Host is network-reachable by untrusted clients |
| Default URL | `launchSettings.json` → `http://localhost:5287` | Consistent with **local dev**; not proof of safe production |
| `AllowedHosts` | `appsettings.json` → `"*"` | Accepts any Host header when deployed — **amplifies misconfiguration risk** if bound to public interfaces without reverse proxy rules |
| CORS | **No** `AddCors` / `UseCors` in Host | Same-origin prod (static + API behind one origin) is plausible; **cross-origin SPA** would fail unless added elsewhere (not in repo) |
| Dev proxy | `vite.config.ts` proxies `/competitions`, `/stages`, `/matches`, `/media` to Host — **no CORS in dev** | Dev convenience; not production topology |
| TLS | No `UseHttpsRedirection` / HSTS in `Program.cs` | **Risque à qualifier** for internet-facing deploy |

**Product / deployment intent (not decided in audit)**

- Local organizer tool on loopback → anonymous API may be **accepted risk**.
- Shared server / SaaS → same code is **full trust boundary failure** without external auth (gateway, mTLS, VPN).

**Conclusion:** **AUD-0008** remains **risque à qualifier** — not a demonstrated remote exploit without a deployment story; **not** dismissed as “local only” without an explicit product decision.

### 1.2 Media endpoints

| Route | Auth | Validation |
| --- | --- | --- |
| `POST /media` | None | Multipart `file` required; domain `MediaContentTypes.NormalizeAndValidate`, `MediaPolicies` (2 MiB max, name sanitize) |
| `GET /media/{id}` | None | Metadata read |
| `GET /media/{id}/content` | None | `Cache-Control: public, max-age=31536000, immutable` |

**Risque à qualifier:** unauthenticated **upload + public read** enables storage abuse and hosting of allowed content types (logos) if endpoint is reachable — mitigated partially by size/type allowlist, **not** by identity.

No `RequestSizeLimit` / Kestrel `MaxRequestBodySize` override found in Host — relies on framework defaults + domain size check after read.

### 1.3 Input validation and errors

- Commands use request DTOs and `UseCaseExecutor` / domain guards; Host maps exceptions via `PlayUpExceptionHandler` / `MediaExceptionHandler`.
- `ProblemDetails.Detail` = `exception.Message` — **information disclosure** possible for domain messages; not proven excessive for production (common pattern). **Amélioration** to review messages exposed on 4xx.

### 1.4 Secrets and configuration

- Connection string: `GetConnectionString("PlayUp")` — required at startup; expected via user secrets / env in dev (`UserSecrets` package in CPM).
- No committed secrets observed in tracked `appsettings.json` (connection string not in file).
- `SECURITY.md` — private disclosure process; supported branches `main` + latest tag.

### 1.5 Link to wave 4 (AUD-0002 / AUD-0015)

Distinct issues preserved:

- **AUD-0002** — CI `npm run build` fails → Playwright step not reached; compile-time check on test fixtures broken.
- **AUD-0015** — even with a green build, **5/8** critical paths lack E2E; API/unit may still cover them.

---

## 2. CI/CD

**Workflow:** sole file `.github/workflows/ci.yml`.

### 2.1 Job topology

| Job | Runs | Blocking steps @ SHA (expected) |
| --- | --- | --- |
| `build` (.NET) | `restore` → `build` → `test` → `publish` Host + DLL isolation checks | **`test` fails** on `CupC2ImpasseRecetteProtocolTests` (**AUD-0004**); build **succeeds** with NU1608 warnings |
| `web` (Node) | `format:check` → `lint` → `lint:css` → `typecheck` → `test:run` → `build` → Playwright | **Fails** on format, lint, lint:css, **build** (**AUD-0002**, **AUD-0003**, **AUD-0006**, **AUD-0007**); steps after first failure not run |

Jobs are **parallel** — either can fail independently; merge is red if any job fails.

### 2.2 Local vs CI gaps (gouvernance)

| Control | In `baseline.md` | In CI `web` job | Notes |
| --- | --- | --- | --- |
| `npm run i18n:check` | Measured OK | **Absent** | Parity can regress without CI signal — **écart de gouvernance** (journal; no new ID if treated as phase 2 “retenu” list) |
| `npm audit` / `dotnet list package --vulnerable` | Not in baseline execution | **Absent** | See §3 and **AUD-0016** |
| Playwright | CI yes | Blocked after build failure | **AUD-0002** + **AUD-0015** |
| Integration tests | via `dotnet test` | Requires Docker on runner (GitHub `ubuntu-latest` provides it) | |

### 2.3 Integration tests and Docker

`IntegrationFactAttribute` (Host.Tests): tests **skipped** when Docker unavailable (Windows named pipe / Unix socket check).

- **CI:** Docker expected → integration tests **run**.
- **Local without Docker:** `dotnet test --filter Category!=Integration` suggested in `.cursor/commands/audit-before-pr.md` — integration silently skipped if developer runs full `dotnet test` without Docker → **false green** possible.

**Qualification:** **Écart de gouvernance** / developer ergonomics — not a CI defect.

### 2.4 Publish guard

Host publish step verifies **Development**, **DevRunner**, **TestKit** DLLs absent from output — **passing** when job completes (currently job fails earlier on test).

---

## 3. Dependencies (AUD-0005 extended)

### 3.1 NU1608 (AUD-0005)

**Declared** (`Directory.Packages.props`):

- `Microsoft.EntityFrameworkCore` **11.0.0-rc.2.26504.105**
- `Npgsql.EntityFrameworkCore.PostgreSQL` **11.0.0-rc.1.1** (requires EF **= 11.0.0-rc.1.26425.128**)

**Resolved:** EF rc.2 wins → **18× NU1608** per project on restore (phase 0 log).

**Comment in CPM:** “same train; do not mix with 10.x” — acknowledges intentional rc pairing, not accidental drift.

**Qualification:** **Risque à investiguer** — compatibility at runtime not proven by warning text alone; not a known CVE.

### 3.2 NuGet vulnerability scan

```text
dotnet list package --vulnerable --include-transitive
# @93b09f16 — sources: nuget.org (+ local SDK feed); all listed projects: no vulnerable packages reported
```

**Écart de gouvernance:** command **not** in CI (baseline already noted).

### 3.3 npm

```text
cd src/PlayUp/web
npm audit --omit=dev   → 0 vulnerabilities (runtime deps)
npm audit              → 13 vulnerabilities (3 moderate, 10 high) — vite / vitest toolchain
```

**AUD-0016** — devDependency advisories (see register). **Not** a demonstrated production bundle vulnerability @ SHA.

---

## 4. Cursor governance (inventory only — no rewrite)

### 4.1 Rule sources @ SHA

| Source | Count / role |
| --- | --- |
| `.cursor/rules/**/*.mdc` | **22** files (see `docs/audit/cursor-rules-inventory.md`) |
| `.cursor/commands/*.md` | `audit-before-pr.md`, `cross-design-review.md` — PR prep, not alwaysApply |
| `AGENTS.md` | **Absent** |
| User/plugin rules | Out of repo scope (e.g. Mobbin plugin in Cursor install) |

### 4.2 Scope model

| alwaysApply: true (5) | `architecture`, `workflow`, `general`, `documentation`, `no-cargo-cult` |
| Scoped | `csharp` (`**/*.cs`), `testing` (`*Tests*`), `playup-web-quality`, `design-system`, `loading-states`, `design/00`–`11` (`src/PlayUp/web/**`) |

**Gap (phase 0 carry-over):** no dedicated rules for **Media** module, **DevRunner**, or **Infrastructure** beyond `architecture.mdc` / `csharp.mdc`.

### 4.3 Overlap / hierarchy (no contradiction proven fatal)

| Topic | Rules | Observation |
| --- | --- | --- |
| Design SoT | `design/00-design-principles.mdc` (Notion why / Lab what / rules how) vs `design-system.mdc` (points to `docs/design-system.md`) vs `documentation.mdc` (Notion product SoT) | **Aligned** if Notion = product and Lab = visual; agents must reconcile two entry points — **écart de gouvernance** mild |
| Web quality | `playup-web-quality.mdc` vs `src/PlayUp/web/README.md` CI table | README mentions legacy CSS hors CI; rules enforce oxlint/prettier — **consistent** |
| Architecture | `architecture.mdc` vs code | Wave 2: largely aligned |

### 4.4 Obsolete instructions in rules vs repo

| Rule / doc | Drift |
| --- | --- |
| `playup-web-quality.mdc` / web docs | Still document `css:audit`, `tile:check` — **AUD-0001** |
| `audit-before-pr` command | Suggests `typecheck` not `build` — **masks AUD-0002** if author skips `npm run build` |

**No rule file modified in wave 5.**

---

## 5. Quality controls (static analysis)

### 5.1 .NET

| Mechanism | Setting |
| --- | --- |
| Analyzers | `AnalysisMode=AllEnabledByDefault`, NetAnalyzers + StyleCop + Roslynator (`build/code-analysis.props`) |
| Warnings as errors | **Not** enabled globally |
| Global `NoWarn` | `SYSLIB0013`, `SYSLIB1054`, `NU5104`, `NU5017`, `NETSDK1206`, `NETSDK1057` |
| `IDE0005` | Suppressed when `GenerateDocumentationFile != true` (`Directory.build.props`) |
| Per-project `NoWarn` | Host: CA1515, CA2007; tests: +CA1031, CA2234, SA1204; etc. (see `baseline.md`) |

**Qualification:** suppressions are **documented policy candidates** for phase 2 — not automatically defects.

### 5.2 Web

| Control | Config | CI |
| --- | --- | --- |
| Oxlint | `--deny-warnings` | yes — **AUD-0006** |
| Prettier | `format:check` | yes — **AUD-0007** |
| Stylelint | `src/**/*.css` | yes — **AUD-0003**; README notes some legacy page CSS **out of scope** intentionally |
| Typecheck | app + node only | yes |
| Build | `tsc -b` includes tests | yes — **AUD-0002** |
| Coverage threshold | none in CI | — |

### 5.3 Known CI red signals @ SHA (cross-reference)

| Signal | Finding |
| --- | --- |
| .NET test 1 fail | AUD-0004 |
| Web build fail | AUD-0002 |
| Web lint:css | AUD-0003 |
| Web format/lint | AUD-0006, AUD-0007 |
| NU1608 on restore/build | AUD-0005 |

---

## 6. Register summary (wave 5)

| ID | Action |
| --- | --- |
| AUD-0008 | Extended — deployment factors, Media, TLS/CORS/AllowedHosts |
| AUD-0005 | Extended — `dotnet list package --vulnerable` clean; NU1608 unchanged |
| AUD-0002 | Referenced — CI build gate vs Playwright |
| AUD-0015 | Referenced — E2E scope independent of build |
| **AUD-0016** | **New** — npm audit 13 advisories in devDependencies (vite/vitest); prod 0 |
| **AUD-0017** | **New** — supply-chain audits absent from CI (NuGet/npm) |

**Next free reference:** AUD-0018.

## Phase 1 closure

Waves 1–5 complete @ `93b09f16…`. Phase 2: arbitrate AUD-0008 deployment model, AUD-0004/C2, AUD-0002 build graph, warning policy, E2E scope (**AUD-0015**), and Cursor rule updates — before corrective lots.
