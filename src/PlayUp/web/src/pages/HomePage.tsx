import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { PageHeader } from '../ui'

const seedCompetitionId = import.meta.env.VITE_SEED_COMPETITION_ID as
  | string
  | undefined

/**
 * Product entry — Accueil.
 * Primary path is Competition List; seed remains an optional local shortcut.
 */
export function HomePage() {
  const { t } = useTranslation('home')

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('eyebrow')}
        title={t('title')}
        lede={t('lede')}
      />

      <div className="card-grid">
        <Link className="nav-card" to="/competitions">
          <span className="nav-card__title">
            {t('competitions.title')}
            <span className="row__chevron" aria-hidden="true">
              →
            </span>
          </span>
          <span className="nav-card__desc">{t('competitions.desc')}</span>
        </Link>
        {seedCompetitionId && (
          <Link className="nav-card" to={`/competitions/${seedCompetitionId}`}>
            <span className="nav-card__title">
              {t('seed.title')}
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
            <span className="nav-card__desc">
              {t('seed.descBefore')} <code>.env.local</code>
              {t('seed.descAfter')}
            </span>
          </Link>
        )}
      </div>

      <p className="caption">
        {t('deepLinksBefore')} <code>/stages/:id</code>,{' '}
        <code>/stages/:id/matches</code> {t('deepLinksAnd')}{' '}
        <code>/matches/:id</code>.
      </p>
    </main>
  )
}
