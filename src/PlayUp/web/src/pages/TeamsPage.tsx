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
import { useNavigate, useParams } from 'react-router-dom'
import {
  addCompetitionEntry,
  deleteCompetitionEntries,
  deleteCompetitionEntry,
  fetchOrganisationView,
  renameCompetitionEntry,
  updateEntryPresentation,
  withdrawCompetitionEntries,
  withdrawCompetitionEntry,
} from '../api'
import { Dialog } from '../design-system/components/Dialog'
import { LogoMediaField } from '../design-system/LogoMediaField'
import { TeamCrest } from '../design-system/TeamCrest'
import { CloseIcon } from '../design-system/icons/shellIcons'
import {
  EmptySelectionIcon,
  LayersIcon,
  PersonIcon,
  PencilIcon,
  PlusIcon,
  TrashIcon,
  WithdrawIcon,
} from '../design-system/icons/overviewIcons'
import { queryKeys } from '../queryKeys'
import {
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
  StatusBadge,
} from '../ui'
import { entryStatusLabel } from '../i18n/enumLabels'
import type {
  EntryStatus,
  OrganisationEntry,
  OrganisationView,
} from '../types'
import { TeamRosterDrawer } from './TeamRosterDrawer'
import { isTeamsNarrowViewport } from '../layout/viewportBreakpoints'
import './teams.css'

async function invalidateAfterTeamsMutation(
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
      queryKey: queryKeys.competitions.overview(competitionId),
    }),
  ])
}

/**
 * Équipes — grille de tuiles + tiroir d’effectif.
 * Read: GET …/organisation. Mutations: Add/Rename/Presentation/Delete/Withdraw.
 */
