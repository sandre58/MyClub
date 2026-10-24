# Phase 7 — Lot P7-Ops — Health + draw Host E2E

**Go:** 2026-10-22  
**Scope (borné):** observabilité déploiement minimale + chemin critique #3 (signal régression), sans auth/TLS/CI élargi.

## Livrables

| Item | Détail |
| --- | --- |
| **Health** | `GET /health` — `PlayUpDatabaseHealthCheck` (`CanConnectAsync`, tag `ready`) |
| **CI readiness** | `e2e-integration` attend `/health` au lieu de `/competitions` |
| **Host E2E draw** | `e2e/business/cup-draw.spec.ts` — seed `cup-composition-complete`, flux API create → inputs → generate → publish-and-apply |
| **Seed CI** | `championship-ready,cup-composition-complete` |
| **Matrice** | `docs/audit/critical-paths.md` path #3 |

## Hors scope

- Materialize E2E (path #2)
- Draw **UI** Playwright (dialog Structure)
- Liveness séparée / sonde Media DB
- Trajectoire C AUD-0008 restante

## Vérification locale

```bash
dotnet test tests/MyClub.PlayUp.Host.Tests -c Release --filter "FullyQualifiedName~HealthEndpointTests"
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios championship-ready,cup-composition-complete
# Host :5287 puis npm run test:e2e:host dans src/PlayUp/web
```

## Statut

**Clos** après tests verts (Host integration + E2E en CI).
