# Phase 7 — Lot P7-Sec — AUD-0008 trajectoire C (Internet exposure)

**Status:** §4 arbitrage **figé** (décideur 2026-10-22) — §4.3 perimeter + tests **defined**. **No product PR** until explicit **go implémentation** on this frozen §4.3 (separate from arbitrage commit).

**Priority:** P0 (first phase 7 corrective lot).

**Wave 1 evidence:** [`phase-7-wave-1-security-perf-ops-baseline.md`](phase-7-wave-1-security-perf-ops-baseline.md) @ commit documenting investigation (SHA `a6764084`).

**Decision SoT (unchanged):** Notion — [Play'Up Host — exposition locale (dette auth)](https://app.notion.com/p/40075fb92d10818390f6dcceef89a75d) — model **A** + **trajectoire C** (authentication and TLS before Internet or multi-tenant hosting). Phase 2 arbitration; C1 closed documentation/guardrails only (`docs/audit/phase-3-lot-c1-plan.md`, `docs/guides/host-exposure.md`).

**Deferred (explicit):** AUD-0012 (baseline only — no refactor lot). Ops file: health checks, draw/materialize E2E.

---

## 1. Arbitrage décideur (2026-10-22)

| Point | Decision |
| --- | --- |
| **Priority** | P0 |
| **Scope** | Trajectoire C already arbitrated for AUD-0008 — **strictly** reduce **Internet** exposure already identified and **refused** |
| **First step** | Re-read phase 2 decision + **precisely inventory** surfaces in scope (not “fix all 97 routes”) |
| **Out of scope** | Global authentication redesign; blanket TLS/CORS/HSTS middleware; business refactor; CI workflow changes |
| **Done** | Target exposure reduced per existing decision, with **targeted** tests and non-regression proof |
| **Stop** | If trajectoire C cannot define an **authorized implementation perimeter**, return to arbitration **before** code |

### 1.1 How to read wave 1 (mandatory)

Wave 1 prioritizes security **investigation**; it does **not** authorize changing the whole exposure surface.

| Wave 1 fact | Meaning for this lot |
| --- | --- |
| 97 routes without ASP.NET auth | **Context** — anonymous API if TCP-reachable; not proof each route is exploitable in isolation |
| No CORS / TLS / HSTS in Host | **Signal** for Internet hosting — not a mandate to add all three generically in one PR |
| `dotnet list package --vulnerable` clean | Supply-chain snapshot — **not** application-layer risk absence |
| `npm audit --omit=dev` clean | Same for prod npm tree — dev advisories remain AUD-0016 policy |

**Internet exposure (product):** already **refused** until trajectoire C measures exist. This lot implements **only** what is needed to enforce that refusal in-repo (or document residual operator duty if enforcement belongs outside Host).

---

## 2. Relationship to lot C1

| Layer | C1 (phase 3, closed) | This lot (trajectoire C — partial) |
| --- | --- | --- |
| Intent | Document model A; dev guardrails (AllowedHosts dev, non-loopback warning) | Close the **gap** between “Internet refused” and **concrete** repo behaviour / operator contract |
| Auth/TLS | Explicitly excluded (Tier C) | **Not** full auth platform — only what perimeter step 1 authorizes |
| Media policy | Document only | No tightening without separate product decision (same as C1) |

---

## 3. Step 1 — Inventory findings (read-only, 2026-10-22)

**Method:** grep + file read on `main` post wave 1 commits; no product, config, or CI edits.

### 3.1 Exposure scenarios S1–S6 (definitions)

Source of truth for scenario IDs: [`docs/guides/host-exposure.md`](../../docs/guides/host-exposure.md) § Exposure scenarios.

| ID | Summary |
| --- | --- |
| S1 | Solo developer, loopback bind |
| S2 | IDE compound Host + Web, loopback |
| S3 | Club LAN — non-loopback Kestrel bind |
| S4 | Port forward / tunnel without auth → Internet path |
| S5 | CI / automated E2E on loopback |
| S6 | Future hosted production (reverse proxy + TLS) |

**Internet access model (repo facts):** the repository ships **no** Internet-facing deployment manifest (no Host Dockerfile, no K8s/ingress, no reverse-proxy config). Internet reachability today is **not** a supported product path; it arises only from **operator/network misconfiguration** (especially S3→S4), or from a **future** S6 topology not defined in Git.

### 3.2 `AllowedHosts` — where `*` applies and how config loads

| Layer | Evidence | Behaviour |
| --- | --- | --- |
| Base config | `src/PlayUp/MyClub.PlayUp.Host/appsettings.json` → `"AllowedHosts": "*"` | Loaded for **every** environment unless overridden |
| Development override | `appsettings.Development.json` → `"localhost;127.0.0.1"` | Merged when `IHostEnvironment.EnvironmentName` is **Development** (ASP.NET Core default configuration order: `appsettings.json` then `appsettings.{Environment}.json`) |
| Other environments | **No** `appsettings.Production.json` / `Staging.json` in repo | **Production**, **Staging**, or any non-Development name → **only** base `*` applies |
| Default dev run | `Properties/launchSettings.json` sets `ASPNETCORE_ENVIRONMENT=Development` | Rider/VS `dotnet run` profile → Development override active |
| CI Host E2E | `.github/workflows/ci.yml` runs `dotnet run … --urls http://127.0.0.1:5287` **without** setting `ASPNETCORE_ENVIRONMENT` | Default environment is **Production** → `AllowedHosts: *` at runtime (loopback bind limits remote TCP; Host header filter is permissive) |
| Integration tests | `PlayUpWebApplicationFactory` calls `UseEnvironment("Development")` | Host.Tests use Development `AllowedHosts` restriction |

**Note:** `AllowedHosts` filters the HTTP **`Host` header**; it does **not** replace firewall, bind address, TLS, or authentication.

### 3.3 Reverse proxy vs Host (no automatic compensation)

| Concern | In this repository | Typical reverse proxy (S6 — **not defined here**) |
| --- | --- | --- |
| TLS termination | **Not** in `Program.cs` | Proxy / platform |
| HSTS | **Not** configured | Proxy or Host (product choice) |
| CORS | **Not** configured; Vite dev uses same-origin proxy (`vite.config.ts`) | Proxy or Host if SPA on another origin |
| Authentication | **No** `AddAuthentication` / `[Authorize]` on API maps | Proxy (mTLS, OAuth gateway) **or** Host (future lot) |
| Client IP trust | **No** `UseForwardedHeaders` | Proxy must not be assumed; forwarded headers need explicit Host config |
| Bind / listen | Kestrel URLs from `--urls` / `ASPNETCORE_URLS` / launch profile | Proxy listens public; upstream often private HTTP to Host |
| Internet refusal | Documented misuse (S3–S4); Dev-only non-loopback **warning** (`HostListenExposureWarnings.cs`) | Operator network + proxy ACLs **required** for any public edge |

**Rule for this lot:** choosing proxy-side controls **does not** authorize skipping Host-side measures agreed in §4, and Host-side docs/guards **do not** imply a proxy exists.

### 3.4 API anonymity (context — not per-route scoring)

When TCP to Kestrel is open, **all** mapped Play'Up + Media endpoints are reachable without ASP.NET auth (`Program.cs` pipeline; wave 1 route count 97). Model **A** accepts anonymity on loopback (S1/S2). This lot **does not** authorize securing each route individually unless a §4 row explicitly says so.

### 3.5 Tests — existing vs minimal (proposed, not implemented)

| Kind | Evidence | Gap |
| --- | --- | --- |
| **Functional Host** | `tests/MyClub.PlayUp.Host.Tests/*` via `PlayUpWebApplicationFactory` (Development) | Proves API contracts, **not** exposure/bind/`AllowedHosts` |
| **Exposure guard** | `HostListenExposureWarnings` — **no** dedicated unit/integration test in repo | C1 plan optional AllowedHosts test — **not** present |
| **DevRunner DB** | `DevDatabaseGuard` — loopback + `_dev` suffix | Protects **Postgres reset**, not HTTP Host |
| **CI E2E** | `e2e-integration` curls `http://127.0.0.1:5287/competitions` | Loopback smoke; **not** an Internet-exposure test |
| **Minimal additions (candidates)** | To agree when validating §4 “In lot” rows | e.g. test: non-loopback URL logs warning in Development; test: request with disallowed `Host` rejected when Development `AllowedHosts` active; **avoid** CI workflow edits unless re-arbitrated |

**Gate:** décideur validates §4 **Decision** column → then implementation PR + only the tests bound to **In lot** rows.

---

## 4. Authorized perimeter — scenario matrix (figé 2026-10-22)

**Legend — expected access from Internet:**

| Label | Meaning |
| --- | --- |
| **Not reachable** | No supported repo path publishes Host to the Internet; process not routable without external misconfiguration |
| **Forbidden** | Product decision (trajectoire C): must not be offered as anonymous Internet API |
| **N/A** | Topology or component not defined in repository |

**Legend — Decision:**

| Label | Meaning |
| --- | --- |
| **In lot** | May be changed in P7-Sec implementation after §4 validation |
| **Out of scope** | No change in this lot (document or maintain status quo) |
| **Non arbitré** | Expected access or responsible layer cannot be fixed precisely — **stop** and re-arbitrate before implementing |

### 4.1 Scenarios S1–S6

| Scenario | Surface | Current access (proof) | Expected from Internet | Responsible control | Proof | Decision |
| --- | --- | --- | --- | --- | --- | --- |
| **S1** | Kestrel listen URL default | `launchSettings.json` → `http://localhost:5287`; anonymous API if client can open TCP | **Not reachable** (loopback) | Operator (bind URL) | `Properties/launchSettings.json`, `host-exposure.md` S1 | **Out of scope** — aligns with model A |
| **S2** | IDE compound Host + Vite proxy | Same loopback default; browser → Vite → proxy → Host (`vite.config.ts`, `VITE_API_PROXY_TARGET`) | **Not reachable** (loopback) | Operator (URLs) + dev tooling | `vite.config.ts`, `web/README.md` | **Out of scope** — maintain S1/S2 dev path |
| **S3** | Non-loopback bind (`0.0.0.0`, `+`, LAN hostname) | Kestrel accepts operator `--urls` / `ASPNETCORE_URLS`; **Development:** Serilog **Warning** only (`HostListenExposureWarnings.cs`); anonymous full API on LAN | **Forbidden** if extended to untrusted networks (LAN misuse); not Internet unless S4 | **Primary:** operator bind + network; **Host:** **warn only** (no fail-fast) | `host-exposure.md` S3, `HostListenExposureWarnings.cs` | **In lot** — **warn only**; actionable message (§4.3); **no** startup block |
| **S4** | Port forward / tunnel → Host port | Repo does not configure NAT/tunnel; if forward exists, anonymous API is world-reachable | **Forbidden** | **Primary:** operator / edge network; Host cannot detect NAT | `host-exposure.md` S4, `SECURITY.md` | **In lot (partial)** — documentation + **bind-linked** warnings/verification only (§4.3); **no** TLS/CORS/HSTS/forwarded headers/global auth |
| **S5** | CI `e2e-integration` Host | `ci.yml`: `dotnet run … --urls http://127.0.0.1:5287`; likely **Production** env → `AllowedHosts: *` | **Not reachable** from Internet (runner loopback) | CI operator + GitHub network | `.github/workflows/ci.yml` L179–199 | **Out of scope** — **no CI change** per arbitrage; document Production+`*` behaviour |
| **S6** | Future hosted production | **Not implemented** — no proxy/TLS/auth topology in Git | **Forbidden** until trajectoire C complete (future **controlled** public access with auth+TLS) | **Non arbitré** split proxy vs Host | `host-exposure.md` S6, § Explicitly not covered | **Non arbitré** — **no implementation** in this lot until hosting model exists |

### 4.2 Cross-cutting surfaces (supporting S3–S4 / Production template)

| Scenario | Surface | Current access (proof) | Expected from Internet | Responsible control | Proof | Decision |
| --- | --- | --- | --- | --- | --- | --- |
| **CX-1** | `AllowedHosts: *` (non-Development) | Any `Host` header accepted when environment ≠ Development | Does not create Internet route; **does not mitigate** misbound public listener | Host configuration (+ operator env files) | `appsettings.json`, §3.2 | **In lot (doc debt)** — **keep `*` provisionally**; document risk + absence of Production hostnames; **not** sufficient protection; final value deferred until hosting model exists |
| **CX-2** | Play'Up HTTP API maps (97) | Anonymous when TCP open; no `[Authorize]` | **Forbidden** on Internet as anonymous API | Future auth lot / edge gateway | wave 1 baseline §1.2, `Program.cs` | **Out of scope** — global auth excluded |
| **CX-3** | Media `GET /media/{id}/content` | Public cache headers; anonymous | **Forbidden** on Internet (same as API) | Product + future auth; **not** tightened in C1 | `MediaEndpoints.cs` L65–77, C1 plan §1.2 | **Out of scope** — separate product decision |
| **CX-4** | Vite dev server (SPA) | Default dev server bind (not Host); can expose UI separately from Kestrel | **Forbidden** if operator exposes dev server to WAN | Operator / Vite CLI | `vite.config.ts` (proxy only documents Host target) | **Out of scope** — Host lot; cross-link in docs if needed |
| **CX-5** | Postgres via `compose.yml` | `ports: "5432:5432"` on localhost Docker publish | DB port **forbidden** on untrusted networks if published to LAN | Operator Docker / firewall | `compose.yml`, `host-exposure.md` Related tooling | **Out of scope** — not HTTP Host; optional doc pointer only |

### 4.3 Frozen implementation package (décideur 2026-10-22)

**Arbitrage summary**

| Topic | Decision | Notes |
| --- | --- | --- |
| **S3** | **Warn only** | No fail-fast while legitimate hosting scenarios are undefined |
| **CX-1** | **Keep `*`** (provisional) | Document debt; do not replace with `localhost;127.0.0.1` blindly (future proxy hostnames unknown) |
| **S4** | **Docs + bounded bind measures** | Distinguish network bind, `AllowedHosts`, and NAT/tunnel; no implicit network hardening |

**Explicitly out of this PR (unchanged):** S1, S2, S5, S6, CX-2, CX-3, CX-4, CX-5; no TLS, CORS, HSTS, `UseForwardedHeaders`, global authentication, CI workflow edits, Media policy change.

#### 4.3.1 Documentation deliverables (S4 + CX-1)

| # | Deliverable | Content |
| --- | --- | --- |
| D1 | `docs/guides/host-exposure.md` | Table or section: **bind address** (who can open TCP) vs **`AllowedHosts`** (HTTP Host header) vs **NAT/tunnel** (operator/network — Host cannot detect). State that a bind warning ≠ proof of Internet exposure. |
| D2 | Same guide + `SECURITY.md` pointer | **CX-1 debt:** `AllowedHosts: *` in base `appsettings.json`; no `appsettings.Production.json`; provisional until hosting SoT defines expected hostnames; wildcard is **not** a substitute for auth or firewall. |
| D3 | S3/S4 misuse | Retain S3–S4 examples (`--urls`, port forward); cross-link Notion AUD-0008 without duplicating decision text in code comments. |

#### 4.3.2 Host code deliverables (bounded — bind + warning only)

| # | Change | In scope? |
| --- | --- | --- |
| H1 | **Actionable non-loopback warning** (`HostListenExposureWarnings` or equivalent) | **Yes** — must include: detected listen URL(s); statement that non-loopback listen may expose the anonymous API to other interfaces; pointer to `host-exposure.md` / `--urls` / `launchSettings`; **must not** claim “Internet exposure detected” |
| H2 | Fail-fast on non-loopback | **No** (S3) |
| H3 | Change `AllowedHosts` defaults | **No** (CX-1 keep `*`) |
| H4 | TLS / CORS / HSTS / forwarded headers / auth middleware | **No** (S4 / lot exclusions) |
| H5 | Extend warning to non-Development | **Optional** only if needed for parity; still **warn only**; default proposal: keep current **Development-only** hook unless PR proves Production CI needs the same signal without CI file changes |

#### 4.3.3 Automated tests (exact — `MyClub.PlayUp.Host.Tests`)

New focused fixture (e.g. `HostExposureWarningTests`) using `WebApplicationFactory<Program>` + log capture (`ILoggerProvider` / `FakeLogCollector`) unless an existing pattern is preferred.

| ID | Scenario | Setup | Assertion |
| --- | --- | --- | --- |
| **T1** | Loopback bind, Development | `UseEnvironment("Development")` + listen URL `http://127.0.0.1:{port}` (or `localhost`) | **No** `HostExposure` warning at Information/Warning for non-loopback message |
| **T2** | Non-loopback bind, Development | `UseEnvironment("Development")` + `UseUrls("http://0.0.0.0:0")` or explicit LAN URL | **Exactly one** warning per non-loopback listen URL; message contains the **URL**; text explains **other interfaces** may reach the API; references exposure guide; **does not** contain “Internet” / “public Internet” style definitive exposure claim |
| **T3** | Warn-only (S3) | Same as T2 | Host starts; `GET /competitions` (or health-less smoke) returns **not** 503 from blocked startup |
| **T4** | Development `AllowedHosts` (regression — B1) | Development factory default | Request with disallowed `Host` header → **400** (existing behaviour preserved) |
| **T5** | CX-1 — Production wildcard unchanged | `UseEnvironment("Production")` + typical test request with arbitrary `Host` | Request **succeeds** (not rejected by AllowedHosts) — guards against accidental tightening |
| **T6** | CI unchanged | No new workflow steps | Existing `e2e-integration` job untouched; local verify optional |

**Not in scope:** tests that require real NAT, firewall, or WAN connectivity.

#### 4.3.4 Manual recette (configuration warning ≠ network exposure)

| Step | Action | Pass criterion |
| --- | --- | --- |
| R1 | Start Host Development, loopback `--urls http://127.0.0.1:5287` | No exposure warning in console |
| R2 | Start Host Development, `--urls http://0.0.0.0:5287` | Warning visible with bind URL + guide pointer; process still serves API locally |
| R3 | **Without** changing router/NAT | Confirm warning alone does **not** imply WAN reachability (document operator checklist: port forward = S4 misuse) |
| R4 | S1/S2 smoke | Vite proxy + SPA load competition list against loopback Host |

---

## 4.4 Gate before product PR

| Gate | Status |
| --- | --- |
| S3 / CX-1 / S4 arbitrage | **Done** (§4.1–4.2, §4.3 table) |
| §4.3 tests + recette defined | **Done** (§4.3.3–4.4) |
| Out-of-scope scenarios unchanged | **Confirmed** (§4.3 explicit list) |
| **Explicit go implémentation** | **Pending** — décideur authorizes PR against frozen §4.3 only |
| S6 hosting model | **Still non arbitré** — remains out of lot |

---

## 5. Acceptance criteria (lot closure)

- [x] §4 perimeter filled and aligned with Notion trajectoire C (no scope creep).
- [ ] Internet exposure refusal **demonstrated** (code, config, and/or documented operator contract — as agreed in §4).
- [ ] **No** generalized auth on all 97 routes unless explicitly added to §4 and re-arbitrated.
- [ ] **No** CI workflow changes unless separately arbitrated.
- [ ] Targeted automated tests green; existing Host / Development integration paths unchanged for supported S1/S2 local dev.
- [ ] Contributor docs updated (`host-exposure.md` or linked guide) — English, link Notion only in docs not code comments.
- [ ] AUD-0008 register / Notion status updated post-merge.

---

## 6. Progress

- [x] Wave 1 baseline published (Git).
- [x] First lot arbitrated: P7-Sec / AUD-0008-C (this document).
- [x] Step 1 inventory (read-only) → §4 draft matrix + §4.4 stop check.
- [x] Décideur validates §4 — S3 warn only; CX-1 provisional `*` + doc debt; S4 doc + bind warnings.
- [ ] **Go implémentation** explicite on §4.3.
- [ ] Implementation PR (§4.3.1–4.3.2 + tests §4.3.3 only).
- [ ] Lot closure + phase 7 continuation (other streams remain queued).
