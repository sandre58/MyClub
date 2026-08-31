import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import {
  useEffect,
  useId,
  useState,
  type FormEvent,
  type ReactNode,
} from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import {
  addCompetitionEntry,
  configureOrganisationStructure,
  excludeCompetitionEntry,
  fetchOrganisationView,
  renameCompetitionEntry,
  replaceCompetitionRegulation,
  setCompetitionSchedule,
  updateCompetitionPresentation,
  updateEntryPresentation,
  withdrawCompetitionEntry,
} from '../api'
import { LogoMediaField } from '../design-system/LogoMediaField'
import { TeamCrest } from '../design-system/TeamCrest'
import {
  CheckIcon,
  RegulationIcon,
  StructureIcon,
  TeamsIcon,
} from '../design-system/icons/overviewIcons'
import {
  attentionSourceLabel,
  competitionStatusLabel,
  matchGenerationFormatLabel,
  structureFormatKindLabel,
} from '../i18n/enumLabels'
import { queryKeys } from '../queryKeys'
import {
  EmptyState,
  EntryStatusBadge,
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
  StageStatusBadge,
} from '../ui'
import {
  type DisciplinaryType,
  type MatchGenerationFormat,
  type OrganisationEntry,
  type OrganisationView,
  type ReplaceRegulationRequest,
  type StructureFormatKind,
} from '../types'
import './organisation.css'

type OrganisationEditor = null | 'teams' | 'regulation' | 'structure'

/**
 * After Organisation writes that change readiness, refresh Organisation + Cockpit.
 * Cockpit projects MaterializeMatches from ReadyForMaterialization
 * (Cup: primary skeleton incomplete — not from-slots). Must not stay stale after structure edits.
 */
