import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { fetchMatchesByStage, fetchStageOverview } from '../api'
import { MatchRow, MatchRowScore } from '../design-system/components/MatchRow'
import { TeamCrest } from '../design-system/TeamCrest'
import { queryKeys } from '../queryKeys'
import {
  EmptyState,
  ErrorState,
  LoadingState,
  MatchStatusBadge,
  PageHeader,
} from '../ui'
import { sideLabel, type MatchSummary } from '../types'
import {
  matchResultTypeLabel,
  matchScheduledLabel,
  matchSportingContext,
} from './matchListMeta'

export function StageMatchesPage() {
  const { stageId = '' } = useParams()
  const { t } = useTranslation('matches')

  // Stage overview shares queryKeys.stages.detail with StagePage (cache when coming from there).
  const stageQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId),
    queryFn: () => fetchStageOverview(stageId),
    enabled: stageId.length > 0,
  })

  const matchesQuery = useQuery({
    queryKey: queryKeys.matches.byStage(stageId),
    queryFn: () => fetchMatchesByStage(stageId),
    enabled: stageId.length > 0,
  })

  const pending = stageQuery.isPending || matchesQuery.isPending
  const error = stageQuery.error ?? matchesQuery.error

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('stageList.eyebrow')}
        title={
          stageQuery.data
            ? t('stageList.titleNamed', { name: stageQuery.data.name })
            : t('stageList.titleFallback')
        }
        back={
          stageId
            ? { to: `/stages/${stageId}`, label: t('stageList.back') }
            : undefined
        }
      />

      {pending && <LoadingState />}
      {error && <ErrorState error={error} />}
      {matchesQuery.data && <MatchList matches={matchesQuery.data} />}
    </main>
  )
}

function MatchList({ matches }: { matches: MatchSummary[] }) {
  const { t } = useTranslation('matches')

  if (matches.length === 0) {
    return (
      <EmptyState title={t('stageList.emptyTitle')}>
        {t('stageList.emptyBody')}
      </EmptyState>
    )
  }

  return (
    <div>
      {matches.map((match) => (
        <StageMatchRow key={match.matchId} match={match} />
      ))}
    </div>
  )
}

function StageMatchRow({ match }: { match: MatchSummary }) {
  const { t } = useTranslation('matches')
  const homeName = sideLabel(match.home)
  const awayName = sideLabel(match.away)
  const sporting = matchSportingContext(match, t, 'stageList.matchday')
  const when = matchScheduledLabel(match)
  const resultKind = matchResultTypeLabel(match)
  const asideLabel = match.score
    ? `${match.score.homeGoals}–${match.score.awayGoals}`
    : when ?? t('stageList.vs')

  const score = match.score ? (
    <MatchRowScore home={match.score.homeGoals} away={match.score.awayGoals} />
  ) : when ? (
    <MatchRowScore home={when} away="" muted />
  ) : (
    <MatchRowScore home={t('stageList.vs')} away="" muted />
  )

  const meta = [sporting, resultKind].filter(Boolean).join(' · ')

  return (
    <MatchRow
      to={`/matches/${match.matchId}`}
      ariaLabel={`${homeName} – ${awayName}, ${asideLabel}`}
      scoreMuted={match.score == null}
      home={{
        name: homeName,
        crest: (
          <TeamCrest
            name={homeName}
            logoMediaId={match.home.logoMediaId}
            primaryColor={match.home.primaryColor}
            size="sm"
          />
        ),
      }}
      away={{
        name: awayName,
        crest: (
          <TeamCrest
            name={awayName}
            logoMediaId={match.away.logoMediaId}
            primaryColor={match.away.primaryColor}
            size="sm"
          />
        ),
      }}
      score={score}
      aside={
        <>
          {meta ? <span>{meta}</span> : null}
          <MatchStatusBadge status={match.status} />
        </>
      }
    />
  )
}
