# CTRL-O3-MAT — DevRunner recette (gate Matchs / production en Suspendu)

**Status:** Reference recipe — **gate de validation** post-gel produit ([Matrice UI Structure](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4) · **O-03** fermée).  
**Does not reopen** O-03 or lifecycle V2. Separates **presentation** checks from **capacity** debts (C-18/19/20).

## Purpose

Prove that Structure N2 does **not** offer an **actionable** materialize/production path when the domain contract forbids it for a **Suspended** phase (and related competition/stage combinations), while keeping **consultative** progress and lawful **dematerialize** when the read model exposes it.

Evidence required per case (not screenshot-only):

| Layer | What to verify |
|-------|----------------|
| **Domain / command** | `Materialize*` rejected when stage ≠ Ready (and competition rules for Cup) |
| **Read model** | `materialize.commandEnabled`, `disabledReason`, `dematerialize.enabled` |
| **SPA policy** | `classifyMaterializePresentation`, `shouldShowProductionInMatchsSection`, `showMaterializePrimary` (`presentation === 'actionable'`) |
| **UI** | No enabled primary Materialize button; frozen/blocked copy not pretending actionable |
| **Mutation attempt** | POST materialize → 4xx with expected code when UI hides/disables (spot-check) |

Automated coverage:

- **Integration:** `tests/MyClub.PlayUp.Development.Tests/CtrlO3MatRecetteIntegrationTests.cs` (read model after seed)
- **SPA unit:** `structureMatchsSectionUi.test.ts`, `structureMaterializeCopy.test.ts`, `StructurePhaseOperationalStrip.test.tsx`

Manual / DevRunner UI pass still recommended after seed (Structure fiche → Aside Matchs).