export function TeamsPage() {
  const { competitionId = '', entryId } = useParams()

  const query = useQuery({
    queryKey: queryKeys.competitions.organisation(competitionId),
    queryFn: () => fetchOrganisationView(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page page--teams">
      {query.isPending && !query.data && <LoadingState />}
      {query.isError && !query.data && <ErrorState error={query.error} />}
      {query.data && (
        <TeamsView data={query.data} routeEntryId={entryId} />
      )}
    </main>
  )
}

function TeamsView({
  data,
  routeEntryId,
}: {
  data: OrganisationView
  routeEntryId?: string
}) {
  const { t } = useTranslation('teams')
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const can = (action: string) => data.actions.includes(action)
  const canAddAction = can('AddEntry')
  const canDelete = can('DeleteEntry')
  const canWithdraw = can('WithdrawEntry')
  const atCap = data.participants.occupyingCount >= data.regulation.maximumTeams
  const canAdd = canAddAction && !atCap
  const showPlateauReading = true
  const missingMinimum = Math.max(
    0,
    data.regulation.minimumTeams - data.participants.activeCount,
  )
  const emptyCount = canAdd ? missingMinimum : 0
  const entries = data.participants.entries
  const teamsHref = `/competitions/${data.competitionId}/teams`
  const [selectedIds, setSelectedIds] = useState<string[]>(() =>
    routeEntryId ? [routeEntryId] : [],
  )
  const [addOpen, setAddOpen] = useState(false)
  const [identityEntryId, setIdentityEntryId] = useState<string | null>(null)

  const selectedCount = selectedIds.length
  const multi = selectedCount >= 2
  const drawerEntryId = selectedCount === 1 ? selectedIds[0] : undefined
  const identityEntry =
    identityEntryId == null
      ? undefined
      : entries.find((entry) => entry.entryId === identityEntryId)

  function goToSelection(nextIds: string[], options?: { openRoster?: boolean }) {
    setSelectedIds(nextIds)
    if (nextIds.length === 0) {
      navigate(teamsHref, { replace: true })
      return
    }
    if (nextIds.length >= 2) {
      navigate(teamsHref, { replace: true })
      return
    }
    const openRoster = options?.openRoster === true
    if (openRoster || !isTeamsNarrowViewport()) {
      navigate(`${teamsHref}/${nextIds[0]}`, { replace: true })
      return
    }
    navigate(teamsHref, { replace: true })
  }

  useEffect(() => {
    if (routeEntryId) {
      setSelectedIds([routeEntryId])
      return
    }
    setSelectedIds((current) => (current.length === 1 ? [] : current))
  }, [routeEntryId])

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        if (addOpen || identityEntryId) {
          return
        }
        goToSelection([])
      }
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  })

  const deleteMutation = useMutation({
    mutationFn: (ids: string[]) =>
      ids.length === 1
        ? deleteCompetitionEntry(data.competitionId, ids[0])
        : deleteCompetitionEntries(data.competitionId, { entryIds: ids }),
    onSuccess: async () => {
      goToSelection([])
      await invalidateAfterTeamsMutation(queryClient, data.competitionId)
    },
  })

  const withdrawMutation = useMutation({
    mutationFn: (ids: string[]) =>
      ids.length === 1
        ? withdrawCompetitionEntry(data.competitionId, ids[0])
        : withdrawCompetitionEntries(data.competitionId, { entryIds: ids }),
    onSuccess: async () => {
      goToSelection([])
      await invalidateAfterTeamsMutation(queryClient, data.competitionId)
    },
  })

  function removeKind(): 'delete' | 'withdraw' {
    return data.status === 'Draft' || data.status === 'Ready'
      ? 'delete'
      : 'withdraw'
  }

  function canRemove() {
    return removeKind() === 'delete' ? canDelete : canWithdraw
  }

  function confirmAndRemove(ids: string[]) {
    if (ids.length === 0 || !canRemove()) {
      return
    }
    const verb = removeKind()
    const targets =
      verb === 'withdraw'
        ? ids.filter(
            (id) =>
              entries.find((entry) => entry.entryId === id)?.status === 'Active',
          )
        : ids
    if (targets.length === 0) {
      return
    }
    const first = entries.find((entry) => entry.entryId === targets[0])
    const confirmed =
      verb === 'delete'
        ? window.confirm(
            targets.length === 1
              ? t('confirmDelete', { name: first?.displayName ?? targets[0] })
              : t('confirmDeleteLot', { count: targets.length }),
          )
        : window.confirm(
            targets.length === 1
              ? t('confirmWithdraw', { name: first?.displayName ?? targets[0] })
              : t('confirmWithdrawLot', { count: targets.length }),
          )
    if (!confirmed) {
      return
    }
    if (verb === 'delete') {
      deleteMutation.mutate(targets)
      return
    }
    withdrawMutation.mutate(targets)
  }

  function onTileBody(entryId: string) {
    if (selectedCount < 2) {
      goToSelection([entryId], { openRoster: true })
      return
    }
    if (selectedIds.includes(entryId)) {
      goToSelection(selectedIds.filter((id) => id !== entryId))
      return
    }
    goToSelection([...selectedIds, entryId])
  }

  function onToggleCheck(entryId: string) {
    if (selectedIds.includes(entryId)) {
      goToSelection(selectedIds.filter((id) => id !== entryId))
      return
    }
    goToSelection([...selectedIds, entryId])
  }

  const mutationError = deleteMutation.error ?? withdrawMutation.error
  const removePending = deleteMutation.isPending || withdrawMutation.isPending
  const removing = removeKind()
  const removeEnabled = canRemove()
  const selectedActiveCount = selectedIds.filter(
    (id) => entries.find((entry) => entry.entryId === id)?.status === 'Active',
  ).length
  const barRemoveEnabled =
    removeEnabled &&
    (removing === 'delete' || selectedActiveCount > 0)
  const removeLabel =
    removing === 'delete' ? t('deleteEntry') : t('withdrawEntry')
  const removeDisabledHint =
    removing === 'delete' ? t('deleteDisabledHint') : t('withdrawDisabledHint')
  const barRemoveHint =
    removing === 'withdraw' && selectedActiveCount === 0
      ? t('alreadyWithdrawnHint')
      : removeEnabled
        ? removeLabel
        : removeDisabledHint

  const compactIcon =
    'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact'

  const rosterOpen =
    selectedCount === 1 &&
    routeEntryId != null &&
    routeEntryId === selectedIds[0]
  const teamsView =
    selectedCount >= 2 ? 'multi' : rosterOpen ? 'detail' : 'list'

  return (
    <div className="teams" data-teams-view={teamsView}>
      <div className="teams__layout">
        <div className="teams__main">
          <header className="teams__head">
            <div className="teams__head-row">
              <h1 className="teams__title">
                {t('title')}
                {showPlateauReading && (
                  <>
                    <span className="teams__title-sep"> · </span>
                    <span className="teams__title-count ds-num">
                      {data.participants.activeCount}
                    </span>
                  </>
                )}
              </h1>
            </div>
            <div className="teams__ops-row">
              {showPlateauReading && (
                <TeamsPlateauReading
                  activeCount={data.participants.activeCount}
                  occupyingCount={data.participants.occupyingCount}
                  minimumTeams={data.regulation.minimumTeams}
                  maximumTeams={data.regulation.maximumTeams}
                />
              )}
              <div className="teams__ops-tail">
                {selectedCount >= 1 && (
                  <div className="teams-bar" role="status">
                    <p className="teams-bar__count">
                      {t('selectionCount', { count: selectedCount })}
                    </p>
                    <div className="ds-icon-toolbar">
                      <button
                        type="button"
                        className={compactIcon}
                        disabled={!barRemoveEnabled || removePending}
                        title={barRemoveHint}
                        aria-label={removeLabel}
                        onClick={() => confirmAndRemove(selectedIds)}
                      >
                        {removing === 'delete' ? (
                          <TrashIcon size="sm" />
                        ) : (
                          <WithdrawIcon size="sm" />
                        )}
                      </button>
                      <button
                        type="button"
                        className={compactIcon}
                        title={t('clearSelection')}
                        aria-label={t('clearSelection')}
                        onClick={() => goToSelection([])}
                      >
                        <CloseIcon size="sm" />
                      </button>
                    </div>
                  </div>
                )}
                <button
                  type="button"
                  className="ds-btn ds-btn--primary teams__add"
                  disabled={!canAdd}
                  title={canAdd ? t('addAction') : t('addDisabledHint')}
                  aria-label={t('addAction')}
                  onClick={() => setAddOpen(true)}
                >
                  <PlusIcon size="sm" />
                  <span className="teams__add-label">{t('addAction')}</span>
                </button>
              </div>
            </div>
          </header>

          {mutationError && <MutationError error={mutationError} />}

          <ul className="teams__grid">
            {entries.map((entry) => {
              const selected = selectedIds.includes(entry.entryId)
              const withdrawn = entry.status === 'Withdrawn'
              const playerCount = (entry.declaredMembers ?? []).filter(
                (member) => member.role === 'Player',
              ).length
              const tileCanRemove =
                removeEnabled && !(removing === 'withdraw' && withdrawn)
              const tileRemoveHint = withdrawn && removing === 'withdraw'
                ? t('alreadyWithdrawnHint')
                : tileCanRemove
                  ? removeLabel
                  : removeDisabledHint
              const tileRemoveLabel =
                removing === 'delete'
                  ? t('deleteEntryNamed', { name: entry.displayName })
                  : t('withdrawEntryNamed', { name: entry.displayName })
              const statusBadge = tileStatusBadge(entry.status, t)
              return (
                <li key={entry.entryId}>
                  <article
                    className="teams-tile"
                    data-selected={selected ? 'true' : 'false'}
                    data-withdrawn={withdrawn ? 'true' : 'false'}
                  >
                    <button
                      type="button"
                      className="teams-tile__hit"
                      aria-label={entry.displayName}
                      onClick={() => onTileBody(entry.entryId)}
                    />
                    <div className="teams-tile__chrome">
                      <label className="teams-tile__check">
                        <input
                          type="checkbox"
                          checked={selected}
                          aria-label={t('selectEntry', {
                            name: entry.displayName,
                          })}
                          onChange={() => onToggleCheck(entry.entryId)}
                        />
                      </label>
                      {!multi && (
                        <div className="ds-icon-toolbar">
                          <button
                            type="button"
                            className={compactIcon}
                            title={t('editIdentityTooltip')}
                            aria-label={t('editIdentity', {
                              name: entry.displayName,
                            })}
                            onClick={() => setIdentityEntryId(entry.entryId)}
                          >
                            <PencilIcon size="sm" />
                          </button>
                          <button
                            type="button"
                            className={compactIcon}
                            disabled={!tileCanRemove || removePending}
                            title={tileRemoveHint}
                            aria-label={tileRemoveLabel}
                            onClick={() => confirmAndRemove([entry.entryId])}
                          >
                            {removing === 'delete' ? (
                              <TrashIcon size="sm" />
                            ) : (
                              <WithdrawIcon size="sm" />
                            )}
                          </button>
                        </div>
                      )}
                    </div>
                    <div className="teams-tile__body">
                      <span className="teams-tile__figure" aria-hidden="true">
                        <TeamCrest
                          name={entry.displayName}
                          logoMediaId={entry.logoMediaId}
                          primaryColor={entry.primaryColor}
                          size="lg"
                        />
                        <p className="teams-tile__name">{entry.displayName}</p>
                      </span>
                      <span className="teams-tile__meta">
                        <span
                          className="teams-tile__meta-slot teams-tile__players"
                          aria-label={t('playerCount', { count: playerCount })}
                        >
                          <PersonIcon size="sm" />
                          <span className="ds-num">{playerCount}</span>
                        </span>
                        <span className="teams-tile__swatches" aria-hidden="true">
                          <span
                            className="teams-tile__swatch"
                            style={{
                              background: entry.primaryColor || 'transparent',
                            }}
                          />
                          <span
                            className="teams-tile__swatch"
                            style={{
                              background: entry.secondaryColor || 'transparent',
                            }}
                          />
                        </span>
                        <span className="teams-tile__meta-slot">
                          {statusBadge}
                        </span>
                      </span>
                    </div>
                  </article>
                </li>
              )
            })}
            {Array.from({ length: emptyCount }, (_, index) => (
              <li key={`empty-${index}`}>
                <article className="teams-tile teams-tile--empty">
                  <button
                    type="button"
                    className="teams-tile__hit"
                    aria-label={t('emptyTileAria')}
                    onClick={() => setAddOpen(true)}
                  />
                  <div className="teams-tile__chrome" aria-hidden="true" />
                  <div className="teams-tile__body">
                    <PlusIcon size="lg" />
                    <span>{t('emptyTile')}</span>
                  </div>
                </article>
              </li>
            ))}
          </ul>
        </div>

        <aside className="ds-panel teams-drawer" aria-label={t('roster.panelLabel')}>
          {selectedCount === 0 && (
            <div className="teams-drawer__idle">
              <span className="teams-drawer__idle-icon" aria-hidden="true">
                <EmptySelectionIcon size="lg" />
              </span>
              <p className="teams-drawer__idle-title">{t('roster.idleTitle')}</p>
              <p className="teams-drawer__idle-body">{t('roster.idleBody')}</p>
            </div>
          )}
          {selectedCount >= 2 && (
            <div className="teams-drawer__idle">
              <span className="teams-drawer__idle-icon" aria-hidden="true">
                <LayersIcon size="lg" />
              </span>
              <p className="teams-drawer__idle-title">
                {t('roster.multiTitle', { count: selectedCount })}
              </p>
              <p className="teams-drawer__idle-body">{t('roster.multiBody')}</p>
            </div>
          )}
          {drawerEntryId && (
            <TeamRosterDrawer
              data={data}
              entryId={drawerEntryId}
              onBack={() => goToSelection([])}
            />
          )}
        </aside>
      </div>

      <AddEntryDialog
        data={data}
        open={addOpen}
        onClose={() => setAddOpen(false)}
      />
      <IdentityDialog
        competitionId={data.competitionId}
        entry={identityEntry ?? null}
        open={identityEntry != null}
        onClose={() => setIdentityEntryId(null)}
      />
    </div>
  )
}

