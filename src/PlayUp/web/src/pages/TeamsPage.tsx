import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query';
import {
  useEffect,
  useId,
  useState,
  type SubmitEvent,
  type ReactNode,
} from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import {
  addCompetitionEntry,
  deleteCompetitionEntries,
  deleteCompetitionEntry,
  fetchStructureView,
  renameCompetitionEntry,
  updateEntryPresentation,
  withdrawCompetitionEntries,
  withdrawCompetitionEntry,
} from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Meter, type MeterTone } from '../design-system/components/Meter';
import { PageHead } from '../design-system/components/PageHead';
import { SelectionBar } from '../design-system/components/SelectionBar';
import { Tooltip } from '../design-system/components/Tooltip';
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import {
  Field,
  type FieldMessageTone,
} from '../design-system/components/Field';
import { TextInput } from '../design-system/components/TextInput';
import { ColorPicker } from '../design-system/components/ColorPicker';
import { TeamCrest } from '../design-system/TeamCrest';
import { notify } from '../design-system/toastStore';
import { useDismissLayer } from '../design-system/useDismissLayer';
import { deriveShortName, SHORT_NAME_MAX_LENGTH } from './deriveShortName';
import { LogoMediaField } from './LogoMediaField';
import { CloseIcon } from '../design-system/icons/shellIcons';
import {
  EmptySelectionIcon,
  LayersIcon,
  PersonIcon,
  PencilIcon,
  PlusIcon,
  TrashIcon,
  WithdrawIcon,
} from '../design-system/icons/contentIcons';
import { queryKeys } from '../queryKeys';
import {
  ErrorState,
  LoadingState,
  MutationError,
  EmptyState,
  PendingLabel,
  StatusBadge,
} from '../ui';
import type {
  EntryStatus,
  StructureEntry,
  StructureView,
} from '../types';
import { TeamRosterDrawer } from './TeamRosterDrawer';
import { isTeamsNarrowViewport } from '../layout/viewportBreakpoints';
import './teams.css';

