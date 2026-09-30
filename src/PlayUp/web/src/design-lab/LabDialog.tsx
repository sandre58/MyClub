import { useId, useState } from 'react';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { TextInput } from '../design-system/components/TextInput';
import { Select } from '../design-system/components/Select';
import { CheckIcon, PlusIcon } from '../design-system/icons/contentIcons';
import { CloseIcon } from '../design-system/icons/shellIcons';

/**
 * Design Lab — interactive Dialog chrome (sm / md / closeDisabled / scroll).
 * Reference surface for the DS primitive; not a product page.
 */
export function LabDialog() {
  const [smOpen, setSmOpen] = useState(false);
  const [mdOpen, setMdOpen] = useState(false);
  const [lockedOpen, setLockedOpen] = useState(false);
  const [scrollOpen, setScrollOpen] = useState(false);
  const [format, setFormat] = useState<string | null>('Championship');
  const smFormId = useId();
  const mdFormId = useId();
  const nameId = useId();
  const formatId = useId();
  const stageId = useId();

  return (
    <div className="dlab-dialog">
      <header className="dlab-dialog__intro">
        <p className="ds-eyebrow">Design System</p>
        <h1 className="dlab-dialog__title">Dialog</h1>
        <p className="dlab-dialog__lede">
          Chrome overlay centré — titre, corps, footer = statut fenêtre (gauche,
          largeur fluide) + actions (droite). Fermer = icône X (aria-label). Pas
          de filets. Motion 160 ms. Les alertes de fenêtre (warning / erreur de
          save) vont dans <code>footerStatus</code>, pas dans le corps.
        </p>
      </header>

      <section className="ds-panel dlab-dialog__panel" aria-label="Démos">
        <div className="dlab-dialog__actions">
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            onClick={() => setSmOpen(true)}
          >
            <PlusIcon size="sm" />
            Ouvrir sm (28 rem)
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => setMdOpen(true)}
          >
            <CheckIcon size="sm" />
            Ouvrir md (36 rem)
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => setLockedOpen(true)}
          >
            <CloseIcon size="sm" />
            closeDisabled
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => setScrollOpen(true)}
          >
            <CheckIcon size="sm" />
            Corps scrollable
          </button>
        </div>
        <ul className="dlab-dialog__checklist">
          <li>
            Escape / backdrop / icône Fermer (X) — Escape via pile dismiss DS
            (LIFO : couche active la plus haute uniquement)
          </li>
          <li>Focus initial dans le corps (premier champ)</li>
          <li>Trap Tab · retour de focus au déclencheur</li>
          <li>
            Footer sticky : statut (gauche) + actions (droite) ; stack en étroit
          </li>
          <li>
            Clavier propre au composant (ex. flèches Select) reste local — pas
            une commande applicative globale
          </li>
        </ul>
      </section>

      <Dialog
        open={smOpen}
        onClose={() => setSmOpen(false)}
        title="Nouvelle compétition"
        size="sm"
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              onClick={() => setSmOpen(false)}
            >
              <CloseIcon size="sm" />
              Annuler
            </button>
            <button
              type="submit"
              form={smFormId}
              className="ds-btn ds-btn--primary"
            >
              <PlusIcon size="sm" />
              Créer
            </button>
          </>
        }
      >
        <form
          id={smFormId}
          className="ds-form"
          data-density="comfortable"
          onSubmit={(event) => {
            event.preventDefault();
            setSmOpen(false);
          }}
        >
          <Field label="Nom" htmlFor={nameId} required>
            <TextInput
              id={nameId}
              name="name"
              placeholder="Ex. Championnat U15"
              autoComplete="off"
              required
            />
          </Field>
        </form>
      </Dialog>

      <Dialog
        open={mdOpen}
        onClose={() => setMdOpen(false)}
        title="Configurer la structure"
        size="md"
        footerStatus={
          <p className="ds-notice ds-notice--warning" role="status">
            Exemple : 4/2 places vers la Finale
          </p>
        }
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              onClick={() => setMdOpen(false)}
            >
              <CloseIcon size="sm" />
              Annuler
            </button>
            <button
              type="submit"
              form={mdFormId}
              className="ds-btn ds-btn--primary"
            >
              <CheckIcon size="sm" />
              Enregistrer
            </button>
          </>
        }
      >
        <form
          id={mdFormId}
          className="ds-form"
          data-density="comfortable"
          onSubmit={(event) => {
            event.preventDefault();
            setMdOpen(false);
          }}
        >
          <Field label="Format" htmlFor={formatId}>
            <Select
              id={formatId}
              value={format}
              options={[
                { value: 'Championship', label: 'Championnat' },
                { value: 'Cup', label: 'Coupe' },
              ]}
              onChange={setFormat}
            />
          </Field>
          <Field label="Nom de phase" htmlFor={stageId}>
            <TextInput
              id={stageId}
              name="stage"
              defaultValue="Phase principale"
              autoComplete="off"
            />
          </Field>
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
            <CheckIcon size="sm" />
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
          <CheckIcon size="sm" />
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
            <CheckIcon size="sm" />
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
  );
}
