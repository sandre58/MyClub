import { useTranslation } from 'react-i18next'
import { PlayUpLockupMark } from '../PlayUpLockupMark'
import { PlayUpWordmark } from '../PlayUpWordmark'

/**
 * Accueil brand block — lockup raster mockup v3 (mark + wordmark) + tagline HTML.
 */
export function HomeBrand({ lede }: { lede?: string }) {
  const { t } = useTranslation('home')

  return (
    <header className="ds-home__brand">
      <div className="ds-home__mark-row">
        <div className="ds-home__mark-slot">
          <PlayUpLockupMark />
        </div>
        <div className="ds-home__lockup-copy">
          <h1 className="ds-home__wordmark-heading">
            <PlayUpWordmark surface="home" className="ds-home__wordmark" />
          </h1>
          <p className="ds-home__tagline">{t('tagline')}</p>
        </div>
      </div>
      {lede ? <p className="ds-home__lede">{lede}</p> : null}
    </header>
  )
}
