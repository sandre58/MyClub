import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageOverview,
  fetchStageSchematic,
  releaseDrawAlignedPlacements,
  renameStage,
  replaceStageDrawRules,
} from '../../api';
import { Chip } from '../../design-system/components/Chip';
import { ConfirmDialog } from '../../design-system/components/ConfirmDialog';
import { Tooltip } from '../../design-system/components/Tooltip';
import { TextLink } from '../../design-system/components/TextLink';
import {
  ArrowDownIcon,
  ArrowRightIcon,
  AttributionIcon,
  ChampionshipFormatIcon,
  ConfrontationIcon,
  CupFormatIcon,
  GroupsFormatIcon,
  LayersIcon,
  MatchRulesIcon,
  MatchdayStatIcon,
  PersonIcon,
  PlusIcon,
  PencilIcon,
  CheckIcon,
  RoundsStatIcon,
  StandingRulesIcon,
  StructureIcon,
  SwissFormatIcon,
  TrashIcon,
  UnlockIcon,
} from '../../design-system/icons/contentIcons';
import {
  CloseIcon,
  SettingsNavIcon,
} from '../../design-system/icons/shellIcons';
import { structureFormatKindLabel } from '../../i18n/enumLabels';
import { queryKeys } from '../../queryKeys';
import { notify } from '../../design-system/toastStore';
import {
  LoadingState,
  MutationError,
  PendingLabel,
  StageStatusBadge,
  StatusBadge,
} from '../../ui';
import { invalidateAfterStructureMutation } from './structureInvalidation';
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
import {
  areDrawRulesLockedByExecution,
  countAlignedSlotPlacements,
  pickActiveDraw,
  resolveDrawCreateBlockPresentation,
  resolveStageDrawCreateGate,
} from './drawUi';
import { canReleaseDrawAlignedPlacements } from '../stage/lifecycleGates';
import { DrawCtaActionBody, StructureDrawCta } from './StructureDrawCta';
import { resolveDrawCtaPoolTone } from './StructureDrawCta';
import { outboundSortiesFeeds } from './structureSortiesIntentFeed';
import { inboundPopulationConfiguredVolume } from './structurePopulationVolume';
import {
  isMatchFrameBound,
  isStandingFrameBound,
  relevantPhaseSections,
  type StructureSectionId,
} from './structureHubSections';
import { resolvePlacesN } from './structurePlaces';
import { PhaseSchematic } from './phaseSchematic';
import { listCupManualPlaces } from './manualPlacementUi';
import {
  MatchRulesPanel,
  StandingRulesPanel,
} from '../regulation/regulationRulePanels';
import '../regulation/regulation.css';

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
  ConfrontationPanel,
  DomainTile,
  ExitKindMenu,
  FluxGroupList,
  FluxRail,
  PlacementAwardRow,
  RootEntriesRail,
} from './StructurePhaseRails';

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