async function invalidateAfterOrganisationMutation(
  queryClient: QueryClient,
  competitionId: string,
) {
  await Promise.all([
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.organisation(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.detail(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.workspace(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.cockpit(competitionId),
    }),
  ])
}

/**
 * Organisation Hub — GET /competitions/{id}/organisation + Slice 2 mutations.
 * Surfaces V3: bandeau · strip préparation · Équipes/Règlement · Structure.
 * Hub is read-only; editing happens in modal dialogs.
 */
export function OrganisationPage() {
  const { competitionId = '' } = useParams()

  const query = useQuery({
    queryKey: queryKeys.competitions.organisation(competitionId),
    queryFn: () => fetchOrganisationView(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page page--organisation">
      {query.isPending && !query.data && <LoadingState />}
      {query.isError && !query.data && <ErrorState error={query.error} />}
      {query.data && <OrganisationViewPanel data={query.data} />}
    </main>
  )
}

function OrganisationViewPanel({ data }: { data: OrganisationView }) {
  const { t } = useTranslation('organisation')
  const can = (action: string) => data.actions.includes(action)
  const overviewHref = `/competitions/${data.competitionId}`
  const [editor, setEditor] = useState<OrganisationEditor>(null)

  const canAdd = can('AddEntry')
  const canRename = can('RenameEntry')
  const canWithdraw = can('WithdrawEntry')
  const canExclude = can('ExcludeEntry')
  const canManageEntries = canRename || canWithdraw || canExclude
  const canReplace = can('ReplaceRegulation')
  const canConfigure = can('ConfigureStructure')

  const closeEditor = () => setEditor(null)

  return (
    <div className="organisation">
      <header className="organisation__page-head">
        <Link className="organisation__back" to={overviewHref}>
          <span aria-hidden="true">←</span>
          {t('back')}
        </Link>
        <h1 className="organisation__title">{t('title')}</h1>
      </header>

      <ContextBand data={data} />
      <IdentitySection data={data} />
      <PreparationStrip
        data={data}
        onOpenEditor={(next) => setEditor(next)}
      />

      <div className="organisation__mid">
        <ParticipantsSection
          data={data}
          canAdd={canAdd}
          canManage={canManageEntries}
          onAdd={() => setEditor('teams')}
          onManage={() => setEditor('teams')}
        />
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

      {editor === 'teams' && (
        <TeamsEditorDialog
          data={data}
          canAdd={canAdd}
          canRename={canRename}
          canWithdraw={canWithdraw}
          canExclude={canExclude}
          onClose={closeEditor}
        />
      )}
      {editor === 'regulation' && (
        <RegulationEditorDialog data={data} onClose={closeEditor} />
      )}
      {editor === 'structure' && (
        <StructureEditorDialog data={data} onClose={closeEditor} />
      )}
    </div>
  )
}

function OrganisationDialog({
  title,
  onClose,
  children,
}: {
  title: string
  onClose: () => void
  children: ReactNode
}) {
  const { t } = useTranslation('common')
  const titleId = useId()

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  return (
    <div className="organisation-dialog">
      <button
        type="button"
        className="organisation-dialog__backdrop"
        aria-label={t('close')}
        onClick={onClose}
        tabIndex={-1}
      />
      <div
        className="organisation-dialog__panel ds-overlay"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        <header className="organisation-dialog__header">
          <h2 id={titleId} className="organisation-dialog__title">
            {title}
          </h2>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            onClick={onClose}
          >
            {t('close')}
          </button>
        </header>
        <div className="organisation-dialog__body">{children}</div>
      </div>
    </div>
  )
}

function ContextBand({ data }: { data: OrganisationView }) {
  const { t } = useTranslation('organisation')
  const formatText = data.format.kind
    ? structureFormatKindLabel(data.format.kind)
    : t('structure.formatNotConfigured')
  const phaseCount = data.format.primaryStageId ? 1 : 0

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
  )
}

function IdentitySection({ data }: { data: OrganisationView }) {
  const { t } = useTranslation('organisation')
  const queryClient = useQueryClient()
  const [shortName, setShortName] = useState(data.shortName ?? '')
  const [logoMediaId, setLogoMediaId] = useState<string | null>(
    data.logoMediaId ?? null,
  )
  const [scheduledStart, setScheduledStart] = useState(
    data.scheduledStart?.slice(0, 10) ?? '',
  )
  const [scheduledEnd, setScheduledEnd] = useState(
    data.scheduledEnd?.slice(0, 10) ?? '',
  )

  useEffect(() => {
    setShortName(data.shortName ?? '')
    setLogoMediaId(data.logoMediaId ?? null)
    setScheduledStart(data.scheduledStart?.slice(0, 10) ?? '')
    setScheduledEnd(data.scheduledEnd?.slice(0, 10) ?? '')
  }, [data.shortName, data.logoMediaId, data.scheduledStart, data.scheduledEnd])

  const presentationMutation = useMutation({
    mutationFn: () =>
      updateCompetitionPresentation(data.competitionId, {
        shortName: shortName.trim() || null,
        logoMediaId,
      }),
    onSuccess: async () => {
      await invalidateAfterOrganisationMutation(queryClient, data.competitionId)
    },
  })

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
      await invalidateAfterOrganisationMutation(queryClient, data.competitionId)
    },
  })

  return (
    <section className="ds-panel" aria-labelledby="identity-heading">
      <PanelHead id="identity-heading" icon={<TeamsIcon size="md" />}>
        {t('identity.heading')}
      </PanelHead>
      <div className="organisation-identity">
        <form
          className="form"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            presentationMutation.mutate()
          }}
        >
          <label className="field">
            {t('identity.shortName')}
            <input
              value={shortName}
              onChange={(event) => setShortName(event.target.value)}
              maxLength={20}
              disabled={presentationMutation.isPending}
            />
          </label>
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
            event.preventDefault()
            scheduleMutation.mutate()
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
  )
}

function PanelHead({
  id,
  icon,
  children,
}: {
  id: string
  icon: ReactNode
  children: ReactNode
}) {
  return (
    <h2 id={id} className="organisation-panel__head">
      <span className="organisation-panel__icon" aria-hidden="true">
        {icon}
      </span>
      {children}
    </h2>
  )
}

