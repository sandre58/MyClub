// Population (Équipes) rail — affectation list + cardinality meter.
import { useMemo, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  EmptySelectionIcon,
  PencilIcon,
  PlusIcon,
  StructureIssueIcon,
} from '../../design-system/icons/contentIcons';
import { TeamCrest } from '../../design-system/TeamCrest';
import { EmptyState } from '../../ui';
import type { StructureEntry, StructureStageHubSummary } from '../../types';
import { resolvePlacesN } from './structurePlaces';
import { OccupancyMeter, occupancyMeterState } from './OccupancyMeter';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

const EMPTY_ENTRY_IDS: string[] = [];

/** Cardinality gauge — expected Population vs Places N; shortfall = danger. */
export function CompositionMeter({
  entries,
  places,
  t,
}: {
  entries: number;
  places: number | null;
  t: (key: string, opts?: Record<string, unknown>) => string;
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
  const assemblyClass = [
    'structure-assembly',
    gap < 0 ? 'structure-assembly--deficit' : gap > 0 ? 'structure-assembly--over' : null,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <OccupancyMeter
      current={entries}
      capacity={places}
      state={state}
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
            <p className="structure-assembly__gap" role="status">
              <span className="structure-assembly__gap-icon" aria-hidden="true">
                <StructureIssueIcon size="sm" />
              </span>
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
  const composedIds = stage.affectationEntryIds ?? EMPTY_ENTRY_IDS;
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
        <CompositionMeter entries={meterEntries} places={n} t={t} />
      </div>
    </div>
  );
}
