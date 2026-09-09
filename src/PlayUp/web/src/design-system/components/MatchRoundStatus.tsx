import { LiveStatus } from './LiveStatus';
import { Status } from './Status';

/**
 * Journée / round status — calendrier sportif (Lab SoT).
 * Renders via Status / LiveStatus; state mapping stays calendrier-specific.
 */
export function MatchRoundStatus({
  state,
  labels,
}: {
  state: 'done' | 'current' | 'upcoming' | 'partial';
  labels: {
    current: string;
    partial: string;
    done: string;
    upcoming: string;
  };
}) {
  switch (state) {
    case 'current':
      return <LiveStatus>{labels.current}</LiveStatus>;
    case 'partial':
      return (
        <Status
          density="context"
          tone="attention"
          variant="soft"
          shape="rounded"
        >
          {labels.partial}
        </Status>
      );
    case 'done':
      return (
        <Status density="context" tone="neutral" variant="soft" shape="rounded">
          {labels.done}
        </Status>
      );
    default:
      return (
        <Status density="dense" tone="neutral">
          {labels.upcoming}
        </Status>
      );
  }
}