function PreparationStrip({
  data,
  onOpenEditor,
}: {
  data: OrganisationView
  onOpenEditor: (editor: Exclude<OrganisationEditor, null>) => void
}) {
  const { t } = useTranslation('organisation')
  const readiness = data.readiness
  const formatKind = data.format.kind
  const needsDraw = formatKind === 'Groups' || formatKind === 'Cup'
  const readyToMaterialize = readiness.readyForMaterialization
  const blockers = readiness.blockers
  const openCount = blockers.length

  if (readyToMaterialize) {
    const isCup = formatKind === 'Cup'
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
              ? t('readiness.goToCockpitCupSkeleton')
              : t('readiness.goToCockpitMaterialize')}
            <span aria-hidden="true">→</span>
          </Link>
        </div>
      </section>
    )
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
    )
  }

  if (openCount === 0) {
    return null
  }

  return (
    <section
      className="organisation-strip"
      aria-labelledby="readiness-heading"
    >
      <div className="organisation-strip__head">
        <h2 id="readiness-heading" className="organisation-strip__title">
          {t('readiness.openItems', { count: openCount })}
        </h2>
      </div>
      <ul className="organisation-strip__actions-list">
        {blockers.map((code) => {
          const editor = editorForBlocker(code)
          const label = attentionSourceLabel(code)
          return (
            <li key={code}>
              {editor ? (
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
          )
        })}
      </ul>
    </section>
  )
}

function editorForBlocker(
  code: string,
): Exclude<OrganisationEditor, null> | null {
  if (code === 'InsufficientParticipants') {
    return 'teams'
  }
  if (code === 'MissingStage') {
    return 'structure'
  }
  return null
}

function ParticipantsSection({
  data,
  canAdd,
  canManage,
  onAdd,
  onManage,
}: {
  data: OrganisationView
  canAdd: boolean
  canManage: boolean
  onAdd: () => void
  onManage: () => void
}) {
  const { t } = useTranslation('organisation')
  const entries = data.participants.entries
  const activeCount = data.participants.activeCount
  const belowMinimum = activeCount < data.regulation.minimumTeams
  const summaryHint = belowMinimum
    ? t('participants.summaryIncomplete', {
        count: activeCount,
        minimum: data.regulation.minimumTeams,
      })
    : t('participants.summaryComplete', { count: activeCount })

  return (
    <section className="ds-panel" aria-labelledby="participants-heading">
      <PanelHead id="participants-heading" icon={<TeamsIcon size="md" />}>
        {t('participants.heading')}
      </PanelHead>

      {entries.length === 0 ? (
        <EmptyState title={t('participants.emptyTitle')}>
          {t('participants.emptyBody')}
        </EmptyState>
      ) : (
        <>
          <ul
            className="organisation-crests"
            aria-label={t('participants.crestsLabel')}
          >
            {entries.slice(0, 8).map((entry) => (
              <li key={entry.entryId} title={entry.displayName}>
                <TeamCrest
                  name={entry.displayName}
                  logoMediaId={entry.logoMediaId}
                  primaryColor={entry.primaryColor}
                  className="organisation-crest"
                />
              </li>
            ))}
            {entries.length > 8 && (
              <li className="organisation-crest organisation-crest--more">
                +{entries.length - 8}
              </li>
            )}
          </ul>

          <ul className="organisation-entries">
            {entries.map((entry) => (
              <li key={entry.entryId}>
                <Link
                  className="organisation-entry organisation-entry--read"
                  to={`/competitions/${data.competitionId}/organisation/entries/${entry.entryId}`}
                >
                  <TeamCrest
                    name={entry.displayName}
                    logoMediaId={entry.logoMediaId}
                    primaryColor={entry.primaryColor}
                    className="organisation-crest"
                  />
                  <p className="organisation-entry__identity">
                    <span className="organisation-entry__name">
                      {entry.displayName}
                    </span>
                    {entry.status === 'Active' ? (
                      <span
                        className="organisation-entry__ok"
                        aria-label={t('participants.statusOk')}
                      >
                        <CheckIcon size="sm" aria-hidden="true" />
                      </span>
                    ) : (
                      <EntryStatusBadge status={entry.status} />
                    )}
                  </p>
                  <span className="organisation-entry__chevron" aria-hidden="true">
                    ›
                  </span>
                </Link>
              </li>
            ))}
          </ul>

          <p
            className={`organisation-panel__summary${belowMinimum ? ' organisation-panel__summary--warn' : ''}`}
          >
            {summaryHint}
          </p>
        </>
      )}

      {(canAdd || canManage) && (
        <div className="organisation-panel__footer organisation-panel__footer--spread">
          {canAdd && (
            <button
              type="button"
              className="organisation-action"
              onClick={onAdd}
            >
              {t('participants.addAction')}
            </button>
          )}
          {canManage && (
            <button
              type="button"
              className="organisation-action"
              onClick={onManage}
            >
              {t('participants.manageAction')}
              <span aria-hidden="true">→</span>
            </button>
          )}
        </div>
      )}
    </section>
  )
}

function TeamsEditorDialog({
  data,
  canAdd,
  canRename,
  canWithdraw,
  canExclude,
  onClose,
}: {
  data: OrganisationView
  canAdd: boolean
  canRename: boolean
  canWithdraw: boolean
  canExclude: boolean
  onClose: () => void
}) {
  const { t } = useTranslation('organisation')
  const queryClient = useQueryClient()
  const [displayName, setDisplayName] = useState('')
  const [shortName, setShortName] = useState('')
  const [logoMediaId, setLogoMediaId] = useState<string | null>(null)
  const [primaryColor, setPrimaryColor] = useState('')
  const [secondaryColor, setSecondaryColor] = useState('')
  const competitionId = data.competitionId

  const invalidateOrganisation = () =>
    invalidateAfterOrganisationMutation(queryClient, competitionId)

  const addMutation = useMutation({
    mutationFn: () =>
      addCompetitionEntry(competitionId, {
        displayName: displayName.trim(),
        shortName: shortName.trim() || null,
        logoMediaId,
        primaryColor: primaryColor.trim() || null,
        secondaryColor: secondaryColor.trim() || null,
      }),
    onSuccess: async () => {
      setDisplayName('')
      setShortName('')
      setLogoMediaId(null)
      setPrimaryColor('')
      setSecondaryColor('')
      await invalidateOrganisation()
    },
  })

  return (
    <OrganisationDialog title={t('participants.heading')} onClose={onClose}>
      {data.participants.entries.length === 0 ? (
        <EmptyState title={t('participants.emptyTitle')}>
          {t('participants.emptyBody')}
        </EmptyState>
      ) : (
        <ul className="organisation-entries">
          {data.participants.entries.map((entry) => (
            <li key={entry.entryId}>
              <EntryEditorRow
                competitionId={competitionId}
                entry={entry}
                canRename={canRename}
                canWithdraw={canWithdraw}
                canExclude={canExclude}
                onChanged={invalidateOrganisation}
              />
            </li>
          ))}
        </ul>
      )}

      {canAdd && (
        <form
          className="form"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            if (displayName.trim().length === 0 || addMutation.isPending) {
              return
            }
            addMutation.mutate()
          }}
        >
          <label className="field">
            {t('participants.newEntryName')}
            <input
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              disabled={addMutation.isPending}
              placeholder={t('participants.newEntryPlaceholder')}
              required
            />
          </label>
          <label className="field">
            {t('participants.shortName')}
            <input
              value={shortName}
              onChange={(event) => setShortName(event.target.value)}
              disabled={addMutation.isPending}
              maxLength={20}
            />
          </label>
          <LogoMediaField
            name={displayName.trim() || t('participants.newEntryPlaceholder')}
            value={logoMediaId}
            onChange={setLogoMediaId}
            disabled={addMutation.isPending}
            label={t('participants.logo')}
          />
          <div className="form form--inline">
            <label className="field">
              {t('participants.primaryColor')}
              <input
                value={primaryColor}
                onChange={(event) => setPrimaryColor(event.target.value)}
                disabled={addMutation.isPending}
                placeholder="#RRGGBB"
              />
            </label>
            <label className="field">
              {t('participants.secondaryColor')}
              <input
                value={secondaryColor}
                onChange={(event) => setSecondaryColor(event.target.value)}
                disabled={addMutation.isPending}
                placeholder="#RRGGBB"
              />
            </label>
          </div>
          <button
            type="submit"
            className="ds-btn ds-btn--primary"
            disabled={addMutation.isPending || displayName.trim().length === 0}
          >
            {addMutation.isPending ? (
              <PendingLabel>{t('participants.adding')}</PendingLabel>
            ) : (
              t('participants.add')
            )}
          </button>
          {addMutation.isError && <MutationError error={addMutation.error} />}
        </form>
      )}
    </OrganisationDialog>
  )
}

