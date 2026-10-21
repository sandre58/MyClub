export type OverviewPodiumStep = {
  rank: 1 | 2 | 3;
  name: string;
  subtitle?: string;
  testId?: string;
};

/** Visual order: 2 — 1 — 3 (Completed podium). */
const PODIUM_ORDER = [2, 1, 3] as const;

export function OverviewPodium({
  steps,
  testId,
  presentation,
}: {
  steps: OverviewPodiumStep[];
  testId?: string;
  presentation?: string;
}) {
  const byRank = (rank: number) => steps.find((step) => step.rank === rank);

  return (
    <div
      className="ds-overview-podium"
      data-testid={testId}
      data-presentation={presentation}
    >
      {PODIUM_ORDER.map((rank) => {
        const step = byRank(rank);
        if (!step) {
          return null;
        }
        return (
          <div
            key={step.rank}
            className="ds-overview-podium__step"
            data-rank={step.rank}
            data-testid={step.testId}
          >
            <div className="ds-overview-podium__block ds-num">{step.rank}</div>
            <span className="ds-overview-podium__name">{step.name}</span>
            {step.subtitle ? (
              <span className="ds-overview-podium__subtitle">
                {step.subtitle}
              </span>
            ) : null}
          </div>
        );
      })}
    </div>
  );
}
