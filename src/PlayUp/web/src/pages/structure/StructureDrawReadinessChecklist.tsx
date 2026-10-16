import { useTranslation } from 'react-i18next';
import {
  CheckCircleIcon,
  StructureIssueIcon,
} from '../../design-system/icons/contentIcons';
import type { DrawReadinessChecklistItem } from './drawUi';

export function StructureDrawReadinessChecklist({
  items,
}: {
  items: readonly DrawReadinessChecklistItem[];
}) {
  const { t } = useTranslation('structure');

  if (items.length === 0) {
    return null;
  }

  return (
    <ul
      className="structure-draw-readiness"
      aria-label={t('fiche.operational.draw.readiness.aria')}
    >
      {items.map((item) => (
        <li key={item.id} className="structure-draw-readiness__item">
          <span
            className={[
              'structure-draw-readiness__icon',
              item.met
                ? 'structure-draw-readiness__icon--met'
                : 'structure-draw-readiness__icon--unmet',
            ].join(' ')}
            aria-hidden="true"
          >
            {item.met ? (
              <CheckCircleIcon size="sm" />
            ) : (
              <StructureIssueIcon size="sm" />
            )}
          </span>
          <span className="structure-draw-readiness__label">
            {item.current != null && item.capacity != null
              ? t(`fiche.operational.draw.readiness.${item.id}`, {
                  current: item.current,
                  capacity: item.capacity,
                })
              : t(`fiche.operational.draw.readiness.${item.id}Plain`)}
          </span>
        </li>
      ))}
    </ul>
  );
}
