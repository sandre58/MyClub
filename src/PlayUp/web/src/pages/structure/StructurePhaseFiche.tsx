import { useIsFetching, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageOverview,
  fetchStageSchematic,
  releaseDrawAlignedPlacements,
  renameStage,
  replaceStageDrawRules,
} from '../../api';
import { ConfirmDialog } from '../../design-system/components/ConfirmDialog';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  GroupsFormatIcon,
  LayersIcon,
  MatchdayStatIcon,
  MatchesStatIcon,
  PlusIcon,
  PencilIcon,
  RoundsStatIcon,
  UnlockIcon,
} from '../../design-system/icons/contentIcons';
import { structureFormatKindLabel } from '../../i18n/enumLabels';
import { queryKeys } from '../../queryKeys';
import { notify } from '../../design-system/toastStore';
import { MutationError } from '../../ui';
import { scheduleStructureRefresh } from './structureInvalidation';
import {
  patchDrawRulesDeactivatedOptimistic,
  patchStageNameOptimistic,
} from './structureQueryCache';
import type {
  StructureStageHubSummary,
  StructureView,
} from '../../types';
import {
  EditSkeletonDialog,
  QualificationRulesDialog,
  ProgressionRulesDialog,
  RemovePhaseDialog,
} from './StructureGraphDialogs';
import { StructurePlacementAwardDialog } from './StructurePlacementAwardDialog';
import { StructureManualPlacementDialog } from './StructureManualPlacementDialog';
import {
  DrawRulesDialog,
  MatchRulesDialog,
  StandingRulesDialog,
  TieFormatDialog,
} from './StructureRegulationDialogs';
import { StructureDrawDialog } from './StructureDrawDialog';
import { StructureCompositionDialog } from './StructureCompositionDialog';
import { StructurePhaseFicheHeader } from './StructurePhaseFicheHeader';
import { StructurePhaseSchematicAndAside } from './StructurePhaseSchematicAndAside';
import { StructurePhaseRailIssueBanner } from './structurePhaseRailIssueBanner';
import { materializePresentationForStage } from './structureOperationalCopy';
import { shouldShowAsideMatchsTile } from './structureMatchsSectionUi';
import { StructureOperationalIssueBanner } from './StructureOperationalIssueBanner';
import {
  entryPlaceProjectionFromSchematic,
  placedEntryIdsFromSchematic,
} from './structureEntryPlacementProjection';
import { countSchematicOccupiedPlaces } from './structurePlaceOccupancy';
import { showProductionOperationalSection } from './structureOperationalCopy';
import {
  areDrawRulesLockedByExecution,
  drawResolutionPlacementCount,
  pickReleasableCancelledDraw,
  stageHasPublishedAppliedDraw,
  pickActiveDraw,
  resolveDrawCreateBlockPresentation,
  resolveDrawReadinessChecklist,
  resolveStageDrawCreateGate,
} from './drawUi';
import {
  isSlotFeedAwaitingDraw,
  structureDiagnosticPresentation,
  structureIssueBannersForTile,
  structureIssuesForTile,
} from './structureDiagnosticPresentation';
import { structureDiagnosticContextFromStage } from './structureDiagnosticContext';
import {
  stageHasPopulationShortfall,
} from './structureReadinessSummary';
import { outboundSortiesFeeds } from './structureSortiesIntentFeed';
import {
  expectedPopulationOccupancy,
  inboundPopulationConfiguredVolume,
} from './structurePopulationVolume';
import {
  isMatchFrameBound,
  isStandingFrameBound,
  relevantPhaseSections,
  type StructureSectionId,
} from './structureHubSections';
import { resolvePlacesN } from './structurePlaces';
import { swissRoundCounts } from './swissRoundCounts';
import type { StructureFocusId } from './structureNavigation';
import { listCupManualPlaces } from './manualPlacementUi';
import '../regulation/regulation-shared.css';

import {
  exitKindsPresent,
  groupFeeds,
  inboundFeeds,
  outboundFeeds,
  type ExitKind,
  type FeedGroup,
} from './structurePhaseFeeds';
import { CompositionPopulationGauge } from './CompositionPopulationGauge';
import { StructurePhaseCompositionBand } from './StructurePhaseCompositionBand';
import { CompositionQualifiersResolvedMeta } from './CompositionQualifiersResolvedMeta';
import { ExitKindMenu, listComposedAffectationEntries } from './StructurePhaseRails';
import { StructurePhaseRegulationStrip } from './StructurePhaseRegulationStrip';

type EditTarget =
  | 'qualification'
  | 'progression'
  | 'placement'
  | 'tirage-activate'
  | 'tirage-params'
  | 'confrontation'
  | 'matchs'
  | 'classement'
  | 'rebind-match'
  | 'rebind-standing'
  | null;

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';
const compactDangerIcon =
  'ds-btn ds-btn--destructive ds-icon-button ds-icon-button--compact';

function stageActions(stage: StructureStageHubSummary): string[] {
  return stage.actions ?? [];
}

