export type CycleStepState = 'current' | 'done' | 'todo'

export function CycleLine({
  steps,
  current,
  done = [],
  'aria-label': ariaLabel = 'Cycle de vie',
}: {
  steps: string[]
  current: string
  done?: string[]
  'aria-label'?: string
}) {
  return (
    <div className="ds-cockpit-cycle" aria-label={ariaLabel}>
      {steps.map((step) => {
        const state: CycleStepState =
          step === current ? 'current' : done.includes(step) ? 'done' : 'todo'
        return (
          <span key={step} data-state={state}>
            {step}
          </span>
        )
      })}
    </div>
  )
}
