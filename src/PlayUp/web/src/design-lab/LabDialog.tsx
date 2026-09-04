import { useId, useState } from 'react'
import { Dialog } from '../design-system/components/Dialog'

/**
 * Design Lab — interactive Dialog chrome (sm / md / closeDisabled / scroll).
 * Reference surface for the DS primitive; not a product page.
 */
export function LabDialog() {
  const [smOpen, setSmOpen] = useState(false)
  const [mdOpen, setMdOpen] = useState(false)
  const [lockedOpen, setLockedOpen] = useState(false)
  const [scrollOpen, setScrollOpen] = useState(false)
  const smFormId = useId()
  const mdFormId = useId()

  return (
    <div className="dlab-dialog">
      <header className="dlab-dialog__intro">
        <p className="ds-eyebrow">Design System</p>
        <h1 className="dlab-dialog__title">Dialog</h1>
        <p className="dlab-dialog__lede">
          Chrome overlay centré — titre, corps, footer d’actions à droite,
          Fermer icône. Pas de filets. Motion 160 ms.
        </p>
      </header>

      <section className="ds-panel dlab-dialog__panel" aria-label="Démos">
        <div className="dlab-dialog__actions">
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            onClick={() => setSmOpen(true)}
          >
            Ouvrir sm (28 rem)
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => setMdOpen(true)}
          >
            Ouvrir md (36 rem)
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => setLockedOpen(true)}
          >
            closeDisabled
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => setScrollOpen(true)}
          >
            Corps scrollable
          </button>
        </div>
        <ul className="dlab-dialog__checklist">
          <li>Escape / backdrop / icône Fermer</li>
          <li>Focus initial dans le corps (premier champ)</li>
          <li>Trap Tab · retour de focus au déclencheur</li>
          <li>Footer sticky sans filet quand le corps déborde</li>
        </ul>
      </section>

      <Dialog
        open={smOpen}
        onClose={() => setSmOpen(false)}
        title="Nouvelle compétition"
        size="sm"
        footer={
          <button type="submit" form={smFormId} className="ds-btn ds-btn--primary">
            Créer
          </button>
        }
      >
        <form
          id={smFormId}
          className="dlab-dialog__form"
          onSubmit={(event) => {
            event.preventDefault()
            setSmOpen(false)
          }}
        >
          <label className="dlab-dialog__field">
            Nom
            <input
              name="name"
              placeholder="Ex. Championnat U15"
              autoComplete="off"
            />
          </label>
        </form>
      </Dialog>

      <Dialog
        open={mdOpen}
        onClose={() => setMdOpen(false)}
        title="Configurer la structure"
        size="md"
        footer={
          <button type="submit" form={mdFormId} className="ds-btn ds-btn--primary">
            Enregistrer
          </button>
        }
      >
        <form
          id={mdFormId}
          className="dlab-dialog__form"
          onSubmit={(event) => {
            event.preventDefault()
            setMdOpen(false)
          }}
        >
          <label className="dlab-dialog__field">
            Format
            <select defaultValue="Championship">
              <option value="Championship">Championnat</option>
              <option value="Cup">Coupe</option>
            </select>
          </label>
          <label className="dlab-dialog__field">
            Nom de phase
            <input name="stage" defaultValue="Phase principale" autoComplete="off" />
          </label>
        </form>
      </Dialog>

      <Dialog
        open={lockedOpen}
        onClose={() => setLockedOpen(false)}
        title="Mutation en cours"
        size="sm"
        closeDisabled
        footer={
          <button type="button" className="ds-btn ds-btn--primary" disabled>
            Traitement…
          </button>
        }
      >
        <p className="ds-body">
          Escape, backdrop et Fermer sont bloqués tant que{' '}
          <code>closeDisabled</code> est actif. Utilise le bouton ci-dessous
          pour simuler la fin.
        </p>
        <button
          type="button"
          className="ds-btn ds-btn--secondary"
          onClick={() => setLockedOpen(false)}
        >
          Simuler la fin (corps)
        </button>
      </Dialog>

      <Dialog
        open={scrollOpen}
        onClose={() => setScrollOpen(false)}
        title="Corps scrollable"
        size="sm"
        footer={
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            onClick={() => setScrollOpen(false)}
          >
            OK
          </button>
        }
      >
        {Array.from({ length: 16 }, (_, index) => (
          <p key={index} className="ds-body">
            Ligne {index + 1} — le header et le footer restent visibles ; le
            corps défile. Séparation par rythme, sans filet.
          </p>
        ))}
      </Dialog>
    </div>
  )
}
