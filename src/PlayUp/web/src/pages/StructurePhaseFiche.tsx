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
import { fetchStageOverview, fetchStageSchematic } from '../api';
import { Chip } from '../design-system/components/Chip';
import { Popover } from '../design-system/components/Popover';
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
  PlusIcon,
  PencilIcon,
  RandomIcon,
  RoundsStatIcon,
  SeedsIcon,
  StandingRulesIcon,
  StructureIcon,
  SwissFormatIcon,
  EmptySelectionIcon,
} from '../design-system/icons/contentIcons';
import { structureFormatKindLabel } from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import { TeamCrest } from '../design-system/TeamCrest';
import { EmptyState, LoadingState, StageStatusBadge, StatusBadge } from '../ui';
import type {
  SelectionMode,
  StageSchematic,
  StructureConfrontationSegment,
  StructureEntry,
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
  StandingRulesDialog,
  TieFormatDialog,
} from './StructureRegulationDialogs';
import { StructureDrawDialog } from './StructureDrawDialog';
import { StructureCompositionDialog } from './StructureCompositionDialog';
import {
  getDrawTilePrimarySummary,
  pickActiveDraw,
} from './drawUi';
import {
  isPopulationDestination,
} from './structureProgression';
import { placeChromeForDestinationSlotKey } from './structurePlaceLabel';
import {
  inboundPopulationConfiguredVolume,
  qualificationPathVolume,
} from './structurePopulationVolume';
import {
  isMatchFrameBound,
  isStandingFrameBound,
  relevantPhaseSections,
  type StructureSectionId,
} from './structureHubSections';
import { resolvePlacesN } from './structurePlaces';
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
  return qualificationPathVolume(path);
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
  schematic?: StageSchematic | null,
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
  // Same as progression: Place → chrome chip; else optional Points ≥ gate.
  let extra: string | undefined;
  if (!isPopulationDestination(path.destinationSlotKey)) {
    extra =
      placeChromeForDestinationSlotKey(schematic, path.destinationSlotKey) ??
      undefined;
  } else if (path.minimumPoints != null) {
    extra = t('fiche.rule.minimumPoints', { n: path.minimumPoints });
  }

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
  schematic?: StageSchematic | null,
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
  const isWinner = path.outcome === 'Winner';
  let destinationExtra: string | undefined;
  if (isPopulationDestination(path.destinationSlotKey)) {
    destinationExtra = t('fiche.rule.destinationPopulation');
  } else {
    // U4/C2: same Place id as schematic chrome (SlotKey) — fits the rail chip.
    destinationExtra =
      placeChromeForDestinationSlotKey(schematic, path.destinationSlotKey) ??
      undefined;
  }
  return {
    badge: isWinner ? t('fiche.rule.winner') : t('fiche.rule.loser'),
    badgeTone: isWinner ? 'win' : 'loss',
    context: matchNumberContext(path.sourceLabel, path.sourceFixtureId, t),
    extra: destinationExtra,
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
  schematic?: StageSchematic | null,
) {
  const feeds: FeedRow[] = [];

  for (const source of data.stages) {
    for (const path of source.qualificationPaths ?? []) {
      if (path.destinationStageId !== stageId) continue;
      const parts = qualificationRuleParts(path, t, schematic);
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
      const parts = progressionRuleParts(path, t, schematic);
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
  schematic?: StageSchematic | null,
) {
  const feeds: FeedRow[] = [];
  const nameOf = (id: string) =>
    data.stages.find((s) => s.stageId === id)?.name ?? id;

  for (const path of stage.qualificationPaths ?? []) {
    const parts = qualificationRuleParts(path, t, schematic);
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
    const parts = progressionRuleParts(path, t, schematic);
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
      <span className="structure-flux-rule__source">
        <Chip tone={badgeTone}>{badge}</Chip>
        <span className="structure-flux-rule__context">{context}</span>
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
              <span className="structure-flux-group__peer">{group.peerName}</span>
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
                />
              </li>
            ))}
          </ul>
        </li>
      ))}
    </ul>
  );
}

