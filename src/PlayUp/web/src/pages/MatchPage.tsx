import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query';
import { useId, useState, type SubmitEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import {
  fetchMatchDetail,
  fetchStageOverview,
  finishMatch,
  setRunningScore,
  startMatch,
} from '../api';
import {
  MatchHero,
  MatchHeroMetaItem,
  MatchHeroScore,
  MatchHeroScoreActions,
} from '../design-system/components/MatchHero';
import { PageHead } from '../design-system/components/PageHead';
import { Status } from '../design-system/components/Status';
import { TeamCrest } from '../design-system/TeamCrest';
import { CalendarIcon, CheckIcon } from '../design-system/icons/contentIcons';
import { ClockIcon, PinIcon } from '../design-system/icons/metaIcons';
import { matchStatusLabel } from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import {
  BackLink,
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
} from '../ui';
import {
  sideLabel,
  type FinishMatchRequest,
  type MatchDetail,
  type MatchScore,
  type MatchStatus,
} from '../types';
import { formatKickoffParts } from './matchListMeta';
import { MatchDisciplinaryPanel } from './MatchDisciplinaryPanel';
import { adjustRunningScore } from './matchGoalsHelpers';
import { MatchGoalsPanel } from './MatchGoalsPanel';
import { MatchSheetPanel } from './MatchSheetPanel';
import { MatchSubstitutionsPanel } from './MatchSubstitutionsPanel';
import './matches.css';

/**
 * Championship match detail — two jobs for score, plus sheet:
 * live observed counter (`SetRunningScore`) ≠ official close (`Finish`).
 * Sheet = declared composition (Starter/Bench/jersey).
 */
export function MatchPage() {
  const { matchId = '' } = useParams();

  const matchQuery = useQuery({
    queryKey: queryKeys.matches.detail(matchId),
    queryFn: () => fetchMatchDetail(matchId),
    enabled: matchId.length > 0,
  });

  const stageId = matchQuery.data?.stageId;
  const stageQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId ?? ''),
    queryFn: () => fetchStageOverview(stageId!),
    enabled: Boolean(stageId),
  });

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
  );
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
      queryKey: queryKeys.competitions.overview(data.competitionId),
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
  ]);
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
  };
}