function AddEntryDialog({
  data,
  open,
  onClose,
}: {
  data: OrganisationView
  open: boolean
  onClose: () => void
}) {
  const { t } = useTranslation('teams')
  const { t: tCommon } = useTranslation('common')
  const queryClient = useQueryClient()
  const formId = useId()
  const [displayName, setDisplayName] = useState('')
  const [shortName, setShortName] = useState('')
  const [logoMediaId, setLogoMediaId] = useState<string | null>(null)
  const [primaryColor, setPrimaryColor] = useState('')
  const [secondaryColor, setSecondaryColor] = useState('')

  useEffect(() => {
    if (!open) {
      return
    }
    setDisplayName('')
    setShortName('')
    setLogoMediaId(null)
    setPrimaryColor('')
    setSecondaryColor('')
  }, [open])

  const addMutation = useMutation({
    mutationFn: () =>
      addCompetitionEntry(data.competitionId, {
        displayName: displayName.trim(),
        shortName: shortName.trim() || null,
        logoMediaId,
        primaryColor: primaryColor.trim() || null,
        secondaryColor: secondaryColor.trim() || null,
      }),
    onSuccess: async () => {
      await invalidateAfterTeamsMutation(queryClient, data.competitionId)
      onClose()
    },
  })

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('addDialogTitle')}
      closeLabel={tCommon('close')}
      closeDisabled={addMutation.isPending}
      size="sm"
      footer={
        <button
          type="submit"
          form={formId}
          className="ds-btn ds-btn--primary"
          disabled={addMutation.isPending || displayName.trim().length === 0}
        >
          {addMutation.isPending ? (
            <PendingLabel>{t('adding')}</PendingLabel>
          ) : (
            t('add')
          )}
        </button>
      }
    >
      <form
        id={formId}
        className="form"
        onSubmit={(event: FormEvent) => {
          event.preventDefault()
          if (displayName.trim().length === 0 || addMutation.isPending) {
            return
          }
          addMutation.mutate()
        }}
      >
        <IdentityFields
          name={displayName}
          shortName={shortName}
          logoMediaId={logoMediaId}
          primaryColor={primaryColor}
          secondaryColor={secondaryColor}
          disabled={addMutation.isPending}
          onName={setDisplayName}
          onShortName={setShortName}
          onLogo={setLogoMediaId}
          onPrimary={setPrimaryColor}
          onSecondary={setSecondaryColor}
        />
        {addMutation.isError && <MutationError error={addMutation.error} />}
      </form>
    </Dialog>
  )
}

