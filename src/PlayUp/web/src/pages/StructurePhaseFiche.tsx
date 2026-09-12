import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeftRight,
  ArrowRight,
  CircleAlert,
  EllipsisVertical,
  Goal,
  MapPin,
  Sigma,
  Timer,
} from 'lucide-react';
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { fetchStageOverview } from '../api';
import { Chip } from '../design-system/components/Chip';
import { Popover } from '../design-system/components/Popover';
import { Status } from '../design-system/components/Status';
import { Tooltip } from '../design-system/components/Tooltip';
import { TextLink } from '../design-system/components/TextLink';
import { LucideIcon } from '../design-system/icons/Icon';
import {
  ArrowDownIcon,
  ArrowRightIcon,
  AttributionIcon,
  ChampionshipFormatIcon,
  ConfrontationIcon,
  CupFormatIcon,
  DrawConfigIcon,
  DrawConstraintIcon,
  DrawPendingIcon,
  GroupsFormatIcon,
  LayersIcon,
  MatchRulesIcon,
  MatchdayStatIcon,
  PencilIcon,
  RandomIcon,
  RoundsStatIcon,
  SeedsIcon,
  StandingRulesIcon,
  StructureIcon,
  SwissFormatIcon,
} from '../design-system/icons/contentIcons';
import { structureFormatKindLabel } from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import { StageStatusBadge, StatusBadge } from '../ui';
import type {
  SelectionMode,
  StructureConfrontationSegment,
  StructureFormatKind,
  StructurePlacementAward,
  StructureProgressionPath,
  StructureQualificationPath,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import {
  PlacementAwardRulesDialog,
  QualificationRulesDialog,
  ProgressionRulesDialog,
  RemovePhaseDialog,
} from './StructureGraphDialogs';
import {
  DrawRulesDialog,
  MatchRulesDialog,
  RebindDialog,
  StandingRulesDialog,
  TieFormatDialog,
} from './StructureRegulationDialogs';
import {
  isMatchFrameBound,
  isStandingFrameBound,
  relevantPhaseSections,
  type StructureSectionId,
} from './structureHubSections';
import { PhaseSchematic } from './phaseSchematic';
import {
  MatchRulesPanel,
  StandingRulesPanel,
} from './regulationRulePanels';
import './regulation.css';

type EditTarget =
  | 'qualification'
  | 'progression'
  | 'placement'
  | 'tirage'
  | 'confrontation'
  | 'matchs'
  | 'classement'
  | 'rebind-match'
  | 'rebind-standing'
  | null;

type SchematicMode = 'slots' | 'equipes';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

function stageActions(stage: StructureStageHubSummary): string[] {
  return stage.actions ?? [];
}

function FormatGlyph({
  kind,
  size = 'md',
}: {
  kind?: StructureFormatKind | null;
  size?: 'sm' | 'md' | 'lg';
}) {
  switch (kind) {
    case 'Cup':
      return <CupFormatIcon size={size} aria-hidden="true" />;
    case 'Championship':
      return <ChampionshipFormatIcon size={size} aria-hidden="true" />;
    case 'Groups':
      return <GroupsFormatIcon size={size} aria-hidden="true" />;
    case 'Swiss':
      return <SwissFormatIcon size={size} aria-hidden="true" />;
    default:
      return <StructureIcon size={size} aria-hidden="true" />;
  }
}

function selectionVolume(path: StructureQualificationPath): number {
  switch (path.selectionMode as SelectionMode) {
    case 'Position':
      return 1;
    case 'Range': {
      const end = path.selectionEndValue ?? path.selectionValue;
      return Math.max(1, end - path.selectionValue + 1);
    }
    case 'Top':
    case 'Bottom':
    case 'Best':
    case 'Worst':
    default:
      return Math.max(1, path.selectionValue || 1);
  }
}

type FeedFamily = 'place' | 'result';

type FeedRow = {
  key: string;
  peerId: string;
  peerName: string;
  peerOrder: number;
  badge: string;
  badgeTone: 'win' | 'loss' | 'neutral' | 'accent';
  context: string;
  extra?: string;
  volume: number;
  family: FeedFamily;
  /** Place lower bound, or Winner=0 / Loser=1. */
  sortPrimary: number;
  /** Group name (alpha) or match number (numeric as string padded). */
  sortSecondary: string;
};

type FeedGroup = {
  peerId: string;
  peerName: string;
  peerOrder: number;
  volume: number;
  rules: {
    key: string;
    badge: string;
    badgeTone: 'win' | 'loss' | 'neutral' | 'accent';
    context: string;
    extra?: string;
    volume: number;
    family: FeedFamily;
    sortPrimary: number;
    sortSecondary: string;
  }[];
};

function formatPlace(
  value: number,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  return value === 1
    ? t('fiche.rule.placeFirst')
    : t('fiche.rule.placeNth', { value });
}

function selectionModeBadge(
  mode: SelectionMode,
  value: number,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  switch (mode) {
    case 'Top':
      return t('fiche.rule.selectionTop', { value });
    case 'Bottom':
      return t('fiche.rule.selectionBottom', { value });
    case 'Best':
      return t('fiche.rule.selectionBest', { value });
    case 'Worst':
      return t('fiche.rule.selectionWorst', { value });
    default:
      return t('fiche.rule.topLike', { mode, value });
  }
}

function qualificationRuleParts(
  path: StructureQualificationPath,
  t: (key: string, opts?: Record<string, unknown>) => string,
): Pick<
  FeedRow,
  | 'badge'
  | 'badgeTone'
  | 'context'
  | 'extra'
  | 'family'
  | 'sortPrimary'
  | 'sortSecondary'
> {
  const place = formatPlace(path.selectionValue, t);
  const group = path.groupName?.trim() ?? '';
  const mode = path.selectionMode as SelectionMode;
  const sortPrimary = path.selectionValue;
  const sortSecondary = group;
  const extra =
    path.minimumPoints != null
      ? t('fiche.rule.minimumPoints', { n: path.minimumPoints })
      : undefined;

  if (mode === 'Range') {
    const from = formatPlace(path.selectionValue, t);
    const to = formatPlace(
      path.selectionEndValue ?? path.selectionValue,
      t,
    );
    const badge = `${from}–${to}`;
    if (group) {
      return {
        badge,
        badgeTone: 'accent',
        context: t('fiche.rule.contextGroup', { group }),
        extra,
        family: 'place',
        sortPrimary,
        sortSecondary,
      };
    }
    return {
      badge,
      badgeTone: 'accent',
      context: t('fiche.rule.contextOverall'),
      extra,
      family: 'place',
      sortPrimary,
      sortSecondary,
    };
  }

  if (mode === 'Position') {
    if (group) {
      return {
        badge: place,
        badgeTone: 'accent',
        context: t('fiche.rule.contextGroup', { group }),
        extra,
        family: 'place',
        sortPrimary,
        sortSecondary,
      };
    }
    if (path.rankingScope === 'AcrossGroups') {
      return {
        badge: place,
        badgeTone: 'accent',
        context: t('fiche.rule.contextAcross'),
        extra,
        family: 'place',
        sortPrimary,
        sortSecondary,
      };
    }
    return {
      badge: place,
      badgeTone: 'accent',
      context: t('fiche.rule.contextOverall'),
      extra,
      family: 'place',
      sortPrimary,
      sortSecondary,
    };
  }

  const badge = selectionModeBadge(mode, path.selectionValue, t);
  if (group) {
    return {
      badge,
      badgeTone: 'accent',
      context: t('fiche.rule.contextGroup', { group }),
      extra,
      family: 'place',
      sortPrimary,
      sortSecondary,
    };
  }
  return {
    badge,
    badgeTone: 'accent',
    context: t('fiche.rule.contextOverall'),
    extra,
    family: 'place',
    sortPrimary,
    sortSecondary,
  };
}

/** M2: always Match #n — phase name already shown by flux grouping. */
function matchNumberContext(
  sourceLabel: string | null | undefined,
  fixtureId: string | null | undefined,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const fromLabel = sourceLabel?.match(/#(\d+)/);
  if (fromLabel?.[1]) {
    return t('fiche.rule.matchNumber', { n: fromLabel[1] });
  }
  if (fixtureId) {
    return t('fiche.rule.matchFallback', { id: fixtureId.slice(0, 8) });
  }
  return t('fiche.rule.matchUnknown');
}

function matchSortKey(
  sourceLabel: string | null | undefined,
  fixtureId: string | null | undefined,
): string {
  const fromLabel = sourceLabel?.match(/#(\d+)/);
  if (fromLabel?.[1]) {
    return fromLabel[1].padStart(8, '0');
  }
  return fixtureId ?? '';
}

function progressionRuleParts(
  path: StructureProgressionPath,
  t: (key: string, opts?: Record<string, unknown>) => string,
): Pick<
  FeedRow,
  'badge' | 'badgeTone' | 'context' | 'family' | 'sortPrimary' | 'sortSecondary'
> {
  const isWinner = path.outcome === 'Winner';
  return {
    badge: isWinner ? t('fiche.rule.winner') : t('fiche.rule.loser'),
    badgeTone: isWinner ? 'win' : 'loss',
    context: matchNumberContext(path.sourceLabel, path.sourceFixtureId, t),
    family: 'result',
    sortPrimary: isWinner ? 0 : 1,
    sortSecondary: matchSortKey(path.sourceLabel, path.sourceFixtureId),
  };
}

function placementRuleParts(
  award: StructurePlacementAward,
  t: (key: string, opts?: Record<string, unknown>) => string,
): {
  rank: number;
  medal: 'gold' | 'silver' | 'bronze' | null;
  badge: string;
  badgeTone: 'win' | 'loss';
  context: string;
} {
  const isWinner = award.outcome === 'Winner';
  return {
    rank: award.rank,
    medal:
      award.rank === 1
        ? 'gold'
        : award.rank === 2
          ? 'silver'
          : award.rank === 3
            ? 'bronze'
            : null,
    badge: isWinner ? t('fiche.rule.winner') : t('fiche.rule.loser'),
    badgeTone: isWinner ? 'win' : 'loss',
    context: matchNumberContext(
      award.sourceLabel,
      award.sourceFixtureId,
      t,
    ),
  };
}

function stagePeerOrder(data: StructureView, stageId: string): number {
  const index = data.stages.findIndex((s) => s.stageId === stageId);
  return index >= 0 ? index : Number.MAX_SAFE_INTEGER;
}

function compareFeedRules(
  a: Pick<FeedRow, 'family' | 'sortPrimary' | 'sortSecondary'>,
  b: Pick<FeedRow, 'family' | 'sortPrimary' | 'sortSecondary'>,
): number {
  const familyOrder = (f: FeedFamily) => (f === 'place' ? 0 : 1);
  const byFamily = familyOrder(a.family) - familyOrder(b.family);
  if (byFamily !== 0) return byFamily;
  if (a.sortPrimary !== b.sortPrimary) return a.sortPrimary - b.sortPrimary;
  return a.sortSecondary.localeCompare(b.sortSecondary, undefined, {
    numeric: true,
    sensitivity: 'base',
  });
}

function inboundFeeds(
  data: StructureView,
  stageId: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
) {
  const feeds: FeedRow[] = [];

  for (const source of data.stages) {
    for (const path of source.qualificationPaths ?? []) {
      if (path.destinationStageId !== stageId) continue;
      const parts = qualificationRuleParts(path, t);
      feeds.push({
        key: `q-${source.stageId}-${path.order}`,
        peerId: source.stageId,
        peerName: source.name,
        peerOrder: stagePeerOrder(data, source.stageId),
        ...parts,
        volume: selectionVolume(path),
      });
    }
    for (const path of source.progressionPaths ?? []) {
      if (path.destinationStageId !== stageId) continue;
      const parts = progressionRuleParts(path, t);
      feeds.push({
        key: `p-${source.stageId}-${path.sourceFixtureId}-${path.outcome}`,
        peerId: source.stageId,
        peerName: source.name,
        peerOrder: stagePeerOrder(data, source.stageId),
        ...parts,
        volume: 1,
      });
    }
  }
  return feeds;
}

function outboundFeeds(
  data: StructureView,
  stage: StructureStageHubSummary,
  t: (key: string, opts?: Record<string, unknown>) => string,
) {
  const feeds: FeedRow[] = [];
  const nameOf = (id: string) =>
    data.stages.find((s) => s.stageId === id)?.name ?? id;

  for (const path of stage.qualificationPaths ?? []) {
    const parts = qualificationRuleParts(path, t);
    feeds.push({
      key: `q-out-${path.order}-${path.destinationStageId}`,
      peerId: path.destinationStageId,
      peerName: nameOf(path.destinationStageId),
      peerOrder: stagePeerOrder(data, path.destinationStageId),
      ...parts,
      volume: selectionVolume(path),
    });
  }
  for (const path of stage.progressionPaths ?? []) {
    const parts = progressionRuleParts(path, t);
    feeds.push({
      key: `p-out-${path.sourceFixtureId}-${path.outcome}-${path.destinationStageId}`,
      peerId: path.destinationStageId,
      peerName: nameOf(path.destinationStageId),
      peerOrder: stagePeerOrder(data, path.destinationStageId),
      ...parts,
      volume: 1,
    });
  }
  return feeds;
}

function groupFeeds(feeds: FeedRow[]): FeedGroup[] {
  const map = new Map<string, FeedGroup>();
  for (const feed of feeds) {
    const existing = map.get(feed.peerId);
    if (existing) {
      existing.volume += feed.volume;
      existing.rules.push({
        key: feed.key,
        badge: feed.badge,
        badgeTone: feed.badgeTone,
        context: feed.context,
        extra: feed.extra,
        volume: feed.volume,
        family: feed.family,
        sortPrimary: feed.sortPrimary,
        sortSecondary: feed.sortSecondary,
      });
    } else {
      map.set(feed.peerId, {
        peerId: feed.peerId,
        peerName: feed.peerName,
        peerOrder: feed.peerOrder,
        volume: feed.volume,
        rules: [
          {
            key: feed.key,
            badge: feed.badge,
            badgeTone: feed.badgeTone,
            context: feed.context,
            extra: feed.extra,
            volume: feed.volume,
            family: feed.family,
            sortPrimary: feed.sortPrimary,
            sortSecondary: feed.sortSecondary,
          },
        ],
      });
    }
  }

  const groups = [...map.values()];
  for (const group of groups) {
    group.rules.sort(compareFeedRules);
  }
  groups.sort((a, b) => {
    if (a.peerOrder !== b.peerOrder) return a.peerOrder - b.peerOrder;
    return a.peerName.localeCompare(b.peerName, undefined, {
      sensitivity: 'base',
    });
  });
  return groups;
}

function FluxRuleRow({
  badge,
  badgeTone,
  context,
  extra,
}: {
  badge: string;
  badgeTone: 'win' | 'loss' | 'neutral' | 'accent';
  context: string;
  extra?: string;
}) {
  return (
    <span className="structure-flux-rule">
      <Chip tone={badgeTone}>{badge}</Chip>
      {extra ? <Chip tone="neutral">{extra}</Chip> : null}
      <span className="structure-flux-rule__context">{context}</span>
    </span>
  );
}

function PlacementAwardRow({
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

function FluxGroupList({
  groups,
  onOpenPeer,
  teamsLabel,
}: {
  groups: FeedGroup[];
  onOpenPeer?: (peerId: string) => void;
  teamsLabel: (count: number) => string;
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
              <span className="structure-flux-group__peer">{group.peerName}</span>
            )}
            <Chip tone="neutral">{teamsLabel(group.volume)}</Chip>
          </div>
          <ul className="structure-flux-group__rules">
            {group.rules.map((rule) => (
              <li key={rule.key} className="structure-flux-group__rule">
                <FluxRuleRow
                  badge={rule.badge}
                  badgeTone={rule.badgeTone}
                  context={rule.context}
                  extra={rule.extra}
                />
              </li>
            ))}
          </ul>
        </li>
      ))}
    </ul>
  );
}

