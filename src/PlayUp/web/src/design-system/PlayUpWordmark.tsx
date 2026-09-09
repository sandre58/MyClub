/**
 * Play'Up wordmark — Accueil and chrome rasters from mockup v3.
 *
 * `home` swaps light/dark assets via CSS when `data-theme` changes
 * (applyTheme remains the sole DOM authority — no React theme prop).
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
      <span
        className={['ds-lockup-wordmark-pair', className]
          .filter(Boolean)
          .join(' ')}
      >
        <img
          className="ds-lockup-wordmark ds-lockup-wordmark--on-light"
          src="/brand/accueil-wordmark.png"
          alt="Play’Up"
          width={406}
          height={112}
          draggable={false}
        />
        <img
          className="ds-lockup-wordmark ds-lockup-wordmark--on-dark"
          src="/brand/accueil-wordmark-on-chrome.png"
          alt=""
          width={406}
          height={112}
          draggable={false}
          aria-hidden="true"
        />
      </span>
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
