import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import {
  addDeclaredParticipation,
  changeDeclaredParticipationCompositionStatus,
  fetchOrganisationView,
  removeDeclaredParticipation,
  setDeclaredParticipationJerseyNumber,
} from '../api'
import { queryKeys } from '../queryKeys'
import {
  EmptyState,
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
} from '../ui'
import type {
  CompositionStatus,
  DeclaredMember,
  DeclaredParticipation,
  MatchDetail,
  MatchSide,
  OrganisationEntry,
} from '../types'

/**
 * Championship match sheet — composition déclarée only (Lot 2).
 * No goals / subs / discipline. Effectif stays on Organisation (cas 7).
 */
export function MatchSheetPanel({ match }: { match: MatchDetail }) {
  const { t } = useTranslation('matches')
  const queryClient = useQueryClient()
  const canMutate = canMutateMatchSheet(match)

  const organisationQuery = useQuery({
    queryKey: queryKeys.competitions.organisation(match.competitionId),
    queryFn: () => fetchOrganisationView(match.competitionId),
  })

  const participations = match.declaredParticipations ?? []

  return (
    <section className="ds-panel" aria-labelledby="sheet-heading">
      <h2 className="matches-panel__head" id="sheet-heading">
        {t('sheet.heading')}
      </h2>
      <p className="matches-panel__meta">{t('sheet.hint')}</p>

      {!canMutate && (
        <p className="notice notice--info">{t('sheet.readOnly')}</p>
      )}

      {organisationQuery.isPending && <LoadingState />}
      {organisationQuery.isError && (
        <ErrorState error={organisationQuery.error} />
      )}
      {organisationQuery.data && (
        <div className="match-sheet__sides">
          <SheetSideColumn
            match={match}
            side="Home"
            sideLabel={t('detail.home')}
            entry={findEntry(
              organisationQuery.data.participants.entries,
              match.home.entryId,
            )}
            participations={participations.filter((row) => row.side === 'Home')}
            allParticipations={participations}
            canMutate={canMutate}
            queryClient={queryClient}
          />
          <SheetSideColumn
            match={match}
            side="Away"
            sideLabel={t('detail.away')}
            entry={findEntry(
              organisationQuery.data.participants.entries,
              match.away.entryId,
            )}
            participations={participations.filter((row) => row.side === 'Away')}
            allParticipations={participations}
            canMutate={canMutate}
            queryClient={queryClient}
          />
        </div>
      )}
    </section>
  )
}

export function canMutateMatchSheet(match: MatchDetail): boolean {
  if (match.status === 'Scheduled' || match.status === 'Postponed') {
    return true
  }

  return match.status === 'Finished' && !match.hasObservedLive
}

function findEntry(
  entries: OrganisationEntry[],
  entryId: string,
): OrganisationEntry | undefined {
  return entries.find((entry) => entry.entryId === entryId)
}

function playersOf(entry: OrganisationEntry | undefined): DeclaredMember[] {
  return (entry?.declaredMembers ?? []).filter(
    (member) => member.role === 'Player',
  )
}

