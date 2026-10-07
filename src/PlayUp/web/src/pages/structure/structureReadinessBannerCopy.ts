/** PageHead readiness banner title — problem count only (actions are listed below). */
export function structureReadinessBannerHeadline(args: {
  presentedProblemCount: number;
  t: (key: string, options?: Record<string, unknown>) => string;
}): string {
  return args.t('readiness.bannerProblemsIdentified', {
    count: args.presentedProblemCount,
  });
}
