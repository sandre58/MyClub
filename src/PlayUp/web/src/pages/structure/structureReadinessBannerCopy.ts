/**
 * PageHead readiness banner title — problems identified vs priority actions shown.
 */
export function structureReadinessBannerHeadline(args: {
  presentedProblemCount: number;
  priorityActionsShown: number;
  t: (key: string, options?: Record<string, unknown>) => string;
}): string {
  const problems = args.t('readiness.bannerProblemsIdentified', {
    count: args.presentedProblemCount,
  });
  if (args.priorityActionsShown <= 0) {
    return problems;
  }
  const actions = args.t('readiness.bannerPriorityActions', {
    count: args.priorityActionsShown,
  });
  return `${problems} · ${actions}`;
}
