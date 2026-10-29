import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import type { ReactNode } from 'react';
import { ApiError } from './api';
import {
  apiErrorLabel,
  genericNetworkLabel,
  isGenericNetworkFailure,
} from './i18n/apiErrorLabel';
import { formatStructureMutationMessage } from './pages/structure/problems/formatStructureMutationMessage';
import { Status } from './design-system/components/Status';
import { statusToneFromBadgeTone } from './design-system/components/statusTone';
import { Alert } from './design-system/components/Alert';
import { WaitMark, type WaitSize } from './design-system/components/WaitMark';
import type {
  CompetitionStatus,
  DrawResolutionState,
  DrawStatus,
  EntryStatus,
  MatchStatus,
  StageStatus,
  StructureStageHubSummary,
} from './types';
import {
  competitionStatusLabel,
  drawResolutionStateLabel,
  drawStatusLabel,
  entryStatusLabel,
  matchStatusLabel,
  stageStatusLabel,
} from './i18n/enumLabels';

/**
 * Shared page primitives: status badges and loading / error / empty / pending
 * states. Prefer design-system PageHead / Status / Alert / WaitMark at call
 * sites when the mapping is 1:1; this module keeps product adapters
 * (domain badges, i18n error formatting, unlabeled loading).
 */

/** Visual meaning of a state, shared by every status family. */
export type StatusTone =
  'neutral' | 'info' | 'ok' | 'live' | 'done' | 'warn' | 'danger';

export function BackLink({
  to,
  children,
}: {
  to: string;
  children: ReactNode;
}) {
  return (
    <Link className="ds-back-link" to={to}>
      <span aria-hidden="true">←</span>
      {children}
    </Link>
  );
}

/** Thin Status adapter for domain badges — not for new call sites (use Status). */
function StatusBadge({
  tone,
  children,
  variant = 'soft',
  shape = 'rounded',
  density = 'context',
  ...rest
}: {
  tone: StatusTone;
  children: ReactNode;
  variant?: 'soft' | 'outline';
  shape?: 'rounded' | 'pill';
  density?: 'context' | 'compact';
} & Omit<
  import('react').HTMLAttributes<HTMLSpanElement>,
  'children' | 'title' | 'className'
>) {
  return (
    <Status
      density={density}
      tone={statusToneFromBadgeTone(tone)}
      variant={variant}
      shape={shape}
      {...rest}
    >
      {children}
    </Status>
  );
}

export function CompetitionStatusBadge({
  status,
  density = 'context',
}: {
  status: CompetitionStatus;
  density?: 'context' | 'compact';
}) {
  return (
    <StatusBadge density={density} tone={competitionStatusTone[status]}>
      {competitionStatusLabel(status)}
    </StatusBadge>
  );
}

export function StageStatusBadge({
  status,
  density = 'context',
}: {
  status: StageStatus;
  density?: 'context' | 'compact';
}) {
  return (
    <StatusBadge tone={stageStatusTone[status]} density={density}>
      {stageStatusLabel(status)}
    </StatusBadge>
  );
}

export function MatchStatusBadge({ status }: { status: MatchStatus }) {
  return (
    <StatusBadge tone={matchStatusTone[status]}>
      {matchStatusLabel(status)}
    </StatusBadge>
  );
}

export function EntryStatusBadge({ status }: { status: EntryStatus }) {
  return (
    <StatusBadge tone={entryStatusTone[status]}>
      {entryStatusLabel(status)}
    </StatusBadge>
  );
}

export function DrawStatusBadge({
  status,
  density = 'context',
  variant = 'soft',
}: {
  status: DrawStatus;
  density?: 'context' | 'compact';
  variant?: 'soft' | 'outline';
}) {
  return (
    <StatusBadge
      tone={drawStatusTone[status]}
      density={density}
      variant={variant}
    >
      {drawStatusLabel(status)}
    </StatusBadge>
  );
}

export function DrawResolutionBadge({
  state,
  density = 'context',
}: {
  state: DrawResolutionState;
  density?: 'context' | 'compact';
}) {
  return (
    <StatusBadge tone={drawResolutionTone[state]} density={density}>
      {drawResolutionStateLabel(state)}
    </StatusBadge>
  );
}

/** One colour vocabulary for every status family across the app. */
const competitionStatusTone: Record<CompetitionStatus, StatusTone> = {
  Draft: 'info',
  Ready: 'info',
  Running: 'live',
  Suspended: 'warn',
  Completed: 'done',
  Archived: 'neutral',
};

const stageStatusTone: Record<StageStatus, StatusTone> = {
  Draft: 'neutral',
  Ready: 'info',
  Running: 'live',
  Suspended: 'warn',
  Completed: 'done',
};

const matchStatusTone: Record<MatchStatus, StatusTone> = {
  Scheduled: 'neutral',
  Live: 'live',
  Finished: 'done',
  Postponed: 'warn',
  Cancelled: 'danger',
};

const entryStatusTone: Record<EntryStatus, StatusTone> = {
  Active: 'ok',
  Withdrawn: 'warn',
};

