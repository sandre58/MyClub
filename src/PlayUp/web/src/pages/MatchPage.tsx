import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import {
  applyProgressionOutcome,
  finishMatch,
  startMatch,
  fetchMatchDetail,
  fetchStageOverview,
} from '../api'
import { queryKeys } from '../queryKeys'
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
  const { t } = useTranslation('matches')

  // SERVER STATE: the match lives in TanStack Query cache, not in React useState.
  const matchQuery = useQuery({
    queryKey: queryKeys.matches.detail(matchId),
    queryFn: () => fetchMatchDetail(matchId),
    enabled: matchId.length > 0,
  })

  const stageId = matchQuery.data?.stageId
  const stageQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId ?? ''),
    queryFn: () => fetchStageOverview(stageId!),
    enabled: Boolean(stageId),
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={
          stageQuery.data?.name
            ? t('detail.eyebrowNamed', { name: stageQuery.data.name })
            : t('detail.eyebrow')
        }
        title={
          matchQuery.data
            ? t('detail.titleVs', {
                home: sideLabel(matchQuery.data.home),
                away: sideLabel(matchQuery.data.away),
              })
            : t('detail.titleFallback')
        }
        back={
          stageId
            ? {
                to: `/stages/${stageId}/matches`,
                label: stageQuery.data?.name
                  ? t('detail.backNamed', { name: stageQuery.data.name })
                  : t('detail.back'),
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
  const { t } = useTranslation('matches')
  const queryClient = useQueryClient()
  const homeName = sideLabel(data.home)
  const awayName = sideLabel(data.away)

  // useMutation = “run this write when the user asks”, not “keep this data fresh”.
  const startMutation = useMutation({
    mutationFn: () => startMatch(data.matchId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.matches.detail(data.matchId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.matches.byStage(data.stageId),
      })
    },
  })

  const finishMutation = useMutation({
    mutationFn: (request: FinishMatchRequest) =>
      finishMatch(data.matchId, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.matches.detail(data.matchId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.matches.byStage(data.stageId),
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
        queryKey: queryKeys.matches.detail(data.matchId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.matches.byStage(data.stageId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(data.stageId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.attention(data.competitionId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.workspace(data.competitionId),
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

  const homeGoalsLabel =
    data.result?.homeGoals != null
      ? String(data.result.homeGoals)
      : t('detail.scoreNone')
  const awayGoalsLabel =
    data.result?.awayGoals != null
      ? String(data.result.awayGoals)
      : t('detail.scoreNone')

  return (
    <div className="section-stack">
      <section className="card card--hero" aria-labelledby="scoreboard-heading">
        <h2 className="card__title" id="scoreboard-heading">
          {t('detail.scoreboard')}
        </h2>
        <div
          className="scoreboard"
          aria-live="polite"
          aria-label={t('detail.scoreAria', {
            home: homeName,
            away: awayName,
            homeGoals: homeGoalsLabel,
            awayGoals: awayGoalsLabel,
          })}
        >
          <div className="scoreboard__side">
            <span className="scoreboard__role">{t('detail.home')}</span>
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
            <span className="scoreboard__role">{t('detail.away')}</span>
            <span className="scoreboard__name">{awayName}</span>
          </div>
        </div>

        {data.status === 'Finished' && data.result && (
          <dl className="fact-list">
            <div className="fact">
              <dt className="fact__label">{t('detail.type')}</dt>
              <dd className="fact__value">
                {resultTypeLabel(data.result.type)}
              </dd>
            </div>
            <div className="fact">
              <dt className="fact__label">{t('detail.extraTime')}</dt>
              <dd className="fact__value">
                {data.result.extraTimePlayed ? t('detail.yes') : t('detail.no')}
              </dd>
            </div>
            {data.result.shootout && (
              <div className="fact">
                <dt className="fact__label">{t('detail.shootout')}</dt>
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
            {t('detail.actions')}
          </h2>
          <p className="card__subtitle">
            {t('detail.actionsSubtitle', {
              status: matchStatusLabel(data.status).toLowerCase(),
            })}
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
                <PendingLabel>{t('detail.starting')}</PendingLabel>
              ) : (
                t('detail.start')
              )}
            </button>
            <span className="caption">{t('detail.startHint')}</span>
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
                <PendingLabel>{t('detail.applyingProgression')}</PendingLabel>
              ) : (
                t('detail.applyProgression')
              )}
            </button>
            <span className="caption">{t('detail.progressionHint')}</span>
          </div>
        )}

        {data.status !== 'Scheduled' &&
          data.status !== 'Live' &&
          data.status !== 'Finished' && (
            <p className="notice">
              {t('detail.noAction', {
                status: matchStatusLabel(data.status),
              })}
            </p>
          )}

        {mutationError && <MutationError error={mutationError} />}

        {progressionMutation.isSuccess && sf1a?.entryId && (
          <p className="notice notice--success" role="status">
            {t('detail.progressionApplied')}{' '}
            <strong>{sf1a.displayName ?? sf1a.entryId}</strong>
          </p>
        )}
      </section>

      <section className="card" aria-labelledby="context-heading">
        <h2 className="card__title" id="context-heading">
          {t('detail.context')}
        </h2>
        <dl className="fact-list">
          <div className="fact">
            <dt className="fact__label">{t('detail.stage')}</dt>
            <dd className="fact__value">
              {stageName ?? <span className="mono">{data.stageId}</span>}
            </dd>
          </div>
          <div className="fact">
            <dt className="fact__label">{t('detail.competition')}</dt>
            <dd className="fact__value">
              <span className="mono">{data.competitionId}</span>
            </dd>
          </div>
          {data.fixtureId && (
            <div className="fact">
              <dt className="fact__label">
                {data.legIndex != null
                  ? t('detail.fixtureLeg', { leg: data.legIndex })
                  : t('detail.fixture')}
              </dt>
              <dd className="fact__value">
                <span className="mono">{data.fixtureId}</span>
              </dd>
            </div>
          )}
          {sf1a && (
            <div className="fact">
              <dt className="fact__label">{t('detail.slotSf1a')}</dt>
              <dd className="fact__value">
                {sf1a.entryId
                  ? (sf1a.displayName ?? sf1a.entryId)
                  : t('detail.empty')}
              </dd>
            </div>
          )}
        </dl>
        <p className="caption">
          {t('detail.matchId')} <span className="id-chip">{data.matchId}</span>
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
  const { t } = useTranslation('matches')
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
      setLocalError(t('detail.errorGoals'))
      return
    }

    const homeShoot = shootoutHome.trim()
    const awayShoot = shootoutAway.trim()
    const hasHomeShoot = homeShoot.length > 0
    const hasAwayShoot = awayShoot.length > 0

    if (hasHomeShoot !== hasAwayShoot) {
      setLocalError(t('detail.errorShootoutPair'))
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
        setLocalError(t('detail.errorShootoutValues'))
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
        <legend className="fieldset__legend">{t('detail.finalScore')}</legend>
        <div className="form-row">
          <label className="field" htmlFor="homeGoals">
            {t('detail.goals', { name: homeName })}
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
            {t('detail.goals', { name: awayName })}
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
          {t('detail.resultType')}
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
          {t('detail.extraTimePlayed')}
        </label>
      </div>

      <fieldset className="fieldset" disabled={pending}>
        <legend className="fieldset__legend">{t('detail.shootoutOptional')}</legend>
        <div className="form-row">
          <label className="field" htmlFor="shootoutHome">
            {t('detail.kicks', { name: homeName })}
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
            {t('detail.kicks', { name: awayName })}
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
        <p className="field__hint">{t('detail.shootoutHint')}</p>
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
          {pending ? (
            <PendingLabel>{t('detail.finishing')}</PendingLabel>
          ) : (
            t('detail.finish')
          )}
        </button>
        <span className="caption">{t('detail.finishHint')}</span>
      </div>
    </form>
  )
}