function EntryEditorRow({
  competitionId,
  entry,
  canRename,
  canWithdraw,
  canExclude,
  onChanged,
}: {
  competitionId: string
  entry: OrganisationEntry
  canRename: boolean
  canWithdraw: boolean
  canExclude: boolean
  onChanged: () => Promise<void>
}) {
  const { t } = useTranslation('organisation')
  const [name, setName] = useState(entry.displayName)
  const [shortName, setShortName] = useState(entry.shortName ?? '')
  const [logoMediaId, setLogoMediaId] = useState<string | null>(entry.logoMediaId ?? null)
  const [primaryColor, setPrimaryColor] = useState(entry.primaryColor ?? '')
  const [secondaryColor, setSecondaryColor] = useState(
    entry.secondaryColor ?? '',
  )
  const busyLabel = t('working')

  const renameMutation = useMutation({
    mutationFn: () =>
      renameCompetitionEntry(competitionId, entry.entryId, {
        displayName: name.trim(),
      }),
    onSuccess: onChanged,
  })

  const presentationMutation = useMutation({
    mutationFn: () =>
      updateEntryPresentation(competitionId, entry.entryId, {
        shortName: shortName.trim() || null,
        logoMediaId,
        primaryColor: primaryColor.trim() || null,
        secondaryColor: secondaryColor.trim() || null,
      }),
    onSuccess: onChanged,
  })

  const withdrawMutation = useMutation({
    mutationFn: () => withdrawCompetitionEntry(competitionId, entry.entryId),
    onSuccess: onChanged,
  })

  const excludeMutation = useMutation({
    mutationFn: () => excludeCompetitionEntry(competitionId, entry.entryId),
    onSuccess: onChanged,
  })

  const pending =
    renameMutation.isPending ||
    presentationMutation.isPending ||
    withdrawMutation.isPending ||
    excludeMutation.isPending

  const mutationError =
    renameMutation.error ??
    presentationMutation.error ??
    withdrawMutation.error ??
    excludeMutation.error

  return (
    <div className="organisation-entry">
      <p className="organisation-entry__identity">
        <TeamCrest
          name={entry.displayName}
          logoMediaId={entry.logoMediaId}
          primaryColor={entry.primaryColor}
          className="organisation-crest"
        />
        <span className="organisation-entry__name">{entry.displayName}</span>
        <EntryStatusBadge status={entry.status} />
      </p>
      {(canRename || canWithdraw || canExclude) && (
        <div className="organisation-entry__actions">
          {canRename && (
            <>
            <form
              className="form form--inline"
              onSubmit={(event: FormEvent) => {
                event.preventDefault()
                if (name.trim().length === 0 || pending) {
                  return
                }
                renameMutation.mutate()
              }}
            >
              <label className="field">
                {t('participants.rename')}
                <input
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  disabled={pending}
                  required
                />
              </label>
              <button
                type="submit"
                className="ds-btn ds-btn--ghost"
                disabled={pending || name.trim().length === 0}
              >
                {renameMutation.isPending ? (
                  <PendingLabel>{busyLabel}</PendingLabel>
                ) : (
                  t('participants.rename')
                )}
              </button>
            </form>
            <form
              className="form"
              onSubmit={(event: FormEvent) => {
                event.preventDefault()
                if (pending) {
                  return
                }
                presentationMutation.mutate()
              }}
            >
              <label className="field">
                {t('participants.shortName')}
                <input
                  value={shortName}
                  onChange={(event) => setShortName(event.target.value)}
                  disabled={pending}
                  maxLength={20}
                />
              </label>
              <LogoMediaField
                name={entry.displayName}
                value={logoMediaId}
                onChange={setLogoMediaId}
                disabled={pending}
                label={t('participants.logo')}
              />
              <div className="form form--inline">
                <label className="field">
                  {t('participants.primaryColor')}
                  <input
                    value={primaryColor}
                    onChange={(event) => setPrimaryColor(event.target.value)}
                    disabled={pending}
                    placeholder="#RRGGBB"
                  />
                </label>
                <label className="field">
                  {t('participants.secondaryColor')}
                  <input
                    value={secondaryColor}
                    onChange={(event) => setSecondaryColor(event.target.value)}
                    disabled={pending}
                    placeholder="#RRGGBB"
                  />
                </label>
              </div>
              <button
                type="submit"
                className="ds-btn ds-btn--ghost"
                disabled={pending}
              >
                {presentationMutation.isPending ? (
                  <PendingLabel>{busyLabel}</PendingLabel>
                ) : (
                  t('participants.savePresentation')
                )}
              </button>
            </form>
            </>
          )}
          {canWithdraw && (
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              disabled={pending}
              onClick={() => {
                if (
                  !window.confirm(
                    t('participants.confirmWithdraw', {
                      name: entry.displayName,
                    }),
                  )
                ) {
                  return
                }
                withdrawMutation.mutate()
              }}
            >
              {withdrawMutation.isPending ? (
                <PendingLabel>{busyLabel}</PendingLabel>
              ) : (
                t('participants.withdraw')
              )}
            </button>
          )}
          {canExclude && (
            <button
              type="button"
              className="ds-btn ds-btn--destructive"
              disabled={pending}
              onClick={() => {
                if (
                  !window.confirm(
                    t('participants.confirmExclude', {
                      name: entry.displayName,
                    }),
                  )
                ) {
                  return
                }
                excludeMutation.mutate()
              }}
            >
              {excludeMutation.isPending ? (
                <PendingLabel>{busyLabel}</PendingLabel>
              ) : (
                t('participants.exclude')
              )}
            </button>
          )}
        </div>
      )}
      {mutationError && (
        <div className="organisation-entry__error">
          <MutationError error={mutationError} />
        </div>
      )}
    </div>
  )
}

