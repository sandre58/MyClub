# Phase 7 — Wave 1 — Security, performance & operations baseline

**Launched:** 2026-10-22  
**Executed:** 2026-10-22 (investigation complète)  
**Go décideur:** vague 1 investigation only (2026-10-22)  
**Mode:** investigation — **no product code**, **no CI gate changes**, **no corrective lot** during this wave.

**Objectif:** alimenter l’arbitrage du **premier lot** par des **preuves** — **sans** trancher Sec vs Perf vs Ops avant résultats.

**Reference SHA:** `feature/ci-obs-01-phase-c` @ `a6764084` (mesures rejouées sur cette branche).

---

## 0. Contrat rappelé (validé Notion)

| Règle | Détail |
| --- | --- |
| Périmètre indicatif | P7-Sec, P7-Perf, P7-Ops (parallèle en vague 1) |
| Exclusions | S4-INV, doublons CSS, heuristiques i18n, fixtures/orga, F5, refactor gros TSX |
| AUD-0008 | Priorité **analyse** post-go — pas d’impl auth/TLS sans lot |
| AUD-0012 | **Uniquement** via protocole comparatif reproductible (§3) — pas d’optim EF « au feeling » |
| Heuristiques phase 6 | Report-only (S1) |

---

## 1. P7-Sec — baseline (faits confirmés vs signaux)

### 1.1 Faits confirmés (sources existantes + relecture @ `a6764084`)

| Topic | Observation | Source |
| --- | --- | --- |
| **AUD-0008** | Host Play’Up : pas de `AddAuthentication` / `UseAuthorization` / `[Authorize]` sur la surface API produit (modèle confiance réseau / local) | `docs/audit/phase-1-wave-5.md` §1.1 ; grep `src/PlayUp` @ wave 1 |
| **TLS / HSTS** | Pas de `UseHttpsRedirection` / HSTS dans `Program.cs` | `MyClub.PlayUp.Host/Program.cs` |
| **CORS** | Pas de `AddCors` / `UseCors` | grep Host @ wave 1 |
| **Forwarded headers** | Pas de `UseForwardedHeaders` | grep Host @ wave 1 |
| **AllowedHosts** | `appsettings.json` → `"*"` ; Development → `localhost;127.0.0.1` | `appsettings*.json` |
| **Décision** | Arbitrage phase 2 : option A + trajectoire C (auth/TLS avant exposition Internet) | Notion AUD-0008 / phase 3 closure |
| **Secrets** | `UserSecretsId` sur Host ; chaînes via `ConnectionStrings:PlayUp` / `Media` — pas de secrets committés dans Git | `MyClub.PlayUp.Host.csproj`, `Program.cs` |
| **Dépendances** | `dotnet list MyClub.slnx package --vulnerable --include-transitive` → **aucun** package vulnérable signalé | exécution wave 1 |
| **npm prod** | `npm audit --omit=dev` (web) → **0** vulnérabilités | exécution wave 1 ; dev advisories = dette AUD-0016 (politique phase 3) |

### 1.2 Cartographie endpoints (lecture seule)

**Total:** **97** appels `MapGet` / `MapPost` / `MapPut` / `MapDelete` dans `MyClub.PlayUp.Host/Endpoints/` (tous **sans** auth middleware).

| Fichier | Maps | Préfixes / domaines |
| --- | ---: | --- |
| `CompetitionEndpoints.cs` | 27 | `/competitions`, reads workspace/overview/structure/consultation/attention, lifecycle commandes |
| `StageEndpoints.cs` | 45 | `/stages/{id}/*` (draw, slots, progression, regulation, etc.) |
| `MatchEndpoints.cs` | 17 | `/matches`, lifecycle, lineups, events |
| `StructureEndpoints.cs` | 4 | structure opérationnelle (fiche / strip) |
| `MediaEndpoints.cs` | 4 | `/media` upload, metadata, **content binaire public** (`Cache-Control: public, max-age=31536000`) |

**Exposition effective (wave 1):**

| Cible | Comportement actuel | Risque signal (pas scoring produit) |
| --- | --- | --- |
| **Local dev** | HTTP `localhost:5287`, Host + Vite proxy | Accepté par modèle AUD-0008 A |
| **LAN / binding non loopback** | `HostListenExposureWarnings` en Development uniquement | Signal si `--urls` élargi sans trajectoire C |
| **Internet** | Pas TLS/HSTS/auth ; `AllowedHosts: *` en prod template | **Bloquant** pour exposition publique (déjà arbitré → trajectoire C) |

### 1.3 Checklist vague 1 — Sec

- [x] Cartographie endpoints publics (Host + Media) — inventaire ci-dessus.
- [x] CORS, forwarded headers, secrets — aucun CORS/forwarded ; secrets via config standard.
- [x] `dotnet list package --vulnerable` — clean @ `a6764084`.
- [x] npm prod advisories — 0 ; politique dev inchangée (AUD-0016).

### 1.4 Livrable Sec — risque × exposition cible

