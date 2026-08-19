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
import {
  ErrorState,
  LoadingState,
  MatchStatusBadge,
  MutationError,
  PageHeader,
  PendingLabel,
} from '../ui'
import {
  matchStatusLabel,
  resultTypeLabel,
} from '../i18n/enumLabels'
import {
  formatScore,
  resultTypeOptions,
  sideLabel,
  type FinishMatchRequest,
  type MatchDetail,
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
    <main id="main" className="page">
      <PageHeader
        eyebrow={stageQuery.data?.name ? `Match · ${stageQuery.data.name}` : 'Match'}
        title={
          matchQuery.data
            ? `${sideLabel(matchQuery.data.home)} vs ${sideLabel(matchQuery.data.away)}`
            : 'Match'
        }
        back={
          stageId
            ? {
                to: `/stages/${stageId}/matches`,
                label: stageQuery.data?.name
                  ? `Back to ${stageQuery.data.name} matches`
                  : 'Back to match list',
              }
            : undefined
        }
        badges={
          matchQuery.data && <MatchStatusBadge status={matchQuery.data.status} />
        }
      />

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
  stageSlots?: {
    slotKey: string
    entryId: string | null
    displayName: string | null
  }[]
}) {
  const queryClient = useQueryClient()
  const homeName = sideLabel(data.home)
  const awayName = sideLabel(data.away)

  // useMutation = “run this write when the user asks”, not “keep this data fresh”.
  const startMutation = useMutation({
    mutationFn: () => startMatch(data.matchId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['matches', data.matchId],
      })
      await queryClient.invalidateQueries({
        queryKey: ['matches', 'by-stage', data.stageId],
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
      await queryClient.invalidateQueries({
        queryKey: ['competitions', data.competitionId, 'attention'],
      })
      await queryClient.invalidateQueries({
        queryKey: ['competitions', data.competitionId, 'workspace'],
      })
    },
  })

  const mutationError =
    startMutation.error ?? finishMutation.error ?? progressionMutation.error

  const sf1a = stageSlots?.find((slot) => slot.slotKey === 'SF1-A')
  const busy =
    startMutation.isPending ||
    finishMutation.isPending ||
    progressionMutation.isPending

  return (
    <div className="section-stack">
      <section className="card card--hero" aria-labelledby="scoreboard-heading">
        <h2 className="card__title" id="scoreboard-heading">
          Scoreboard
        </h2>
        <div
          className="scoreboard"
          aria-live="polite"
          aria-label={`Score ${homeName} ${data.result?.homeGoals ?? 'none'} to ${awayName} ${data.result?.awayGoals ?? 'none'}`}
        >
          <div className="scoreboard__side">
            <span className="scoreboard__role">Home</span>
            <span className="scoreboard__name">{homeName}</span>
          </div>
          <p className="scoreboard__score">
            <span
              className={
                data.result
                  ? 'scoreboard__goals'
                  : 'scoreboard__goals scoreboard__goals--empty'
              }
            >
              {data.result?.homeGoals ?? '–'}
            </span>
            <span className="scoreboard__sep" aria-hidden="true">
              :
            </span>
            <span
              className={
                data.result
                  ? 'scoreboard__goals'
                  : 'scoreboard__goals scoreboard__goals--empty'
              }
            >
              {data.result?.awayGoals ?? '–'}
            </span>
          </p>
          <div className="scoreboard__side">
            <span className="scoreboard__role">Away</span>
            <span className="scoreboard__name">{awayName}</span>
          </div>
        </div>

        {data.status === 'Finished' && data.result && (
          <dl className="fact-list">
            <div className="fact">
              <dt className="fact__label">Type</dt>
              <dd className="fact__value">
                {resultTypeLabel(data.result.type)}
              </dd>
            </div>
            <div className="fact">
              <dt className="fact__label">Extra time</dt>
              <dd className="fact__value">
                {data.result.extraTimePlayed ? 'Yes' : 'No'}
              </dd>
            </div>
            {data.result.shootout && (
              <div className="fact">
                <dt className="fact__label">Shootout</dt>
                <dd className="fact__value">
                  {formatScore(data.result.shootout)}
                </dd>
              </div>
            )}
          </dl>
        )}
      </section>

      <section className="card" aria-labelledby="actions-heading" aria-busy={busy}>
        <div className="card__head">
          <h2 className="card__title" id="actions-heading">
            Actions
          </h2>
          <p className="card__subtitle">
            Available steps for a {matchStatusLabel(data.status).toLowerCase()}{' '}
            match
          </p>
        </div>

        {data.status === 'Scheduled' && (
          <div className="button-row">
            <button
              type="button"
              className="btn btn--primary btn--lg"
              disabled={startMutation.isPending}
              onClick={() => startMutation.mutate()}
            >
              {startMutation.isPending ? (
                <PendingLabel>Starting…</PendingLabel>
              ) : (
                'Start match'
              )}
            </button>
            <span className="caption">
              Kicks the match off and unlocks the result form.
            </span>
          </div>
        )}

        {data.status === 'Live' && (
          <FinishMatchForm
            homeName={homeName}
            awayName={awayName}
            pending={finishMutation.isPending}
            onSubmit={(request) => finishMutation.mutate(request)}
          />
        )}

        {data.status === 'Finished' && data.fixtureId && (
          <div className="button-row">
            <button
              type="button"
              className="btn btn--primary"
              disabled={progressionMutation.isPending}
              onClick={() => progressionMutation.mutate()}
            >
              {progressionMutation.isPending ? (
                <PendingLabel>Applying progression…</PendingLabel>
              ) : (
                'Apply progression'
              )}
            </button>
            <span className="caption">
              Moves the qualified entry into its next slot.
            </span>
          </div>
        )}

        {data.status !== 'Scheduled' &&
          data.status !== 'Live' &&
          data.status !== 'Finished' && (
            <p className="notice">
              No organizer action for status {matchStatusLabel(data.status)}.
            </p>
          )}

        {mutationError && <MutationError error={mutationError} />}

        {progressionMutation.isSuccess && sf1a?.entryId && (
          <p className="notice notice--success" role="status">
            Progression applied: slot SF1-A →{' '}
            <strong>{sf1a.displayName ?? sf1a.entryId}</strong>
          </p>
        )}
      </section>

      <section className="card" aria-labelledby="context-heading">
        <h2 className="card__title" id="context-heading">
          Context
        </h2>
        <dl className="fact-list">
          <div className="fact">
            <dt className="fact__label">Stage</dt>
            <dd className="fact__value">
              {stageName ?? <span className="mono">{data.stageId}</span>}
            </dd>
          </div>
          <div className="fact">
            <dt className="fact__label">Competition</dt>
            <dd className="fact__value">
              <span className="mono">{data.competitionId}</span>
            </dd>
          </div>
          {data.fixtureId && (
            <div className="fact">
              <dt className="fact__label">
                Fixture{data.legIndex != null ? ` · leg ${data.legIndex}` : ''}
              </dt>
              <dd className="fact__value">
                <span className="mono">{data.fixtureId}</span>
              </dd>
            </div>
          )}
          {sf1a && (
            <div className="fact">
              <dt className="fact__label">Slot SF1-A</dt>
              <dd className="fact__value">
                {sf1a.entryId ? (sf1a.displayName ?? sf1a.entryId) : 'empty'}
              </dd>
            </div>
          )}
        </dl>
        <p className="caption">
          Match <span className="id-chip">{data.matchId}</span>
        </p>
      </section>
    </div>
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
  const [type, setType] = useState<ResultType>('Played')
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
    <form className="form form--wide" onSubmit={handleSubmit} noValidate>
      <fieldset className="fieldset" disabled={pending}>
        <legend className="fieldset__legend">Final score</legend>
        <div className="form-row">
          <label className="field" htmlFor="homeGoals">
            {homeName} goals
            <input
              id="homeGoals"
              type="number"
              min={0}
              step={1}
              inputMode="numeric"
              value={homeGoals}
              onChange={(e) => setHomeGoals(e.target.value)}
            />
          </label>
          <label className="field" htmlFor="awayGoals">
            {awayName} goals
            <input
              id="awayGoals"
              type="number"
              min={0}
              step={1}
              inputMode="numeric"
              value={awayGoals}
              onChange={(e) => setAwayGoals(e.target.value)}
            />
          </label>
        </div>
      </fieldset>

      <div className="form-row">
        <label className="field" htmlFor="resultType">
          Result type
          <select
            id="resultType"
            value={type}
            disabled={pending}
            onChange={(e) => setType(e.target.value as ResultType)}
          >
            {resultTypeOptions.map((value) => (
              <option key={value} value={value}>
                {resultTypeLabel(value)}
              </option>
            ))}
          </select>
        </label>
        <label className="field field--check" htmlFor="extraTime">
          <input
            id="extraTime"
            type="checkbox"
            checked={extraTimePlayed}
            disabled={pending}
            onChange={(e) => setExtraTimePlayed(e.target.checked)}
          />
          Extra time played
        </label>
      </div>

      <fieldset className="fieldset" disabled={pending}>
        <legend className="fieldset__legend">
          Penalty shootout (optional)
        </legend>
        <div className="form-row">
          <label className="field" htmlFor="shootoutHome">
            {homeName} kicks
            <input
              id="shootoutHome"
              type="number"
              min={0}
              step={1}
              inputMode="numeric"
              value={shootoutHome}
              onChange={(e) => setShootoutHome(e.target.value)}
            />
          </label>
          <label className="field" htmlFor="shootoutAway">
            {awayName} kicks
            <input
              id="shootoutAway"
              type="number"
              min={0}
              step={1}
              inputMode="numeric"
              value={shootoutAway}
              onChange={(e) => setShootoutAway(e.target.value)}
            />
          </label>
        </div>
        <p className="field__hint">Leave both empty when there was no shootout.</p>
      </fieldset>

      {localError && (
        <p className="notice notice--danger" role="alert">
          {localError}
        </p>
      )}

      <div className="button-row">
        <button
          type="submit"
          className="btn btn--primary btn--lg"
          disabled={pending}
        >
          {pending ? <PendingLabel>Finishing…</PendingLabel> : 'Finish match'}
        </button>
        <span className="caption">Records the result and closes the match.</span>
      </div>
    </form>
  )
}
