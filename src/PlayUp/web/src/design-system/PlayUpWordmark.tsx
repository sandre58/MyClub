/**
 * Play'Up wordmark — IBM Plex Sans bold.
 * Chrome: 12° skew, arrow after Up.
 * Accueil (`surface="home"`): raster from mockup v3 (italic, gradient Up, U-stem arrow).
 */
export function PlayUpWordmark({
  className,
  skewDeg = 12,
  surface = 'chrome',
}: {
  className?: string
  /** Product chrome always uses 12. 9 is for the Design Lab arbitration view only. */
  skewDeg?: 9 | 12
  surface?: 'chrome' | 'home'
}) {
  const arrow = (
    <svg
      className="ds-wordmark__arrow"
      viewBox="0 0 16 16"
      aria-hidden="true"
      focusable="false"
    >
      <path d="M2 14 L13 3" />
      <path d="M6.5 3 H13 V9.5" />
    </svg>
  )

  if (surface === 'home') {
    return (
      <img
        className={['ds-lockup-wordmark', className].filter(Boolean).join(' ')}
        src="/brand/accueil-wordmark.png"
        alt="Play’Up"
        width={406}
        height={112}
        draggable={false}
      />
    )
  }

  return (
    <span
      className={['ds-wordmark', className].filter(Boolean).join(' ')}
      data-skew={skewDeg}
    >
      Play&apos;
      <span className="ds-wordmark__up">
        Up
        {arrow}
      </span>
    </span>
  )
}
