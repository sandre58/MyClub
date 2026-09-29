import { Chip } from '../design-system/components/Chip';
import { Status } from '../design-system/components/Status';
import { Tooltip } from '../design-system/components/Tooltip';
import { PencilIcon } from '../design-system/icons/contentIcons';

/**
 * Design Lab — Tooltip specimens (Hint + DisabledReason + Label).
 * Product tooltips use the surface (Overlay) treatment; the ink mock is comparison-only.
 */
export function LabTooltip() {
  return (
    <div className="dlab-toast">
      <header className="dlab-toast__intro">
        <p className="ds-eyebrow">Design System</p>
        <h1 className="dlab-toast__title">Tooltip</h1>
        <p className="dlab-toast__lede">
          Hint, DisabledReason, et Labels courts sur icon-only (cas par cas).
          Desktop : hover 400&nbsp;ms, focus immédiat. Mobile : long-press sur
          action primaire, tap-toggle sinon. Texte seul ; Popover pour le riche.
          Surface Overlay (= Popover) ; ombre locale plus courte que{' '}
          <code>--shadow-overlay</code>.
        </p>
      </header>

      <section className="ds-panel dlab-toast__panel" aria-label="Hint">
        <h2 className="dlab-form__panel-title">Hint</h2>
        <div className="dlab-toast__actions">
          <Tooltip content="Victoire — points attribués au classement">
            <Chip tone="win">3</Chip>
          </Tooltip>
          <Tooltip content="Cette phase diffère du cadre compétition">
            <Status density="compact" tone="attention">
              Personnalisée
            </Status>
          </Tooltip>
          <Tooltip content="Pots utilisés pour le tirage au sort">
            <span className="ds-chip ds-chip--soft">4 pots</span>
          </Tooltip>
        </div>
      </section>

      <section
        className="ds-panel dlab-toast__panel"
        aria-label="DisabledReason"
      >
        <h2 className="dlab-form__panel-title">DisabledReason</h2>
        <div className="dlab-toast__actions">
          <Tooltip content="Le plafond d’équipes est atteint">
            <button type="button" className="ds-btn ds-btn--primary" disabled>
              Ajouter
            </button>
          </Tooltip>
          <Tooltip content="Lecture seule — compétition verrouillée">
            <button
              type="button"
              className="ds-btn ds-btn--ghost ds-icon-button"
              disabled
              aria-label="Modifier"
            >
              <PencilIcon size="sm" />
            </button>
          </Tooltip>
        </div>
      </section>

      <section className="ds-panel dlab-toast__panel" aria-label="Label">
        <h2 className="dlab-form__panel-title">Label (icon-only)</h2>
        <div className="dlab-toast__actions">
          <Tooltip content="Modifier">
            <button
              type="button"
              className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
              aria-label="Modifier Racing Sablons"
            >
              <PencilIcon size="sm" />
            </button>
          </Tooltip>
        </div>
        <p className="dlab-form__hint">
          Tooltip court · <code>aria-label</code> peut rester plus riche (nom).
        </p>
      </section>

      <section
        className="ds-panel dlab-toast__panel"
        aria-label="Surface vs ink"
      >
        <h2 className="dlab-form__panel-title">Surface vs ink</h2>
        <p className="dlab-form__hint">
          Lab comparison — toggle Light/Dark in Preferences to judge both
          themes. <strong>Product treatment: surface</strong> (Overlay family).
          ink / on-ink is comparison-only (OS-tip contrast, breaks from Popover).
        </p>
        <div className="dlab-tooltip-compare">
          <figure className="dlab-tooltip-compare__item">
            <div
              className="dlab-tooltip-mock dlab-tooltip-mock--surface"
              aria-hidden="true"
            >
              Victoire — 3 points
            </div>
            <figcaption>
              <strong>surface</strong> (product) · border · short shadow
            </figcaption>
          </figure>
          <figure className="dlab-tooltip-compare__item">
            <div
              className="dlab-tooltip-mock dlab-tooltip-mock--ink"
              aria-hidden="true"
            >
              Victoire — 3 points
            </div>
            <figcaption>
              <strong>ink</strong> (comparison-only) · OS-like tip
            </figcaption>
          </figure>
        </div>
      </section>

      <section className="ds-panel dlab-toast__panel" aria-label="Checklist">
        <ul className="dlab-toast__checklist">
          <li>
            surface + border · radius-control 4px · ombre locale 0 2px 6px (pas
            --shadow-overlay)
          </li>
          <li>role=&quot;tooltip&quot; · aria-describedby · pas de focus trap</li>
          <li>
            Une seule ouverte · Escape / tap extérieur · auto-dismiss ~3&nbsp;s
            mobile
          </li>
          <li>
            Labels icon-only au cas par cas — ne pas déduire Tooltip de tout{' '}
            <code>title</code>
          </li>
        </ul>
      </section>
    </div>
  );
}