function IdentityDialog({
  competitionId,
  entry,
  open,
  onClose,
}: {
  competitionId: string
  entry: OrganisationEntry | null
  open: boolean
  onClose: () => void
}) {
  const { t } = useTranslation('teams')
  const { t: tCommon } = useTranslation('common')
  const queryClient = useQueryClient()
  const formId = useId()
  const [name, setName] = useState(entry?.displayName ?? '')
  const [shortName, setShortName] = useState(entry?.shortName ?? '')
  const [logoMediaId, setLogoMediaId] = useState<string | null>(
    entry?.logoMediaId ?? null,
  )
  const [primaryColor, setPrimaryColor] = useState(entry?.primaryColor ?? '')
  const [secondaryColor, setSecondaryColor] = useState(
    entry?.secondaryColor ?? '',
  )

  useEffect(() => {
    if (!entry) {
      return
    }
    setName(entry.displayName)
    setShortName(entry.shortName ?? '')
    setLogoMediaId(entry.logoMediaId ?? null)
    setPrimaryColor(entry.primaryColor ?? '')
    setSecondaryColor(entry.secondaryColor ?? '')
  }, [entry])

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!entry) {
        return
      }
      if (name.trim() !== entry.displayName) {
        await renameCompetitionEntry(competitionId, entry.entryId, {
          displayName: name.trim(),
        })
      }
      await updateEntryPresentation(competitionId, entry.entryId, {
        shortName: shortName.trim() || null,
        logoMediaId,
        primaryColor: primaryColor.trim() || null,
        secondaryColor: secondaryColor.trim() || null,
      })
    },
    onSuccess: async () => {
      await invalidateAfterTeamsMutation(queryClient, competitionId)
      onClose()
    },
  })

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('identityDialogTitle')}
      closeLabel={tCommon('close')}
      closeDisabled={saveMutation.isPending}
      size="sm"
      footer={
        <button
          type="submit"
          form={formId}
          className="ds-btn ds-btn--primary"
          disabled={saveMutation.isPending || name.trim().length === 0}
        >
          {saveMutation.isPending ? (
            <PendingLabel>{t('saving')}</PendingLabel>
          ) : (
            t('saveIdentity')
          )}
        </button>
      }
    >
      <form
        id={formId}
        className="form"
        onSubmit={(event: FormEvent) => {
          event.preventDefault()
          if (name.trim().length === 0 || saveMutation.isPending) {
            return
          }
          saveMutation.mutate()
        }}
      >
        <IdentityFields
          name={name}
          shortName={shortName}
          logoMediaId={logoMediaId}
          primaryColor={primaryColor}
          secondaryColor={secondaryColor}
          disabled={saveMutation.isPending}
          onName={setName}
          onShortName={setShortName}
          onLogo={setLogoMediaId}
          onPrimary={setPrimaryColor}
          onSecondary={setSecondaryColor}
        />
        {saveMutation.isError && <MutationError error={saveMutation.error} />}
      </form>
    </Dialog>
  )
}

