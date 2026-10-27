import type { StructureDematerialize } from '../../../types';

export function dematerializeDisabledCaption(
  dematerialize: StructureDematerialize,

  t: (key: string) => string,
): string {
  const code = dematerialize.disabledReason?.trim();

  if (code) {
    const key = `fiche.dematerialize.disabledReason.${code}`;

    const msg = t(key);

    if (msg !== key) {
      return msg;
    }
  }

  return t('fiche.dematerialize.blockedGeneric');
}