| Risque | Local dev | LAN (HTTP) | Internet |
| --- | --- | --- | --- |
| API métier sans auth | Atténué (loopback) | **Élevé** si Host joignable | **Inacceptable** sans lot C |
| Media content GET public | Idem | Fuite média si IDs devinables | Idem + cache long |
| AllowedHosts `*` (prod json) | N/A | Host header abuse si joignable | Renforcer avec lot C |
| Pas TLS | OK en dev | Signal | Requis avant Internet |

**Implémentation:** aucune en vague 1 — preuves suffisantes pour **cadrer** un lot AUD-0008 trajectoire C, pas pour choisir le périmètre technique (JWT, reverse proxy, etc.) sans arbitrage produit.

---

## 2. P7-Perf — baseline

### 2.1 Faits / signaux

| Topic | Mesure @ `a6764084` | Référence antérieure |
| --- | --- | --- |
| Bundle Vite `index-*.js` | **1 164.57 kB** min / **301.96 kB** gzip (`index-BmJ2zCKr.js`) ; warning Vite >500 kB | ~1 165 kB / ~302 kB (phase 6) — **stable** |
| CSS `index-*.css` | 269.88 kB / 37.17 kB gzip | signal inchangé |
| Warnings .NET | Gate CI inchangé | `dotnet-warnings-baseline.json` |
| **AUD-0012** | Baselines SQL **rejouées** (§2.2) | Aligné `phase-4-d2` (ex. cup-qf-sf 8→17 SQL) |

### 2.2 Scénarios `Category=PerformanceBaseline` (6 tests, tous verts)

**Commande:** `dotnet test tests/MyClub.PlayUp.Development.Tests -c Release --filter "Category=PerformanceBaseline"`

| Test class | Scénario | Comptes SQL (labels test) |
| --- | --- | --- |
| `StageRepositoryLoadBaselineTests` | cup-qf-sf single stage | Summary **8**, Structure **14**, Full **17**, ForUpdate **17** |
| `StageRepositoryLoadBaselineTests` | GetByIdsReadOnly(Full) N=2 | **18** |
| `StageRepositoryLoadBaselineTests` | Attention StageCapabilities | **17** |
| `ReadPerformanceBaselineTests` | groups:running many matches | overview 21, detail 3, stage matches 18, matches-hub 19, fan-out 21 — **TOTAL listed 82** |
| `ReadPerformanceBaselineTests` | cup-qf-sf multi-stage | overview/structure/consultation/attention 21 each ; matches-hub 19 ; fan-out stages 33 ; **TOTAL listed 286** |
| `ReadPerformanceBaselineTests` | champions-league:running heavy | overview 21, stage matches 18 — **TOTAL listed 39** |

**Interprétation wave 1:** reproduction fidèle des baselines phase 4 — **pas de nouveau delta** mesuré sur cette branche. Un lot AUD-0012 exige une **hypothèse ciblée** + comparaison avant/après (§3), pas une optimisation opportuniste.

### 2.3 Checklist vague 1 — Perf

- [x] `npm run build` @ SHA courant — métriques ci-dessus.
- [x] Scénarios PerformanceBaseline listés et exécutés.
- [x] Aucune modification Includes EF / profils repository.

### 2.4 Livrable Perf — signal vs mesuré

| Candidat lot futur | Preuve vague 1 | Verdict |
| --- | --- | --- |
| AUD-0012 D2-B (repository) | Baseline SQL stable ; écart Summary→Full documenté (8→17) | **Insuffisant seul** — besoin hypothèse + PR comparatif §3 |
| Bundle / code-split | Signal stable ~1.16 MB ; pas de régression mesurée | **Insuffisant** pour lot prioritaire sans objectif produit (perf perçue, TTI) |
| .NET warnings | Gate existant | Hors premier lot sauf décision contraire |

---

## 3. AUD-0012 — protocole de mesure (obligatoire avant tout lot D2-B)

Reprise normative de `phase-4-d2-stage-repository-investigation.md` §6 :

| Exigence | Détail |
| --- | --- |
| **Comparatif** | Avant / après **ou** profil A vs B sur **mêmes** scénarios |
| **Reproductible** | Même projet test (`MyClub.PlayUp.Development.Tests`), fixture Postgres Testcontainers, `SqlCommandCounterInterceptor` |
| **Scénarios figés** | `StageRepositoryLoadBaselineTests` + `ReadPerformanceBaselineTests` (`Category=PerformanceBaseline`) |
| **Commande** | `dotnet test … --filter "Category=PerformanceBaseline"` — comptes SQL **par label** dans la sortie test (voir §2.2) |
| **Métriques définies à l’avance** | Nb commandes SQL / scénario ; latence optionnelle même machine |
| **Interdit en vague 1** | Refactor repository, réduction Include « opportuniste », changement comportement métier |

**Archive wave 1 (référence comparative future):** lignes §2.2 @ `a6764084`.

**Critère « preuve suffisante pour lot »:** écart mesuré > bruit + hypothèse de changement liée au hot path identifié + critères d’acceptation du lot documentés.

---

## 4. P7-Ops — baseline (exploitation)

### 4.1 Faits confirmés

