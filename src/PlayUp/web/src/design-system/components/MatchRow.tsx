import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

/**
 * Match row — state-driven score cell + aside (Design Lab grammar).
 */
export function MatchRow({
  home,
  away,
  score,
  aside,
  className,
  to,
  ariaLabel,
  scoreMuted = false,
}: {
  home: { name: string; crest: ReactNode }
  away: { name: string; crest: ReactNode }
  score: ReactNode
  aside?: ReactNode
  className?: string
  to?: string
  ariaLabel?: string
  scoreMuted?: boolean
}) {
  const classes = ['ds-match-row', className].filter(Boolean).join(' ')
  const body = (
    <>
      <span className="ds-match-row__team ds-match-row__team--home">
        <span className="ds-match-row__name">{home.name}</span>
        {home.crest}
      </span>

      <span
        className={
          scoreMuted
            ? 'ds-match-row__score'
            : 'ds-match-row__score ds-num ds-num-row-score'
        }
        data-muted={scoreMuted ? 'true' : undefined}
      >
        {score}
      </span>

      <span className="ds-match-row__team ds-match-row__team--away">
        {away.crest}
        <span className="ds-match-row__name">{away.name}</span>
      </span>

      {aside ? (
        <span className="ds-match-row__aside">{aside}</span>
      ) : (
        <span className="ds-match-row__aside" aria-hidden="true">
          ›
        </span>
      )}
    </>
  )

  if (to) {
    return (
      <Link className={classes} to={to} aria-label={ariaLabel}>
        {body}
      </Link>
    )
  }

  return <div className={classes}>{body}</div>
}

export function MatchRowScore({
  home,
  away,
  muted = false,
}: {
  home: string | number
  away: string | number
  muted?: boolean
}) {
  if (muted) {
    return <>{home}</>
  }

  return `${home}–${away}`
}
