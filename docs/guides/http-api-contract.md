# Play'up Host HTTP contract (Phase 12.8)

Stable conventions for the Play'up Minimal API consumed by the Phase 13 frontend.

## Serialization

| Kind | JSON representation |
| :--- | :--- |
| `Guid` | string |
| Enums | **string** (enum member name), e.g. `"Running"` — not numeric |
| Property names | camelCase (ASP.NET Core defaults) |

Host configures `JsonStringEnumConverter` via `ConfigureHttpJsonOptions`. Domain enum numeric values are unchanged and unused on the wire.

### Enums exposed on HTTP responses / request bodies

| Enum | Typical fields |
| :--- | :--- |
| `CompetitionStatus` | workspace / overview / organisation / consultation / cockpit `status` |
| `CompletionMode` | completion mode on workspace / overview / consultation / cockpit |
| `EntryStatus` | entry rows |
| `StageStatus` | stage / organisation format / cockpit operational focus |
| `MatchStatus` | match list / detail / consultation results |
| `ResultType` | match detail / finish request / consultation |
| `DrawResolutionKind` | draw summaries / stage overview / cockpit draws |
| `DrawStatus` | draw summaries / stage overview / cockpit draws |
| `DrawResolutionState` | draw summaries / stage overview / cockpit draws |
| `StructureFormatKind` | organisation / consultation format (`Championship` \| `Groups` \| `Cup` \| `Swiss`) |
| `MatchGenerationFormat` | organisation `structure.matchGenerationFormat`; `ConfigureStructureRequest.matchGenerationFormat` (`SingleRoundRobin` \| `DoubleRoundRobin`) |
| `ProgressionOutcome` | progression-rules paths (`Winner` \| `Loser`) |

## Named response contracts (Phase 12.8)

### `GET /competitions/{competitionId}/cockpit` → `CockpitViewDto` (Phase 16.1)

Aggregated Cockpit Read projection (Application interpretation). Does **not** replace workspace / organisation / attention endpoints.

**String strategy:** organizer-facing copy is **not** on the wire. The API exposes **codes + structured facts** only; the SPA maps codes to i18n labels. Exception diagnostics remain English on ProblemDetails.

```json
{
  "competitionId": "<guid>",
  "name": "…",
  "status": "Draft",
  "completionMode": null,
  "cycleReading": { "code": "Construction" },
  "preparationFocus": "Setup",
  "calendarSummary": null,
  "constructionDimensions": {
    "teams": { "prominence": "Present", "facts": { "activeCount": "1", "minimumTeams": "2" } },
    "structure": { "prominence": "Present", "facts": { "formatKind": "None" } },
    "regulation": {
      "prominence": "Present",
      "competition": {
        "minimumTeams": 2,
        "maximumTeams": 64,
        "durationPerPeriod": 45,
        "numberOfPeriods": 2,
        "winPoints": 3,
        "drawPoints": 1,
        "lossPoints": 0
      },
      "stage": {
        "stageId": "<guid>",
        "stageName": "…",
        "hasDrawRules": false,
        "numberOfPots": null,
        "hasQualificationRules": false,
        "qualificationPathCount": 0,
        "hasProgressionRules": false,
        "progressionPathCount": 0,
        "hasTieFormat": false
      },
      "competitionRegulationMutable": true,
      "transitionReadiness": [
        {
          "transition": "Draw",
          "ready": false,
          "blockerCodes": ["InsufficientParticipants", "MissingStage"]
        },
        {
          "transition": "MaterializeMatches",
          "ready": false,
          "blockerCodes": ["InsufficientParticipants", "MissingStage"]
        }
      ]
    },
    "matches": { "prominence": "Absent", "facts": { "total": "0" } }
  },
  "operationalFocus": {
    "stages": [{ "stageId": "<guid>", "name": "…", "status": "Draft" }],
    "draws": [{
      "stageId": "<guid>",
      "drawId": "<guid>",
      "kind": "Pairing",
      "status": "Draft",
      "resolutionState": "Resolved",
      "isApplied": false
    }],
    "matchCounts": {
      "live": 0, "scheduled": 0, "finished": 0, "postponed": 0, "cancelled": 0, "total": 0
    },
    "swissByes": [],
    "recentUnit": null,
    "nextUnit": null,
    "standingCompact": null,
    "referenceStageGameRules": null
  },
  "situations": [{
    "source": "InsufficientParticipants",
    "nature": "Blocking",
    "targetType": "Organisation",
    "targetId": "<guid>",
    "matchId": null,
    "actionable": true,
    "actionCode": "AddEntry",
    "impactCode": "BlocksConstruction",
    "params": { "minimumTeams": "2", "activeCount": "0" }
  }],
  "attentionSummary": { "count": 1, "items": ["…same situation objects…"] },
  "availableActions": [{
    "code": "AddEntry",
    "guaranteed": false,
    "stageId": null,
    "drawId": null,
    "matchId": null,
    "fixtureId": null,
    "params": null
  }],
  "naturalProgression": null,
  "closureHint": { "canCompleteNormally": false, "blockerCodes": [] },
  "navigationHints": [
    { "targetType": "Fixture", "targetId": "<guid>", "matchId": "<guid>", "stageId": "<guid>", "competitionId": "<guid>" }
  ]
}
```

