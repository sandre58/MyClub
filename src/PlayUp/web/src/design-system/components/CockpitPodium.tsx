export type CockpitPodiumStep = {
  rank: 1 | 2 | 3
  name: string
  subtitle?: string
  testId?: string
}

/** Visual order: 2 — 1 — 3 (Lab Terminée). */
const PODIUM_ORDER = [2, 1, 3] as const

export function CockpitPodium({
  steps,
  testId,
  presentation,
}: {
  steps: CockpitPodiumStep[]
  testId?: string
  presentation?: string
}) {
  const byRank = (rank: number) => steps.find((step) => step.rank === rank)

  return (
    <div
      className="ds-cockpit-podium"
      data-testid={testId}
      data-presentation={presentation}
    >
      {PODIUM_ORDER.map((rank) => {
        const step = byRank(rank)
        if (!step) {
          return null
        }
        return (
          <div
            key={step.rank}
            className="ds-cockpit-podium__step"
            data-rank={step.rank}
            data-testid={step.testId}
          >
            <div className="ds-cockpit-podium__block ds-num">{step.rank}</div>
            <span className="ds-cockpit-podium__name">{step.name}</span>
            {step.subtitle ? (
              <span className="ds-cockpit-podium__subtitle">{step.subtitle}</span>
            ) : null}
          </div>
        )
      })}
    </div>
  )
}
