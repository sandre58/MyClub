import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { useEffect, useRef, useState, type CSSProperties, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import {
  addDeclaredMember,
  changeDeclaredMemberRole,
  removeDeclaredMember,
  renameDeclaredMember,
} from '../api'
import { TeamCrest } from '../design-system/TeamCrest'
import { ChevronDownIcon } from '../design-system/icons/shellIcons'
import {
  PencilIcon,
  PersonIcon,
  PlusIcon,
  TrashIcon,
} from '../design-system/icons/overviewIcons'
import { queryKeys } from '../queryKeys'
import {
  EntryStatusBadge,
  MutationError,
  PendingLabel,
} from '../ui'
import {
  MEMBER_DISPLAY_NAME_MAX_LENGTH,
  type DeclaredMember,
  type DeclaredMemberRole,
  type EntryStatus,
  type OrganisationEntry,
  type OrganisationView,
} from '../types'

async function invalidateAfterRosterMutation(
  queryClient: QueryClient,
  competitionId: string,
) {
  await Promise.all([
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.organisation(competitionId),
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
  ])
}

function membersOf(
  entry: OrganisationEntry,
  role: DeclaredMemberRole,
): DeclaredMember[] {
  return (entry.declaredMembers ?? []).filter((member) => member.role === role)
}

function memberInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) {
    return '?'
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase()
  }
  return `${parts[0][0] ?? ''}${parts[parts.length - 1][0] ?? ''}`.toUpperCase()
}

function canMutateRoster(entryStatus: EntryStatus): boolean {
  return entryStatus === 'Active'
}

function parseRgbChannels(hex: string): [number, number, number] | null {
  const raw = hex.replace('#', '')
  if (raw.length !== 6 || Number.isNaN(Number.parseInt(raw, 16))) {
    return null
  }

  return [
    Number.parseInt(raw.slice(0, 2), 16),
    Number.parseInt(raw.slice(2, 4), 16),
    Number.parseInt(raw.slice(4, 6), 16),
  ]
}

function contrastInk(hex: string): string {
  const channels = parseRgbChannels(hex)
  if (!channels) {
    return 'var(--color-text-secondary)'
  }

  const [r, g, b] = channels
  const yiq = (r * 299 + g * 587 + b * 114) / 1000
  return yiq >= 150 ? '#1a1a1a' : '#fff'
}

function relativeLuminance(hex: string): number | null {
  const channels = parseRgbChannels(hex)
  if (!channels) {
    return null
  }

  const linear = channels.map((channel) => {
    const c = channel / 255
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
  })
  return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2]
}

function hasReadableContrast(fill: string, ink: string): boolean {
  const a = relativeLuminance(fill)
  const b = relativeLuminance(ink)
  if (a == null || b == null) {
    return false
  }

  const lighter = Math.max(a, b)
  const darker = Math.min(a, b)
  return (lighter + 0.05) / (darker + 0.05) >= 2.5
}

function kitAvatarStyle(
  primary: string | null | undefined,
  secondary: string | null | undefined,
): CSSProperties | undefined {
  const home = primary?.trim()
  const away = secondary?.trim()
  if (!home && !away) {
    return undefined
  }

  const fill = home || away!
  const awayInk =
    away && away !== fill && hasReadableContrast(fill, away) ? away : null
  return {
    backgroundColor: fill,
    color: awayInk ?? contrastInk(fill),
  }
}

