import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  useEffect,
  useId,
  useState,
  type FormEvent,
  type ReactNode,
} from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import {
  configureOrganisationStructure,
  fetchOrganisationView,
  setCompetitionSchedule,
  updateCompetitionPresentation,
} from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { PageHead } from '../design-system/components/PageHead';
import { TextInput } from '../design-system/components/TextInput';
import { LogoMediaField } from './LogoMediaField';
import { SHORT_NAME_MAX_LENGTH } from './deriveShortName';
import {
  CheckIcon,
  RegulationIcon,
  StructureIcon,
  TeamsIcon,
} from '../design-system/icons/overviewIcons';
import {
  attentionSourceLabel,
  competitionStatusLabel,
  matchGenerationFormatLabel,
  structureFormatKindLabel,
} from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import {
  EmptyState,
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
  StageStatusBadge,
} from '../ui';
import {
  type MatchGenerationFormat,
  type OrganisationView,
  type StructureFormatKind,
} from '../types';
import { invalidateAfterOrganisationMutation } from './organisationInvalidation';
import { RegulationEditorDialog } from './RegulationEditorDialog';
import './organisation.css';

type OrganisationEditor = null | 'regulation' | 'structure';

/**
 * Organisation Hub — GET /competitions/{id}/organisation + Slice 2 mutations.
 * Surfaces V3: bandeau · strip préparation · Équipes/Règlement · Structure.
 * Hub is read-only; editing happens in modal dialogs.
 */
export function OrganisationPage() {
  const { competitionId = '' } = useParams();

  const query = useQuery({
    queryKey: queryKeys.competitions.organisation(competitionId),
    queryFn: () => fetchOrganisationView(competitionId),
    enabled: competitionId.length > 0,
  });

  return (
    <main id="main" className="page page--organisation">
      {query.isPending && !query.data && <LoadingState />}
      {query.isError && !query.data && <ErrorState error={query.error} />}
      {query.data && <OrganisationViewPanel data={query.data} />}
    </main>
  );
}

function OrganisationViewPanel({ data }: { data: OrganisationView }) {
  const { t } = useTranslation('organisation');
  const can = (action: string) => data.actions.includes(action);
  const [editor, setEditor] = useState<OrganisationEditor>(null);

  const canReplace = can('ReplaceRegulation');
  const canConfigure = can('ConfigureStructure');

  const closeEditor = () => setEditor(null);

  return (
    <div className="organisation">
      <PageHead title={t('title')} />

      <ContextBand data={data} />
      <IdentitySection data={data} />
      <PreparationStrip data={data} onOpenEditor={(next) => setEditor(next)} />

      <div className="organisation__mid">
        <TeamsFactSection data={data} />
        <RegulationSection
          data={data}
          canReplace={canReplace}
          onEdit={() => setEditor('regulation')}
        />
      </div>

      <StructureSection
        data={data}
        canConfigure={canConfigure}
        onConfigure={() => setEditor('structure')}
      />

      <RegulationEditorDialog
        data={data}
        open={editor === 'regulation'}
        onClose={closeEditor}
      />
      <StructureEditorDialog
        data={data}
        open={editor === 'structure'}
        onClose={closeEditor}
      />
    </div>
  );
}

function ContextBand({ data }: { data: OrganisationView }) {
  const { t } = useTranslation('organisation');
  const formatText = data.format.kind
    ? structureFormatKindLabel(data.format.kind)
    : t('structure.formatNotConfigured');
  const phaseCount = data.format.primaryStageId ? 1 : 0;

  return (
    <ul className="organisation-band" aria-label={data.name}>
      <li className="organisation-band__chip organisation-band__chip--status">
        <CheckIcon size="sm" aria-hidden="true" />
        {competitionStatusLabel(data.status)}
      </li>
      <li className="organisation-band__chip">
        <StructureIcon size="sm" aria-hidden="true" />
        {formatText}
      </li>
      <li className="organisation-band__chip">
        <TeamsIcon size="sm" aria-hidden="true" />
        {t('band.teams', { count: data.participants.activeCount })}
      </li>
      <li className="organisation-band__chip organisation-band__chip--muted">
        {t('band.phases', { count: phaseCount })}
      </li>
    </ul>
  );
}

