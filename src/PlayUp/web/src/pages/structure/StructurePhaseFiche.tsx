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
import { TextInput } from '../../design-system/components/TextInput';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  ArrowDownIcon,
  ArrowRightIcon,
  AttributionIcon,
  ChampionshipFormatIcon,
  CupFormatIcon,
  GroupsFormatIcon,
  LayersIcon,
  MatchdayStatIcon,
  MatchesStatIcon,
  PlusIcon,
  PencilIcon,
  CheckIcon,
  RoundsStatIcon,
  StructureIcon,
  SwissFormatIcon,
  TrashIcon,
  UnlockIcon,
} from '../../design-system/icons/contentIcons';
import {
  CloseIcon,
} from '../../design-system/icons/shellIcons';
import { structureFormatKindLabel } from '../../i18n/enumLabels';
import { queryKeys } from '../../queryKeys';
import { notify } from '../../design-system/toastStore';
import { MutationError, StageStatusBadge } from '../../ui';
import { scheduleStructureRefresh } from './structureInvalidation';
import {
  patchDrawRulesDeactivatedOptimistic,
  patchStageNameOptimistic,
} from './structureQueryCache';
import type {
  SchematicCase,
  StructureFormatKind,
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
import { StructureDiagnosticIssueIcon } from './StructureDiagnosticIssueIcon';
import { StructurePhaseOperationalBar } from './StructurePhaseOperationalBar';
import { shouldShowMatchsSection } from './structureMatchsSectionUi';
import { StructureOperationalIssueBanner } from './StructureOperationalIssueBanner';
import { countSchematicOccupiedPlaces } from './structurePlaceOccupancy';
import { showProductionOperationalSection } from './structureOperationalCopy';
import {
  areDrawRulesLockedByExecution,
  drawResolutionPlacementCount,
  pickReleasableCancelledDraw,
  stageHasPublishedAppliedDraw,
  pickActiveDraw,
  resolveDrawCreateBlockPresentation,
  resolveStageDrawCreateGate,
} from './drawUi';
import {
  isSlotFeedAwaitingDraw,
  structureDiagnosticTitleKey,
  structureDiagnosticTone,
  structureIssueBannersForTile,
  structureIssuesForTile,
} from './structureDiagnosticPresentation';
import {
  stageHasPopulationShortfall,
} from './structureReadinessSummary';
import { outboundSortiesFeeds } from './structureSortiesIntentFeed';
import { inboundPopulationConfiguredVolume } from './structurePopulationVolume';
import {
  isMatchFrameBound,
  isStandingFrameBound,
  relevantPhaseSections,
  type StructureSectionId,
} from './structureHubSections';
import { resolvePlacesN } from './structurePlaces';
import { swissRoundCounts } from './swissRoundCounts';
import type { StructureFocusId } from './structureNavigation';
import { PhaseSchematic } from './phaseSchematic';
import { StructureSchematicViewport } from './StructureSchematicViewport';
import { listCupManualPlaces } from './manualPlacementUi';
import '../regulation/regulation-shared.css';

import {
  exitKindsPresent,
  groupFeeds,
  inboundFeeds,
  outboundFeeds,
  placementRuleParts,
  type ExitKind,
  type FeedGroup,
} from './structurePhaseFeeds';
import {
  ExitKindMenu,
  FluxGroupList,
  FluxRail,
  PlacementAwardRow,
  RootEntriesRail,
} from './StructurePhaseRails';
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

function FormatGlyph({
  kind,
  size = 'md',
}: {
  kind?: StructureFormatKind | null;
  size?: 'sm' | 'md' | 'lg';
}) {
  switch (kind) {
    case 'Cup':
      return <CupFormatIcon size={size} aria-hidden="true" />;
    case 'Championship':
      return <ChampionshipFormatIcon size={size} aria-hidden="true" />;
    case 'Groups':
      return <GroupsFormatIcon size={size} aria-hidden="true" />;
    case 'Swiss':
      return <SwissFormatIcon size={size} aria-hidden="true" />;
    default:
      return <StructureIcon size={size} aria-hidden="true" />;
  }
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

  const feeds = inboundFeeds(data, stage.stageId, t);
  const feedGroups = groupFeeds(feeds);
  // Places N meter / Affectation reserve: Qual + Prog (Population and Place).
  // Place fills occupy capacity even though they skip the Population set.
  const populationFeedVolume = inboundPopulationConfiguredVolume(
    data,
    stage.stageId,
  );
  const pathOutbounds = outboundFeeds(data, stage, t);
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
  const teamsLabel = (count: number) => t('fiche.teamsCount', { count });
  const formatLabel = stage.formatKind
    ? structureFormatKindLabel(stage.formatKind)
    : null;

  const issueBanner = (codes: string[]) =>
    codes.length === 0 ? undefined : (
      <ul className="structure-phase-rail__issues" role="list">
        {codes.map((code) => {
          const tone = structureDiagnosticTone(code);
          return (
            <li
              key={code}
              className="structure-phase-rail__issue"
              data-tone={tone}
              role="status"
            >
              <span
                className="structure-phase-rail__issue-icon"
                aria-hidden="true"
              >
                <StructureDiagnosticIssueIcon tone={tone} />
              </span>
              <span>
                {t(structureDiagnosticTitleKey(code), {
                  defaultValue: code,
                })}
              </span>
            </li>
          );
        })}
      </ul>
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
  const showHeroStructureActions =
    showPlacementBlock ||
    showProductionOperationalBar ||
    (stage != null && shouldShowMatchsSection(stage));
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
      <header className="structure-fiche__header structure-fiche__header--toolbar">
        <div className="structure-fiche__title-row structure-fiche__title-row--primary">
            {formatLabel ? (
              <Tooltip content={formatLabel}>
                <span className="structure-fiche__format-icon">
                  <FormatGlyph kind={stage.formatKind} size="lg" />
                </span>
              </Tooltip>
            ) : (
              <span className="structure-fiche__format-icon" aria-hidden="true">
                <FormatGlyph kind={stage.formatKind} size="lg" />
              </span>
            )}
            {editingName ? (
              <form
                className="structure-fiche__title-edit"
                onSubmit={(event) => {
                  event.preventDefault();
                  commitRename();
                }}
              >
                <div className="structure-fiche__title-edit-field">
                  <label className="ds-visually-hidden" htmlFor="phase-rename-input">
                    {t('fiche.renamePhase')}
                  </label>
                  <TextInput
                    id="phase-rename-input"
                    value={nameDraft}
                    onChange={(event) => setNameDraft(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === 'Escape') {
                        event.preventDefault();
                        cancelRename();
                      }
                    }}
                    disabled={renameMutation.isPending}
                    maxLength={100}
                    required
                    autoFocus
                    invalid={renameMutation.isError}
                  />
                </div>
                <div className="ds-icon-toolbar">
                  <Tooltip content={t('fiche.confirmRename')}>
                    <button
                      type="submit"
                      className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact ds-icon-button--affirm"
                      disabled={
                        renameMutation.isPending ||
                        nameDraft.trim().length === 0
                      }
                      aria-label={t('fiche.confirmRename')}
                    >
                      {renameMutation.isPending ? (
                        <span className="ds-spinner" aria-hidden="true" />
                      ) : (
                        <CheckIcon size="sm" />
                      )}
                    </button>
                  </Tooltip>
                  <Tooltip content={tCommon('cancel')}>
                    <button
                      type="button"
                      className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact ds-icon-button--dismiss"
                      disabled={renameMutation.isPending}
                      aria-label={tCommon('cancel')}
                      onClick={cancelRename}
                    >
                      <CloseIcon size="sm" />
                    </button>
                  </Tooltip>
                </div>
                {renameMutation.isError ? (
                  <MutationError error={renameMutation.error} />
                ) : null}
              </form>
            ) : (
              <>
                <h2
                  id="phase-overview-heading"
                  className="structure-fiche__title"
                >
                  {stage.name}
                </h2>
                {canRename ? (
                  <Tooltip content={t('fiche.editPhase')}>
                    <button
                      type="button"
                      className={compactIcon}
                      aria-label={t('fiche.editPhase')}
                      onClick={beginRename}
                    >
                      <PencilIcon size="sm" />
                    </button>
                  </Tooltip>
                ) : null}
              </>
            )}
          {!editingName ? (
            <div className="structure-fiche__header-end">
              <StageStatusBadge status={stage.status} />
              <div className="structure-fiche__header-tools ds-icon-toolbar">
                {canRebuild ? (
                  <Tooltip content={t('fiche.editForme')}>
                    <button
                      type="button"
                      className={compactIcon}
                      aria-label={t('fiche.editForme')}
                      onClick={() => setSkeletonOpen(true)}
                    >
                      <StructureIcon size="sm" />
                    </button>
                  </Tooltip>
                ) : null}
                {canRemove ? (
                  <Tooltip content={t('graph.removePhase')}>
                    <button
                      type="button"
                      className={compactDangerIcon}
                      aria-label={t('graph.removePhase')}
                      onClick={() => setRemoveOpen(true)}
                    >
                      <TrashIcon size="sm" />
                    </button>
                  </Tooltip>
                ) : null}
              </div>
            </div>
          ) : null}
        </div>
      </header>

      {showHeroStructureActions && stage ? (
        <StructurePhaseOperationalBar
          structureHubBackgroundRefresh={structureHubBackgroundRefresh}
          showPlaces={showPlacementBlock}
          showDraw={showDrawCta || showActivateDrawCta}
          drawApplied={stageHasAppliedDraw}
          placement={
            showPlacementBlock || showDrawCta || showActivateDrawCta
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
                  placesDiagnostics:
                    placesBannersForStrip.length > 0 ? (
                      <StructureOperationalIssueBanner codes={placesBannersForStrip} />
                    ) : undefined,
                }
              : null
          }
          competitionId={data.competitionId}
          competitionStatus={data.status}
          stage={stage}
        />
      ) : null}

      <div className="structure-phase-hero structure-phase-hero--lite">
        <div
          className={[
            'structure-phase-hero__triptych',
            showOutRail ? null : 'structure-phase-hero__triptych--no-out',
          ]
            .filter(Boolean)
            .join(' ')}
        >
          <FluxRail
            id="rail-entries"
            side="in"
            title={t('fiche.tiles.population')}
            icon={<ArrowDownIcon size="md" />}
            attentionBanner={issueBanner(capacityBanners)}
          >
            <RootEntriesRail
              stage={stage}
              entries={data.participants.entries}
              sourcesConfiguredVolume={populationFeedVolume}
              canCompose={canCompose}
              onEditCompose={() => openCompose(false)}
              sources={
                feedGroups.length > 0 ? (
                  <FluxGroupList
                    groups={feedGroups}
                    onOpenPeer={onSelectStage}
                    teamsLabel={teamsLabel}
                    renderGroupAction={renderAvalSourceAction}
                  />
                ) : undefined
              }
            />
          </FluxRail>

          <div className="structure-phase-hero__center">
            {showHeroStats ? (
              <ul
                className="structure-phase-hero__stats"
                aria-label={t('fiche.statsAria')}
              >
                {heroFacts.map((fact) => (
                  <li key={fact.statKey} className="structure-phase-hero__stat">
                    <span
                      className="structure-phase-hero__stat-icon"
                      aria-hidden="true"
                    >
                      {fact.icon}
                    </span>
                    <span className="structure-phase-hero__stat-value">
                      {fact.value}
                    </span>
                    <span className="structure-phase-hero__stat-label">
                      {fact.label}
                    </span>
                  </li>
                ))}
              </ul>
            ) : null}
            <div
              id="tile-places"
              className="structure-schematic-viewport"
            >
              <StructureSchematicViewport
                schematic={schematicQuery.data}
                isError={schematicQuery.isError}
                isFetching={schematicQuery.isFetching}
                loadingLabel={t('fiche.schematicLoading')}
                unavailableLabel={t('fiche.occupantsUnavailable')}
              >
                {(schematic) => (
                  <PhaseSchematic
                    schematic={schematic}
                    terminal={!hasExits}
                    cupRoundCount={stage.roundCount}
                    cupPairProductionStates={cupPairProductionStates}
                    onPlaceActivate={
                      canManualPlace
                        ? (place: SchematicCase) => {
                            const key = place.formPosition.slotKey?.trim();
                            if (!key) return;
                            setManualSlotKey(key);
                            setManualOpen(true);
                          }
                        : undefined
                    }
                  />
                )}
              </StructureSchematicViewport>
              {!showPlacementBlock && placesBanners.length > 0 ? (
                <div className="structure-schematic-viewport__issues">
                  {issueBanner(placesBanners)}
                </div>
              ) : null}
            </div>
          </div>

          {showOutRail ? (
            <div className="structure-phase-hero__out">
              {showSortiesRail ? (
                <FluxRail
                  id="rail-exits"
                  side="out"
                  title={t('fiche.tiles.exits')}
                  icon={<ArrowRightIcon size="md" />}
                  editControl={sortiesEditControl}
                  empty={!hasExits && exitsBanners.length === 0}
                  attentionBanner={issueBanner(exitsBanners)}
                >
                  {hasExits ? (
                    <FluxGroupList
                      groups={outboundGroups}
                      onOpenPeer={onSelectStage}
                      teamsLabel={teamsLabel}
                    />
                  ) : null}
                </FluxRail>
              ) : null}
              {showAttributionRail ? (
                <FluxRail
                  id="rail-attribution"
                  side="out"
                  title={t('fiche.tiles.attribution')}
                  icon={<AttributionIcon size="md" />}
                  editControl={attributionEditControl}
                  empty={!hasAttribution && attributionBanners.length === 0}
                  attentionBanner={issueBanner(attributionBanners)}
                >
                  {hasAttribution ? (
                    <ul className="structure-flux-group__rules">
                      {[...placementAwards]
                        .sort((a, b) => a.rank - b.rank)
                        .map((award) => {
                          const parts = placementRuleParts(award, t);
                          return (
                            <li
                              key={`${award.rank}-${award.outcome}-${award.sourcePairKey}`}
                              className="structure-flux-group__rule"
                            >
                              <PlacementAwardRow
                                rank={parts.rank}
                                medal={parts.medal}
                                badge={parts.badge}
                                badgeTone={parts.badgeTone}
                                context={parts.context}
                              />
                            </li>
                          );
                        })}
                    </ul>
                  ) : null}
                </FluxRail>
              ) : null}
            </div>
          ) : null}
        </div>
      </div>

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
          standingBanners.length > 0 ? issueBanner(standingBanners) : undefined
        }
        confrontationIssues={
          confrontationBanners.length > 0
            ? issueBanner(confrontationBanners)
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
            <MutationError error={deactivateDrawMutation.error} />
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
            <MutationError error={releaseMutation.error} />
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
        entries={data.participants.entries}
        cases={schematicQuery.data?.cases ?? []}
        initialSlotKey={manualSlotKey}
      />
      <TieFormatDialog
        competitionId={data.competitionId}
        stage={stage}
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