export function StructurePhaseFiche({
  data,
  stage,
  singlePhaseHub: _singlePhaseHub = false,
  onSelectStage,
  initialEdit,
  initialCompose,
  initialFocus,
  initialManual,
  initialDrawWorkflow,
  onInitialEditConsumed,
}: {
  data: StructureView;
  stage: StructureStageHubSummary | null;
  /** Hub has a single phase — context lives in the fiche header (no topology column). */
  singlePhaseHub?: boolean;
  onSelectStage?: (stageId: string) => void;
  initialEdit?: StructureSectionId | null;
  initialCompose?: boolean;
  initialFocus?: StructureFocusId | null;
  initialManual?: boolean;
  initialDrawWorkflow?: boolean;
  onInitialEditConsumed?: () => void;
}) {
  const { t, i18n } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tDraw } = useTranslation('draw');
  const queryClient = useQueryClient();
  const structureHubBackgroundRefresh =
    useIsFetching({
      queryKey: queryKeys.competitions.structure(data.competitionId),
    }) > 0;
  const [edit, setEdit] = useState<EditTarget>(null);
  const [rulesEditStage, setRulesEditStage] =
    useState<StructureStageHubSummary | null>(null);
  const [removeOpen, setRemoveOpen] = useState(false);
  const [skeletonOpen, setSkeletonOpen] = useState(false);
  const [editingName, setEditingName] = useState(false);
  const [nameDraft, setNameDraft] = useState('');
  const [drawWorkflowOpen, setDrawWorkflowOpen] = useState(false);
  const [deactivateDrawOpen, setDeactivateDrawOpen] = useState(false);
  const [releaseConfirmOpen, setReleaseConfirmOpen] = useState(false);
  const [composeOpen, setComposeOpen] = useState(false);
  const [composeFocusSearch, setComposeFocusSearch] = useState(false);
  const [manualOpen, setManualOpen] = useState(false);
  const [manualSlotKey, setManualSlotKey] = useState<string | null>(null);

  useEffect(() => {
    setEdit(null);
    setRulesEditStage(null);
    setRemoveOpen(false);
    setSkeletonOpen(false);
    setEditingName(false);
    setNameDraft('');
    setDrawWorkflowOpen(false);
    setDeactivateDrawOpen(false);
    setReleaseConfirmOpen(false);
    setComposeOpen(false);
    setComposeFocusSearch(false);
    setManualOpen(false);
    setManualSlotKey(null);
  }, [stage?.stageId]);

  const deactivateDrawMutation = useMutation({
    mutationFn: () => replaceStageDrawRules(stage!.stageId, { clear: true }),
    onSuccess: () => {
      setDeactivateDrawOpen(false);
      patchDrawRulesDeactivatedOptimistic(
        queryClient,
        data.competitionId,
        stage!.stageId,
      );
    },
  });

  const renameMutation = useMutation({
    mutationFn: (name: string) => renameStage(stage!.stageId, name),
    onSuccess: (_data, name) => {
      setEditingName(false);
      patchStageNameOptimistic(
        queryClient,
        data.competitionId,
        stage!.stageId,
        name,
      );
    },
  });

  const releaseMutation = useMutation({
    mutationFn: (drawId: string) =>
      releaseDrawAlignedPlacements(stage!.stageId, drawId),
    onSuccess: (result) => {
      setReleaseConfirmOpen(false);
      notify.success(tDraw('toastReleased', { count: result.releasedCount }));
      scheduleStructureRefresh(queryClient, data.competitionId, ['hubStageShell'], {
        stageId: stage!.stageId,
      });
    },
  });

  useEffect(() => {
    if (!stage) return;
    if (initialManual) {
      setManualOpen(true);
      onInitialEditConsumed?.();
      return;
    }
    if (initialDrawWorkflow && stage.hasDrawRules) {
      setDrawWorkflowOpen(true);
      onInitialEditConsumed?.();
      return;
    }
    if (initialCompose) {
      setComposeFocusSearch(true);
      setComposeOpen(true);
      onInitialEditConsumed?.();
      return;
    }
    if (!initialEdit) return;
    const map: Partial<Record<StructureSectionId, EditTarget>> = {
      qualification: 'qualification',
      progression: 'progression',
      confrontation: 'confrontation',
      matchs: 'matchs',
      classement: 'classement',
      construction: null,
    };
    if (initialEdit === 'tirage') {
      setRulesEditStage(stage);
      setEdit(stage.hasDrawRules ? 'tirage-params' : 'tirage-activate');
      onInitialEditConsumed?.();
      return;
    }
    const target = map[initialEdit];
    if (target) {
      setRulesEditStage(stage);
      setEdit(target);
    } else if (initialEdit === 'construction') {
      if ((stage.actions ?? []).includes('RebuildStructure')) {
        setSkeletonOpen(true);
      }
    }
    onInitialEditConsumed?.();
  }, [
    initialEdit,
    initialCompose,
    initialManual,
    initialDrawWorkflow,
    stage,
    onInitialEditConsumed,
  ]);

  useEffect(() => {
    if (!stage || !initialFocus) {
      return;
    }
    const id = initialFocus;
    const frame = requestAnimationFrame(() => {
      document.getElementById(id)?.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest',
      });
      if (!initialEdit && !initialCompose && !initialManual && !initialDrawWorkflow) {
        onInitialEditConsumed?.();
      }
    });
    return () => cancelAnimationFrame(frame);
  }, [
    stage?.stageId,
    initialFocus,
    initialEdit,
    initialCompose,
    initialManual,
    initialDrawWorkflow,
    onInitialEditConsumed,
  ]);

  const schematicQuery = useQuery({
    queryKey: queryKeys.stages.schematic(stage?.stageId ?? ''),
    queryFn: () => fetchStageSchematic(stage!.stageId),
    enabled: !!stage?.stageId,
  });

  const drawOverviewQuery = useQuery({
    queryKey: queryKeys.stages.detail(stage?.stageId ?? ''),
    queryFn: () => fetchStageOverview(stage!.stageId),
    enabled:
      !!stage?.stageId &&
      (stage.formatKind === 'Groups' ||
        stage.formatKind === 'Cup' ||
        Boolean(stage.hasDrawRules)),
  });

  const cupPairProductionStates = useMemo(() => {
    if (
      !stage ||
      stage.formatKind !== 'Cup' ||
      !stage.materialize?.confrontations.length
    ) {
      return undefined;
    }
    const states: Record<string, 'eligible' | 'incomplete'> = {};
    for (const row of stage.materialize.confrontations) {
      if (row.materialized) {
        continue;
      }
      if (row.eligible) {
        states[row.key] = 'eligible';
      } else if (!row.determined) {
        states[row.key] = 'incomplete';
      }
    }
    return Object.keys(states).length > 0 ? states : undefined;
  }, [stage]);

  const occupiedPlacesCount = useMemo(() => {
    if (!stage) {
      return null;
    }
    const placesNForOccupancy =
      stage.compositionCapacity ?? resolvePlacesN(stage);
    if (!schematicQuery.isSuccess || !schematicQuery.data) {
      return null;
    }
    return countSchematicOccupiedPlaces(
      schematicQuery.data.cases,
      stage.formatKind,
      placesNForOccupancy,
    );
  }, [stage, schematicQuery.isSuccess, schematicQuery.data]);

  const placedEntryIds = useMemo(() => {
    if (!stage || !schematicQuery.isSuccess || !schematicQuery.data) {
      return new Set<string>();
    }
    return placedEntryIdsFromSchematic(
      schematicQuery.data.cases,
      stage.formatKind,
    );
  }, [stage, schematicQuery.isSuccess, schematicQuery.data]);

  const entryPlaceProjections = useMemo(() => {
    if (!stage || !schematicQuery.isSuccess || !schematicQuery.data) {
      return new Map();
    }
    return entryPlaceProjectionFromSchematic(
      schematicQuery.data.cases,
      stage.formatKind,
      t,
    );
  }, [stage, schematicQuery.isSuccess, schematicQuery.data, t]);

  const composedAffectationEntries = useMemo(() => {
    if (!stage) {
      return [];
    }
    return listComposedAffectationEntries(stage, data.participants.entries);
  }, [stage, data.participants.entries]);

  if (!stage) {
    return (
      <section className="structure-fiche" aria-labelledby="fiche-empty">
        <h2 id="fiche-empty" className="structure-fiche__title">
          {t('hub.phaseFiche')}
        </h2>
        <p className="structure-panel__muted">{t('hub.noPhaseSelectedBody')}</p>
      </section>
    );
  }

  const feeds = inboundFeeds(data, stage.stageId, i18n.language, t);
  const feedGroups = groupFeeds(feeds);
  // Places N meter / Affectation reserve: Qual + Prog (Population and Place).
  // Place fills occupy capacity even though they skip the Population set.
  const populationFeedVolume = inboundPopulationConfiguredVolume(
    data,
    stage.stageId,
  );
  const pathOutbounds = outboundFeeds(data, stage, i18n.language, t);
  const outbounds = outboundSortiesFeeds(
    data,
    stage,
    i18n.language,
    t,
    pathOutbounds,
  );
  const outboundGroups = groupFeeds(outbounds);
  const matchBound = isMatchFrameBound(stage.defaultsBinding);
  const standingBound = isStandingFrameBound(stage.defaultsBinding);
  const sections = relevantPhaseSections(stage);
  const actions = stageActions(stage);
  const regulationHref = `/competitions/${data.competitionId}/regulation`;
  const activeDraw = pickActiveDraw(drawOverviewQuery.data?.draws ?? []);
  const drawRulesLocked = areDrawRulesLockedByExecution(
    drawOverviewQuery.data?.draws ?? [],
  );
  const canEditDraw = actions.includes('ReplaceDrawRules');
  /** Execution CTA only when DrawRules engage the mechanism. */
  const showDrawCta = stage.hasDrawRules;
  /**
   * Secondary discovery CTA — Groups/Cup, mechanism not yet engaged.
   * Visibility = format + !DrawRules + ReplaceDrawRules only.
   * No occupation / Qual / Composition / Places N predicate (Activate ≠ coverage).
   */
  const showActivateDrawCta =
    !stage.hasDrawRules &&
    canEditDraw &&
    (stage.formatKind === 'Cup' || stage.formatKind === 'Groups');
  const hasNonCancelledDraw = (drawOverviewQuery.data?.draws ?? []).some(
    (draw) => draw.status !== 'Cancelled',
  );
  const placesN = stage.compositionCapacity ?? resolvePlacesN(stage);
  const poolFilled = stage.compositionEntryCount ?? 0;
  const showPoolHint = showDrawCta && placesN != null && placesN > 0;
  const overviewSlots = drawOverviewQuery.data?.slots ?? [];
  const overviewDraws = drawOverviewQuery.data?.draws ?? [];
  const overviewGroups = drawOverviewQuery.data?.groups ?? [];
  const drawGateReady =
    drawOverviewQuery.isSuccess ||
    stage.formatKind === 'Groups' ||
    !showDrawCta;
  const createGate =
    drawGateReady && showDrawCta
      ? resolveStageDrawCreateGate({
          formatKind: stage.formatKind,
          draws: overviewDraws,
          slots: overviewSlots,
          compositionEntryCount: poolFilled,
          isRootComposition: stage.isRootComposition,
          numberOfPots: stage.numberOfPots,
          groupCount: stage.groupCount,
          placesN,
          minimumTeams: data.regulation.minimumTeams,
          directAssignmentCount: stage.directAssignmentCount,
        })
      : null;
  const createBlockedReason =
    createGate != null && !createGate.ok ? createGate.reason : null;
  const qualifiersConfiguredVolume =
    stage.inboundComposition?.configuredVolume ??
    feedGroups.reduce((sum, group) => sum + group.volume, 0);
  const qualifiersResolvedCount =
    stage.inboundComposition?.resolvedCount ?? 0;
  const drawReadinessChecklist =
    drawGateReady && (showDrawCta || showActivateDrawCta)
      ? resolveDrawReadinessChecklist({
          compositionEntryCount: poolFilled,
          placesN,
          inboundResolvedCount: qualifiersResolvedCount,
          inboundConfiguredVolume: qualifiersConfiguredVolume,
          slots: overviewSlots,
        })
      : null;
  const qualifiersInboundReady = stage.inboundComposition != null;
  const qualifiersResolvedEyebrow = (
    <CompositionQualifiersResolvedMeta
      resolved={qualifiersResolvedCount}
      configured={qualifiersConfiguredVolume}
      ready={qualifiersInboundReady}
    />
  );
  const createBlockPresentation =
    createBlockedReason != null
      ? resolveDrawCreateBlockPresentation(createBlockedReason)
      : null;
  const createBlockedShort =
    createBlockedReason != null
      ? t(`fiche.drawWorkflow.createBlocked.short.${createBlockedReason}`)
      : null;
  const stageHasAppliedDraw = stageHasPublishedAppliedDraw(overviewDraws);
  const schematicCases = schematicQuery.data?.cases;
  const releasablePick = pickReleasableCancelledDraw({
    draws: overviewDraws,
    slots: overviewSlots,
    groups: overviewGroups,
    schematicCases,
    competitionStatus: data.status,
    stageStatus: stage.status,
    stageHasPublishedAppliedDraw: stageHasAppliedDraw,
  });
  const releasableDraw = releasablePick?.draw ?? null;
  const releasableAlignedCount = releasablePick?.aligned ?? 0;
  const showReleaseCta =
    releasableDraw != null && releasableAlignedCount > 0;
  /** Nouveau path blocked with nothing to manage in the dialog → disable primary CTA. */
  const blockPerformDrawCta =
    createBlockedShort != null && !activeDraw && overviewDraws.length === 0;
  /** Inline short caption on the stage card — skip when Release is the unblock action, and
   * skip countMismatch when the filled/capacity ratio already carries the signal. */
  const showCreateBlockedCaption =
    createBlockedShort != null &&
    createBlockPresentation?.kind === 'inline' &&
    !showReleaseCta &&
    !(createBlockedReason === 'countMismatch' && showPoolHint) &&
    createBlockedReason !== 'emptyPool' &&
    createBlockedReason !== 'emptyPoolUpstream';
  const canEditProg = actions.includes('ReplaceProgressionRules');
  const canEditPlacement = actions.includes('ReplacePlacementAwardRules');
  const canEditTie =
    actions.includes('ReplaceDefaultTieFormat') ||
    actions.includes('ReplaceRoundTieFormat');
  const canEditMatch = actions.includes('ReplaceMatchRules');
  const canEditStanding = actions.includes('ReplaceStandingRules');
  const canRebind = actions.includes('BindToCompetition');
  const canRemove = actions.includes('RemoveStage');
  const canRename = actions.includes('RenameStage');
  const canRebuild = actions.includes('RebuildStructure');
  const canCompose = actions.includes('ReplaceAffectationAuthoring');

  const beginRename = () => {
    if (!canRename || renameMutation.isPending) return;
    renameMutation.reset();
    setNameDraft(stage.name);
    setEditingName(true);
  };

  const cancelRename = () => {
    if (renameMutation.isPending) return;
    setEditingName(false);
    setNameDraft(stage.name);
    renameMutation.reset();
  };

  const commitRename = () => {
    const next = nameDraft.trim();
    if (!next || renameMutation.isPending) return;
    if (next === stage.name) {
      setEditingName(false);
      return;
    }
    renameMutation.mutate(next);
  };
  const canEditQualif = actions.includes('ReplaceQualificationRules');
  const canAssignEntryToSlot =
    stage.formatKind === 'Cup' && actions.includes('AssignEntryToSlot');
  /** Same capacity predicate as schematic click: at least one editable Cup place. */
  const editableManualPlaceCount = listCupManualPlaces(
    schematicQuery.data?.cases ?? [],
  ).filter((place) => place.mode === 'editable').length;
  const canManualPlace =
    canAssignEntryToSlot &&
    schematicQuery.isSuccess &&
    editableManualPlaceCount > 0;
  const cupLikePlaces =
    stage.formatKind === 'Cup' ||
    (stage.formatKind == null && (stage.slotCount ?? 0) > 0);
  const tracksOperationalPlaces =
    placesN != null &&
    placesN > 0 &&
    (stage.formatKind === 'Championship' ||
      stage.formatKind === 'Swiss' ||
      stage.formatKind === 'Groups' ||
      cupLikePlaces);
  const showPlacementBlock =
    showDrawCta ||
    showActivateDrawCta ||
    canManualPlace ||
    tracksOperationalPlaces;
  const placementAwards = stage.placementAwards ?? [];
  const hasExits = outboundGroups.length > 0;
  const hasAttribution = placementAwards.length > 0;
  /** Inter-Stage only — no Sorties create without a downstream peer. */
  const canAddExit = data.stages.length >= 2;
  const canCreateExit = canAddExit && (canEditQualif || canEditProg);
  const canCreateAttribution = canEditPlacement;
  const issueCodes = stage.structureIssues ?? [];
  const awaitingDraw = isSlotFeedAwaitingDraw(stage);
  const slotFeedMissingActive =
    issueCodes.includes('SlotFeedMissing') && !awaitingDraw;
  /** Banners only — meter owns shortfall/over-capacity/NonRootUnfed(+empty); RESULT merged. */
  const placesBanners = structureIssueBannersForTile(issueCodes, 'places', {
    silenceSlotFeedMissing: awaitingDraw,
  });
  const placesBannersForStrip = placesBanners.filter(
    (code) => !(slotFeedMissingActive && code === 'SlotFeedMissing'),
  );
  /** Detection for rails visibility / attention chrome (all tile-owned codes). */
  const exitsIssues = structureIssuesForTile(issueCodes, 'exits');
  const capacityOwnedIssues = [
    ...structureIssuesForTile(issueCodes, 'capacity'),
    ...(stageHasPopulationShortfall(data, stage) &&
    !issueCodes.includes('PopulationShortfall')
      ? ['PopulationShortfall']
      : []),
  ];
  const confrontationIssues = structureIssuesForTile(
    issueCodes,
    'confrontation',
  );
  const attributionOwnedIssues = structureIssuesForTile(
    issueCodes,
    'attribution',
  );
  const exitsBanners = structureIssueBannersForTile(issueCodes, 'exits');
  const capacityBanners = structureIssueBannersForTile(
    capacityOwnedIssues,
    'capacity',
    {
      meterExpressesPopulationAbsence: stageHasPopulationShortfall(data, stage),
    },
  );
  const confrontationBanners = structureIssueBannersForTile(
    issueCodes,
    'confrontation',
  );
  const standingBanners = structureIssueBannersForTile(issueCodes, 'standing');
  const attributionBanners = structureIssueBannersForTile(
    issueCodes,
    'attribution',
  );
  const showConfrontation =
    sections.includes('confrontation') || confrontationIssues.length > 0;

  const showSortiesRail = hasExits || canCreateExit || exitsIssues.length > 0;
  /** Attribution = KO/Cup only (gated by ReplacePlacementAwardRules), or RESULT diagnostics. */
  const showAttributionRail =
    canEditPlacement || attributionOwnedIssues.length > 0;
  const showOutRail = showSortiesRail || showAttributionRail;
  const formatLabel = stage.formatKind
    ? structureFormatKindLabel(stage.formatKind)
    : null;

  const railIssueBanner = (codes: readonly string[]) =>
    codes.length === 0 ? undefined : (
      <StructurePhaseRailIssueBanner
        codes={codes}
        data={data}
        stage={stage}
      />
    );

  const openCompose = (focusSearch = false) => {
    setComposeFocusSearch(focusSearch);
    setComposeOpen(true);
  };

  const closeRulesEdit = () => {
    setEdit(null);
    setRulesEditStage(null);
  };

  /** Open Qualif/Prog editor for a source stage without changing the selected stage card. */
  const openExitEdit = (
    kind: ExitKind,
    sourceStage: StructureStageHubSummary = stage,
  ) => {
    setRulesEditStage(sourceStage);
    setEdit(kind);
  };

  const exitEditOptions = (): {
    kind: ExitKind;
    label: string;
    onSelect: () => void;
  }[] => {
    const options: {
      kind: ExitKind;
      label: string;
      onSelect: () => void;
    }[] = [];
    if (canEditQualif) {
      options.push({
        kind: 'qualification',
        label: t('fiche.exitKind.qualification'),
        onSelect: () => openExitEdit('qualification'),
      });
    }
    if (canEditProg) {
      options.push({
        kind: 'progression',
        label: t('fiche.exitKind.progression'),
        onSelect: () => openExitEdit('progression'),
      });
    }
    return options;
  };

  const renderAvalSourceAction = (group: FeedGroup): ReactNode => {
    const present = exitKindsPresent(group);
    const peer = data.stages.find((s) => s.stageId === group.peerId);
    if (!peer) return null;

    const peerActions = peer.actions ?? [];
    const options: {
      kind: ExitKind;
      label: string;
      onSelect: () => void;
    }[] = [];

    if (
      present.has('qualification') &&
      peerActions.includes('ReplaceQualificationRules')
    ) {
      options.push({
        kind: 'qualification',
        label: t('fiche.exitKind.qualification'),
        onSelect: () => openExitEdit('qualification', peer),
      });
    }
    if (
      present.has('progression') &&
      peerActions.includes('ReplaceProgressionRules')
    ) {
      options.push({
        kind: 'progression',
        label: t('fiche.exitKind.progression'),
        onSelect: () => openExitEdit('progression', peer),
      });
    }

    if (options.length === 0) return null;

    return <ExitKindMenu label={t('fiche.editExits')} options={options} />;
  };

  const showProductionOperationalBar = showProductionOperationalSection(stage);

  const heroFacts: {
    icon: ReactNode;
    value: string | number;
    label: string;
    statKey: string;
  }[] = [];
  const kind = stage.formatKind;
  if (kind === 'Groups' && (stage.groupCount ?? 0) > 0) {
    const n = stage.groupCount!;
    heroFacts.push({
      icon: <GroupsFormatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.groups', { count: n }),
      statKey: 'groups',
    });
  } else if (kind === 'Cup' && (stage.roundCount ?? 0) > 0) {
    const n = stage.roundCount!;
    heroFacts.push({
      icon: <RoundsStatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.rounds', { count: n }),
      statKey: 'rounds',
    });
  } else if (kind === 'Championship') {
    const n = stage.matchdayCount ?? 0;
    heroFacts.push({
      icon: <MatchdayStatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.matchdays', { count: n }),
      statKey: 'matchdays',
    });
  } else if (kind === 'Swiss') {
    const { generated, planned } = swissRoundCounts(stage);
    if ((planned != null && planned > 0) || generated > 0) {
      heroFacts.push({
        icon: <RoundsStatIcon size="sm" />,
        value:
          planned != null && planned > 0
            ? `${generated}/${planned}`
            : generated,
        label:
          planned != null && planned > 0
            ? t('fiche.stat.swissRoundsGeneratedCaption')
            : t('fiche.stat.rounds', { count: generated }),
        statKey: 'swiss-rounds',
      });
    }
  }
  if (!showPlacementBlock) {
    const n = resolvePlacesN(stage);
    if (n != null && n > 0) {
      heroFacts.push({
        icon: <LayersIcon size="sm" />,
        value: n,
        label: t('fiche.stat.places', { count: n }),
        statKey: 'places',
      });
    }
  }
  if (stage.materialize && !showProductionOperationalBar) {
    const matchCount = stage.materialize.materializedCount;
    heroFacts.push({
      icon: <MatchesStatIcon size="sm" />,
      value: matchCount,
      label: t('fiche.stat.matches', { count: matchCount }),
      statKey: 'matches',
    });
  }

  const showHeroStats = heroFacts.length > 0;
  const showAsideMatchsSection = shouldShowAsideMatchsTile(stage, {
    showProductionOperationalBar,
    showPlacementBlock,
    showDrawCta,
    showActivateDrawCta,
  });
  const showAffectationSection =
    showPlacementBlock || showDrawCta || showActivateDrawCta;
  const showQualifiersSection = feedGroups.length > 0;
  const teamsHasListContent =
    composedAffectationEntries.length > 0 ||
    (stage.affectationPreviewNames?.length ?? 0) > 0 ||
    (stage.affectationIneligibleCount ?? 0) > 0;
  const showTeamsSection = teamsHasListContent;
  const showCompositionContentEmpty =
    !showQualifiersSection && !teamsHasListContent && canCompose;
  const showCompositionBandEmpty =
    showCompositionContentEmpty && !showAffectationSection;
  const showPopulationColumn =
    showTeamsSection || showCompositionContentEmpty;
  const showCompositionPopulationGauge =
    placesN != null &&
    placesN > 0 &&
    (showQualifiersSection || showTeamsSection || showCompositionContentEmpty);
  const sharedCompositionPopulationGauge =
    showCompositionPopulationGauge &&
    showQualifiersSection &&
    showTeamsSection;
  const populationMeterCurrent = expectedPopulationOccupancy(data, stage);
  const stageDiagnosticContext = structureDiagnosticContextFromStage(data, stage);
  const populationShortfallMessageKind =
    stageHasPopulationShortfall(data, stage)
      ? (() => {
          const tone = structureDiagnosticPresentation(
            'PopulationShortfall',
            {
              formatKind: stageDiagnosticContext.formatKind,
              stageStatus: stageDiagnosticContext.stageStatus,
              isEntryBaselineStage: stageDiagnosticContext.isEntryBaselineStage,
              hasInboundQualProgFeed:
                stageDiagnosticContext.hasInboundQualProgFeed,
              affectationEntryCount: stageDiagnosticContext.affectationEntryCount,
            },
          ).tone;
          return tone === 'danger' || tone === 'attention' ? tone : null;
        })()
      : null;
  const compositionPopulationGaugeFooter = showCompositionPopulationGauge ? (
    <CompositionPopulationGauge
      current={populationMeterCurrent}
      capacity={placesN!}
      populationShortfallMessageKind={populationShortfallMessageKind}
      className={
        sharedCompositionPopulationGauge
          ? 'structure-phase-composition__population-gauge--span-sources'
          : undefined
      }
    />
  ) : null;
  const teamsComposeActionLabel =
    composedAffectationEntries.length > 0 ||
    (stage.affectationPreviewNames?.length ?? 0) > 0 ||
    (stage.affectationIneligibleCount ?? 0) > 0
      ? t('entries.editComposition')
      : t('entries.addAffectation');
  const materializePresentation = materializePresentationForStage(
    stage,
    data.status,
    stage.status,
  );
  const placementOperationalProps =
    showAffectationSection
      ? {
          competitionId: data.competitionId,
          placesN,
          poolFilled,
          occupiedPlacesCount,
          occupancyReady:
            stage.formatKind === 'Championship' ||
            stage.formatKind === 'Swiss' ||
            schematicQuery.isSuccess,
          showDrawCta,
          showActivateDrawCta,
          showResolvedPoolGauge:
            showDrawCta ||
            showActivateDrawCta ||
            stage.formatKind === 'Championship' ||
            stage.formatKind === 'Swiss',
          canManualPlace,
          canEditDraw,
          showReleaseCta,
          hasNonCancelledDraw,
          activeDraw: Boolean(activeDraw),
          blockPerformDrawCta,
          createBlockedShort,
          createBlockedReason,
          showCreateBlockedCaption,
          showPoolHint,
          releasableAlignedCount,
          releasableTotal:
            releasableDraw != null
              ? drawResolutionPlacementCount(releasableDraw)
              : 0,
          releasePending: releaseMutation.isPending,
          deactivateDrawPending: deactivateDrawMutation.isPending,
          onOpenDrawWorkflow: () => setDrawWorkflowOpen(true),
          onActivateDraw: () => setEdit('tirage-activate'),
          onEditDrawParams: () => setEdit('tirage-params'),
          onManualPlace: () => {
            setManualSlotKey(null);
            setManualOpen(true);
          },
          onReleasePlacements: () => setReleaseConfirmOpen(true),
          onDeactivateDraw: () => setDeactivateDrawOpen(true),
          slotFeedMissingActive,
          drawOperationalReady: drawGateReady,
          drawReadinessChecklist,
          placesDiagnostics:
            placesBannersForStrip.length > 0 ? (
              <StructureOperationalIssueBanner
                codes={placesBannersForStrip}
                diagnosticContext={stageDiagnosticContext}
              />
            ) : undefined,
        }
      : null;
  const compositionGridClass = [
    'structure-phase-composition__grid',
    !showQualifiersSection ? 'structure-phase-composition__grid--two-cols' : null,
    showQualifiersSection
      ? 'structure-phase-composition__grid--with-qualifiers'
      : null,
    showAffectationSection
      ? 'structure-phase-composition__grid--with-affectation'
      : null,
    showQualifiersSection && !showTeamsSection
      ? 'structure-phase-composition__grid--without-teams'
      : null,
    sharedCompositionPopulationGauge
      ? 'structure-phase-composition__grid--shared-population-gauge'
      : null,
  ]
    .filter(Boolean)
    .join(' ');

  const compositionEmptyAction = canCompose ? (
    <button
      type="button"
      className="ds-btn ds-btn--primary"
      onClick={() => openCompose(false)}
    >
      <PlusIcon size="sm" aria-hidden="true" />
      {t('entries.addAffectation')}
    </button>
  ) : null;

  const compositionComposeToolbar =
    canCompose ? (
      <Tooltip content={teamsComposeActionLabel}>
        <button
          type="button"
          className={compactIcon}
          aria-label={teamsComposeActionLabel}
          onClick={() => openCompose(false)}
        >
          {teamsHasListContent ? (
            <PencilIcon size="sm" />
          ) : (
            <PlusIcon size="sm" />
          )}
        </button>
      </Tooltip>
    ) : null;
  // Overflow removed — Edit/Delete live in the header toolbar.

  const sortiesEditControl = (() => {
    if (hasExits) {
      const options = exitEditOptions();
      if (options.length === 0) return undefined;
      return <ExitKindMenu label={t('fiche.editExits')} options={options} />;
    }
    if (!canCreateExit) return undefined;
    // Qual XOR Prog — open the single available editor.
    const kind: ExitKind = canEditQualif ? 'qualification' : 'progression';
    return (
      <Tooltip content={t('fiche.addExit')}>
        <button
          type="button"
          className={compactIcon}
          aria-label={t('fiche.addExit')}
          onClick={() => openExitEdit(kind)}
        >
          <PlusIcon size="sm" />
        </button>
      </Tooltip>
    );
  })();

  const attributionEditControl = (() => {
    if (hasAttribution) {
      if (!canEditPlacement) return undefined;
      return (
        <Tooltip content={t('fiche.edit')}>
          <button
            type="button"
            className={compactIcon}
            aria-label={t('fiche.edit')}
            onClick={() => setEdit('placement')}
          >
            <PencilIcon size="sm" />
          </button>
        </Tooltip>
      );
    }
    if (!canCreateAttribution) return undefined;
    return (
      <Tooltip content={t('fiche.addAttribution')}>
        <button
          type="button"
          className={compactIcon}
          aria-label={t('fiche.addAttribution')}
          onClick={() => setEdit('placement')}
        >
          <PlusIcon size="sm" />
        </button>
      </Tooltip>
    );
  })();

  return (
    <section
      className="structure-fiche structure-fiche--flat"
      aria-labelledby="phase-overview-heading"
    >
      <StructurePhaseFicheHeader
        stage={stage}
        competitionId={data.competitionId}
        structureStages={data.stages}
        formatLabel={formatLabel}
        editingName={editingName}
        nameDraft={nameDraft}
        renameMutation={renameMutation}
        canRename={canRename}
        canRebuild={canRebuild}
        canRemove={canRemove}
        compactIcon={compactIcon}
        compactDangerIcon={compactDangerIcon}
        onNameDraftChange={setNameDraft}
        onBeginRename={beginRename}
        onCancelRename={cancelRename}
        onCommitRename={commitRename}
        onOpenSkeleton={() => setSkeletonOpen(true)}
        onOpenRemove={() => setRemoveOpen(true)}
      />

      <div
        className={[
          'structure-phase-tile',
          'structure-phase-tile--composition',
          'structure-phase-composition',
          showCompositionBandEmpty
            ? 'structure-phase-composition--band-empty'
            : null,
        ]
          .filter(Boolean)
          .join(' ')}
      >
        <StructurePhaseCompositionBand
          showCompositionBandEmpty={showCompositionBandEmpty}
          compositionGridClass={compositionGridClass}
          compositionEmptyAction={compositionEmptyAction}
          showQualifiersSection={showQualifiersSection}
          structureHubBackgroundRefresh={structureHubBackgroundRefresh}
          qualifiersResolvedEyebrow={qualifiersResolvedEyebrow}
          compositionComposeToolbar={compositionComposeToolbar}
          feedGroups={feedGroups}
          inboundComposition={stage.inboundComposition}
          entries={data.participants.entries}
          formatKind={stage.formatKind}
          schematicReady={schematicQuery.isSuccess}
          entryPlaceProjections={entryPlaceProjections}
          onViewPhase={onSelectStage}
          renderAvalSourceAction={renderAvalSourceAction}
          compositionPopulationGaugeFooter={compositionPopulationGaugeFooter}
          sharedCompositionPopulationGauge={sharedCompositionPopulationGauge}
          showTeamsSection={showTeamsSection}
          showCompositionContentEmpty={showCompositionContentEmpty}
          showPopulationColumn={showPopulationColumn}
          showAffectationSection={showAffectationSection}
          canCompose={canCompose}
          teamsComposeActionLabel={teamsComposeActionLabel}
          compactIcon={compactIcon}
          openCompose={openCompose}
          teamsHasListContent={teamsHasListContent}
          capacityIssueBanner={railIssueBanner(capacityBanners)}
          stage={stage}
          placedEntryIds={placedEntryIds}
          populationFeedVolume={populationFeedVolume}
          placementOperationalProps={placementOperationalProps}
          showPlacementBlock={showPlacementBlock}
          showDrawCta={showDrawCta}
          showActivateDrawCta={showActivateDrawCta}
          competitionId={data.competitionId}
          competitionStatus={data.status}
          structureStages={data.stages}
          materializePresentation={materializePresentation}
          stageHasAppliedDraw={stageHasAppliedDraw}
        />
      </div>

      <StructurePhaseSchematicAndAside
        showHeroStats={showHeroStats}
        heroFacts={heroFacts}
        schematicQuery={schematicQuery}
        stage={stage}
        hasExits={hasExits}
        cupPairProductionStates={cupPairProductionStates}
        canManualPlace={canManualPlace}
        onManualPlaceActivate={(place) => {
          const key = place.formPosition.slotKey?.trim();
          if (!key) return;
          setManualSlotKey(key);
          setManualOpen(true);
        }}
        schematicIssuesBanner={
          !showPlacementBlock ? railIssueBanner(placesBanners) : undefined
        }
        showAsideMatchsSection={showAsideMatchsSection}
        structureHubBackgroundRefresh={structureHubBackgroundRefresh}
        placementOperationalProps={placementOperationalProps}
        showPlacementBlock={showPlacementBlock}
        showDrawCta={showDrawCta}
        showActivateDrawCta={showActivateDrawCta}
        competitionId={data.competitionId}
        competitionStatus={data.status}
        structureStages={data.stages}
        materializePresentation={materializePresentation}
        stageHasAppliedDraw={stageHasAppliedDraw}
        showOutRail={showOutRail}
        showSortiesRail={showSortiesRail}
        showAttributionRail={showAttributionRail}
        sortiesEditControl={sortiesEditControl}
        exitsBanners={railIssueBanner(exitsBanners)}
        outboundGroups={outboundGroups}
        onSelectStage={onSelectStage}
        attributionEditControl={attributionEditControl}
        attributionBanners={railIssueBanner(attributionBanners)}
        hasAttribution={hasAttribution}
        placementAwards={placementAwards}
      />

      <StructurePhaseRegulationStrip
        stage={stage}
        regulationHref={regulationHref}
        matchBound={matchBound}
        standingBound={standingBound}
        showStanding={standingBound !== null}
        showConfrontation={showConfrontation}
        canEditMatch={canEditMatch}
        canRebind={canRebind}
        canEditStanding={canEditStanding}
        canEditTie={canEditTie}
        onEditMatch={() =>
          setEdit(canEditMatch ? 'matchs' : 'rebind-match')
        }
        onEditStanding={() =>
          setEdit(canEditStanding ? 'classement' : 'rebind-standing')
        }
        onEditConfrontation={() => setEdit('confrontation')}
        standingIssues={
          standingBanners.length > 0
            ? railIssueBanner(standingBanners)
            : undefined
        }
        confrontationIssues={
          confrontationBanners.length > 0
            ? railIssueBanner(confrontationBanners)
            : undefined
        }
      />

      {canRemove && (
        <RemovePhaseDialog
          data={data}
          stage={stage}
          open={removeOpen}
          onClose={() => setRemoveOpen(false)}
        />
      )}
      <EditSkeletonDialog
        data={data}
        stage={stage}
        open={skeletonOpen}
        onClose={() => setSkeletonOpen(false)}
      />
      <QualificationRulesDialog
        data={data}
        stage={rulesEditStage ?? stage}
        open={edit === 'qualification'}
        onClose={closeRulesEdit}
        openedFromDestinationStageId={
          rulesEditStage != null && rulesEditStage.stageId !== stage.stageId
            ? stage.stageId
            : null
        }
      />
      <ProgressionRulesDialog
        data={data}
        stage={rulesEditStage ?? stage}
        open={edit === 'progression'}
        onClose={closeRulesEdit}
        openedFromDestinationStageId={
          rulesEditStage != null && rulesEditStage.stageId !== stage.stageId
            ? stage.stageId
            : null
        }
      />
      <StructurePlacementAwardDialog
        data={data}
        stage={stage}
        open={edit === 'placement'}
        onClose={() => setEdit(null)}
      />
      <DrawRulesDialog
        competitionId={data.competitionId}
        stage={stage}
        structureStages={data.stages}
        open={edit === 'tirage-activate' || edit === 'tirage-params'}
        onClose={() => setEdit(null)}
        intent={edit === 'tirage-activate' ? 'activate' : 'params'}
        rulesLocked={edit === 'tirage-params' && drawRulesLocked}
      />
      <ConfirmDialog
        open={deactivateDrawOpen}
        title={t('regulation.deactivateDrawConfirmTitle')}
        message={t('regulation.deactivateDrawConfirmBody')}
        confirmLabel={t('fiche.deactivateDraw')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        danger
        confirmPending={deactivateDrawMutation.isPending}
        footerStatus={
          deactivateDrawMutation.isError ? (
            <MutationError
              error={deactivateDrawMutation.error}
              structureStages={data.stages}
            />
          ) : null
        }
        onConfirm={() => deactivateDrawMutation.mutate()}
        onCancel={() => setDeactivateDrawOpen(false)}
      />
      <ConfirmDialog
        open={releaseConfirmOpen}
        title={tDraw('confirmReleaseTitle')}
        message={tDraw('confirmRelease')}
        confirmLabel={tDraw('releasePlacements')}
        confirmIcon={<UnlockIcon size="sm" />}
        cancelLabel={tCommon('close')}
        closeLabel={tCommon('close')}
        confirmDisabled={releaseMutation.isPending}
        confirmPending={releaseMutation.isPending}
        confirmPendingLabel={tDraw('releasingPlacements')}
        footerStatus={
          releaseMutation.isError ? (
            <MutationError
              error={releaseMutation.error}
              structureStages={data.stages}
            />
          ) : null
        }
        onCancel={() => {
          if (releaseMutation.isPending) {
            return;
          }
          setReleaseConfirmOpen(false);
        }}
        onConfirm={() => {
          if (!releasableDraw || releaseMutation.isPending) {
            return;
          }
          releaseMutation.mutate(releasableDraw.id);
        }}
      />
      <StructureDrawDialog
        competitionId={data.competitionId}
        competitionStatus={data.status}
        minimumTeams={data.regulation.minimumTeams}
        stage={stage}
        structureStages={data.stages}
        open={drawWorkflowOpen}
        onClose={() => setDrawWorkflowOpen(false)}
      />
      <StructureCompositionDialog
        open={composeOpen}
        onClose={() => {
          setComposeOpen(false);
          setComposeFocusSearch(false);
        }}
        competitionId={data.competitionId}
        stage={stage}
        stages={data.stages}
        entries={data.participants.entries}
        reservedFromFeeds={populationFeedVolume}
        focusSearch={composeFocusSearch}
      />
      <StructureManualPlacementDialog
        open={manualOpen}
        onClose={() => {
          setManualOpen(false);
          setManualSlotKey(null);
        }}
        competitionId={data.competitionId}
        stage={stage}
        structureStages={data.stages}
        entries={data.participants.entries}
        cases={schematicQuery.data?.cases ?? []}
        initialSlotKey={manualSlotKey}
      />
      <TieFormatDialog
        competitionId={data.competitionId}
        stage={stage}
        structureStages={data.stages}
        open={edit === 'confrontation'}
        onClose={() => setEdit(null)}
      />
      <MatchRulesDialog
        data={data}
        stage={stage}
        open={edit === 'matchs' || edit === 'rebind-match'}
        onClose={() => setEdit(null)}
      />
      <StandingRulesDialog
        data={data}
        stage={stage}
        open={edit === 'classement' || edit === 'rebind-standing'}
        onClose={() => setEdit(null)}
      />
    </section>
  );
}
