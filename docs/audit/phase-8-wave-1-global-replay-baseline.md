# Phase 8 — Wave 1 — Global replay baseline

**Launched:** 2026-10-22 (go décideur — vague 1 phase 8)  
**Mode:** investigation only — **no application changes** in this wave.

## Reference

| Field | Value |
| --- | --- |
| Git ref | `main` |
| SHA | `422aba3378832a55e23c280df145acd462580440` |
| Compared to | Phase 0 [`baseline.md`](baseline.md) @ `93b09f16` · Phase 6 wave 1 @ `4b6b3fe7` |

## Environment

| Item | Observed |
| --- | --- |
| OS | Windows (local agent) |
| Node | v24.18.0 |
| npm | 11.16.0 (noise: `Unknown env config "devdir"`) |
| .NET | Release build + test OK locally |
| `bash` / `gh` | Unavailable locally — `verify-dotnet-warnings-baseline.sh` and `gh run` not replayed here |

---

## 1. P8-Ctrl — Mandatory controls (CI contract)

Aligned with `.github/workflows/ci.yml` jobs **build** and **web** (stub Playwright not re-run locally — duration / browser deps).

### 1.1 .NET (`build` job)

| Control | Exit | Result |
| --- | ---: | --- |
| `dotnet build MyClub.slnx -c Release` | **0** | Build succeeded |
| Warning summary | — | **1** warning — **CA1859** on `StageRepository.cs:302` (`HydrateOrderedCollectionsAsync` — `IReadOnlyList<Stage>` vs `List<Stage>`) |
| `verify-dotnet-warnings-baseline.sh` | **(predicted fail)** | Baseline [`dotnet-warnings-baseline.json`](dotnet-warnings-baseline.json) allows **0** total and only **SA1143** / **SA1137** by code — **CA1859 is a new code** → CI gate would reject (introduced with P7 AUD-0012 D2-B hydration) |
| `dotnet test -c Release --no-build` | **0** | All suites green (incl. Application.Tests — **fixed** vs phase 6 wave 1 red test) |

**vs phase 0:** NU1608 / web red state largely remediated in phases 3–6.  
**vs phase 6 wave 1:** tests green; **new** .NET warnings regression on current `main`.

### 1.2 Play’Up web (`web` job)

Run from `src/PlayUp/web`.

| Control | Exit | Result |
| --- | ---: | --- |
| `npm run format:check` | **1** | **4 files** need Prettier: `e2e/business/cup-draw.spec.ts`, `groups-materialize.spec.ts`, `hostSeed.ts`, `package.json` (P7 E2E / ops commits) |
| `npm run lint` | **0** | Oxlint OK |
| `npm run lint:css` | **0** | Stylelint OK |
| `npm run typecheck` | **0** | OK |
| `npm run test:run` | **0** | **140** files, **1132** tests |
| `npm run build` | **0** | Entry chunk `index-*.js` **283.73 kB** min / **86.10 kB** gzip — **no** Vite `> 500 kB` warning on entry (P7 bundle lot) |
| `npm run i18n:check` | **0** | 16/16 parity (not in CI; green) |
| `npx playwright test` (stub) | — | **Not run** locally |

**vs phase 6 wave 1:** lint/css/typecheck/tests/build improved; **format:check** would block CI if job runs on this SHA.

### 1.3 Garde-fous (not re-run)

| Guard | Note |
| --- | --- |
| Host publish isolation | Covered by `HostArchitectureIsolationTests` in `dotnet test` |
| `npm audit --omit=dev` | Not re-run — AUD-0016 prod gate historically clean |
| `e2e-integration` (Host + Postgres) | Not re-run locally — P7 added `/health` + draw/materialize seeds |

---

## 2. P8-Reg — Register reconciliation (Git SoT)

See detail: [`phase-8-register-reconciliation.md`](phase-8-register-reconciliation.md).

| Check | Result |
| --- | --- |
| AUD-0001…0017 closure phase 3 | 14 × Vérifié · 3 × Dette acceptée (0009, 0010, 0012) — unchanged intent |
| Post-phase 3–7 deltas | AUD-0005-C baseline 0 warnings **broken** by CA1859 (signal technique, not registre row) |
| AUD-0008 | Mitigation C+C2 livrée · auth/TLS/S6 **dette acceptée** |
| AUD-0012 | Dette acceptée + mitigations D2-B — cohérent |
| Reportés produit | AUD-0004, F5 — documentés phase 5–7 |
| **P0/P1 orphelins** | **Aucun** identifié hors statut explicite dans bilans Git |

**Action registre Notion:** optionnel — ajouter note CA1859 / gate warnings si clôture chantier exige CI verte (pas de nouvelle référence AUD sans arbitrage).

---

## 3. P8-Path — Parcours critiques

Source: [`critical-paths.md`](critical-paths.md) (post P7).

| Gap (déjà documenté) | Statut vague 1 |
| --- | --- |
| Draw **UI** E2E (path #3 depth) | Signal résiduel — Host API slice OK |
| Materialize **UI** E2E (path #2 depth) | Signal résiduel — Host API slice OK |
| Progression assignment (path #5) | Stub partial — inchangé |

**Conclusion:** acceptable pour clôture chantier **si** décideur confirme (aligné phase 7 closure).

---

## 4. P8-Cursor — Snapshot (light)

| Check | Result |
| --- | --- |
| File count | **22** rule files under `.cursor/rules/` (vs **21** + commands in [`cursor-rules-inventory.md`](cursor-rules-inventory.md) phase 0 — C8 added `AGENTS.md`, rules touched) |
| Conflicts connus | **Aucun** signalé en vague 1 — revue formelle reportée à `phase-8-cursor-rules-reconciliation.md` |

---

## 5. Chantier closure criteria (plan directeur) — snapshot

| Critère | Vague 1 |
| --- | --- |
| Builds backend + frontend | **OK** local |
| Tests obligatoires | **OK** local (`dotnet test`, Vitest) |
| P0/P1 arbitrés | **OK** (Git bilans) |
| Zéro warning contrôles retenus | **NON** — CA1859 + format Prettier |
| Parcours critiques | **OK** avec gaps UI documentés |
| Garde-fous CI | **Risque rouge** sur `main` tant que CA1859 / Prettier non traités |
| Dette documentée | **OK** |
| Règles Cursor | **À compléter** (stream dédié) |

---

## 6. Écarts confirmés — lots correctifs proposés (hors vague 1)

| Id | Écart | Piste | Effort |
| --- | --- | --- | --- |
| **P8-W1-A** | CA1859 `StageRepository` | Ajuster signature `List<Stage>` **ou** mettre à jour baseline + justification AUD-0005-C | Faible |
| **P8-W1-B** | Prettier 4 fichiers web/e2e | `npm run format` ciblé | Très faible |

**Recommandation:** go **lot unique P8-W1** (A+B) avant clôture phase 8 / statut Notion **Terminé**, sans rouvrir P7.

---

## 7. Prochaines étapes

1. Décideur : **go lot P8-W1** (correctifs gate) ou accepter dette + assouplir critère clôture (non aligné plan directeur).
2. Exécuter `phase-8-cursor-rules-reconciliation.md` (revue ciblée).
3. Vérifier CI distante sur `422aba33` (ou post P8-W1).
4. Puis `phase-8-closure.md` + Notion.
