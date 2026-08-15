import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import {
  applyProgressionOutcome,
  finishMatch,
  startMatch,
  fetchMatchDetail,
  fetchStageOverview,
} from '../api'
import { BackLink, ErrorState, LoadingState, formatError } from '../queryUi'
import {
  formatScore,
  matchStatusLabel,
  resultTypeLabel,
  sideLabel,
  type FinishMatchRequest,
  type MatchDetail,
  type MatchStatus,
  type ResultType,
} from '../types'

export function MatchPage() {
  const { matchId = '' } = useParams()

  // SERVER STATE: the match lives in TanStack Query cache, not in React useState.
  const matchQuery = useQuery({
    queryKey: ['matches', matchId],
    queryFn: () => fetchMatchDetail(matchId),
    enabled: matchId.length > 0,
  })

  const stageId = matchQuery.data?.stageId
  const stageQuery = useQuery({
    queryKey: ['stages', stageId ?? ''],
    queryFn: () => fetchStageOverview(stageId!),
    enabled: Boolean(stageId),
  })

  return (
    <main className="page">
      <header className="page__header">
        <p className="eyebrow">Match</p>
        <h1>
          {matchQuery.data
            ? `${sideLabel(matchQuery.data.home)} vs ${sideLabel(matchQuery.data.away)}`
            : 'Detail'}
        </h1>
        {stageId && (
          <BackLink to={`/stages/${stageId}/matches`}>
            ← Back to{' '}
            {stageQuery.data?.name
              ? `${stageQuery.data.name} matches`
              : 'match list'}
          </BackLink>
        )}
      </header>

      {matchQuery.isPending && <LoadingState />}
      {matchQuery.isError && <ErrorState error={matchQuery.error} />}
      {matchQuery.data && (
        <MatchDetailView
          data={matchQuery.data}
          stageName={stageQuery.data?.name}
          stageSlots={stageQuery.data?.slots}
        />
      )}
    </main>
  )
}

