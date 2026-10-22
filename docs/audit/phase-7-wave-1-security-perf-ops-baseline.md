# Phase 7 — Wave 1 — Security, performance & operations baseline

**Launched:** 2026-10-22  
**Go décideur:** vague 1 investigation only (2026-10-22)  
**Mode:** investigation — **no product code**, **no CI gate changes**, **no corrective lot** during this wave.

**Objectif:** alimenter l’arbitrage du **premier lot** par des **preuves** — **sans** trancher Sec vs Perf vs Ops avant résultats.

**Reference SHA:** `main` @ `5dea8411` (post cadrage phase 7).

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

### 1.1 Faits confirmés (sources existantes)

| Topic | Observation | Source |
| --- | --- | --- |
| **AUD-0008** | Host Play’Up : pas de `AddAuthentication` / `UseAuthorization` / `[Authorize]` sur la surface API produit (modèle confiance réseau / local) | `docs/audit/phase-1-wave-5.md` §1.1 |
| **TLS / HSTS** | Pas de `UseHttpsRedirection` / HSTS dans `Program.cs` (signal déploiement Internet) | idem |
| **AllowedHosts** | `appsettings.json` → `"*"` ; Development → localhost restreint | `MyClub.PlayUp.Host/appsettings*.json` |
| **Décision** | Arbitrage phase 2 : option A + trajectoire C (auth/TLS avant exposition Internet) | Notion AUD-0008 / phase 3 closure |

### 1.2 Signaux à compléter (checklist vague 1 — lecture seule)

- [ ] Cartographie endpoints publics (minimal APIs Host + Media) — grep `MapGet` / `MapPost` inventaire.
- [ ] CORS, forwarded headers, secrets (user secrets, env vars) — config + `docs/`.
- [ ] Dépendances vulnérables : `dotnet list package --vulnerable` (CI déjà sur solution).
- [ ] npm prod advisories : politique phase 3 (dev advisories = dette AUD-0016).

### 1.3 Livrable Sec (fin vague 1)

Tableau **risque × exposition cible** (local dev vs LAN vs Internet) — **sans** décision d’implémentation.

---

## 2. P7-Perf — baseline

### 2.1 Faits / signaux déjà documentés

| Topic | Type | Référence |
| --- | --- | --- |
| Bundle Vite `index-*.js` ~1 165 kB / gzip ~302 kB | Signal (build OK) | `phase-6-wave-1-static-baseline.md`, `phase-5-reconciliation` |
| Warnings .NET | Confirmé gate | `dotnet-warnings-baseline.json` + CI script (phase 4 D1) |
| **AUD-0012** | Dette + shapes documentés | `phase-4-d2-stage-repository-investigation.md` |

### 2.2 Checklist vague 1

- [ ] Rejouer (ou citer) métriques bundle @ SHA courant (`npm run build` — hors doc agent si environnement restreint).
- [ ] Lister scénarios `Category=PerformanceBaseline` existants (Development.Tests).
- [ ] **Ne pas** modifier Includes EF ni profils repository en vague 1.

### 2.3 Livrable Perf

Synthèse **signal vs mesuré** — candidats lot futur classés « preuve suffisante / insuffisante ».

---

## 3. AUD-0012 — protocole de mesure (obligatoire avant tout lot D2-B)

Reprise normative de `phase-4-d2-stage-repository-investigation.md` §6 :

| Exigence | Détail |
| --- | --- |
| **Comparatif** | Avant / après **ou** profil A vs B sur **mêmes** scénarios |
| **Reproductible** | Même projet test (`MyClub.PlayUp.Development.Tests`), fixture Postgres Testcontainers, `SqlCommandCounterInterceptor` |
| **Scénarios figés** | `StageRepositoryLoadBaselineTests` + `ReadPerformanceBaselineTests` (`Category=PerformanceBaseline`) |
| **Commande** | `dotnet test … --filter "Category=PerformanceBaseline"` — enregistrer comptes SQL **par label** dans la sortie test |
| **Métriques définies à l’avance** | Nb commandes SQL / scénario ; latence optionnelle même machine ; pas de conclusion sur une seule exécution sans baseline de référence |
| **Interdit en vague 1** | Refactor repository, réduction Include « opportuniste », changement comportement métier |

**Critère « preuve suffisante pour lot »:** écart mesuré > bruit + hypothèse de changement liée au hot path identifié + critères d’acceptation du lot documentés.

---

## 4. P7-Ops — baseline (exploitation)

### 4.1 Faits confirmés

| Topic | Source |
| --- | --- |
| CI : `build`, `web`, `e2e-integration` (Host E2E Postgres) | `.github/workflows/ci.yml`, `critical-paths.md` |
| Stub Playwright vs Host métier | `critical-paths.md` |
| Dev local | DevRunner + `docs/guides/local-persistence.md` (référence contributeur) |

### 4.2 Signaux (gaps couverture)

| Chemin critique | Signal |
| --- | --- |
| #2 Materialize | Vitest / .NET — pas E2E Host dédié |
| #3 Draw | Pas E2E Host (seed `cup-draw-pending` mentionné futur) |

### 4.3 Checklist vague 1

- [ ] Inventaire health/readiness endpoints Host (grep `Health` / `MapHealthChecks`) — résultat attendu : à documenter ici.
- [ ] Logging structuré / niveaux prod — lecture `appsettings` + `Program.cs` (pas de changement).
- [ ] Documenter dépendances run (Postgres, ports 5287, Vite proxy).

### 4.4 Livrable Ops

Matrice **parcours critique × signal de régression** — proposition de lot E2E **optionnelle** post-arbitrage (pas engagée en vague 1).

---

## 5. Synthèse vague 1 → arbitrage lot unique

À remplir en clôture wave 1 :

| Stream | Preuve suffisante pour lot ? | Candidat lot (titre provisoire) | Bloquant |
| --- | --- | --- | --- |
| Sec | TBD | AUD-0008 périmètre C (?) | TBD |
| Perf | TBD | AUD-0012 D2-B (?) / bundle (?) | Protocole §3 |
| Ops | TBD | E2E draw/materialize (?) | TBD |

**Règle:** un seul lot retenu au premier arbitrage post–wave 1 ; les autres restent en file ou dette.

---

## 6. Reproducibility (wave 1)

```bash
# Référence
git checkout 5dea8411

# Sec (exemple)
rg "Map(Get|Post|Put|Delete)" src/PlayUp/MyClub.PlayUp.Host --glob *.cs

# Perf AUD-0012 (comparatif — ne pas interpréter sans baseline archivée)
dotnet test tests/MyClub.PlayUp.Development.Tests -c Release --filter "Category=PerformanceBaseline"

# Web signal bundle
cd src/PlayUp/web && npm run build

# Ops
# Voir critical-paths.md + CI workflow
```

---

## 7. Limits

- Pas de modification produit ni CI dans ce document.
- Mesures locales non rejouées dans cet artefact initial : compléter §1–4 checkboxes lors de l’exécution contributeur.
- Pas de choix Sec/Perf/Ops avant §5 complété.
