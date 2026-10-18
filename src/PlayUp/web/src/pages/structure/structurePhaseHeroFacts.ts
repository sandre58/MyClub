import type { StructureStageHubSummary } from '../../types';
import { swissRoundCounts } from './swissRoundCounts';

export type StructurePhaseHeroStatKey =
  | 'groups'
  | 'rounds'
  | 'matchdays'
  | 'swiss-rounds'
  | 'places';

export type StructurePhaseHeroStatWire = {
  statKey: StructurePhaseHeroStatKey;
  value: string | number;
};

/**
 * Presentation selector for schematic hero stats — values come from hub wire fields only.
 */
export function resolveStructurePhaseHeroFacts(
  stage: StructureStageHubSummary,
  showPlacementBlock: boolean,
): StructurePhaseHeroStatWire[] {
  const facts: StructurePhaseHeroStatWire[] = [];
  const kind = stage.formatKind;

  if (kind === 'Groups' && (stage.groupCount ?? 0) > 0) {
    facts.push({
      statKey: 'groups',
      value: stage.groupCount!,
    });
  } else if (kind === 'Cup' && (stage.roundCount ?? 0) > 0) {
    facts.push({
      statKey: 'rounds',
      value: stage.roundCount!,
    });
  } else if (kind === 'Championship') {
    facts.push({
      statKey: 'matchdays',
      value: stage.configuredMatchdayCount ?? 0,
    });
  } else if (kind === 'Swiss') {
    const { generated, planned } = swissRoundCounts(stage);
    if ((planned != null && planned > 0) || generated > 0) {
      const isCompleted = stage.status === 'Completed';
      facts.push({
        statKey: 'swiss-rounds',
        value:
          isCompleted
            ? generated
            : planned != null && planned > 0
              ? `${generated}/${planned}`
              : generated,
      });
    }
  }

  if (!showPlacementBlock) {
    const placesN = stage.compositionCapacity;
    if (placesN != null && placesN > 0) {
      facts.push({
        statKey: 'places',
        value: placesN,
      });
    }
  }

  return facts;
}
