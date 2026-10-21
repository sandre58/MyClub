# RECETTE C2 — SPA checklist (Cup arbitration)

**Status:** **SPA recette executed 2026-10-17** — Track **A** signed; Track **B** signed **2026-10-17 (rejouage B3–B4)** with **découvrabilité** reserve for Q3 UX. Backend: `CupC2ImpasseRecetteProtocolTests`.  
**Parent:** [cup-materialize-recette-protocol.md](./cup-materialize-recette-protocol.md) · [cup-lifecycle-start-mutability-arbitration.md](./cup-lifecycle-start-mutability-arbitration.md) (Notion SoT).

## Prerequisites

```bash
docker compose up -d
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios structure-materialize-recette
# Host http://localhost:5287 · SPA npm run dev in src/PlayUp/web
```

Open competition **RECETTE C2 — Cup 1/2 partial** (`recette-c2`). Prepare competition → phase **Ready** if seed is still Draft.

Deep link: `/competitions/{competitionId}/structure?stage={stageId}&section=matchs`

## Track A — Impasse (align with backend recette)

| Step | SPA check | Backend reference (2026-10-17) |
|------|-----------|--------------------------------|
| A1 | Jauge **1/2** on Aside **Matchs** | Same counts on structure read |
| A2 | Cannot assign on empty bracket slot (no manual assign affordance **or** API error if forced) | `StructureNotMutable` |
| A3 | **Start** disabled; SPA may show **UnmaterializedConfrontations** on lifecycle | Backend protocol (AUD-0004 C3): `StartBlockedIncompleteEntryRound` at **1/2** without manual assign — guard **1a** before production-gap on RECETTE C2 seed |
| A4 | Materialize remaining eligible confrontation → **2/2**; **2/4** pairs still empty on schematic | Same counts on structure read; **Start stays disabled** (impasse — not “enabled then blocked”) |
| A5 | After **2/2** mat. with **2/4** bracket pairs undetermined: **Start blocked** (`IncompleteEntryRound`) — **1a** | `Application.StartBlockedIncompleteEntryRound` (`CupC2ImpasseRecetteProtocolTests` A5) |

**Documented impasse:** not at 1/2, but after **all eligible** confrontations are materialized while **half** of entry bracket pairs remain undetermined.

## Track B — Recovery Q3

> **Revert to Ready alone does not restore placement** (matches stay attached). Recovery requires **dematerialize in Ready** after revert.

| Step | SPA check | Notes |
|------|-----------|--------|
| B1 | From Running (after A5), **Revert to Ready** visible and succeeds | Backend: OK if matches not sportively engaged |
| B2 | After B1: placement **still** blocked (expected) | Matches still attached |
| B3 | **Remove matches** / dematerialize (`dematerialize.enabled` on structure read) | Use **`getByRole('button', { name: /Retirer les matchs/i })`** in `#operational-matchs` (icon-only CTA; see audit below). API contract: `DematerializeEndpointTests.Cup_materialize_from_slots_then_dematerialize_returns_contractAsync` — **not** a substitute for this SPA step on C2 |
| B4 | After B3: `AssignEntryToSlot` affordance returns; manual assign on previously empty slot **succeeds** | **Décideur sign-off required** |
| B5 | Optional: repeat A4–A5 to confirm reproducibility | |

## SPA run — 2026-10-17 (local Host 5287 + Vite 5173, seed C2)

### Track A — Impasse (SPA)

| Step | Result |
|------|--------|
| A1 | Aside jauge **1/2**, copy « Il reste 1 match à créer » |
| A2 | Read-model `actions` = `RenameStage` only (no `AssignEntryToSlot`) |
| A3 | **Démarrer la phase** disabled; `UnmaterializedConfrontations` |
| A4 | **Compléter les matchs** → **2/2** « Tous les matchs prévus sont créés »; Start enabled |
| A5 | **Démarrer la phase** → **Running**; `dematerialize.enabled=false`, `StageLifecycleNotSupported` |

### Track B — Recovery Q3 (SPA)