function IdentityFields({
  name,
  shortName,
  logoMediaId,
  primaryColor,
  secondaryColor,
  disabled,
  onName,
  onShortName,
  onLogo,
  onPrimary,
  onSecondary,
}: {
  name: string
  shortName: string
  logoMediaId: string | null
  primaryColor: string
  secondaryColor: string
  disabled: boolean
  onName: (value: string) => void
  onShortName: (value: string) => void
  onLogo: (value: string | null) => void
  onPrimary: (value: string) => void
  onSecondary: (value: string) => void
}) {
  const { t } = useTranslation('teams')

  return (
    <>
      <label className="field">
        {t('newEntryName')}
        <input
          value={name}
          onChange={(event) => onName(event.target.value)}
          disabled={disabled}
          placeholder={t('newEntryPlaceholder')}
          required
        />
      </label>
      <label className="field">
        {t('shortName')}
        <input
          value={shortName}
          onChange={(event) => onShortName(event.target.value)}
          disabled={disabled}
          maxLength={20}
        />
      </label>
      <LogoMediaField
        name={name.trim() || t('newEntryPlaceholder')}
        value={logoMediaId}
        onChange={onLogo}
        disabled={disabled}
        label={t('logo')}
      />
      <div className="form form--inline">
        <label className="field">
          {t('primaryColor')}
          <input
            value={primaryColor}
            onChange={(event) => onPrimary(event.target.value)}
            disabled={disabled}
            placeholder="#RRGGBB"
          />
        </label>
        <label className="field">
          {t('secondaryColor')}
          <input
            value={secondaryColor}
            onChange={(event) => onSecondary(event.target.value)}
            disabled={disabled}
            placeholder="#RRGGBB"
          />
        </label>
      </div>
    </>
  )
}

