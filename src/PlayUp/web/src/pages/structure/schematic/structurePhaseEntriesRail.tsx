// Population (Équipes) rail — affectation list + cardinality meter.
import { useMemo, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Chip, type ChipTone } from '../../../design-system/components/Chip';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  EmptySelectionIcon,
  PencilIcon,
  PlusIcon,
  LossIcon,
  WinIcon,
} from '../../../design-system/icons/contentIcons';
import { StructureDiagnosticIssueIcon } from '../issues/StructureDiagnosticIssueIcon';
import { EntryCell } from '../../../design-system/components/EntryCell';
import { entryCellListItemClassName } from '../../../design-system/components/entryCellListItemClassName';
import { EmptyState } from '../../../ui';
import type {
  StructureEntry,
  StructureFormatKind,
  StructureStageHubSummary,
} from '../../../types';
import {
  type EntryPlaceProjection,
  isEntryPlacedInPhase,
  primaryPlaceLabel,
  showPerEntryPlacement,
} from '../draw/structureEntryPlacementProjection';
import { resolvePlacesN } from '../composition/structurePlaces';
import { structureDiagnosticPresentation } from '../issues/structureDiagnosticPresentationPolicy';
import { OccupancyMeter } from '../composition/OccupancyMeter';
import { listComposedAffectationEntries } from '../composition/composedAffectationEntries';
import { occupancyMeterState } from '../composition/occupancyMeterMath';
import {
  isDraftLikeStageStatus,
  isSuspendedStageStatus,
} from '../fiche/structureOperationalMeterPresentation';
import {
  listEngagedCompositionEntries,
  resolveEngagedPoolResolution,
  type CompositionTileMode,
} from '../composition/compositionTilePresentation';
import type {
  CompositionTeamProvenance,
  CompositionTeamRow,
} from '../composition/compositionTeamsInboundRows';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

function compositionTeamProvenanceBadge(
  provenance: CompositionTeamProvenance,
): ReactNode {
  const tone: ChipTone = provenance.chipTone ?? 'soft';
  const outcome = provenance.progressionOutcome;
  return (
    <Tooltip content={provenance.full}>
      <Chip
        tone={tone}
        className="structure-entries__team-provenance-badge"
        aria-label={provenance.full}
      >
        {outcome ? (
          <span
            className="structure-entries__team-provenance-badge-inner"
            aria-hidden={false}
          >
            {outcome === 'Loser' ? (
              <LossIcon
                size="sm"
                className="structure-entries__team-provenance-outcome-icon"
                aria-hidden
              />
            ) : (
              <WinIcon
                size="sm"
                className="structure-entries__team-provenance-outcome-icon"
                aria-hidden
              />
            )}
            <span>{provenance.badge}</span>
          </span>
        ) : (
          provenance.badge
        )}
      </Chip>
    </Tooltip>
  );
}

