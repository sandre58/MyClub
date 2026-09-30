import type {
  CompetitionStatus,
  StructureHeritablePartBinding,
  StructureRegulationSummary,
  StructureStageDefaultsBinding,
  StructureStageHubSummary,
  RankingCriterion,
  ReplaceRegulationRequest,
} from '../types';

/** Heritable Match/Standing parts — Domain grain (not scalar fields). */
export type HeritablePartKey =
  | 'matchDuration'
  | 'extraTime'
  | 'penaltyShootout'
  | 'administrativeResult'
  | 'points'
  | 'rankingCriteria';

/** UX families for ConfirmDialog — aggregates Domain parts. */
export type ImpactFamilyKey = 'match' | 'forfeit' | 'standing';

const DOMAIN_MATCH_PARTS: HeritablePartKey[] = [
  'matchDuration',
  'extraTime',
  'penaltyShootout',
  'administrativeResult',
];

const STANDING_PARTS: HeritablePartKey[] = ['points', 'rankingCriteria'];

const FAMILY_PARTS: Record<ImpactFamilyKey, HeritablePartKey[]> = {
  match: ['matchDuration', 'extraTime', 'penaltyShootout'],
  forfeit: ['administrativeResult'],
  standing: ['points', 'rankingCriteria'],
};

const FAMILY_ORDER: ImpactFamilyKey[] = ['match', 'forfeit', 'standing'];

export function applicableHeritableParts(
  binding: StructureStageDefaultsBinding | undefined,
): HeritablePartKey[] {
  if (!binding) {
    return [];
  }
  const parts = [...DOMAIN_MATCH_PARTS];
  if (binding.points != null) {
    parts.push('points');
  }
  if (binding.rankingCriteria != null) {
    parts.push('rankingCriteria');
  }
  return parts;
}

export function partBinding(
  binding: StructureStageDefaultsBinding,
  part: HeritablePartKey,
): StructureHeritablePartBinding | null {
  switch (part) {
    case 'matchDuration':
      return binding.matchDuration;
    case 'extraTime':
      return binding.extraTime;
    case 'penaltyShootout':
      return binding.penaltyShootout;
    case 'administrativeResult':
      return binding.administrativeResult;
    case 'points':
      return binding.points;
    case 'rankingCriteria':
      return binding.rankingCriteria;
  }
}

export function isPartOverridden(
  stage: StructureStageHubSummary,
  part: HeritablePartKey,
): boolean {
  const binding = stage.defaultsBinding;
  if (!binding) {
    return false;
  }
  const entry = partBinding(binding, part);
  return entry != null && !entry.isBound;
}

export function isStagePersonalized(stage: StructureStageHubSummary): boolean {
  const binding = stage.defaultsBinding;
  if (!binding) {
    return false;
  }
  return applicableHeritableParts(binding).some((part) =>
    isPartOverridden(stage, part),
  );
}

function sortedAllowedTypes(types: string[] | null | undefined): string {
  return [...(types ?? [])].sort().join(',');
}

function criteriaKey(criteria: RankingCriterion[] | null | undefined): string {
  return (criteria ?? []).join('|');
}

/** Canonical MatchDuration identity (periods + minutes + half-time). */
function matchDurationKey(source: {
  numberOfPeriods: number;
  durationPerPeriod: number;
  halfTimeDuration?: number | null;
}): string {
  return [
    source.numberOfPeriods,
    source.durationPerPeriod,
    source.halfTimeDuration ?? 0,
  ].join(':');
}

function extraTimeKey(source: {
  hasExtraTime?: boolean;
  extraTimeNumberOfPeriods?: number | null;
  extraTimeDurationPerPeriod?: number | null;
}): string {
  if (!source.hasExtraTime) {
    return 'off';
  }
  return `on:${source.extraTimeNumberOfPeriods ?? 0}:${source.extraTimeDurationPerPeriod ?? 0}`;
}

function shootoutKey(source: {
  hasPenaltyShootout?: boolean;
  penaltyInitialKicksPerTeam?: number | null;
}): string {
  if (!source.hasPenaltyShootout) {
    return 'off';
  }
  return `on:${source.penaltyInitialKicksPerTeam ?? 0}`;
}

function administrativeKey(source: {
  forfeitWinnerGoals?: number | null;
  forfeitLoserGoals?: number | null;
}): string {
  return `${source.forfeitWinnerGoals ?? 0}:${source.forfeitLoserGoals ?? 0}`;
}

function pointsKey(source: {
  winPoints: number;
  drawPoints: number;
  lossPoints: number;
}): string {
  return `${source.winPoints}:${source.drawPoints}:${source.lossPoints}`;
}

/**
 * Which heritable Match/Standing sub-VOs differ between form and competition seed
 * using canonical representations (projection only — Domain remains authority).
 */
export function detectChangedHeritableParts(
  form: ReplaceRegulationRequest,
  seed: StructureRegulationSummary,
): HeritablePartKey[] {
  const changed: HeritablePartKey[] = [];
  if (matchDurationKey(form) !== matchDurationKey(seed)) {
    changed.push('matchDuration');
  }
  if (extraTimeKey(form) !== extraTimeKey(seed)) {
    changed.push('extraTime');
  }
  if (shootoutKey(form) !== shootoutKey(seed)) {
    changed.push('penaltyShootout');
  }
  if (administrativeKey(form) !== administrativeKey(seed)) {
    changed.push('administrativeResult');
  }
  if (pointsKey(form) !== pointsKey(seed)) {
    changed.push('points');
  }
  if (criteriaKey(form.rankingCriteria) !== criteriaKey(seed.rankingCriteria)) {
    changed.push('rankingCriteria');
  }
  return changed;
}