function RegulationSection({
  data,
  canReplace,
  onEdit,
}: {
  data: OrganisationView
  canReplace: boolean
  onEdit: () => void
}) {
  const { t } = useTranslation('organisation')
  const regulation = data.regulation

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
  )
}

function RegulationEditorDialog({
  data,
  onClose,
}: {
  data: OrganisationView
  onClose: () => void
}) {
  const { t } = useTranslation('organisation')
  const queryClient = useQueryClient()
  const regulation = data.regulation
  const [form, setForm] = useState<ReplaceRegulationRequest>({
    minimumTeams: regulation.minimumTeams,
    maximumTeams: regulation.maximumTeams,
    durationPerPeriod: regulation.durationPerPeriod,
    numberOfPeriods: regulation.numberOfPeriods,
    halfTimeDuration: 15,
    winPoints: regulation.winPoints,
    drawPoints: regulation.drawPoints,
    lossPoints: regulation.lossPoints,
    forfeitWinnerGoals: 3,
    forfeitLoserGoals: 0,
    allowedTypes: [...(regulation.allowedTypes ?? [])],
  })

  const mutation = useMutation({
    mutationFn: () => replaceCompetitionRegulation(data.competitionId, form),
    onSuccess: async () => {
      await invalidateAfterOrganisationMutation(
        queryClient,
        data.competitionId,
      )
    },
  })

  const setNumber =
    (key: keyof ReplaceRegulationRequest) =>
    (value: string) => {
      const parsed = Number(value)
      setForm((current) => ({
        ...current,
        [key]: Number.isFinite(parsed) ? parsed : current[key],
      }))
    }

  function toggleAllowedType(type: DisciplinaryType) {
    setForm((current) => {
      const selected = current.allowedTypes ?? []
      const next = selected.includes(type)
        ? selected.filter((item) => item !== type)
        : [...selected, type]
      return { ...current, allowedTypes: next }
    })
  }

  return (
    <OrganisationDialog title={t('regulation.replaceLegend')} onClose={onClose}>
      <form
        className="form form--wide"
        onSubmit={(event: FormEvent) => {
          event.preventDefault()
          if (mutation.isPending) {
            return
          }
          mutation.mutate()
        }}
      >
        <fieldset className="fieldset" disabled={mutation.isPending}>
          <legend className="fieldset__legend">
            {t('regulation.replaceLegend')}
          </legend>
          <div className="form-row">
            <label className="field">
              {t('regulation.minimumTeams')}
              <input
                type="number"
                value={form.minimumTeams}
                onChange={(event) =>
                  setNumber('minimumTeams')(event.target.value)
                }
                required
              />
            </label>
            <label className="field">
              {t('regulation.maximumTeams')}
              <input
                type="number"
                value={form.maximumTeams}
                onChange={(event) =>
                  setNumber('maximumTeams')(event.target.value)
                }
                required
              />
            </label>
          </div>
          <div className="form-row">
            <label className="field">
              {t('regulation.durationPerPeriod')}
              <input
                type="number"
                value={form.durationPerPeriod}
                onChange={(event) =>
                  setNumber('durationPerPeriod')(event.target.value)
                }
                required
              />
            </label>
            <label className="field">
              {t('regulation.numberOfPeriods')}
              <input
                type="number"
                value={form.numberOfPeriods}
                onChange={(event) =>
                  setNumber('numberOfPeriods')(event.target.value)
                }
                required
              />
            </label>
            <label className="field">
              {t('regulation.halfTimeDuration')}
              <input
                type="number"
                value={form.halfTimeDuration}
                onChange={(event) =>
                  setNumber('halfTimeDuration')(event.target.value)
                }
                required
              />
            </label>
          </div>
          <div className="form-row">
            <label className="field">
              {t('regulation.winPoints')}
              <input
                type="number"
                value={form.winPoints}
                onChange={(event) => setNumber('winPoints')(event.target.value)}
                required
              />
            </label>
            <label className="field">
              {t('regulation.drawPoints')}
              <input
                type="number"
                value={form.drawPoints}
                onChange={(event) =>
                  setNumber('drawPoints')(event.target.value)
                }
                required
              />
            </label>
            <label className="field">
              {t('regulation.lossPoints')}
              <input
                type="number"
                value={form.lossPoints}
                onChange={(event) =>
                  setNumber('lossPoints')(event.target.value)
                }
                required
              />
            </label>
          </div>
          <fieldset className="fieldset fieldset--nested">
            <legend className="fieldset__legend">
              {t('regulation.allowedTypesLegend')}
            </legend>
            <p className="organisation-panel__muted">
              {t('regulation.allowedTypesHint')}
            </p>
            <div className="form-row">
              {(['Yellow', 'Red', 'White'] as const).map((type) => (
                <label key={type} className="field field--checkbox">
                  <input
                    type="checkbox"
                    checked={(form.allowedTypes ?? []).includes(type)}
                    onChange={() => toggleAllowedType(type)}
                  />
                  {t(`regulation.type.${type}`)}
                </label>
              ))}
            </div>
          </fieldset>
        </fieldset>
        <div className="button-row">
          <button
            type="submit"
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('regulation.saving')}</PendingLabel>
            ) : (
              t('regulation.save')
            )}
          </button>
          <span className="caption">{t('regulation.saveHint')}</span>
        </div>
        {mutation.isError && <MutationError error={mutation.error} />}
      </form>
    </OrganisationDialog>
  )
}

