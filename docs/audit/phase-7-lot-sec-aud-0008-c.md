# Phase 7 — Lot P7-Sec — AUD-0008 trajectoire C (Internet exposure)

**Status:** **Authorized** — décideur 2026-10-22 (post wave 1). **No implementation** until step 1 inventory is complete and perimeter is explicit.

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

## 3. Step 1 — Inventory (required before code)

Deliverable: a short **perimeter table** in this file (§4, to be filled) mapping:

| Dimension | Questions |
| --- | --- |
| **Exposure scenarios** | Reuse S1–S6 from `host-exposure.md` — which does this lot **change** vs **document**? |
| **Bind / listen** | Loopback default, non-loopback bind, `AllowedHosts` in non-Development |
| **Reverse proxy / hosted** | What stays operator responsibility vs Host code? |
| **API anonymity** | Which paths **must** remain anonymous for local A until a future auth lot? |
| **Tests** | Host.Tests / Development.Tests / docs — what proves “Internet path blocked” or “misbind detected”? |

**Gate:** décideur or author confirms §4 perimeter **before** implementation PR.

---

## 4. Authorized perimeter (TBD — fill after step 1)

_To be completed after inventory. If empty at implementation time → **stop** per arbitrage._

| Measure candidate | In scope? | Rationale | Tests |
| --- | --- | --- | --- |
| _TBD_ | | | |

---

## 5. Acceptance criteria (lot closure)

- [ ] §4 perimeter filled and aligned with Notion trajectoire C (no scope creep).
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
- [ ] Step 1 inventory → §4 perimeter.
- [ ] Implementation (single bounded PR or sequenced sub-PRs per §4 only).
- [ ] Lot closure + phase 7 continuation (other streams remain queued).
