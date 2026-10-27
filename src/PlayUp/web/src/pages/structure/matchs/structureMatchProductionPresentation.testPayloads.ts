import type {
  DematerializeDisabledReason,
  MatchProductionMessageKind,
  StructureMatchProductionPresentation,
} from '../../../types';

type HubOverrides = {
  productionVisible?: boolean;
  primaryGaugeMode?: 'materialized' | 'determined';
  primaryCurrent?: number;
  primaryCapacity?: number;
  primaryMessageCode?: string;
  primaryMessageKind?: MatchProductionMessageKind;
  prerequisiteGauge?: StructureMatchProductionPresentation['prerequisiteGauge'];
  followUp?: StructureMatchProductionPresentation['followUp'];
  showPrimaryGauge?: boolean;
  materializeVisible?: boolean;
  materializeEnabled?: boolean;
  materializeCommandCode?: StructureMatchProductionPresentation['materialize']['commandCode'];
  materializeLabelVariant?: StructureMatchProductionPresentation['materialize']['labelVariant'];
  materializeDisabledReason?: string | null;
  dematerializeVisible?: boolean;
  dematerializeEnabled?: boolean;
  /** Presentation-layer codes (e.g. StructureNotMutable) are strings on the wire. */
  dematerializeDisabledReason?: string | null;
  dematerializeScope?: StructureMatchProductionPresentation['dematerialize']['scope'];
};

/** Explicit hub payloads for component tests (no assembler recompute). */
export function readyMatchProductionHub(
  overrides: HubOverrides = {},
): StructureMatchProductionPresentation {
  return {
    productionVisible: overrides.productionVisible ?? true,
    showPrimaryGauge: overrides.showPrimaryGauge ?? true,
    primaryGaugeMode: overrides.primaryGaugeMode ?? 'materialized',
    primaryGauge: {
      current: overrides.primaryCurrent ?? 0,
      capacity: overrides.primaryCapacity ?? 6,
      messageCode: overrides.primaryMessageCode ?? 'Partial',
      messageKind: overrides.primaryMessageKind ?? 'info',
    },
    prerequisiteGauge: overrides.prerequisiteGauge ?? null,
    followUp: overrides.followUp ?? null,
    materialize: {
      visible: overrides.materializeVisible ?? true,
      enabled: overrides.materializeEnabled ?? true,
      commandCode: overrides.materializeCommandCode ?? 'MaterializeMatches',
      labelVariant: overrides.materializeLabelVariant ?? 'create',
      disabledReason: overrides.materializeDisabledReason ?? null,
    },
    dematerialize: {
      visible: overrides.dematerializeVisible ?? false,
      enabled: overrides.dematerializeEnabled ?? false,
      disabledReason: (overrides.dematerializeDisabledReason ??
        null) as DematerializeDisabledReason | null,
      scope: overrides.dematerializeScope ?? null,
    },
  };
}
