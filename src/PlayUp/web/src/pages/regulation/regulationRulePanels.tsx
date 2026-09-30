import { useTranslation } from 'react-i18next';
import { Meter, type MeterTone } from '../../design-system/components/Meter';
import { Tooltip } from '../../design-system/components/Tooltip';
import { MatchRulesIcon, TimerIcon } from '../../design-system/icons/contentIcons';
import type { RankingCriterion } from '../../types';
import { criterionLabel } from './regulationCriterionLabel';
import './regulation.css';

type Translate = (key: string, options?: Record<string, unknown>) => string;

export type MatchRulesPanelProps = {
  numberOfPeriods: number;
  durationPerPeriod: number;
  halfTimeDuration?: number | null;
  hasExtraTime: boolean;
  extraTimeNumberOfPeriods?: number | null;
  extraTimeDurationPerPeriod?: number | null;
  hasPenaltyShootout: boolean;
  penaltyInitialKicksPerTeam?: number | null;
};

export function MatchRulesPanel({
  numberOfPeriods,
  durationPerPeriod,
  halfTimeDuration,
  hasExtraTime,
  extraTimeNumberOfPeriods,
  extraTimeDurationPerPeriod,
  hasPenaltyShootout,
  penaltyInitialKicksPerTeam,
}: MatchRulesPanelProps) {
  const { t } = useTranslation('regulation');
  const halfTime = halfTimeDuration ?? 0;
  const regulationPieces = buildPlayClockPieces({
    periodCount: numberOfPeriods,
    minutesPerPeriod: durationPerPeriod,
    breakMinutes: halfTime,
    periodLabel: (n) => periodLabel(n, t),
    breakLabel: t('matchTimeline.break'),
  });
  const extraPieces =
    hasExtraTime && (extraTimeNumberOfPeriods ?? 0) > 0
      ? buildPlayClockPieces({
          periodCount: extraTimeNumberOfPeriods ?? 0,
          minutesPerPeriod: extraTimeDurationPerPeriod ?? 0,
          breakMinutes: 0,
          periodLabel: (n) => t('matchTimeline.extra', { n }),
          breakLabel: t('matchTimeline.break'),
        })
      : [];
  const regulationPlayMinutes = numberOfPeriods * durationPerPeriod;
  const extraPlayMinutes =
    (extraTimeNumberOfPeriods ?? 0) * (extraTimeDurationPerPeriod ?? 0);
  const hasExtra = extraPieces.length > 0;
  const maxMinutes = regulationPlayMinutes + (hasExtra ? extraPlayMinutes : 0);
  const kicks = penaltyInitialKicksPerTeam ?? null;

  return (
    <div className="regulation-match" aria-label={t('matchTimeline.aria')}>
      <div className="regulation-match__layout">
        <MaxDurationRing minutes={maxMinutes} />
        <div className="regulation-match__timelines">
          <div className="regulation-clock">
            <p className="regulation-match__heading">
              {t('matchTimeline.regulationHeading')}
            </p>
            <ClockRow pieces={regulationPieces} />
          </div>

          <div className="regulation-match__extra">
            {hasExtra ? (
              <div className="regulation-match-line">
                <span className="regulation-match-line__label">
                  <TimerIcon size="sm" />
                  <span>{t('matchTimeline.extraHeading')}</span>
                </span>
                <div className="regulation-match-line__visual regulation-clock--compact">
                  <ClockRow pieces={extraPieces} />
                </div>
              </div>
            ) : null}

            {hasPenaltyShootout ? (
              <div
                className="regulation-match-line"
                aria-label={
                  kicks != null
                    ? t('matchTimeline.tabAria', { count: kicks })
                    : t('matchTimeline.tabHeading')
                }
              >
                <span className="regulation-match-line__label">
                  <MatchRulesIcon size="sm" />
                  <span>{t('matchTimeline.tabHeading')}</span>
                </span>
                <div className="regulation-match-line__visual">
                  {kicks != null ? (
                    <span
                      className="regulation-tab-row__dots"
                      aria-hidden="true"
                    >
                      {Array.from({ length: Math.max(kicks, 1) }, (_, i) => (
                        <span key={i} className="regulation-tab__kick" />
                      ))}
                    </span>
                  ) : null}
                </div>
              </div>
            ) : null}
          </div>
        </div>
      </div>
    </div>
  );
}

