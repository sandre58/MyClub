/** PageHead anomaly hint — one line, same family as positive readiness titles. */
export function structureAnomalyHintTitle(args: {
  t: (key: string, options?: Record<string, unknown>) => string;
}): string {
  return args.t('readiness.anomalyHintTitle');
}
