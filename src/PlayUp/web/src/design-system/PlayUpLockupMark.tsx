/**
 * Accueil lockup mark — raster cropped from mockup v3 (fused P + arrow + U).
 * Chrome / favicon keep the SVG `PlayUpMark`. Tagline stays HTML (i18n).
 */
export function PlayUpLockupMark({
  className,
}: {
  className?: string
}) {
  return (
    <img
      className={['ds-lockup-mark', className].filter(Boolean).join(' ')}
      src="/brand/accueil-mark.png?v=2"
      alt=""
      width={157}
      height={186}
      draggable={false}
    />
  )
}