function RootEntriesEmpty({
  data,
  stage,
  showTirage,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  showTirage: boolean;
}) {
  const { t } = useTranslation('structure');
  const teamsHref = `/competitions/${data.competitionId}/teams`;
  const competitionTeams =
    data.participants.activeCount ?? data.participants.occupyingCount ?? 0;
  const directAssignments = stage.directAssignmentCount ?? 0;

  return (
    <div className="structure-entries-root">
      <p className="structure-entries-root__lede">{t('fiche.entriesRootLede')}</p>
      <ul className="structure-entries-root__facts">
        <li className="structure-entries-root__fact">
          <span>{t('fiche.entriesRootCompetitionTeams')}</span>
          <Chip tone="neutral">
            {t('fiche.teamsCount', { count: competitionTeams })}
          </Chip>
        </li>
        <li className="structure-entries-root__fact">
          <span>{t('fiche.entriesRootDirectAssignments')}</span>
          <Chip tone="neutral">
            {t('fiche.teamsCount', { count: directAssignments })}
          </Chip>
        </li>
        {showTirage ? (
          <li className="structure-entries-root__fact">
            <span>
              {stage.hasDrawRules
                ? t('fiche.entriesRootDrawConfigured')
                : t('fiche.entriesRootDrawRequired')}
            </span>
          </li>
        ) : null}
      </ul>
      <TextLink to={teamsHref}>{t('fiche.entriesRootOpenTeams')}</TextLink>
    </div>
  );
}

