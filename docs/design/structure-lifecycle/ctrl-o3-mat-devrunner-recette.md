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

Creates **four** competitions (search workspace list for prefix `CTRL-O3-MAT`):

| ID | Competition display name | Stage / competition | Production state |
|----|--------------------------|---------------------|------------------|
| **S1** | `CTRL-O3-MAT S1 — Groups suspendu (comp+phase)` | Groups · stage **Suspended** · competition **Suspended** | 3/6 matches materialized, exploitation mid-run |
| **S2** | `CTRL-O3-MAT S2 — Groups phase suspendue (comp Running)` | Groups · stage **Suspended** · competition **Running** | Same 3/6 — **isolated stage pause** |
| **S3** | `CTRL-O3-MAT S3 — Championnat suspendu` | Qual→Forme champ · stage **Suspended** · competition **Running** | 6/6 materialized, Started then Suspended |
| **S4** | `CTRL-O3-MAT S4 — Swiss suspendu` | Swiss 8×3 · stage **Suspended** · competition **Suspended** | Progressive rounds mid-run (Swiss exception path) |

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

- [ ] All four **S1–S4** integration tests green.
- [ ] SPA unit tests for **Suspended** + `frozen` / `blocked` green.
- [ ] Manual spot-check on **S2** and **S4** once per release touching Matchs strip.
- [ ] Then proceed with **Δ-O3-1/2** without conflating with this gate.

## Related code

- `StructureMaterializeAssembler` — stage must be **Ready** to materialize (Ch/G/Cup read path)
- `StageMaterializeGuard` — command enforcement
- `structureMatchsSectionUi.ts` — `shouldShowProductionInMatchsSection`
- `structureMaterializeCopy.ts` — `classifyMaterializePresentation`, `isMaterializeStructureMutable`
- `StructurePhaseOperationalStrip.tsx` — `showMaterializePrimary`
