import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import {
  addDeclaredMember,
  fetchOrganisationView,
  removeDeclaredMember,
  renameDeclaredMember,
} from '../api'
import { TeamCrest } from '../design-system/TeamCrest'
import { TeamsIcon } from '../design-system/icons/overviewIcons'
import { queryKeys } from '../queryKeys'
import {
  EmptyState,
  EntryStatusBadge,
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
} from '../ui'
import {
  MEMBER_DISPLAY_NAME_MAX_LENGTH,
  type CompetitionStatus,
  type DeclaredMember,
  type EntryStatus,
  type OrganisationEntry,
  type OrganisationView,
} from '../types'
import './organisation.css'

/**
 * Players declared on this competition entry — not a generic squad manager.
 * V1: add / rename / remove Player while the cycle and entry are mutable.
 * No Staff / role UI (cas 7: effectif stays on Organisation, not Match).
 */
export function EntryRosterPage() {
  const { competitionId = '', entryId = '' } = useParams()

  const query = useQuery({
    queryKey: queryKeys.competitions.organisation(competitionId),
    queryFn: () => fetchOrganisationView(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page page--organisation">
      {query.isPending && !query.data && <LoadingState />}
      {query.isError && !query.data && <ErrorState error={query.error} />}
      {query.data && (
        <EntryRosterView
          data={query.data}
          entryId={entryId}
        />
      )}
    </main>
  )
}

async function invalidateAfterRosterMutation(
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

function playersOf(entry: OrganisationEntry): DeclaredMember[] {
  return (entry.declaredMembers ?? []).filter(
    (member) => member.role === 'Player',
  )
}

function canMutateRoster(
  competitionStatus: CompetitionStatus,
  entryStatus: EntryStatus,
): boolean {
  if (entryStatus !== 'Active') {
    return false
  }

  return (
    competitionStatus === 'Draft' ||
    competitionStatus === 'Ready' ||
    competitionStatus === 'Running' ||
    competitionStatus === 'Suspended'
  )
}

function EntryRosterView({
  data,
  entryId,
}: {
  data: OrganisationView
  entryId: string
}) {
  const { t } = useTranslation('organisation')
  const { t: tc } = useTranslation('common')
  const queryClient = useQueryClient()
  const nameRef = useRef<HTMLInputElement>(null)
  const [displayName, setDisplayName] = useState('')
  const [pendingRemoveId, setPendingRemoveId] = useState<string | null>(null)
  const [pendingRenameId, setPendingRenameId] = useState<string | null>(null)
  const [renameDraft, setRenameDraft] = useState('')

  const organisationHref = `/competitions/${data.competitionId}/organisation`
  const entry = data.participants.entries.find(
    (candidate) => candidate.entryId === entryId,
  )

  const addMutation = useMutation({
    mutationFn: (name: string) =>
      addDeclaredMember(data.competitionId, entryId, {
        displayName: name,
        role: 'Player',
      }),
    onSuccess: async () => {
      setDisplayName('')
      await invalidateAfterRosterMutation(queryClient, data.competitionId)
      nameRef.current?.focus()
    },
  })

  const renameMutation = useMutation({
    mutationFn: ({
      memberId,
      name,
    }: {
      memberId: string
      name: string
    }) =>
      renameDeclaredMember(data.competitionId, entryId, memberId, {
        displayName: name,
      }),
    onSuccess: async () => {
      setPendingRenameId(null)
      setRenameDraft('')
      await invalidateAfterRosterMutation(queryClient, data.competitionId)
    },
  })

  const removeMutation = useMutation({
    mutationFn: (memberId: string) =>
      removeDeclaredMember(data.competitionId, entryId, memberId),
    onSuccess: async () => {
      setPendingRemoveId(null)
      await invalidateAfterRosterMutation(queryClient, data.competitionId)
    },
    onError: () => {
      setPendingRemoveId(null)
    },
  })

  function clearRowEditors() {
    setPendingRemoveId(null)
    setPendingRenameId(null)
    setRenameDraft('')
  }

  if (!entry) {
    return (
      <div className="organisation">
        <header className="organisation__page-head">
          <Link className="organisation__back" to={organisationHref}>
            <span aria-hidden="true">←</span>
            {t('roster.back')}
          </Link>
        </header>
        <p className="ds-notice ds-notice--danger" role="alert">
          {t('roster.entryMissing')}
        </p>
      </div>
    )
  }

  const players = playersOf(entry)
  const canMutate = canMutateRoster(data.status, entry.status)
  const mutationError =
    addMutation.error ?? renameMutation.error ?? removeMutation.error
  const rowBusy =
    addMutation.isPending ||
    renameMutation.isPending ||
    removeMutation.isPending

  return (
    <div className="organisation">
      <header className="organisation__page-head">
        <Link className="organisation__back" to={organisationHref}>
          <span aria-hidden="true">←</span>
          {t('roster.back')}
        </Link>
        <h1 className="organisation__title">{entry.displayName}</h1>
      </header>

      <section
        className="ds-panel organisation-roster__identity"
        aria-label={entry.displayName}
      >
        <TeamCrest
          name={entry.displayName}
          logoMediaId={entry.logoMediaId}
          primaryColor={entry.primaryColor}
          className="organisation-crest"
        />
        <p className="organisation-entry__identity">
          <span className="organisation-entry__name">{entry.displayName}</span>
          <EntryStatusBadge status={entry.status} />
        </p>
      </section>

      <section className="ds-panel" aria-labelledby="roster-players-heading">
        <PanelHead id="roster-players-heading">
          {t('roster.playersHeading')}
        </PanelHead>

        {!canMutate && (
          <p className="ds-notice ds-notice--info">{t('roster.readOnly')}</p>
        )}

        {players.length === 0 ? (
          <EmptyState title={t('roster.emptyTitle')}>
            {t('roster.emptyBody')}
          </EmptyState>
        ) : (
          <ul className="organisation-roster__list">
            {players.map((player) => (
              <li key={player.memberId}>
                <div className="organisation-roster__row">
                  <span className="organisation-roster__name">
                    {player.displayName}
                  </span>
                  {canMutate &&
                    pendingRemoveId !== player.memberId &&
                    pendingRenameId !== player.memberId && (
                      <div className="organisation-roster__row-actions">
                        <button
                          type="button"
                          className="organisation-action"
                          disabled={rowBusy}
                          onClick={() => {
                            addMutation.reset()
                            renameMutation.reset()
                            removeMutation.reset()
                            setPendingRemoveId(null)
                            setPendingRenameId(player.memberId)
                            setRenameDraft(player.displayName)
                          }}
                        >
                          {t('roster.rename')}
                        </button>
                        <button
                          type="button"
                          className="organisation-action"
                          disabled={rowBusy}
                          onClick={() => {
                            addMutation.reset()
                            renameMutation.reset()
                            removeMutation.reset()
                            setPendingRenameId(null)
                            setRenameDraft('')
                            setPendingRemoveId(player.memberId)
                          }}
                        >
                          {t('roster.remove')}
                        </button>
                      </div>
                    )}
                  {canMutate && pendingRenameId === player.memberId && (
                    <form
                      className="form organisation-roster__rename"
                      onSubmit={(event: FormEvent) => {
                        event.preventDefault()
                        const name = renameDraft.trim()
                        if (
                          name.length === 0 ||
                          renameMutation.isPending ||
                          name === player.displayName
                        ) {
                          return
                        }
                        addMutation.reset()
                        removeMutation.reset()
                        renameMutation.mutate({
                          memberId: player.memberId,
                          name,
                        })
                      }}
                    >
                      <label className="field">
                        {t('roster.renameName')}
                        <input
                          value={renameDraft}
                          onChange={(event) =>
                            setRenameDraft(event.target.value)
                          }
                          disabled={renameMutation.isPending}
                          maxLength={MEMBER_DISPLAY_NAME_MAX_LENGTH}
                          required
                          autoFocus
                        />
                      </label>
                      <div className="organisation-roster__confirm-actions">
                        <button
                          type="submit"
                          className="ds-btn ds-btn--primary"
                          disabled={
                            renameMutation.isPending ||
                            renameDraft.trim().length === 0 ||
                            renameDraft.trim() === player.displayName
                          }
                        >
                          {renameMutation.isPending ? (
                            <PendingLabel>{t('roster.renaming')}</PendingLabel>
                          ) : (
                            t('roster.confirmRename')
                          )}
                        </button>
                        <button
                          type="button"
                          className="ds-btn ds-btn--ghost"
                          disabled={renameMutation.isPending}
                          onClick={() => clearRowEditors()}
                        >
                          {tc('cancel')}
                        </button>
                      </div>
                    </form>
                  )}
                  {canMutate && pendingRemoveId === player.memberId && (
                    <div
                      className="organisation-roster__confirm ds-notice ds-notice--warning"
                      role="group"
                      aria-label={t('roster.removeConsequence', {
                        name: player.displayName,
                      })}
                    >
                      <p>
                        {t('roster.removeConsequence', {
                          name: player.displayName,
                        })}
                      </p>
                      <div className="organisation-roster__confirm-actions">
                        <button
                          type="button"
                          className="ds-btn ds-btn--destructive"
                          disabled={removeMutation.isPending}
                          onClick={() =>
                            removeMutation.mutate(player.memberId)
                          }
                        >
                          {removeMutation.isPending ? (
                            <PendingLabel>{t('roster.removing')}</PendingLabel>
                          ) : (
                            t('roster.confirmRemove')
                          )}
                        </button>
                        <button
                          type="button"
                          className="ds-btn ds-btn--ghost"
                          disabled={removeMutation.isPending}
                          onClick={() => clearRowEditors()}
                        >
                          {tc('cancel')}
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}

        {mutationError && <MutationError error={mutationError} />}

        {canMutate && (
          <form
            className="form organisation-roster__add"
            onSubmit={(event: FormEvent) => {
              event.preventDefault()
              const name = displayName.trim()
              if (name.length === 0 || addMutation.isPending) {
                return
              }
              clearRowEditors()
              renameMutation.reset()
              removeMutation.reset()
              addMutation.mutate(name)
            }}
          >
            <label className="field">
              {t('roster.addName')}
              <input
                ref={nameRef}
                value={displayName}
                onChange={(event) => setDisplayName(event.target.value)}
                disabled={addMutation.isPending}
                placeholder={t('roster.addPlaceholder')}
                maxLength={MEMBER_DISPLAY_NAME_MAX_LENGTH}
                required
              />
            </label>
            <button
              type="submit"
              className="ds-btn ds-btn--primary"
              disabled={addMutation.isPending || displayName.trim().length === 0}
            >
              {addMutation.isPending ? (
                <PendingLabel>{t('roster.adding')}</PendingLabel>
              ) : (
                t('roster.addAction')
              )}
            </button>
          </form>
        )}
      </section>
    </div>
  )
}

function PanelHead({
  id,
  children,
}: {
  id: string
  children: string
}) {
  return (
    <h2 className="organisation-panel__head" id={id}>
      <span className="organisation-panel__icon" aria-hidden="true">
        <TeamsIcon size="md" />
      </span>
      {children}
    </h2>
  )
}
