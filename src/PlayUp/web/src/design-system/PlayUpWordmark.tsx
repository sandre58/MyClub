/**
 * Play'Up wordmark — IBM Plex Sans bold, system angle (12°), arrow fromStem.
 * SoT: Identité · Design Lab · PlayUpMark (monogram companion).
 */
export function PlayUpWordmark({
  className,
  skewDeg = 12,
}: {
  className?: string
  /** Product code always uses 12. 9 is for the Design Lab arbitration view only. */
  skewDeg?: 9 | 12
}) {
  return (
    <span
      className={['ds-wordmark', className].filter(Boolean).join(' ')}
      data-skew={skewDeg}
    >
      Play&apos;
      <span className="ds-wordmark__up">
        Up
        <svg
          className="ds-wordmark__arrow"
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
