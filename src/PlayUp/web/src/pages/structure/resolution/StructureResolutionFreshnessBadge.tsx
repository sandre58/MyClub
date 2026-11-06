import {
  useId,
  useRef,
  useState,
  type KeyboardEvent,
  type MouseEvent,
  type ReactNode,
} from 'react';
import { useInRouterContext, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { Popover } from '../../../design-system/components/Popover';
import { TextLink } from '../../../design-system/components/TextLink';
import { Tooltip } from '../../../design-system/components/Tooltip';
import { Status } from '../../../design-system/components/Status';
import {
  HourglassIcon,
  InfoIcon,
  RefreshIcon,
} from '../../../design-system/icons/contentIcons';
import { ChevronRightIcon } from '../../../design-system/icons/shellIcons';
import type { StructureResolutionOccupant } from '../../../types';
import { freshnessPanel, type FreshnessPanel } from './freshnessGuidance';
import {
  isReplayableStale,
  resolutionOccupantNeedsBadge,
} from './structureResolutionOccupants';

type FreshnessBadgeVariant = 'full' | 'icon';

type StructureResolutionFreshnessBadgeProps = {
  occupant: StructureResolutionOccupant | null | undefined;
  density?: 'context' | 'compact';
  /**
   * `icon` hides the state word (schematic slots). The word moves into the tooltip,
   * or into the guidance panel when the badge opens one.
   * Lists and details keep the default `full` pill.
   */
  variant?: FreshnessBadgeVariant;
  /** Opens the cascade preview. Ignored unless the occupant is stale Qual/Prog. */
  onReplace?: (occupant: StructureResolutionOccupant) => void;
};

function badgeTone(
  state: StructureResolutionOccupant['state'],
): 'attention' | 'error' | 'neutral' {
  switch (state) {
    case 'Perime':
      return 'attention';
    case 'Indetermine':
      return 'attention';
    case 'NonVerifiee':
      return 'neutral';
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
      return <InfoIcon size="sm" className={className} aria-hidden="true" />;
  }
}

export function StructureResolutionFreshnessBadge(
  props: StructureResolutionFreshnessBadgeProps,
) {
  const inRouter = useInRouterContext();
  if (!inRouter) {
    return <FreshnessBadgeBody {...props} competitionId={null} />;
  }
  return <FreshnessBadgeRouted {...props} />;
}

function FreshnessBadgeRouted(props: StructureResolutionFreshnessBadgeProps) {
  const { competitionId } = useParams();
  return (
    <FreshnessBadgeBody {...props} competitionId={competitionId ?? null} />
  );
}

function FreshnessBadgeBody({
  occupant,
  density = 'compact',
  variant = 'full',
  onReplace,
  competitionId,
}: StructureResolutionFreshnessBadgeProps & {
  competitionId: string | null;
}) {
  const { t } = useTranslation('structure');
  const panel =
    occupant && resolutionOccupantNeedsBadge(occupant)
      ? freshnessPanel(occupant, competitionId)
      : null;

  if (!resolutionOccupantNeedsBadge(occupant)) {
    return null;
  }

  const labelKey = `resolution.freshness.state.${occupant.state}` as const;
  const label = t(labelKey);
  const causeKey =
    occupant.state === 'Perime' && occupant.stalenessCause
      ? (`resolution.freshness.cause.${occupant.stalenessCause}` as const)
      : null;
  const causeLabel = causeKey ? t(causeKey) : null;
  const ariaLabel =
    causeLabel != null
      ? t('resolution.freshness.ariaWithCause', { label, cause: causeLabel })
      : label;
  const replaceable = !!onReplace && isReplayableStale(occupant);
  const replaceHint = t('resolution.cascade.replaceHint');
  const replaceDetail =
    causeLabel != null
      ? t('resolution.cascade.replaceHintWithCause', {
          cause: causeLabel,
          action: replaceHint,
        })
      : replaceHint;
  const replaceName =
    variant === 'icon' ? `${label}. ${replaceDetail}` : replaceDetail;
  const iconOnly = variant === 'icon';

  const badge = (
    <Status
      density={density}
      tone={badgeTone(occupant.state)}
      variant="soft"
      shape="pill"
      className={iconOnly ? 'structure-freshness-badge--icon' : undefined}
      aria-label={replaceable || panel ? undefined : ariaLabel}
    >
      {replaceable ? (
        <RefreshIcon
          size="sm"
          className="structure-freshness-badge__icon"
          aria-hidden="true"
        />
      ) : iconOnly ? (
        stateIcon(occupant.state)
      ) : null}
      {iconOnly ? null : label}
      {replaceable ? (
        <ChevronRightIcon
          size="sm"
          className="structure-freshness-badge__chevron"
          aria-hidden="true"
        />
      ) : null}
    </Status>
  );

  if (panel) {
    return (
      <FreshnessGuidancePanel
        occupant={occupant}
        panel={panel}
        badge={badge}
        ariaLabel={ariaLabel}
        stateLabel={label}
        showStateLabel={iconOnly}
        replaceable={replaceable}
        replaceLabel={t('resolution.cascade.action')}
        onReplace={onReplace}
      />
    );
  }

  const tip = (
    <span className="structure-freshness-replace__tip">
      {iconOnly ? (
        <span className="structure-freshness-replace__state">{label}</span>
      ) : null}
      {causeLabel ? (
        <span className="structure-freshness-replace__cause">{causeLabel}</span>
      ) : null}
      {replaceable ? (
        <span className="structure-freshness-replace__action">
          {replaceHint}
        </span>
      ) : null}
    </span>
  );
  const hasTip = iconOnly || causeLabel != null || replaceable;

  if (replaceable) {
    const open = (event: MouseEvent | KeyboardEvent) => {
      event.stopPropagation();
      event.preventDefault();
      onReplace(occupant);
    };
    return (
      <Tooltip content={tip}>
        <button
          type="button"
          className="structure-freshness-replace"
          aria-label={replaceName}
          onClick={open}
        >
          {badge}
        </button>
      </Tooltip>
    );
  }

  if (!hasTip || (causeLabel == null && !iconOnly)) {
    return badge;
  }

  return <Tooltip content={tip}>{badge}</Tooltip>;
}

function FreshnessGuidancePanel({
  occupant,
  panel,
  badge,
  ariaLabel,
  stateLabel,
  showStateLabel,
  replaceable,
  replaceLabel,
  onReplace,
}: {
  occupant: StructureResolutionOccupant;
  panel: FreshnessPanel;
  badge: ReactNode;
  ariaLabel: string;
  stateLabel: string;
  showStateLabel: boolean;
  replaceable: boolean;
  replaceLabel: string;
  onReplace?: (occupant: StructureResolutionOccupant) => void;
}) {
  const { t } = useTranslation('structure');
  const anchorRef = useRef<HTMLButtonElement>(null);
  const [open, setOpen] = useState(false);
  const panelId = useId();
  const cause =
    panel.kind === 'upstream'
      ? t('resolution.freshness.cause.AmontPerime')
      : t(`resolution.freshness.indeterminacy.${panel.cause}.cause`);
  const action =
    panel.kind === 'upstream'
      ? t('resolution.freshness.upstreamLink')
      : panel.cause === 'MatchNonJoue' && panel.matchLabel
        ? t('resolution.freshness.indeterminacy.MatchNonJoue.actionNamed', {
            match: panel.matchLabel,
          })
        : t(`resolution.freshness.indeterminacy.${panel.cause}.action`);
  const name =
    panel.kind === 'upstream'
      ? `${ariaLabel}. ${action}`
      : t('resolution.freshness.ariaWithCause', {
          label: ariaLabel,
          cause: `${cause}. ${action}`,
        });

  const toggle = (event: MouseEvent | KeyboardEvent) => {
    event.stopPropagation();
    event.preventDefault();
    setOpen((current) => !current);
  };

  return (
    <>
      <button
        ref={anchorRef}
        type="button"
        className="structure-freshness-replace"
        aria-label={name}
        aria-expanded={open}
        aria-haspopup="dialog"
        aria-controls={panelId}
        onClick={toggle}
      >
        {badge}
      </button>
      <Popover
        id={panelId}
        open={open}
        onOpenChange={setOpen}
        anchorRef={anchorRef}
        aria-label={name}
        width={240}
      >
        {showStateLabel ? (
          <span className="structure-freshness-replace__state">
            {stateLabel}
          </span>
        ) : null}
        <span className="structure-freshness-panel__cause">{cause}</span>
        {panel.href ? (
          <TextLink to={panel.href}>{action}</TextLink>
        ) : (
          <span>{action}</span>
        )}
        {replaceable && onReplace ? (
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            onClick={(event) => {
              event.stopPropagation();
              setOpen(false);
              onReplace(occupant);
            }}
          >
            {replaceLabel}
          </button>
        ) : null}
      </Popover>
    </>
  );
}