Contract notes:

- `status` is the Domain lifecycle status — distinct from `situations` / attention.
- `cycleReading.code`: `Construction` | `InProgress` | `Completed` | `Archived` (Suspended → `InProgress` + informational situation `CompetitionSuspended`).
- `preparationFocus`: Host-owned Préparation sub-situation — **not** a cycle code. Wire values: `Setup` | `GeneratedCalendar`.
  - V1 `GeneratedCalendar` iff **all**: `cycleReading = Construction` · format = `Championship` · `status = Ready` · `matchCounts.total > 0`.
  - Otherwise `Setup` (includes Draft+matches, Ready without matches, Swiss/Cup/Groups, Running+).
  - SPA composes Préparation variants from this field only — must **not** infer from `Ready && total > 0`.
  - Header/Shell pill stays **Préparation** (Domain status / cycle UI) — no « Calendrier » cycle label.
- `calendarSummary`: overview calendar synthesis when `preparationFocus = GeneratedCalendar`; otherwise `null`.
  - Shape: `{ matchdayCount, matchCount, matchdays: [{ matchdayNumber, matchCount }], nextMatch? }` where `nextMatch` is `{ matchId, stageId, matchdayNumber?, scheduledAt?, homeDisplayName, awayDisplayName }` (first upcoming `Scheduled`, placements preferred).
  - Matchdays come from the **primary** Championship stage (may still be Draft/Ready) — not ReferenceStage (Running/Completed).
  - `matchdays` preview capped (Host `CalendarPreviewMatchdayLimit` = 3). Not a Match hub dump.
