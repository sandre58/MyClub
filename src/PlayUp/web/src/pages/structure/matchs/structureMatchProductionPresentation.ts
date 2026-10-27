import type {
  StructureMaterialize,
  StructureMatchProductionGauge,
  StructureMatchProductionPresentation,
  StructureStageHubSummary,
} from '../../../types';
import {
  isPhaseRosterIncompleteCode,
  resolvePhaseRosterIncompleteMaterializeMessageKey,
} from '../composition/structurePhaseRosterIncompletePresentation';
import { structureDiagnosticIssueCopyFromStage } from '../issues/structurePopulationShortfallPresentation';
import { materializeDisabledCaption } from '../fiche/structureMaterializeCopy';
import { dematerializeDisabledCaption } from '../fiche/structureDematerializeCopy';
import type { RatioMessageGaugeMessageKind } from '../../../design-system/components/RatioMessageGauge';
import type { StripOperationalMessageKind } from '../fiche/structureOperationalStripUi';
import { resolveDrawCtaPoolTone } from '../draw/structureDrawCtaPool';
import { operationalStripPoolTone } from '../fiche/structureOperationalCopy';
export type MatchsProductionGaugeMode = 'materialized' | 'determined';
const MATERIALIZE_DISABLED_REASONS = new Set([
  'StructureNotMutable',
  'NoDeterminedConfrontations',
  'PhaseRosterIncomplete',
  'GroupsNotReady',
  'NoEligibleConfrontations',
  'DrawObsoleteForConsumption',
]);

export type MatchProductionGaugeView = {
  ratio: { filled: number; capacity: number };
  messageKind: StripOperationalMessageKind;
  messageCode: string;
  mode: MatchsProductionGaugeMode;
};

export function stageMatchProductionPresentation(
  stage: StructureStageHubSummary,
): StructureMatchProductionPresentation | null | undefined {
  return stage.matchProductionPresentation;
}

export function matchProductionMaterializeVisible(
  stage: StructureStageHubSummary,
): boolean {
  return stage.matchProductionPresentation?.materialize.visible === true;
}

export function matchProductionDematerializeVisible(
  stage: StructureStageHubSummary,
): boolean {
  return stage.matchProductionPresentation?.dematerialize.visible === true;
}

export function matchProductionMaterializeEnabled(
  stage: StructureStageHubSummary,
): boolean {
  return stage.matchProductionPresentation?.materialize.enabled === true;
}

export function matchProductionDematerializeEnabled(
  stage: StructureStageHubSummary,
): boolean {
  return stage.matchProductionPresentation?.dematerialize.enabled === true;
}

export function matchProductionShowsPrimaryGauge(
  stage: StructureStageHubSummary,
): boolean {
  const presentation = stageMatchProductionPresentation(stage);
  if (!presentation?.productionVisible) {
    return false;
  }
  return presentation.showPrimaryGauge !== false;
}

export function resolveMatchProductionGaugeView(
  stage: StructureStageHubSummary,
): MatchProductionGaugeView | null {
  const presentation = stageMatchProductionPresentation(stage);
  if (
    !presentation?.productionVisible ||
    !matchProductionShowsPrimaryGauge(stage)
  ) {
    return null;
  }

  const gauge = presentation.primaryGauge;
  const mode =
    presentation.primaryGaugeMode === 'determined'
      ? 'determined'
      : 'materialized';

  return {
    ratio: { filled: gauge.current, capacity: gauge.capacity },
    messageKind: mapMessageKind(gauge.messageKind),
    messageCode: gauge.messageCode,
    mode,
  };
}

export function resolveMatchProductionFrozenPartialText(
  t: (key: string, opts?: Record<string, unknown>) => string,
  messageCode: string,
  current: number,
  capacity: number,
): string {
  const key = `fiche.operational.production.stateMessage.${codeToStateSuffix(messageCode, 'materialized')}`;
  const remaining = Math.max(0, capacity - current);
  const msg = t(key, { count: remaining, filled: current, capacity });
  if (msg !== key) {
    return msg;
  }
  return t('fiche.operational.production.statusUnknown');
}

export function resolveMatchProductionFollowUpText(
  t: (key: string, opts?: Record<string, unknown>) => string,
  messageCode: string,
): string {
  const key = `fiche.operational.production.followUp.${messageCode}`;
  const msg = t(key);
  if (msg !== key) {
    return msg;
  }
  return t('fiche.operational.production.statusUnknown');
}

export function resolveMatchProductionMeterText(
  t: (key: string, opts?: Record<string, unknown>) => string,
  gauge: StructureMatchProductionGauge,
  mode: MatchsProductionGaugeMode,
  stage?: Pick<
    StructureStageHubSummary,
    'compositionEntryCount' | 'compositionCapacity'
  > | null,
): string {
  const code = gauge.messageCode;
  if (MATERIALIZE_DISABLED_REASONS.has(code)) {
    const key = isPhaseRosterIncompleteCode(code)
      ? resolvePhaseRosterIncompleteMaterializeMessageKey(
          stage ? structureDiagnosticIssueCopyFromStage(stage) : undefined,
        )
      : `fiche.materialize.disabledReason.${code}`;
    const msg = t(key);
    if (msg !== key) {
      return msg;
    }
  }

  const stateKey = `fiche.operational.production.stateMessage.${codeToStateSuffix(code, mode)}`;
  const remaining = Math.max(0, gauge.capacity - gauge.current);
  const msg = t(stateKey, {
    count: remaining,
    filled: gauge.current,
    capacity: gauge.capacity,
  });
  if (msg !== stateKey) {
    return msg;
  }

  return t('fiche.operational.production.statusUnknown');
}