function TeamsPlateauReading({
  activeCount,
  occupyingCount,
  minimumTeams,
  maximumTeams,
}: {
  activeCount: number
  occupyingCount: number
  minimumTeams: number
  maximumTeams: number
}) {
  const { t } = useTranslation('teams')
  const availableSlots = Math.max(0, maximumTeams - occupyingCount)
  const belowMinimum = activeCount < minimumTeams
  const atCap = occupyingCount >= maximumTeams
  const missingMinimum = Math.max(0, minimumTeams - activeCount)
  const fillRatio =
    maximumTeams > 0 ? Math.min(1, activeCount / maximumTeams) : 0
  const markerRatio =
    maximumTeams > 0 ? Math.min(1, minimumTeams / maximumTeams) : 0
  const showMarker =
    minimumTeams > 0 && maximumTeams > 0 && minimumTeams < maximumTeams

  const statusTone = atCap
    ? 'cap'
    : belowMinimum
      ? missingMinimum === 1
        ? 'blocking'
        : 'warning'
      : 'ok'

  const statusLabel = atCap
    ? t('plateauCapReached')
    : belowMinimum
      ? t('plateauStillNeeded', { count: missingMinimum })
      : t('plateauMinimumReached')

  const gaugeAria = t('plateauGaugeAria', {
    count: activeCount,
    active: activeCount,
    min: minimumTeams,
    max: maximumTeams,
    available: t('plateauPlacesAvailable', { count: availableSlots }),
  })

  return (
    <div className="teams__plateau" role="status">
      <p
        className={`teams__plateau-status teams__plateau-status--${statusTone}`}
      >
        {statusLabel}
      </p>

      {maximumTeams > 0 && (
        <div className="teams__plateau-gaugeBlock">
          <div
            className="teams__plateau-gauge"
            role="progressbar"
            aria-valuemin={0}
            aria-valuemax={maximumTeams}
            aria-valuenow={activeCount}
            aria-valuetext={gaugeAria}
            data-tone={statusTone}
          >
            <div className="teams__plateau-gaugeTrack">
              <div
                className="teams__plateau-gaugeFill"
                style={{ width: `${fillRatio * 100}%` }}
              />
              {showMarker && (
                <span
                  className="teams__plateau-gaugeMarker"
                  style={{ left: `${markerRatio * 100}%` }}
                  aria-hidden="true"
                >
                  <span className="teams__plateau-gaugeMarkerLabel">
                    {t('plateauMinMarker', { min: minimumTeams })}
                  </span>
                </span>
              )}
            </div>
          </div>

          {!atCap && (
            <p className="teams__plateau-capacity">
              {t('plateauPlacesAvailable', { count: availableSlots })}
            </p>
          )}
        </div>
      )}
    </div>
  )
}

function tileStatusBadge(
  status: EntryStatus,
  t: (key: string) => string,
): ReactNode {
  if (status === 'Withdrawn') {
    return (
      <StatusBadge tone="warn" density="compact">
        {t('withdrawnBadge')}
      </StatusBadge>
    )
  }
  if (status === 'Qualified') {
    return (
      <StatusBadge tone="info" density="compact">
        {entryStatusLabel(status)}
      </StatusBadge>
    )
  }
  if (status === 'Eliminated') {
    return (
      <StatusBadge tone="done" density="compact">
        {entryStatusLabel(status)}
      </StatusBadge>
    )
  }
  return null
}