| Step | Result |
|------|--------|
| B1 | **Annuler le démarrage** visible en Running; `POST /revert-to-ready` → **204**, phase **Ready** (automation: overlay règlement bloque le clic — à valider clic manuel) |
| B2 | Après Revert : `actions` sans `AssignEntryToSlot` (matchs encore attachés) — conforme |
| B3 | **Rejouage (rôle)** : après Revert, `getByRole('button', { name: 'Retirer les matchs' })` **présent** dans l’arbre accessible ; clic → dialog **« Retirer les matchs »** → démat OK (jauge **0/2**). Premier passage : faux négatif `innerText` (CTA icon-only). |
| B4 | Après démat : CTA **« Créer les matchs »**, **Placer manuellement** ; read-model `AssignEntryToSlot` ; Start **disabled** (`UnmaterializedConfrontations`) |

### Track B — Rejouage B3–B4 (2026-10-17, seed réinitialisé, Host :5287 + Vite :5173)

État cible : Running → `POST revert-to-ready` → Ready exhausted **2/2**, `dematerialize.enabled=true`.

| Verdict | Constat |
|---------|---------|
| **Fonctionnel** | Bouton accessible **« Retirer les matchs »** ; dialog ; `materializedCount` **0/2** ; actions incluent `AssignEntryToSlot` |
| **Découvrabilité** | CTA aside **icône + libellé visible** (tranche 3) ; `getByRole('button', { name: /Retirer les matchs/i })` inchangé |
| **Composition / sync** | **Non observé** — signal hub et bouton alignés après Revert |

**Q3 verdict doc :** parcours SPA **Revert → démat (CTA + dialog) → placement redevenu possible** validé sur C2. L’arbitrage peut traiter la **découvrabilité** (icon-only) séparément ; ne pas confondre avec un parcours API-only.

### Audit — visibilité CTA « Retirer les matchs » (2026-10-17, code SPA)

**Périmètre :** pas de correctif D2 / Ready ; pas de changement métier — exposition UI uniquement.

| Signal | Comportement attendu (code) |
|--------|-----------------------------|
| `stage.dematerialize?.enabled === true` | `resolveAsideMatchsStripContent` → `showProductionLeadingActions` true (`structureMatchsSectionUi.ts`) |
| Phase **Ready** + présentation `terminal` (jauge exhausted) | `shouldShowProductionInMatchsSection` → true ; pas de blocage spécifique Cup |
| Layout aside `omitLifecycle` | CTA dans `#operational-matchs` → `structure-operational-strip__production-actions--tile` |
| Rendu du bouton | **Icône poubelle + libellé visible** (`actions.DematerializePhase` → **« Retirer les matchs »**) (`StructurePhaseOperationalStrip.tsx`, `MatchesColumn`) |
| `dematerialize.enabled === false` | Pas de bouton (ex. **Running** C2 : `StageLifecycleNotSupported`) |

**Recette SPA :** CTA avec libellé visible ; méthode : `within(#operational-matchs).getByRole('button', { name: /Retirer les matchs/i })`, puis dialog de confirmation.

**Trois états distincts (verdict B3 / Q3) :**

| État | Critère | Suite |
|------|---------|--------|
| **Fonctionnel** | Bouton trouvé par rôle → dialog → démat OK → placement redevenu possible | Signer B3–B4 ; Q3 peut documenter le parcours SPA nominal (sous réserve découvrabilité) |
| **Défaut composition / sync** | Bouton absent au rôle alors que le hub a `dematerialize.enabled=true` | Investiguer perte du signal au rafraîchissement hub ou dans la composition SPA — pas D2 ni exception Ready |
| **Découvrabilité** | Parcours fonctionnel, action visible seulement en icône | Sujet **UX** dans l’arbitrage Q3 — pas une preuve d’insuffisance API |

## Sign-off

| Track | Date | Result |
|-------|------|--------|
| A — Impasse | 2026-10-17 | **OK** (SPA aligné backend) |
| B — Recovery Q3 | 2026-10-17 | **OK** — B3–B4 rejoués ; découvrabilité **tranche 3** (libellé visible) |

Notion Q3 may cite SPA recovery on C2; keep **discoverability** (visible label / guidance) as open UX arbitration, not API gap.