async function invalidateMatchSheet(
  queryClient: QueryClient,
  match: MatchDetail,
) {
  await Promise.all([
    queryClient.invalidateQueries({
      queryKey: queryKeys.matches.detail(match.matchId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.matches.byStage(match.stageId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.organisation(match.competitionId),
    }),
  ])
}

function SheetSideColumn({
  match,
  side,
  sideLabel,
  entry,
  participations,
  allParticipations,
  canMutate,
  queryClient,
}: {
  match: MatchDetail
  side: MatchSide
  sideLabel: string
  entry: OrganisationEntry | undefined
  participations: DeclaredParticipation[]
  allParticipations: DeclaredParticipation[]
  canMutate: boolean
  queryClient: QueryClient
}) {
  const { t } = useTranslation('matches')
  const { t: tc } = useTranslation('common')
  const rosterHref = entry
    ? `/competitions/${match.competitionId}/organisation/entries/${entry.entryId}`
    : `/competitions/${match.competitionId}/organisation`

  const onSheetIds = new Set(allParticipations.map((row) => row.memberId))
  const eligible = playersOf(entry).filter(
    (member) => !onSheetIds.has(member.memberId),
  )

  const [memberId, setMemberId] = useState('')
  const [compositionStatus, setCompositionStatus] =
    useState<CompositionStatus>('Starter')
  const [jerseyDraft, setJerseyDraft] = useState('')
  const [pendingRemoveId, setPendingRemoveId] = useState<string | null>(null)
  const [jerseyEditId, setJerseyEditId] = useState<string | null>(null)
  const [jerseyEditValue, setJerseyEditValue] = useState('')

  const addMutation = useMutation({
    mutationFn: () => {
      const jersey = parseOptionalJersey(jerseyDraft)
      if (jersey === undefined) {
        throw new Error('Invalid jersey')
      }
      return addDeclaredParticipation(match.matchId, {
        memberId,
        side,
        compositionStatus,
        jerseyNumber: jersey,
      })
    },
    onSuccess: async () => {
      setMemberId('')
      setJerseyDraft('')
      setCompositionStatus('Starter')
      await invalidateMatchSheet(queryClient, match)
    },
  })

  const statusMutation = useMutation({
    mutationFn: ({
      id,
      status,
    }: {
      id: string
      status: CompositionStatus
    }) =>
      changeDeclaredParticipationCompositionStatus(match.matchId, id, status),
    onSuccess: async () => {
      await invalidateMatchSheet(queryClient, match)
    },
  })

  const jerseyMutation = useMutation({
    mutationFn: ({
      id,
      jerseyNumber,
    }: {
      id: string
      jerseyNumber: number | null
    }) => setDeclaredParticipationJerseyNumber(match.matchId, id, jerseyNumber),
    onSuccess: async () => {
      setJerseyEditId(null)
      setJerseyEditValue('')
      await invalidateMatchSheet(queryClient, match)
    },
  })

  const removeMutation = useMutation({
    mutationFn: (id: string) =>
      removeDeclaredParticipation(match.matchId, id),
    onSuccess: async () => {
      setPendingRemoveId(null)
      await invalidateMatchSheet(queryClient, match)
    },
    onError: () => {
      setPendingRemoveId(null)
    },
  })

  const mutationError =
    addMutation.error ??
    statusMutation.error ??
    jerseyMutation.error ??
    removeMutation.error

  const busy =
    addMutation.isPending ||
    statusMutation.isPending ||
    jerseyMutation.isPending ||
    removeMutation.isPending

  return (
    <div className="match-sheet__side" aria-label={sideLabel}>
      <h3 className="match-sheet__side-title">{sideLabel}</h3>
      {entry && (
        <p className="match-sheet__side-meta">
          <Link className="match-sheet__roster-link" to={rosterHref}>
            {t('sheet.openRoster', { name: entry.displayName })}
          </Link>
        </p>
      )}

      {participations.length === 0 ? (
        <EmptyState title={t('sheet.emptyTitle')}>
          {t('sheet.emptyBody')}
        </EmptyState>
      ) : (
        <ul className="match-sheet__list">
          {participations.map((row) => (
            <li key={row.memberId}>
              <div className="match-sheet__row">
                <div className="match-sheet__identity">
                  <span className="match-sheet__name">
                    {row.displayName ?? row.memberId}
                  </span>
                  <span className="match-sheet__jersey">
                    {row.jerseyNumber != null
                      ? t('sheet.jerseyValue', { number: row.jerseyNumber })
                      : t('sheet.jerseyNone')}
                  </span>
                </div>

                {canMutate &&
                  pendingRemoveId !== row.memberId &&
                  jerseyEditId !== row.memberId && (
                    <div className="match-sheet__row-actions">
                      <button
                        type="button"
                        className={
                          row.compositionStatus === 'Starter'
                            ? 'match-sheet__status match-sheet__status--active'
                            : 'match-sheet__status'
                        }
                        disabled={busy || row.compositionStatus === 'Starter'}
                        onClick={() =>
                          statusMutation.mutate({
                            id: row.memberId,
                            status: 'Starter',
                          })
                        }
                      >
                        {t('sheet.starter')}
                      </button>
                      <button
                        type="button"
                        className={
                          row.compositionStatus === 'Bench'
                            ? 'match-sheet__status match-sheet__status--active'
                            : 'match-sheet__status'
                        }
                        disabled={busy || row.compositionStatus === 'Bench'}
                        onClick={() =>
                          statusMutation.mutate({
                            id: row.memberId,
                            status: 'Bench',
                          })
                        }
                      >
                        {t('sheet.bench')}
                      </button>
                      <button
                        type="button"
                        className="organisation-action"
                        disabled={busy}
                        onClick={() => {
                          setPendingRemoveId(null)
                          setJerseyEditId(row.memberId)
                          setJerseyEditValue(
                            row.jerseyNumber != null
                              ? String(row.jerseyNumber)
                              : '',
                          )
                        }}
                      >
                        {t('sheet.editJersey')}
                      </button>
                      <button
                        type="button"
                        className="organisation-action"
                        disabled={busy}
                        onClick={() => {
                          setJerseyEditId(null)
                          setPendingRemoveId(row.memberId)
                        }}
                      >
                        {t('sheet.remove')}
                      </button>
                    </div>
                  )}

                {canMutate && jerseyEditId === row.memberId && (
                  <form
                    className="form match-sheet__jersey-form"
                    onSubmit={(event: FormEvent) => {
                      event.preventDefault()
                      if (jerseyMutation.isPending) {
                        return
                      }
                      const jerseyNumber = parseOptionalJersey(jerseyEditValue)
                      if (jerseyNumber === undefined) {
                        return
                      }
                      jerseyMutation.mutate({
                        id: row.memberId,
                        jerseyNumber,
                      })
                    }}
                  >
                    <label className="field">
                      {t('sheet.jersey')}
                      <input
                        inputMode="numeric"
                        value={jerseyEditValue}
                        onChange={(event) =>
                          setJerseyEditValue(event.target.value)
                        }
                        disabled={jerseyMutation.isPending}
                        placeholder={t('sheet.jerseyPlaceholder')}
                      />
                    </label>
                    <div className="match-sheet__confirm-actions">
                      <button
                        type="submit"
                        className="ds-btn ds-btn--primary"
                        disabled={jerseyMutation.isPending}
                      >
                        {jerseyMutation.isPending ? (
                          <PendingLabel>{t('sheet.saving')}</PendingLabel>
                        ) : (
                          t('sheet.saveJersey')
                        )}
                      </button>
                      <button
                        type="button"
                        className="ds-btn ds-btn--ghost"
                        disabled={jerseyMutation.isPending}
                        onClick={() => {
                          setJerseyEditId(null)
                          setJerseyEditValue('')
                        }}
                      >
                        {tc('cancel')}
                      </button>
                    </div>
                  </form>
                )}

                {canMutate && pendingRemoveId === row.memberId && (
                  <div
                    className="match-sheet__confirm notice notice--warning"
                    role="group"
                    aria-label={t('sheet.removeConsequence', {
                      name: row.displayName ?? row.memberId,
                    })}
                  >
                    <p>
                      {t('sheet.removeConsequence', {
                        name: row.displayName ?? row.memberId,
                      })}
                    </p>
                    <div className="match-sheet__confirm-actions">
                      <button
                        type="button"
                        className="ds-btn ds-btn--destructive"
                        disabled={removeMutation.isPending}
                        onClick={() => removeMutation.mutate(row.memberId)}
                      >
                        {removeMutation.isPending ? (
                          <PendingLabel>{t('sheet.removing')}</PendingLabel>
                        ) : (
                          t('sheet.confirmRemove')
                        )}
                      </button>
                      <button
                        type="button"
                        className="ds-btn ds-btn--ghost"
                        disabled={removeMutation.isPending}
                        onClick={() => setPendingRemoveId(null)}
                      >
                        {tc('cancel')}
                      </button>
                    </div>
                  </div>
                )}

                {!canMutate && (
                  <span className="match-sheet__status-label">
                    {row.compositionStatus === 'Starter'
                      ? t('sheet.starter')
                      : t('sheet.bench')}
                  </span>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      {mutationError && <MutationError error={mutationError} />}

      {canMutate && (
        <form
          className="form match-sheet__add"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            if (memberId.length === 0 || addMutation.isPending) {
              return
            }
            addMutation.mutate()
          }}
        >
          {eligible.length === 0 ? (
            <p className="match-sheet__no-eligible">
              {playersOf(entry).length === 0
                ? t('sheet.noRoster')
                : t('sheet.noneEligible')}{' '}
              <Link to={rosterHref}>{t('sheet.manageRoster')}</Link>
            </p>
          ) : (
            <>
              <label className="field">
                {t('sheet.addPlayer')}
                <select
                  value={memberId}
                  onChange={(event) => setMemberId(event.target.value)}
                  disabled={addMutation.isPending}
                  required
                >
                  <option value="">{t('sheet.addPlaceholder')}</option>
                  {eligible.map((member) => (
                    <option key={member.memberId} value={member.memberId}>
                      {member.displayName}
                    </option>
                  ))}
                </select>
              </label>
              <fieldset className="match-sheet__status-fieldset">
                <legend>{t('sheet.composition')}</legend>
                <label className="match-sheet__radio">
                  <input
                    type="radio"
                    name={`composition-${side}`}
                    checked={compositionStatus === 'Starter'}
                    onChange={() => setCompositionStatus('Starter')}
                    disabled={addMutation.isPending}
                  />
                  {t('sheet.starter')}
                </label>
                <label className="match-sheet__radio">
                  <input
                    type="radio"
                    name={`composition-${side}`}
                    checked={compositionStatus === 'Bench'}
                    onChange={() => setCompositionStatus('Bench')}
                    disabled={addMutation.isPending}
                  />
                  {t('sheet.bench')}
                </label>
              </fieldset>
              <label className="field">
                {t('sheet.jersey')}
                <input
                  inputMode="numeric"
                  value={jerseyDraft}
                  onChange={(event) => setJerseyDraft(event.target.value)}
                  disabled={addMutation.isPending}
                  placeholder={t('sheet.jerseyPlaceholder')}
                />
              </label>
              <button
                type="submit"
                className="ds-btn ds-btn--primary"
                disabled={addMutation.isPending || memberId.length === 0}
              >
                {addMutation.isPending ? (
                  <PendingLabel>{t('sheet.adding')}</PendingLabel>
                ) : (
                  t('sheet.addAction')
                )}
              </button>
            </>
          )}
        </form>
      )}
    </div>
  )
}

function parseOptionalJersey(raw: string): number | null | undefined {
  const trimmed = raw.trim()
  if (trimmed.length === 0) {
    return null
  }

  if (!/^\d+$/.test(trimmed)) {
    return undefined
  }

  return Number(trimmed)
}