const drawStatusTone: Record<DrawStatus, StatusTone> = {
  Draft: 'neutral',
  Published: 'info',
  Cancelled: 'danger',
};

const drawResolutionTone: Record<DrawResolutionState, StatusTone> = {
  NotResolved: 'neutral',
  Resolved: 'ok',
  NoSolution: 'danger',
};

export function LoadingState({
  label,
  size = 'page',
}: {
  label?: string;
  size?: WaitSize;
}) {
  const { t } = useTranslation('common');
  const text = label ?? t('loading');

  return <WaitMark size={size}>{text}</WaitMark>;
}

export function ErrorState({ error }: { error: unknown }) {
  const { t } = useTranslation('common');
  const notFound = error instanceof ApiError && error.status === 404;

  return (
    <Alert tone="danger" role="alert">
      {notFound ? t('notFound') : formatError(error, t)}
    </Alert>
  );
}

/** Dialog footer status failure of a write — next to the save actions. */
export function MutationError({
  error,
  sotHref,
  structureStages,
}: {
  error: unknown;
  /** When the failure reveals a persistent Structure problem, link to its SoT. */
  sotHref?: string | null;
  /** When set, downstream-consumer failures list blocking phase names. */
  structureStages?: readonly StructureStageHubSummary[];
}) {
  const { t } = useTranslation('common');
  const { t: tStructure } = useTranslation('structure');

  const message =
    structureStages !== undefined
      ? formatStructureMutationMessage(tStructure, error, structureStages)
      : formatError(error, t);

  return (
    <Alert tone="danger" role="alert">
      <span>{message}</span>
      {sotHref ? (
        <>
          {' '}
          <Link className="ds-text-link" to={sotHref}>
            {t('fixOnStructure')}
          </Link>
        </>
      ) : null}
    </Alert>
  );
}

/**
 * Empty lists are product states, not blanks: say what is missing and,
 * when the Host really exposes one, what the organizer can do next.
 * variant="idle" — centered icon + title + body (drawer / region empty).
 * size="compact" — inline, low vertical footprint (nested rails / tiles).
 */
export function EmptyState({
  title,
  hint,
  children,
  action,
  icon,
  iconAccessible = false,
  variant = 'default',
  size = 'default',
  iconSize,
}: {
  title?: string;
  /** Secondary guidance below the title (idle regions). */
  hint?: ReactNode;
  children?: ReactNode;
  action?: ReactNode;
  icon?: ReactNode;
  /** When true, the icon slot is exposed to assistive tech (e.g. primary CTA). */
  iconAccessible?: boolean;
  variant?: 'default' | 'idle';
  /** `compact` for inset sub-regions; `comfortable` for larger idle zones. */
  size?: 'compact' | 'default' | 'comfortable';
  /** Icon scale in the icon slot (`sm` / `md` / `lg`). Defaults to `sm` when `size="compact"`. */
  iconSize?: 'sm' | 'md' | 'lg';
}) {
  const resolvedIconSize = iconSize ?? (size === 'compact' ? 'sm' : undefined);

  const classes = [
    'ds-empty',
    variant === 'idle' ? 'ds-empty--idle' : null,
    size === 'compact' ? 'ds-empty--compact' : null,
    size === 'comfortable' ? 'ds-empty--comfortable' : null,
  ]
    .filter(Boolean)
    .join(' ');

  const iconClassName = [
    'ds-empty__icon',
    resolvedIconSize ? `ds-empty__icon--${resolvedIconSize}` : null,
  ]
    .filter(Boolean)
    .join(' ');

  const copy = (
    <>
      {title ? <p className="ds-empty__title">{title}</p> : null}
      {hint ? <p className="ds-empty__hint">{hint}</p> : null}
      {children ? <p className="ds-empty__body">{children}</p> : null}
      {action ? <div className="ds-empty__actions">{action}</div> : null}
    </>
  );

  return (
    <div className={classes}>
      {icon ? (
        <span
          className={iconClassName}
          aria-hidden={iconAccessible ? undefined : true}
        >
          {icon}
        </span>
      ) : null}
      {size === 'compact' ? <div className="ds-empty__copy">{copy}</div> : copy}
    </div>
  );
}

/** Inline spinner + label for pending mutations and short status lines. */
export function PendingLabel({ children }: { children: ReactNode }) {
  return (
    <span className="ds-pending-label">
      <span className="ds-spinner ds-spinner--sm" aria-hidden="true" />
      <span className="ds-pending-label__text">{children}</span>
    </span>
  );
}

function formatError(
  error: unknown,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  if (isGenericNetworkFailure(error)) {
    return genericNetworkLabel();
  }

  if (error instanceof ApiError) {
    const localized = apiErrorLabel(error);
    return t('errorWithStatus', {
      message: localized ?? error.detail ?? error.message,
      status: error.status,
    });
  }

  if (error instanceof Error) {
    return error.message;
  }

  return t('unknownError');
}