async function invalidateAfterTeamsMutation(
  queryClient: QueryClient,
  competitionId: string,
) {
  await Promise.all([
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.structure(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.detail(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.workspace(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.overview(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.attention(competitionId),
    }),
  ]);
}

/**
 * Équipes — grille de tuiles + tiroir d’effectif.
 * Read: GET …/structure. Mutations: Add/Rename/Presentation/Delete/Withdraw.
 */
export function TeamsPage() {
  const { competitionId = '', entryId } = useParams();

  const query = useQuery({
    queryKey: queryKeys.competitions.structure(competitionId),
    queryFn: () => fetchStructureView(competitionId),
    enabled: competitionId.length > 0,
  });

  return (
    <main id="main" className="page page--teams">
      {query.isPending && !query.data && <LoadingState />}
      {query.isError && !query.data && <ErrorState error={query.error} />}
      {query.data && <TeamsView data={query.data} routeEntryId={entryId} />}
    </main>
  );
}

function TeamsView({
  data,
  routeEntryId,
}: {
  data: StructureView;
  routeEntryId?: string;
}) {
  const { t } = useTranslation('teams');
  const { t: tCommon } = useTranslation('common');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const can = (action: string) => data.actions.includes(action);
  const canAddAction = can('AddEntry');
  const canDelete = can('DeleteEntry');
  const canWithdraw = can('WithdrawEntry');
  const atCap =
    data.participants.occupyingCount >= data.regulation.maximumTeams;
  const canAdd = canAddAction && !atCap;
  const showPlateauReading = true;
  const missingMinimum = Math.max(
    0,
    data.regulation.minimumTeams - data.participants.activeCount,
  );
  const emptyCount = canAdd ? missingMinimum : 0;
  const entries = data.participants.entries;
  const teamsHref = `/competitions/${data.competitionId}/teams`;
  const [selectedIds, setSelectedIds] = useState<string[]>(() =>
    routeEntryId ? [routeEntryId] : [],
  );
  const [addOpen, setAddOpen] = useState(false);
  const [identityEntryId, setIdentityEntryId] = useState<string | null>(null);
  const [pendingRemove, setPendingRemove] = useState<{
    verb: 'delete' | 'withdraw';
    targets: string[];
    title: string;
    message: string;
    confirmLabel: string;
  } | null>(null);

  const selectedCount = selectedIds.length;
  const multi = selectedCount >= 2;
  const drawerEntryId = selectedCount === 1 ? selectedIds[0] : undefined;
  const identityEntry =
    identityEntryId == null
      ? undefined
      : entries.find((entry) => entry.entryId === identityEntryId);

  function goToSelection(
    nextIds: string[],
    options?: { openRoster?: boolean },
  ) {
    setSelectedIds(nextIds);
    if (nextIds.length === 0) {
      navigate(teamsHref, { replace: true });
      return;
    }
    if (nextIds.length >= 2) {
      navigate(teamsHref, { replace: true });
      return;
    }
    const openRoster = options?.openRoster === true;
    if (openRoster || !isTeamsNarrowViewport()) {
      navigate(`${teamsHref}/${nextIds[0]}`, { replace: true });
      return;
    }
    navigate(teamsHref, { replace: true });
  }

  useEffect(() => {
    if (routeEntryId) {
      setSelectedIds([routeEntryId]);
    }
  }, [routeEntryId]);

  useDismissLayer(selectedIds.length > 0, () => {
    goToSelection([]);
  });

  const deleteMutation = useMutation({
    mutationFn: (ids: string[]) =>
      ids.length === 1
        ? deleteCompetitionEntry(data.competitionId, ids[0])
        : deleteCompetitionEntries(data.competitionId, { entryIds: ids }),
    onSuccess: async () => {
      setPendingRemove(null);
      goToSelection([]);
      await invalidateAfterTeamsMutation(queryClient, data.competitionId);
    },
  });

  const withdrawMutation = useMutation({
    mutationFn: (ids: string[]) =>
      ids.length === 1
        ? withdrawCompetitionEntry(data.competitionId, ids[0])
        : withdrawCompetitionEntries(data.competitionId, { entryIds: ids }),
    onSuccess: async () => {
      setPendingRemove(null);
      goToSelection([]);
      await invalidateAfterTeamsMutation(queryClient, data.competitionId);
    },
  });

  function removeKind(): 'delete' | 'withdraw' {
    return data.status === 'Draft' || data.status === 'Ready'
      ? 'delete'
      : 'withdraw';
  }

  function canRemove() {
    return removeKind() === 'delete' ? canDelete : canWithdraw;
  }

  function confirmAndRemove(ids: string[]) {
    if (ids.length === 0 || !canRemove()) {
      return;
    }
    const verb = removeKind();
    const targets =
      verb === 'withdraw'
        ? ids.filter(
            (id) =>
              entries.find((entry) => entry.entryId === id)?.status ===
              'Active',
          )
        : ids;
    if (targets.length === 0) {
      return;
    }
    const first = entries.find((entry) => entry.entryId === targets[0]);
    const name = first?.displayName ?? targets[0];
    if (verb === 'delete') {
      setPendingRemove({
        verb,
        targets,
        title:
          targets.length === 1
            ? t('confirmDeleteTitle', { name })
            : t('confirmDeleteLotTitle', { count: targets.length }),
        message:
          targets.length === 1
            ? t('confirmDelete', { name })
            : t('confirmDeleteLot', { count: targets.length }),
        confirmLabel: t('deleteEntry'),
      });
      return;
    }
    setPendingRemove({
      verb,
      targets,
      title:
        targets.length === 1
          ? t('confirmWithdrawTitle', { name })
          : t('confirmWithdrawLotTitle', { count: targets.length }),
      message:
        targets.length === 1
          ? t('confirmWithdraw', { name })
          : t('confirmWithdrawLot', { count: targets.length }),
      confirmLabel: t('withdrawEntry'),
    });
  }

  function onTileBody(entryId: string) {
    if (selectedCount < 2) {
      goToSelection([entryId], { openRoster: true });
      return;
    }
    if (selectedIds.includes(entryId)) {
      goToSelection(selectedIds.filter((id) => id !== entryId));
      return;
    }
    goToSelection([...selectedIds, entryId]);
  }

  function onToggleCheck(entryId: string) {
    if (selectedIds.includes(entryId)) {
      goToSelection(selectedIds.filter((id) => id !== entryId));
      return;
    }
    goToSelection([...selectedIds, entryId]);
  }

  const mutationError = deleteMutation.error ?? withdrawMutation.error;
  const removePending = deleteMutation.isPending || withdrawMutation.isPending;
  const removing = removeKind();
  const removeEnabled = canRemove();
  const selectedActiveCount = selectedIds.filter(
    (id) => entries.find((entry) => entry.entryId === id)?.status === 'Active',
  ).length;
  const barRemoveEnabled =
    removeEnabled && (removing === 'delete' || selectedActiveCount > 0);
  const removeLabel =
    removing === 'delete' ? t('deleteEntry') : t('withdrawEntry');
  const removeDisabledHint =
    removing === 'delete' ? t('deleteDisabledHint') : t('withdrawDisabledHint');
  const barRemoveHint =
    removing === 'withdraw' && selectedActiveCount === 0
      ? t('alreadyWithdrawnHint')
      : removeEnabled
        ? removeLabel
        : removeDisabledHint;

  const compactIcon =
    'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

  const rosterOpen =
    selectedCount === 1 &&
    routeEntryId != null &&
    routeEntryId === selectedIds[0];
  const teamsView =
    selectedCount >= 2 ? 'multi' : rosterOpen ? 'detail' : 'list';

  return (
    <div
      className="teams"
      data-teams-view={teamsView}
      data-teams-multi={multi ? 'true' : 'false'}
    >
      <div className="teams__layout">
        <div className="teams__main">
          <PageHead
            title={t('title')}
            titleMeta={
              showPlateauReading ? data.participants.occupyingCount : undefined
            }
            tools={
              <div className="teams__ops-row">
                {showPlateauReading && (
                  <TeamsPlateauReading
                    activeCount={data.participants.activeCount}
                    occupyingCount={data.participants.occupyingCount}
                    minimumTeams={data.regulation.minimumTeams}
                    maximumTeams={data.regulation.maximumTeams}
                  />
                )}
                <div className="teams__ops-tail">
                  {selectedCount >= 1 && (
                    <SelectionBar
                      countLabel={t('selectionCount', { count: selectedCount })}
                    >
                      {barRemoveEnabled ? (
                        <Tooltip content={removeLabel}>
                          <button
                            type="button"
                            className={compactIcon}
                            disabled={removePending}
                            aria-label={removeLabel}
                            onClick={() => confirmAndRemove(selectedIds)}
                          >
                            {removing === 'delete' ? (
                              <TrashIcon size="sm" />
                            ) : (
                              <WithdrawIcon size="sm" />
                            )}
                          </button>
                        </Tooltip>
                      ) : (
                        <Tooltip content={barRemoveHint}>
                          <button
                            type="button"
                            className={compactIcon}
                            disabled
                            aria-label={removeLabel}
                          >
                            {removing === 'delete' ? (
                              <TrashIcon size="sm" />
                            ) : (
                              <WithdrawIcon size="sm" />
                            )}
                          </button>
                        </Tooltip>
                      )}
                      <Tooltip content={t('clearSelection')}>
                        <button
                          type="button"
                          className={compactIcon}
                          aria-label={t('clearSelection')}
                          onClick={() => goToSelection([])}
                        >
                          <CloseIcon size="sm" />
                        </button>
                      </Tooltip>
                    </SelectionBar>
                  )}
                  {canAdd ? (
                    <button
                      type="button"
                      className="ds-btn ds-btn--primary teams__add"
                      aria-label={t('addAction')}
                      onClick={() => setAddOpen(true)}
                    >
                      <PlusIcon size="sm" />
                      <span className="teams__add-label">{t('addAction')}</span>
                    </button>
                  ) : (
                    <Tooltip content={t('addDisabledHint')}>
                      <button
                        type="button"
                        className="ds-btn ds-btn--primary teams__add"
                        disabled
                        aria-label={t('addAction')}
                      >
                        <PlusIcon size="sm" />
                        <span className="teams__add-label">
                          {t('addAction')}
                        </span>
                      </button>
                    </Tooltip>
                  )}
                </div>
              </div>
            }
          />

          {mutationError && <MutationError error={mutationError} />}

          <ul className="teams__grid">
            {entries.map((entry) => {
              const selected = selectedIds.includes(entry.entryId);
              const withdrawn = entry.status === 'Withdrawn';
              const playerCount = (entry.declaredMembers ?? []).filter(
                (member) => member.role === 'Player',
              ).length;
              const tileCanRemove =
                removeEnabled && !(removing === 'withdraw' && withdrawn);
              const tileRemoveHint =
                withdrawn && removing === 'withdraw'
                  ? t('alreadyWithdrawnHint')
                  : tileCanRemove
                    ? removeLabel
                    : removeDisabledHint;
              const tileRemoveLabel =
                removing === 'delete'
                  ? t('deleteEntryNamed', { name: entry.displayName })
                  : t('withdrawEntryNamed', { name: entry.displayName });
              const statusBadge = tileStatusBadge(entry.status, t);
              return (
                <li key={entry.entryId}>
                  <article
                    className="teams-tile ds-selectable-tile"
                    data-selected={selected ? 'true' : 'false'}
                    data-withdrawn={withdrawn ? 'true' : 'false'}
                  >
                    <button
                      type="button"
                      className="teams-tile__hit"
                      aria-label={entry.displayName}
                      onClick={() => onTileBody(entry.entryId)}
                    />
                    <div className="teams-tile__chrome">
                      {selectedCount >= 1 && (
                        <label className="teams-tile__check">
                          <input
                            type="checkbox"
                            checked={selected}
                            aria-label={t('selectEntry', {
                              name: entry.displayName,
                            })}
                            onChange={() => onToggleCheck(entry.entryId)}
                          />
                        </label>
                      )}
                      {!multi && (
                        <div className="ds-icon-toolbar">
                          <Tooltip content={t('editIdentityTooltip')}>
                            <button
                              type="button"
                              className={compactIcon}
                              aria-label={t('editIdentity', {
                                name: entry.displayName,
                              })}
                              onClick={() => setIdentityEntryId(entry.entryId)}
                            >
                              <PencilIcon size="sm" />
                            </button>
                          </Tooltip>
                          {tileCanRemove ? (
                            <Tooltip content={removeLabel}>
                              <button
                                type="button"
                                className={compactIcon}
                                disabled={removePending}
                                aria-label={tileRemoveLabel}
                                onClick={() =>
                                  confirmAndRemove([entry.entryId])
                                }
                              >
                                {removing === 'delete' ? (
                                  <TrashIcon size="sm" />
                                ) : (
                                  <WithdrawIcon size="sm" />
                                )}
                              </button>
                            </Tooltip>
                          ) : (
                            <Tooltip content={tileRemoveHint}>
                              <button
                                type="button"
                                className={compactIcon}
                                disabled
                                aria-label={tileRemoveLabel}
                              >
                                {removing === 'delete' ? (
                                  <TrashIcon size="sm" />
                                ) : (
                                  <WithdrawIcon size="sm" />
                                )}
                              </button>
                            </Tooltip>
                          )}
                        </div>
                      )}
                    </div>
                    <div className="teams-tile__body">
                      <span className="teams-tile__figure" aria-hidden="true">
                        <TeamCrest
                          name={entry.displayName}
                          logoMediaId={entry.logoMediaId}
                          primaryColor={entry.primaryColor}
                          size="lg"
                        />
                        <p className="teams-tile__name">{entry.displayName}</p>
                      </span>
                      <span className="teams-tile__meta">
                        <span
                          className="teams-tile__meta-slot teams-tile__players"
                          aria-label={t('playerCount', { count: playerCount })}
                        >
                          <PersonIcon size="sm" />
                          <span className="ds-num">{playerCount}</span>
                        </span>
                        <span
                          className="teams-tile__swatches"
                          aria-hidden="true"
                        >
                          <span
                            className="teams-tile__swatch"
                            style={{
                              background: entry.primaryColor || 'transparent',
                            }}
                          />
                          <span
                            className="teams-tile__swatch"
                            style={{
                              background: entry.secondaryColor || 'transparent',
                            }}
                          />
                        </span>
                        <span className="teams-tile__meta-slot">
                          {statusBadge}
                        </span>
                      </span>
                    </div>
                  </article>
                </li>
              );
            })}
            {Array.from({ length: emptyCount }, (_, index) => (
              <li key={`empty-${index}`}>
                <article className="teams-tile teams-tile--empty ds-selectable-tile">
                  <button
                    type="button"
                    className="teams-tile__hit"
                    aria-label={t('emptyTileAria')}
                    onClick={() => setAddOpen(true)}
                  />
                  <div className="teams-tile__chrome" aria-hidden="true" />
                  <div className="teams-tile__body">
                    <PlusIcon size="lg" />
                    <span>{t('emptyTile')}</span>
                  </div>
                </article>
              </li>
            ))}
          </ul>
        </div>

        <aside
          className="ds-panel teams-drawer"
          aria-label={t('roster.panelLabel')}
        >
          {selectedCount === 0 && (
            <EmptyState
              variant="idle"
              icon={<EmptySelectionIcon size="lg" />}
              title={t('roster.idleTitle')}
            >
              {t('roster.idleBody')}
            </EmptyState>
          )}
          {selectedCount >= 2 && (
            <EmptyState
              variant="idle"
              icon={<LayersIcon size="lg" />}
              title={t('roster.multiTitle', { count: selectedCount })}
            >
              {t('roster.multiBody')}
            </EmptyState>
          )}
          {drawerEntryId && (
            <TeamRosterDrawer
              data={data}
              entryId={drawerEntryId}
              onBack={() => navigate(teamsHref, { replace: true })}
              onEditIdentity={() => setIdentityEntryId(drawerEntryId)}
            />
          )}
        </aside>
      </div>

      <AddEntryDialog
        data={data}
        open={addOpen}
        onClose={() => setAddOpen(false)}
      />
      <IdentityDialog
        competitionId={data.competitionId}
        entry={identityEntry ?? null}
        entries={data.participants.entries}
        open={identityEntry != null}
        onClose={() => setIdentityEntryId(null)}
      />
      <ConfirmDialog
        open={pendingRemove != null}
        title={pendingRemove?.title ?? ''}
        message={pendingRemove?.message ?? ''}
        confirmLabel={pendingRemove?.confirmLabel ?? t('deleteEntry')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        danger
        confirmDisabled={removePending}
        confirmPending={removePending}
        confirmPendingLabel={
          pendingRemove?.verb === 'withdraw' ? t('withdrawing') : t('deleting')
        }
        onCancel={() => {
          if (removePending) {
            return;
          }
          setPendingRemove(null);
        }}
        onConfirm={() => {
          if (!pendingRemove || removePending) {
            return;
          }
          const { verb, targets } = pendingRemove;
          if (verb === 'delete') {
            deleteMutation.mutate(targets);
            return;
          }
          withdrawMutation.mutate(targets);
        }}
      />
    </div>
  );
}

function AddEntryDialog({
  data,
  open,
  onClose,
}: {
  data: StructureView;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('teams');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const formId = useId();
  const [displayName, setDisplayName] = useState('');
  const [shortName, setShortName] = useState('');
  const [nameTouched, setNameTouched] = useState(false);
  const [shortNameTouched, setShortNameTouched] = useState(false);
  const [logoMediaId, setLogoMediaId] = useState<string | null>(null);
  const [primaryColor, setPrimaryColor] = useState('');
  const [secondaryColor, setSecondaryColor] = useState('');
  const [submitted, setSubmitted] = useState(false);

  const isDirty =
    displayName.trim().length > 0 ||
    shortName.trim().length > 0 ||
    logoMediaId != null ||
    primaryColor.trim().length > 0 ||
    secondaryColor.trim().length > 0;

  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(isDirty, onClose);

  useEffect(() => {
    if (!open) {
      return;
    }
    setDisplayName('');
    setShortName('');
    setNameTouched(false);
    setShortNameTouched(false);
    setLogoMediaId(null);
    setPrimaryColor('');
    setSecondaryColor('');
    setSubmitted(false);
    resetDiscard();
  }, [open, resetDiscard]);

  const duplicateName = hasDuplicateEntryName(
    data.participants.entries,
    displayName,
  );

  const nameError =
    (submitted || nameTouched) && displayName.trim().length === 0
      ? t('nameRequired')
      : undefined;
  const shortNameError =
    (submitted || shortNameTouched) && shortName.trim().length === 0
      ? t('shortNameRequired')
      : undefined;

  const addMutation = useMutation({
    mutationFn: () =>
      addCompetitionEntry(data.competitionId, {
        displayName: displayName.trim(),
        shortName: shortName.trim(),
        logoMediaId,
        primaryColor: primaryColor.trim() || null,
        secondaryColor: secondaryColor.trim() || null,
      }),
    onSuccess: async () => {
      await invalidateAfterTeamsMutation(queryClient, data.competitionId);
      notify.success(t('entryAddedToast'));
      onClose();
    },
  });

  const canSubmit =
    displayName.trim().length > 0 &&
    shortName.trim().length > 0 &&
    shortName.trim().length <= SHORT_NAME_MAX_LENGTH &&
    !addMutation.isPending;

  function requestClose() {
    requestDiscardClose(addMutation.isPending);
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('addDialogTitle')}
        closeLabel={tCommon('close')}
        closeDisabled={addMutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        size="sm"
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              disabled={addMutation.isPending || discardOpen}
              onClick={requestClose}
            >
              {tCommon('cancel')}
            </button>
            <button
              type="submit"
              form={formId}
              className="ds-btn ds-btn--primary"
              disabled={!canSubmit}
            >
              {addMutation.isPending ? (
                <PendingLabel>{t('adding')}</PendingLabel>
              ) : (
                t('add')
              )}
            </button>
          </>
        }
      >
        <form
          id={formId}
          className="ds-form"
          data-density="comfortable"
          onSubmit={(event: SubmitEvent) => {
            event.preventDefault();
            setSubmitted(true);
            if (!canSubmit) {
              return;
            }
            addMutation.mutate();
          }}
        >
          {addMutation.isError && <MutationError error={addMutation.error} />}
          <IdentityFields
            name={displayName}
            shortName={shortName}
            logoMediaId={logoMediaId}
            primaryColor={primaryColor}
            secondaryColor={secondaryColor}
            disabled={addMutation.isPending}
            nameMessage={
              nameError ??
              (duplicateName ? t('duplicateNameWarning') : undefined)
            }
            nameMessageTone={nameError ? 'error' : 'warning'}
            shortNameMessage={shortNameError}
            shortNameMessageTone="error"
            onName={(value) => {
              setNameTouched(true);
              setDisplayName(value);
              if (!shortNameTouched) {
                setShortName(deriveShortName(value));
              }
            }}
            onShortName={(value) => {
              setShortNameTouched(true);
              setShortName(value);
            }}
            onLogo={setLogoMediaId}
            onPrimary={setPrimaryColor}
            onSecondary={setSecondaryColor}
          />
        </form>
      </Dialog>
      <ConfirmDialog
        open={discardOpen}
        title={t('discardIdentityTitle')}
        message={t('discardIdentityChanges')}
        confirmLabel={t('discardIdentityConfirm')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        onCancel={cancelDiscard}
        onConfirm={confirmDiscard}
      />
    </>
  );
}

function IdentityDialog({
  competitionId,
  entry,
  entries,
  open,
  onClose,
}: {
  competitionId: string;
  entry: StructureEntry | null;
  entries: StructureEntry[];
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('teams');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const formId = useId();
  const [name, setName] = useState(entry?.displayName ?? '');
  const [shortName, setShortName] = useState(entry?.shortName ?? '');
  const [logoMediaId, setLogoMediaId] = useState<string | null>(
    entry?.logoMediaId ?? null,
  );
  const [primaryColor, setPrimaryColor] = useState(entry?.primaryColor ?? '');
  const [secondaryColor, setSecondaryColor] = useState(
    entry?.secondaryColor ?? '',
  );
  const [nameTouched, setNameTouched] = useState(false);
  const [shortNameTouched, setShortNameTouched] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  const baselineName = entry?.displayName ?? '';
  const baselineShort = entry?.shortName ?? '';
  const baselineLogo = entry?.logoMediaId ?? null;
  const baselinePrimary = entry?.primaryColor ?? '';
  const baselineSecondary = entry?.secondaryColor ?? '';

  const isDirty =
    name.trim() !== baselineName.trim() ||
    shortName.trim() !== baselineShort.trim() ||
    logoMediaId !== baselineLogo ||
    primaryColor.trim() !== baselinePrimary.trim() ||
    secondaryColor.trim() !== baselineSecondary.trim();

  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(isDirty, onClose);

  useEffect(() => {
    if (!entry) {
      return;
    }
    setName(entry.displayName);
    setShortName(entry.shortName ?? '');
    setLogoMediaId(entry.logoMediaId ?? null);
    setPrimaryColor(entry.primaryColor ?? '');
    setSecondaryColor(entry.secondaryColor ?? '');
    setNameTouched(false);
    setShortNameTouched(false);
    setSubmitted(false);
    resetDiscard();
  }, [entry, resetDiscard]);

  const duplicateName = hasDuplicateEntryName(entries, name, entry?.entryId);

  const nameError =
    (submitted || nameTouched) && name.trim().length === 0
      ? t('nameRequired')
      : undefined;
  const shortNameError =
    (submitted || shortNameTouched) && shortName.trim().length === 0
      ? t('shortNameRequired')
      : undefined;

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!entry) {
        return;
      }
      if (name.trim() !== entry.displayName) {
        await renameCompetitionEntry(competitionId, entry.entryId, {
          displayName: name.trim(),
        });
      }
      await updateEntryPresentation(competitionId, entry.entryId, {
        shortName: shortName.trim(),
        logoMediaId,
        primaryColor: primaryColor.trim() || null,
        secondaryColor: secondaryColor.trim() || null,
      });
    },
    onSuccess: async () => {
      await invalidateAfterTeamsMutation(queryClient, competitionId);
      notify.success(t('identitySavedToast'));
      onClose();
    },
  });

  const canSubmit =
    name.trim().length > 0 &&
    shortName.trim().length > 0 &&
    shortName.trim().length <= SHORT_NAME_MAX_LENGTH &&
    !saveMutation.isPending;

  function requestClose() {
    requestDiscardClose(saveMutation.isPending);
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('identityDialogTitle')}
        closeLabel={tCommon('close')}
        closeDisabled={saveMutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        size="sm"
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              disabled={saveMutation.isPending || discardOpen}
              onClick={requestClose}
            >
              {tCommon('cancel')}
            </button>
            <button
              type="submit"
              form={formId}
              className="ds-btn ds-btn--primary"
              disabled={!canSubmit}
            >
              {saveMutation.isPending ? (
                <PendingLabel>{t('saving')}</PendingLabel>
              ) : (
                t('saveIdentity')
              )}
            </button>
          </>
        }
      >
        <form
          id={formId}
          className="ds-form"
          data-density="comfortable"
          onSubmit={(event: SubmitEvent) => {
            event.preventDefault();
            setSubmitted(true);
            if (!canSubmit) {
              return;
            }
            saveMutation.mutate();
          }}
        >
          {saveMutation.isError && <MutationError error={saveMutation.error} />}
          <IdentityFields
            name={name}
            shortName={shortName}
            logoMediaId={logoMediaId}
            primaryColor={primaryColor}
            secondaryColor={secondaryColor}
            disabled={saveMutation.isPending}
            nameMessage={
              nameError ??
              (duplicateName ? t('duplicateNameWarning') : undefined)
            }
            nameMessageTone={nameError ? 'error' : 'warning'}
            shortNameMessage={shortNameError}
            shortNameMessageTone="error"
            onName={(value) => {
              setNameTouched(true);
              setName(value);
            }}
            onShortName={(value) => {
              setShortNameTouched(true);
              setShortName(value);
            }}
            onLogo={setLogoMediaId}
            onPrimary={setPrimaryColor}
            onSecondary={setSecondaryColor}
          />
        </form>
      </Dialog>
      <ConfirmDialog
        open={discardOpen}
        title={t('discardIdentityTitle')}
        message={t('discardIdentityChanges')}
        confirmLabel={t('discardIdentityConfirm')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        onCancel={cancelDiscard}
        onConfirm={confirmDiscard}
      />
    </>
  );
}

function hasDuplicateEntryName(
  entries: StructureEntry[],
  name: string,
  excludeEntryId?: string,
): boolean {
  const normalized = name.trim().toLocaleLowerCase('fr');
  if (normalized.length === 0) {
    return false;
  }
  return entries.some(
    (entry) =>
      entry.entryId !== excludeEntryId &&
      entry.displayName.trim().toLocaleLowerCase('fr') === normalized,
  );
}

function IdentityFields({
  name,
  shortName,
  logoMediaId,
  primaryColor,
  secondaryColor,
  disabled,
  nameMessage,
  nameMessageTone = 'hint',
  shortNameMessage,
  shortNameMessageTone = 'hint',
  onName,
  onShortName,
  onLogo,
  onPrimary,
  onSecondary,
}: {
  name: string;
  shortName: string;
  logoMediaId: string | null;
  primaryColor: string;
  secondaryColor: string;
  disabled: boolean;
  nameMessage?: string;
  nameMessageTone?: FieldMessageTone;
  shortNameMessage?: string;
  shortNameMessageTone?: FieldMessageTone;
  onName: (value: string) => void;
  onShortName: (value: string) => void;
  onLogo: (value: string | null) => void;
  onPrimary: (value: string) => void;
  onSecondary: (value: string) => void;
}) {
  const { t } = useTranslation('teams');
  const nameId = useId();
  const shortId = useId();
  const primaryId = useId();
  const secondaryId = useId();

  return (
    <>
      <Field
        label={t('newEntryName')}
        htmlFor={nameId}
        required
        counter={`${name.length}/100`}
        message={nameMessage}
        messageTone={nameMessageTone}
      >
        <TextInput
          id={nameId}
          value={name}
          maxLength={100}
          required
          disabled={disabled}
          allowClear
          invalid={nameMessageTone === 'error' && Boolean(nameMessage)}
          placeholder={t('newEntryPlaceholder')}
          onChange={(event) => onName(event.target.value)}
        />
      </Field>
      <Field
        label={t('shortName')}
        htmlFor={shortId}
        required
        width="sm"
        counter={`${shortName.length}/${SHORT_NAME_MAX_LENGTH}`}
        message={shortNameMessage}
        messageTone={shortNameMessageTone}
      >
        <TextInput
          id={shortId}
          value={shortName}
          maxLength={SHORT_NAME_MAX_LENGTH}
          required
          disabled={disabled}
          allowClear
          invalid={
            shortNameMessageTone === 'error' && Boolean(shortNameMessage)
          }
          onChange={(event) => onShortName(event.target.value)}
        />
      </Field>
      <LogoMediaField
        name={name.trim() || t('newEntryPlaceholder')}
        value={logoMediaId}
        primaryColor={primaryColor.trim() || null}
        onChange={onLogo}
        disabled={disabled}
        label={t('logo')}
      />
      <div className="ds-form--inline">
        <Field label={t('primaryColor')} htmlFor={primaryId}>
          <ColorPicker
            id={primaryId}
            aria-label={t('primaryColor')}
            value={primaryColor}
            disabled={disabled}
            onChange={onPrimary}
          />
        </Field>
        <Field label={t('secondaryColor')} htmlFor={secondaryId}>
          <ColorPicker
            id={secondaryId}
            aria-label={t('secondaryColor')}
            value={secondaryColor}
            disabled={disabled}
            onChange={onSecondary}
          />
        </Field>
      </div>
    </>
  );
}

function TeamsPlateauReading({
  activeCount,
  occupyingCount,
  minimumTeams,
  maximumTeams,
}: {
  activeCount: number;
  occupyingCount: number;
  minimumTeams: number;
  maximumTeams: number;
}) {
  const { t } = useTranslation('teams');
  const availableSlots = Math.max(0, maximumTeams - occupyingCount);
  const belowMinimum = activeCount < minimumTeams;
  const atCap = occupyingCount >= maximumTeams;
  const missingMinimum = Math.max(0, minimumTeams - activeCount);
  const fillRatio =
    maximumTeams > 0 ? Math.min(1, occupyingCount / maximumTeams) : 0;
  const markerRatio =
    maximumTeams > 0 ? Math.min(1, minimumTeams / maximumTeams) : 0;
  const showMarker =
    minimumTeams > 0 && maximumTeams > 0 && minimumTeams < maximumTeams;

  const statusTone = atCap ? 'cap' : belowMinimum ? 'blocking' : 'ok';

  const meterTone: MeterTone =
    statusTone === 'cap'
      ? 'info'
      : statusTone === 'blocking'
        ? 'error'
        : 'success';

  const statusLabel = atCap
    ? t('plateauCapReached')
    : belowMinimum
      ? t('plateauStillNeeded', { count: missingMinimum })
      : t('plateauMinimumReached');

  const gaugeAria = t('plateauGaugeAria', {
    count: occupyingCount,
    occupying: occupyingCount,
    active: activeCount,
    min: minimumTeams,
    max: maximumTeams,
    available: t('plateauPlacesAvailable', { count: availableSlots }),
  });

  return (
    <div className="teams__plateau" role="status">
      <p
        className={`teams__plateau-status teams__plateau-status--${statusTone}`}
      >
        {statusLabel}
      </p>

      {maximumTeams > 0 && (
        <div className="teams__plateau-gaugeBlock">
          <Meter
            ratio={fillRatio}
            tone={meterTone}
            aria-valuemin={0}
            aria-valuemax={maximumTeams}
            aria-valuenow={occupyingCount}
            aria-valuetext={gaugeAria}
            marker={
              showMarker
                ? {
                    ratio: markerRatio,
                    label: t('plateauMinMarker', { min: minimumTeams }),
                  }
                : undefined
            }
          />

          {!atCap && (
            <p className="teams__plateau-capacity">
              {t('plateauPlacesAvailable', { count: availableSlots })}
            </p>
          )}
        </div>
      )}
    </div>
  );
}

function tileStatusBadge(
  status: EntryStatus,
  t: (key: string) => string,
): ReactNode {
  if (status === 'Withdrawn') {
    return (
      <StatusBadge tone="warn" density="compact">
        {t('withdrawnBadge')}
      </StatusBadge>
    );
  }
  return null;
}
