import type { DrawConstraintType, StructureFormatKind } from '../../../types';

/**
 * Read-only parameters shown in the draw dialog.
 * Pots × groups are the only inputs the generator consumes.
 * Avoidance constraints are filtered out, `numberOfSeeds` is not wired, and
 * `MaxSameAssociationPerGroup` fails generation because no constraint context
 * is passed — every stored constraint and any seed count is marked not applied.
 */
export type DrawParameterFact =
  | {
      id: 'potsGroups';
      kind: 'applied';
      pots: number;
      groups: number;
    }
  | {
      id: 'seeds';
      kind: 'notApplied';
      seeds: number;
    }
  | {
      id: string;
      kind: 'notApplied';
      constraintType: DrawConstraintType;
      maxPerGroup?: number | null;
    };

export function resolveDrawParameterFacts(input: {
  formatKind: StructureFormatKind | null | undefined;
  numberOfPots: number | null | undefined;
  groupCount: number | null | undefined;
  numberOfSeeds: number | null | undefined;
  constraints?:
    | readonly {
        type: DrawConstraintType;
        maxPerGroup?: number | null;
      }[]
    | null;
}): DrawParameterFact[] {
  const facts: DrawParameterFact[] = [];
  const pots = input.numberOfPots ?? null;
  const groups = input.groupCount ?? null;
  if (
    input.formatKind === 'Groups' &&
    pots != null &&
    pots > 0 &&
    groups != null &&
    groups > 0
  ) {
    facts.push({ id: 'potsGroups', kind: 'applied', pots, groups });
  }

  const seeds = input.numberOfSeeds ?? null;
  if (seeds != null && seeds > 0) {
    facts.push({ id: 'seeds', kind: 'notApplied', seeds });
  }

  (input.constraints ?? []).forEach((constraint, index) => {
    facts.push({
      id: `constraint-${constraint.type}-${index}`,
      kind: 'notApplied',
      constraintType: constraint.type,
      maxPerGroup: constraint.maxPerGroup,
    });
  });

  return facts;
}
