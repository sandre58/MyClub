/**
 * Journée / round status — calendrier sportif (Lab SoT).
 */
export function MatchRoundStatus({
  state,
  labels,
}: {
  state: 'done' | 'current' | 'upcoming' | 'partial'
  labels: {
    current: string
    partial: string
    done: string
    upcoming: string
  }
}) {
  switch (state) {
    case 'current':
      return (
        <span className="ds-status-live">
          <span className="ds-live-dot" />
          {labels.current}
        </span>
      )
    case 'partial':
      return (
        <span className="ds-status ds-status--context ds-status--rounded ds-status--soft ds-status--tone-attention">
          {labels.partial}
        </span>
      )
    case 'done':
      return (
        <span className="ds-status ds-status--context ds-status--rounded ds-status--soft ds-status--tone-neutral">
          {labels.done}
        </span>
      )
    default:
      return (
        <span className="ds-status ds-status--dense ds-status--tone-neutral">
          {labels.upcoming}
        </span>
      )
  }
}
