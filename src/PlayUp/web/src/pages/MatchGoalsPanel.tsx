import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import {
  correctRecordedGoal,
  recordGoal,
  removeRecordedGoal,
  setRunningScore,
} from '../api'
import { queryKeys } from '../queryKeys'
import {
  EmptyState,
  MutationError,
  PendingLabel,
} from '../ui'
import type {
  DeclaredParticipation,
  MatchDetail,
  MatchScore,
  MatchSide,
  RecordedGoal,
  RecordGoalRequest,
} from '../types'

/**
 * Nominative goals panel (Lot 3) — faits ≠ RunningScore ≠ Finish.
 * Live: RecordGoal / Correct / Remove then SetRunningScore as separate calls (not atomic).
 */
export function MatchGoalsPanel({ match }: { match: MatchDetail }) {
  const { t } = useTranslation('matches')
  const { t: tc } = useTranslation('common')
  const queryClient = useQueryClient()
  const canMutate = canMutateRecordedGoals(match)
  const isLive = match.status === 'Live'
  const sheet = match.declaredParticipations ?? []
  const goals = match.recordedGoals ?? []

  const [scorerId, setScorerId] = useState('')
  const [creditedSide, setCreditedSide] = useState<MatchSide>('Home')
  const [assisterId, setAssisterId] = useState('')
  const [editingGoalId, setEditingGoalId] = useState<string | null>(null)
  const [pendingRemoveId, setPendingRemoveId] = useState<string | null>(null)
  const [runningScoreError, setRunningScoreError] = useState<unknown>(null)

  const createMutation = useMutation({
    mutationFn: async (request: RecordGoalRequest) => {
      setRunningScoreError(null)
      await recordGoal(match.matchId, request)
      if (isLive) {
        try {
          await setRunningScore(
            match.matchId,
            adjustRunningScore(match.runningScore, request.creditedSide, 1),
          )
        } catch (error) {
          // Fact succeeded; RS is a separate call — reload and surface desync.
          setRunningScoreError(error)
        }
      }
    },
    onSuccess: async () => {
      resetCreateForm()
      await invalidateMatchGoals(queryClient, match)
    },
  })

  const correctMutation = useMutation({
    mutationFn: async ({
      goal,
      request,
    }: {
      goal: RecordedGoal
      request: RecordGoalRequest
    }) => {
      setRunningScoreError(null)
      await correctRecordedGoal(match.matchId, goal.goalId, request)
      if (isLive && goal.creditedSide !== request.creditedSide) {
        try {
          const afterMinus = adjustRunningScore(
            match.runningScore,
            goal.creditedSide,
            -1,
          )
          await setRunningScore(
            match.matchId,
            adjustRunningScore(afterMinus, request.creditedSide, 1),
          )
        } catch (error) {
          setRunningScoreError(error)
        }
      }
    },
    onSuccess: async () => {
      setEditingGoalId(null)
      resetCreateForm()
      await invalidateMatchGoals(queryClient, match)
    },
  })

  const removeMutation = useMutation({
    mutationFn: async (goal: RecordedGoal) => {
      setRunningScoreError(null)
      await removeRecordedGoal(match.matchId, goal.goalId)
      if (isLive) {
        try {
          await setRunningScore(
            match.matchId,
            adjustRunningScore(match.runningScore, goal.creditedSide, -1),
          )
        } catch (error) {
          setRunningScoreError(error)
        }
      }
    },
    onSuccess: async () => {
      setPendingRemoveId(null)
      await invalidateMatchGoals(queryClient, match)
    },
    onError: () => {
      setPendingRemoveId(null)
    },
  })

  function resetCreateForm() {
    setScorerId('')
    setAssisterId('')
    setCreditedSide('Home')
  }

  function onScorerPicked(memberId: string) {
    setScorerId(memberId)
    const participation = sheet.find((row) => row.memberId === memberId)
    if (participation) {
      setCreditedSide(participation.side)
    }
    if (assisterId === memberId) {
      setAssisterId('')
    }
  }

  const scorerParticipation = sheet.find((row) => row.memberId === scorerId)
  const isOwnGoalDraft =
    scorerParticipation != null && scorerParticipation.side !== creditedSide
  const mutationError =
    createMutation.error ?? correctMutation.error ?? removeMutation.error
  const busy =
    createMutation.isPending ||
    correctMutation.isPending ||
    removeMutation.isPending

  return (
    <section className="ds-panel" aria-labelledby="goals-heading">
      <h2 className="matches-panel__head" id="goals-heading">
        {t('goals.heading')}
      </h2>
      <p className="matches-panel__meta">{t('goals.hint')}</p>

      {!canMutate && (
        <p className="notice notice--info">{t('goals.readOnly')}</p>
      )}

      {sheet.length === 0 && canMutate && (
        <p className="notice notice--info">{t('goals.needSheet')}</p>
      )}

      {goals.length === 0 ? (
        <EmptyState title={t('goals.emptyTitle')}>
          {t('goals.emptyBody')}
        </EmptyState>
      ) : (
        <ul className="match-goals__list">
          {goals.map((goal) => (
            <li key={goal.goalId}>
              <div className="match-goals__row">
                <div className="match-goals__identity">
                  <span className="match-goals__scorer">
                    {goal.scorerDisplayName ?? goal.scorerMemberId}
                  </span>
                  <span className="match-goals__meta">
                    {t('goals.credited', {
                      side:
                        goal.creditedSide === 'Home'
                          ? t('detail.home')
                          : t('detail.away'),
                    })}
                    {goal.isOwnGoal ? ` · ${t('goals.ownGoal')}` : ''}
                    {goal.assisterDisplayName
                      ? ` · ${t('goals.assist', { name: goal.assisterDisplayName })}`
                      : ''}
                  </span>
                </div>

                {canMutate &&
                  editingGoalId !== goal.goalId &&
                  pendingRemoveId !== goal.goalId && (
                    <div className="match-goals__row-actions">
                      <button
                        type="button"
                        className="organisation-action"
                        disabled={busy}
                        onClick={() => {
                          setPendingRemoveId(null)
                          setEditingGoalId(goal.goalId)
                          setScorerId(goal.scorerMemberId)
                          setCreditedSide(goal.creditedSide)
                          setAssisterId(goal.assisterMemberId ?? '')
                        }}
                      >
                        {t('goals.correct')}
                      </button>
                      <button
                        type="button"
                        className="organisation-action"
                        disabled={busy}
                        onClick={() => {
                          setEditingGoalId(null)
                          setPendingRemoveId(goal.goalId)
                        }}
                      >
                        {t('goals.remove')}
                      </button>
                    </div>
                  )}

                {canMutate && editingGoalId === goal.goalId && (
                  <GoalForm
                    sheet={sheet}
                    scorerId={scorerId}
                    creditedSide={creditedSide}
                    assisterId={assisterId}
                    pending={correctMutation.isPending}
                    submitLabel={t('goals.saveCorrect')}
                    pendingLabel={t('goals.saving')}
                    onScorerChange={onScorerPicked}
                    onCreditedSideChange={setCreditedSide}
                    onAssisterChange={setAssisterId}
                    onCancel={() => {
                      setEditingGoalId(null)
                      resetCreateForm()
                    }}
                    onSubmit={(request) =>
                      correctMutation.mutate({ goal, request })
                    }
                  />
                )}

                {canMutate && pendingRemoveId === goal.goalId && (
                  <div
                    className="match-goals__confirm notice notice--warning"
                    role="group"
                  >
                    <p>
                      {t('goals.removeConsequence', {
                        name: goal.scorerDisplayName ?? goal.scorerMemberId,
                      })}
                    </p>
                    <div className="match-goals__confirm-actions">
                      <button
                        type="button"
                        className="ds-btn ds-btn--destructive"
                        disabled={removeMutation.isPending}
                        onClick={() => removeMutation.mutate(goal)}
                      >
                        {removeMutation.isPending ? (
                          <PendingLabel>{t('goals.removing')}</PendingLabel>
                        ) : (
                          t('goals.confirmRemove')
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
          ))}
        </ul>
      )}

      {(mutationError || runningScoreError) && (
        <div className="match-goals__errors">
          {mutationError && <MutationError error={mutationError} />}
          {runningScoreError && (
            <>
              <p className="notice notice--warning" role="status">
                {t('goals.runningScoreDesync')}
              </p>
              <MutationError error={runningScoreError} />
            </>
          )}
        </div>
      )}

      {canMutate && sheet.length > 0 && editingGoalId === null && (
        <GoalForm
          sheet={sheet}
          scorerId={scorerId}
          creditedSide={creditedSide}
          assisterId={assisterId}
          pending={createMutation.isPending}
          submitLabel={t('goals.addAction')}
          pendingLabel={t('goals.adding')}
          showOwnGoalHint={isOwnGoalDraft}
          onScorerChange={onScorerPicked}
          onCreditedSideChange={setCreditedSide}
          onAssisterChange={setAssisterId}
          onSubmit={(request) => createMutation.mutate(request)}
        />
      )}
    </section>
  )
}

export function canMutateRecordedGoals(match: MatchDetail): boolean {
  if (
    match.status === 'Scheduled' ||
    match.status === 'Postponed' ||
    match.status === 'Live'
  ) {
    return true
  }

  return match.status === 'Finished' && !match.hasObservedLive
}

export function adjustRunningScore(
  current: MatchScore | null | undefined,
  creditedSide: MatchSide,
  delta: number,
): MatchScore {
  const base = current ?? { homeGoals: 0, awayGoals: 0 }
  if (creditedSide === 'Home') {
    return {
      homeGoals: Math.max(0, base.homeGoals + delta),
      awayGoals: base.awayGoals,
    }
  }

  return {
    homeGoals: base.homeGoals,
    awayGoals: Math.max(0, base.awayGoals + delta),
  }
}

async function invalidateMatchGoals(
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
      queryKey: queryKeys.competitions.cockpit(match.competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.consultation(match.competitionId),
    }),
  ])
}

function GoalForm({
  sheet,
  scorerId,
  creditedSide,
  assisterId,
  pending,
  submitLabel,
  pendingLabel,
  showOwnGoalHint = false,
  onScorerChange,
  onCreditedSideChange,
  onAssisterChange,
  onCancel,
  onSubmit,
}: {
  sheet: DeclaredParticipation[]
  scorerId: string
  creditedSide: MatchSide
  assisterId: string
  pending: boolean
  submitLabel: string
  pendingLabel: string
  showOwnGoalHint?: boolean
  onScorerChange: (memberId: string) => void
  onCreditedSideChange: (side: MatchSide) => void
  onAssisterChange: (memberId: string) => void
  onCancel?: () => void
  onSubmit: (request: RecordGoalRequest) => void
}) {
  const { t } = useTranslation('matches')
  const { t: tc } = useTranslation('common')
  const scorer = sheet.find((row) => row.memberId === scorerId)
  const isOwnGoal = scorer != null && scorer.side !== creditedSide
  const assisters = sheet.filter((row) => row.memberId !== scorerId)

  return (
    <form
      className="form match-goals__form"
      onSubmit={(event: FormEvent) => {
        event.preventDefault()
        if (scorerId.length === 0 || pending) {
          return
        }
        onSubmit({
          scorerMemberId: scorerId,
          creditedSide,
          assisterMemberId:
            isOwnGoal || assisterId.length === 0 ? null : assisterId,
        })
      }}
    >
      <label className="field">
        {t('goals.scorer')}
        <select
          value={scorerId}
          onChange={(event) => onScorerChange(event.target.value)}
          disabled={pending}
          required
        >
          <option value="">{t('goals.scorerPlaceholder')}</option>
          {sheet.map((row) => (
            <option key={row.memberId} value={row.memberId}>
              {row.displayName ?? row.memberId}
            </option>
          ))}
        </select>
      </label>

      <fieldset className="match-goals__side-fieldset">
        <legend>{t('goals.creditedSide')}</legend>
        <label className="match-goals__radio">
          <input
            type="radio"
            name="credited-side"
            checked={creditedSide === 'Home'}
            onChange={() => onCreditedSideChange('Home')}
            disabled={pending}
          />
          {t('detail.home')}
        </label>
        <label className="match-goals__radio">
          <input
            type="radio"
            name="credited-side"
            checked={creditedSide === 'Away'}
            onChange={() => onCreditedSideChange('Away')}
            disabled={pending}
          />
          {t('detail.away')}
        </label>
      </fieldset>

      {(showOwnGoalHint || isOwnGoal) && (
        <p className="match-goals__csc">{t('goals.ownGoalHint')}</p>
      )}

      <label className="field">
        {t('goals.assister')}
        <select
          value={isOwnGoal ? '' : assisterId}
          onChange={(event) => onAssisterChange(event.target.value)}
          disabled={pending || isOwnGoal}
        >
          <option value="">{t('goals.assisterNone')}</option>
          {assisters.map((row) => (
            <option key={row.memberId} value={row.memberId}>
              {row.displayName ?? row.memberId}
            </option>
          ))}
        </select>
      </label>

      <div className="match-goals__confirm-actions">
        <button
          type="submit"
          className="ds-btn ds-btn--primary"
          disabled={pending || scorerId.length === 0}
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
