import { useTranslation } from 'react-i18next';
import {
  CheckCircleIcon,
  StructureIssueIcon,
} from '../../design-system/icons/contentIcons';
import type { DrawReadinessChecklistItem } from './drawUi';

function readinessFactualLabel(
  t: (key: string, options?: Record<string, unknown>) => string,
  item: DrawReadinessChecklistItem,
): string {
  const prefix = 'fiche.operational.draw.readiness';
  switch (item.id) {
    case 'teamsFillPlaces':
      if (item.capacity != null && item.current != null) {
        return t(`${prefix}.teamsFillPlaces`, {
          current: item.current,
          capacity: item.capacity,
        });
      }
      return t(`${prefix}.teamsFillPlacesNoCapacity`, {
        current: item.current ?? 0,
      });
    case 'teamsAllKnown':
      if (item.capacity != null && item.current != null) {
        return t(`${prefix}.teamsAllKnown`, {
          current: item.current,
          capacity: item.capacity,
        });
      }
      return t(`${prefix}.teamsAllKnownNoQuota`, {
        current: item.current ?? 0,
      });
    case 'bracketPlacesFree': {
      const count = item.current ?? 0;
      if (count === 0) {
        return t(`${prefix}.bracketPlacesOccupiedNone`);
      }
      return t(`${prefix}.bracketPlacesOccupiedCount`, { count });
    }
  }
}

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
      {items.map((item) => {
        const factualLabel = readinessFactualLabel(t, item);
        const statusLabel = item.met
          ? t('fiche.operational.draw.readiness.statusMet')
          : t('fiche.operational.draw.readiness.statusUnmet');

        return (
          <li
            key={item.id}
            className="structure-draw-readiness__item"
            aria-label={`${statusLabel}. ${factualLabel}`}
          >
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
            <span className="structure-draw-readiness__label" aria-hidden="true">
              {factualLabel}
            </span>
          </li>
        );
      })}
    </ul>
  );
}
