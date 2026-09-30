// -----------------------------------------------------------------------
// Structure phase fiche — rails, domain tiles, confrontation summary UI.
// Feed math: structurePhaseFeeds.ts · Orchestration: StructurePhaseFiche.tsx
// -----------------------------------------------------------------------

import { useMemo, useRef, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Chip } from '../design-system/components/Chip';
import { Popover } from '../design-system/components/Popover';
import { Tooltip } from '../design-system/components/Tooltip';
import {
  AggregateIcon,
  ArrowRightIcon,
  EmptySelectionIcon,
  LegsStatIcon,
  PenaltiesIcon,
  PencilIcon,
  PlusIcon,
  StructureIssueIcon,
  TimerIcon,
} from '../design-system/icons/contentIcons';
import { PinIcon } from '../design-system/icons/metaIcons';
import { TeamCrest } from '../design-system/TeamCrest';
import { EmptyState } from '../ui';
import type {
  StructureConfrontationSegment,
  StructureEntry,
  StructureStageHubSummary,
} from '../types';
import type { FeedGroup } from './structurePhaseFeeds';
import { resolvePlacesN } from './structurePlaces';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

export function FluxRuleRow({
  badge,
  badgeTone,
  context,
  extra,
  badgeAsText = false,
}: {
  badge: string;
  badgeTone: 'win' | 'loss' | 'neutral' | 'accent';
  context: string;
  extra?: string;
  badgeAsText?: boolean;
}) {
  return (
    <span className="structure-flux-rule">
      <span className="structure-flux-rule__source">
        {badgeAsText ? (
          <span className="structure-flux-rule__who">{badge}</span>
        ) : (
          <Chip tone={badgeTone}>{badge}</Chip>
        )}
        {context ? (
          <span className="structure-flux-rule__context">{context}</span>
        ) : null}
      </span>
      {extra ? (
        <>
          <span className="structure-flux-rule__arrow" aria-hidden="true">
            →
          </span>
          <Chip tone="neutral">{extra}</Chip>
        </>
      ) : null}
    </span>
  );
}

export function PlacementAwardRow({
  rank,
  medal,
  badge,
  badgeTone,
  context,
}: {
  rank: number;
  medal: 'gold' | 'silver' | 'bronze' | null;
  badge: string;
  badgeTone: 'win' | 'loss';
  context: string;
}) {
  return (
    <span className="structure-flux-rule">
      <span
        className={[
          'structure-place-rank',
          medal ? `structure-place-rank--${medal}` : null,
        ]
          .filter(Boolean)
          .join(' ')}
      >
        {rank}
      </span>
      <Chip tone={badgeTone}>{badge}</Chip>
      <span className="structure-flux-rule__context">{context}</span>
    </span>
  );
}

export function FluxGroupList({
  groups,
  onOpenPeer,
  teamsLabel,
  renderGroupAction,
}: {
  groups: FeedGroup[];
  onOpenPeer?: (peerId: string) => void;
  teamsLabel: (count: number) => string;
  /** Compact control in the group head (e.g. edit exits on source). */
  renderGroupAction?: (group: FeedGroup) => ReactNode;
}) {
  return (
    <ul className="structure-flux-groups">
      {groups.map((group) => (
        <li key={group.peerId} className="structure-flux-group">
          <div className="structure-flux-group__head">
            {onOpenPeer ? (
              <button
                type="button"
                className="structure-flux-group__peer"
                onClick={() => onOpenPeer(group.peerId)}
              >
                {group.peerName}
              </button>
            ) : (
              <span className="structure-flux-group__peer">
                {group.peerName}
              </span>
            )}
            <div className="structure-flux-group__head-trail">
              <Chip tone="neutral">{teamsLabel(group.volume)}</Chip>
              {renderGroupAction?.(group)}
            </div>
          </div>
          <ul className="structure-flux-group__rules">
            {group.rules.map((rule) => (
              <li key={rule.key} className="structure-flux-group__rule">
                <FluxRuleRow
                  badge={rule.badge}
                  badgeTone={rule.badgeTone}
                  context={rule.context}
                  extra={rule.extra}
                  badgeAsText={rule.badgeAsText}
                />
              </li>
            ))}
          </ul>
        </li>
      ))}
    </ul>
  );
}