export type StandingRulesPanelProps = {
  winPoints?: number | null;
  drawPoints?: number | null;
  lossPoints?: number | null;
  rankingCriteria?: RankingCriterion[];
  forfeitWinnerGoals?: number | null;
  forfeitLoserGoals?: number | null;
};

export function StandingRulesPanel({
  winPoints = 0,
  drawPoints = 0,
  lossPoints = 0,
  rankingCriteria,
  forfeitWinnerGoals,
  forfeitLoserGoals,
}: StandingRulesPanelProps) {
  const { t } = useTranslation('regulation');
  const win = winPoints ?? 0;
  const draw = drawPoints ?? 0;
  const loss = lossPoints ?? 0;
  const maxPts = Math.max(win, draw, loss, 1);
  const criteria = rankingCriteria ?? [];
  const forfeitWinner = forfeitWinnerGoals;
  const forfeitLoser = forfeitLoserGoals;
  const showForfeit = forfeitWinner != null && forfeitLoser != null;

  return (
    <div className="regulation-standing">
      <div className="regulation-standing__points">
        <p className="regulation-standing__heading">{t('points.heading')}</p>
        <ul className="regulation-gauges" aria-label={t('points.aria')}>
          <PointGauge
            value={win}
            max={maxPts}
            label={t('points.win')}
            tone="win"
          />
          <PointGauge
            value={draw}
            max={maxPts}
            label={t('points.draw')}
            tone="draw"
          />
          <PointGauge
            value={loss}
            max={maxPts}
            label={t('points.loss')}
            tone="loss"
          />
        </ul>
      </div>

      <div className="regulation-standing__side">
        {criteria.length > 0 ? (
          <div className="regulation-standing__criteria">
            <p className="regulation-standing__heading">
              {t('criteria.heading')}
            </p>
            <ol className="regulation-criteria" aria-label={t('criteria.aria')}>
              {criteria.map((criterion, index) => (
                <li key={criterion} className="regulation-criteria__item">
                  <span className="regulation-criteria__n" aria-hidden="true">
                    {index + 1}
                  </span>
                  <span>{criterionLabel(criterion, t)}</span>
                </li>
              ))}
            </ol>
          </div>
        ) : null}

        {showForfeit ? (
          <div
            className="regulation-forfeit-score"
            aria-label={t('forfeit.aria', {
              winner: forfeitWinner,
              loser: forfeitLoser,
            })}
          >
            <p className="regulation-standing__heading">
              {t('forfeit.heading')}
            </p>
            <p className="regulation-forfeit-score__hint">
              {t('forfeit.hint')}
            </p>
            <div className="regulation-forfeit-score__board" aria-hidden="true">
              <span className="regulation-forfeit-score__goals regulation-forfeit-score__goals--win">
                {forfeitWinner}
              </span>
              <span className="regulation-forfeit-score__sep">–</span>
              <span className="regulation-forfeit-score__goals">
                {forfeitLoser}
              </span>
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}

function MaxDurationRing({ minutes }: { minutes: number }) {
  const { t } = useTranslation('regulation');
  const size = 96;
  const stroke = 7;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const ratio = Math.min(0.92, Math.max(0.55, minutes / 135));
  const dash = circumference * ratio;

  return (
    <Tooltip content={t('matchTimeline.maxHint')}>
      <div
        className="regulation-max"
        aria-label={t('matchTimeline.maxAria', { minutes })}
      >
        <svg
          className="regulation-max__svg"
          width={size}
          height={size}
          viewBox={`0 0 ${size} ${size}`}
          aria-hidden="true"
        >
          <circle
            className="regulation-max__track"
            cx={size / 2}
            cy={size / 2}
            r={radius}
            strokeWidth={stroke}
            fill="none"
          />
          <circle
            className="regulation-max__fill"
            cx={size / 2}
            cy={size / 2}
            r={radius}
            strokeWidth={stroke}
            fill="none"
            strokeDasharray={`${dash} ${circumference - dash}`}
            strokeLinecap="round"
            transform={`rotate(-90 ${size / 2} ${size / 2})`}
          />
        </svg>
        <div className="regulation-max__center">
          <span className="regulation-max__value">{minutes}</span>
          <span className="regulation-max__unit">
            {t('matchTimeline.maxUnit')}
          </span>
          <span className="regulation-max__label">
            {t('matchTimeline.maxLabel')}
          </span>
        </div>
      </div>
    </Tooltip>
  );
}

type MatchClockPiece =
  | {
      key: string;
      kind: 'play';
      label: string;
      minutes: number;
      alt: boolean;
    }
  | {
      key: string;
      kind: 'break';
      label: string;
      minutes: number;
    };

function buildPlayClockPieces({
  periodCount,
  minutesPerPeriod,
  breakMinutes,
  periodLabel: labelFor,
  breakLabel,
}: {
  periodCount: number;
  minutesPerPeriod: number;
  breakMinutes: number;
  periodLabel: (n: number) => string;
  breakLabel: string;
}): MatchClockPiece[] {
  const count = Math.max(periodCount, 0);
  const pieces: MatchClockPiece[] = [];
  for (let i = 0; i < count; i++) {
    pieces.push({
      key: `play-${i}`,
      kind: 'play',
      label: labelFor(i + 1),
      minutes: minutesPerPeriod,
      alt: i % 2 === 1,
    });
    if (i < count - 1 && breakMinutes > 0) {
      pieces.push({
        key: `break-${i}`,
        kind: 'break',
        label: breakLabel,
        minutes: breakMinutes,
      });
    }
  }
  return pieces;
}

function ClockRow({ pieces }: { pieces: MatchClockPiece[] }) {
  if (pieces.length === 0) {
    return null;
  }

  return (
    <div className="regulation-clock__row">
      {pieces.map((piece) =>
        piece.kind === 'break' ? (
          <div
            key={piece.key}
            className="regulation-clock__piece regulation-clock__piece--break"
            style={{ flex: `${Math.max(piece.minutes, 1)} 1 0` }}
          >
            <span className="regulation-clock__label">{piece.label}</span>
            <span className="regulation-clock__bar" aria-hidden="true" />
            <span className="regulation-clock__mins">{piece.minutes}′</span>
          </div>
        ) : (
          <div
            key={piece.key}
            className={[
              'regulation-clock__piece',
              piece.alt ? 'regulation-clock__piece--alt' : '',
            ]
              .filter(Boolean)
              .join(' ')}
            style={{ flex: `${Math.max(piece.minutes, 1)} 1 0` }}
          >
            <span className="regulation-clock__label">{piece.label}</span>
            <span className="regulation-clock__bar" aria-hidden="true" />
            <span className="regulation-clock__mins">{piece.minutes}′</span>
          </div>
        ),
      )}
    </div>
  );
}

function PointGauge({
  value,
  max,
  label,
  tone,
}: {
  value: number;
  max: number;
  label: string;
  tone: 'win' | 'draw' | 'loss';
}) {
  const { t } = useTranslation('regulation');
  const ratio = max > 0 ? Math.max(value === 0 ? 0 : 0.12, value / max) : 0;
  const meterTone: MeterTone =
    tone === 'win' ? 'success' : tone === 'draw' ? 'brand' : 'error';
  return (
    <li className="regulation-gauge">
      <div className="regulation-gauge__top">
        <span className="regulation-gauge__label">{label}</span>
        <span className="regulation-gauge__value">
          {value}
          <small> {t('points.unit')}</small>
        </span>
      </div>
      <Meter ratio={ratio} tone={meterTone} size="lg" clip aria-hidden="true" />
    </li>
  );
}

function periodLabel(n: number, t: Translate) {
  if (n === 1) {
    return t('matchTimeline.periodFirst');
  }
  if (n === 2) {
    return t('matchTimeline.periodSecond');
  }
  return t('matchTimeline.periodNth', { n });
}
