import { WaitMark, type WaitSize } from '../design-system/components/WaitMark';
import { PanelHead } from '../design-system/components/PanelHead';

export type LabWaitKind = 'b' | 'c';
export type LabWaitScale = WaitSize | 'button';

/**
 * Lab wait atom — B spinner + bar (comparison) · C WaitMark (product treatment).
 */
export function LabWaitAtom({
  kind,
  scale,
  label,
}: {
  kind: LabWaitKind;
  scale: LabWaitScale;
  label?: string;
}) {
  const text = label ?? (scale === 'button' ? 'Traitement…' : 'Chargement…');

  if (kind === 'c' && scale !== 'button') {
    return <WaitMark size={scale}>{text}</WaitMark>;
  }

  const showBar = kind === 'b' && scale !== 'button';
  const glyph = <WaitSpinner />;

  if (scale === 'button') {
    return (
      <button
        type="button"
        className={[
          'ds-btn',
          'ds-btn--primary',
          'dlab-wait',
          `dlab-wait--${kind}`,
          'dlab-wait--button',
        ].join(' ')}
        disabled
        aria-busy="true"
      >
        {glyph}
        {text}
      </button>
    );
  }

  return (
    <p
      className={[
        'dlab-wait',
        `dlab-wait--${kind}`,
        `dlab-wait--${scale}`,
      ].join(' ')}
      role="status"
      aria-live="polite"
    >
      <span className="dlab-wait__row">
        {glyph}
        <span className="dlab-wait__label">{text}</span>
      </span>
      {showBar ? <WaitBar /> : null}
    </p>
  );
}

function WaitSpinner() {
  return <span className="dlab-wait-spin" aria-hidden="true" />;
}

function WaitBar() {
  return (
    <span className="dlab-wait-bar" aria-hidden="true">
      <span className="dlab-wait-bar__run" />
    </span>
  );
}

/**
 * Wait comparison board — C is the product treatment; B remains visible for comparison.
 */
export function LabWait() {
  return (
    <div className="dlab-wait-board ds-page">
      <header className="dlab-wait-board__intro">
        <h1 className="ds-heading">États d’attente</h1>
        <p className="ds-body">
          Product treatment: C — static Home mark, orbiting ring, label
          centered under the animation. Sizes: Home · page · region.
          Product: <code>WaitMark</code> / <code>LoadingState</code>. Buttons:
          spinner (the PNG is not a control glyph).
        </p>
      </header>

      <div className="dlab-wait-board__grid">
        <div className="dlab-wait-board__col-head" aria-hidden="true" />
        <div className="dlab-wait-board__col-head">
          <p className="ds-label">B — spinner + bar</p>
          <p className="ds-meta">
            Anneau currentColor · piste 12° · Chargement…
          </p>
        </div>
        <div className="dlab-wait-board__col-head">
          <p className="ds-label">C — mark + ring (product)</p>
          <p className="ds-meta">WaitMark · 3 tailles · label centré</p>
        </div>

        <p className="dlab-wait-board__row-label ds-eyebrow">Accueil</p>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="b" scale="home" />
        </div>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="c" scale="home" />
        </div>

        <p className="dlab-wait-board__row-label ds-eyebrow">Page</p>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="b" scale="page" />
        </div>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="c" scale="page" />
        </div>

        <p className="dlab-wait-board__row-label ds-eyebrow">Région</p>
        <div className="dlab-wait-board__cell">
          <RegionMock kind="b" />
        </div>
        <div className="dlab-wait-board__cell">
          <RegionMock kind="c" />
        </div>

        <p className="dlab-wait-board__row-label ds-eyebrow">Bouton</p>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="b" scale="button" />
        </div>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="c" scale="button" />
          <p className="ds-meta">
            Même spinner que B — le mark PNG n’est pas un glyphe de bouton.
          </p>
        </div>
      </div>
    </div>
  );
}

function RegionMock({ kind }: { kind: LabWaitKind }) {
  return (
    <div className="dlab-wait-region">
      <section className="ds-panel">
        <PanelHead title="Journée 3" aside="1 / 3 terminé" />
        <p className="dlab-wait-board__ghost ds-tabular">
          FC Nord 2–1 AS Montval
        </p>
        <p className="dlab-wait-board__ghost ds-tabular">United — Racing</p>
      </section>
      <section className="ds-panel">
        <PanelHead title="Classement" aside="Provisoire" />
        <LabWaitAtom kind={kind} scale="region" />
      </section>
    </div>
  );
}