## DevRunner seed

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios ctrl-o3-mat-recette
```

Creates **seven** competitions (search workspace list for prefix `CTRL-O3-MAT`):

| ID | Competition display name | Stage / competition | Production state |
|----|--------------------------|---------------------|------------------|
| **S1** | `CTRL-O3-MAT S1 — Groups suspendu (comp+phase)` | Groups · stage **Suspended** · competition **Suspended** | 3/6 matches materialized, exploitation mid-run |
| **S2** | `CTRL-O3-MAT S2 — Groups phase suspendue (comp Running)` | Groups · stage **Suspended** · competition **Running** | Same 3/6 — **isolated stage pause** (tableau groupes **plein** — lifecycle / production gate) |
| **S3** | `CTRL-O3-MAT S3 — Championnat suspendu` | Qual→Forme champ · stage **Suspended** · competition **Running** | 6/6 materialized, Started then Suspended |
| **S4** | `CTRL-O3-MAT S4 — Swiss suspendu` | Swiss 8×3 · stage **Suspended** · competition **Suspended** | Progressive rounds mid-run (Swiss exception path) |
| **S5** | `CTRL-O3-MAT S5 — Cup suspendu places vides (comp Running)` | Cup 8 · stage **Suspended** · comp **Running** | Composition 8/8 · **0/8** bracket occupied (Δ-O3-1 jauge places vides) |
| **S6** | `CTRL-O3-MAT S6 — Cup suspendu places partielles (comp Running)` | Cup 8 · stage **Suspended** · comp **Running** | Composition 8/8 · **2/8** bracket occupied (1 paire tirée) |
| **S7** | `CTRL-O3-MAT S7 — Cup suspendu déficit population (comp Running)` | Cup 8 · stage **Suspended** · comp **Running** | Composition **5/8** · rail Équipes + `PopulationShortfall` |

### Δ-O3-1 visual recette (S5–S7)

After seed, open Structure → select competition → phase fiche. Assert **pause** copy (FR/EN per locale), **no** diagnostic ⚠️ on the gauge gap lines, **no** Materialize primary, **Resume phase** available.

| Row | URL pattern | Placement gauge | Population / TEAMS rail |
|-----|-------------|-----------------|-------------------------|
| **S5** | `/competitions/{S5-competitionId}/structure` | `0/8 OCCUPIED` · `emptyPaused` (no “fill the bracket now” tone) | 8/8 entries complete (not the shortfall row) |
| **S6** | `/competitions/{S6-competitionId}/structure` | `2/8 OCCUPIED` · `partialPaused` | 8/8 entries complete |
| **S7** | `/competitions/{S7-competitionId}/structure` | `0/8` or unschematic until draw — focus **TEAMS** meter `5/8` · `shortfallPaused` / `populationShortfallPaused` in strip, **no** diagnostic icon on paused gap |

Competition IDs are **stable** for a given identity scope after `--reset` (S1–S4 unchanged). Example after seed:

| Row | `competitionId` (deterministic scope) |
|-----|----------------------------------------|
| S1 | `f7d53519-533a-5c0a-be31-56f8602c43f9` (`ctrl-o3-s1`) |
| S2 | `264e8653-628f-584d-9a90-8ed78fa9b677` (`ctrl-o3-s2`) |
| S3 | `2c4ae578-e4ef-5662-8e1a-98ed26fb4266` (`ctrl-o3-s3`) |
| S4 | `a95b3550-18b4-58de-a14a-8f0f500dc45e` (`ctrl-o3-s4`) |
| S5 | `2be391d0-3133-58b9-9028-aba9cd058501` (`ctrl-o3-s5`) |
| S6 | `94073383-f111-5e18-a991-7c81aa62258e` (`ctrl-o3-s6`) |
| S7 | `f56556d9-91fb-5b76-81e1-f087a182ce0a` (`ctrl-o3-s7`) |

**S2** remains the reference for **Groups** suspended + production gate; **S5–S7** cover cup bracket + population pause copy without changing S1–S4 behaviour.

### Legacy seeds (still valid for spot checks)

| Scenario | Use when |
|----------|----------|
| `groups-suspended` | Same family as **S1** (single competition) |
| `qual-form-champ-running` | Baseline **Running** before manual Suspend — compare **S3** |
| `structure-materialize-recette` (`RECETTE R1`) | Running 3/6 frozen — contrast with **S1/S2** Suspended |

## Scenario matrix (product assertions)

| Scenario | Assertion attendue | Read model (minimum) | SPA (minimum) |
|----------|-------------------|----------------------|---------------|
| Phase **Suspended**, production non disponible | Aucun CTA Materialize **actif** | `materialize.commandEnabled === false` | `presentation !== 'actionable'` → `showMaterializePrimary === false` |
| Phase **Suspended**, jauge `frozen` (3/6) | État lisible ; pas de promesse d’action | `disabledReason` often `StructureNotMutable` when partial | `shouldShowProductionInMatchsSection` **true** only if gauge/demat path applies; **no** primary materialize button |
| **Suspended** + demat autorisé | Demat reste accessible si exposé | `dematerialize != null` and `enabled === true` when capability allows | Demat control visible; materialize primary absent |
| Compétition **Suspended**, phase **Running** | Ne pas inférer depuis la compétition seule | Assert **hub** fields for the **displayed** stage | Use **S2** (comp Running, stage Suspended) + Cup domain tests (`competition.Suspend` blocks Cup materialize on Ready stage) |
| **Running → Suspend** | Pas d’apparition d’un materialize invalide | Before/after: `commandEnabled` stays false on exploitation | Same as Suspended rows |
| **Swiss** | Exception documentée (progressive round / demat scope) | **S4**: `materialize` hub **null** (no batch path); assert **no** Materialize primary in Matchs; demat scope may be `LastSwissRound` when exposed | Do not apply Ch/G “frozen partial” rule blindly |

### Frozen vs hidden production block

When `presentation === 'frozen'` and `materializedCount < determinedCount`:

- **Running** recette (`RECETTE R1`) may show **gauge + exploitation copy** without materialize primary (see strip test).
- **Suspended** must follow the same **no primary materialize** rule; gauge visibility follows `shouldShowProductionInMatchsSection` (typically **shows** when dematerialize enabled or policy keeps consultative ratio — if implementation differs, **fail the gate** and fix under Δ-O3, not by weakening assertions).

## Manual UI checklist (per seed row)

1. Open Structure → select competition → phase fiche.
2. Aside **Matchs**: confirm Resume (lifecycle header) per `stage.lifecycle`.
3. Confirm **no** enabled button labelled Materialize / production primary.
4. If dematerialize exposed: control present and opens confirm dialog; cancel safe.
5. Optional: DevTools → attempt materialize API → expect refusal when `commandEnabled` false.

## Gate criteria (close CTRL-O3-MAT)

- [x] All four **S1–S4** integration tests green — verified 2026-10-15 (`dotnet test --filter CtrlO3MatRecetteIntegrationTests`).
- [x] SPA unit tests (**38** tests, 3 files) green — verified 2026-10-15.
- [x] Manual spot-check **S2** — comp `Running`, phase `Suspended` (`264e8653-…`) : **Reprendre** visible ; pas de bouton Matérialiser ; read `commandEnabled: false`, `disabledReason: StructureNotMutable`.
- [x] Manual spot-check **S4** — Swiss suspendu (`a95b3550-…`) : **Reprendre** visible ; pas de CTA batch materialize ; copy Swiss (« Générer la prochaine ronde suisse ») ≠ jauge Ch/G.
- [ ] Re-run spot-checks when Matchs strip / materialize copy changes (per release).

**Gate status:** **FERMÉ** (2026-10-15) pour l’environnement local (seed `ctrl-o3-mat-recette` + Host 5287 + Vite 5173). Réouverture = échec d’un critère ci-dessus.

Proceed with **Δ-O3-1/2** only after this gate stays green in your target environment.

## Δ-drawer-1 — drawer « À traiter » (same seed)

**Status:** **Closed** (2026-10-16) — commit `440eabb6` on `feature/new_version_alt`. Presentation only (no policy / collector / bell count changes).

| Scenario | Drawer checks |
|----------|----------------|
| **S6** | Group **Bloquant** unchanged; row title pause (`SlotFeedMissing`); neutral lede when only presentable rows are structure on **Suspended** |
| **S7** | Group **À traiter**; `PopulationShortfall` paused copy; neutral lede if A+B+C |
| **S2** | Paused row copy; neutral lede when single presentable structure row on suspended Groups |
| **R1** (`c0408365-…` from `structure-materialize-recette`) | Action lede; short titles (no pause) |
| **Draft** (`74598c06-…` from `cup-composition-partial`) | Action lede; short titles |

**Automated:** `attentionDrawerPresentation.test.ts`, `functionalProblemPresentation.test.ts` (shell Vitest).

**Notion:** [Matrice UI Structure — § Δ-drawer-1](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4).

## Related code

- `StructureMaterializeAssembler` — stage must be **Ready** to materialize (Ch/G/Cup read path)
- `StageMaterializeGuard` — command enforcement
- `structureMatchsSectionUi.ts` — `shouldShowProductionInMatchsSection`
- `structureMaterializeCopy.ts` — `classifyMaterializePresentation`, `isMaterializeStructureMutable`
- `StructurePhaseOperationalStrip.tsx` — `showMaterializePrimary`
