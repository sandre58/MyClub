# Structure invalidation — CTA safety audit (PR1)

After structure mutations, the hub may show stale data until `GET …/structure` completes. CTAs must not allow invalid actions during that window, or scopes must refresh the fields that gate those actions quickly enough.

## Scope summary

| Mutation | Refresh scope | Stale risk |
| --- | --- | --- |
| Activate / params draw | `hubStageShell` | Activate CTA may flash until hub refetch; perform-draw uses stage overview (included). |
| Deactivate / release draw | `hubStageShell` | Deactivate disabled when draws exist (overview refetch). |
| Rename | `hub` | Name label stale briefly; no dangerous CTA. |
| Draw workflow create (draft) | `stageOverview` | Structure hub / schematic / attention unchanged. |
| Draw workflow publish/apply/cancel/release | `hubStageShell` | Dialog stays open; hub + schematic when placements change. |
| Manual placement | `hubStage` | Schematic + hub occupation refresh. |
| Lifecycle transitions | `hubStageShell` | `status` / `actions` stale until hub refetch; buttons use last transition flags (mutation pending blocks double-submit). |
| Materialize / dematerialize | `hubStageShell` + match lists | Materialize command flags stale until hub refetch; mutation pending blocks repeat. |
| Graph (add/remove/rebuild) | `graph` + optimistic `setQueryData` on structure | Topology from response body. |
| Qual / Prog / composition / placement award | `graph` | Graph-wide; dialog closes immediately. |
| Regulation editor | `competitionReads` | Global regulation reads. |
| Match / standing / tie (stage dialogs) | `hubStage` | Stage regulation slice on hub. |

## Fields gating dangerous actions

- **`hasDrawRules` / `showActivateDrawCta`**: hub structure query (scheduled on draw rule mutations).
- **`stage.actions`**: hub structure query.
- **`stage.status` / lifecycle transitions**: hub structure query; `transitionMutation.isPending` prevents concurrent transitions.
- **`materialize.commandEnabled`**: hub structure query; materialize/dematerialize `isPending` prevents double materialize.

## Manual verification

See checklist in `structure-performance-verification.md`.
