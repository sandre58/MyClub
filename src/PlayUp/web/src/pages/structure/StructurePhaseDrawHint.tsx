import { useTranslation } from 'react-i18next';
import { DrawPendingIcon } from '../../design-system/icons/contentIcons';
import type { StructureStageHubSummary } from '../../types';

/** Draw execution badge (E6) — fiche header and topology tile (label only, no tooltip). */
export function StructurePhaseDrawHint({
  stage,
  variant = 'fiche',
}: {
  stage: StructureStageHubSummary;
  variant?: 'fiche' | 'topology';
}) {
  const { t } = useTranslation('structure');
  if (!stage.hasDrawRules) {
    return null;
  }
  const badge = stage.drawExecutionBadge ?? 'ToLaunch';

  const key =
    badge === 'ToLaunch'
      ? 'toLaunch'
      : badge === 'InProgress'
        ? 'inProgress'
        : badge === 'ToApply'
          ? 'toApply'
          : 'applied';
  const tone =
    badge === 'ToLaunch'
      ? 'info'
      : badge === 'InProgress'
        ? 'live'
        : badge === 'ToApply'
          ? 'attention'
          : 'done';

  const className =
    variant === 'topology'
      ? `structure-topology__draw-signal structure-topology__draw-signal--${tone}`
      : `structure-fiche__draw-hint structure-phase-draw-hint--${tone}`;

  return (
    <span className={className}>
      <DrawPendingIcon size="sm" aria-hidden="true" />
      <span>{t(`hub.drawExecution.${key}`)}</span>
    </span>
  );
}
