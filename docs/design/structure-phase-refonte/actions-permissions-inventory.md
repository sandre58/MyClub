# Structure phase fiche — actions & permissions inventory (Phase 0)

Baseline for Phase 3 **parity checklist** (same labels, disabled states, tooltips after refonte). Authority: `stage.actions` from `StructureViewAssembler.BuildStageActions` plus lifecycle/materialize read models.

Competition-level: `AddCompetitionStage` on `StructurePage` (`data.actions`).

---

## Hub action codes (stage.actions)

Emitted only when competition `Draft` and stage `Draft` (otherwise typically `RenameStage` only). Constants in `StructureViewAssembler.cs`.

| Action | Fiche usage (current) | UI surface |
|--------|----------------------|------------|
| `RenameStage` | `canRename` | Header inline rename |
| `ReplaceMatchRules` | `canEditMatch` | Regulation strip → matchs editor |
| `BindToCompetition` | `canRebind` | Regulation rebind when not specialized |
| `ReplaceQualificationRules` | `canEditQualif`, exit menus | Sorties create/edit, inbound `ExitKindMenu` |
| `ReplaceProgressionRules` | `canEditProg`, exit menus | Sorties create/edit |
| `ReplacePlacementAwardRules` | `canEditPlacement`, `canCreateAttribution` | Attribution rail |
| `ReplaceStandingRules` | `canEditStanding` | Regulation strip |
| `ReplaceDrawRules` | `canEditDraw`, activate/deactivate draw | Placement strip, draw dialog |
| `ReplaceAffectationAuthoring` | `canCompose` | Équipes composition dialog |
| `ReplaceDefaultTieFormat` / `ReplaceRoundTieFormat` | `canEditTie` | Regulation confrontation |
| `RebuildStructure` | `canRebuild` | Header forme (skeleton) dialog |
| `AssignEntryToSlot` | `canAssignEntryToSlot` (Cup only) | Manual placement + schematic click |
| `RemoveStage` | `canRemove` | Header delete confirm |
| `AddMatchday` / `AddGroup` / `AddRound` / `AddSlot` / … | Skeleton / construction dialogs | Not on band-3 tiles directly |
| `ClearSlotAssignment` | Manual placement clear | Manual placement dialog |

Lifecycle transitions are **not** in `stage.actions`; they come from `stage.lifecycle.transition` (enabled/disabled + codes).

---

## Derived permission predicates (client)

| Predicate | Expression | Affects |
|-----------|------------|---------|
| `canManualPlace` | Cup && `AssignEntryToSlot` && schematic success && editable manual places | Placement primary/secondary, schematic click |
| `canCreateExit` | `stages.length >= 2` && (Qual or Prog action) | Sorties “add” |
| `canCreateAttribution` | `ReplacePlacementAwardRules` | Attribution “add” |
| `showDrawCta` | `stage.hasDrawRules` | Draw workflow cluster |
| `showActivateDrawCta` | !hasDrawRules && `ReplaceDrawRules` && (Cup or Groups) | Activate draw |
| Peer exit edit | Peer must have matching Qual/Prog action | `renderAvalSourceAction` |

---

## User-visible actions by refonte zone (preserve)

### Header

| Action | Permission | Notes |
|--------|------------|-------|
| Rename phase | `RenameStage` | |
| Edit forme (skeleton) | `RebuildStructure` | |
| Remove phase | `RemoveStage` | |
| Lifecycle (Prepare / Start / Suspend / …) | `lifecycle.transition` | `StructurePhaseMatchsLifecycleBlock` |

### Équipes

| Action | Permission |
|--------|------------|
| Open composition dialog | `ReplaceAffectationAuthoring` |

### Qualifiés (inbound groups)

| Action | Permission |
|--------|------------|
| Edit exits on peer | Peer `ReplaceQualificationRules` / `ReplaceProgressionRules` via `ExitKindMenu` |
| Open peer phase | Navigation only |

### Affectation

| Action | Permission |
|--------|------------|
| Manual placement | `canManualPlace` |
| Open draw workflow | `showDrawCta` (read); create gated by `resolveStageDrawCreateGate` |
| Activate draw rules | `showActivateDrawCta` |
| Draw settings / deactivate | `canEditDraw` |
| Release cancelled placements | `showReleaseCta` (derived from draws + schematic) |

### Matchs

| Action | Permission |
|--------|------------|
| Materialize | `materialize.commandEnabled` |
| Dematerialize | `dematerialize.enabled` |
| View matches link | `showViewMaterializedMatchesLink` helper |

### Flux sorties / attribution

| Action | Permission |
|--------|------------|
| Add / edit sorties | `canCreateExit`, `ExitKindMenu` |
| Add / edit attribution | `canCreateAttribution`, `canEditPlacement` |

### Règlement strip

| Action | Permission |
|--------|------------|
| Edit match / standing / tie panels | `canEdit*` or rebind via `BindToCompetition` |
| Link to regulation page | Always (read) |

---

## Deep-link opened actions

| Query | Opens |
|-------|--------|
| `compose=1` | Composition dialog |
| `manual=1` | Manual placement dialog |
| `draw=1` | Draw workflow dialog |
| `focus=operational-placement` | Scroll to placement (`id` on strip) |
| `focus=rail-*` | Scroll to rail ids |
| `section=tirage` etc. | Section editor routing via `initialEdit` |

---

## Phase 3 parity table (template)

Recette checklist and automated test map: [phase-3-recette.md](./phase-3-recette.md).

| Action | Before (strip/triptych) | After (band 3) | Match? |
|--------|-------------------------|----------------|--------|
| *fill during Phase 3 recette* | | | |

Any mismatch → classify per refonte plan: conforming / pre-existing defect / indeterminate.
