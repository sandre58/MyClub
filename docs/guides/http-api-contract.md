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
| `StructureFormatKind` | organisation / consultation format |

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
    "upcomingMatches": []
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
  "naturalProgression": { "code": "ContinueOrganisation" },
  "closureHint": { "canCompleteNormally": false, "blockerCodes": [] },
  "navigationHints": [
    { "targetType": "Fixture", "targetId": "<guid>", "matchId": "<guid>", "stageId": "<guid>", "competitionId": "<guid>" }
  ]
}
```

Contract notes:

- `status` is the Domain lifecycle status — distinct from `situations` / attention.
- `cycleReading.code`: `Construction` | `InProgress` | `Completed` | `Archived` (Suspended → `InProgress` + informational situation `CompetitionSuspended`).
- No `label` / `reason` / `summary` / cycle `note` fields — SPA i18n owns copy (`source` + `params` → reason templates; `impactCode` → impact copy).
- Situation identity = `source` + `targetType` + `targetId` (stable; not translated text).
- `actionable` is Host-projected (`true` iff `actionCode` is set). SPA must not infer actionability from `source`.
- `impactCode` is optional (`BlocksConstruction` | `BlocksDraw` | `BlocksProgression` in V1). Omit when not reliably derivable.
- Nature V1: `Blocking` | `Informational` only. Richer natures (e.g. Opportunity) remain OPEN — opportunities live in `availableActions` / `naturalProgression`.
- `draws[].isApplied` is Application-derived (Publish ≠ Apply). Domain has no Applied status.
- `attentionSummary` is a **derived subset** of `situations` where `nature === "Blocking"` — not a second independent list / ranking.
- Organisation construction blockers (`InsufficientParticipants`, `MissingStage`, …) become Cockpit situations only while competition is Draft/Ready.
- `constructionDimensions.regulation` (Phase 16.4): factual Competition summary (`competition`) + optional Stage regulation flags (`stage`) + `competitionRegulationMutable` + `transitionReadiness[]`. **No** global `isValid` / `isSatisfactory`.
- `transitionReadiness[].transition`: `Draw` | `MaterializeMatches` in V1 (construction only; empty when Running/Suspended/Completed/Archived). Championship omits `Draw` (format never uses draw path).
- `transitionReadiness` reuses Organisation readiness (`ReadyForDraw` / `ReadyForMaterialization`) and the same blocker codes as Organisation / Situations — not a parallel validation system.
- Absence of optional Stage families (`hasDrawRules: false`, …) is a **fact**, not an automatic invalidity claim.
- Competition Prepare/Start are Host-exposed (`POST …/prepare`, `POST …/start`) but **not** projected yet as Cockpit `availableActions` (Read projection = Phase 17.2). Resume (Suspended) remains Domain-only — not projected as an action.
- `closureHint` (CompletionAnalyzer) is **distinct** from attention / situations — completion blockers ≠ À traiter.
- `availableActions` are opportunities from known state — not execution guarantees. Competition Prepare/Start action codes are **not** projected yet (Host exists; Read projection OPEN until 17.2). Resume (Suspended) is Domain-only — not projected as an action.
- `naturalProgression` replaces the workspace `nextAction*` stub for Cockpit consumption (code only).
- Fixture → Match: `navigationHints` with `targetType: "Fixture"` include resolved `matchId` when an attachment exists; progression situations may also carry `matchId`.

DTO source: `MyClub.PlayUp.Application.Reads.CockpitViewDto`.

### Organizer copy on Read endpoints

All organizer-facing copy is owned by the SPA i18n layer. Read DTOs expose **codes + structured facts** only:

- Workspace: `nextActionCode` (no `nextActionLabel`)
- Organisation: `format.kind`, `readiness.blockers` (no `format.label`, no `hints`)
- Needs Attention: `source` / `severity` / targets (no `reason`)
- Completion: reason `code` only (no `message`)
- Cockpit: codes + facts only (already)

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

```json
{
  "createdCount": 6,
  "attachedMatchIds": ["<guid>", "..."],
  "alreadyComplete": false
}
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

Including (non-exhaustive; see `ApplicationErrorCodes`): `DanglingFeedTarget`, `StageNotInCompetition`, `SlotFeedsInvalid`, `FixtureInvalid`, `TieFormatRequired`, qualification standing codes, `DrawApplyFailure`, `DrawKindNotSupported`, `DrawGenerationFailure`, `ScheduleGenerationFailure`, `ScheduleApplyFailure`, `EntryCapacityExceeded`, `InvalidStructureIntent`, `CupBracketNotPowerOfTwo`, `OrganisationNotMutable`, `MaterializationFailure`, `InvalidCompletionMode`.

## Frontend boundary

The client should depend only on:

- HTTP routes + named JSON DTOs
- Guid strings + enum strings
- ProblemDetails `code` / `reasons`

It must not depend on Domain entities, EF, repositories, `UseCaseExecutor`, or Application assemblers.