function MatchDetailView({
  data,
  stageName,
}: {
  data: MatchDetail;
  stageName?: string;
}) {
  const { t, i18n } = useTranslation('matches');
  const queryClient = useQueryClient();
  const homeName = sideLabel(data.home);
  const awayName = sideLabel(data.away);
  const board = displayedScore(data);

  const startMutation = useMutation({
    mutationFn: () => startMatch(data.matchId),
    onSuccess: () => invalidateAfterMatchMutation(queryClient, data),
  });

  const runningScoreMutation = useMutation({
    mutationFn: (score: MatchScore) => setRunningScore(data.matchId, score),
    onSuccess: () => invalidateAfterMatchMutation(queryClient, data),
  });

  const finishMutation = useMutation({
    mutationFn: (request: FinishMatchRequest) =>
      finishMatch(data.matchId, request),
    onSuccess: () => invalidateAfterMatchMutation(queryClient, data),
  });

  const mutationError =
    startMutation.error ?? runningScoreMutation.error ?? finishMutation.error;

  const busy =
    startMutation.isPending ||
    runningScoreMutation.isPending ||
    finishMutation.isPending;

  const canStart = data.status === 'Scheduled';
  const canSetRunningScore = data.status === 'Live';
  const canFinish =
    data.status === 'Scheduled' ||
    data.status === 'Postponed' ||
    data.status === 'Live';

  const runningPrefill: MatchScore = data.runningScore ?? {
    homeGoals: 0,
    awayGoals: 0,
  };

  const kickoffParts = formatKickoffParts(data.scheduledAt, i18n.language);
  const scoreStepper =
    canSetRunningScore &&
    MatchHeroScoreActions({
      homeIncrementLabel: t('detail.scoreStepHomeUp', { name: homeName }),
      homeDecrementLabel: t('detail.scoreStepHomeDown', { name: homeName }),
      awayIncrementLabel: t('detail.scoreStepAwayUp', { name: awayName }),
      awayDecrementLabel: t('detail.scoreStepAwayDown', { name: awayName }),
      disabled: busy,
      onHomeIncrement: () =>
        runningScoreMutation.mutate(
          adjustRunningScore(data.runningScore, 'Home', 1),
        ),
      onHomeDecrement: () =>
        runningScoreMutation.mutate(
          adjustRunningScore(data.runningScore, 'Home', -1),
        ),
      onAwayIncrement: () =>
        runningScoreMutation.mutate(
          adjustRunningScore(data.runningScore, 'Away', 1),
        ),
      onAwayDecrement: () =>
        runningScoreMutation.mutate(
          adjustRunningScore(data.runningScore, 'Away', -1),
        ),
    });

  return (
    <div className="ds-page matches match-detail">
      <PageHead
        title={t('detail.titleVs', { home: homeName, away: awayName })}
        back={
          <BackLink to={`/competitions/${data.competitionId}/matches`}>
            {t('detail.backToMatches')}
          </BackLink>
        }
      />

      <MatchHero
        eyebrow={stageName}
        status={
          <MatchHeroStatus
            status={data.status}
            official={board.source === 'official'}
          />
        }
        home={{
          name: homeName,
          crest: (
            <TeamCrest
              name={homeName}
              logoMediaId={data.home.logoMediaId}
              primaryColor={data.home.primaryColor}
            />
          ),
          scoreActions: scoreStepper ? scoreStepper.home : undefined,
        }}
        away={{
          name: awayName,
          crest: (
            <TeamCrest
              name={awayName}
              logoMediaId={data.away.logoMediaId}
              primaryColor={data.away.primaryColor}
            />
          ),
          scoreActions: scoreStepper ? scoreStepper.away : undefined,
        }}
        center={
          <MatchHeroCenter
            board={board}
            busy={busy}
            canStart={canStart}
            homeName={homeName}
            awayName={awayName}
            kickoffTime={kickoffParts.time}
            onStart={() => startMutation.mutate()}
            startPending={startMutation.isPending}
          />
        }
        meta={
          <>
            {kickoffParts.date ? (
              <MatchHeroMetaItem icon={<CalendarIcon size="sm" />}>
                {kickoffParts.date}
              </MatchHeroMetaItem>
            ) : null}
            {kickoffParts.time ? (
              <MatchHeroMetaItem icon={<ClockIcon size="sm" />}>
                {kickoffParts.time}
              </MatchHeroMetaItem>
            ) : null}
            {stageName ? (
              <MatchHeroMetaItem icon={<PinIcon size="sm" />}>
                {stageName}
              </MatchHeroMetaItem>
            ) : null}
          </>
        }
      />

      <div
        className="match-detail__score-live"
        aria-live="polite"
        aria-label={t('detail.scoreAria', {
          home: homeName,
          away: awayName,
          homeGoals: board.homeLabel,
          awayGoals: board.awayLabel,
        })}
      />

      {board.source === 'live' ? (
        <p className="match-detail__caption">{t('detail.scoreCaptionLive')}</p>
      ) : null}

      {board.source === 'official' ? (
        <p className="match-detail__caption">
          {t('detail.scoreCaptionOfficial')}
        </p>
      ) : null}

      <div className="ds-grid-2 ds-grid-2--major">
        <MatchGoalsPanel match={data} />
        <MatchSheetPanel match={data} />
      </div>

      <MatchSubstitutionsPanel match={data} />

      <MatchDisciplinaryPanel match={data} />

      {(canSetRunningScore || canFinish) && (
        <div className="match-detail__ops" aria-busy={busy}>
          {canSetRunningScore && (
            <section className="ds-panel" aria-labelledby="counter-job-heading">
              <h2 className="matches-panel__head" id="counter-job-heading">
                {t('detail.runningScore')}
              </h2>
              <p className="matches-panel__meta">
                {t('detail.runningScoreHint')}
              </p>
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
        <p className="ds-notice">
          {t('detail.noAction', { status: matchStatusLabel(data.status) })}
        </p>
      )}

      {mutationError && <MutationError error={mutationError} />}
    </div>
  );
}

function MatchHeroStatus({
  status,
  official,
}: {
  status: MatchStatus;
  official: boolean;
}) {
  const { t } = useTranslation('matches');

  if (status === 'Live') {
    return (
      <span className="ds-status-live">
        <span className="ds-live-dot" />
        {matchStatusLabel(status)}
      </span>
    );
  }

  if (status === 'Finished' && official) {
    return (
      <Status density="context" tone="neutral" variant="soft" shape="rounded">
        {`${matchStatusLabel(status)} · ${t('detail.scoreCaptionOfficial')}`}
      </Status>
    );
  }

  const tone =
    status === 'Scheduled' || status === 'Postponed' ? 'info' : 'neutral';

  return (
    <Status density="context" tone={tone} variant="soft" shape="rounded">
      {matchStatusLabel(status)}
    </Status>
  );
}

function MatchHeroCenter({
  board,
  canStart,
  kickoffTime,
  onStart,
  startPending,
  busy,
  homeName,
  awayName,
}: {
  board: ReturnType<typeof displayedScore>;
  canStart: boolean;
  kickoffTime: string | null;
  onStart: () => void;
  startPending: boolean;
  busy: boolean;
  homeName: string;
  awayName: string;
}) {
  const { t } = useTranslation('matches');

  if (canStart) {
    return (
      <>
        <MatchHeroScore pending>{kickoffTime ?? '–'}</MatchHeroScore>
        <button
          type="button"
          className="ds-btn ds-btn--primary"
          disabled={startPending || busy}
          onClick={onStart}
        >
          {startPending ? (
            <PendingLabel>{t('detail.starting')}</PendingLabel>
          ) : (
            <>
              <CheckIcon size="sm" />
              {t('detail.start')}
            </>
          )}
        </button>
      </>
    );
  }

  if (board.source === 'empty') {
    return <MatchHeroScore pending>–</MatchHeroScore>;
  }

  return (
    <MatchHeroScore
      ariaLabel={t('detail.scoreAria', {
        home: homeName,
        away: awayName,
        homeGoals: board.homeLabel,
        awayGoals: board.awayLabel,
      })}
    >
      {board.homeDisplay}
      <span className="ds-match-hero__sep" aria-hidden="true">
        –
      </span>
      {board.awayDisplay}
    </MatchHeroScore>
  );
}

function displayedScore(data: MatchDetail): {
  source: 'official' | 'live' | 'empty';
  homeDisplay: string | number;
  awayDisplay: string | number;
  homeLabel: string;
  awayLabel: string;
} {
  if (data.result) {
    return {
      source: 'official',
      homeDisplay: data.result.homeGoals,
      awayDisplay: data.result.awayGoals,
      homeLabel: String(data.result.homeGoals),
      awayLabel: String(data.result.awayGoals),
    };
  }
  if (data.status === 'Live' && data.runningScore) {
    return {
      source: 'live',
      homeDisplay: data.runningScore.homeGoals,
      awayDisplay: data.runningScore.awayGoals,
      homeLabel: String(data.runningScore.homeGoals),
      awayLabel: String(data.runningScore.awayGoals),
    };
  }
  return {
    source: 'empty',
    homeDisplay: '–',
    awayDisplay: '–',
    homeLabel: '–',
    awayLabel: '–',
  };
}

function parseNonNegativeInt(raw: string): number | null {
  const value = Number(raw);
  if (!Number.isInteger(value) || value < 0) {
    return null;
  }
  return value;
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
  homeName: string;
  awayName: string;
  defaultHome: number;
  defaultAway: number;
  pending: boolean;
  submitLabel: string;
  pendingLabel: string;
  hint: string;
  onSubmit: (homeGoals: number, awayGoals: number) => void;
}) {
  const { t } = useTranslation('matches');
  const id = useId();
  const [homeGoals, setHomeGoals] = useState(String(defaultHome));
  const [awayGoals, setAwayGoals] = useState(String(defaultAway));
  const [localError, setLocalError] = useState<string | null>(null);

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    const home = parseNonNegativeInt(homeGoals);
    const away = parseNonNegativeInt(awayGoals);
    if (home == null || away == null) {
      setLocalError(t('detail.errorGoals'));
      return;
    }
    setLocalError(null);
    onSubmit(home, away);
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
        <p className="ds-notice ds-notice--danger" role="alert">
          {localError}
        </p>
      )}

      <div className="button-row">
        <button
          type="submit"
          className="ds-btn ds-btn--primary"
          disabled={pending}
        >
          {pending ? (
            <PendingLabel>{pendingLabel}</PendingLabel>
          ) : (
            <>
              <CheckIcon size="sm" />
              {submitLabel}
            </>
          )}
        </button>
        <span className="caption">{hint}</span>
      </div>
    </form>
  );
}