function IdentitySection({ data }: { data: OrganisationView }) {
  const { t } = useTranslation('organisation');
  const queryClient = useQueryClient();
  const shortNameId = useId();
  const [shortName, setShortName] = useState(data.shortName ?? '');
  const [logoMediaId, setLogoMediaId] = useState<string | null>(
    data.logoMediaId ?? null,
  );
  const [scheduledStart, setScheduledStart] = useState(
    data.scheduledStart?.slice(0, 10) ?? '',
  );
  const [scheduledEnd, setScheduledEnd] = useState(
    data.scheduledEnd?.slice(0, 10) ?? '',
  );

  useEffect(() => {
    setShortName(data.shortName ?? '');
    setLogoMediaId(data.logoMediaId ?? null);
    setScheduledStart(data.scheduledStart?.slice(0, 10) ?? '');
    setScheduledEnd(data.scheduledEnd?.slice(0, 10) ?? '');
  }, [
    data.shortName,
    data.logoMediaId,
    data.scheduledStart,
    data.scheduledEnd,
  ]);

  const presentationMutation = useMutation({
    mutationFn: () =>
      updateCompetitionPresentation(data.competitionId, {
        shortName: shortName.trim() || null,
        logoMediaId,
      }),
    onSuccess: async () => {
      await invalidateAfterOrganisationMutation(
        queryClient,
        data.competitionId,
      );
    },
  });

  const scheduleMutation = useMutation({
    mutationFn: () =>
      setCompetitionSchedule(data.competitionId, {
        scheduledStart: scheduledStart
          ? new Date(`${scheduledStart}T00:00:00.000Z`).toISOString()
          : null,
        scheduledEnd: scheduledEnd
          ? new Date(`${scheduledEnd}T00:00:00.000Z`).toISOString()
          : null,
      }),
    onSuccess: async () => {
      await invalidateAfterOrganisationMutation(
        queryClient,
        data.competitionId,
      );
    },
  });

  return (
    <section className="ds-panel" aria-labelledby="identity-heading">
      <PanelHead id="identity-heading" icon={<TeamsIcon size="md" />}>
        {t('identity.heading')}
      </PanelHead>
      <div className="organisation-identity">
        <form
          className="ds-form"
          data-density="comfortable"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            presentationMutation.mutate();
          }}
        >
          <Field
            label={t('identity.shortName')}
            htmlFor={shortNameId}
            width="sm"
            counter={`${shortName.length}/${SHORT_NAME_MAX_LENGTH}`}
          >
            <TextInput
              id={shortNameId}
              value={shortName}
              maxLength={SHORT_NAME_MAX_LENGTH}
              disabled={presentationMutation.isPending}
              allowClear
              onChange={(event) => setShortName(event.target.value)}
            />
          </Field>
          <LogoMediaField
            name={data.name}
            value={logoMediaId}
            onChange={setLogoMediaId}
            disabled={presentationMutation.isPending}
            label={t('identity.logo')}
          />
          <button
            type="submit"
            className="ds-btn ds-btn--ghost"
            disabled={presentationMutation.isPending}
          >
            {presentationMutation.isPending ? (
              <PendingLabel>{t('working')}</PendingLabel>
            ) : (
              t('identity.savePresentation')
            )}
          </button>
          {presentationMutation.isError && (
            <MutationError error={presentationMutation.error} />
          )}
        </form>
        <form
          className="form form--inline"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            scheduleMutation.mutate();
          }}
        >
          <label className="field">
            {t('identity.scheduledStart')}
            <input
              type="date"
              value={scheduledStart}
              onChange={(event) => setScheduledStart(event.target.value)}
              disabled={scheduleMutation.isPending}
            />
          </label>
          <label className="field">
            {t('identity.scheduledEnd')}
            <input
              type="date"
              value={scheduledEnd}
              onChange={(event) => setScheduledEnd(event.target.value)}
              disabled={scheduleMutation.isPending}
            />
          </label>
          <button
            type="submit"
            className="ds-btn ds-btn--ghost"
            disabled={scheduleMutation.isPending}
          >
            {scheduleMutation.isPending ? (
              <PendingLabel>{t('working')}</PendingLabel>
            ) : (
              t('identity.saveSchedule')
            )}
          </button>
          {scheduleMutation.isError && (
            <MutationError error={scheduleMutation.error} />
          )}
        </form>
      </div>
    </section>
  );
}