- No `label` / `reason` / `summary` / cycle `note` fields — SPA i18n owns copy (`source` + `params` → reason templates; `impactCode` → impact copy).
- Situation identity = `source` + `targetType` + `targetId` (stable; not translated text).
- `actionable` is Host-projected (`true` iff `actionCode` is set). SPA must not infer actionability from `source`.
- `impactCode` is optional (`BlocksConstruction` | `BlocksDraw` | `BlocksProgression` in V1). Omit when not reliably derivable.
- Nature V1: `Blocking` | `Informational` only. Richer natures (e.g. Opportunity) remain OPEN — opportunities live in `availableActions` / `naturalProgression`.
- `draws[].isApplied` is Application-derived (Publish ≠ Apply). Domain has no Applied status.
- `attentionSummary` is a **derived subset** of `situations` where `nature === "Blocking"` — not a second independent list / ranking.
- Organisation construction blockers (`InsufficientParticipants`, `MissingStage`, …) become Cockpit situations only while competition is Draft/Ready.
- `constructionDimensions.regulation` (Phase 16.4): factual Competition summary (`competition`) + optional Stage regulation flags (`stage`) + `competitionRegulationMutable` + `transitionReadiness[]`. **No** global `isValid` / `isSatisfactory`.
- `transitionReadiness[].transition`: `Draw` | `MaterializeMatches` | `MaterializeFromOccupiedSlots` | `GenerateNextRound`. `Draw` / `MaterializeMatches` reuse Organisation readiness (construction). `MaterializeFromOccupiedSlots` is a **distinct** Cup opportunity (occupied slots not yet covered by complete SlotA/B fixtures) and may appear while competition is Draft/Ready/**Running** when a target Cup stage is still Draft/Ready. **Championship** and **Swiss** omit `Draw`. **Swiss** omits `MaterializeMatches` and projects `GenerateNextRound` instead (ready when stage Running, previous round Finished, rounds remaining).
- `transitionReadiness` for Draw / MaterializeMatches reuses Organisation readiness (`ReadyForDraw` / `ReadyForMaterialization`) and the same blocker codes as Organisation / Situations — not a parallel validation system. **Cup `ReadyForMaterialization`** means the primary stage still needs its empty Fixture **skeleton** (fixture count below `slotCount / 2`) — it is **not** the from-slots path and is **not** equal to `ReadyForDraw` after the skeleton exists. From-slots uses its own opportunity check; the Cockpit projects that transition **only when ready** (no standing `InsufficientOccupiedSlots` regulation gap during early Cup construction). Swiss `GenerateNextRound` blockers: `SwissStageNotRunning` | `SwissAwaitingRoundResults` | `SwissRoundsComplete` | `SwissInsufficientParticipants`.
- Structure facts may include `swissRoundCount` / `swissByeCount` when Kind is Swiss. `operationalFocus.swissByes[]` lists recorded bye pairing events (`roundIndex`, `entryId`, `entryDisplayName`) — **not** fixtures/matches.
- `operationalFocus.recentUnit` / `nextUnit`: temporal sport units on **ReferenceStage** only (Championship/Groups/Swiss → Matchday; Cup → Round). Full unit, **no** silent truncation.
  - `recentUnit` = highest-order unit with ≥1 `Live` or `Finished` match (includes that unit’s `Scheduled` matches). Null → SPA empty state « Dernières » (card still shown in En cours).
  - `nextUnit` = first unit with order strictly greater than `recentUnit` that has matches; before kickoff (`recentUnit` null) = first unit with matches. Null → SPA empty state « Prochaines ».
  - Shape: `{ stageId, stageName, unitKind, unitKey, matchdayNumber?, roundName?, matchCount, matches[] }` where each match is `{ matchId, stageId, status, scheduledAt?, homeDisplayName, awayDisplayName, score? }` (`score` only when Finished; Domain has no in-progress score/minute).
  - No separate `liveMatches` panel — Live is a status inside `recentUnit` (SPA badge).
  - Distinct from `preparationFocus` / `calendarSummary` (Préparation calendar overview — not En cours temporal units).
- `operationalFocus.referenceStageGameRules`: machine facts for En cours **Règlement** from **ReferenceStage** only (`null` when none). SPA selects 2–3 explanatory facts by `formatKind` — does not dump all fields.
  - Includes standing points, match duration, extra-time / shootout presence, effective tie legs / aggregate, optional `swissPlannedRounds`.
  - Distinct from `constructionDimensions.regulation` (construction readiness + competition summary / primary-stage flags).
- `operationalFocus.standingCompact`: compact standing for pilotage derived from **ReferenceStage** only:
  - ReferenceStage = first `StageIds[i]` with `Stage.Status` in (`Running`, `Suspended`), else last `StageIds[i]` with `Completed`, else none → `standingCompact` null.
  - For Vue d’ensemble display, **Suspended = Running**. Multiple active candidates → first wins (no error in V1).
  - Shape: `{ stageId, stageName, tables[] }` where each table is `{ scope, groupId?, groupName?, rows[] }` (Pos · displayName · played · points — **all** rows, no silent top-N truncation).
  - Championship / Swiss → one `Overall` table. Groups → **all** group tables for that stage (SPA may show one at a time).
  - Cup / no standing for ReferenceStage → null.
  - Projected only when competition is Running / Suspended / Completed / Archived.
  - Same `CalculateStanding` path as Consultation (`ProjectStandingsForStage`) — not a second ranking algorithm.
- Natural progression prefers **from-slots** over skeleton `MaterializeMatches` when both apply (multi-stage).
- `naturalProgression` (Draft / Ready): **0 or 1** structural tip — from-slots when applicable, else scan projected actions by priority `PrepareStage` → `StartStage` → `MaterializeMatches` → `PublishDraw` → `ApplyDraw`. **`null` is a valid calm Construction state** (no `ContinueOrganisation` fallback; SPA hides the card unless a lifecycle Prepare/Start competition action is available alone). Distinct from `availableActions` and from À traiter. `AddEntry` is never a tip.
- `naturalProgression` (Running / Suspended): **0 or 1** structural tip from `availableActions` in fixed priority — `MaterializeFromOccupiedSlots` → `GenerateNextRound` → `PublishDraw` → `ApplyDraw` → `ApplyProgression` → `ApplyQualification` → `PrepareStage` → `StartStage` → `CompleteCompetition`. **`null` is a valid calm state** (no `OpenMatches` fallback; SPA hides the card). Distinct from `availableActions` and from À traiter.
- `naturalProgression` (Completed / Archived): **`null`** — no Cockpit tip (consultation is not a “next action”; `OpenConsultation` may still appear on other surfaces such as workspace).
- Absence of optional Stage families (`hasDrawRules: false`, …) is a **fact**, not an automatic invalidity claim.
- Competition Prepare/Start are Host-exposed (`POST …/prepare`, `POST …/start`) and projected as Cockpit `availableActions` (`PrepareCompetition` / `StartCompetition`) when Domain preconditions appear satisfied. They are **not** elevated to `naturalProgression` (intentional lifecycle — L7). SPA may show **one** of them on Prochaine action **only when** `naturalProgression` is null — never stacked with a structural tip. Stage `PrepareStage` / `StartStage` **are** eligible as Préparation tips when projected. Resume (Suspended) remains Domain-only — not projected as an action.
- `closureHint` (CompletionAnalyzer) is **distinct** from attention / situations — completion blockers ≠ À traiter.
- `availableActions` are opportunities from known state — not execution guarantees (R19). Resume (Suspended) is Domain-only — not projected as an action.
- **`MaterializeFromOccupiedSlots` (Cockpit):** projected with `stageId` + params (`stageName`, `occupiedSlotCount`) when a Cup stage has an from-slots opportunity. The Cockpit does **not** choose SlotA/SlotB pairs and does **not** POST materialize-from-slots. SPA intent is **navigate** to `/stages/{stageId}`; the organizer selects pairs explicitly on the Stage surface, then calls `POST …/matches/materialize-from-slots`.
- Stage overview `slots[].coveredByCompleteFixture`: true when that slot key is already on a Fixture with the expected legs attached. Confrontations pairing UI excludes those slots (same coverage rule as Cockpit opportunity).
- `naturalProgression` replaces the workspace `nextAction*` stub for Cockpit consumption (code only). May be `MaterializeFromOccupiedSlots` when that opportunity is the relevant tip. On Running/Suspended may be **null** when no structural tip applies.
- Fixture → Match: `navigationHints` with `targetType: "Fixture"` include resolved `matchId` when an attachment exists; progression situations may also carry `matchId`.

DTO source: `MyClub.PlayUp.Application.Reads.CockpitViewDto`.

### Organizer copy on Read endpoints

All organizer-facing copy is owned by the SPA i18n layer. Read DTOs expose **codes + structured facts** only:

- Workspace: `nextActionCode` (no `nextActionLabel`)
- Organisation: `format.kind`, `structure.matchGenerationFormat`, `readiness.blockers` (no `format.label`, no `hints`)
- Needs Attention: `source` / `severity` / targets (no `reason`)
- Completion: reason `code` only (no `message`)
- Cockpit: codes + facts only (already)

### `POST /competitions/{competitionId}/organisation/structure` → `OrganisationViewDto`

Configures primary stage structure (`ConfigureStructureRequest`).

```json
{
  "format": "Championship",
  "stageName": "League",
  "matchdayCount": 1,
  "groupCount": null,
  "participantsPerGroup": null,
  "bracketSize": null,
  "matchGenerationFormat": "DoubleRoundRobin",
  "swissRoundCount": null
}
```

| Field | Notes |
| :--- | :--- |
| `format` | `Championship` \| `Groups` \| `Cup` \| `Swiss` (case-insensitive) |
| `matchGenerationFormat` | Optional. `SingleRoundRobin` (default) \| `DoubleRoundRobin`. Applies to **Championship / Groups** only. Ignored for Cup / Swiss. |
| `swissRoundCount` | Required when `format` is `Swiss` (≥ 1). Persists `SwissSettings.RoundCount`. Matchdays are created later by `GenerateNextRound`, **not** by `MaterializeMatches`. |

**Materialization principle:** `POST …/matches/materialize` does **not** accept a generation-mode body. `MaterializeMatches` reads `Stage.MatchGenerationFormat` persisted by ConfigureStructure (or Domain defaults). **Swiss** stages reject materialize — use `GenerateNextRound` instead.

Organisation Read already exposes `structure.matchGenerationFormat` and `structure.swissRoundCount` on `OrganisationStructureSummaryDto` (same enum / nullable int on wire).

Host contract: `MyClub.PlayUp.Host.Contracts.ConfigureStructureRequest`.

### `POST /competitions/{competitionId}/prepare` → 204 No Content

Competition lifecycle: Domain `Draft → Ready`. No request body.

| HTTP | When |
| :--- | :--- |
| **204** | Prepared successfully |
| **404** | Competition not found (`Application.CompetitionNotFound`) |
| **409** | Invalid transition / closed competition (`Competition.InvalidTransition` or `Application.CompetitionClosed`) |

### `POST /competitions/{competitionId}/start` → 204 No Content

Competition lifecycle: Domain `Ready → Running`. No request body.

| HTTP | When |
| :--- | :--- |
| **204** | Started successfully |
| **404** | Competition not found (`Application.CompetitionNotFound`) |
| **409** | Invalid transition / closed competition (`Competition.InvalidTransition` or `Application.CompetitionClosed`) |

### Thin authoring (multi-stage Cup)

Additional stages / rounds / slots / progression rules without Domain seeding. Does **not** author Fixtures (first tour remains MaterializeCup + Pairing; later tours use materialize-from-slots).

#### `POST /competitions/{competitionId}/stages` → 201 `AddCompetitionStageResponse`

```json
{ "name": "Semi-Finals" }
```

```json
{ "stageId": "<guid>", "name": "Semi-Finals" }
```

#### `POST /stages/{stageId}/rounds` → 201 `AddStageRoundResponse`

```json
{ "name": "SF", "numberOfLegs": 2, "aggregateScoring": true }
```

`numberOfLegs` / `aggregateScoring` optional. Omit legs to leave the Round without an explicit TieFormat (stage regulation default may still apply at Domain add). **Effective contract:** null `Round.TieFormat` means **OneLeg** for materialize / draw / prepare / progression; **TwoLegs** only when explicit (`numberOfLegs: 2`). `numberOfLegs` must be `1` or `2` when set.

```json
{ "roundId": "<guid>", "name": "SF" }
```

#### `POST /stages/{stageId}/slots` → 201 `AddStageSlotResponse`

```json
{ "slotKey": "SF1-A" }
```

```json
{ "slotKey": "SF1-A" }
```

#### `PUT /stages/{stageId}/progression-rules` → 204 No Content

```json
{
  "paths": [
    {
      "sourceFixtureId": "<guid>",
      "outcome": "Winner",
      "destinationStageId": "<guid>",
      "destinationSlotKey": "SF1-A"
    }
  ]
}
```

Empty or null `paths` clears rules. Domain validates source fixture ownership and local destinations.

### `POST /stages/{stageId}/qualification/apply` → `QualificationApplyResponse`

```json
{
  "appliedCount": 1,
  "assignments": [
    { "stageId": "<guid>", "slotKey": "Champ", "entryId": "<guid>" }
  ]
}
```

### `POST /stages/{stageId}/matches/materialize` → `MaterializeMatchesResponse`

Creates Fixtures/Matches for the stage format (Championship / Groups RR, or Cup empty-fixture skeleton for Pairing). No request body. Reads persisted `Stage.MatchGenerationFormat` for RR (see ConfigureStructure above). **Not applicable to Swiss** (`Application.MaterializationFailure` — use `GenerateNextRound`).

```json
{
  "createdCount": 6,
  "attachedMatchIds": ["<guid>", "..."],
  "alreadyComplete": false
}
```

### `POST /stages/{stageId}/swiss/generate-next-round` → `GenerateNextRoundResponse`

Generates the next Swiss round (pairings + Matchday + Matches, optional bye). Stage must be **Running**. Distinct from `…/matches/materialize` (RR / Cup skeleton).

```json
{
  "roundIndex": 1,
  "createdCount": 4,
  "attachedMatchIds": ["<guid>", "..."],
  "byeEntryId": null,
  "alreadyComplete": false
}
```

| Gate | Allowed |
| :--- | :--- |
| Stage | Swiss (`SwissSettings`) + `Running` |
| Previous round | Fully **Finished** before generating the next |
| Planned rounds | At most `SwissSettings.RoundCount` |
| Competition | Not Suspended / Completed / Archived |

Host contract: `MyClub.PlayUp.Host.Contracts.GenerateNextRoundResponse`.

### `POST /stages/{stageId}/matches/materialize-from-slots` → `MaterializeMatchesResponse`

**Distinct from** `…/matches/materialize`. Materializes Cup confrontations from **explicit** occupied SlotA/SlotB pairs (creates Fixture + Matches; reuses TieFormat legs). Does **not** invent pairs.

```json
{
  "pairs": [
    { "slotAKey": "SF1-A", "slotBKey": "SF1-B" }
  ]
}
```

Response shape is the same `MaterializeMatchesResponse` as materialize.

| Gate | Allowed |
| :--- | :--- |
| Competition | `Draft` \| `Ready` \| `Running` |
| Target stage | `Draft` \| `Ready` only |
| Target stage `Running` | Rejected (`Application.OrganisationNotMutable`) |
| StructureLocked / AttachMatch on Running stage | Unchanged — not unlocked by this endpoint |

Typical product flow (Cockpit is readiness + navigation only):

```text
Cockpit (MaterializeFromOccupiedSlots)
  → navigate to Stage
  → user selects SlotA ↔ SlotB
  → POST /stages/{id}/matches/materialize-from-slots
```

These DTOs live in `MyClub.PlayUp.Host/Contracts`. They expose Guids only — no Domain aggregates.

## Error contract (`ProblemDetails`)

| HTTP | When |
| :--- | :--- |
| **404** | Missing resource — `ApplicationFailureException` with `Application.StageNotFound`, `Application.CompetitionNotFound`, or `Application.MatchNotFound` |
| **409** | Business conflict / closed competition / Domain rule — specific Application codes below, or any `DomainException` (`extensions.code` = Domain code) |
| **400** | Other `ApplicationFailureException` codes (invalid request / preconditions) |
| **500** | Unhandled exception (not mapped by `PlayUpExceptionHandler`) |

ProblemDetails extensions:

- `code` — machine-readable string (SPA maps via `errors` i18n namespace; English `detail` is diagnostic fallback)
- `reasons` — optional string array (e.g. completion blockers)

### Application codes mapped to 409

- `Application.MatchOperationNotAllowed`
- `Application.ConsequenceOperationNotAllowed`
- `Application.SlotOccupancyConflict`
- `Application.CompletionNotAllowed`
- `Application.CompetitionClosed`

### Application codes mapped to 404

- `Application.StageNotFound`
- `Application.CompetitionNotFound`
- `Application.MatchNotFound`

### Other Application codes (400 by default)

Including (non-exhaustive; see `ApplicationErrorCodes`): `DanglingFeedTarget`, `StageNotInCompetition`, `SlotFeedsInvalid`, `FixtureInvalid`, `TieFormatRequired`, qualification standing codes, `DrawApplyFailure`, `DrawKindNotSupported`, `DrawGenerationFailure`, `ScheduleGenerationFailure`, `ScheduleApplyFailure`, `EntryCapacityExceeded`, `InvalidStructureIntent`, `CupBracketNotPowerOfTwo`, `OrganisationNotMutable`, `MaterializationFailure`, `SwissRoundGenerationFailure`, `InvalidCompletionMode`.

Cockpit from-slots readiness is projected only when an opportunity exists (occupied uncovered slots). It does **not** leave a standing regulation blocker `InsufficientOccupiedSlots` when slots are still empty during early Cup construction.

## Frontend boundary

The client should depend only on:

- HTTP routes + named JSON DTOs
- Guid strings + enum strings
- ProblemDetails `code` / `reasons`

It must not depend on Domain entities, EF, repositories, `UseCaseExecutor`, or Application assemblers.
