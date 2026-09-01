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
  correctRecordedDisciplinaryEvent,
  fetchOrganisationView,
  recordDisciplinaryEvent,
  removeRecordedDisciplinaryEvent,
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
  DeclaredParticipation,
  DisciplinaryType,
  MatchDetail,
  RecordDisciplinaryEventRequest,
  RecordedDisciplinaryEvent,
} from '../types'

/**
 * Nominative discipline panel (Lot 1) — faits ≠ score ≠ présence ≠ conséquences.
 * Types = Organisation AllowedTypes ; cible = toute personne sur la feuille.
 */
export function MatchDisciplinaryPanel({ match }: { match: MatchDetail }) {
  const { t } = useTranslation('matches')
  const { t: tc } = useTranslation('common')
  const queryClient = useQueryClient()
  const canMutate = canMutateRecordedDisciplinaryEvents(match)
  const sheet = match.declaredParticipations ?? []
  const events = match.recordedDisciplinaryEvents ?? []

  const organisationQuery = useQuery({
    queryKey: queryKeys.competitions.organisation(match.competitionId),
    queryFn: () => fetchOrganisationView(match.competitionId),
  })

  const allowedTypes = organisationQuery.data?.regulation.allowedTypes ?? []
  const catalogueReady = organisationQuery.isSuccess
  const noneAllowed = catalogueReady && allowedTypes.length === 0
  const canCreate =
    canMutate && sheet.length > 0 && catalogueReady && allowedTypes.length > 0

  const [memberId, setMemberId] = useState('')
  const [type, setType] = useState<DisciplinaryType | ''>('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [pendingRemoveId, setPendingRemoveId] = useState<string | null>(null)

  const createMutation = useMutation({
    mutationFn: (request: RecordDisciplinaryEventRequest) =>
      recordDisciplinaryEvent(match.matchId, request),
    onSuccess: async () => {
      resetForm()
      await invalidateMatchDiscipline(queryClient, match)
    },
  })

  const correctMutation = useMutation({
    mutationFn: ({
      disciplinaryEventId,
      request,
    }: {
      disciplinaryEventId: string
      request: RecordDisciplinaryEventRequest
    }) =>
      correctRecordedDisciplinaryEvent(
        match.matchId,
        disciplinaryEventId,
        request,
      ),
    onSuccess: async () => {
      setEditingId(null)
      resetForm()
      await invalidateMatchDiscipline(queryClient, match)
    },
  })

  const removeMutation = useMutation({
    mutationFn: (disciplinaryEventId: string) =>
      removeRecordedDisciplinaryEvent(match.matchId, disciplinaryEventId),
    onSuccess: async () => {
      setPendingRemoveId(null)
      await invalidateMatchDiscipline(queryClient, match)
    },
    onError: () => {
      setPendingRemoveId(null)
    },
  })

  function resetForm() {
    setMemberId('')
    setType('')
  }

  function beginEdit(evt: RecordedDisciplinaryEvent) {
    setPendingRemoveId(null)
    setEditingId(evt.disciplinaryEventId)
    setMemberId(evt.memberId)
    setType(evt.type)
  }

  const mutationError =
    createMutation.error ?? correctMutation.error ?? removeMutation.error
  const busy =
    createMutation.isPending ||
    correctMutation.isPending ||
    removeMutation.isPending

  return (
    <section className="ds-panel" aria-labelledby="discipline-heading">
      <h2 className="matches-panel__head" id="discipline-heading">
        {t('discipline.heading')}
      </h2>
      <p className="matches-panel__meta">{t('discipline.hint')}</p>

      {!canMutate && (
        <p className="ds-notice ds-notice--info">{t('discipline.readOnly')}</p>
      )}

      {organisationQuery.isPending && <LoadingState />}
      {organisationQuery.isError && (
        <ErrorState error={organisationQuery.error} />
      )}

      {canMutate && catalogueReady && sheet.length === 0 && (
        <p className="ds-notice ds-notice--info">{t('discipline.needSheet')}</p>
      )}

      {canMutate && noneAllowed && (
        <p className="ds-notice ds-notice--info">
          {t('discipline.noneAllowed')}{' '}
          <Link to={`/competitions/${match.competitionId}/organisation`}>
            {t('discipline.configureRegulation')}
          </Link>
        </p>
      )}

      {events.length === 0 ? (
        <EmptyState title={t('discipline.emptyTitle')}>
          {t('discipline.emptyBody')}
        </EmptyState>
      ) : (
        <ul className="match-discipline__list">
          {events.map((evt) => {
            const participation = sheet.find(
              (row) => row.memberId === evt.memberId,
            )
            return (
              <li key={evt.disciplinaryEventId}>
                <div className="match-discipline__row">
                  <div className="match-discipline__identity">
                    <span className="match-discipline__fact">
                      {typeLabel(t, evt.type)}
                      {' · '}
                      {evt.memberDisplayName ?? evt.memberId}
                    </span>
                    {participation && (
                      <span className="match-discipline__meta">
                        {participation.side === 'Home'
                          ? t('detail.home')
                          : t('detail.away')}
                      </span>
                    )}
                  </div>

                  {canMutate &&
                    !noneAllowed &&
                    editingId !== evt.disciplinaryEventId &&
                    pendingRemoveId !== evt.disciplinaryEventId && (
                      <div className="match-discipline__row-actions">
                        <button
                          type="button"
                          className="organisation-action"
                          disabled={busy}
                          onClick={() => beginEdit(evt)}
                        >
                          {t('discipline.correct')}
                        </button>
                        <button
                          type="button"
                          className="organisation-action"
                          disabled={busy}
                          onClick={() => {
                            setEditingId(null)
                            setPendingRemoveId(evt.disciplinaryEventId)
                          }}
                        >
                          {t('discipline.remove')}
                        </button>
                      </div>
                    )}

                  {canMutate &&
                    !noneAllowed &&
                    editingId === evt.disciplinaryEventId && (
                      <DisciplinaryForm
                        memberId={memberId}
                        type={type}
                        sheet={sheet}
                        allowedTypes={allowedTypes}
                        pending={correctMutation.isPending}
                        submitLabel={t('discipline.saveCorrect')}
                        pendingLabel={t('discipline.saving')}
                        onMemberChange={setMemberId}
                        onTypeChange={setType}
                        onCancel={() => {
                          setEditingId(null)
                          resetForm()
                        }}
                        onSubmit={(request) =>
                          correctMutation.mutate({
                            disciplinaryEventId: evt.disciplinaryEventId,
                            request,
                          })
                        }
                      />
                    )}

                  {canMutate &&
                    pendingRemoveId === evt.disciplinaryEventId && (
                      <div
                        className="match-discipline__confirm ds-notice ds-notice--warning"
                        role="group"
                      >
                        <p>
                          {t('discipline.removeConsequence', {
                            type: typeLabel(t, evt.type),
                            name: evt.memberDisplayName ?? evt.memberId,
                          })}
                        </p>
                        <div className="match-discipline__confirm-actions">
                          <button
                            type="button"
                            className="ds-btn ds-btn--destructive"
                            disabled={removeMutation.isPending}
                            onClick={() =>
                              removeMutation.mutate(evt.disciplinaryEventId)
                            }
                          >
                            {removeMutation.isPending ? (
                              <PendingLabel>
                                {t('discipline.removing')}
                              </PendingLabel>
                            ) : (
                              t('discipline.confirmRemove')
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
                </div>
              </li>
            )
          })}
        </ul>
      )}

      {mutationError && <MutationError error={mutationError} />}

      {canCreate && editingId === null && (
        <DisciplinaryForm
          memberId={memberId}
          type={type}
          sheet={sheet}
          allowedTypes={allowedTypes}
          pending={createMutation.isPending}
          submitLabel={t('discipline.addAction')}
          pendingLabel={t('discipline.adding')}
          onMemberChange={setMemberId}
          onTypeChange={setType}
          onSubmit={(request) => createMutation.mutate(request)}
        />
      )}
    </section>
  )
}

/** Domain CanMutateDisciplinaryEventsFreely — Create/Remove/Correct UI V1 (#4-like). */
export function canMutateRecordedDisciplinaryEvents(
  match: MatchDetail,
): boolean {
  if (
    match.status === 'Scheduled' ||
    match.status === 'Postponed' ||
    match.status === 'Live'
  ) {
    return true
  }

  return match.status === 'Finished' && !match.hasObservedLive
}

function typeLabel(
  t: (key: string) => string,
  type: DisciplinaryType,
): string {
  switch (type) {
    case 'Yellow':
      return t('discipline.typeYellow')
    case 'Red':
      return t('discipline.typeRed')
    case 'White':
      return t('discipline.typeWhite')
  }
}

async function invalidateMatchDiscipline(
  queryClient: QueryClient,
  match: MatchDetail,
) {
  await queryClient.invalidateQueries({
    queryKey: queryKeys.matches.detail(match.matchId),
  })
}

function DisciplinaryForm({
  memberId,
  type,
  sheet,
  allowedTypes,
  pending,
  submitLabel,
  pendingLabel,
  onMemberChange,
  onTypeChange,
  onCancel,
  onSubmit,
}: {
  memberId: string
  type: DisciplinaryType | ''
  sheet: DeclaredParticipation[]
  allowedTypes: DisciplinaryType[]
  pending: boolean
  submitLabel: string
  pendingLabel: string
  onMemberChange: (id: string) => void
  onTypeChange: (type: DisciplinaryType | '') => void
  onCancel?: () => void
  onSubmit: (request: RecordDisciplinaryEventRequest) => void
}) {
  const { t } = useTranslation('matches')
  const { t: tc } = useTranslation('common')

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!memberId || !type) {
      return
    }

    onSubmit({ memberId, type })
  }

  return (
    <form className="match-discipline__form" onSubmit={handleSubmit}>
      <label className="field">
        <span className="field__label">{t('discipline.member')}</span>
        <select
          className="field__control"
          value={memberId}
          required
          disabled={pending}
          onChange={(e) => onMemberChange(e.target.value)}
        >
          <option value="">{t('discipline.memberPlaceholder')}</option>
          {sheet.map((row) => (
            <option key={row.memberId} value={row.memberId}>
              {row.displayName ?? row.memberId}
              {' · '}
              {row.side === 'Home' ? t('detail.home') : t('detail.away')}
            </option>
          ))}
        </select>
      </label>

      <label className="field">
        <span className="field__label">{t('discipline.type')}</span>
        <select
          className="field__control"
          value={type}
          required
          disabled={pending}
          onChange={(e) =>
            onTypeChange(e.target.value as DisciplinaryType | '')
          }
        >
          <option value="">{t('discipline.typePlaceholder')}</option>
          {allowedTypes.map((allowed) => (
            <option key={allowed} value={allowed}>
              {typeLabel(t, allowed)}
            </option>
          ))}
        </select>
      </label>

      <div className="button-row">
        <button
          type="submit"
          className="ds-btn ds-btn--primary"
          disabled={pending || !memberId || !type}
        >
          {pending ? <PendingLabel>{pendingLabel}</PendingLabel> : submitLabel}
        </button>
        {onCancel && (
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={pending}
            onClick={onCancel}
          >
            {tc('cancel')}
          </button>
        )}
      </div>
    </form>
  )
}