| Topic | Source / observation @ wave 1 |
| --- | --- |
| CI | `build`, `web`, `e2e-integration` (Host E2E Postgres) — `.github/workflows/ci.yml`, `critical-paths.md` |
| Stub Playwright vs Host métier | `critical-paths.md` |
| Dev local | DevRunner + `docs/guides/local-persistence.md` |
| **Health / readiness** | **Aucun** `AddHealthChecks` / `MapHealthChecks` dans `src/PlayUp` (grep wave 1) |
| **Logging** | Serilog : console template (Development), `CompactJsonFormatter` (non-Development) ; request logging Information | `Program.cs` |
| **Run deps** | Postgres (`ConnectionStrings:PlayUp`) ; Host **5287** ; Vite proxy → `VITE_API_PROXY_TARGET` défaut `http://localhost:5287` | `launchSettings.json`, `vite.config.ts`, `web/README.md` |
| **E2E Host** | Seed `championship-ready` via DevRunner ; spec match lifecycle | `critical-paths.md`, `playwright.host.config.ts` |

### 4.2 Signaux (gaps couverture)

| Chemin critique | Signal |
| --- | --- |
| #2 Materialize | Vitest / .NET — pas E2E Host dédié |
| #3 Draw | Pas E2E Host (seed `cup-draw-pending` mentionné futur) |
| **Observabilité déploiement** | Pas d’endpoint health pour orchestration / sonde |

### 4.3 Checklist vague 1 — Ops

- [x] Health/readiness — **gap confirmé** (aucun endpoint).
- [x] Logging structuré — documenté (pas de changement).
- [x] Dépendances run — Postgres, port 5287, proxy Vite documentés.

### 4.4 Livrable Ops — parcours critique × signal

| # | Parcours | Signal régression actuel | Gap vague 1 |
| --- | --- | --- | --- |
| 1 | Structure hub | Host.Tests + stub E2E | — |
| 2 | Materialize | .NET + Vitest | Pas E2E Host |
| 3 | Draw | .NET endpoint tests | Pas E2E Host |
| 4 | Match lifecycle | Host E2E **oui** | — |
| 5–8 | Progression, shell, accueil, overview | stub / Vitest | Host partiel |

**Lot E2E optionnel (post-arbitrage):** draw (#3) ou materialize (#2) — **non engagé** en vague 1. Lot health checks = signal ops distinct (pas de preuve « urgent » sans cible déploiement).

---

## 5. Synthèse vague 1 → arbitrage lot unique

**Exécution wave 1 terminée** — le décideur choisit **un** lot ; les autres restent en file.

| Stream | Preuve suffisante pour lot ? | Candidat lot (titre provisoire) | Bloquant / note |
| --- | --- | --- | --- |
| **Sec** | **Oui (cadrage)** — surface 97 endpoints sans auth, gaps TLS/CORS/hosts confirmés ; trajectoire C déjà arbitrée métier | **AUD-0008 — trajectoire C** (auth + durcissement exposition avant Internet) | Périmètre technique (proxy vs middleware, identity) = **arbitrage produit** dans le lot |
| **Perf** | **Non (priorisation)** — baselines rejouées, **aucun nouveau delta** vs phase 4 | AUD-0012 D2-B ciblé **ou** bundle split | Lot Perf **après** hypothèse écrite + critères §3 ; bundle seul = signal faible |
| **Ops** | **Partiel** — gaps E2E #2/#3 + health documentés | Health/readiness **ou** E2E draw (`cup-draw-pending`) | Utile si prochain jalon = déploiement ou draw vertical ; pas bloquant dev local |

**Lecture recommandée (sans imposer le choix):** le flux **Sec / AUD-0008 C** est le seul dont la vague 1 confirme un **écart explicite** avec l’exposition Internet déjà refusée par décision — les flux Perf et Ops apportent des **baselines** et des **files d’attente** plutôt qu’un déclencheur nouveau.

**Règle:** un seul lot retenu au premier arbitrage post–wave 1.

---

## 6. Reproducibility (wave 1)

```bash
git checkout a6764084   # ou branche courante feature/ci-obs-01-phase-c

# Sec
rg "Map(Get|Post|Put|Delete)" src/PlayUp/MyClub.PlayUp.Host/Endpoints --glob "*.cs"
rg "AddAuthentication|UseAuthorization|MapHealthChecks|UseCors" src/PlayUp --glob "*.cs"
dotnet list MyClub.slnx package --vulnerable --include-transitive

# Perf AUD-0012
dotnet test tests/MyClub.PlayUp.Development.Tests -c Release \
  --filter "Category=PerformanceBaseline" \
  --logger "console;verbosity=detailed"

cd src/PlayUp/web && npm run build
cd src/PlayUp/web && npm audit --omit=dev

# Ops
# docs/audit/critical-paths.md + Host launchSettings (5287) + vite.config.ts proxy
```

---

## 7. Limits

- Aucune modification produit ni CI dans cette vague.
- Choix Sec vs Perf vs Ops : **§5** pour arbitrage décideur uniquement.
- Mesures bundle / SQL valides pour le SHA indiqué ; re-archiver après merge significatif sur `main`.
