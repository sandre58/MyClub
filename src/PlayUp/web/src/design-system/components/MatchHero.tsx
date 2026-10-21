import type { ReactNode } from 'react';

export type MatchHeroTeam = {
  name: string;
  crest: ReactNode;
  scoreActions?: ReactNode;
};

/**
 * Match Hero — canonical object header (scheduled / live / finished).
 * Center slot carries score or kickoff + primary action.
 */
export function MatchHero({
  eyebrow,
  status,
  home,
  away,
  center,
  meta,
  className,
}: {
  eyebrow?: ReactNode;
  status?: ReactNode;
  home: MatchHeroTeam;
  away: MatchHeroTeam;
  center: ReactNode;
  meta?: ReactNode;
  className?: string;
}) {
  return (
    <section
      className={['ds-panel ds-match-hero', className]
        .filter(Boolean)
        .join(' ')}
    >
      <div className="ds-match-hero__top">
        {eyebrow ? <span className="ds-eyebrow">{eyebrow}</span> : <span />}
        {status}
      </div>

      <div className="ds-match-hero__stage">
        <div className="ds-match-hero__team">
          {home.crest}
          <span className="ds-match-hero__name">{home.name}</span>
          {home.scoreActions}
        </div>

        <div className="ds-match-hero__center">{center}</div>

        <div className="ds-match-hero__team">
          {away.crest}
          <span className="ds-match-hero__name">{away.name}</span>
          {away.scoreActions}
        </div>
      </div>

      {meta ? <div className="ds-match-hero__meta">{meta}</div> : null}
    </section>
  );
}

export function MatchHeroScore({
  children,
  pending = false,
  ariaLabel,
}: {
  children: ReactNode;
  pending?: boolean;
  ariaLabel?: string;
}) {
  return (
    <span
      className="ds-match-hero__score ds-num ds-num-score"
      data-pending={pending ? 'true' : undefined}
      aria-label={ariaLabel}
    >
      {children}
    </span>
  );
}

export function MatchHeroSideScoreActions({
  incrementLabel,
  decrementLabel,
  onIncrement,
  onDecrement,
  disabled = false,
}: {
  incrementLabel: string;
  decrementLabel: string;
  onIncrement: () => void;
  onDecrement: () => void;
  disabled?: boolean;
}) {
  return (
    <div className="ds-match-hero__score-actions">
      <button
        type="button"
        className="ds-match-hero__score-btn"
        aria-label={incrementLabel}
        disabled={disabled}
        onClick={onIncrement}
      >
        +
      </button>
      <button
        type="button"
        className="ds-match-hero__score-btn"
        aria-label={decrementLabel}
        disabled={disabled}
        onClick={onDecrement}
      >
        −
      </button>
    </div>
  );
}

export function MatchHeroScoreActions({
  onHomeIncrement,
  onHomeDecrement,
  onAwayIncrement,
  onAwayDecrement,
  homeIncrementLabel,
  homeDecrementLabel,
  awayIncrementLabel,
  awayDecrementLabel,
  disabled = false,
}: {
  onHomeIncrement: () => void;
  onHomeDecrement: () => void;
  onAwayIncrement: () => void;
  onAwayDecrement: () => void;
  homeIncrementLabel: string;
  homeDecrementLabel: string;
  awayIncrementLabel: string;
  awayDecrementLabel: string;
  disabled?: boolean;
}) {
  return {
    home: (
      <MatchHeroSideScoreActions
        incrementLabel={homeIncrementLabel}
        decrementLabel={homeDecrementLabel}
        onIncrement={onHomeIncrement}
        onDecrement={onHomeDecrement}
        disabled={disabled}
      />
    ),
    away: (
      <MatchHeroSideScoreActions
        incrementLabel={awayIncrementLabel}
        decrementLabel={awayDecrementLabel}
        onIncrement={onAwayIncrement}
        onDecrement={onAwayDecrement}
        disabled={disabled}
      />
    ),
  };
}

export function MatchHeroMetaItem({
  icon,
  children,
}: {
  icon?: ReactNode;
  children: ReactNode;
}) {
  return (
    <span className="ds-match-hero__meta-item">
      {icon}
      {children}
    </span>
  );
}