export function RootEntriesRail({
  stage,
  entries,
  sources,
  sourcesConfiguredVolume = 0,
  canCompose,
  onEditCompose,
}: {
  stage: StructureStageHubSummary;
  entries: StructureEntry[];
  /** B2 — inbound Qualif/Prog rules (same rail as Affectation teams). */
  sources?: ReactNode;
  /** Expected volume from inbound rules (promised Entries, not yet necessarily in CompositionEntries). */
  sourcesConfiguredVolume?: number;
  canCompose?: boolean;
  onEditCompose?: () => void;
}) {
  const { t } = useTranslation('structure');
  const fromAffectation = stage.affectationEntryCount ?? 0;
  const fromFeeds = sourcesConfiguredVolume;
  const n = resolvePlacesN(stage);
  const ineligible = stage.affectationIneligibleCount ?? 0;
  const composedIds = stage.affectationEntryIds ?? [];
  const previewNames = stage.affectationPreviewNames ?? [];
  // U1 — Population gauge always covers authoring alimentations (never runtime membership).
  const meterEntries = fromAffectation + fromFeeds;
  const byId = useMemo(
    () => new Map(entries.map((entry) => [entry.entryId, entry])),
    [entries],
  );
  const composed = useMemo(
    () =>
      composedIds
        .map((id) => byId.get(id))
        .filter((entry): entry is StructureEntry => entry != null)
        .sort((a, b) =>
          a.displayName.localeCompare(b.displayName, undefined, {
            sensitivity: 'base',
          }),
        ),
    [composedIds, byId],
  );

  let state: 'E0' | 'E1' | 'E2' | 'E3' | 'E4';
  if (n == null) {
    state = 'E4';
  } else if (meterEntries === 0) {
    state = 'E0';
  } else if (meterEntries > n) {
    state = 'E3';
  } else if (meterEntries < n) {
    state = 'E1';
  } else {
    state = 'E2';
  }

  const danger = state === 'E0' || state === 'E1';
  const warn = state === 'E3';
  const soft = state === 'E4';

  const hasTeams =
    composed.length > 0 || previewNames.length > 0 || ineligible > 0;
  // Hide empty Affectation when not editable (e.g. Coupe 16es: feeds only, no authoring).
  const showAffectationSection = hasTeams || Boolean(canCompose);

  const affectationActionLabel = hasTeams
    ? t('entries.editComposition')
    : t('entries.addAffectation');
  const affectationEdit =
    canCompose && onEditCompose ? (
      <Tooltip content={affectationActionLabel}>
        <button
          type="button"
          className={compactIcon}
          aria-label={affectationActionLabel}
          onClick={onEditCompose}
        >
          {hasTeams ? <PencilIcon size="sm" /> : <PlusIcon size="sm" />}
        </button>
      </Tooltip>
    ) : null;

  return (
    <div
      className={[
        'structure-entries',
        danger ? 'structure-entries--danger' : null,
        warn ? 'structure-entries--warn' : null,
        soft ? 'structure-entries--soft' : null,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <div className="structure-entries__main">
        {sources ? (
          <section
            className="structure-entries__section"
            aria-labelledby="population-sources-title"
          >
            <header className="structure-entries__section-head">
              <h4
                id="population-sources-title"
                className="structure-entries__section-title"
              >
                {t('population.sectionSources')}
              </h4>
            </header>
            <div className="structure-entries__sources">{sources}</div>
          </section>
        ) : null}
        {showAffectationSection ? (
          <section
            className="structure-entries__section"
            aria-labelledby="population-affectation-title"
          >
            <header className="structure-entries__section-head">
              <h4
                id="population-affectation-title"
                className="structure-entries__section-title"
              >
                {t('population.sectionAffectation')}
              </h4>
              {affectationEdit}
            </header>
            {composed.length > 0 ? (
              <ul className="structure-entries__teams">
                {composed.map((entry) => (
                  <li
                    key={entry.entryId}
                    className={[
                      'structure-entries__team',
                      entry.status !== 'Active'
                        ? 'structure-entries__team--ineligible'
                        : null,
                    ]
                      .filter(Boolean)
                      .join(' ')}
                  >
                    <TeamCrest
                      name={entry.displayName}
                      logoMediaId={entry.logoMediaId}
                      primaryColor={entry.primaryColor}
                      size="sm"
                    />
                    <span className="structure-entries__team-name">
                      {entry.displayName}
                    </span>
                    {entry.status !== 'Active' ? (
                      <span className="structure-entries__team-warn">
                        {t('composition.noLongerEligible')}
                      </span>
                    ) : null}
                  </li>
                ))}
              </ul>
            ) : previewNames.length > 0 ? (
              <p className="structure-entries__preview" role="status">
                {previewNames.join(' · ')}
              </p>
            ) : ineligible > 0 ? (
              <p className="structure-entries__anomaly" role="status">
                {t('entries.ineligibleOverlay', { count: ineligible })}
              </p>
            ) : null}
          </section>
        ) : !sources ? (
          <EmptyState
            variant="idle"
            icon={<EmptySelectionIcon size="lg" />}
            title={t('entries.emptyTitle')}
          >
            {t('entries.emptyBody')}
          </EmptyState>
        ) : null}
      </div>
      <div className="structure-entries__footer">
        <CompositionMeter
          entries={meterEntries}
          places={n}
          assigned={fromAffectation}
          expected={fromFeeds}
          t={t}
        />
      </div>
    </div>
  );
}

/** Cardinality gauge — expected Population vs Places N; shortfall = danger. */
export function CompositionMeter({
  entries,
  places,
  assigned,
  expected,
  t,
}: {
  entries: number;
  places: number | null;
  /** Affectation count (Draft breakdown). */
  assigned?: number;
  /** Inbound rule volume (Draft breakdown). */
  expected?: number;
  t: (key: string, opts?: Record<string, unknown>) => string;
}) {
  const gap = places != null && places > 0 ? entries - places : null;
  const ratio =
    places != null && places > 0
      ? Math.min(1, Math.max(0, entries / places))
      : entries > 0
        ? 1
        : 0;
  const tone = gap == null || gap === 0 ? 'ok' : gap < 0 ? 'short' : 'over';

  return (
    <div
      className={`structure-assembly structure-assembly--${tone}`}
      aria-label={t('entries.meterAria')}
    >
      <div className="structure-assembly__row">
        <span>{t('entries.meter.population')}</span>
        <strong>{entries}</strong>
      </div>
      <div className="structure-assembly__track" aria-hidden="true">
        <span
          className="structure-assembly__fill"
          style={{ width: `${ratio * 100}%` }}
        />
      </div>
      <div className="structure-assembly__row">
        <span>{t('entries.meter.places')}</span>
        <strong>{places ?? '—'}</strong>
      </div>
      {gap != null && gap !== 0 ? (
        <p className="structure-assembly__gap" role="status">
          <span className="structure-assembly__gap-icon" aria-hidden="true">
            <StructureIssueIcon size="sm" />
          </span>
          {gap < 0
            ? t('entries.meter.shortfall', { count: Math.abs(gap) })
            : t('entries.meter.surplus', { count: gap })}
        </p>
      ) : null}
    </div>
  );
}

export type ExitKind = 'qualification' | 'progression';

/** Compact pencil → single action, or Qualif | Prog menu when both apply. */
export function ExitKindMenu({
  label,
  options,
}: {
  label: string;
  options: { kind: ExitKind; label: string; onSelect: () => void }[];
}) {
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);
  if (options.length === 0) return null;

  if (options.length === 1) {
    const only = options[0]!;
    return (
      <Tooltip content={label}>
        <button
          type="button"
          className={compactIcon}
          aria-label={label}
          onClick={only.onSelect}
        >
          <PencilIcon size="sm" />
        </button>
      </Tooltip>
    );
  }

  return (
    <>
      <Tooltip content={label}>
        <button
          ref={anchorRef}
          type="button"
          className={compactIcon}
          aria-label={label}
          aria-haspopup="menu"
          aria-expanded={open}
          onClick={() => setOpen((value) => !value)}
        >
          <PencilIcon size="sm" />
        </button>
      </Tooltip>
      <Popover
        open={open}
        onOpenChange={setOpen}
        anchorRef={anchorRef}
        role="menu"
        align="end"
        width={220}
        aria-label={label}
      >
        <ul className="structure-overflow-menu">
          {options.map((option) => (
            <li key={option.kind} role="none">
              <button
                type="button"
                role="menuitem"
                className="structure-overflow-menu__item"
                onClick={() => {
                  setOpen(false);
                  option.onSelect();
                }}
              >
                {option.label}
              </button>
            </li>
          ))}
        </ul>
      </Popover>
    </>
  );
}

export function exitKindsPresent(group: FeedGroup): Set<ExitKind> {
  const kinds = new Set<ExitKind>();
  for (const rule of group.rules) {
    kinds.add(rule.family === 'place' ? 'qualification' : 'progression');
  }
  return kinds;
}

export function DomainTile({
  id,
  title,
  icon,
  children,
  onEdit,
  editLabel,
  badge,
  footer,
  span = 'half',
  compact = false,
  attention = false,
}: {
  id: string;
  title: string;
  icon: ReactNode;
  children: ReactNode;
  onEdit?: () => void;
  editLabel: string;
  badge?: ReactNode;
  footer?: ReactNode;
  span?: 'half' | 'full';
  compact?: boolean;
  attention?: boolean;
}) {
  const titleId = `${id}-title`;
  return (
    <section
      id={id}
      className={[
        'ds-form-section',
        'structure-domain-tile',
        span === 'full' ? 'structure-domain-tile--full' : null,
        compact ? 'structure-domain-tile--compact' : null,
        attention ? 'structure-domain-tile--attention' : null,
      ]
        .filter(Boolean)
        .join(' ')}
      aria-labelledby={titleId}
    >
      <header className="ds-form-section__head structure-domain-tile__head">
        <span className="ds-form-section__icon" aria-hidden="true">
          {icon}
        </span>
        <div className="ds-form-section__copy">
          <h3 id={titleId} className="ds-form-section__title">
            {title}
          </h3>
        </div>
        {badge}
        {onEdit ? (
          <div className="ds-icon-toolbar structure-domain-tile__toolbar">
            <Tooltip content={editLabel}>
              <button
                type="button"
                className={compactIcon}
                aria-label={editLabel}
                onClick={onEdit}
              >
                <PencilIcon size="sm" />
              </button>
            </Tooltip>
          </div>
        ) : null}
      </header>
      <div className="ds-form-section__body structure-domain-tile__content">
        {children}
      </div>
      {footer ? (
        <div className="structure-domain-tile__footer">{footer}</div>
      ) : null}
    </section>
  );
}

export function FluxRail({
  id,
  title,
  icon,
  editControl,
  children,
  side,
  empty = false,
}: {
  id: string;
  title: string;
  icon: ReactNode;
  editControl?: ReactNode;
  children?: ReactNode;
  side: 'in' | 'out';
  /** When true: header only — body omitted so it consumes no vertical space. */
  empty?: boolean;
}) {
  const titleId = `${id}-title`;
  return (
    <aside
      id={id}
      className={[
        'structure-phase-rail',
        `structure-phase-rail--${side}`,
        empty ? 'structure-phase-rail--empty' : null,
      ]
        .filter(Boolean)
        .join(' ')}
      aria-labelledby={titleId}
    >
      <header className="structure-phase-rail__head">
        <span className="structure-phase-rail__icon" aria-hidden="true">
          {icon}
        </span>
        <h3 id={titleId} className="structure-phase-rail__title">
          {title}
        </h3>
        {editControl}
      </header>
      {empty ? null : (
        <div className="structure-phase-rail__body">{children}</div>
      )}
    </aside>
  );
}

export function ConfrontationPanel({
  stage,
}: {
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('regulation');
  const segments = [...(stage.confrontationSegments ?? [])].sort((a, b) => {
    const ao = a.rounds[0]?.sortOrder ?? 0;
    const bo = b.rounds[0]?.sortOrder ?? 0;
    return ao - bo;
  });

  const renderItems = (seg: StructureConfrontationSegment, prefix: string) => {
    const twoLegs = seg.numberOfLegs > 1;
    const items: { key: string; label: string; icon: ReactNode }[] = [
      {
        key: `${prefix}-legs`,
        label: twoLegs ? t('tokens.tieTwoLegs') : t('tokens.tieOneLeg'),
        icon: twoLegs ? (
          <LegsStatIcon size="sm" />
        ) : (
          <ArrowRightIcon size="sm" />
        ),
      },
    ];
    if (seg.aggregateScoring) {
      items.push({
        key: `${prefix}-agg`,
        label: t('tokens.tieAggregate'),
        icon: <AggregateIcon size="sm" />,
      });
    }
    if (seg.hasAwayGoalsRule) {
      items.push({
        key: `${prefix}-away`,
        label: t('tokens.tieAwayGoals'),
        icon: <PinIcon size="sm" />,
      });
    }
    if (seg.hasTieExtraTime) {
      items.push({
        key: `${prefix}-et`,
        label: t('tokens.tieExtraTime'),
        icon: <TimerIcon size="sm" />,
      });
    }
    if (seg.hasTiePenaltyShootout) {
      items.push({
        key: `${prefix}-tab`,
        label: t('tokens.tiePenalties'),
        icon: <PenaltiesIcon size="sm" />,
      });
    }
    return items;
  };

  if (segments.length === 0) {
    const fallback: StructureConfrontationSegment = {
      rounds: [],
      numberOfLegs: stage.numberOfLegs ?? 1,
      aggregateScoring: stage.aggregateScoring ?? false,
      hasAwayGoalsRule: stage.hasAwayGoalsRule ?? false,
      hasTieExtraTime: stage.hasTieExtraTime ?? false,
      hasTiePenaltyShootout: stage.hasTiePenaltyShootout ?? false,
    };
    const items = renderItems(fallback, 'default');
    return (
      <ul className="regulation-rule-list structure-confrontation-list">
        {items.map((item) => (
          <li key={item.key} className="regulation-rule-list__item">
            <span className="regulation-rule-list__mark" aria-hidden="true">
              {item.icon}
            </span>
            <span className="regulation-rule-list__label">{item.label}</span>
          </li>
        ))}
      </ul>
    );
  }

  return (
    <div className="structure-confrontation-segments">
      {segments.map((seg, index) => {
        const title =
          seg.rounds
            .map((r) => r.name)
            .filter(Boolean)
            .join(' / ') || `§${index + 1}`;
        const items = renderItems(seg, `seg-${index}`);
        return (
          <div key={title + index} className="structure-confrontation-segment">
            {segments.length > 1 ? (
              <p className="structure-confrontation-segment__title">{title}</p>
            ) : null}
            <ul className="regulation-rule-list structure-confrontation-list">
              {items.map((item) => (
                <li key={item.key} className="regulation-rule-list__item">
                  <span
                    className="regulation-rule-list__mark"
                    aria-hidden="true"
                  >
                    {item.icon}
                  </span>
                  <span className="regulation-rule-list__label">
                    {item.label}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        );
      })}
    </div>
  );
}
