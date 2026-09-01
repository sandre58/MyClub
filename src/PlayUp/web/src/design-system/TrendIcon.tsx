/** Trend arrow — same vocabulary as the brand (system angle). */
export function TrendIcon({
  direction,
  skewDeg = 12,
}: {
  direction: 'up' | 'down' | 'flat'
  skewDeg?: 9 | 12
}) {
  if (direction === 'flat') {
    return (
      <svg className="ds-trend" viewBox="0 0 12 12" aria-hidden="true">
        <path d="M2 6 H10" />
      </svg>
    )
  }
  return (
    <svg
      className={`ds-trend ds-trend--${direction}`}
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