function RootEntriesRail({
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
  /** Expected volume from inbound rules (promised Entrées, not yet necessarily in CompositionEntries). */
  sourcesConfiguredVolume?: number;
  canCompose?: boolean;
  onEditCompose?: () => void;
}) {
  const { t } = useTranslation('structure');
  const fromAffectation = stage.compositionEntryCount ?? 0;
  const fromFeeds = sourcesConfiguredVolume;
  const n = resolvePlacesN(stage);
  const ineligible = stage.compositionIneligibleCount ?? 0;
  const composedIds = stage.compositionEntryIds ?? [];
  const previewNames = stage.compositionPreviewNames ?? [];
  const previewOverflow = stage.compositionPreviewOverflow ?? 0;
  const isLive =
    stage.status === 'Running' ||
    stage.status === 'Suspended' ||
    stage.status === 'Completed';
  // Draft/Ready: expected Places N occupancy = Affectation + inbound Qual/Prog.
  // Live: CompositionEntries is the Draw/Apply truth (rules would double-count).
  const meterEntries = isLive
    ? fromAffectation
    : fromAffectation + fromFeeds;
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
    composed.length > 0 ||
    previewNames.length > 0 ||
    ineligible > 0;
  const showAffectationSection = Boolean(canCompose) || hasTeams;

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
          <section className="structure-entries__section" aria-labelledby="population-sources-title">
            <header className="structure-entries__section-head">
              <h4 id="population-sources-title" className="structure-entries__section-title">
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
                {previewOverflow > 0
                  ? ` ${t('population.previewOverflow', { count: previewOverflow })}`
                  : ''}
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
          assigned={isLive ? undefined : fromAffectation}
          expected={isLive ? undefined : fromFeeds}
          t={t}
        />
      </div>
    </div>
  );
}

/** Cardinality gauge — expected Population vs Places N; shortfall = danger. */
function CompositionMeter({
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
  const tone =
    gap == null || gap === 0 ? 'ok' : gap < 0 ? 'short' : 'over';

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
            <LucideIcon icon={CircleAlert} size="sm" />
          </span>
          {gap < 0
            ? t('entries.meter.shortfall', { count: Math.abs(gap) })
            : t('entries.meter.surplus', { count: gap })}
        </p>
      ) : null}
    </div>
  );
}

type OverflowItem = {
  id: string;
  label: string;
  onSelect?: () => void;
  submenu?: OverflowItem[];
  danger?: boolean;
  disabled?: boolean;
};

type ExitKind = 'qualification' | 'progression';

