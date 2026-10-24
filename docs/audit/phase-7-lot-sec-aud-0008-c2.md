# Phase 7 — Lot P7-Sec — AUD-0008 trajectoire C (suite C2)

**Go:** 2026-10-22  
**Status:** **Clos** après tests T1–T7 + T3–T5 intégration.

**Prérequis:** lot C1 + [phase-7-lot-sec-aud-0008-c.md](phase-7-lot-sec-aud-0008-c.md) (bind warn Development, doc S3/S4).

## Arbitrage (lot C2)

| Inclus | Exclu (arbitrage produit requis) |
| --- | --- |
| `appsettings.Production.json` — `AllowedHosts` loopback | ASP.NET Authentication / `[Authorize]` sur les 97 routes |
| Refus bind non-loopback en **non-Development** (opt-in `PlayUp:Host:AllowNonLoopbackBind`) | TLS, HSTS, CORS, `UseForwardedHeaders` |
| Doc `host-exposure.md` + `SECURITY.md` | Modèle S6 (reverse proxy, hostnames publics) |
| Tests T5–T7 (régression + garde Production) | Changements CI workflow |

**Signal décideur:** la trajectoire C **métier** (auth + TLS avant Internet hébergé) reste **non implémentée** — ce lot ferme le durcissement **repo** possible sans plateforme d’identité.

## Livrables

| ID | Fichier / comportement |
| --- | --- |
| CX-1 (Production) | `appsettings.Production.json` → `127.0.0.1;localhost` |
| S3 (Production) | `HostListenExposureGuard` — shutdown si bind non-loopback sans opt-in |
| H1 (Dev) | Inchangé — warnings seulement |

## Tests

| ID | Description |
| --- | --- |
| T1–T2 | Unit — messages loopback / non-loopback (Development) |
| T3–T4 | Intégration — API OK ; Development `Host` rejeté |
| T5 | Production — `Host` arbitraire → **400** (plus de `*`) |
| T6–T7 | Unit — logique refus bind Production + opt-in |

## Registre AUD-0008

**Dette acceptée** maintenue pour **auth/TLS/S6** ; mitigation exposition **renforcée** (Production loopback contract).

## Références

- Notion [AUD-0008](https://app.notion.com/p/40075fb92d10818390f6dcceef89a75d) — trajectoire C ; compléter la page après merge.
- `docs/guides/host-exposure.md`