function removePhaseDisabledHint(
  t: (key: string) => string,
  data: StructureView,
  stage: StructureStageHubSummary,
): string {
  if (data.stages.length <= 1) {
    return t('graph.removePhaseDisabledLast');
  }
  if (stage.matchCount > 0) {
    return t('graph.removePhaseDisabledMatches');
  }
  return t('graph.removePhaseDisabled');
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
  onSelectStage,
  initialEdit,
  initialCompose,
  onInitialEditConsumed,
}: {
  data: StructureView;
  stage: StructureStageHubSummary | null;
  onSelectStage?: (stageId: string) => void;
  initialEdit?: StructureSectionId | null;
  initialCompose?: boolean;
  onInitialEditConsumed?: () => void;
}) {
  const { t, i18n } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tDraw } = useTranslation('draw');
  const queryClient = useQueryClient();
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
  }, [stage?.stageId]);

  const deactivateDrawMutation = useMutation({
    mutationFn: () => replaceStageDrawRules(stage!.stageId, { clear: true }),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      setDeactivateDrawOpen(false);
    },
  });

  const renameMutation = useMutation({
    mutationFn: (name: string) => renameStage(stage!.stageId, name),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      setEditingName(false);
    },
  });

  const releaseMutation = useMutation({
    mutationFn: (drawId: string) =>
      releaseDrawAlignedPlacements(stage!.stageId, drawId),
    onSuccess: async (result) => {
      setReleaseConfirmOpen(false);
      await invalidateAfterStructureMutation(queryClient, data.competitionId, {
        stageId: stage!.stageId,
      });
      notify.success(tDraw('toastReleased', { count: result.releasedCount }));
    },
  });

  useEffect(() => {
    if (!stage) return;
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
  }, [initialEdit, initialCompose, stage, onInitialEditConsumed]);

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
  const drawCtaPoolTone =
    showPoolHint && placesN != null
      ? resolveDrawCtaPoolTone(poolFilled, placesN)
      : 'neutral';
  const overviewSlots = drawOverviewQuery.data?.slots ?? [];
  const overviewDraws = drawOverviewQuery.data?.draws ?? [];
  const drawGateReady =
    drawOverviewQuery.isSuccess ||
    stage.formatKind === 'Groups' ||
    !showDrawCta;
  const createGate = drawGateReady
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
  const releasableDraw = overviewDraws.find((draw) =>
    canReleaseDrawAlignedPlacements({
      competitionStatus: data.status,
      stageStatus: stage.status,
      drawStatus: draw.status,
      drawKind: draw.kind,
      alignedPlacementCount: countAlignedSlotPlacements(draw, overviewSlots),
    }),
  );
  const releasableAlignedCount =
    releasableDraw != null
      ? countAlignedSlotPlacements(releasableDraw, overviewSlots)
      : 0;
  const showReleaseCta =
    releasableDraw != null &&
    createBlockPresentation?.kind === 'inline' &&
    createBlockPresentation.action;
  /** Nouveau path blocked with nothing to manage in the dialog → disable primary CTA. */
  const blockPerformDrawCta =
    createBlockedShort != null && !activeDraw && overviewDraws.length === 0;
  /** Inline short caption on the stage card — skip when Release is the unblock action, and
   * skip countMismatch when the filled/capacity ratio already carries the signal. */
  const showCreateBlockedCaption =
    createBlockedShort != null &&
    createBlockPresentation?.kind === 'inline' &&
    !showReleaseCta &&
    !(createBlockedReason === 'countMismatch' && showPoolHint);
  const showConfrontation = sections.includes('confrontation');
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
  const removeDisabledHint = removePhaseDisabledHint(t, data, stage);

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
  const showPlacementBlock =
    showDrawCta || showActivateDrawCta || canManualPlace;
  const placementAwards = stage.placementAwards ?? [];
  const hasExits = outboundGroups.length > 0;
  const hasAttribution = placementAwards.length > 0;
  /** Inter-Stage only — no Sorties create without a downstream peer. */
  const canAddExit = data.stages.length >= 2;
  const canCreateExit = canAddExit && (canEditQualif || canEditProg);
  const canCreateAttribution = canEditPlacement;
  const showSortiesRail = hasExits || canCreateExit;
  /** Attribution = KO/Cup only (gated by ReplacePlacementAwardRules). */
  const showAttributionRail = canEditPlacement;
  const showOutRail = showSortiesRail || showAttributionRail;
  const teamsLabel = (count: number) => t('fiche.teamsCount', { count });
  const formatLabel = stage.formatKind
    ? structureFormatKindLabel(stage.formatKind)
    : null;

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

  const heroFacts: {
    icon: ReactNode;
    value: string | number;
    label: string;
  }[] = [];
  const kind = stage.formatKind;
  if (kind === 'Groups' && (stage.groupCount ?? 0) > 0) {
    const n = stage.groupCount!;
    heroFacts.push({
      icon: <GroupsFormatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.groups', { count: n }),
    });
  } else if (kind === 'Cup' && (stage.roundCount ?? 0) > 0) {
    const n = stage.roundCount!;
    heroFacts.push({
      icon: <RoundsStatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.rounds', { count: n }),
    });
  } else if (kind === 'Championship' && (stage.matchdayCount ?? 0) > 0) {
    const n = stage.matchdayCount!;
    heroFacts.push({
      icon: <MatchdayStatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.matchdays', { count: n }),
    });
  } else if (kind === 'Swiss' && (stage.swissRoundCount ?? 0) > 0) {
    const n = stage.swissRoundCount!;
    heroFacts.push({
      icon: <RoundsStatIcon size="sm" />,
      value: n,
      label: t('fiche.stat.rounds', { count: n }),
    });
  }
  {
    const n = resolvePlacesN(stage);
    if (n != null && n > 0) {
      heroFacts.push({
        icon: <LayersIcon size="sm" />,
        value: n,
        label: t('fiche.stat.places', { count: n }),
      });
    }
  }

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
      <header className="structure-fiche__header">
        <div className="structure-fiche__titles">
          <div className="structure-fiche__title-row">
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
                <label className="structure-fiche__title-edit-field">
                  <span className="ds-visually-hidden">
                    {t('fiche.renamePhase')}
                  </span>
                  <input
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
                    aria-invalid={renameMutation.isError || undefined}
                  />
                </label>
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
          </div>
        </div>
        <div className="structure-fiche__actions">
          <ul className="structure-fiche__pills">
            <li className="structure-fiche__pill structure-fiche__pill--status">
              <StageStatusBadge status={stage.status} />
            </li>
          </ul>
          <div className="ds-icon-toolbar">
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
            ) : (
              <Tooltip content={removeDisabledHint}>
                <button
                  type="button"
                  className={compactDangerIcon}
                  disabled
                  aria-label={t('graph.removePhase')}
                >
                  <TrashIcon size="sm" />
                </button>
              </Tooltip>
            )}
          </div>
        </div>
      </header>

      <div className="structure-phase-hero">
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
            {(heroFacts.length > 0 || canRebuild) && (
              <div className="structure-phase-hero__meta">
                {heroFacts.length > 0 ? (
                  <ul
                    className="structure-phase-hero__stats"
                    aria-label={t('fiche.statsAria')}
                  >
                    {heroFacts.map((fact) => (
                      <li
                        key={fact.label}
                        className="structure-phase-hero__stat"
                      >
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
                {canRebuild ? (
                  <button
                    type="button"
                    className="ds-btn ds-btn--ghost structure-phase-hero__forme-cta"
                    onClick={() => setSkeletonOpen(true)}
                  >
                    <StructureIcon size="sm" />
                    {t('fiche.editForme')}
                  </button>
                ) : null}
              </div>
            )}
            <div className="structure-schematic-viewport">
              <div className="structure-schematic-viewport__scale">
                {schematicQuery.data ? (
                  <PhaseSchematic
                    schematic={schematicQuery.data}
                    terminal={!hasExits}
                    cupRoundCount={stage.roundCount}
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
                ) : schematicQuery.isError ? (
                  <p className="structure-panel__muted">
                    {t('fiche.occupantsUnavailable')}
                  </p>
                ) : (
                  <div className="structure-schematic-loading">
                    <LoadingState
                      size="region"
                      label={t('fiche.schematicLoading')}
                    />
                  </div>
                )}
              </div>
            </div>
            {showPlacementBlock ? (
              <div className="structure-phase-hero__draw">
                <div
                  className={[
                    'structure-draw-block',
                    (showDrawCta || showActivateDrawCta) && canManualPlace
                      ? 'structure-draw-block--paired'
                      : null,
                  ]
                    .filter(Boolean)
                    .join(' ')}
                >
                  {showDrawCta || showActivateDrawCta ? (
                    <div className="structure-draw-block__col">
                      {showDrawCta ? (
                        blockPerformDrawCta ? (
                          <Tooltip content={createBlockedShort}>
                            <span className="structure-draw-cta-wrap">
                              <StructureDrawCta
                                tone="emphasis"
                                title={t('fiche.performDraw')}
                                body={
                                  showPoolHint && placesN != null ? (
                                    <DrawCtaActionBody
                                      filled={poolFilled}
                                      capacity={placesN}
                                      teamsCaption={t(
                                        'fiche.drawCtaTeamsCaption',
                                      )}
                                      poolTone={drawCtaPoolTone}
                                    />
                                  ) : showCreateBlockedCaption ? (
                                    <span className="structure-draw-cta__caption">
                                      {createBlockedShort}
                                    </span>
                                  ) : null
                                }
                                onClick={() => setDrawWorkflowOpen(true)}
                                disabled
                              />
                            </span>
                          </Tooltip>
                        ) : (
                          <StructureDrawCta
                            tone="emphasis"
                            title={
                              activeDraw
                                ? t('fiche.openDrawWorkflow')
                                : t('fiche.performDraw')
                            }
                            body={
                              <>
                                {showPoolHint && placesN != null ? (
                                  <DrawCtaActionBody
                                    filled={poolFilled}
                                    capacity={placesN}
                                    teamsCaption={t(
                                      'fiche.drawCtaTeamsCaption',
                                    )}
                                    poolTone={drawCtaPoolTone}
                                  />
                                ) : null}
                                {showCreateBlockedCaption ? (
                                  <span
                                    className="structure-draw-cta__caption"
                                    role="status"
                                  >
                                    {createBlockedShort}
                                  </span>
                                ) : null}
                              </>
                            }
                            onClick={() => setDrawWorkflowOpen(true)}
                          />
                        )
                      ) : (
                        <StructureDrawCta
                          tone="ghost"
                          title={t('fiche.activateDraw')}
                          body={t('fiche.activateDrawBody')}
                          onClick={() => setEdit('tirage-activate')}
                        />
                      )}
                      {showDrawCta && (canEditDraw || showReleaseCta) ? (
                        <div className="structure-draw-actions">
                          {canEditDraw ? (
                            <button
                              type="button"
                              className="ds-btn ds-btn--ghost ds-btn--sm"
                              onClick={() => setEdit('tirage-params')}
                            >
                              <SettingsNavIcon size="sm" />
                              {t('fiche.drawParamsAction')}
                            </button>
                          ) : null}
                          {showReleaseCta || canEditDraw ? (
                            <div className="structure-draw-actions__risk">
                              {showReleaseCta && releasableDraw ? (
                                <Tooltip
                                  content={tDraw('releasePlacementsHint')}
                                >
                                  <button
                                    type="button"
                                    className="ds-btn ds-btn--ghost ds-btn--sm"
                                    disabled={releaseMutation.isPending}
                                    onClick={() => setReleaseConfirmOpen(true)}
                                  >
                                    {releaseMutation.isPending ? (
                                      <PendingLabel>
                                        {tDraw('releasingPlacements')}
                                      </PendingLabel>
                                    ) : (
                                      <>
                                        <UnlockIcon size="sm" />
                                        {tDraw('releasePlacementsCount', {
                                          aligned: releasableAlignedCount,
                                          total:
                                            releasableDraw.slotPlacements
                                              .length,
                                        })}
                                      </>
                                    )}
                                  </button>
                                </Tooltip>
                              ) : null}
                              {canEditDraw ? (
                                hasNonCancelledDraw ? (
                                  <Tooltip
                                    content={t(
                                      'regulation.deactivateDrawBlockedHint',
                                    )}
                                  >
                                    <button
                                      type="button"
                                      className="ds-btn ds-btn--ghost ds-btn--destructive ds-btn--sm ds-icon-button"
                                      disabled
                                      aria-label={t('fiche.deactivateDraw')}
                                    >
                                      <TrashIcon size="sm" />
                                    </button>
                                  </Tooltip>
                                ) : (
                                  <Tooltip content={t('fiche.deactivateDraw')}>
                                    <button
                                      type="button"
                                      className="ds-btn ds-btn--ghost ds-btn--destructive ds-btn--sm ds-icon-button"
                                      disabled={
                                        deactivateDrawMutation.isPending
                                      }
                                      aria-label={t('fiche.deactivateDraw')}
                                      onClick={() =>
                                        setDeactivateDrawOpen(true)
                                      }
                                    >
                                      <TrashIcon size="sm" />
                                    </button>
                                  </Tooltip>
                                )
                              ) : null}
                            </div>
                          ) : null}
                        </div>
                      ) : null}
                    </div>
                  ) : null}
                  {canManualPlace ? (
                    <div className="structure-draw-block__col">
                      <StructureDrawCta
                        tone="ghost"
                        title={t('fiche.manualPlacementCta')}
                        body={t('fiche.manualPlacementCtaBody')}
                        leading={<PersonIcon size="sm" />}
                        onClick={() => {
                          setManualSlotKey(null);
                          setManualOpen(true);
                        }}
                      />
                    </div>
                  ) : null}
                </div>
              </div>
            ) : null}
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
                  empty={!hasExits}
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
                  empty={!hasAttribution}
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

      <div className="structure-domain-tiles">
        <DomainTile
          id="tile-match"
          title={t('fiche.tiles.match')}
          icon={<MatchRulesIcon size="md" />}
          editLabel={t('fiche.edit')}
          badge={
            matchBound ? (
              <Chip tone="neutral">{t('fiche.binding.general')}</Chip>
            ) : (
              <StatusBadge tone="warn" density="compact">
                {t('fiche.binding.personalized')}
              </StatusBadge>
            )
          }
          onEdit={
            canEditMatch || canRebind
              ? () => setEdit(canEditMatch ? 'matchs' : 'rebind-match')
              : undefined
          }
          compact
          footer={
            <TextLink to={regulationHref}>
              {t('hub.overview.openRegulation')}
            </TextLink>
          }
        >
          <div className="structure-domain-tile--reg">
            <MatchRulesPanel
              numberOfPeriods={stage.numberOfPeriods}
              durationPerPeriod={stage.durationPerPeriod}
              halfTimeDuration={stage.halfTimeDuration}
              hasExtraTime={stage.hasExtraTime}
              extraTimeNumberOfPeriods={stage.extraTimeNumberOfPeriods}
              extraTimeDurationPerPeriod={stage.extraTimeDurationPerPeriod}
              hasPenaltyShootout={stage.hasPenaltyShootout}
              penaltyInitialKicksPerTeam={stage.penaltyInitialKicksPerTeam}
            />
          </div>
        </DomainTile>

        {standingBound !== null ? (
          <DomainTile
            id="tile-standing"
            title={t('fiche.tiles.standing')}
            icon={<StandingRulesIcon size="md" />}
            editLabel={t('fiche.editStanding')}
            badge={
              standingBound ? (
                <Chip tone="neutral">{t('fiche.binding.general')}</Chip>
              ) : (
                <StatusBadge tone="warn" density="compact">
                  {t('fiche.binding.personalized')}
                </StatusBadge>
              )
            }
            onEdit={
              canEditStanding || canRebind
                ? () =>
                    setEdit(canEditStanding ? 'classement' : 'rebind-standing')
                : undefined
            }
            compact
            footer={
              <TextLink to={regulationHref}>
                {t('hub.overview.openRegulation')}
              </TextLink>
            }
          >
            <div className="structure-domain-tile--reg">
              <StandingRulesPanel
                winPoints={stage.winPoints ?? 0}
                drawPoints={stage.drawPoints ?? 0}
                lossPoints={stage.lossPoints ?? 0}
                rankingCriteria={stage.rankingCriteria}
                forfeitWinnerGoals={stage.forfeitWinnerGoals}
                forfeitLoserGoals={stage.forfeitLoserGoals}
              />
            </div>
          </DomainTile>
        ) : null}

        {showConfrontation ? (
          <DomainTile
            id="tile-confrontation"
            title={t('fiche.tiles.confrontation')}
            icon={<ConfrontationIcon size="md" />}
            editLabel={t('fiche.edit')}
            onEdit={canEditTie ? () => setEdit('confrontation') : undefined}
            compact
          >
            <ConfrontationPanel stage={stage} />
          </DomainTile>
        ) : null}
      </div>

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