function PanelHead({
  id,
  icon,
  children,
}: {
  id: string;
  icon: ReactNode;
  children: ReactNode;
}) {
  return (
    <h2 id={id} className="organisation-panel__head">
      <span className="organisation-panel__icon" aria-hidden="true">
        {icon}
      </span>
      {children}
    </h2>
  );
}

function PreparationStrip({
  data,
  onOpenEditor,
}: {
  data: OrganisationView;
  onOpenEditor: (editor: Exclude<OrganisationEditor, null>) => void;
}) {
  const { t } = useTranslation('organisation');
  const readiness = data.readiness;
  const formatKind = data.format.kind;
  const needsDraw = formatKind === 'Groups' || formatKind === 'Cup';
  const readyToMaterialize = readiness.readyForMaterialization;
  const blockers = readiness.blockers;
  const openCount = blockers.length;

  if (readyToMaterialize) {
    const isCup = formatKind === 'Cup';
    return (
      <section
        className="organisation-strip organisation-strip--ready"
        aria-labelledby="readiness-heading"
      >
        <div className="organisation-strip__head">
          <h2 id="readiness-heading" className="organisation-strip__title">
            <CheckIcon size="sm" aria-hidden="true" />
            {isCup
              ? t('readiness.readyForCupSkeleton')
              : t('readiness.readyForMaterialization')}
          </h2>
        </div>
        <div className="organisation-strip__actions">
          <p className="organisation-panel__muted">
            {isCup
              ? t('readiness.cupSkeletonHint')
              : t('readiness.materializeHint')}
          </p>
          <Link
            className="organisation-link"
            to={`/competitions/${data.competitionId}`}
          >
            {isCup
              ? t('readiness.goToOverviewCupSkeleton')
              : t('readiness.goToOverviewMaterialize')}
            <span aria-hidden="true">→</span>
          </Link>
        </div>
      </section>
    );
  }

  if (openCount === 0 && needsDraw && readiness.readyForDraw) {
    return (
      <section
        className="organisation-strip organisation-strip--ready"
        aria-labelledby="readiness-heading"
      >
        <div className="organisation-strip__head">
          <h2 id="readiness-heading" className="organisation-strip__title">
            <CheckIcon size="sm" aria-hidden="true" />
            {t('readiness.readyForDraw')}
          </h2>
        </div>
      </section>
    );
  }

  if (openCount === 0) {
    return null;
  }

  return (
    <section className="organisation-strip" aria-labelledby="readiness-heading">
      <div className="organisation-strip__head">
        <h2 id="readiness-heading" className="organisation-strip__title">
          {t('readiness.openItems', { count: openCount })}
        </h2>
      </div>
      <ul className="organisation-strip__actions-list">
        {blockers.map((code) => {
          const editor = editorForBlocker(code);
          const label = attentionSourceLabel(code);
          const teamsHref =
            code === 'InsufficientParticipants'
              ? `/competitions/${data.competitionId}/teams`
              : null;
          return (
            <li key={code}>
              {teamsHref ? (
                <Link className="organisation-strip__action" to={teamsHref}>
                  <span aria-hidden="true">•</span>
                  {label}
                  <span aria-hidden="true">→</span>
                </Link>
              ) : editor ? (
                <button
                  type="button"
                  className="organisation-strip__action"
                  onClick={() => onOpenEditor(editor)}
                >
                  <span aria-hidden="true">•</span>
                  {label}
                  <span aria-hidden="true">→</span>
                </button>
              ) : (
                <span className="organisation-strip__action organisation-strip__action--static">
                  <span aria-hidden="true">•</span>
                  {label}
                </span>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function editorForBlocker(
  code: string,
): Exclude<OrganisationEditor, null> | null {
  if (code === 'MissingStage') {
    return 'structure';
  }
  return null;
}

function TeamsFactSection({ data }: { data: OrganisationView }) {
  const { t } = useTranslation('organisation');
  const teamsHref = `/competitions/${data.competitionId}/teams`;
  const activeCount = data.participants.activeCount;
  const belowMinimum = activeCount < data.regulation.minimumTeams;
  const summaryHint = belowMinimum
    ? t('participants.summaryIncomplete', {
        count: activeCount,
        minimum: data.regulation.minimumTeams,
      })
    : t('participants.summaryComplete', { count: activeCount });

  return (
    <section className="ds-panel" aria-labelledby="participants-heading">
      <PanelHead id="participants-heading" icon={<TeamsIcon size="md" />}>
        {t('participants.heading')}
      </PanelHead>
      <p
        className={`organisation-panel__summary${belowMinimum ? ' organisation-panel__summary--warn' : ''}`}
      >
        {summaryHint}
      </p>
      <p className="organisation-panel__footer">
        <Link className="organisation-link" to={teamsHref}>
          {t('participants.openTeams')}
          <span aria-hidden="true">→</span>
        </Link>
      </p>
    </section>
  );
}

function RegulationSection({
  data,
  canReplace,
  onEdit,
}: {
  data: OrganisationView;
  canReplace: boolean;
  onEdit: () => void;
}) {
  const { t } = useTranslation('organisation');
  const regulation = data.regulation;

  return (
    <section className="ds-panel" aria-labelledby="regulation-heading">
      <PanelHead id="regulation-heading" icon={<RegulationIcon size="md" />}>
        {t('regulation.heading')}
      </PanelHead>

      <p className="organisation-status organisation-status--ok">
        <CheckIcon size="sm" aria-hidden="true" />
        {t('regulation.configured')}
      </p>

      <ul className="organisation-points" aria-label={t('regulation.points')}>
        <li className="organisation-points__item organisation-points__item--win">
          <span className="organisation-points__value">
            {t('regulation.pointsValue', { count: regulation.winPoints })}
          </span>
          <span className="organisation-points__label">
            {t('regulation.win')}
          </span>
        </li>
        <li className="organisation-points__item organisation-points__item--draw">
          <span className="organisation-points__value">
            {t('regulation.pointsValue', { count: regulation.drawPoints })}
          </span>
          <span className="organisation-points__label">
            {t('regulation.draw')}
          </span>
        </li>
        <li className="organisation-points__item organisation-points__item--loss">
          <span className="organisation-points__value">
            {t('regulation.pointsValue', { count: regulation.lossPoints })}
          </span>
          <span className="organisation-points__label">
            {t('regulation.loss')}
          </span>
        </li>
      </ul>

      <ul className="organisation-meta">
        <li>
          {t('regulation.matchMeta', {
            periods: regulation.numberOfPeriods,
            minutes: regulation.durationPerPeriod,
          })}
        </li>
        <li>
          {t('regulation.teamsMeta', {
            min: regulation.minimumTeams,
            max: regulation.maximumTeams,
          })}
        </li>
        <li>
          {(regulation.allowedTypes ?? []).length === 0
            ? t('regulation.allowedTypesNone')
            : t('regulation.allowedTypesMeta', {
                types: (regulation.allowedTypes ?? [])
                  .map((type) => t(`regulation.type.${type}`))
                  .join(', '),
              })}
        </li>
      </ul>

      {canReplace && (
        <div className="organisation-panel__footer">
          <button
            type="button"
            className="organisation-action"
            onClick={onEdit}
          >
            {t('regulation.editAction')}
            <span aria-hidden="true">→</span>
          </button>
        </div>
      )}
    </section>
  );
}

function StructureSection({
  data,
  canConfigure,
  onConfigure,
}: {
  data: OrganisationView;
  canConfigure: boolean;
  onConfigure: () => void;
}) {
  const { t } = useTranslation('organisation');
  const formatKind = data.format.kind;
  const primaryStageId = data.format.primaryStageId;
  const stageName =
    data.format.primaryStageName ?? t('structure.primaryStageFallback');
  const formatLabel = formatKind
    ? structureFormatKindLabel(formatKind)
    : t('structure.formatNotConfigured');
  const phaseCount = primaryStageId ? 1 : 0;
  const drawLabel =
    formatKind === 'Championship'
      ? t('structure.drawNotRequired')
      : data.structure.hasDrawRules
        ? t('structure.drawConfigured', {
            pots: data.structure.numberOfPots ?? '—',
          })
        : t('structure.drawMissing');

  return (
    <section
      className="ds-panel organisation-structure"
      aria-labelledby="structure-heading"
    >
      <div className="organisation-structure__head">
        <PanelHead id="structure-heading" icon={<StructureIcon size="md" />}>
          {t('structure.heading')}
        </PanelHead>
        <p className="organisation-panel__muted">
          {t('structure.subtitle', {
            count: phaseCount,
            format: formatLabel,
          })}
        </p>
      </div>

      {primaryStageId ? (
        <article className="organisation-phase">
          <header className="organisation-phase__head">
            <div className="organisation-phase__titles">
              <h3 className="organisation-phase__title">{stageName}</h3>
              <ul className="organisation-phase__pills">
                <li className="organisation-phase__pill">{formatLabel}</li>
                {data.format.primaryStageStatus && (
                  <li className="organisation-phase__pill organisation-phase__pill--status">
                    <StageStatusBadge status={data.format.primaryStageStatus} />
                  </li>
                )}
              </ul>
            </div>
            <div className="organisation-phase__actions">
              <Link
                className="organisation-action"
                to={`/stages/${primaryStageId}`}
              >
                {t('structure.openStage')}
                <span aria-hidden="true">→</span>
              </Link>
              {canConfigure && (
                <button
                  type="button"
                  className="organisation-action"
                  onClick={onConfigure}
                >
                  {t('structure.editPhase')}
                  <span aria-hidden="true">→</span>
                </button>
              )}
            </div>
          </header>

          <dl className="organisation-phase__grid">
            <div className="organisation-phase__cell">
              <dt>{t('structure.format')}</dt>
              <dd>{formatLabel}</dd>
            </div>
            {(formatKind === 'Championship' || formatKind === 'Groups') && (
              <div className="organisation-phase__cell">
                <dt>{t('structure.matchGenerationFormat')}</dt>
                <dd>
                  {matchGenerationFormatLabel(
                    data.structure.matchGenerationFormat,
                  )}
                </dd>
              </div>
            )}
            <div className="organisation-phase__cell">
              <dt>{t('structure.composition')}</dt>
              <dd>
                {t('structure.compositionValue', {
                  groups: data.structure.groupCount,
                  teams: data.participants.occupyingCount,
                })}
              </dd>
            </div>
            <div className="organisation-phase__cell">
              <dt>{t('structure.calendar')}</dt>
              <dd>
                {formatKind === 'Swiss'
                  ? t('structure.swissCalendarValue', {
                      planned: data.structure.swissRoundCount ?? 0,
                      matchdays: data.structure.matchdayCount,
                      matches: data.readiness.attachedMatchCount,
                    })
                  : t('structure.calendarValue', {
                      matchdays: data.structure.matchdayCount,
                      slots: data.structure.slotCount,
                      matches: data.readiness.attachedMatchCount,
                    })}
              </dd>
            </div>
            <div className="organisation-phase__cell">
              <dt>{t('structure.draw')}</dt>
              <dd>{drawLabel}</dd>
            </div>
          </dl>
        </article>
      ) : (
        <EmptyState title={t('structure.emptyTitle')}>
          {t('structure.emptyBody')}
        </EmptyState>
      )}

      {canConfigure && (
        <div className="organisation-panel__footer organisation-panel__footer--center">
          <button
            type="button"
            className="organisation-action"
            onClick={onConfigure}
          >
            {t('structure.configureAction')}
            <span aria-hidden="true">→</span>
          </button>
        </div>
      )}
    </section>
  );
}

function StructureEditorDialog({
  data,
  open,
  onClose,
}: {
  data: OrganisationView;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('organisation');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const formId = useId();
  const [format, setFormat] = useState<StructureFormatKind>(
    data.format.kind ?? 'Championship',
  );
  const [stageName, setStageName] = useState('');
  const [matchdayCount, setMatchdayCount] = useState(
    Math.max(1, data.structure.matchdayCount || 1),
  );
  const [groupCount, setGroupCount] = useState(
    Math.max(1, data.structure.groupCount || 2),
  );
  const [participantsPerGroup, setParticipantsPerGroup] = useState(2);
  const [bracketSize, setBracketSize] = useState(
    Math.max(2, data.structure.slotCount || 4),
  );
  const [swissRoundCount, setSwissRoundCount] = useState(
    Math.max(1, data.structure.swissRoundCount || 3),
  );
  const [matchGenerationFormat, setMatchGenerationFormat] =
    useState<MatchGenerationFormat>(
      data.structure.matchGenerationFormat ?? 'SingleRoundRobin',
    );

  const mutation = useMutation({
    mutationFn: () =>
      configureOrganisationStructure(data.competitionId, {
        format,
        stageName: stageName.trim() || null,
        matchdayCount: format === 'Championship' ? matchdayCount : null,
        groupCount: format === 'Groups' ? groupCount : null,
        participantsPerGroup: format === 'Groups' ? participantsPerGroup : null,
        bracketSize: format === 'Cup' ? bracketSize : null,
        swissRoundCount: format === 'Swiss' ? swissRoundCount : null,
        matchGenerationFormat:
          format === 'Championship' || format === 'Groups'
            ? matchGenerationFormat
            : null,
      }),
    onSuccess: async () => {
      await invalidateAfterOrganisationMutation(
        queryClient,
        data.competitionId,
      );
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('structure.configureLegend')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      size="md"
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={mutation.isPending}
            onClick={onClose}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('structure.configuring')}</PendingLabel>
            ) : (
              t('structure.configure')
            )}
          </button>
        </>
      }
    >
      <form
        id={formId}
        className="form"
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          if (mutation.isPending) {
            return;
          }
          mutation.mutate();
        }}
      >
        <fieldset className="fieldset" disabled={mutation.isPending}>
          <legend className="fieldset__legend">
            {t('structure.configureLegend')}
          </legend>
          <label className="field">
            {t('structure.format')}
            <select
              value={format}
              onChange={(event) =>
                setFormat(event.target.value as StructureFormatKind)
              }
            >
              <option value="Championship">
                {structureFormatKindLabel('Championship')}
              </option>
              <option value="Groups">
                {structureFormatKindLabel('Groups')}
              </option>
              <option value="Cup">{structureFormatKindLabel('Cup')}</option>
              <option value="Swiss">{structureFormatKindLabel('Swiss')}</option>
            </select>
          </label>
          <label className="field">
            {t('structure.stageName')}
            <input
              value={stageName}
              onChange={(event) => setStageName(event.target.value)}
              placeholder={t('structure.stageNamePlaceholder')}
            />
          </label>
          {(format === 'Championship' || format === 'Groups') && (
            <label className="field">
              {t('structure.matchGenerationFormat')}
              <select
                value={matchGenerationFormat}
                onChange={(event) =>
                  setMatchGenerationFormat(
                    event.target.value as MatchGenerationFormat,
                  )
                }
                aria-describedby="match-generation-hint"
              >
                <option value="SingleRoundRobin">
                  {matchGenerationFormatLabel('SingleRoundRobin')}
                </option>
                <option value="DoubleRoundRobin">
                  {matchGenerationFormatLabel('DoubleRoundRobin')}
                </option>
              </select>
              <span id="match-generation-hint" className="caption">
                {t('structure.matchGenerationHint')}
              </span>
            </label>
          )}
          {format === 'Championship' && (
            <label className="field">
              {t('structure.matchdayCount')}
              <input
                type="number"
                min={1}
                value={matchdayCount}
                onChange={(event) =>
                  setMatchdayCount(Number(event.target.value) || 1)
                }
                required
              />
            </label>
          )}
          {format === 'Groups' && (
            <div className="form-row">
              <label className="field">
                {t('structure.groupCount')}
                <input
                  type="number"
                  min={1}
                  value={groupCount}
                  onChange={(event) =>
                    setGroupCount(Number(event.target.value) || 1)
                  }
                  required
                />
              </label>
              <label className="field">
                {t('structure.participantsPerGroup')}
                <input
                  type="number"
                  min={1}
                  value={participantsPerGroup}
                  onChange={(event) =>
                    setParticipantsPerGroup(Number(event.target.value) || 1)
                  }
                  required
                />
              </label>
            </div>
          )}
          {format === 'Cup' && (
            <label className="field">
              {t('structure.bracketSize')}
              <input
                type="number"
                min={2}
                value={bracketSize}
                onChange={(event) =>
                  setBracketSize(Number(event.target.value) || 2)
                }
                required
              />
            </label>
          )}
          {format === 'Swiss' && (
            <label className="field">
              {t('structure.swissRoundCount')}
              <input
                type="number"
                min={1}
                value={swissRoundCount}
                onChange={(event) =>
                  setSwissRoundCount(Number(event.target.value) || 1)
                }
                required
                aria-describedby="swiss-round-hint"
              />
              <span id="swiss-round-hint" className="caption">
                {t('structure.swissRoundHint')}
              </span>
            </label>
          )}
        </fieldset>
        <p className="caption">{t('structure.configureHint')}</p>
        {mutation.isError && <MutationError error={mutation.error} />}
      </form>
    </Dialog>
  );
}
