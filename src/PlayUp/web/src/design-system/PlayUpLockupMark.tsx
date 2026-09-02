/**
 * Play’Up lockup mark — raster cropped from mockup v3 (fused P + arrow + U).
 * Same asset on Accueil and chrome: the arrow is a real transparency, so the
 * surface (canvas or navy) shows through. Favicon uses the same PNG mark.
 * Tagline stays HTML (i18n).
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
