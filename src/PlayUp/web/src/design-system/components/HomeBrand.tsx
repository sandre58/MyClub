import { PlayUpMark } from '../PlayUpMark'
import { PlayUpWordmark } from '../PlayUpWordmark'

/**
 * Accueil brand block — monogram dégradé + wordmark (pleine expression marque).
 */
export function HomeBrand({ lede }: { lede?: string }) {
  return (
    <header className="ds-home__brand">
      <div className="ds-home__mark-row">
        <PlayUpMark size={44} variant="gradient" />
        <h1 className="ds-home__wordmark-heading">
          <PlayUpWordmark className="ds-home__wordmark" />
        </h1>
      </div>
      {lede ? <p className="ds-home__lede">{lede}</p> : null}
    </header>
  )
}