function MatchDetailView({
  data,
  stageName,
  stageSlots,
}: {
  data: MatchDetail
  stageName?: string
  stageSlots?: { slotKey: string; entryId: string | null; displayName: string | null }[]
}) {
  const queryClient = useQueryClient()
  const homeName = sideLabel(data.home)
  const awayName = sideLabel(data.away)

  // useMutation = “run this write when the user asks”, not “keep this data fresh”.
  // Analogy: closer to calling an Application use case than to a GET repository.
  // Unlike .NET, there is no Command/Handler class — just a function + cache side-effects.
  const startMutation = useMutation({
    mutationFn: () => startMatch(data.matchId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['matches', data.matchId],
      })
    },
  })

  const finishMutation = useMutation({
    mutationFn: (request: FinishMatchRequest) =>
      finishMatch(data.matchId, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['matches', data.matchId],
      })
      await queryClient.invalidateQueries({
        queryKey: ['matches', 'by-stage', data.stageId],
      })
    },
  })

  const progressionMutation = useMutation({
    mutationFn: () => {
      if (!data.fixtureId) {
        throw new Error('This match has no fixture; progression is unavailable.')
      }
      return applyProgressionOutcome(data.stageId, data.fixtureId)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['matches', data.matchId],
      })
      await queryClient.invalidateQueries({
        queryKey: ['matches', 'by-stage', data.stageId],
      })
      await queryClient.invalidateQueries({
        queryKey: ['stages', data.stageId],
      })
    },
  })

  const mutationError =
    startMutation.error ?? finishMutation.error ?? progressionMutation.error
  const anyPending =
    startMutation.isPending ||
    finishMutation.isPending ||
    progressionMutation.isPending

  const sf1a = stageSlots?.find((slot) => slot.slotKey === 'SF1-A')

  return (
    <article className="panel match-panel">
      <header className="panel__header">
        <div className="scoreboard" aria-live="polite">
          <div className="scoreboard__side">
            <span className="scoreboard__name">{homeName}</span>
            <span className="scoreboard__goals">
              {data.result?.homeGoals ?? '–'}
            </span>
          </div>
          <span className="scoreboard__sep" aria-hidden="true">
            –
          </span>
          <div className="scoreboard__side scoreboard__side--away">
            <span className="scoreboard__goals">
              {data.result?.awayGoals ?? '–'}
            </span>
            <span className="scoreboard__name">{awayName}</span>
          </div>
        </div>

        <p className={`match-status match-status--${statusTone(data.status)}`}>
          <span className="match-status__dot" aria-hidden="true" />
          {matchStatusLabel[data.status]}
        </p>
        <p className="mono">{data.matchId}</p>
      </header>

      {data.status === 2 && data.result && (
        <section>
          <h3>Result</h3>
          <ul className="plain-list">
            <li>
              Score:{' '}
              <strong>
                {formatScore({
                  homeGoals: data.result.homeGoals,
                  awayGoals: data.result.awayGoals,
                })}
              </strong>
            </li>
            <li>Type: {resultTypeLabel[data.result.type]}</li>
            <li>
              Extra time:{' '}
              {data.result.extraTimePlayed ? 'Yes' : 'No'}
            </li>
            {data.result.shootout && (
              <li>Shootout: {formatScore(data.result.shootout)}</li>
            )}
          </ul>
        </section>
      )}

      <section className="match-actions">
        <h3>Actions</h3>

        {data.status === 0 && (
          <button
            type="button"
            className="btn"
            disabled={startMutation.isPending}
            onClick={() => startMutation.mutate()}
          >
            {startMutation.isPending ? 'Starting…' : 'Start match'}
          </button>
        )}

        {data.status === 1 && (
          <FinishMatchForm
            homeName={homeName}
            awayName={awayName}
            pending={finishMutation.isPending}
            onSubmit={(request) => finishMutation.mutate(request)}
          />
        )}

        {data.status === 2 && data.fixtureId && (
          <button
            type="button"
            className="btn"
            disabled={progressionMutation.isPending}
            onClick={() => progressionMutation.mutate()}
          >
            {progressionMutation.isPending
              ? 'Applying progression…'
              : 'Apply progression'}
          </button>
        )}

        {data.status !== 0 &&
          data.status !== 1 &&
          data.status !== 2 && (
            <p className="hint">
              No organizer action for status{' '}
              {matchStatusLabel[data.status]}.
            </p>
          )}

        {anyPending && (
          <p className="hint" role="status">
            Working…
          </p>
        )}

        {mutationError && (
          <p className="error" role="alert">
            {formatError(mutationError)}
          </p>
        )}

        {progressionMutation.isSuccess && sf1a?.entryId && (
          <p className="success" role="status">
            Progression applied: slot SF1-A →{' '}
            <strong>{sf1a.displayName ?? sf1a.entryId}</strong>
          </p>
        )}
      </section>

      <section>
        <h3>Context</h3>
        <ul className="plain-list">
          <li>
            Stage:{' '}
            {stageName ?? (
              <span className="mono">{data.stageId}</span>
            )}
          </li>
          <li>
            Competition: <span className="mono">{data.competitionId}</span>
          </li>
          {data.fixtureId && (
            <li>
              Fixture: <span className="mono">{data.fixtureId}</span>
              {data.legIndex != null ? ` · leg ${data.legIndex}` : ''}
            </li>
          )}
          {sf1a && (
            <li>
              Slot SF1-A:{' '}
              {sf1a.entryId
                ? (sf1a.displayName ?? sf1a.entryId)
                : 'empty'}
            </li>
          )}
        </ul>
      </section>
    </article>
  )
}

/**
 * CLIENT STATE only: draft values typed by the organizer.
 * The finished Match.result remains server state (refetched after mutation).
 */
