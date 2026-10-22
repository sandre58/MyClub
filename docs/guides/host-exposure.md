# Play'Up Host — network exposure (local tool)

This guide explains **who can reach the Play'Up HTTP API** when you run `MyClub.PlayUp.Host`, what the repository assumes by default, and what is **out of scope** in the current product phase.

**Product decision (SoT):** [Play'Up Host — exposition locale (dette auth)](https://app.notion.com/p/40075fb92d10818390f6dcceef89a75d) — model **A** (local organizer) with **trajectoire C** (authentication and TLS required before any Internet or multi-tenant hosting).

Git audit context: `docs/audit/phase-1-wave-5.md` §1, lot **C1** (`docs/audit/phase-3-lot-c1-plan.md`), lot **P7-Sec** (`docs/audit/phase-7-lot-sec-aud-0008-c.md`).

## What the Host exposes

| Surface | Behaviour if the process is reachable |
| --- | --- |
| Play'Up API | **No ASP.NET authentication or authorization** — all mapped competition, structure, stage, match endpoints are anonymous. |
| Media API | **Anonymous** `POST /media` (type/size allowlist) and public `GET /media/{id}/content` (long cache). |
| TLS / HSTS / CORS | Not configured in Host; local dev typically uses HTTP + Vite proxy. |

Treating the Host as “safe because it is dev” is only valid when **only trusted local clients** can open a TCP connection to the listening address.

## Default listen URL

| Source | Typical value |
| --- | --- |
| `Properties/launchSettings.json` | `http://localhost:5287` |
| Rider / VS **PlayUp Host** run config | Uses launch profile (see [`.run/PlayUp Host.run.xml`](../../.run/PlayUp%20Host.run.xml)) |
| CI E2E (`e2e-integration`) | `http://127.0.0.1:5287` (scripted; not a production pattern) |

Overrides: `dotnet run --urls …`, `ASPNETCORE_URLS`, or IDE **applicationUrl** edits.

**Canonical local bind for day-to-day work:** loopback (`localhost` or `127.0.0.1`). The Vite dev server proxies to the Host; see [Play'Up Web README](../../src/PlayUp/web/README.md).

## Bind address vs `AllowedHosts` vs NAT / tunnel

These layers are **independent**. None of them replaces authentication or a firewall.

| Layer | What it controls | In this repository |
| --- | --- | --- |
| **Kestrel bind** (`--urls`, `ASPNETCORE_URLS`, `launchSettings` `applicationUrl`) | Which **network interfaces** accept TCP connections to the Host process | Default loopback; operator can bind `0.0.0.0` or `+` (scenario S3) |
| **`AllowedHosts`** (`appsettings*.json`) | Which HTTP **`Host` header** values the app accepts | Base `appsettings.json` → `"*"`; Development override → `localhost;127.0.0.1` only |
| **NAT / port forward / tunnel** (S4) | Whether clients **outside the LAN** can reach a listening port | **Not configured or detected by Host** — operator and network equipment |

A **Development startup warning** for non-loopback bind addresses signals that other interfaces *may* reach the anonymous API. It does **not** prove WAN or “Internet” exposure; that also depends on firewall rules, routing, and port forwarding.

## `AllowedHosts` provisional default (AUD-0008-C)

| File | Value | When it applies |
| --- | --- | --- |
| `appsettings.json` | `"AllowedHosts": "*"` | Always loaded; **only** effective when no environment-specific override |
| `appsettings.Development.json` | `localhost;127.0.0.1` | When `ASPNETCORE_ENVIRONMENT=Development` (typical IDE / `launchSettings`) |

There is **no** `appsettings.Production.json` in the repository. Processes started with **Production** (for example CI `dotnet run` without `ASPNETCORE_ENVIRONMENT`) therefore keep `AllowedHosts: *` while still often binding loopback via `--urls`.

**Debt (trajectoire C):** `*` is **provisional** until a hosting model defines legitimate public hostnames (often behind a reverse proxy). Replacing `*` blindly with `localhost;127.0.0.1` could break a future proxy deployment. `AllowedHosts` filters headers; it does **not** stop anonymous API access when TCP is open, and it is **not** a substitute for TLS or authentication.

## Exposure scenarios

| ID | Scenario | Typical bind | Risk if untrusted clients can connect |
| --- | --- | --- | --- |
| S1 | Solo developer | `localhost` / `127.0.0.1` | Low — aligns with decision A |
| S2 | IDE compound Host + Web | launchSettings localhost | Low if URLs stay loopback |
| S3 | Club LAN / Wi‑Fi | `http://0.0.0.0:5287`, machine hostname, `*:port` | **High** — full read/write API + Media upload |
| S4 | Port forward / tunnel without auth | Public IP → dev Host | **Critical** — violates trajectoire C |
| S5 | Automated E2E | `127.0.0.1` in CI | Controlled; isolated Postgres + seed |
| S6 | Future hosted production | Reverse proxy + TLS (not in repo) | Requires **auth + TLS** product decision before go-live |

Scenarios **S3–S4** are **unsupported misuse** of the current Host: there is no API key, VPN integration, or firewall automation in this repository.

### Examples of risky overrides

```bash
# Binds all interfaces — any device on the LAN can call the API (S3)
dotnet run --project src/PlayUp/MyClub.PlayUp.Host --urls http://0.0.0.0:5287

# Same effect via environment
set ASPNETCORE_URLS=http://+:5287
```

Port forwarding from a home router to a machine running the Host without a gateway equates to **S4**.

## Development guardrails (lot C1)

These reduce accidental misconfiguration; they **do not** replace network isolation or future authentication.

| Measure | Effect |
| --- | --- |
| `appsettings.Development.json` → `AllowedHosts` | Restricts acceptable HTTP `Host` headers to `localhost` and `127.0.0.1` when `ASPNETCORE_ENVIRONMENT=Development`. Use a local override (same file, gitignored elsewhere) if you deliberately use another dev hostname. |
| Startup warning (Development) | **Warning** when listen URLs use a **non-loopback** host (e.g. `0.0.0.0`): includes the URL, notes other interfaces may reach the API, points here and to `--urls` / `launchSettings`. Does **not** stop startup and does **not** claim WAN exposure. |

Production / non-Development configurations still use `AllowedHosts: *` in base `appsettings.json` until a separate hosting decision exists.

## Related tooling

| Component | Network posture |
| --- | --- |
| **DevRunner** `DevDatabaseGuard` | Refuses reset unless DB name ends with `_dev`, host is loopback, and environment is not Production. |
| **Docker Compose** (`compose.yml`) | PostgreSQL on `localhost:5432` for developers — distinct from Host HTTP bind; lock down published ports on shared networks. |
| **Play'Up Web** | Browser talks to Vite; API calls go through the dev proxy to the Host URL you configure. |

Database setup: [local-persistence.md](local-persistence.md).

## Explicitly not covered here

The following require **separate** product or security decisions (trajectoire C); they are **not** implemented in lot C1:

- ASP.NET Authentication / Authorization
- TLS termination, HSTS, certificate management in Host
- CORS policy for third-party SPAs
- Media upload authentication, quotas, malware scanning
- Automated firewall or Docker network policies

Report vulnerabilities through [SECURITY.md](../../SECURITY.md).