function StructureSection({
  data,
  canConfigure,
  onConfigure,
}: {
  data: OrganisationView
  canConfigure: boolean
  onConfigure: () => void
}) {
  const { t } = useTranslation('organisation')
  const formatKind = data.format.kind
  const primaryStageId = data.format.primaryStageId
  const stageName =
    data.format.primaryStageName ?? t('structure.primaryStageFallback')
  const formatLabel = formatKind
    ? structureFormatKindLabel(formatKind)
    : t('structure.formatNotConfigured')
  const phaseCount = primaryStageId ? 1 : 0
  const drawLabel =
    formatKind === 'Championship'
      ? t('structure.drawNotRequired')
      : data.structure.hasDrawRules
        ? t('structure.drawConfigured', {
            pots: data.structure.numberOfPots ?? '—',
          })
        : t('structure.drawMissing')

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
                  {matchGenerationFormatLabel(data.structure.matchGenerationFormat)}
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
  )
}

function StructureEditorDialog({
  data,
  onClose,
}: {
  data: OrganisationView
  onClose: () => void
}) {
  const { t } = useTranslation('organisation')
  const queryClient = useQueryClient()
  const [format, setFormat] = useState<StructureFormatKind>(
    data.format.kind ?? 'Championship',
  )
  const [stageName, setStageName] = useState('')
  const [matchdayCount, setMatchdayCount] = useState(
    Math.max(1, data.structure.matchdayCount || 1),
  )
  const [groupCount, setGroupCount] = useState(
    Math.max(1, data.structure.groupCount || 2),
  )
  const [participantsPerGroup, setParticipantsPerGroup] = useState(2)
  const [bracketSize, setBracketSize] = useState(
    Math.max(2, data.structure.slotCount || 4),
  )
  const [swissRoundCount, setSwissRoundCount] = useState(
    Math.max(1, data.structure.swissRoundCount || 3),
  )
  const [matchGenerationFormat, setMatchGenerationFormat] =
    useState<MatchGenerationFormat>(
      data.structure.matchGenerationFormat ?? 'SingleRoundRobin',
    )

  const mutation = useMutation({
    mutationFn: () =>
      configureOrganisationStructure(data.competitionId, {
        format,
        stageName: stageName.trim() || null,
        matchdayCount: format === 'Championship' ? matchdayCount : null,
        groupCount: format === 'Groups' ? groupCount : null,
        participantsPerGroup:
          format === 'Groups' ? participantsPerGroup : null,
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
      )
    },
  })

  return (
    <OrganisationDialog
      title={t('structure.configureLegend')}
      onClose={onClose}
    >
      <form
        className="form"
        onSubmit={(event: FormEvent) => {
          event.preventDefault()
          if (mutation.isPending) {
            return
          }
          mutation.mutate()
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
              <option value="Swiss">
                {structureFormatKindLabel('Swiss')}
              </option>
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
        <div className="button-row">
          <button
            type="submit"
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('structure.configuring')}</PendingLabel>
            ) : (
              t('structure.configure')
            )}
          </button>
          <span className="caption">{t('structure.configureHint')}</span>
        </div>
        {mutation.isError && <MutationError error={mutation.error} />}
      </form>
    </OrganisationDialog>
  )
}