function phaseCapacity(stage: StructureStageHubSummary): number {
  if ((stage.slotCount ?? 0) > 0) return stage.slotCount!;
  if ((stage.teamCount ?? 0) > 0) return stage.teamCount;
  return 0;
}

type OverflowItem = {
  id: string;
  label: string;
  onSelect: () => void;
  danger?: boolean;
  disabled?: boolean;
};

function PhaseOverflowMenu({
  items,
  label,
}: {
  items: OverflowItem[];
  label: string;
}) {
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);
  if (items.length === 0) return null;

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
          <LucideIcon icon={EllipsisVertical} size="sm" />
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
          {items.map((item) => (
            <li key={item.id} role="none">
              <button
                type="button"
                role="menuitem"
                className={[
                  'structure-overflow-menu__item',
                  item.danger ? 'structure-overflow-menu__item--danger' : null,
                ]
                  .filter(Boolean)
                  .join(' ')}
                disabled={item.disabled}
                onClick={() => {
                  setOpen(false);
                  if (!item.disabled) item.onSelect();
                }}
              >
                {item.label}
              </button>
            </li>
          ))}
        </ul>
      </Popover>
    </>
  );
}

function DomainTile({
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

function FluxRail({
  id,
  title,
  icon,
  editLabel,
  onEdit,
  children,
  side,
}: {
  id: string;
  title: string;
  icon: ReactNode;
  editLabel: string;
  onEdit?: () => void;
  children: ReactNode;
  side: 'in' | 'out';
}) {
  const titleId = `${id}-title`;
  return (
    <aside
      id={id}
      className={`structure-phase-rail structure-phase-rail--${side}`}
      aria-labelledby={titleId}
    >
      <header className="structure-phase-rail__head">
        <span className="structure-phase-rail__icon" aria-hidden="true">
          {icon}
        </span>
        <h3 id={titleId} className="structure-phase-rail__title">
          {title}
        </h3>
        {onEdit ? (
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
        ) : null}
      </header>
      <div className="structure-phase-rail__body">{children}</div>
    </aside>
  );
}

function AssemblyMeter({
  entries,
  places,
  gap,
  t,
}: {
  entries: number;
  places: number;
  gap: number | null;
  t: (key: string, opts?: Record<string, unknown>) => string;
}) {
  const ratio =
    places > 0 ? Math.min(1, Math.max(0, entries / places)) : entries > 0 ? 1 : 0;
  const tone = gap == null || gap === 0 ? 'ok' : gap < 0 ? 'short' : 'over';
  return (
    <div
      className={`structure-assembly structure-assembly--${tone}`}
      aria-label={t('fiche.assemblyAria')}
    >
      <div className="structure-assembly__row">
        <span>{t('fiche.assembly.entries')}</span>
        <strong>{entries}</strong>
      </div>
      <div className="structure-assembly__track" aria-hidden="true">
        <span
          className="structure-assembly__fill"
          style={{ width: `${ratio * 100}%` }}
        />
      </div>
      <div className="structure-assembly__row">
        <span>{t('fiche.assembly.places')}</span>
        <strong>{places}</strong>
      </div>
      {gap != null && gap !== 0 ? (
        <p className="structure-assembly__gap" role="status">
          <span className="structure-assembly__gap-icon" aria-hidden="true">
            <LucideIcon icon={CircleAlert} size="sm" />
          </span>
          {gap < 0
            ? t('fiche.assembly.shortfall', { count: Math.abs(gap) })
            : t('fiche.assembly.surplus', { count: gap })}
        </p>
      ) : null}
    </div>
  );
}

function ConfrontationPanel({
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
    const items: { key: string; label: string; icon: typeof ArrowRight }[] = [
      {
        key: `${prefix}-legs`,
        label: twoLegs ? t('tokens.tieTwoLegs') : t('tokens.tieOneLeg'),
        icon: twoLegs ? ArrowLeftRight : ArrowRight,
      },
    ];
    if (seg.aggregateScoring) {
      items.push({
        key: `${prefix}-agg`,
        label: t('tokens.tieAggregate'),
        icon: Sigma,
      });
    }
    if (seg.hasAwayGoalsRule) {
      items.push({
        key: `${prefix}-away`,
        label: t('tokens.tieAwayGoals'),
        icon: MapPin,
      });
    }
    if (seg.hasTieExtraTime) {
      items.push({
        key: `${prefix}-et`,
        label: t('tokens.tieExtraTime'),
        icon: Timer,
      });
    }
    if (seg.hasTiePenaltyShootout) {
      items.push({
        key: `${prefix}-tab`,
        label: t('tokens.tiePenalties'),
        icon: Goal,
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
              <LucideIcon icon={item.icon} size="sm" />
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
          seg.rounds.map((r) => r.name).filter(Boolean).join(' / ') ||
          `§${index + 1}`;
        const items = renderItems(seg, `seg-${index}`);
        return (
          <div key={title + index} className="structure-confrontation-segment">
            {segments.length > 1 ? (
              <p className="structure-confrontation-segment__title">{title}</p>
            ) : null}
            <ul className="regulation-rule-list structure-confrontation-list">
              {items.map((item) => (
                <li key={item.key} className="regulation-rule-list__item">
                  <span className="regulation-rule-list__mark" aria-hidden="true">
                    <LucideIcon icon={item.icon} size="sm" />
                  </span>
                  <span className="regulation-rule-list__label">{item.label}</span>
                </li>
              ))}
            </ul>
          </div>
        );
      })}
    </div>
  );
}

function DrawConfigPanel({ stage }: { stage: StructureStageHubSummary }) {
  const { t } = useTranslation('structure');
  if (!stage.hasDrawRules) {
    return null;
  }
  const pots = Math.max(stage.numberOfPots ?? 0, 0);
  const modeLabel =
    stage.drawMode === 'Random'
      ? t('fiche.tirage.random')
      : stage.drawMode ?? null;
  return (
    <ul className="structure-draw-facts">
      {modeLabel ? (
        <li>
          <RandomIcon size="sm" />
          <span>{modeLabel}</span>
        </li>
      ) : null}
      {pots > 0 ? (
        <li>
          <LayersIcon size="sm" />
          <span>{t('fiche.tirage.pots', { pots })}</span>
        </li>
      ) : null}
      {(stage.numberOfSeeds ?? 0) > 0 ? (
        <li>
          <SeedsIcon size="sm" />
          <span>{t('fiche.tirage.seeds', { count: stage.numberOfSeeds })}</span>
        </li>
      ) : null}
      {(stage.drawConstraints?.length ?? 0) > 0 ? (
        <li>
          <DrawConstraintIcon size="sm" />
          <span>
            {t('fiche.tirage.constraints', {
              count: stage.drawConstraints!.length,
            })}
          </span>
        </li>
      ) : null}
    </ul>
  );
}

export function StructurePhaseFiche({
  data,
  stage,
  canConfigure: _canConfigure,
  onConfigure,
  onSelectStage,
  initialEdit,
  onInitialEditConsumed,
}: {
  data: StructureView;
  stage: StructureStageHubSummary | null;
  canConfigure: boolean;
  onConfigure: () => void;
  onSelectStage?: (stageId: string) => void;
  initialEdit?: StructureSectionId | null;
  onInitialEditConsumed?: () => void;
}) {
  const { t } = useTranslation('structure');
  const [schematicMode, setSchematicMode] = useState<SchematicMode>('slots');
  const [edit, setEdit] = useState<EditTarget>(null);
  const [removeOpen, setRemoveOpen] = useState(false);

  useEffect(() => {
    setSchematicMode('slots');
    setEdit(null);
    setRemoveOpen(false);
  }, [stage?.stageId]);

  useEffect(() => {
    if (!initialEdit || !stage) return;
    const map: Partial<Record<StructureSectionId, EditTarget>> = {
      qualification: 'qualification',
      progression: 'progression',
      tirage: 'tirage',
      confrontation: 'confrontation',
      matchs: 'matchs',
      classement: 'classement',
      construction: null,
    };
    const target = map[initialEdit];
    if (target) {
      setEdit(target);
    } else if (initialEdit === 'construction') {
      onConfigure();
    }
    onInitialEditConsumed?.();
  }, [initialEdit, stage, onConfigure, onInitialEditConsumed]);

  const occupantsQuery = useQuery({
    queryKey: queryKeys.stages.detail(stage?.stageId ?? ''),
    queryFn: () => fetchStageOverview(stage!.stageId),
    enabled: schematicMode === 'equipes' && !!stage?.stageId,
  });

  const occupantLabels = useMemo(() => {
    if (schematicMode !== 'equipes' || !occupantsQuery.data) return undefined;
    return occupantsQuery.data.slots.map(
      (s) => s.displayName?.trim() || '',
    );
  }, [schematicMode, occupantsQuery.data]);

  if (!stage) {
    return (
      <section className="structure-fiche" aria-labelledby="fiche-empty">
        <h2 id="fiche-empty" className="structure-fiche__title">
          {t('hub.phaseFiche')}
        </h2>
        <p className="structure-panel__muted">{t('hub.noPhaseSelectedBody')}</p>
      </section>
    );
  }

  const capacity = phaseCapacity(stage);
  const feeds = inboundFeeds(data, stage.stageId, t);
  const feedGroups = groupFeeds(feeds);
  const feedSum = feeds.reduce((acc, f) => acc + f.volume, 0);
  const gap = capacity > 0 ? feedSum - capacity : null;
  const outbounds = outboundFeeds(data, stage, t);
  const outboundGroups = groupFeeds(outbounds);
  const matchBound = isMatchFrameBound(stage.defaultsBinding);
  const standingBound = isStandingFrameBound(stage.defaultsBinding);
  const sections = relevantPhaseSections(stage);
  const actions = stageActions(stage);
  const regulationHref = `/competitions/${data.competitionId}/regulation`;
  const drawHref = `/competitions/${data.competitionId}/stages/${stage.stageId}`;
  const showTirage = sections.includes('tirage');
  const showConfrontation = sections.includes('confrontation');
  const tirageRequired = showTirage && !stage.hasDrawRules;
  const canEditProg = actions.includes('ReplaceProgressionRules');
  const canEditPlacement = actions.includes('ReplacePlacementAwardRules');
  const canEditDraw = actions.includes('ReplaceDrawRules');
  const canEditTie = actions.includes('ReplaceDefaultTieFormat');
  const canEditMatch = actions.includes('ReplaceMatchRules');
  const canEditStanding = actions.includes('ReplaceStandingRules');
  const canRebind = actions.includes('BindToCompetition');
  const canRemove = actions.includes('RemoveStage');
  const placementAwards = stage.placementAwards ?? [];
  const hasExits = outboundGroups.length > 0;
  const hasAttribution = placementAwards.length > 0;
  const showOutRail = hasExits || hasAttribution;
  const teamsLabel = (count: number) => t('fiche.teamsCount', { count });
  const formatLabel = stage.formatKind
    ? structureFormatKindLabel(stage.formatKind)
    : null;

  const heroFacts: { icon: ReactNode; value: string | number; label: string }[] =
    [];
  const kind = stage.formatKind;
  if (kind === 'Groups' && (stage.groupCount ?? 0) > 0) {
    const n = stage.groupCount!;
    heroFacts.push({
      icon: <GroupsFormatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.groups', { count: n }),
    });
  } else if (kind === 'Cup' && (stage.roundCount ?? 0) > 0) {
    const n = stage.roundCount!;
    heroFacts.push({
      icon: <RoundsStatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.rounds', { count: n }),
    });
  } else if (kind === 'Championship' && (stage.matchdayCount ?? 0) > 0) {
    const n = stage.matchdayCount!;
    heroFacts.push({
      icon: <MatchdayStatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.matchdays', { count: n }),
    });
  } else if (kind === 'Swiss' && (stage.swissRoundCount ?? 0) > 0) {
    const n = stage.swissRoundCount!;
    heroFacts.push({
      icon: <RoundsStatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.rounds', { count: n }),
    });
  }
  {
    const n = capacity || stage.teamCount;
    heroFacts.push({
      icon: <LayersIcon size="sm" />,
      value: n,
      label: t('fiche.stat.teams', { count: n }),
    });
  }

  const overflowItems: OverflowItem[] = [];
  if (canEditProg && !hasExits) {
    overflowItems.push({
      id: 'add-exit',
      label: t('fiche.addExit'),
      onSelect: () => setEdit('progression'),
    });
  }
  if (canEditPlacement && !hasAttribution) {
    overflowItems.push({
      id: 'add-attribution',
      label: t('fiche.addAttribution'),
      onSelect: () => setEdit('placement'),
    });
  }
  overflowItems.push({
    id: 'remove',
    label: t('graph.removePhase'),
    onSelect: () => setRemoveOpen(true),
    danger: true,
    disabled: !canRemove,
  });

  return (
    <section
      className="structure-fiche structure-fiche--flat"
      aria-labelledby="phase-overview-heading"
    >
      <header className="structure-fiche__header">
        <div className="structure-fiche__titles">
          <div className="structure-fiche__title-row">
            {formatLabel ? (
              <Tooltip content={formatLabel}>
                <span className="structure-fiche__format-icon">
                  <FormatGlyph kind={stage.formatKind} size="lg" />
                </span>
              </Tooltip>
            ) : (
              <span className="structure-fiche__format-icon" aria-hidden="true">
                <FormatGlyph kind={stage.formatKind} size="lg" />
              </span>
            )}
            <h2 id="phase-overview-heading" className="structure-fiche__title">
              {stage.name}
            </h2>
          </div>
          <ul className="structure-fiche__pills">
            <li className="structure-fiche__pill structure-fiche__pill--status">
              <StageStatusBadge status={stage.status} />
            </li>
          </ul>
        </div>
        <div className="structure-fiche__actions">
          <PhaseOverflowMenu
            label={t('fiche.moreActions')}
            items={overflowItems}
          />
        </div>
      </header>

      <div className="structure-phase-hero">
        <div
          className={[
            'structure-phase-hero__triptych',
            showOutRail ? null : 'structure-phase-hero__triptych--no-out',
          ]
            .filter(Boolean)
            .join(' ')}
        >
          <FluxRail
            id="rail-entries"
            side="in"
            title={t('fiche.tiles.entries')}
            icon={<ArrowDownIcon size="md" />}
            editLabel={t('fiche.edit')}
          >
            {feedGroups.length === 0 ? (
              <RootEntriesEmpty
                data={data}
                stage={stage}
                showTirage={showTirage}
              />
            ) : (
              <FluxGroupList
                groups={feedGroups}
                onOpenPeer={onSelectStage}
                teamsLabel={teamsLabel}
              />
            )}
            {capacity > 0 ? (
              <AssemblyMeter
                entries={feedSum}
                places={capacity}
                gap={gap}
                t={t}
              />
            ) : null}
          </FluxRail>

          <div className="structure-phase-hero__center">
            <div className="structure-phase-hero__toolbar">
              <div
                className="structure-schematic-toggle"
                role="group"
                aria-label={t('fiche.schematicToggle')}
              >
                <button
                  type="button"
                  className={
                    schematicMode === 'slots'
                      ? 'structure-schematic-toggle__btn structure-schematic-toggle__btn--active'
                      : 'structure-schematic-toggle__btn'
                  }
                  aria-pressed={schematicMode === 'slots'}
                  onClick={() => setSchematicMode('slots')}
                >
                  {t('fiche.slots')}
                </button>
                <button
                  type="button"
                  className={
                    schematicMode === 'equipes'
                      ? 'structure-schematic-toggle__btn structure-schematic-toggle__btn--active'
                      : 'structure-schematic-toggle__btn'
                  }
                  aria-pressed={schematicMode === 'equipes'}
                  onClick={() => setSchematicMode('equipes')}
                >
                  {t('fiche.equipes')}
                </button>
              </div>
            </div>
            <div className="structure-schematic-viewport">
              <div className="structure-schematic-viewport__scale">
                <PhaseSchematic
                  stage={stage}
                  density="hero"
                  occupantLabels={occupantLabels}
                  showSlotPlaceholders={schematicMode === 'slots'}
                />
              </div>
            </div>
            <ul
              className="structure-phase-hero__stats"
              aria-label={t('fiche.statsAria')}
            >
              {heroFacts.map((fact) => (
                <li key={fact.label} className="structure-phase-hero__stat">
                  <span
                    className="structure-phase-hero__stat-icon"
                    aria-hidden="true"
                  >
                    {fact.icon}
                  </span>
                  <span className="structure-phase-hero__stat-value">
                    {fact.value}
                  </span>
                  <span className="structure-phase-hero__stat-label">
                    {fact.label}
                  </span>
                </li>
              ))}
            </ul>
            {showTirage ? (
              <div className="structure-phase-hero__draw">
                <Link className="ds-btn ds-btn--primary" to={drawHref}>
                  <DrawPendingIcon size="sm" />
                  <span>{t('fiche.openDrawWorkflow')}</span>
                </Link>
              </div>
            ) : null}
          </div>

          {showOutRail ? (
            <div className="structure-phase-hero__out">
              {hasExits ? (
                <FluxRail
                  id="rail-exits"
                  side="out"
                  title={t('fiche.tiles.exits')}
                  icon={<ArrowRightIcon size="md" />}
                  editLabel={t('fiche.edit')}
                  onEdit={
                    canEditProg ? () => setEdit('progression') : undefined
                  }
                >
                  <FluxGroupList
                    groups={outboundGroups}
                    onOpenPeer={onSelectStage}
                    teamsLabel={teamsLabel}
                  />
                </FluxRail>
              ) : null}
              {hasAttribution ? (
                <FluxRail
                  id="rail-attribution"
                  side="out"
                  title={t('fiche.tiles.attribution')}
                  icon={<AttributionIcon size="md" />}
                  editLabel={t('fiche.edit')}
                  onEdit={
                    canEditPlacement ? () => setEdit('placement') : undefined
                  }
                >
                  <ul className="structure-flux-group__rules">
                    {[...placementAwards]
                      .sort((a, b) => a.rank - b.rank)
                      .map((award) => {
                      const parts = placementRuleParts(award, t);
                      return (
                        <li
                          key={`${award.rank}-${award.outcome}-${award.sourceFixtureId ?? ''}`}
                          className="structure-flux-group__rule"
                        >
                          <PlacementAwardRow
                            rank={parts.rank}
                            medal={parts.medal}
                            badge={parts.badge}
                            badgeTone={parts.badgeTone}
                            context={parts.context}
                          />
                        </li>
                      );
                    })}
                  </ul>
                </FluxRail>
              ) : null}
            </div>
          ) : null}
        </div>
        {schematicMode === 'equipes' && occupantsQuery.isError ? (
          <p className="structure-panel__muted">
            {t('fiche.occupantsUnavailable')}
          </p>
        ) : null}
      </div>

      <div className="structure-domain-tiles">
        <DomainTile
          id="tile-match"
          title={t('fiche.tiles.match')}
          icon={<MatchRulesIcon size="md" />}
          editLabel={t('fiche.edit')}
          badge={
            matchBound ? (
              <Chip tone="neutral">{t('fiche.binding.general')}</Chip>
            ) : (
              <StatusBadge tone="warn" density="compact">
                {t('fiche.binding.personalized')}
              </StatusBadge>
            )
          }
          onEdit={
            canEditMatch || canRebind
              ? () => setEdit(canEditMatch ? 'matchs' : 'rebind-match')
              : undefined
          }
          compact
          footer={
            <TextLink to={regulationHref}>
              {t('hub.overview.openRegulation')}
            </TextLink>
          }
        >
          <div className="structure-domain-tile--reg">
            <MatchRulesPanel
              numberOfPeriods={stage.numberOfPeriods}
              durationPerPeriod={stage.durationPerPeriod}
              halfTimeDuration={stage.halfTimeDuration}
              hasExtraTime={stage.hasExtraTime}
              extraTimeNumberOfPeriods={stage.extraTimeNumberOfPeriods}
              extraTimeDurationPerPeriod={stage.extraTimeDurationPerPeriod}
              hasPenaltyShootout={stage.hasPenaltyShootout}
              penaltyInitialKicksPerTeam={stage.penaltyInitialKicksPerTeam}
            />
          </div>
        </DomainTile>

        {standingBound !== null ? (
          <DomainTile
            id="tile-standing"
            title={t('fiche.tiles.standing')}
            icon={<StandingRulesIcon size="md" />}
            editLabel={t('fiche.editStanding')}
            badge={
              standingBound ? (
                <Chip tone="neutral">{t('fiche.binding.general')}</Chip>
              ) : (
                <StatusBadge tone="warn" density="compact">
                  {t('fiche.binding.personalized')}
                </StatusBadge>
              )
            }
            onEdit={
              canEditStanding || canRebind
                ? () =>
                    setEdit(canEditStanding ? 'classement' : 'rebind-standing')
                : undefined
            }
            compact
            footer={
              <TextLink to={regulationHref}>
                {t('hub.overview.openRegulation')}
              </TextLink>
            }
          >
            <div className="structure-domain-tile--reg">
              <StandingRulesPanel
                winPoints={stage.winPoints ?? 0}
                drawPoints={stage.drawPoints ?? 0}
                lossPoints={stage.lossPoints ?? 0}
                rankingCriteria={stage.rankingCriteria}
                forfeitWinnerGoals={stage.forfeitWinnerGoals}
                forfeitLoserGoals={stage.forfeitLoserGoals}
              />
            </div>
          </DomainTile>
        ) : null}

        {showTirage ? (
          <DomainTile
            id="tile-tirage"
            title={t('fiche.tiles.tirage')}
            icon={<DrawConfigIcon size="md" />}
            editLabel={t('fiche.edit')}
            onEdit={canEditDraw ? () => setEdit('tirage') : undefined}
            compact
            attention={tirageRequired}
            badge={
              tirageRequired ? (
                <Status density="compact" tone="attention">
                  {t('fiche.tirage.required')}
                </Status>
              ) : undefined
            }
          >
            {tirageRequired ? (
              <p className="structure-panel__muted">{t('fiche.tirage.missingBody')}</p>
            ) : (
              <DrawConfigPanel stage={stage} />
            )}
          </DomainTile>
        ) : null}

        {showConfrontation ? (
          <DomainTile
            id="tile-confrontation"
            title={t('fiche.tiles.confrontation')}
            icon={<ConfrontationIcon size="md" />}
            editLabel={t('fiche.edit')}
            onEdit={canEditTie ? () => setEdit('confrontation') : undefined}
            compact
          >
            <ConfrontationPanel stage={stage} />
          </DomainTile>
        ) : null}
      </div>

      {canRemove && (
        <RemovePhaseDialog
          data={data}
          stage={stage}
          open={removeOpen}
          onClose={() => setRemoveOpen(false)}
        />
      )}
      <QualificationRulesDialog
        data={data}
        stage={stage}
        open={edit === 'qualification'}
        onClose={() => setEdit(null)}
      />
      <ProgressionRulesDialog
        data={data}
        stage={stage}
        open={edit === 'progression'}
        onClose={() => setEdit(null)}
      />
      <PlacementAwardRulesDialog
        data={data}
        stage={stage}
        open={edit === 'placement'}
        onClose={() => setEdit(null)}
      />
      <DrawRulesDialog
        competitionId={data.competitionId}
        stage={stage}
        open={edit === 'tirage'}
        onClose={() => setEdit(null)}
      />
      <TieFormatDialog
        competitionId={data.competitionId}
        stage={stage}
        open={edit === 'confrontation'}
        onClose={() => setEdit(null)}
      />
      <MatchRulesDialog
        competitionId={data.competitionId}
        stage={stage}
        open={edit === 'matchs'}
        onClose={() => setEdit(null)}
      />
      <StandingRulesDialog
        competitionId={data.competitionId}
        stage={stage}
        open={edit === 'classement'}
        onClose={() => setEdit(null)}
      />
      <RebindDialog
        competitionId={data.competitionId}
        stage={stage}
        scope={edit === 'rebind-standing' ? 'Standing' : 'Match'}
        open={edit === 'rebind-match' || edit === 'rebind-standing'}
        onClose={() => setEdit(null)}
      />
    </section>
  );
}
