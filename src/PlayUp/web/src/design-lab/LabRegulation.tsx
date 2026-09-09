/**
 * Design Lab — Règlement editor scenarios (impact Confirm, binding, garde-fous).
 * Specimens only; product Dialog stays the SoT implementation.
 */
export function LabRegulation() {
  return (
    <div className="dlab-form">
      <header className="dlab-form__intro">
        <p className="ds-eyebrow">Design System</p>
        <h1 className="dlab-form__title">Règlement — édition</h1>
        <p className="dlab-form__lede">
          Surface dédiée aux scénarios produit (Confirm d’impact,
          DefaultsBinding, min≤max, barème soft-warn, Points épinglé, Ready→Draft).
          Les contrôles atomiques vivent dans <strong>Form</strong>. À peupler
          lorsque la Dialog consomme le DS unifié.
        </p>
      </header>
      <section className="ds-panel dlab-form__panel" aria-label="À venir">
        <h2 className="dlab-form__panel-title">Scénarios</h2>
        <p className="dlab-form__hint">
          Placeholder — brancher ici des fixtures qui réutilisent
          FormSection / SwitchPanel / ReorderList / OutcomePoints, sans forker
          le chrome du Dialog produit.
        </p>
      </section>
    </div>
  )
}
