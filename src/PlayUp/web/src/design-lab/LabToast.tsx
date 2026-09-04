import { useEffect } from 'react'
import { clearToasts, notify } from '../design-system/toastStore'

/**
 * Design Lab — interactive Toast demos (tones, stack, sticky-ish error).
 * Host + Toaster live on the board (canvas positioning). Not a product page.
 */
export function LabToast() {
  useEffect(() => {
    return () => {
      clearToasts()
    }
  }, [])

  return (
    <div className="dlab-toast">
      <header className="dlab-toast__intro">
        <p className="ds-eyebrow">Design System</p>
        <h1 className="dlab-toast__title">Toast</h1>
        <p className="dlab-toast__lede">
          Feedback d’événement éphémère — bas-droit du canvas. Soft fill ~16 %,
          sans bordure ; icône tone ; barre 2 px vers la gauche (pause au survol).
          Slot action réservé, pas en V1.
        </p>
      </header>

      <section className="ds-panel dlab-toast__panel" aria-label="Démos">
        <div className="dlab-toast__actions">
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            onClick={() => notify.success('Compétition publiée')}
          >
            Success
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => notify.error('Impossible d’enregistrer le score')}
          >
            Error (sticky-ish)
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => notify.info('Classement recalculé — déjà visible')}
          >
            Info
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() =>
              notify.attention('Deux matchs sans terrain pour la journée')
            }
          >
            Attention
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            onClick={() => {
              notify.success('Équipe ajoutée')
              notify.info('Structure à jour')
              notify.attention('Vérifier les créneaux')
              notify.error('Synchronisation différée')
            }}
          >
            Empiler (max 3)
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            onClick={() => clearToasts()}
          >
            Vider
          </button>
        </div>
        <ul className="dlab-toast__checklist">
          <li>Soft fill ~16 % · sans bordure · shadow overlay</li>
          <li>Icône tone · barre 2 px shrink vers la gauche (pause hover)</li>
          <li>Error ~10 s · success/info ~4 s · attention ~6 s</li>
          <li>Ne vole pas le focus · role status / alert</li>
        </ul>
      </section>
    </div>
  )
}
