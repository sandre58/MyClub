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
  const showPlateauReading =
    data.status === 'Draft' || data.status === 'Ready'
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

  function goToSelection(nextIds: string[]) {
    setSelectedIds(nextIds)
    if (nextIds.length === 1) {
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
      goToSelection([entryId])
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

  return (
    <div className="teams">
      <div className="teams__layout">
        <div className="teams__main">
          <header className="teams__head">
            <div className="teams__title-cluster">
              <h1 className="teams__title">{t('title')}</h1>
              {showPlateauReading && (
                <div className="teams__plateau" role="status">
                  <div className="teams__plateau-cards">
                    <div className="teams__plateau-card">
                      <p className="teams__plateau-cardLabel">
                        {t('plateauLabel')}
                      </p>
                      <p className="teams__plateau-cardValue">
                        <span className="ds-num ds-num-counter">
                          {data.participants.occupyingCount}
                        </span>
                        <span className="teams__plateau-cardMax" aria-hidden="true">
                          /{data.regulation.maximumTeams}
                        </span>
                      </p>
                    </div>

                    {missingMinimum > 0 ? (
                      <p
                        className={
                          missingMinimum === 1
                            ? 'ds-notice ds-notice--danger teams__plateau-notice'
                            : 'ds-notice ds-notice--warning teams__plateau-notice'
                        }
                      >
                        {t('plateauMissing', { count: missingMinimum })}
                      </p>
                    ) : atCap ? (
                      <p className="ds-notice ds-notice--info teams__plateau-notice">
                        {t('maxHelper', {
                          count: data.participants.occupyingCount,
                        })}
                      </p>
                    ) : (
                      <p className="teams__plateau-subtle">
                        {t('plateauMinimumOk', {
                          count: data.participants.activeCount,
                          min: data.regulation.minimumTeams,
                        })}
                      </p>
                    )}
                  </div>
                </div>
              )}
            </div>
            {selectedCount >= 1 ? (
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
            ) : (
              <span className="teams__head-fill" aria-hidden="true" />
            )}
            <button
              type="button"
              className="ds-btn ds-btn--primary teams__add"
              disabled={!canAdd}
              title={canAdd ? t('addAction') : t('addDisabledHint')}
              onClick={() => setAddOpen(true)}
            >
              <PlusIcon size="sm" />
              {t('addAction')}
            </button>
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
                    <button
                      type="button"
                      className="teams-tile__body"
                      aria-label={entry.displayName}
                      onClick={() => onTileBody(entry.entryId)}
                    >
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
                    </button>
                  </article>
                </li>
              )
            })}
            {Array.from({ length: emptyCount }, (_, index) => (
              <li key={`empty-${index}`}>
                <article className="teams-tile teams-tile--empty">
                  <div className="teams-tile__chrome" aria-hidden="true" />
                  <button
                    type="button"
                    className="teams-tile__body"
                    aria-label={t('emptyTileAria')}
                    onClick={() => setAddOpen(true)}
                  >
                    <PlusIcon size="lg" />
                    <span>{t('emptyTile')}</span>
                  </button>
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
            <TeamRosterDrawer data={data} entryId={drawerEntryId} />
          )}
        </aside>
      </div>

      {addOpen && (
        <AddEntryDialog
          data={data}
          onClose={() => setAddOpen(false)}
        />
      )}
      {identityEntry && (
        <IdentityDialog
          competitionId={data.competitionId}
          entry={identityEntry}
          onClose={() => setIdentityEntryId(null)}
        />
      )}
    </div>
  )
}

function AddEntryDialog({
  data,
  onClose,
}: {
  data: OrganisationView
  onClose: () => void
}) {
  const { t } = useTranslation('teams')
  const queryClient = useQueryClient()
  const [displayName, setDisplayName] = useState('')
  const [shortName, setShortName] = useState('')
  const [logoMediaId, setLogoMediaId] = useState<string | null>(null)
  const [primaryColor, setPrimaryColor] = useState('')
  const [secondaryColor, setSecondaryColor] = useState('')

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
    <TeamsDialog title={t('addDialogTitle')} onClose={onClose}>
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
        <button
          type="submit"
          className="ds-btn ds-btn--primary"
          disabled={addMutation.isPending || displayName.trim().length === 0}
        >
          {addMutation.isPending ? (
            <PendingLabel>{t('adding')}</PendingLabel>
          ) : (
            t('add')
          )}
        </button>
        {addMutation.isError && <MutationError error={addMutation.error} />}
      </form>
    </TeamsDialog>
  )
}

function IdentityDialog({
  competitionId,
  entry,
  onClose,
}: {
  competitionId: string
  entry: OrganisationEntry
  onClose: () => void
}) {
  const { t } = useTranslation('teams')
  const queryClient = useQueryClient()
  const [name, setName] = useState(entry.displayName)
  const [shortName, setShortName] = useState(entry.shortName ?? '')
  const [logoMediaId, setLogoMediaId] = useState<string | null>(
    entry.logoMediaId ?? null,
  )
  const [primaryColor, setPrimaryColor] = useState(entry.primaryColor ?? '')
  const [secondaryColor, setSecondaryColor] = useState(entry.secondaryColor ?? '')

  const saveMutation = useMutation({
    mutationFn: async () => {
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
    <TeamsDialog title={t('identityDialogTitle')} onClose={onClose}>
      <form
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
        <button
          type="submit"
          className="ds-btn ds-btn--primary"
          disabled={saveMutation.isPending || name.trim().length === 0}
        >
          {saveMutation.isPending ? (
            <PendingLabel>{t('saving')}</PendingLabel>
          ) : (
            t('saveIdentity')
          )}
        </button>
        {saveMutation.isError && <MutationError error={saveMutation.error} />}
      </form>
    </TeamsDialog>
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

function TeamsDialog({
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
    <div className="teams-dialog">
      <button
        type="button"
        className="teams-dialog__backdrop"
        aria-label={t('close')}
        onClick={onClose}
        tabIndex={-1}
      />
      <div
        className="teams-dialog__panel ds-overlay"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        <header className="teams-dialog__header">
          <h2 id={titleId} className="teams-dialog__title">
            {title}
          </h2>
          <button type="button" className="ds-btn ds-btn--ghost" onClick={onClose}>
            {t('close')}
          </button>
        </header>
        {children}
      </div>
    </div>
  )
}