function RunningScoreForm({
  homeName,
  awayName,
  defaultScore,
  pending,
  onSubmit,
}: {
  homeName: string;
  awayName: string;
  defaultScore: MatchScore;
  pending: boolean;
  onSubmit: (score: MatchScore) => void;
}) {
  const { t } = useTranslation('matches');
  const id = useId();
  const [homeGoals, setHomeGoals] = useState(String(defaultScore.homeGoals));
  const [awayGoals, setAwayGoals] = useState(String(defaultScore.awayGoals));
  const [localError, setLocalError] = useState<string | null>(null);

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    const home = parseNonNegativeInt(homeGoals);
    const away = parseNonNegativeInt(awayGoals);
    if (home == null || away == null) {
      setLocalError(t('detail.errorGoals'));
      return;
    }
    setLocalError(null);
    onSubmit({ homeGoals: home, awayGoals: away });
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
        <p className="ds-notice ds-notice--danger" role="alert">
          {localError}
        </p>
      )}

      <div className="button-row">
        <button
          type="submit"
          className="ds-btn ds-btn--primary"
          disabled={pending}
        >
          {pending ? (
            <PendingLabel>{t('detail.updatingScore')}</PendingLabel>
          ) : (
            <>
              <CheckIcon size="sm" />
              {t('detail.updateRunningScore')}
            </>
          )}
        </button>
      </div>
    </form>
  );
}