/** Cardinality gauge — expected Population vs Places N (D5 phase-aware chrome). */
export function CompositionMeter({
  entries,
  places,
  t,
  stage,
}: {
  entries: number;
  places: number | null;
  t: (key: string, opts?: Record<string, unknown>) => string;
  stage?: import('../../../types').StructureStageHubSummary;
}) {
  if (places == null || places <= 0) {
    return (
      <div className="structure-assembly" aria-label={t('entries.meterAria')}>
        <div className="structure-assembly__row">
          <span>{t('entries.meter.population')}</span>
          <strong>{entries}</strong>
        </div>
        <div className="structure-assembly__row">
          <span>{t('entries.meter.places')}</span>
          <strong>—</strong>
        </div>
      </div>
    );
  }

  const gap = entries - places;
  const state = occupancyMeterState(entries, places);
  const gapDiagnosticTone =
    gap < 0
      ? structureDiagnosticPresentation('PopulationShortfall', {
          formatKind: stage?.formatKind,
          stageStatus: stage?.status,
          affectationEntryCount: stage?.affectationEntryCount ?? 0,
        }).tone
      : gap > 0
        ? structureDiagnosticPresentation('OverCapacityEdge', {
            formatKind: stage?.formatKind,
            stageStatus: stage?.status,
          }).tone
        : null;
  const shortfallTone = gap < 0 ? gapDiagnosticTone : null;
  const showDeficitChrome = gap < 0 && shortfallTone === 'danger';
  const draftIncomplete = gap < 0 && isDraftLikeStageStatus(stage?.status);
  const pausedPopulationGap = gap < 0 && isSuspendedStageStatus(stage?.status);
  const pausedPopulationSurplus =
    gap > 0 && isSuspendedStageStatus(stage?.status);
  const assemblyClass = [
    'structure-assembly',
    showDeficitChrome ? 'structure-assembly--deficit' : null,
    gap > 0 ? 'structure-assembly--over' : null,
    draftIncomplete ? 'structure-assembly--incomplete' : null,
    pausedPopulationGap || pausedPopulationSurplus
      ? 'structure-assembly--paused'
      : null,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <OccupancyMeter
      current={entries}
      capacity={places}
      state={state}
      fillVariant={draftIncomplete ? 'brand' : 'default'}
      ariaLabel={t('entries.meterAria')}
      className={assemblyClass}
      header={
        <div className="structure-assembly__row">
          <span>{t('entries.meter.population')}</span>
          <strong>{entries}</strong>
        </div>
      }
      footer={
        <>
          <div className="structure-assembly__row">
            <span>{t('entries.meter.places')}</span>
            <strong>{places}</strong>
          </div>
          {gap !== 0 ? (
            <p
              className={
                draftIncomplete
                  ? 'structure-assembly__gap structure-assembly__gap--expected'
                  : pausedPopulationGap || pausedPopulationSurplus
                    ? 'structure-assembly__gap structure-assembly__gap--paused'
                    : 'structure-assembly__gap'
              }
              role="status"
            >
              {!draftIncomplete &&
              !pausedPopulationGap &&
              !pausedPopulationSurplus &&
              gapDiagnosticTone != null ? (
                <span
                  className="structure-assembly__gap-icon"
                  aria-hidden="true"
                >
                  <StructureDiagnosticIssueIcon
                    tone={gapDiagnosticTone}
                    size="sm"
                  />
                </span>
              ) : null}
              {gap < 0
                ? pausedPopulationGap
                  ? t('entries.meter.shortfallPaused', { count: Math.abs(gap) })
                  : t('entries.meter.shortfall', { count: Math.abs(gap) })
                : pausedPopulationSurplus
                  ? t('entries.meter.surplusPaused', { count: gap })
                  : t('entries.meter.surplus', { count: gap })}
            </p>
          ) : null}
        </>
      }
    />
  );
}
export function RootEntriesRail({
  stage,
  entries,
  sources,
  sourcesConfiguredVolume = 0,
  canCompose,
  onEditCompose,
  variant = 'default',
  formatKind,
  placedEntryIds,
  entryPlaceProjections,
  schematicReady = false,
  compositionTileMode = 'prep',
  entryProvenanceByEntryId,
  compositionTeamRows = null,
  suppressRuntimeEmptyPresentation = false,
}: {
  stage: StructureStageHubSummary;
  entries: readonly StructureEntry[];
  /** B2 — inbound Qualif/Prog rules (same rail as Affectation teams). */
  sources?: ReactNode;
  /** Expected volume from inbound rules (promised Entries, not yet necessarily in CompositionEntries). */
  sourcesConfiguredVolume?: number;
  canCompose?: boolean;
  onEditCompose?: () => void;
  variant?: 'default' | 'composition-band';
  formatKind?: StructureFormatKind | null;
  placedEntryIds?: ReadonlySet<string>;
  entryPlaceProjections?: ReadonlyMap<string, EntryPlaceProjection>;
  schematicReady?: boolean;
  compositionTileMode?: CompositionTileMode;
  entryProvenanceByEntryId?: ReadonlyMap<string, CompositionTeamProvenance>;
  compositionTeamRows?: CompositionTeamRow[] | null;
  /** Runtime empty / undetermined rendered by Composition band header zone. */
  suppressRuntimeEmptyPresentation?: boolean;
}) {
  const { t } = useTranslation('structure');
  const fromAffectation = stage.affectationEntryCount ?? 0;
  const fromFeeds = sourcesConfiguredVolume;
  const n = resolvePlacesN(stage);
  const runtimeMode = compositionTileMode !== 'prep';
  const ineligible = runtimeMode
    ? (stage.compositionIneligibleCount ?? 0)
    : (stage.affectationIneligibleCount ?? 0);
  const previewNames = runtimeMode
    ? (stage.compositionPreviewNames ?? [])
    : (stage.affectationPreviewNames ?? []);
  // U1 — Population gauge always covers authoring alimentations (never runtime membership).
  const meterEntries = fromAffectation + fromFeeds;
  const composed = useMemo(
    () =>
      runtimeMode
        ? listEngagedCompositionEntries(stage, entries)
        : listComposedAffectationEntries(stage, entries),
    [stage, entries, runtimeMode],
  );
  const engagedPoolUndetermined =
    runtimeMode && resolveEngagedPoolResolution(stage).kind === 'undetermined';
  const perEntryPlacement = showPerEntryPlacement(
    formatKind ?? stage.formatKind,
  );
  const placedIds = placedEntryIds ?? new Set<string>();

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

  const teamListClass = [
    'structure-entries__teams',
    variant === 'composition-band' ? 'structure-entries__teams--band' : null,
  ]
    .filter(Boolean)
    .join(' ');

  const showPlacementChrome =
    variant !== 'composition-band' && perEntryPlacement;

  const renderTeamCell = (
    entry: StructureEntry,
    provenance?: CompositionTeamProvenance | null,
  ) => {
    const placed = showPlacementChrome
      ? isEntryPlacedInPhase(
          entry.entryId,
          placedIds,
          formatKind ?? stage.formatKind,
          schematicReady,
        )
      : false;
    const showUnplacedOpacity =
      showPlacementChrome &&
      schematicReady &&
      entry.status === 'Active' &&
      !placed;
    const provenanceLabel =
      provenance ?? entryProvenanceByEntryId?.get(entry.entryId) ?? null;
    const projection = entryPlaceProjections?.get(entry.entryId);
    const placeLabel = primaryPlaceLabel(projection);
    const placedTooltip =
      placed && placeLabel
        ? t('entries.entryPlacedTooltip', { place: placeLabel })
        : placed
          ? t('entries.entryPlacedStatus')
          : null;
    return (
      <li
        key={entry.entryId}
        className={entryCellListItemClassName({
          ineligible: entry.status !== 'Active',
          unplaced: showUnplacedOpacity,
        })}
        aria-label={
          showPlacementChrome && schematicReady && entry.status === 'Active'
            ? placed
              ? t('entries.entryPlacedAria', { name: entry.displayName })
              : t('entries.entryNotPlacedAria', { name: entry.displayName })
            : undefined
        }
      >
        <EntryCell
          displayName={entry.displayName}
          logoMediaId={entry.logoMediaId}
          primaryColor={entry.primaryColor}
          ineligible={entry.status !== 'Active'}
          ineligibleTooltip={
            entry.status === 'Withdrawn'
              ? t('composition.withdrawnFromCompetitionTip')
              : undefined
          }
          ineligibleLabel={
            entry.status !== 'Active' && entry.status !== 'Withdrawn'
              ? t('composition.noLongerEligible')
              : undefined
          }
          placed={placed}
          placedTooltip={
            placedTooltip ?? (placed ? t('entries.entryPlacedStatus') : null)
          }
          trailing={
            provenanceLabel
              ? compositionTeamProvenanceBadge(provenanceLabel)
              : undefined
          }
        />
      </li>
    );
  };

  if (variant === 'composition-band') {
    const bandStateClass = runtimeMode
      ? null
      : danger
        ? 'structure-entries--danger'
        : warn
          ? 'structure-entries--warn'
          : soft
            ? 'structure-entries--soft'
            : null;
    return (
      <div
        className={[
          'structure-entries',
          'structure-entries--composition-band',
          runtimeMode ? 'structure-entries--engaged-pool' : null,
          bandStateClass,
        ]
          .filter(Boolean)
          .join(' ')}
      >
        {compositionTeamRows && compositionTeamRows.length > 0 ? (
          <ul className={teamListClass}>
            {compositionTeamRows.map((row) =>
              row.kind === 'unresolved' ? (
                <li key={row.key} className="structure-entries__team">
                  <EntryCell
                    displayName=""
                    pending
                    pendingLabel={t('composition.qualifiers.unresolved')}
                    trailing={compositionTeamProvenanceBadge(row.provenance)}
                  />
                </li>
              ) : (
                renderTeamCell(row.entry, row.provenance)
              ),
            )}
          </ul>
        ) : engagedPoolUndetermined ? (
          suppressRuntimeEmptyPresentation ? null : (
            <EmptyState variant="idle" icon={<EmptySelectionIcon size="lg" />}>
              <p className="structure-entries__engaged-undetermined-title">
                {t('composition.tile.engagedPoolUndeterminedTitle')}
              </p>
              <p className="structure-entries__engaged-undetermined-body">
                {t('composition.tile.engagedPoolUndeterminedBody')}
              </p>
            </EmptyState>
          )
        ) : composed.length > 0 ? (
          <ul className={teamListClass}>
            {composed.map((entry) => renderTeamCell(entry))}
          </ul>
        ) : previewNames.length > 0 ? (
          <p className="structure-entries__preview" role="status">
            {previewNames.join(' · ')}
          </p>
        ) : ineligible > 0 ? (
          <p className="structure-entries__anomaly" role="status">
            {t('entries.ineligibleOverlay', { count: ineligible })}
          </p>
        ) : suppressRuntimeEmptyPresentation && runtimeMode ? null : (
          <EmptyState variant="idle" icon={<EmptySelectionIcon size="lg" />}>
            {runtimeMode
              ? t('composition.tile.engagedPoolEmpty')
              : t('entries.emptyCompositionExtra')}
          </EmptyState>
        )}
      </div>
    );
  }

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
              <ul className={teamListClass}>
                {composed.map((entry) => renderTeamCell(entry))}
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
          t={t}
          stage={stage}
        />
      </div>
    </div>
  );
}
