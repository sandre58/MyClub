/** Shared binding hint keys (regulation i18n) — Structure strip + Regulation hub. */

export function regulationFamilyBindingHintKey(bound: boolean): string {
  return bound ? 'binding.inheritedFromFrame' : 'binding.specificToPhase';
}

export function regulationStagePersonalizedHintKey(): string {
  return 'binding.specificToPhase';
}
