import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { PageHeader } from '../ui'

/**
 * Classements workspace stub (Phase 18.1).
 * Standings content lands in 18.2 — no ConsultationView fetch here.
 */
export function ClassementsPage() {
  const { competitionId = '' } = useParams()
  const { t } = useTranslation('classements')

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('eyebrow')}
        title={t('title')}
        back={
          competitionId
            ? {
                to: `/competitions/${competitionId}`,
                label: t('back'),
              }
            : undefined
        }
      />
      <p className="muted">{t('stub')}</p>
    </main>
  )
}
