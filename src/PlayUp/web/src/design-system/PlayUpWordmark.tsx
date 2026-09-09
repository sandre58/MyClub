/**
 * Play'Up wordmark — Accueil and chrome rasters from mockup v3.
 */
export function PlayUpWordmark({
  className,
  surface = 'chrome',
}: {
  className?: string;
  surface?: 'chrome' | 'home';
}) {
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
    );
  }

  return (
    <img
      className={['ds-lockup-wordmark', className].filter(Boolean).join(' ')}
      src="/brand/accueil-wordmark-on-chrome.png"
      alt=""
      width={406}
      height={112}
      draggable={false}
    />
  );
}
