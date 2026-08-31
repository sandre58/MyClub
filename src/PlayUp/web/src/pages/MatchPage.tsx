import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { useId, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import {
  fetchMatchDetail,
  fetchStageOverview,
  finishMatch,
  setRunningScore,
  startMatch,
} from '../api'
import { TeamCrest } from '../design-system/TeamCrest'
import { matchStatusLabel, resultTypeLabel } from '../i18n/enumLabels'
import { queryKeys } from '../queryKeys'
import {
  ErrorState,
  LoadingState,
  MatchStatusBadge,
  MutationError,
  PendingLabel,
} from '../ui'
import {
  sideLabel,
  type FinishMatchRequest,
  type MatchDetail,
  type MatchScore,
} from '../types'
import { formatMatchKickoff } from './matchListMeta'
import { MatchDisciplinaryPanel } from './MatchDisciplinaryPanel'
import { MatchGoalsPanel } from './MatchGoalsPanel'
import { MatchSheetPanel } from './MatchSheetPanel'
import { MatchSubstitutionsPanel } from './MatchSubstitutionsPanel'
import './matches.css'

/**
 * Championship match detail — two jobs for score, plus Lot 2 sheet:
 * live observed counter (`SetRunningScore`) ≠ official close (`Finish`).
 * Sheet = composition déclarée (Starter/Bench/jersey). No goals UI yet (Lot 3).
 */
export function MatchPage() {
  const { matchId = '' } = useParams()

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
    <main id="main" className="page page--matches">
      {matchQuery.isPending && <LoadingState />}
      {matchQuery.isError && <ErrorState error={matchQuery.error} />}
      {matchQuery.data && (
        <MatchDetailView
          data={matchQuery.data}
          stageName={stageQuery.data?.name}
        />
      )}
    </main>
  )
}

