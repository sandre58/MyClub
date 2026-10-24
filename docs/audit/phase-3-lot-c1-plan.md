# Phase 3 — Lot C1 plan (AUD-0008)

**Status:** Implemented per Notion authorization 2026-10-21 (Tier **A1–A4** + **B1** + **B3**; B4 and Tier C excluded).

**Prerequisite:** CI green on `802d077f` — jobs `web` and `e2e-integration` (met before C1 implementation).

**Decision SoT:** Notion — [Play'Up Host — exposition locale (dette auth)](https://app.notion.com/p/40075fb92d10818390f6dcceef89a75d) (A + trajectoire C). Git evidence: `docs/audit/phase-1-wave-5.md` §1.

---

## 1. Targeted audit (@ branch `feature/new_version`, post-C5)

### 1.1 Host HTTP surface

| Item | Evidence | Note |
| --- | --- | --- |
| Listen URL (default dev) | `Properties/launchSettings.json` → `http://localhost:5287` | Loopback-oriented default |
| Override | `dotnet run --urls …`, `ASPNETCORE_URLS` | **Can bind all interfaces** — no Host guard today |
| `AllowedHosts` | `appsettings.json` → `"*"` | Any Host header accepted when deployed behind unknown topology |
| AuthN / AuthZ | `Program.cs` — no `AddAuthentication` / `UseAuthorization` | All `MapCompetition*`, `MapStage*`, `MapMatch*`, `MapMedia*` anonymous if reachable |
| CORS | Not configured in Host | Dev: Vite proxy same-origin to browser; prod cross-origin SPA not solved in repo |
| TLS / HSTS | Not in `Program.cs` | Expected gap for local tool; **blocking** for Internet per decision |
| Request size | Kestrel defaults; Media 2 MiB domain check | No extra Host-level cap found (wave 5) |

### 1.2 Media (unchanged behaviour — document only)

| Route | Exposure if Host reachable |
| --- | --- |
| `POST /media` | Anonymous upload (type/size allowlist) |
| `GET /media/{id}/content` | Public cache (`max-age=31536000`) |

**C1 must not** tighten Media policy without a separate product decision.

### 1.3 Related tooling (contrast)

| Component | Guard |
| --- | --- |
| DevRunner `DevDatabaseGuard` | DB `*_dev`, **localhost-only** host for reset/seed |
| Play’Up Host | **No** equivalent network guard |
| Vite dev (`playwright.host.config.ts`) | `127.0.0.1:5173` in CI E2E job |
| CI `e2e-integration` | Host `--urls http://127.0.0.1:5287` |

### 1.4 Gap vs product decision

Decision **A** (local organizer) assumes bind loopback or equivalent. Repo **documents** `localhost:5287` but does **not enforce** loopback at Host startup — misconfiguration (LAN bind, port forward, `0.0.0.0`) turns accepted auth debt into **de facto open API**.

---

## 2. Exposure scenarios (non exhaustive)

| ID | Scenario | Typical bind | Untrusted client risk |
| --- | --- | --- | --- |
| S1 | Solo developer | `localhost` / `127.0.0.1` | Low — aligns with decision A |
| S2 | Rider compound Host + Web | launchSettings localhost | Low if unchanged |
| S3 | Club LAN (same Wi‑Fi) | `http://0.0.0.0:5287` or machine hostname | **High** — full read/write API + Media |
| S4 | Port forwarding / tunnel without auth | Public IP → dev Host | **Critical** — violates trajectoire C guardrails |
| S5 | CI / automated E2E | `127.0.0.1` in job script | Controlled — document as non-prod pattern |
| S6 | Future hosted prod | Reverse proxy + TLS (not implemented) | Requires auth + TLS decision before go-live |

**C1 does not** decide that Play’Up is “local only” in code; it makes **S1/S2** the documented default and **S3–S4** explicit misuse.

---

## 3. Proposed measures (for arbitration — not committed)

### Tier A — Documentation & operator guidance (low functional risk)

| # | Deliverable | Purpose |
| --- | --- | --- |
| A1 | New guide `docs/guides/host-exposure.md` | Exposure model A + trajectoire C; S1–S6 table; “do not bind WAN/LAN without decision” |
| A2 | Cross-links | `local-persistence.md`, `src/PlayUp/web/README.md`, Host `README` or contributor pointer |
| A3 | `SECURITY.md` | Short “Play’Up Host local deployment” pointer (not duplicating Notion decision text) |
| A4 | Run configuration note | `.run/PlayUp Host` uses launchSettings — warn if custom `--urls` |

**Acceptance:** A new contributor can answer: default exposure, what changes risk, what is out of scope for C1.

### Tier B — Configuration defaults (functional — each line needs explicit OK)

| # | Candidate | Effect | Regression risk |
| --- | --- | --- | --- |
| B1 | `appsettings.Development.json` → `AllowedHosts`: `localhost;127.0.0.1` | Rejects odd Host headers in dev | Low for local; **breaks** dev via custom hostname unless configured |
| B2 | `launchSettings` comment / secondary profile | Document `http://127.0.0.1:5287` as canonical | None |
| B3 | Startup **warning** (Serilog) when `IServerAddressesFeature` contains non-loopback in Development | Operator signal only | None on API contract |
| B4 | Startup **fail-fast** non-loopback in Development | Strong guard | **Breaks** intentional LAN dev — needs product OK |
| B5 | `compose.yml` / Postgres port commentary | DB exposure on LAN if Docker publishes 5432 | Doc only unless compose change arbitrated |

**Recommendation for arbitration:** ship **A\*** + **B1 + B3** first; defer **B4** unless decision maker explicitly wants enforceable dev bind.

### Tier C — Explicitly excluded from C1

- ASP.NET Authentication / Authorization / API keys
- TLS termination, HSTS, certificate management in Host
- CORS policy design for third-party SPAs
- Media upload auth, quotas, virus scan, CDN policy
- Network firewall / Docker network isolation automation
- Production deployment manifests

---

## 4. Validation & non-regression

| Check | Type |
| --- | --- |
| `dotnet build` + `dotnet test` (Release) | Unchanged pass |
| `dotnet test` Host.Tests — no new failures | Baseline |
| Optional: small test on `AllowedHosts` / config binding | Only if B1 implemented — document expected behaviour |
| `npm run test:run` / web CI job | Unaffected |
| Manual: Host on localhost — SPA proxy still works | S1 smoke |
| Manual: document scenario S3 — state “unsupported without gateway” | Doc review |

**No change** to HTTP contracts, status codes, or Media payloads unless a separate decision says otherwise.

---

## 5. Expected diff (when authorized)

```
docs/guides/host-exposure.md          (new)
docs/guides/local-persistence.md      (link + 1 paragraph)
docs/audit/phase-3-lot-c1-plan.md     (status → implemented / link commits)
src/PlayUp/MyClub.PlayUp.Host/        (optional appsettings.Development.json, Program.cs warning)
SECURITY.md                           (optional pointer)
.github/workflows/ci.yml              (only if CI bind docs/comments — unlikely)
```

Estimated size: **small** (mostly docs); **≤30 lines** Host code if B3 only.

---

## 6. Acceptance criteria (lot closure)

- [x] Exposure model **A + trajectoire C** is explicit in Git contributor docs (with Notion link).
- [x] Untrusted-client scenarios **S3–S4** documented with concrete misuse examples (`--urls`, Docker, port forward).
- [x] Proposed guardrails (Tier A + agreed Tier B) implemented or explicitly deferred with reason in Notion task.
- [x] Media and API behaviour **unchanged** unless listed and arbitrated.
- [x] Auth / TLS / Media policy tracked as **future decisions**, not smuggled into C1.
- [x] AUD-0008 register row updated: C1 technical closure vs remaining trajectoire C work (Notion — post-merge @ `bbb76630`).

---

## 7. Authorization gate

| Required before code | Owner |
| --- | --- |
| CI C5 green (`web` + `e2e-integration`) | CI |
| Written authorization (Notion page or audit task) naming allowed Tier A/B items | Decision maker |
| Explicit deferral list for B4 and any compose change | Decision maker |

**C5 CI green ≠ C1 authorization** — stability prerequisite only.
