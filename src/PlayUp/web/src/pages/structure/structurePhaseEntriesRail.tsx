// Population (Équipes) rail — affectation list + cardinality meter.
import { useMemo, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  EmptySelectionIcon,
  PencilIcon,
  PlusIcon,
} from '../../design-system/icons/contentIcons';
import { StructureDiagnosticIssueIcon } from './StructureDiagnosticIssueIcon';
import {
  EntryCell,
  entryCellListItemClassName,
} from '../../design-system/components/EntryCell';
import { EmptyState } from '../../ui';
import type {
  StructureEntry,
  StructureFormatKind,
  StructureStageHubSummary,
} from '../../types';
import {
  type EntryPlaceProjection,
  isEntryPlacedInPhase,
  primaryPlaceLabel,
  showPerEntryPlacement,
} from './structureEntryPlacementProjection';
import { resolvePlacesN } from './structurePlaces';
import { structureDiagnosticPresentation } from './structureDiagnosticPresentationPolicy';
import { OccupancyMeter, occupancyMeterState } from './OccupancyMeter';
import { isDraftLikeStageStatus } from './structureOperationalMeterPresentation';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

const EMPTY_ENTRY_IDS: string[] = [];

export function listComposedAffectationEntries(
  stage: StructureStageHubSummary,
  entries: readonly StructureEntry[],
): StructureEntry[] {
  const composedIds = stage.affectationEntryIds ?? EMPTY_ENTRY_IDS;
  const byId = new Map(entries.map((entry) => [entry.entryId, entry]));
  return composedIds
    .map((id) => byId.get(id))
    .filter((entry): entry is StructureEntry => entry != null)
    .sort((a, b) =>
      a.displayName.localeCompare(b.displayName, undefined, {
        sensitivity: 'base',
      }),
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
  stage?: import('../../types').StructureStageHubSummary;
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
  const draftIncomplete =
    gap < 0 && isDraftLikeStageStatus(stage?.status);
  const assemblyClass = [
    'structure-assembly',
    showDeficitChrome ? 'structure-assembly--deficit' : null,
    gap > 0 ? 'structure-assembly--over' : null,
    draftIncomplete ? 'structure-assembly--incomplete' : null,
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
                  : 'structure-assembly__gap'
              }
              role="status"
            >
              {!draftIncomplete && gapDiagnosticTone != null ? (
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
                ? t('entries.meter.shortfall', { count: Math.abs(gap) })
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
}) {
  const { t } = useTranslation('structure');
  const fromAffectation = stage.affectationEntryCount ?? 0;
  const fromFeeds = sourcesConfiguredVolume;
  const n = resolvePlacesN(stage);
  const ineligible = stage.affectationIneligibleCount ?? 0;
  const previewNames = stage.affectationPreviewNames ?? [];
  // U1 — Population gauge always covers authoring alimentations (never runtime membership).
  const meterEntries = fromAffectation + fromFeeds;
  const composed = useMemo(
    () => listComposedAffectationEntries(stage, entries),
    [stage, entries],
  );
  const perEntryPlacement = showPerEntryPlacement(formatKind ?? stage.formatKind);
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

  const renderTeamCell = (entry: StructureEntry) => {
    const placed = isEntryPlacedInPhase(
      entry.entryId,
      placedIds,
      formatKind ?? stage.formatKind,
      schematicReady,
    );
    const showUnplacedOpacity =
      perEntryPlacement &&
      schematicReady &&
      entry.status === 'Active' &&
      !placed;
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
          perEntryPlacement && schematicReady && entry.status === 'Active'
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
          ineligibleLabel={t('composition.noLongerEligible')}
          placed={placed}
          placedTooltip={
            placedTooltip ?? (placed ? t('entries.entryPlacedStatus') : null)
          }
        />
      </li>
    );
  };

  if (variant === 'composition-band') {
    return (
      <div
        className={[
          'structure-entries',
          'structure-entries--composition-band',
          danger ? 'structure-entries--danger' : null,
          warn ? 'structure-entries--warn' : null,
          soft ? 'structure-entries--soft' : null,
        ]
          .filter(Boolean)
          .join(' ')}
      >
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
        ) : (
          <EmptyState
            variant="idle"
            icon={<EmptySelectionIcon size="lg" />}
          >
            {t('entries.emptyCompositionExtra')}
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
        <CompositionMeter entries={meterEntries} places={n} t={t} stage={stage} />
      </div>
    </div>
  );
}
