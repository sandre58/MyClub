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
| `CompetitionStatus` | workspace / overview / organisation / consultation `status` |
| `CompletionMode` | completion mode on workspace / overview / consultation |
| `EntryStatus` | entry rows |
| `StageStatus` | stage / organisation format |
| `MatchStatus` | match list / detail / consultation results |
| `ResultType` | match detail / finish request / consultation |
| `DrawResolutionKind` | draw summaries / stage overview |
| `DrawStatus` | draw summaries / stage overview |
| `DrawResolutionState` | draw summaries / stage overview |
| `StructureFormatKind` | organisation / consultation format |

## Named response contracts (Phase 12.8)

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

- `code` — machine-readable string
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
