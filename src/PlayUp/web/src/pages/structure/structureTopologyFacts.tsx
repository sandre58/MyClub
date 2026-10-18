import type { ReactNode } from 'react';
import {
  GroupsFormatIcon,
  LayersIcon,
  LegsStatIcon,
  MatchdayStatIcon,
  MatchesStatIcon,
  RoundsStatIcon,
} from '../../design-system/icons/contentIcons';
import type { StructureStageHubSummary } from '../../types';
import { resolvePlacesN, resolvePlacesPerGroup } from './structurePlaces';
import { formatSwissRoundsProgress, swissRoundCounts } from './swissRoundCounts';

export type TopologyCardFact = {
  id: string;
  icon: ReactNode;
  value: string;
};

export function topologyCardFacts(
  stage: StructureStageHubSummary,
  t: (key: string, options?: Record<string, unknown>) => string,
): TopologyCardFact[] {
  const kind = stage.formatKind;
  const placesN = resolvePlacesN(stage);
  const placesFact =
    placesN != null
      ? {
          id: 'places',
          icon: <LayersIcon size="md" aria-hidden="true" />,
          value: t('hub.topologyStatPlaces', { count: placesN }),
        }
      : null;
  const championshipMatchdays = t('hub.topologyStatMatchdays', {
    count: stage.configuredMatchdayCount ?? 0,
  });
  const operationalMatchdays = t('hub.topologyStatMatchdays', {
    count: stage.matchdayCount ?? 0,
  });
  const matches = t('hub.topologyStatMatches', { count: stage.matchCount });

  switch (kind) {
    case 'Cup': {
      return [
        {
          id: 'rounds',
          icon: <RoundsStatIcon size="md" aria-hidden="true" />,
          value: t('hub.topologyStatRounds', {
            count: stage.roundCount ?? 0,
          }),
        },
        {
          id: 'legs',
          icon: <LegsStatIcon size="md" aria-hidden="true" />,
          value:
            (stage.numberOfLegs ?? 1) >= 2
              ? t('hub.topologyLegsReturn')
              : t('hub.topologyLegsSingle'),
        },
        ...(placesFact ? [placesFact] : []),
      ];
    }
    case 'Championship':
      return [
        {
          id: 'matchdays',
          icon: <MatchdayStatIcon size="md" aria-hidden="true" />,
          value: championshipMatchdays,
        },
        {
          id: 'matches',
          icon: <MatchesStatIcon size="md" aria-hidden="true" />,
          value: matches,
        },
        ...(placesFact ? [placesFact] : []),
      ];
    case 'Groups': {
      const groups = stage.groupCount ?? 0;
      const perGroup = resolvePlacesPerGroup(stage);
      const sizeFact =
        groups > 0 && perGroup != null
          ? {
              id: 'grid',
              icon: <GroupsFormatIcon size="md" aria-hidden="true" />,
              value: t('hub.topologyStatGroupGrid', {
                groups,
                size: perGroup,
              }),
            }
          : {
              id: 'groups',
              icon: <GroupsFormatIcon size="md" aria-hidden="true" />,
              value: t('hub.topologyStatGroups', { count: groups }),
            };
      return [
        {
          id: 'matchdays',
          icon: <MatchdayStatIcon size="md" aria-hidden="true" />,
          value: operationalMatchdays,
        },
        {
          id: 'matches',
          icon: <MatchesStatIcon size="md" aria-hidden="true" />,
          value: matches,
        },
        sizeFact,
        ...(placesFact ? [placesFact] : []),
      ];
    }
    case 'Swiss': {
      const { generated, planned } = swissRoundCounts(stage);
      return [
        {
          id: 'swiss',
          icon: <RoundsStatIcon size="md" aria-hidden="true" />,
          value: formatSwissRoundsProgress(t, generated, planned),
        },
        {
          id: 'matches',
          icon: <MatchesStatIcon size="md" aria-hidden="true" />,
          value: matches,
        },
        ...(placesFact ? [placesFact] : []),
      ];
    }
    default:
      return placesFact ? [placesFact] : [];
  }
}
