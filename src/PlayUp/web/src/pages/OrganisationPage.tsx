import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  addCompetitionEntry,
  configureOrganisationStructure,
  excludeCompetitionEntry,
  fetchOrganisationView,
  renameCompetitionEntry,
  replaceCompetitionRegulation,
  withdrawCompetitionEntry,
} from '../api'
import { structureFormatKindLabel } from '../i18n/enumLabels'
import {
  CompetitionStatusBadge,
  EmptyState,
  EntryStatusBadge,
  ErrorState,
  LoadingState,
  MutationError,
  PageHeader,
  PendingLabel,
  StageStatusBadge,
} from '../ui'
import {
  type OrganisationEntry,
  type OrganisationView,
  type ReplaceRegulationRequest,
  type StructureFormatKind,
} from '../types'

const organisationQueryKey = (competitionId: string) =>
  ['competitions', competitionId, 'organisation'] as const

/**
 * Organisation Hub — GET /competitions/{id}/organisation + Slice 2 mutations.
 */
export function OrganisationPage() {
  const { competitionId = '' } = useParams()

  const query = useQuery({
    queryKey: organisationQueryKey(competitionId),
    queryFn: () => fetchOrganisationView(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow="Organisation"
        title="Organisation"
        back={
          competitionId
            ? {
                to: `/competitions/${competitionId}`,
                label: 'Back to workspace',
              }
            : undefined
        }
        badges={
          query.data && <CompetitionStatusBadge status={query.data.status} />
        }
      />

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <OrganisationViewPanel data={query.data} />}
    </main>
  )
}

function OrganisationViewPanel({ data }: { data: OrganisationView }) {
  const can = (action: string) => data.actions.includes(action)

  return (
    <div className="section-stack">
      <ReadinessSection readiness={data.readiness} />
      <ParticipantsSection
        data={data}
        canAdd={can('AddEntry')}
        canRename={can('RenameEntry')}
        canWithdraw={can('WithdrawEntry')}
        canExclude={can('ExcludeEntry')}
      />
      <StructureSection data={data} canConfigure={can('ConfigureStructure')} />
      <RegulationSection data={data} canReplace={can('ReplaceRegulation')} />
    </div>
  )
}

function ReadinessSection({
  readiness,
}: {
  readiness: OrganisationView['readiness']
}) {
  const ready = readiness.readyForNextSlice

  return (
    <section className="card" aria-labelledby="readiness-heading">
      <div className="card__head">
        <h2 className="card__title" id="readiness-heading">
          Readiness
        </h2>
        <span className={`status-badge status-badge--${ready ? 'ok' : 'warn'}`}>
          <span className="status-badge__dot" aria-hidden="true" />
          {ready ? 'Ready for next slice' : 'Preparation in progress'}
        </span>
      </div>

      <ul className="check-list">
        <Check ok={readiness.readyForNextSlice}>Ready for next slice</Check>
        <Check ok={readiness.readyForDraw}>Ready for draw</Check>
        <li className="check">
          <span className="check__mark" aria-hidden="true">
            ·
          </span>
          <span>Attached matches</span>
          <span className="caption">{readiness.attachedMatchCount}</span>
        </li>
      </ul>

      {readiness.blockers.length > 0 && (
        <div className="stack stack--tight">
          <h3 className="stat__label">Blockers</h3>
          <ul className="check-list">
            {readiness.blockers.map((code) => (
              <li key={code} className="check check--no">
                <span className="check__mark" aria-hidden="true">
                  !
                </span>
                <span className="mono">{code}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {readiness.hints.length > 0 && (
        <div className="stack stack--tight">
          <h3 className="stat__label">Hints</h3>
          <ul className="check-list">
            {readiness.hints.map((hint) => (
              <li key={hint} className="check">
                <span className="check__mark" aria-hidden="true">
                  →
                </span>
                <span className="muted">{hint}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  )
}

function Check({ ok, children }: { ok: boolean; children: string }) {
  return (
    <li className={`check ${ok ? 'check--yes' : 'check--no'}`}>
      <span className="check__mark" aria-hidden="true">
        {ok ? '✓' : '·'}
      </span>
      <span>{children}</span>
      <span className="caption">{ok ? 'Yes' : 'No'}</span>
    </li>
  )
}

function ParticipantsSection({
  data,
  canAdd,
  canRename,
  canWithdraw,
  canExclude,
}: {
  data: OrganisationView
  canAdd: boolean
  canRename: boolean
  canWithdraw: boolean
  canExclude: boolean
}) {
  const queryClient = useQueryClient()
  const [displayName, setDisplayName] = useState('')
  const competitionId = data.competitionId

  const invalidateOrganisation = async () => {
    await queryClient.invalidateQueries({
      queryKey: organisationQueryKey(competitionId),
    })
    await queryClient.invalidateQueries({
      queryKey: ['competitions', competitionId],
    })
    await queryClient.invalidateQueries({
      queryKey: ['competitions', competitionId, 'workspace'],
    })
  }

  const addMutation = useMutation({
    mutationFn: () =>
      addCompetitionEntry(competitionId, { displayName: displayName.trim() }),
    onSuccess: async () => {
      setDisplayName('')
      await invalidateOrganisation()
    },
  })

  return (
    <section className="card" aria-labelledby="participants-heading">
      <div className="card__head">
        <h2 className="card__title" id="participants-heading">
          Participants
        </h2>
        <p className="card__subtitle">
          {data.participants.activeCount} active ·{' '}
          {data.participants.occupyingCount} occupying a slot
        </p>
      </div>

      {data.participants.entries.length === 0 ? (
        <EmptyState title="No entries yet">
          Add the teams taking part; the structure and the draw need them.
        </EmptyState>
      ) : (
        <ul className="row-list">
          {data.participants.entries.map((entry) => (
            <li key={entry.entryId}>
              <EntryRow
                competitionId={competitionId}
                entry={entry}
                canRename={canRename}
                canWithdraw={canWithdraw}
                canExclude={canExclude}
                onChanged={invalidateOrganisation}
              />
            </li>
          ))}
        </ul>
      )}

      {canAdd && (
        <form
          className="form form--inline"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            if (displayName.trim().length === 0 || addMutation.isPending) {
              return
            }
            addMutation.mutate()
          }}
        >
          <label className="field">
            New entry name
            <input
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              disabled={addMutation.isPending}
              placeholder="e.g. Alpha FC"
              required
            />
          </label>
          <button
            type="submit"
            className="btn btn--primary"
            disabled={addMutation.isPending || displayName.trim().length === 0}
          >
            {addMutation.isPending ? (
              <PendingLabel>Adding…</PendingLabel>
            ) : (
              'Add entry'
            )}
          </button>
          {addMutation.isError && <MutationError error={addMutation.error} />}
        </form>
      )}
    </section>
  )
}

function EntryRow({
  competitionId,
  entry,
  canRename,
  canWithdraw,
  canExclude,
  onChanged,
}: {
  competitionId: string
  entry: OrganisationEntry
  canRename: boolean
  canWithdraw: boolean
  canExclude: boolean
  onChanged: () => Promise<void>
}) {
  const [name, setName] = useState(entry.displayName)
  const busyLabel = 'Working…'

  const renameMutation = useMutation({
    mutationFn: () =>
      renameCompetitionEntry(competitionId, entry.entryId, {
        displayName: name.trim(),
      }),
    onSuccess: onChanged,
  })

  const withdrawMutation = useMutation({
    mutationFn: () => withdrawCompetitionEntry(competitionId, entry.entryId),
    onSuccess: onChanged,
  })

  const excludeMutation = useMutation({
    mutationFn: () => excludeCompetitionEntry(competitionId, entry.entryId),
    onSuccess: onChanged,
  })

  const pending =
    renameMutation.isPending ||
    withdrawMutation.isPending ||
    excludeMutation.isPending

  const mutationError =
    renameMutation.error ?? withdrawMutation.error ?? excludeMutation.error

  return (
    <div className="entry">
      <p className="entry__identity">
        <span className="entry__name">{entry.displayName}</span>
        <EntryStatusBadge status={entry.status} />
      </p>
      {(canRename || canWithdraw || canExclude) && (
        <div className="entry__actions">
          {canRename && (
            <form
              className="form form--inline"
              onSubmit={(event: FormEvent) => {
                event.preventDefault()
                if (name.trim().length === 0 || pending) {
                  return
                }
                renameMutation.mutate()
              }}
            >
              <label className="field">
                Rename
                <input
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  disabled={pending}
                  required
                />
              </label>
              <button
                type="submit"
                className="btn btn--sm"
                disabled={pending || name.trim().length === 0}
              >
                {renameMutation.isPending ? (
                  <PendingLabel>{busyLabel}</PendingLabel>
                ) : (
                  'Rename'
                )}
              </button>
            </form>
          )}
          {canWithdraw && (
            <button
              type="button"
              className="btn btn--sm btn--ghost"
              disabled={pending}
              onClick={() => {
                if (
                  !window.confirm(
                    `Withdraw “${entry.displayName}” from this competition?`,
                  )
                ) {
                  return
                }
                withdrawMutation.mutate()
              }}
            >
              {withdrawMutation.isPending ? (
                <PendingLabel>{busyLabel}</PendingLabel>
              ) : (
                'Withdraw'
              )}
            </button>
          )}
          {canExclude && (
            <button
              type="button"
              className="btn btn--sm btn--danger"
              disabled={pending}
              onClick={() => {
                if (
                  !window.confirm(
                    `Exclude “${entry.displayName}” from this competition?`,
                  )
                ) {
                  return
                }
                excludeMutation.mutate()
              }}
            >
              {excludeMutation.isPending ? (
                <PendingLabel>{busyLabel}</PendingLabel>
              ) : (
                'Exclude'
              )}
            </button>
          )}
        </div>
      )}
      {mutationError && (
        <div className="entry__error">
          <MutationError error={mutationError} />
        </div>
      )}
    </div>
  )
}

function RegulationSection({
  data,
  canReplace,
}: {
  data: OrganisationView
  canReplace: boolean
}) {
  const queryClient = useQueryClient()
  const regulation = data.regulation
  const [form, setForm] = useState<ReplaceRegulationRequest>({
    minimumTeams: regulation.minimumTeams,
    maximumTeams: regulation.maximumTeams,
    durationPerPeriod: regulation.durationPerPeriod,
    numberOfPeriods: regulation.numberOfPeriods,
    halfTimeDuration: 15,
    winPoints: regulation.winPoints,
    drawPoints: regulation.drawPoints,
    lossPoints: regulation.lossPoints,
    forfeitWinnerGoals: 3,
    forfeitLoserGoals: 0,
  })

  const mutation = useMutation({
    mutationFn: () => replaceCompetitionRegulation(data.competitionId, form),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: organisationQueryKey(data.competitionId),
      })
      await queryClient.invalidateQueries({
        queryKey: ['competitions', data.competitionId, 'workspace'],
      })
    },
  })

  const setNumber =
    (key: keyof ReplaceRegulationRequest) =>
    (value: string) => {
      const parsed = Number(value)
      setForm((current) => ({
        ...current,
        [key]: Number.isFinite(parsed) ? parsed : current[key],
      }))
    }

  return (
    <section className="card" aria-labelledby="regulation-heading">
      <div className="card__head">
        <h2 className="card__title" id="regulation-heading">
          Regulation
        </h2>
      </div>

      <dl className="fact-list">
        <div className="fact">
          <dt className="fact__label">Teams</dt>
          <dd className="fact__value">
            {regulation.minimumTeams}–{regulation.maximumTeams}
          </dd>
        </div>
        <div className="fact">
          <dt className="fact__label">Match</dt>
          <dd className="fact__value">
            {regulation.numberOfPeriods}×{regulation.durationPerPeriod}′
          </dd>
        </div>
        <div className="fact">
          <dt className="fact__label">Points W / D / L</dt>
          <dd className="fact__value">
            {regulation.winPoints} / {regulation.drawPoints} /{' '}
            {regulation.lossPoints}
          </dd>
        </div>
      </dl>

      {canReplace && (
        <form
          className="form form--wide"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            if (mutation.isPending) {
              return
            }
            mutation.mutate()
          }}
        >
          <fieldset className="fieldset" disabled={mutation.isPending}>
            <legend className="fieldset__legend">Replace regulation</legend>
            <div className="form-row">
              <label className="field">
                Minimum teams
                <input
                  type="number"
                  value={form.minimumTeams}
                  onChange={(event) =>
                    setNumber('minimumTeams')(event.target.value)
                  }
                  required
                />
              </label>
              <label className="field">
                Maximum teams
                <input
                  type="number"
                  value={form.maximumTeams}
                  onChange={(event) =>
                    setNumber('maximumTeams')(event.target.value)
                  }
                  required
                />
              </label>
            </div>
            <div className="form-row">
              <label className="field">
                Duration per period
                <input
                  type="number"
                  value={form.durationPerPeriod}
                  onChange={(event) =>
                    setNumber('durationPerPeriod')(event.target.value)
                  }
                  required
                />
              </label>
              <label className="field">
                Number of periods
                <input
                  type="number"
                  value={form.numberOfPeriods}
                  onChange={(event) =>
                    setNumber('numberOfPeriods')(event.target.value)
                  }
                  required
                />
              </label>
              <label className="field">
                Half-time duration
                <input
                  type="number"
                  value={form.halfTimeDuration}
                  onChange={(event) =>
                    setNumber('halfTimeDuration')(event.target.value)
                  }
                  required
                />
              </label>
            </div>
            <div className="form-row">
              <label className="field">
                Win points
                <input
                  type="number"
                  value={form.winPoints}
                  onChange={(event) =>
                    setNumber('winPoints')(event.target.value)
                  }
                  required
                />
              </label>
              <label className="field">
                Draw points
                <input
                  type="number"
                  value={form.drawPoints}
                  onChange={(event) =>
                    setNumber('drawPoints')(event.target.value)
                  }
                  required
                />
              </label>
              <label className="field">
                Loss points
                <input
                  type="number"
                  value={form.lossPoints}
                  onChange={(event) =>
                    setNumber('lossPoints')(event.target.value)
                  }
                  required
                />
              </label>
            </div>
          </fieldset>
          <div className="button-row">
            <button
              type="submit"
              className="btn btn--primary"
              disabled={mutation.isPending}
            >
              {mutation.isPending ? (
                <PendingLabel>Saving…</PendingLabel>
              ) : (
                'Save regulation'
              )}
            </button>
            <span className="caption">
              Replaces the whole regulation of the competition.
            </span>
          </div>
          {mutation.isError && <MutationError error={mutation.error} />}
        </form>
      )}
    </section>
  )
}

function StructureSection({
  data,
  canConfigure,
}: {
  data: OrganisationView
  canConfigure: boolean
}) {
  const queryClient = useQueryClient()
  const [format, setFormat] = useState<StructureFormatKind>('Championship')
  const [stageName, setStageName] = useState('')
  const [matchdayCount, setMatchdayCount] = useState(1)
  const [groupCount, setGroupCount] = useState(2)
  const [participantsPerGroup, setParticipantsPerGroup] = useState(2)
  const [bracketSize, setBracketSize] = useState(4)

  const mutation = useMutation({
    mutationFn: () =>
      configureOrganisationStructure(data.competitionId, {
        format,
        stageName: stageName.trim() || null,
        matchdayCount: format === 'Championship' ? matchdayCount : null,
        groupCount: format === 'Groups' ? groupCount : null,
        participantsPerGroup:
          format === 'Groups' ? participantsPerGroup : null,
        bracketSize: format === 'Cup' ? bracketSize : null,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: organisationQueryKey(data.competitionId),
      })
      await queryClient.invalidateQueries({
        queryKey: ['competitions', data.competitionId],
      })
      await queryClient.invalidateQueries({
        queryKey: ['competitions', data.competitionId, 'workspace'],
      })
    },
  })

  const formatKind = data.format.kind
  const primaryStageId = data.format.primaryStageId

  return (
    <section className="card" aria-labelledby="structure-heading">
      <div className="card__head">
        <h2 className="card__title" id="structure-heading">
          Structure
        </h2>
        <p className="card__subtitle">
          {formatKind ? structureFormatKindLabel(formatKind) : data.format.label}
        </p>
      </div>

      <dl className="fact-list">
        <div className="fact">
          <dt className="fact__label">Groups</dt>
          <dd className="fact__value">{data.structure.groupCount}</dd>
        </div>
        <div className="fact">
          <dt className="fact__label">Rounds</dt>
          <dd className="fact__value">{data.structure.roundCount}</dd>
        </div>
        <div className="fact">
          <dt className="fact__label">Matchdays</dt>
          <dd className="fact__value">{data.structure.matchdayCount}</dd>
        </div>
        <div className="fact">
          <dt className="fact__label">Slots</dt>
          <dd className="fact__value">{data.structure.slotCount}</dd>
        </div>
        {data.structure.numberOfPots != null && (
          <div className="fact">
            <dt className="fact__label">Pots</dt>
            <dd className="fact__value">{data.structure.numberOfPots}</dd>
          </div>
        )}
      </dl>

      {primaryStageId && (
        <Link className="row" to={`/stages/${primaryStageId}`}>
          <span className="row__main">
            <span className="row__title">
              {data.format.primaryStageName ?? 'Primary stage'}
            </span>
            <span className="row__meta">Prepare the draw and the matches</span>
          </span>
          <span className="row__aside">
            {data.format.primaryStageStatus && (
              <StageStatusBadge status={data.format.primaryStageStatus} />
            )}
            <span className="row__chevron" aria-hidden="true">
              →
            </span>
          </span>
        </Link>
      )}

      {canConfigure && (
        <form
          className="form"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            if (mutation.isPending) {
              return
            }
            mutation.mutate()
          }}
        >
          <fieldset className="fieldset" disabled={mutation.isPending}>
            <legend className="fieldset__legend">Configure structure</legend>
            <label className="field">
              Format
              <select
                value={format}
                onChange={(event) =>
                  setFormat(event.target.value as StructureFormatKind)
                }
              >
                <option value="Championship">Championship</option>
                <option value="Groups">Groups</option>
                <option value="Cup">Cup</option>
              </select>
            </label>
            <label className="field">
              Stage name (optional)
              <input
                value={stageName}
                onChange={(event) => setStageName(event.target.value)}
                placeholder="e.g. Regular season"
              />
            </label>
            {format === 'Championship' && (
              <label className="field">
                Matchday count
                <input
                  type="number"
                  min={1}
                  value={matchdayCount}
                  onChange={(event) =>
                    setMatchdayCount(Number(event.target.value) || 1)
                  }
                  required
                />
              </label>
            )}
            {format === 'Groups' && (
              <div className="form-row">
                <label className="field">
                  Group count
                  <input
                    type="number"
                    min={1}
                    value={groupCount}
                    onChange={(event) =>
                      setGroupCount(Number(event.target.value) || 1)
                    }
                    required
                  />
                </label>
                <label className="field">
                  Participants per group
                  <input
                    type="number"
                    min={1}
                    value={participantsPerGroup}
                    onChange={(event) =>
                      setParticipantsPerGroup(Number(event.target.value) || 1)
                    }
                    required
                  />
                </label>
              </div>
            )}
            {format === 'Cup' && (
              <label className="field">
                Bracket size (power of two)
                <input
                  type="number"
                  min={2}
                  value={bracketSize}
                  onChange={(event) =>
                    setBracketSize(Number(event.target.value) || 2)
                  }
                  required
                />
              </label>
            )}
          </fieldset>
          <div className="button-row">
            <button
              type="submit"
              className="btn btn--primary"
              disabled={mutation.isPending}
            >
              {mutation.isPending ? (
                <PendingLabel>Configuring…</PendingLabel>
              ) : (
                'Configure structure'
              )}
            </button>
            <span className="caption">
              Rebuilds the stage structure of the competition.
            </span>
          </div>
          {mutation.isError && <MutationError error={mutation.error} />}
        </form>
      )}
    </section>
  )
}
