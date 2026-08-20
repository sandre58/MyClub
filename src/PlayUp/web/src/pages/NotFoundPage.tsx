import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

/**
 * Pilot page for 13.5 → Design System + i18n migration (see docs/page-migration.md).
 */
export function NotFoundPage() {
  const { t } = useTranslation('common')

  return (
    <main id="main" className="shell-page">
      <header className="ds-group">
        <p className="ds-label">{t('notFoundPage.eyebrow')}</p>
        <h1 className="ds-heading">{t('notFoundPage.title')}</h1>
        <p className="ds-body">{t('notFoundPage.lede')}</p>
      </header>
      <p>
        <Link className="ds-btn ds-btn--primary" to="/">
          {t('notFoundPage.home')}
        </Link>
      </p>
    </main>
  )
}