function PhaseOverflowMenu({
  items,
  label,
  backLabel,
}: {
  items: OverflowItem[];
  label: string;
  backLabel: string;
}) {
  const [open, setOpen] = useState(false);
  const [submenuParent, setSubmenuParent] = useState<OverflowItem | null>(null);
  const anchorRef = useRef<HTMLButtonElement>(null);
  if (items.length === 0) return null;

  const visible = submenuParent?.submenu ?? items;
  const menuLabel = submenuParent?.label ?? label;

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
          onClick={() => {
            setSubmenuParent(null);
            setOpen((value) => !value);
          }}
        >
          <LucideIcon icon={EllipsisVertical} size="sm" />
        </button>
      </Tooltip>
      <Popover
        open={open}
        onOpenChange={(next) => {
          setOpen(next);
          if (!next) setSubmenuParent(null);
        }}
        anchorRef={anchorRef}
        role="menu"
        align="end"
        width={240}
        aria-label={menuLabel}
      >
        <ul className="structure-overflow-menu">
          {submenuParent ? (
            <li role="none">
              <button
                type="button"
                role="menuitem"
                className="structure-overflow-menu__item structure-overflow-menu__item--back"
                onClick={() => setSubmenuParent(null)}
              >
                {backLabel}
              </button>
            </li>
          ) : null}
          {visible.map((item) => (
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
                  if (item.disabled) return;
                  if (item.submenu && item.submenu.length > 0) {
                    setSubmenuParent(item);
                    return;
                  }
                  setOpen(false);
                  setSubmenuParent(null);
                  item.onSelect?.();
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

/** Compact pencil → single action, or Qualif | Prog menu when both apply. */
function ExitKindMenu({
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

function exitKindsPresent(group: FeedGroup): Set<ExitKind> {
  const kinds = new Set<ExitKind>();
  for (const rule of group.rules) {
    kinds.add(rule.family === 'place' ? 'qualification' : 'progression');
  }
  return kinds;
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
  editControl,
  children,
  side,
}: {
  id: string;
  title: string;
  icon: ReactNode;
  editControl?: ReactNode;
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
        {editControl}
      </header>
      <div className="structure-phase-rail__body">{children}</div>
    </aside>
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
  initialCompose,
  onInitialEditConsumed,
}: {
  data: StructureView;
  stage: StructureStageHubSummary | null;
  canConfigure: boolean;
  onConfigure: () => void;
  onSelectStage?: (stageId: string) => void;
  initialEdit?: StructureSectionId | null;
  initialCompose?: boolean;
  onInitialEditConsumed?: () => void;
}) {
  const { t } = useTranslation('structure');
  const [edit, setEdit] = useState<EditTarget>(null);
  const [rulesEditStage, setRulesEditStage] =
    useState<StructureStageHubSummary | null>(null);
  const [removeOpen, setRemoveOpen] = useState(false);
  const [drawWorkflowOpen, setDrawWorkflowOpen] = useState(false);
  const [composeOpen, setComposeOpen] = useState(false);
  const [composeFocusSearch, setComposeFocusSearch] = useState(false);

  useEffect(() => {
    setEdit(null);
    setRulesEditStage(null);
    setRemoveOpen(false);
    setDrawWorkflowOpen(false);
    setComposeOpen(false);
    setComposeFocusSearch(false);
  }, [stage?.stageId]);

  useEffect(() => {
    if (!stage) return;
    if (initialCompose) {
      setComposeFocusSearch(true);
      setComposeOpen(true);
      onInitialEditConsumed?.();
      return;
    }
    if (!initialEdit) return;
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
      setRulesEditStage(stage);
      setEdit(target);
    } else if (initialEdit === 'construction') {
      onConfigure();
    }
    onInitialEditConsumed?.();
  }, [initialEdit, initialCompose, stage, onConfigure, onInitialEditConsumed]);

  const schematicQuery = useQuery({
    queryKey: queryKeys.stages.schematic(stage?.stageId ?? ''),
    queryFn: () => fetchStageSchematic(stage!.stageId),
    enabled: !!stage?.stageId,
  });

  const drawOverviewQuery = useQuery({
    queryKey: queryKeys.stages.detail(stage?.stageId ?? ''),
    queryFn: () => fetchStageOverview(stage!.stageId),
    enabled:
      !!stage?.stageId &&
      (stage.formatKind === 'Groups' ||
        stage.formatKind === 'Cup' ||
        Boolean(stage.hasDrawRules)),
  });

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

  const feeds = inboundFeeds(data, stage.stageId, t, schematicQuery.data);
  const feedGroups = groupFeeds(feeds);
  // Places N meter / Affectation reserve: Qual + Prog (Population and Place).
  // Place fills occupy capacity even though they skip the Population set.
  const populationFeedVolume = inboundPopulationConfiguredVolume(
    data,
    stage.stageId,
  );
  const populationCount = stage.compositionEntryCount ?? 0;
  const outbounds = outboundFeeds(data, stage, t, schematicQuery.data);
  const outboundGroups = groupFeeds(outbounds);
  const matchBound = isMatchFrameBound(stage.defaultsBinding);
  const standingBound = isStandingFrameBound(stage.defaultsBinding);
  const sections = relevantPhaseSections(stage);
  const actions = stageActions(stage);
  const regulationHref = `/competitions/${data.competitionId}/regulation`;
  const activeDraw = pickActiveDraw(drawOverviewQuery.data?.draws ?? []);
  const showTirage = stage.hasDrawRules || Boolean(activeDraw);
  const showConfrontation = sections.includes('confrontation');
  const tiragePrimary = activeDraw
    ? getDrawTilePrimarySummary(
        activeDraw,
        drawOverviewQuery.data?.slots ?? [],
        drawOverviewQuery.data?.rounds ?? [],
      )
    : null;
  const canEditProg = actions.includes('ReplaceProgressionRules');
  const canEditPlacement = actions.includes('ReplacePlacementAwardRules');
  const canEditDraw = actions.includes('ReplaceDrawRules');
  const canEditTie =
    actions.includes('ReplaceDefaultTieFormat') ||
    actions.includes('ReplaceRoundTieFormat');
  const canEditMatch = actions.includes('ReplaceMatchRules');
  const canEditStanding = actions.includes('ReplaceStandingRules');
  const canRebind = actions.includes('BindToCompetition');
  const canRemove = actions.includes('RemoveStage');
  const canCompose = actions.includes('ReplaceCompositionEntries');
  const canEditQualif = actions.includes('ReplaceQualificationRules');
  const placementAwards = stage.placementAwards ?? [];
  const hasExits = outboundGroups.length > 0;
  const hasAttribution = placementAwards.length > 0;
  const showOutRail = hasExits || hasAttribution;
  const teamsLabel = (count: number) => t('fiche.teamsCount', { count });
  const formatLabel = stage.formatKind
    ? structureFormatKindLabel(stage.formatKind)
    : null;

  const openCompose = (focusSearch = false) => {
    setComposeFocusSearch(focusSearch);
    setComposeOpen(true);
  };

  const closeRulesEdit = () => {
    setEdit(null);
    setRulesEditStage(null);
  };

  /** Open Qualif/Prog editor for a source stage without changing the selected fiche. */
  const openExitEdit = (
    kind: ExitKind,
    sourceStage: StructureStageHubSummary = stage,
  ) => {
    setRulesEditStage(sourceStage);
    setEdit(kind);
  };

  const canAddExit = data.stages.length >= 2;

  const exitEditOptions = (
    mode: 'add' | 'edit' = 'edit',
  ): {
    kind: ExitKind;
    label: string;
    onSelect: () => void;
  }[] => {
    if (mode === 'add' && !canAddExit) return [];
    const options: {
      kind: ExitKind;
      label: string;
      onSelect: () => void;
    }[] = [];
    if (canEditQualif) {
      options.push({
        kind: 'qualification',
        label: t('fiche.exitKind.qualification'),
        onSelect: () => openExitEdit('qualification'),
      });
    }
    if (canEditProg) {
      options.push({
        kind: 'progression',
        label: t('fiche.exitKind.progression'),
        onSelect: () => openExitEdit('progression'),
      });
    }
    return options;
  };

  const renderAvalSourceAction = (group: FeedGroup): ReactNode => {
    const present = exitKindsPresent(group);
    const peer = data.stages.find((s) => s.stageId === group.peerId);
    if (!peer) return null;

    const peerActions = peer.actions ?? [];
    const options: {
      kind: ExitKind;
      label: string;
      onSelect: () => void;
    }[] = [];

    if (
      present.has('qualification') &&
      peerActions.includes('ReplaceQualificationRules')
    ) {
      options.push({
        kind: 'qualification',
        label: t('fiche.exitKind.qualification'),
        onSelect: () => openExitEdit('qualification', peer),
      });
    }
    if (
      present.has('progression') &&
      peerActions.includes('ReplaceProgressionRules')
    ) {
      options.push({
        kind: 'progression',
        label: t('fiche.exitKind.progression'),
        onSelect: () => openExitEdit('progression', peer),
      });
    }

    if (options.length === 0) return null;

    return (
      <ExitKindMenu label={t('fiche.editExits')} options={options} />
    );
  };

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
    const n = resolvePlacesN(stage);
    if (n != null && n > 0) {
      heroFacts.push({
        icon: <LayersIcon size="sm" />,
        value: n,
        label: t('fiche.stat.places', { count: n }),
      });
    }
  }

  const overflowItems: OverflowItem[] = [];
  if (canEditDraw && !showTirage) {
    overflowItems.push({
      id: 'configure-draw',
      label: t('fiche.configureDraw'),
      onSelect: () => setEdit('tirage'),
    });
  }
  {
    const addExitOptions = exitEditOptions('add');
    if (addExitOptions.length === 1) {
      const only = addExitOptions[0]!;
      overflowItems.push({
        id: 'add-exit',
        label: t('fiche.addExit'),
        onSelect: only.onSelect,
      });
    } else if (addExitOptions.length > 1) {
      overflowItems.push({
        id: 'add-exit',
        label: t('fiche.addExit'),
        submenu: addExitOptions.map((option) => ({
          id: `add-exit-${option.kind}`,
          label: option.label,
          onSelect: option.onSelect,
        })),
      });
    }
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

  const sortiesEditControl = (() => {
    if (!hasExits) return undefined;
    const options = exitEditOptions();
    if (options.length === 0) return undefined;
    return (
      <ExitKindMenu label={t('fiche.editExits')} options={options} />
    );
  })();

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
            backLabel={t('fiche.menuBack')}
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
            title={t('fiche.tiles.population')}
            icon={<ArrowDownIcon size="md" />}
          >
            <RootEntriesRail
              stage={stage}
              entries={data.participants.entries}
              sourcesConfiguredVolume={populationFeedVolume}
              canCompose={canCompose}
              onEditCompose={() => openCompose(false)}
              sources={
                feedGroups.length > 0 ? (
                  <FluxGroupList
                    groups={feedGroups}
                    onOpenPeer={onSelectStage}
                    teamsLabel={teamsLabel}
                    renderGroupAction={renderAvalSourceAction}
                  />
                ) : undefined
              }
            />
          </FluxRail>

          <div className="structure-phase-hero__center">
            <div className="structure-schematic-viewport">
              <div className="structure-schematic-viewport__scale">
                {schematicQuery.data ? (
                  <PhaseSchematic
                    schematic={schematicQuery.data}
                    terminal={!hasExits}
                    cupRoundCount={stage.roundCount}
                  />
                ) : schematicQuery.isError ? (
                  <p className="structure-panel__muted">
                    {t('fiche.occupantsUnavailable')}
                  </p>
                ) : (
                  <div className="structure-schematic-loading">
                    <LoadingState
                      size="region"
                      label={t('fiche.schematicLoading')}
                    />
                  </div>
                )}
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
            {showTirage ||
            (data.readiness.readyForDraw && populationCount > 0) ? (
              <div className="structure-phase-hero__draw">
                <button
                  type="button"
                  className="ds-btn ds-btn--primary"
                  onClick={() => setDrawWorkflowOpen(true)}
                >
                  <DrawPendingIcon size="sm" />
                  <span>
                    {data.readiness.readyForDraw && populationCount > 0
                      ? t('fiche.performDraw')
                      : t('fiche.openDrawWorkflow')}
                  </span>
                </button>
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
                  editControl={sortiesEditControl}
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
                  editControl={
                    canEditPlacement ? (
                      <Tooltip content={t('fiche.edit')}>
                        <button
                          type="button"
                          className={compactIcon}
                          aria-label={t('fiche.edit')}
                          onClick={() => setEdit('placement')}
                        >
                          <PencilIcon size="sm" />
                        </button>
                      </Tooltip>
                    ) : undefined
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
          >
            {tiragePrimary ? (
              <p className="structure-panel__muted">
                {t(`fiche.tirage.summary.${tiragePrimary}`)}
              </p>
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
        stage={rulesEditStage ?? stage}
        open={edit === 'qualification'}
        onClose={closeRulesEdit}
        openedFromDestinationStageId={
          rulesEditStage != null && rulesEditStage.stageId !== stage.stageId
            ? stage.stageId
            : null
        }
      />
      <ProgressionRulesDialog
        data={data}
        stage={rulesEditStage ?? stage}
        open={edit === 'progression'}
        onClose={closeRulesEdit}
        openedFromDestinationStageId={
          rulesEditStage != null && rulesEditStage.stageId !== stage.stageId
            ? stage.stageId
            : null
        }
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
      <StructureDrawDialog
        competitionId={data.competitionId}
        stage={stage}
        open={drawWorkflowOpen}
        onClose={() => setDrawWorkflowOpen(false)}
      />
      <StructureCompositionDialog
        open={composeOpen}
        onClose={() => {
          setComposeOpen(false);
          setComposeFocusSearch(false);
        }}
        competitionId={data.competitionId}
        stage={stage}
        entries={data.participants.entries}
        reservedFromFeeds={populationFeedVolume}
        focusSearch={composeFocusSearch}
      />
      <TieFormatDialog
        competitionId={data.competitionId}
        stage={stage}
        open={edit === 'confrontation'}
        onClose={() => setEdit(null)}
      />
      <MatchRulesDialog
        data={data}
        stage={stage}
        open={edit === 'matchs' || edit === 'rebind-match'}
        onClose={() => setEdit(null)}
      />
      <StandingRulesDialog
        data={data}
        stage={stage}
        open={edit === 'classement' || edit === 'rebind-standing'}
        onClose={() => setEdit(null)}
      />
    </section>
  );
}