function codeToStateSuffix(
  code: string,
  mode: MatchsProductionGaugeMode,
): string {
  if (mode === 'determined') {
    switch (code) {
      case 'DeterminedPartial':
        return 'determinedPartial';
      case 'DeterminedComplete':
        return 'determinedComplete';
      case 'NoDeterminedConfrontations':
        return 'noDeterminedConfrontations';
      default:
        break;
    }
  }

  switch (code) {
    case 'Complete':
      return 'complete';
    case 'Partial':
      return 'partial';
    case 'Unavailable':
      return 'unavailable';
    case 'StatusUnknown':
      return 'statusUnknown';
    case 'NoDeterminedConfrontations':
      return 'noDeterminedConfrontations';
    case 'DeterminedPartial':
      return 'determinedPartial';
    case 'DeterminedComplete':
      return 'determinedComplete';
    case 'MatchesBlockedByGroupsPools':
      return 'matchesBlockedByGroupsPools';
    case 'FollowUpExhausted':
      return 'partial';
    case 'FrozenPartial':
      return 'frozenPartial';
    default:
      return code.charAt(0).toLowerCase() + code.slice(1);
  }
}

export function mapMessageKind(
  kind:
    StructureMatchProductionGauge['messageKind'] | StripOperationalMessageKind,
): StripOperationalMessageKind {
  switch (kind) {
    case 'success':
      return 'success';
    case 'attention':
      return 'attention';
    case 'danger':
      return 'danger';
    default:
      return 'info';
  }
}

export function resolveMatchProductionPoolTone(
  gaugeView: MatchProductionGaugeView | null,
): ReturnType<typeof resolveDrawCtaPoolTone> {
  if (!gaugeView) {
    return 'neutral';
  }
  const base = resolveDrawCtaPoolTone(
    gaugeView.ratio.filled,
    gaugeView.ratio.capacity,
  );
  return operationalStripPoolTone(gaugeView.messageKind, base);
}

export function mapPrerequisiteMessageCode(code: string): string {
  switch (code) {
    case 'PrerequisiteComposition':
      return 'composition';
    case 'PrerequisiteGroupsPlacement':
      return 'groupsPlacement';
    case 'PrerequisiteCupEntryRound':
      return 'cupEntryRound';
    case 'PrerequisiteInboundFeed':
      return 'inboundFeed';
    default:
      return code;
  }
}

export function toRatioMessageGaugeKind(
  kind: StripOperationalMessageKind,
): RatioMessageGaugeMessageKind {
  return kind;
}

export function matchProductionDematerializeScope(
  stage: StructureStageHubSummary,
): string | null | undefined {
  return (
    stage.matchProductionPresentation?.dematerialize.scope ??
    stage.dematerialize?.scope ??
    null
  );
}

export function matchProductionMaterializeCommandCode(
  stage: StructureStageHubSummary,
): StructureMaterialize['commandCode'] | null {
  return (
    stage.matchProductionPresentation?.materialize.commandCode ??
    stage.materialize?.commandCode ??
    null
  );
}

export function matchProductionMaterializeActionLabel(
  stage: StructureStageHubSummary,
  tActions: (key: string) => string,
): string {
  const action = stage.matchProductionPresentation?.materialize;
  const materialize = stage.materialize;
  const commandCode =
    action?.commandCode ?? materialize?.commandCode ?? 'MaterializeMatches';
  const labelVariant =
    action?.labelVariant ?? materialize?.labelVariant ?? 'create';
  const complete = labelVariant === 'complete';
  if (commandCode === 'MaterializeFromOccupiedSlots') {
    return complete
      ? tActions('MaterializeFromOccupiedSlotsComplete')
      : tActions('MaterializeFromOccupiedSlots');
  }
  return complete
    ? tActions('MaterializeMatchesComplete')
    : tActions('MaterializeMatches');
}

export function matchProductionMaterializeDisabledCaption(
  stage: StructureStageHubSummary,
  t: (key: string) => string,
): string | null {
  const action = stage.matchProductionPresentation?.materialize;
  if (!action || action.enabled) {
    return null;
  }
  const code = action.disabledReason?.trim();
  if (code) {
    const key = isPhaseRosterIncompleteCode(code)
      ? resolvePhaseRosterIncompleteMaterializeMessageKey(
          structureDiagnosticIssueCopyFromStage(stage),
        )
      : `fiche.materialize.disabledReason.${code}`;
    const msg = t(key);
    if (msg !== key) {
      return msg;
    }
  }
  if (stage.materialize) {
    return materializeDisabledCaption(stage.materialize, t, stage);
  }
  return null;
}

export function matchProductionDematerializeDisabledCaption(
  stage: StructureStageHubSummary,
  t: (key: string) => string,
): string | null {
  const action = stage.matchProductionPresentation?.dematerialize;
  if (!action || action.enabled) {
    return null;
  }
  const code = action.disabledReason?.trim();
  if (code) {
    const key = `fiche.dematerialize.disabledReason.${code}`;
    const msg = t(key);
    if (msg !== key) {
      return msg;
    }
  }
  if (stage.dematerialize) {
    return dematerializeDisabledCaption(stage.dematerialize, t);
  }
  return t('fiche.dematerialize.blockedGeneric');
}