function FinishMatchForm({
  homeName,
  awayName,
  pending,
  onSubmit,
}: {
  homeName: string
  awayName: string
  pending: boolean
  onSubmit: (request: FinishMatchRequest) => void
}) {
  const [homeGoals, setHomeGoals] = useState('0')
  const [awayGoals, setAwayGoals] = useState('0')
  const [type, setType] = useState<ResultType>(0)
  const [extraTimePlayed, setExtraTimePlayed] = useState(false)
  const [shootoutHome, setShootoutHome] = useState('')
  const [shootoutAway, setShootoutAway] = useState('')
  const [localError, setLocalError] = useState<string | null>(null)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const home = Number(homeGoals)
    const away = Number(awayGoals)

    if (
      !Number.isInteger(home) ||
      !Number.isInteger(away) ||
      home < 0 ||
      away < 0
    ) {
      setLocalError('Goals must be non-negative whole numbers.')
      return
    }

    const homeShoot = shootoutHome.trim()
    const awayShoot = shootoutAway.trim()
    const hasHomeShoot = homeShoot.length > 0
    const hasAwayShoot = awayShoot.length > 0

    if (hasHomeShoot !== hasAwayShoot) {
      setLocalError('Provide both shootout values, or leave both empty.')
      return
    }

    let penaltyShootoutHomeGoals: number | undefined
    let penaltyShootoutAwayGoals: number | undefined

    if (hasHomeShoot && hasAwayShoot) {
      const sh = Number(homeShoot)
      const sa = Number(awayShoot)
      if (
        !Number.isInteger(sh) ||
        !Number.isInteger(sa) ||
        sh < 0 ||
        sa < 0
      ) {
        setLocalError('Shootout kicks must be non-negative whole numbers.')
        return
      }
      penaltyShootoutHomeGoals = sh
      penaltyShootoutAwayGoals = sa
    }

    setLocalError(null)
    onSubmit({
      type,
      homeGoals: home,
      awayGoals: away,
      extraTimePlayed,
      ...(penaltyShootoutHomeGoals !== undefined &&
      penaltyShootoutAwayGoals !== undefined
        ? { penaltyShootoutHomeGoals, penaltyShootoutAwayGoals }
        : {}),
    })
  }

  return (
    <form className="finish-form" onSubmit={handleSubmit} noValidate>
      <div className="finish-form__row">
        <label htmlFor="homeGoals">
          {homeName} goals
          <input
            id="homeGoals"
            type="number"
            min={0}
            step={1}
            inputMode="numeric"
            value={homeGoals}
            disabled={pending}
            onChange={(e) => setHomeGoals(e.target.value)}
          />
        </label>
        <label htmlFor="awayGoals">
          {awayName} goals
          <input
            id="awayGoals"
            type="number"
            min={0}
            step={1}
            inputMode="numeric"
            value={awayGoals}
            disabled={pending}
            onChange={(e) => setAwayGoals(e.target.value)}
          />
        </label>
      </div>

      <label htmlFor="resultType">
        Result type
        <select
          id="resultType"
          value={type}
          disabled={pending}
          onChange={(e) => setType(Number(e.target.value) as ResultType)}
        >
          {([0, 1, 2, 3] as const).map((value) => (
            <option key={value} value={value}>
              {resultTypeLabel[value]}
            </option>
          ))}
        </select>
      </label>

      <label className="finish-form__check" htmlFor="extraTime">
        <input
          id="extraTime"
          type="checkbox"
          checked={extraTimePlayed}
          disabled={pending}
          onChange={(e) => setExtraTimePlayed(e.target.checked)}
        />
        Extra time played
      </label>

      <div className="finish-form__row">
        <label htmlFor="shootoutHome">
          Shootout {homeName} (optional)
          <input
            id="shootoutHome"
            type="number"
            min={0}
            step={1}
            inputMode="numeric"
            value={shootoutHome}
            disabled={pending}
            onChange={(e) => setShootoutHome(e.target.value)}
          />
        </label>
        <label htmlFor="shootoutAway">
          Shootout {awayName} (optional)
          <input
            id="shootoutAway"
            type="number"
            min={0}
            step={1}
            inputMode="numeric"
            value={shootoutAway}
            disabled={pending}
            onChange={(e) => setShootoutAway(e.target.value)}
          />
        </label>
      </div>

      {localError && (
        <p className="error" role="alert">
          {localError}
        </p>
      )}

      <button type="submit" className="btn" disabled={pending}>
        {pending ? 'Finishing…' : 'Finish match'}
      </button>
    </form>
  )
}

function statusTone(status: MatchStatus): string {
  switch (status) {
    case 0:
      return 'scheduled'
    case 1:
      return 'live'
    case 2:
      return 'finished'
    default:
      return 'other'
  }
}