export function TeamRosterDrawer({
  data,
  entryId,
}: {
  data: OrganisationView
  entryId: string
}) {
  const { t } = useTranslation('teams')
  const queryClient = useQueryClient()
  const nameRef = useRef<HTMLInputElement>(null)
  const splitRef = useRef<HTMLDivElement>(null)
  const [displayName, setDisplayName] = useState('')
  const [staffMenuOpen, setStaffMenuOpen] = useState(false)
  const [pendingRemoveId, setPendingRemoveId] = useState<string | null>(null)
  const [pendingRenameId, setPendingRenameId] = useState<string | null>(null)
  const [renameDraft, setRenameDraft] = useState('')
  const [roleDraft, setRoleDraft] = useState<DeclaredMemberRole>('Player')

  const entry = data.participants.entries.find(
    (candidate) => candidate.entryId === entryId,
  )

  const addMutation = useMutation({
    mutationFn: (payload: { name: string; role: DeclaredMemberRole }) =>
      addDeclaredMember(data.competitionId, entryId, {
        displayName: payload.name,
        role: payload.role,
      }),
    onSuccess: async () => {
      setDisplayName('')
      setStaffMenuOpen(false)
      await invalidateAfterRosterMutation(queryClient, data.competitionId)
      nameRef.current?.focus()
    },
  })

  const renameMutation = useMutation({
    mutationFn: ({ memberId, name }: { memberId: string; name: string }) =>
      renameDeclaredMember(data.competitionId, entryId, memberId, {
        displayName: name,
      }),
    onSuccess: async () => {
      setPendingRenameId(null)
      setRenameDraft('')
      await invalidateAfterRosterMutation(queryClient, data.competitionId)
    },
  })

  const removeMutation = useMutation({
    mutationFn: (memberId: string) =>
      removeDeclaredMember(data.competitionId, entryId, memberId),
    onSuccess: async () => {
      setPendingRemoveId(null)
      await invalidateAfterRosterMutation(queryClient, data.competitionId)
    },
    onError: () => {
      setPendingRemoveId(null)
    },
  })

  const roleMutation = useMutation({
    mutationFn: ({
      memberId,
      nextRole,
    }: {
      memberId: string
      nextRole: DeclaredMemberRole
    }) =>
      changeDeclaredMemberRole(data.competitionId, entryId, memberId, {
        role: nextRole,
      }),
    onSuccess: async () => {
      setPendingRenameId(null)
      await invalidateAfterRosterMutation(queryClient, data.competitionId)
    },
  })

  useEffect(() => {
    if (!staffMenuOpen) {
      return
    }
    function onPointerDown(event: PointerEvent) {
      if (
        splitRef.current &&
        event.target instanceof Node &&
        !splitRef.current.contains(event.target)
      ) {
        setStaffMenuOpen(false)
      }
    }
    window.addEventListener('pointerdown', onPointerDown)
    return () => window.removeEventListener('pointerdown', onPointerDown)
  }, [staffMenuOpen])

  function clearRowEditors() {
    setPendingRemoveId(null)
    setPendingRenameId(null)
    setRenameDraft('')
    setRoleDraft('Player')
  }

  function submitAdd(role: DeclaredMemberRole) {
    const name = displayName.trim()
    if (name.length === 0 || addMutation.isPending) {
      return
    }
    clearRowEditors()
    renameMutation.reset()
    removeMutation.reset()
    addMutation.mutate({ name, role })
  }

  if (!entry) {
    return (
      <div className="teams-drawer__idle">
        <p className="ds-notice ds-notice--danger" role="alert">
          {t('roster.entryMissing')}
        </p>
      </div>
    )
  }

  const players = membersOf(entry, 'Player')
  const staff = membersOf(entry, 'Staff')
  const canMutate = canMutateRoster(entry.status)
  const avatarStyle = kitAvatarStyle(entry.primaryColor, entry.secondaryColor)
  const mutationError =
    addMutation.error ??
    renameMutation.error ??
    removeMutation.error ??
    roleMutation.error
  const rowBusy =
    addMutation.isPending ||
    renameMutation.isPending ||
    removeMutation.isPending ||
    roleMutation.isPending
  const addDisabled =
    !canMutate || addMutation.isPending || displayName.trim().length === 0

  return (
    <>
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
      </header>

      <div className="teams-drawer__body">
        <RosterGroup
          headingId="teams-roster-players"
          heading={t('roster.playersHeading')}
          emptyLabel={t('roster.emptyPlayers')}
          members={players}
          avatarStyle={avatarStyle}
          canMutate={canMutate}
          rowBusy={rowBusy}
          pendingRemoveId={pendingRemoveId}
          pendingRenameId={pendingRenameId}
          renameDraft={renameDraft}
          roleDraft={roleDraft}
          renamePending={renameMutation.isPending || roleMutation.isPending}
          removePending={removeMutation.isPending}
          onRenameDraft={setRenameDraft}
          onRoleDraft={setRoleDraft}
          onStartRename={(member) => {
            addMutation.reset()
            renameMutation.reset()
            removeMutation.reset()
            setPendingRemoveId(null)
            setPendingRenameId(member.memberId)
            setRenameDraft(member.displayName)
            setRoleDraft(member.role)
          }}
          onStartRemove={(member) => {
            addMutation.reset()
            renameMutation.reset()
            removeMutation.reset()
            setPendingRenameId(null)
            setRenameDraft('')
            setPendingRemoveId(member.memberId)
          }}
          onConfirmRename={(member) => {
            const name = renameDraft.trim()
            if (name.length === 0 || rowBusy) {
              return
            }
            const nameChanged = name !== member.displayName
            const roleChanged = roleDraft !== member.role
            if (!nameChanged && !roleChanged) {
              clearRowEditors()
              return
            }
            addMutation.reset()
            removeMutation.reset()
            if (nameChanged) {
              renameMutation.mutate({ memberId: member.memberId, name })
            }
            if (roleChanged) {
              roleMutation.mutate({
                memberId: member.memberId,
                nextRole: roleDraft,
              })
            }
            if (!nameChanged) {
              setPendingRenameId(null)
            }
          }}
          onConfirmRemove={(member) => removeMutation.mutate(member.memberId)}
          onCancelRow={clearRowEditors}
        />

        <RosterGroup
          headingId="teams-roster-staff"
          heading={t('roster.staffHeading')}
          emptyLabel={t('roster.emptyStaff')}
          members={staff}
          avatarStyle={avatarStyle}
          canMutate={canMutate}
          rowBusy={rowBusy}
          pendingRemoveId={pendingRemoveId}
          pendingRenameId={pendingRenameId}
          renameDraft={renameDraft}
          roleDraft={roleDraft}
          renamePending={renameMutation.isPending || roleMutation.isPending}
          removePending={removeMutation.isPending}
          onRenameDraft={setRenameDraft}
          onRoleDraft={setRoleDraft}
          onStartRename={(member) => {
            addMutation.reset()
            renameMutation.reset()
            removeMutation.reset()
            setPendingRemoveId(null)
            setPendingRenameId(member.memberId)
            setRenameDraft(member.displayName)
            setRoleDraft(member.role)
          }}
          onStartRemove={(member) => {
            addMutation.reset()
            renameMutation.reset()
            removeMutation.reset()
            setPendingRenameId(null)
            setRenameDraft('')
            setPendingRemoveId(member.memberId)
          }}
          onConfirmRename={(member) => {
            const name = renameDraft.trim()
            if (name.length === 0 || rowBusy) {
              return
            }
            const nameChanged = name !== member.displayName
            const roleChanged = roleDraft !== member.role
            if (!nameChanged && !roleChanged) {
              clearRowEditors()
              return
            }
            addMutation.reset()
            removeMutation.reset()
            if (nameChanged) {
              renameMutation.mutate({ memberId: member.memberId, name })
            }
            if (roleChanged) {
              roleMutation.mutate({
                memberId: member.memberId,
                nextRole: roleDraft,
              })
            }
            if (!nameChanged) {
              setPendingRenameId(null)
            }
          }}
          onConfirmRemove={(member) => removeMutation.mutate(member.memberId)}
          onCancelRow={clearRowEditors}
        />
      </div>

      {mutationError && <MutationError error={mutationError} />}

      <form
        className="teams-drawer__add"
        onSubmit={(event: FormEvent) => {
          event.preventDefault()
          submitAdd('Player')
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
            <button
              type="submit"
              className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact teams-add-group__add"
              disabled={addDisabled}
              title={canMutate ? t('roster.addPlayer') : t('roster.readOnly')}
              aria-label={t('roster.addPlayer')}
            >
              {addMutation.isPending ? (
                <span className="ds-spinner" aria-hidden="true" />
              ) : (
                <PlusIcon size="sm" />
              )}
            </button>
            <button
              type="button"
              className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact teams-add-group__toggle"
              disabled={!canMutate || addMutation.isPending}
              aria-expanded={staffMenuOpen}
              aria-haspopup="menu"
              title={canMutate ? t('roster.addStaff') : t('roster.readOnly')}
              aria-label={t('roster.addStaff')}
              onClick={() => setStaffMenuOpen((open) => !open)}
            >
              <ChevronDownIcon size="sm" />
            </button>
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
    </>
  )
}

function RosterGroup({
  headingId,
  heading,
  emptyLabel,
  members,
  avatarStyle,
  canMutate,
  rowBusy,
  pendingRemoveId,
  pendingRenameId,
  renameDraft,
  roleDraft,
  renamePending,
  removePending,
  onRenameDraft,
  onRoleDraft,
  onStartRename,
  onStartRemove,
  onConfirmRename,
  onConfirmRemove,
  onCancelRow,
}: {
  headingId: string
  heading: string
  emptyLabel: string
  members: DeclaredMember[]
  avatarStyle?: CSSProperties
  canMutate: boolean
  rowBusy: boolean
  pendingRemoveId: string | null
  pendingRenameId: string | null
  renameDraft: string
  roleDraft: DeclaredMemberRole
  renamePending: boolean
  removePending: boolean
  onRenameDraft: (value: string) => void
  onRoleDraft: (value: DeclaredMemberRole) => void
  onStartRename: (member: DeclaredMember) => void
  onStartRemove: (member: DeclaredMember) => void
  onConfirmRename: (member: DeclaredMember) => void
  onConfirmRemove: (member: DeclaredMember) => void
  onCancelRow: () => void
}) {
  const { t } = useTranslation('teams')
  const { t: tc } = useTranslation('common')

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
            const editing = pendingRenameId === member.memberId
            const removing = pendingRemoveId === member.memberId
            return (
              <li key={member.memberId}>
                <div className="teams-member">
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
                        onSubmit={(event: FormEvent) => {
                          event.preventDefault()
                          onConfirmRename(member)
                        }}
                      >
                        <label className="field">
                          {t('roster.renameName')}
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
                        <label className="field">
                          {t('roster.changeRole')}
                          <select
                            value={roleDraft}
                            disabled={renamePending}
                            aria-label={t('roster.changeRole')}
                            onChange={(event) =>
                              onRoleDraft(
                                event.target.value as DeclaredMemberRole,
                              )
                            }
                          >
                            <option value="Player">
                              {t('roster.rolePlayer')}
                            </option>
                            <option value="Staff">{t('roster.roleStaff')}</option>
                          </select>
                        </label>
                        <div className="teams-member__confirm-actions">
                          <button
                            type="submit"
                            className="ds-btn ds-btn--primary"
                            disabled={
                              renamePending || renameDraft.trim().length === 0
                            }
                          >
                            {renamePending ? (
                              <PendingLabel>{t('roster.renaming')}</PendingLabel>
                            ) : (
                              t('roster.confirmRename')
                            )}
                          </button>
                          <button
                            type="button"
                            className="ds-btn ds-btn--ghost"
                            disabled={renamePending}
                            onClick={onCancelRow}
                          >
                            {tc('cancel')}
                          </button>
                        </div>
                      </form>
                    ) : (
                      <span className="teams-member__name">
                        {member.displayName}
                      </span>
                    )}
                  </span>
                  {!editing && !removing && (
                    <div className="ds-icon-toolbar">
                      <button
                        type="button"
                        className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
                        disabled={!canMutate || rowBusy}
                        title={
                          canMutate
                            ? t('roster.editMemberTooltip')
                            : t('roster.readOnly')
                        }
                        aria-label={t('roster.editMember', {
                          name: member.displayName,
                        })}
                        onClick={() => onStartRename(member)}
                      >
                        <PencilIcon size="sm" />
                      </button>
                      <button
                        type="button"
                        className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
                        disabled={!canMutate || rowBusy}
                        title={
                          canMutate
                            ? t('roster.removeMemberTooltip')
                            : t('roster.readOnly')
                        }
                        aria-label={t('roster.removeMember', {
                          name: member.displayName,
                        })}
                        onClick={() => onStartRemove(member)}
                      >
                        <TrashIcon size="sm" />
                      </button>
                    </div>
                  )}
                </div>
                {removing && (
                  <div
                    className="teams-member__confirm ds-notice ds-notice--warning"
                    role="group"
                    aria-label={t('roster.removeConsequence', {
                      name: member.displayName,
                    })}
                  >
                    <p>
                      {t('roster.removeConsequence', {
                        name: member.displayName,
                      })}
                    </p>
                    <div className="teams-member__confirm-actions">
                      <button
                        type="button"
                        className="ds-btn ds-btn--destructive"
                        disabled={removePending}
                        onClick={() => onConfirmRemove(member)}
                      >
                        {removePending ? (
                          <PendingLabel>{t('roster.removing')}</PendingLabel>
                        ) : (
                          t('roster.confirmRemove')
                        )}
                      </button>
                      <button
                        type="button"
                        className="ds-btn ds-btn--ghost"
                        disabled={removePending}
                        onClick={onCancelRow}
                      >
                        {tc('cancel')}
                      </button>
                    </div>
                  </div>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