export function detectCompetitionOnlyChanges(
  form: ReplaceRegulationRequest,
  seed: StructureRegulationSummary,
): { entry: boolean; discipline: boolean } {
  const entry =
    form.minimumTeams !== seed.minimumTeams ||
    form.maximumTeams !== seed.maximumTeams;
  const discipline =
    sortedAllowedTypes(form.allowedTypes) !==
    sortedAllowedTypes(seed.allowedTypes);
  return { entry, discipline };
}

export type PartImpactCounts = {
  inherit: number;
  keepOverride: number;
};

export type FamilyImpactLine = {
  family: ImpactFamilyKey;
  changedParts: HeritablePartKey[];
  inherit: number;
  keepOverride: number;
};

export type RegulationImpactPreview = {
  hasChanges: boolean;
  demotesToDraft: boolean;
  competitionOnly: { entry: boolean; discipline: boolean };
  /** UX families with at least one Domain part changed — ConfirmDialog source. */
  families: FamilyImpactLine[];
  byPart: Partial<Record<HeritablePartKey, PartImpactCounts>>;
  changedParts: HeritablePartKey[];
  eligibleStageCount: number;
  stagesUpdatedCount: number;
  /** Heritable parts changed and at least one stage is Running/Suspended. */
  runningIgnored: boolean;
  hasKeptOverrides: boolean;
};

function isEligibleForPropagation(status: string): boolean {
  return status === 'Draft' || status === 'Ready';
}

function isRunningLike(status: string): boolean {
  return status === 'Running' || status === 'Suspended';
}

function aggregateFamily(
  family: ImpactFamilyKey,
  changedParts: HeritablePartKey[],
  eligible: StructureStageHubSummary[],
): FamilyImpactLine | null {
  const familyParts = FAMILY_PARTS[family];
  const changedInFamily = changedParts.filter((part) =>
    familyParts.includes(part),
  );
  if (changedInFamily.length === 0) {
    return null;
  }

  const inheritIds = new Set<string>();
  const keepIds = new Set<string>();

  for (const stage of eligible) {
    const binding = stage.defaultsBinding;
    if (!binding) {
      continue;
    }
    for (const part of changedInFamily) {
      if (STANDING_PARTS.includes(part) && stage.hasStandingRules !== true) {
        continue;
      }
      const entry = partBinding(binding, part);
      if (entry == null) {
        continue;
      }
      if (entry.isBound) {
        inheritIds.add(stage.stageId);
      } else {
        keepIds.add(stage.stageId);
      }
    }
  }

  return {
    family,
    changedParts: changedInFamily,
    inherit: inheritIds.size,
    keepOverride: keepIds.size,
  };
}

/**
 * UI projection of ReplaceRegulation + PropagateBoundDefaults.
 * Does not implement Domain rules — anticipates from DefaultsBinding + changed parts.
 */
export function buildRegulationImpactPreview(
  form: ReplaceRegulationRequest,
  data: {
    regulation: StructureRegulationSummary;
    stages: StructureStageHubSummary[];
    status: CompetitionStatus;
  },
): RegulationImpactPreview {
  const changedParts = detectChangedHeritableParts(form, data.regulation);
  const competitionOnly = detectCompetitionOnlyChanges(form, data.regulation);
  const hasChanges =
    changedParts.length > 0 ||
    competitionOnly.entry ||
    competitionOnly.discipline;

  const eligible = data.stages.filter((stage) =>
    isEligibleForPropagation(stage.status),
  );

  const byPart: Partial<Record<HeritablePartKey, PartImpactCounts>> = {};
  for (const part of changedParts) {
    byPart[part] = { inherit: 0, keepOverride: 0 };
  }

  let stagesUpdatedCount = 0;
  for (const stage of eligible) {
    const binding = stage.defaultsBinding;
    if (!binding) {
      continue;
    }
    let stageTouched = false;
    for (const part of changedParts) {
      if (STANDING_PARTS.includes(part) && stage.hasStandingRules !== true) {
        continue;
      }
      const entry = partBinding(binding, part);
      if (entry == null) {
        continue;
      }
      const counts = byPart[part];
      if (!counts) {
        continue;
      }
      if (entry.isBound) {
        counts.inherit += 1;
        stageTouched = true;
      } else {
        counts.keepOverride += 1;
      }
    }
    if (stageTouched) {
      stagesUpdatedCount += 1;
    }
  }

  const families = FAMILY_ORDER.map((family) =>
    aggregateFamily(family, changedParts, eligible),
  ).filter((line): line is FamilyImpactLine => line != null);

  const hasKeptOverrides = families.some((line) => line.keepOverride > 0);
  const runningIgnored =
    changedParts.length > 0 &&
    data.stages.some((stage) => isRunningLike(stage.status));

  return {
    hasChanges,
    demotesToDraft: hasChanges && data.status === 'Ready',
    competitionOnly,
    families,
    byPart,
    changedParts,
    eligibleStageCount: eligible.length,
    stagesUpdatedCount,
    runningIgnored,
    hasKeptOverrides,
  };
}
