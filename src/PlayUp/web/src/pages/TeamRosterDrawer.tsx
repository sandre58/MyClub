import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query';
import {
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type SubmitEvent,
  type ReactNode,
} from 'react';
import { useTranslation } from 'react-i18next';
import {
  addDeclaredMember,
  removeDeclaredMember,
  removeDeclaredMembers,
  renameDeclaredMember,
} from '../api';
import { SelectionBar } from '../design-system/components/SelectionBar';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Alert } from '../design-system/components/Alert';
import { Tooltip } from '../design-system/components/Tooltip';
import { TeamCrest } from '../design-system/TeamCrest';
import {
  ChevronDownIcon,
  CloseIcon,
  SidebarCollapseIcon,
} from '../design-system/icons/shellIcons';
import {
  CheckIcon,
  PencilIcon,
  PersonIcon,
  PlusIcon,
  TrashIcon,
} from '../design-system/icons/contentIcons';
import { queryKeys } from '../queryKeys';
import { EntryStatusBadge, MutationError } from '../ui';
import {
  MEMBER_DISPLAY_NAME_MAX_LENGTH,
  type DeclaredMember,
  type DeclaredMemberRole,
  type EntryStatus,
  type StructureEntry,
  type StructureView,
} from '../types';

async function invalidateAfterRosterMutation(
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
  ]);
}

function membersOf(
  entry: StructureEntry,
  role: DeclaredMemberRole,
): DeclaredMember[] {
  return (entry.declaredMembers ?? []).filter((member) => member.role === role);
}

function memberInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return '?';
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0] ?? ''}${parts[parts.length - 1][0] ?? ''}`.toUpperCase();
}

function canMutateRoster(entryStatus: EntryStatus): boolean {
  return entryStatus === 'Active';
}

function parseRgbChannels(hex: string): [number, number, number] | null {
  const raw = hex.replace('#', '');
  if (raw.length !== 6 || Number.isNaN(Number.parseInt(raw, 16))) {
    return null;
  }

  return [
    Number.parseInt(raw.slice(0, 2), 16),
    Number.parseInt(raw.slice(2, 4), 16),
    Number.parseInt(raw.slice(4, 6), 16),
  ];
}

function contrastInk(hex: string): string {
  const channels = parseRgbChannels(hex);
  if (!channels) {
    return 'var(--color-text-secondary)';
  }

  const [r, g, b] = channels;
  const yiq = (r * 299 + g * 587 + b * 114) / 1000;
  return yiq >= 150 ? '#1a1a1a' : '#fff';
}

function relativeLuminance(hex: string): number | null {
  const channels = parseRgbChannels(hex);
  if (!channels) {
    return null;
  }

  const linear = channels.map((channel) => {
    const c = channel / 255;
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
}

function hasReadableContrast(fill: string, ink: string): boolean {
  const a = relativeLuminance(fill);
  const b = relativeLuminance(ink);
  if (a == null || b == null) {
    return false;
  }

  const lighter = Math.max(a, b);
  const darker = Math.min(a, b);
  return (lighter + 0.05) / (darker + 0.05) >= 2.5;
}

function kitAvatarStyle(
  primary: string | null | undefined,
  secondary: string | null | undefined,
): CSSProperties | undefined {
  const home = primary?.trim();
  const away = secondary?.trim();
  if (!home && !away) {
    return undefined;
  }

  const fill = home || away!;
  const awayInk =
    away && away !== fill && hasReadableContrast(fill, away) ? away : null;
  return {
    backgroundColor: fill,
    color: awayInk ?? contrastInk(fill),
  };
}

export function TeamRosterDrawer({
  data,
  entryId,
  onBack,
  onEditIdentity,
}: {
  data: StructureView;
  entryId: string;
  onBack?: () => void;
  onEditIdentity?: () => void;
}) {
  const { t } = useTranslation('teams');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const nameRef = useRef<HTMLInputElement>(null);
  const splitRef = useRef<HTMLDivElement>(null);
  const [displayName, setDisplayName] = useState('');
  const [staffMenuOpen, setStaffMenuOpen] = useState(false);
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);
  const [pendingRemove, setPendingRemove] = useState<{
    removable: DeclaredMember[];
    blocked: DeclaredMember[];
  } | null>(null);
  const [pendingRenameId, setPendingRenameId] = useState<string | null>(null);
  const [renameDraft, setRenameDraft] = useState('');

  const entry = data.participants.entries.find(
    (candidate) => candidate.entryId === entryId,
  );

  useEffect(() => {
    setSelectedMemberIds([]);
    setPendingRemove(null);
    setPendingRenameId(null);
    setRenameDraft('');
  }, [entryId]);

  const addMutation = useMutation({
    mutationFn: (payload: { name: string; role: DeclaredMemberRole }) =>
      addDeclaredMember(data.competitionId, entryId, {
        displayName: payload.name,
        role: payload.role,
      }),
    onSuccess: async () => {
      setDisplayName('');
      setStaffMenuOpen(false);
      await invalidateAfterRosterMutation(queryClient, data.competitionId);
      nameRef.current?.focus();
    },
  });

  const renameMutation = useMutation({
    mutationFn: ({ memberId, name }: { memberId: string; name: string }) =>
      renameDeclaredMember(data.competitionId, entryId, memberId, {
        displayName: name,
      }),
    onSuccess: async () => {
      setPendingRenameId(null);
      setRenameDraft('');
      await invalidateAfterRosterMutation(queryClient, data.competitionId);
    },
  });

  const removeMutation = useMutation({
    mutationFn: (memberIds: string[]) =>
      memberIds.length === 1
        ? removeDeclaredMember(data.competitionId, entryId, memberIds[0])
        : removeDeclaredMembers(data.competitionId, entryId, {
            memberIds,
          }),
    onSuccess: async () => {
      setPendingRemove(null);
      setSelectedMemberIds([]);
      await invalidateAfterRosterMutation(queryClient, data.competitionId);
    },
    onError: () => {
      setPendingRemove(null);
    },
  });

  useEffect(() => {
    if (!staffMenuOpen) {
      return;
    }
    function onPointerDown(event: PointerEvent) {
      if (
        splitRef.current &&
        event.target instanceof Node &&
        !splitRef.current.contains(event.target)
      ) {
        setStaffMenuOpen(false);
      }
    }
    window.addEventListener('pointerdown', onPointerDown);
    return () => window.removeEventListener('pointerdown', onPointerDown);
  }, [staffMenuOpen]);

  function clearRowEditors() {
    setPendingRenameId(null);
    setRenameDraft('');
  }

  function submitAdd(role: DeclaredMemberRole) {
    const name = displayName.trim();
    if (name.length === 0 || addMutation.isPending) {
      return;
    }
    clearRowEditors();
    setPendingRemove(null);
    renameMutation.reset();
    removeMutation.reset();
    addMutation.mutate({ name, role });
  }

  if (!entry) {
    return (
      <div className="teams-drawer__idle">
        <Alert tone="danger" role="alert">
          {t('roster.entryMissing')}
        </Alert>
      </div>
    );
  }

  const players = membersOf(entry, 'Player');
  const staff = membersOf(entry, 'Staff');
  const canMutate = canMutateRoster(entry.status);
  const avatarStyle = kitAvatarStyle(entry.primaryColor, entry.secondaryColor);
  const mutationError =
    addMutation.error ?? renameMutation.error ?? removeMutation.error;
  const rowBusy =
    addMutation.isPending ||
    renameMutation.isPending ||
    removeMutation.isPending;
  const addDisabled =
    !canMutate || addMutation.isPending || displayName.trim().length === 0;

  function startRename(member: DeclaredMember) {
    addMutation.reset();
    renameMutation.reset();
    removeMutation.reset();
    setPendingRemove(null);
    setPendingRenameId(member.memberId);
    setRenameDraft(member.displayName);
  }

  function startRemove(member: DeclaredMember) {
    if (member.referencedOnMatchSheet) {
      return;
    }
    addMutation.reset();
    renameMutation.reset();
    removeMutation.reset();
    clearRowEditors();
    setPendingRemove({ removable: [member], blocked: [] });
  }

  function startRemoveSelection() {
    if (!entry || selectedMemberIds.length === 0 || pendingRenameId != null) {
      return;
    }
    const selected = (entry.declaredMembers ?? []).filter((member) =>
      selectedMemberIds.includes(member.memberId),
    );
    const removable = selected.filter(
      (member) => member.referencedOnMatchSheet !== true,
    );
    const blocked = selected.filter(
      (member) => member.referencedOnMatchSheet === true,
    );
    addMutation.reset();
    renameMutation.reset();
    removeMutation.reset();
    clearRowEditors();
    setPendingRemove({ removable, blocked });
  }

  function toggleMember(memberId: string) {
    if (pendingRenameId != null) {
      return;
    }
    setSelectedMemberIds((current) =>
      current.includes(memberId)
        ? current.filter((id) => id !== memberId)
        : [...current, memberId],
    );
  }

  function confirmRename(member: DeclaredMember) {
    const name = renameDraft.trim();
    if (name.length === 0 || rowBusy) {
      return;
    }
    if (name === member.displayName) {
      clearRowEditors();
      return;
    }
    addMutation.reset();
    removeMutation.reset();
    renameMutation.mutate({ memberId: member.memberId, name });
  }

  const selectionSuspended = pendingRenameId != null;
  const selectedCount = selectedMemberIds.length;
  const rosterGroupProps = {
    avatarStyle,
    canMutate,
    rowBusy,
    pendingRenameId,
    renameDraft,
    renamePending: renameMutation.isPending,
    selectedMemberIds,
    selectionSuspended,
    onToggleMember: toggleMember,
    onRenameDraft: setRenameDraft,
    onStartRename: startRename,
    onStartRemove: startRemove,
    onConfirmRename: confirmRename,
    onCancelRow: clearRowEditors,
  } as const;

  const compactIcon =
    'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';
  const confirmRemovable = pendingRemove?.removable ?? [];
  const confirmBlocked = pendingRemove?.blocked ?? [];
  const confirmNoneOk =
    pendingRemove != null &&
    confirmRemovable.length === 0 &&
    confirmBlocked.length > 0;
  const confirmMixed =
    pendingRemove != null &&
    confirmRemovable.length > 0 &&
    confirmBlocked.length > 0;
  const confirmTitle =
    pendingRemove == null
      ? ''
      : confirmRemovable.length === 1 && confirmBlocked.length === 0
        ? t('roster.removeTitle', { name: confirmRemovable[0].displayName })
        : t('roster.removeLotTitle', {
            count: Math.max(confirmRemovable.length, selectedCount),
          });
  const confirmMessage =
    pendingRemove == null ? (
      ''
    ) : confirmNoneOk ? (
      <>
        <p className="ds-body">{t('roster.removeLotNoneOkLead')}</p>
        <ul className="teams-confirm-remove__list">
          {confirmBlocked.map((member) => (
            <li key={member.memberId}>{member.displayName}</li>
          ))}
        </ul>
      </>
    ) : confirmMixed ? (
      <>
        <p className="ds-body">
          {t('roster.removeLotMixedLead', { count: confirmRemovable.length })}
        </p>
        <p className="ds-body">
          {t('roster.removeLotBlockedLead', { count: confirmBlocked.length })}
        </p>
        <ul className="teams-confirm-remove__list">
          {confirmBlocked.map((member) => (
            <li key={member.memberId}>{member.displayName}</li>
          ))}
        </ul>
      </>
    ) : confirmRemovable.length === 1 ? (
      t('roster.removeConsequence', {
        name: confirmRemovable[0].displayName,
      })
    ) : (
      t('roster.removeLotAllOk', { count: confirmRemovable.length })
    );
  const confirmLabel =
    confirmRemovable.length <= 1
      ? t('roster.confirmRemove')
      : t('roster.confirmRemoveLot', { count: confirmRemovable.length });

  return (
    <>
      {onBack && (
        <button type="button" className="teams-drawer__back" onClick={onBack}>
          <SidebarCollapseIcon size="sm" aria-hidden="true" />
          {t('title')}
        </button>
      )}
      <header className="teams-drawer__head">
        <div className="teams-drawer__identity">
          <TeamCrest
            className="teams-drawer__crest"
            name={entry.displayName}
            logoMediaId={entry.logoMediaId}
            primaryColor={entry.primaryColor}
            size="lg"
          />
          <div className="teams-drawer__identity-text">
            <h2 className="teams-drawer__name">{entry.displayName}</h2>
            <EntryStatusBadge status={entry.status} />
          </div>
        </div>
        {onEditIdentity && (
          <div className="teams-drawer__identity-actions">
            <Tooltip content={t('roster.editIdentityTooltip')}>
              <button
                type="button"
                className={compactIcon}
                aria-label={t('roster.editIdentity')}
                onClick={onEditIdentity}
              >
                <PencilIcon size="sm" />
              </button>
            </Tooltip>
          </div>
        )}
      </header>

      {canMutate && (
        <div className="ds-selection-bar-slot teams-drawer__roster-bar-slot">
          {selectedCount >= 1 && (
            <SelectionBar
              countLabel={t('roster.selectionCount', { count: selectedCount })}
            >
              <Tooltip content={t('roster.removeSelection')}>
                <button
                  type="button"
                  className={compactIcon}
                  aria-label={t('roster.removeSelection')}
                  disabled={rowBusy || selectionSuspended}
                  onClick={startRemoveSelection}
                >
                  <TrashIcon size="sm" />
                </button>
              </Tooltip>
              <Tooltip content={t('roster.clearSelection')}>
                <button
                  type="button"
                  className={compactIcon}
                  aria-label={t('roster.clearSelection')}
                  disabled={selectionSuspended}
                  onClick={() => setSelectedMemberIds([])}
                >
                  <CloseIcon size="sm" />
                </button>
              </Tooltip>
            </SelectionBar>
          )}
        </div>
      )}

      <div className="teams-drawer__body">
        <RosterGroup
          headingId="teams-roster-players"
          heading={
            <>
              <span className="teams-roster-heading__label">
                {t('roster.playersHeading')}
              </span>
              <span className="teams-roster-heading__count">
                {' · '}
                {players.length}
              </span>
            </>
          }
          emptyLabel={t('roster.emptyPlayers')}
          members={players}
          {...rosterGroupProps}
        />

        <RosterGroup
          headingId="teams-roster-staff"
          heading={
            <>
              <span className="teams-roster-heading__label">
                {t('roster.staffHeading')}
              </span>
              <span className="teams-roster-heading__count">
                {' · '}
                {staff.length}
              </span>
            </>
          }
          emptyLabel={t('roster.emptyStaff')}
          members={staff}
          {...rosterGroupProps}
        />
      </div>

      {mutationError && <MutationError error={mutationError} />}

      <form
        className="teams-drawer__add"
        onSubmit={(event: SubmitEvent) => {
          event.preventDefault();
          submitAdd('Player');
        }}
      >
        <div className="teams-add-group" ref={splitRef}>
          <label className="teams-add-group__field">
            <span className="ds-visually-hidden">{t('roster.addName')}</span>
            <input
              ref={nameRef}
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              disabled={!canMutate || addMutation.isPending}
              placeholder={t('roster.addPlaceholder')}
              maxLength={MEMBER_DISPLAY_NAME_MAX_LENGTH}
            />
          </label>
          <div className="teams-add-group__actions">
            {canMutate ? (
              <Tooltip content={t('roster.addPlayer')}>
                <button
                  type="submit"
                  className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact teams-add-group__add"
                  disabled={addDisabled}
                  aria-label={t('roster.addPlayer')}
                >
                  {addMutation.isPending ? (
                    <span className="ds-spinner" aria-hidden="true" />
                  ) : (
                    <PlusIcon size="sm" />
                  )}
                </button>
              </Tooltip>
            ) : (
              <Tooltip content={t('roster.readOnly')}>
                <button
                  type="submit"
                  className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact teams-add-group__add"
                  disabled
                  aria-label={t('roster.addPlayer')}
                >
                  <PlusIcon size="sm" />
                </button>
              </Tooltip>
            )}
            {canMutate ? (
              <Tooltip content={t('roster.addStaff')}>
                <button
                  type="button"
                  className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact teams-add-group__toggle"
                  disabled={addMutation.isPending}
                  aria-expanded={staffMenuOpen}
                  aria-haspopup="menu"
                  aria-label={t('roster.addStaff')}
                  onClick={() => setStaffMenuOpen((open) => !open)}
                >
                  <ChevronDownIcon size="sm" />
                </button>
              </Tooltip>
            ) : (
              <Tooltip content={t('roster.readOnly')}>
                <button
                  type="button"
                  className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact teams-add-group__toggle"
                  disabled
                  aria-expanded={false}
                  aria-haspopup="menu"
                  aria-label={t('roster.addStaff')}
                >
                  <ChevronDownIcon size="sm" />
                </button>
              </Tooltip>
            )}
          </div>
          {staffMenuOpen && (
            <ul className="teams-split__menu" role="menu">
              <li role="none">
                <button
                  type="button"
                  className="teams-split__item"
                  role="menuitem"
                  disabled={addDisabled}
                  onClick={() => submitAdd('Staff')}
                >
                  {t('roster.addStaff')}
                </button>
              </li>
            </ul>
          )}
        </div>
      </form>

      <ConfirmDialog
        open={pendingRemove != null}
        title={confirmTitle}
        message={confirmMessage}
        confirmLabel={confirmLabel}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        danger
        confirmDisabled={removeMutation.isPending || confirmNoneOk}
        confirmPending={removeMutation.isPending}
        confirmPendingLabel={t('roster.removing')}
        onCancel={() => {
          if (removeMutation.isPending) {
            return;
          }
          setPendingRemove(null);
        }}
        onConfirm={() => {
          if (
            !pendingRemove ||
            removeMutation.isPending ||
            pendingRemove.removable.length === 0
          ) {
            return;
          }
          removeMutation.mutate(
            pendingRemove.removable.map((member) => member.memberId),
          );
        }}
      />
    </>
  );
}

function RosterGroup({
  headingId,
  heading,
  emptyLabel,
  members,
  avatarStyle,
  canMutate,
  rowBusy,
  pendingRenameId,
  renameDraft,
  renamePending,
  selectedMemberIds,
  selectionSuspended,
  onToggleMember,
  onRenameDraft,
  onStartRename,
  onStartRemove,
  onConfirmRename,
  onCancelRow,
}: {
  headingId: string;
  heading: ReactNode;
  emptyLabel: string;
  members: DeclaredMember[];
  avatarStyle?: CSSProperties;
  canMutate: boolean;
  rowBusy: boolean;
  pendingRenameId: string | null;
  renameDraft: string;
  renamePending: boolean;
  selectedMemberIds: string[];
  selectionSuspended: boolean;
  onToggleMember: (memberId: string) => void;
  onRenameDraft: (value: string) => void;
  onStartRename: (member: DeclaredMember) => void;
  onStartRemove: (member: DeclaredMember) => void;
  onConfirmRename: (member: DeclaredMember) => void;
  onCancelRow: () => void;
}) {
  const { t } = useTranslation('teams');
  const { t: tc } = useTranslation('common');

  return (
    <section className="teams-drawer__section" aria-labelledby={headingId}>
      <h3 className="teams-drawer__heading" id={headingId}>
        {heading}
      </h3>

      {members.length === 0 ? (
        <div className="teams-member teams-member--empty">
          <span className="teams-member__empty-icon" aria-hidden="true">
            <PersonIcon size="lg" />
          </span>
          <p>{emptyLabel}</p>
        </div>
      ) : (
        <ul className="teams-drawer__list">
          {members.map((member) => {
            const editing = pendingRenameId === member.memberId;
            const onMatchSheet = member.referencedOnMatchSheet === true;
            const removeBlocked = !canMutate || onMatchSheet;
            const selected = selectedMemberIds.includes(member.memberId);
            const removeHint = onMatchSheet
              ? t('roster.onMatchSheetHint')
              : canMutate
                ? t('roster.removeMemberTooltip')
                : t('roster.readOnly');
            return (
              <li key={member.memberId}>
                <div
                  className="teams-member"
                  data-selected={selected ? 'true' : 'false'}
                >
                  {canMutate && (
                    <label className="teams-member__check">
                      <input
                        type="checkbox"
                        checked={selected}
                        disabled={selectionSuspended || rowBusy}
                        aria-label={t('roster.selectMember', {
                          name: member.displayName,
                        })}
                        onChange={() => onToggleMember(member.memberId)}
                      />
                    </label>
                  )}
                  <span className="teams-member__main">
                    <span
                      className="teams-member__avatar"
                      style={avatarStyle}
                      aria-hidden="true"
                    >
                      {memberInitials(member.displayName)}
                    </span>
                    {editing ? (
                      <form
                        className="teams-member__edit"
                        onSubmit={(event: SubmitEvent) => {
                          event.preventDefault();
                          onConfirmRename(member);
                        }}
                      >
                        <label className="teams-member__edit-field">
                          <span className="ds-visually-hidden">
                            {t('roster.renameName')}
                          </span>
                          <input
                            value={renameDraft}
                            onChange={(event) =>
                              onRenameDraft(event.target.value)
                            }
                            disabled={renamePending}
                            maxLength={MEMBER_DISPLAY_NAME_MAX_LENGTH}
                            required
                            autoFocus
                          />
                        </label>
                        <div className="ds-icon-toolbar">
                          <Tooltip content={t('roster.confirmRename')}>
                            <button
                              type="submit"
                              className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact ds-icon-button--affirm"
                              disabled={
                                renamePending || renameDraft.trim().length === 0
                              }
                              aria-label={t('roster.confirmRename')}
                            >
                              {renamePending ? (
                                <span className="ds-spinner" aria-hidden="true" />
                              ) : (
                                <CheckIcon size="sm" />
                              )}
                            </button>
                          </Tooltip>
                          <Tooltip content={tc('cancel')}>
                            <button
                              type="button"
                              className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact ds-icon-button--dismiss"
                              disabled={renamePending}
                              aria-label={tc('cancel')}
                              onClick={onCancelRow}
                            >
                              <CloseIcon size="sm" />
                            </button>
                          </Tooltip>
                        </div>
                      </form>
                    ) : (
                      <span className="teams-member__name">
                        {member.displayName}
                      </span>
                    )}
                  </span>
                  {!editing && (
                    <div className="teams-member__actions">
                      <div className="ds-icon-toolbar">
                        {canMutate ? (
                          <Tooltip content={t('roster.editMemberTooltip')}>
                            <button
                              type="button"
                              className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
                              disabled={rowBusy}
                              aria-label={t('roster.editMember', {
                                name: member.displayName,
                              })}
                              onClick={() => onStartRename(member)}
                            >
                              <PencilIcon size="sm" />
                            </button>
                          </Tooltip>
                        ) : (
                          <Tooltip content={t('roster.readOnly')}>
                            <button
                              type="button"
                              className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
                              disabled
                              aria-label={t('roster.editMember', {
                                name: member.displayName,
                              })}
                            >
                              <PencilIcon size="sm" />
                            </button>
                          </Tooltip>
                        )}
                        {removeBlocked ? (
                          <Tooltip content={removeHint}>
                            <button
                              type="button"
                              className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
                              disabled
                              aria-label={t('roster.removeMember', {
                                name: member.displayName,
                              })}
                            >
                              <TrashIcon size="sm" />
                            </button>
                          </Tooltip>
                        ) : (
                          <Tooltip content={removeHint}>
                            <button
                              type="button"
                              className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
                              disabled={rowBusy}
                              aria-label={t('roster.removeMember', {
                                name: member.displayName,
                              })}
                              onClick={() => onStartRemove(member)}
                            >
                              <TrashIcon size="sm" />
                            </button>
                          </Tooltip>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
