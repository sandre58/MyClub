/** PageHead readiness hint — one line, same family as positive readiness titles. */
export function structureReadinessHintTitle(args: {
  t: (key: string, options?: Record<string, unknown>) => string;
  severity: 'error' | 'warning';
}): string {
  return args.severity === 'error'
    ? args.t('readiness.anomalyHintTitle')
    : args.t('readiness.warningHintTitle');
}

/** @deprecated Use structureReadinessHintTitle */
export function structureAnomalyHintTitle(args: {
  t: (key: string, options?: Record<string, unknown>) => string;
}): string {
  return structureReadinessHintTitle({ ...args, severity: 'error' });
}
