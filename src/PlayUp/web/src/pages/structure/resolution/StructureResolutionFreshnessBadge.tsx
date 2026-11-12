import type { ReactNode } from 'react';
import { Link, useInRouterContext, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { Tooltip } from '../../../design-system/components/Tooltip';
import { Status } from '../../../design-system/components/Status';
import {
  HourglassIcon,
  RefreshIcon,
} from '../../../design-system/icons/contentIcons';
import { ChevronRightIcon } from '../../../design-system/icons/shellIcons';
import type { StructureResolutionOccupant } from '../../../types';
import {
  freshnessAction,
  indeterminacyExplanation,
  type FreshnessAction,
} from './freshnessGuidance';
import {
  isDefinitivelyUnconfirmed,
  resolutionOccupantNeedsBadge,
} from './structureResolutionOccupants';

type FreshnessBadgeVariant = 'full' | 'icon';

type StructureResolutionFreshnessBadgeProps = {
  occupant: StructureResolutionOccupant | null | undefined;
  density?: 'context' | 'compact';
  /**
   * `icon` hides the state word (schematic slots). The word moves into the tooltip.
   * Lists and details keep the default `full` pill.
   */
  variant?: FreshnessBadgeVariant;
  /** Opens the cascade preview. Ignored unless the occupant is replayable. */
  onReplace?: (occupant: StructureResolutionOccupant) => void;
  /** Opens the release preview. Ignored unless the occupant is releasable. */
  onRelease?: (occupant: StructureResolutionOccupant) => void;
  /** Stored team, used to name who is no longer confirmed. */
  entryName?: string | null;
};

function badgeTone(
  state: StructureResolutionOccupant['state'],
): 'attention' | 'error' | 'neutral' {
  switch (state) {
    case 'Perime':
    case 'Indetermine':
      return 'attention';
    default:
      return 'neutral';
  }
}

function stateIcon(state: StructureResolutionOccupant['state']): ReactNode {
  const className = 'structure-freshness-badge__icon';
  switch (state) {
    case 'Perime':
      return <RefreshIcon size="sm" className={className} aria-hidden="true" />;
    case 'Indetermine':
      return (
        <HourglassIcon size="sm" className={className} aria-hidden="true" />
      );
    default:
      return null;
  }
}

export function StructureResolutionFreshnessBadge(
  props: StructureResolutionFreshnessBadgeProps,
) {
  const inRouter = useInRouterContext();
  if (!inRouter) {
    return (
      <FreshnessBadgeBody {...props} competitionId={null} linked={false} />
    );
  }
  return <FreshnessBadgeInRouter {...props} />;
}

function FreshnessBadgeInRouter(props: StructureResolutionFreshnessBadgeProps) {
  const { competitionId } = useParams();
  return (
    <FreshnessBadgeBody
      {...props}
      competitionId={competitionId ?? null}
      linked
    />
  );
}

function FreshnessBadgeBody({
  occupant,
  density = 'compact',
  variant = 'full',
  onReplace,
  onRelease,
  entryName,
  competitionId,
  linked,
}: StructureResolutionFreshnessBadgeProps & {
  competitionId: string | null;
  /** False outside a router: navigation stays a tooltip, with no link. */
  linked: boolean;
}) {
  const { t } = useTranslation('structure');

  if (!resolutionOccupantNeedsBadge(occupant)) {
    return null;
  }

  const action = freshnessAction(occupant, competitionId, {
    canReplace: !!onReplace,
    canRelease: !!onRelease,
  });
  const control =
    action?.kind === 'replace'
      ? 'replace'
      : action?.kind === 'release'
        ? 'release'
        : action?.kind === 'navigate' && linked
          ? 'navigate'
          : null;
  const labelKey = `resolution.freshness.state.${occupant.state}` as const;
  const label = t(labelKey);
  const causeKey =
    occupant.state === 'Perime' && occupant.stalenessCause
      ? (`resolution.freshness.cause.${occupant.stalenessCause}` as const)
      : occupant.state === 'Indetermine' && occupant.indeterminacyCause
        ? (`resolution.freshness.indeterminacy.${occupant.indeterminacyCause}.cause` as const)
        : null;
  const causeLabel = causeKey ? t(causeKey) : null;
  const explanation = indeterminacyExplanation(occupant, entryName);
  const sentence = explanation
    ? t('resolution.freshness.unconfirmedSentence', {
        team: explanation.team,
        detail: t(explanation.detailKey, explanation.detailParams),
      })
    : null;
  const explained = sentence ?? causeLabel;
  const definitiveLabel = isDefinitivelyUnconfirmed(occupant)
    ? t('resolution.freshness.definitive')
    : null;
  const causeText = [explained, definitiveLabel].filter(Boolean).join(' — ');
  const actionLabel = actionText(t, action);

  const ariaLabel =
    control && actionLabel
      ? causeText
        ? t('resolution.freshness.ariaAction', {
            label,
            cause: causeText,
            action: actionLabel,
          })
        : t('resolution.freshness.ariaActionNoCause', {
            label,
            action: actionLabel,
          })
      : causeText
        ? t('resolution.freshness.ariaWithCause', {
            label,
            cause: causeText,
          })
        : label;

  const iconOnly = variant === 'icon';
  const showStateIcon =
    iconOnly ||
    occupant.state === 'Indetermine' ||
    (control === 'replace' && occupant.state === 'Perime');

  const badge = (
    <Status
      density={density}
      tone={badgeTone(occupant.state)}
      variant="soft"
      shape="pill"
      className={iconOnly ? 'structure-freshness-badge--icon' : undefined}
      aria-label={control ? undefined : ariaLabel}
    >
      {showStateIcon ? stateIcon(occupant.state) : null}
      {iconOnly ? null : label}
      {control ? (
        <ChevronRightIcon
          size="sm"
          className="structure-freshness-badge__chevron"
          aria-hidden="true"
        />
      ) : null}
    </Status>
  );

  const tip = (
    <span className="structure-freshness-replace__tip">
      {iconOnly ? (
        <span className="structure-freshness-replace__state">{label}</span>
      ) : null}
      {explained ? (
        <span className="structure-freshness-replace__cause">{explained}</span>
      ) : null}
      {definitiveLabel ? (
        <span className="structure-freshness-replace__cause">
          {definitiveLabel}
        </span>
      ) : null}
      {actionLabel ? (
        <span className="structure-freshness-replace__action">
          {actionLabel}
        </span>
      ) : null}
    </span>
  );
  const hasTip =
    iconOnly ||
    explained != null ||
    definitiveLabel != null ||
    actionLabel != null;
  const wrapped = hasTip ? <Tooltip content={tip}>{badge}</Tooltip> : badge;
  const showSubtitle = !iconOnly && definitiveLabel != null;

  if (control === 'replace' || control === 'release') {
    return present(
      <Tooltip content={tip}>
        <button
          type="button"
          className="structure-freshness-replace"
          aria-label={ariaLabel}
          onClick={(event) => {
            event.stopPropagation();
            if (control === 'release') {
              onRelease?.(occupant);
              return;
            }
            onReplace?.(occupant);
          }}
        >
          {badge}
        </button>
      </Tooltip>,
      showSubtitle,
      definitiveLabel,
    );
  }

  if (control === 'navigate' && action?.kind === 'navigate') {
    return present(
      <Tooltip content={tip}>
        <Link
          to={action.href}
          className="structure-freshness-replace"
          aria-label={ariaLabel}
          onClick={(event) => {
            event.stopPropagation();
          }}
        >
          {badge}
        </Link>
      </Tooltip>,
      showSubtitle,
      definitiveLabel,
    );
  }

  return present(wrapped, showSubtitle, definitiveLabel);
}

function present(
  node: ReactNode,
  showSubtitle: boolean,
  definitiveLabel: string | null,
) {
  if (!showSubtitle || definitiveLabel == null) {
    return node;
  }
  return (
    <span className="structure-freshness-definitive">
      {node}
      <span className="structure-freshness-definitive__label">
        {definitiveLabel}
      </span>
    </span>
  );
}

function actionText(
  t: (key: string, params?: Record<string, string>) => string,
  action: FreshnessAction | null,
): string | null {
  if (action?.kind === 'replace') {
    return t('resolution.cascade.replaceHint');
  }
  if (action?.kind === 'release') {
    return t('resolution.release.action');
  }
  if (action?.kind === 'navigate') {
    return t(action.labelKey, action.labelParams);
  }
  return null;
}
