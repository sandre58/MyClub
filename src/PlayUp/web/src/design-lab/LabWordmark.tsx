/*
 * Wordmark Play'Up — référence Design Lab (arbitre visuel).
 *
 * IBM Plex Sans bold, obliquée au système d'angle unique (12°), flèche
 * fromStem prolongeant la hampe du U. `skewDeg` sert uniquement à la vue
 * d'arbitrage 9° vs 12° ; en produit, toujours 12°.
 */
export function LabWordmark({
  className,
  skewDeg = 12,
}: {
  className?: string
  skewDeg?: 9 | 12
}) {
  return (
    <span
      className={['dlab-wordmark', className].filter(Boolean).join(' ')}
      data-skew={skewDeg}
    >
      Play'
      <span className="dlab-wordmark__up">
        Up
        <svg
          className="dlab-wordmark__arrow"
          viewBox="0 0 12 12"
          aria-hidden="true"
          focusable="false"
        >
          <path d="M1.5 10.5 L9 3 M4.5 2.5 H9.5 V7.5" />
        </svg>
      </span>
    </span>
  )
}

/** Flèche de tendance — même vocabulaire que la marque, angle système. */
export function TrendIcon({
  direction,
  skewDeg = 12,
}: {
  direction: 'up' | 'down' | 'flat'
  skewDeg?: 9 | 12
}) {
  if (direction === 'flat') {
    return (
      <svg className="dlab-trend" viewBox="0 0 12 12" aria-hidden="true">
        <path d="M2 6 H10" />
      </svg>
    )
  }
  return (
    <svg
      className={`dlab-trend dlab-trend--${direction}`}
      viewBox="0 0 12 12"
      aria-hidden="true"
      data-skew={skewDeg}
    >
      {direction === 'up' ? (
        <path d="M2 10 L9.5 2.5 M5 2 H10 V7" />
      ) : (
        <path d="M2 2 L9.5 9.5 M10 5 V10 H5" />
      )}
    </svg>
  )
}