async function invalidateAfterMatchMutation(
  queryClient: QueryClient,
  data: MatchDetail,
) {
  await Promise.all([
    queryClient.invalidateQueries({
      queryKey: queryKeys.matches.detail(data.matchId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.matches.byStage(data.stageId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.cockpit(data.competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.workspace(data.competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.attention(data.competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.consultation(data.competitionId),
    }),
  ])
}

function championshipFinish(
  homeGoals: number,
  awayGoals: number,
): FinishMatchRequest {
  return {
    type: 'Played',
    homeGoals,
    awayGoals,
    extraTimePlayed: false,
  }
}

function MatchDetailView({
  data,
  stageName,
}: {
  data: MatchDetail
  stageName?: string
}) {
  const { t } = useTranslation('matches')
  const queryClient = useQueryClient()
  const homeName = sideLabel(data.home)
  const awayName = sideLabel(data.away)
  const kickoff = formatMatchKickoff(data.scheduledAt)
  const board = displayedScore(data)

  const startMutation = useMutation({
    mutationFn: () => startMatch(data.matchId),
    onSuccess: () => invalidateAfterMatchMutation(queryClient, data),
  })

  const runningScoreMutation = useMutation({
    mutationFn: (score: MatchScore) => setRunningScore(data.matchId, score),
    onSuccess: () => invalidateAfterMatchMutation(queryClient, data),
  })

  const finishMutation = useMutation({
    mutationFn: (request: FinishMatchRequest) =>
      finishMatch(data.matchId, request),
    onSuccess: () => invalidateAfterMatchMutation(queryClient, data),
  })

  const mutationError =
    startMutation.error ??
    runningScoreMutation.error ??
    finishMutation.error

  const busy =
    startMutation.isPending ||
    runningScoreMutation.isPending ||
    finishMutation.isPending

  const canStart = data.status === 'Scheduled'
  const canSetRunningScore = data.status === 'Live'
  const canFinish =
    data.status === 'Scheduled' ||
    data.status === 'Postponed' ||
    data.status === 'Live'

  const runningPrefill: MatchScore = data.runningScore ?? {
    homeGoals: 0,
    awayGoals: 0,
  }

  return (
    <div className="matches match-detail">
      <header className="matches__page-head">
        <Link
          className="matches__back"
          to={`/competitions/${data.competitionId}/matches`}
        >
          <span aria-hidden="true">←</span>
          {t('detail.backToMatches')}
        </Link>
        <div className="match-detail__title-row">
          <h1 className="matches__title">
            {t('detail.titleVs', { home: homeName, away: awayName })}
          </h1>
          <MatchStatusBadge status={data.status} />
        </div>
      </header>

      {(stageName || kickoff) && (
        <p className="match-detail__meta">
          {[stageName, kickoff].filter(Boolean).join(' · ')}
        </p>
      )}

      <section className="ds-panel" aria-labelledby="scoreboard-heading">
        <h2 className="matches-panel__head" id="scoreboard-heading">
          {t('detail.scoreboard')}
        </h2>
        <div
          className="match-detail__scoreboard"
          aria-live="polite"
          aria-label={t('detail.scoreAria', {
            home: homeName,
            away: awayName,
            homeGoals: board.homeLabel,
            awayGoals: board.awayLabel,
          })}
        >
          <div className="match-detail__side">
            <TeamCrest
              name={homeName}
              logoMediaId={data.home.logoMediaId}
              primaryColor={data.home.primaryColor}
            />
            <span className="match-detail__role">{t('detail.home')}</span>
            <span className="match-detail__name">{homeName}</span>
          </div>
          <p
            className={
              board.source === 'empty'
                ? 'match-detail__score match-detail__score--empty'
                : 'match-detail__score'
            }
          >
            <span>{board.homeDisplay}</span>
            <span className="match-detail__sep" aria-hidden="true">
              :
            </span>
            <span>{board.awayDisplay}</span>
          </p>
          <div className="match-detail__side">
            <TeamCrest
              name={awayName}
              logoMediaId={data.away.logoMediaId}
              primaryColor={data.away.primaryColor}
            />
            <span className="match-detail__role">{t('detail.away')}</span>
            <span className="match-detail__name">{awayName}</span>
          </div>
        </div>
        {board.source === 'official' && data.result && (
          <p className="match-detail__caption">
            {t('detail.scoreCaptionOfficial')}
            {data.result.type !== 'Played'
              ? ` · ${resultTypeLabel(data.result.type)}`
              : null}
          </p>
        )}
        {board.source === 'live' && (
          <p className="match-detail__caption">
            {t('detail.scoreCaptionLive')}
          </p>
        )}
      </section>

      <MatchSheetPanel match={data} />

      <MatchGoalsPanel match={data} />

      <MatchSubstitutionsPanel match={data} />

      <MatchDisciplinaryPanel match={data} />

      {(canStart || canSetRunningScore || canFinish) && (
        <div className="match-detail__ops" aria-busy={busy}>
          {canStart && (
            <section className="ds-panel" aria-labelledby="live-job-heading">
              <h2 className="matches-panel__head" id="live-job-heading">
                {t('detail.liveJob')}
              </h2>
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
            </section>
          )}

          {canSetRunningScore && (
            <section className="ds-panel" aria-labelledby="counter-job-heading">
              <h2 className="matches-panel__head" id="counter-job-heading">
                {t('detail.runningScore')}
              </h2>
              <p className="matches-panel__meta">{t('detail.runningScoreHint')}</p>
              <RunningScoreForm
                key={`${runningPrefill.homeGoals}-${runningPrefill.awayGoals}`}
                homeName={homeName}
                awayName={awayName}
                defaultScore={runningPrefill}
                pending={runningScoreMutation.isPending}
                onSubmit={(score) => runningScoreMutation.mutate(score)}
              />
            </section>
          )}

          {canFinish && (
            <section className="ds-panel" aria-labelledby="close-job-heading">
              <h2 className="matches-panel__head" id="close-job-heading">
                {t('detail.closeJob')}
              </h2>
              <p className="matches-panel__meta">
                {data.status === 'Live'
                  ? t('detail.finishFromLiveHint')
                  : t('detail.finishAfterHint')}
              </p>
              <OfficialScoreForm
                key={
                  data.status === 'Live'
                    ? `${runningPrefill.homeGoals}-${runningPrefill.awayGoals}`
                    : 'after-the-fact'
                }
                homeName={homeName}
                awayName={awayName}
                defaultHome={
                  data.status === 'Live' ? runningPrefill.homeGoals : 0
                }
                defaultAway={
                  data.status === 'Live' ? runningPrefill.awayGoals : 0
                }
                pending={finishMutation.isPending}
                submitLabel={t('detail.finish')}
                pendingLabel={t('detail.finishing')}
                hint={t('detail.finishHint')}
                onSubmit={(homeGoals, awayGoals) =>
                  finishMutation.mutate(
                    championshipFinish(homeGoals, awayGoals),
                  )
                }
              />
            </section>
          )}
        </div>
      )}

      {data.status === 'Cancelled' && (
        <p className="notice">
          {t('detail.noAction', { status: matchStatusLabel(data.status) })}
        </p>
      )}

      {mutationError && <MutationError error={mutationError} />}
    </div>
  )
}

function displayedScore(data: MatchDetail): {
  source: 'official' | 'live' | 'empty'
  homeDisplay: string | number
  awayDisplay: string | number
  homeLabel: string
  awayLabel: string
} {
  if (data.result) {
    return {
      source: 'official',
      homeDisplay: data.result.homeGoals,
      awayDisplay: data.result.awayGoals,
      homeLabel: String(data.result.homeGoals),
      awayLabel: String(data.result.awayGoals),
    }
  }
  if (data.status === 'Live' && data.runningScore) {
    return {
      source: 'live',
      homeDisplay: data.runningScore.homeGoals,
      awayDisplay: data.runningScore.awayGoals,
      homeLabel: String(data.runningScore.homeGoals),
      awayLabel: String(data.runningScore.awayGoals),
    }
  }
  return {
    source: 'empty',
    homeDisplay: '–',
    awayDisplay: '–',
    homeLabel: '–',
    awayLabel: '–',
  }
}

function parseNonNegativeInt(raw: string): number | null {
  const value = Number(raw)
  if (!Number.isInteger(value) || value < 0) {
    return null
  }
  return value
}

function OfficialScoreForm({
  homeName,
  awayName,
  defaultHome,
  defaultAway,
  pending,
  submitLabel,
  pendingLabel,
  hint,
  onSubmit,
}: {
  homeName: string
  awayName: string
  defaultHome: number
  defaultAway: number
  pending: boolean
  submitLabel: string
  pendingLabel: string
  hint: string
  onSubmit: (homeGoals: number, awayGoals: number) => void
}) {
  const { t } = useTranslation('matches')
  const id = useId()
  const [homeGoals, setHomeGoals] = useState(String(defaultHome))
  const [awayGoals, setAwayGoals] = useState(String(defaultAway))
  const [localError, setLocalError] = useState<string | null>(null)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const home = parseNonNegativeInt(homeGoals)
    const away = parseNonNegativeInt(awayGoals)
    if (home == null || away == null) {
      setLocalError(t('detail.errorGoals'))
      return
    }
    setLocalError(null)
    onSubmit(home, away)
  }

  return (
    <form className="form form--wide" onSubmit={handleSubmit} noValidate>
      <fieldset className="fieldset" disabled={pending}>
        <legend className="fieldset__legend">{t('detail.finalScore')}</legend>
        <div className="form-row">
          <label className="field" htmlFor={`${id}-official-home`}>
            {t('detail.goalsOfficial', { name: homeName })}
            <input
              id={`${id}-official-home`}
              type="number"
              min={0}
              step={1}
              inputMode="numeric"
              value={homeGoals}
              onChange={(e) => setHomeGoals(e.target.value)}
            />
          </label>
          <label className="field" htmlFor={`${id}-official-away`}>
            {t('detail.goalsOfficial', { name: awayName })}
            <input
              id={`${id}-official-away`}
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
          {pending ? <PendingLabel>{pendingLabel}</PendingLabel> : submitLabel}
        </button>
        <span className="caption">{hint}</span>
      </div>
    </form>
  )
}

function RunningScoreForm({
  homeName,
  awayName,
  defaultScore,
  pending,
  onSubmit,
}: {
  homeName: string
  awayName: string
  defaultScore: MatchScore
  pending: boolean
  onSubmit: (score: MatchScore) => void
}) {
  const { t } = useTranslation('matches')
  const id = useId()
  const [homeGoals, setHomeGoals] = useState(String(defaultScore.homeGoals))
  const [awayGoals, setAwayGoals] = useState(String(defaultScore.awayGoals))
  const [localError, setLocalError] = useState<string | null>(null)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const home = parseNonNegativeInt(homeGoals)
    const away = parseNonNegativeInt(awayGoals)
    if (home == null || away == null) {
      setLocalError(t('detail.errorGoals'))
      return
    }
    setLocalError(null)
    onSubmit({ homeGoals: home, awayGoals: away })
  }

  return (
    <form className="form form--wide" onSubmit={handleSubmit} noValidate>
      <fieldset className="fieldset" disabled={pending}>
        <legend className="fieldset__legend">
          {t('detail.runningScoreLegend')}
        </legend>
        <div className="form-row">
          <label className="field" htmlFor={`${id}-running-home`}>
            {t('detail.goalsRunning', { name: homeName })}
            <input
              id={`${id}-running-home`}
              type="number"
              min={0}
              step={1}
              inputMode="numeric"
              value={homeGoals}
              onChange={(e) => setHomeGoals(e.target.value)}
            />
          </label>
          <label className="field" htmlFor={`${id}-running-away`}>
            {t('detail.goalsRunning', { name: awayName })}
            <input
              id={`${id}-running-away`}
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

      {localError && (
        <p className="notice notice--danger" role="alert">
          {localError}
        </p>
      )}

      <div className="button-row">
        <button
          type="submit"
          className="btn btn--primary"
          disabled={pending}
        >
          {pending ? (
            <PendingLabel>{t('detail.updatingScore')}</PendingLabel>
          ) : (
            t('detail.updateRunningScore')
          )}
        </button>
      </div>
    </form>
  )
}
