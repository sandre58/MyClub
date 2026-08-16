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
import {
  BackLink,
  EmptyState,
  ErrorState,
  LoadingState,
  formatError,
} from '../queryUi'
import {
  competitionStatusLabel,
  entryStatusLabel,
  stageStatusLabel,
  structureFormatKindLabel,
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
      <header className="page__header">
        <p className="eyebrow">Organisation</p>
        <h1>{query.data?.name ?? 'Organisation'}</h1>
        {competitionId && (
          <BackLink to={`/competitions/${competitionId}`}>
            ← Back to workspace
          </BackLink>
        )}
      </header>

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <OrganisationViewPanel data={query.data} />}
    </main>
  )
}

function OrganisationViewPanel({ data }: { data: OrganisationView }) {
  const can = (action: string) => data.actions.includes(action)

  return (
    <article className="panel">
      <header className="panel__header">
        <p className="status-line">
          <span className="status-badge status-badge--neutral">
            <span className="status-badge__dot" aria-hidden="true" />
            {competitionStatusLabel[data.status]}
          </span>
        </p>
        <p className="mono muted">{data.competitionId}</p>
      </header>

      <ReadinessSection readiness={data.readiness} />
      <ParticipantsSection
        data={data}
        canAdd={can('AddEntry')}
        canRename={can('RenameEntry')}
        canWithdraw={can('WithdrawEntry')}
        canExclude={can('ExcludeEntry')}
      />
      <RegulationSection
        data={data}
        canReplace={can('ReplaceRegulation')}
      />
      <StructureSection
        data={data}
        canConfigure={can('ConfigureStructure')}
      />
    </article>
  )
}

function ReadinessSection({
  readiness,
}: {
  readiness: OrganisationView['readiness']
}) {
  return (
    <section>
      <h2 className="section-title">Readiness</h2>
      <ul className="plain-list">
        <li>
          Ready for next slice:{' '}
          <span className="muted">
            {readiness.readyForNextSlice ? 'Yes' : 'No'}
          </span>
        </li>
        <li>
          Ready for draw:{' '}
          <span className="muted">
            {readiness.readyForDraw ? 'Yes' : 'No'}
          </span>
        </li>
        <li>
          Attached matches:{' '}
          <span className="muted">{readiness.attachedMatchCount}</span>
        </li>
      </ul>
      {readiness.blockers.length > 0 && (
        <>
          <h3 className="section-title">Blockers</h3>
          <ul className="plain-list">
            {readiness.blockers.map((code) => (
              <li key={code} className="mono muted">
                {code}
              </li>
            ))}
          </ul>
        </>
      )}
      {readiness.hints.length > 0 && (
        <>
          <h3 className="section-title">Hints</h3>
          <ul className="plain-list">
            {readiness.hints.map((hint) => (
              <li key={hint} className="muted">
                {hint}
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
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
    <section>
      <h2 className="section-title">Participants</h2>
      <p className="hint">
        Active {data.participants.activeCount} · Occupying{' '}
        {data.participants.occupyingCount}
      </p>

      {data.participants.entries.length === 0 ? (
        <EmptyState>No entries yet.</EmptyState>
      ) : (
        <ul className="plain-list">
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
          className="org-form"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            if (displayName.trim().length === 0 || addMutation.isPending) {
              return
            }
            addMutation.mutate()
          }}
        >
          <label>
            New entry name
            <input
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              disabled={addMutation.isPending}
              required
            />
          </label>
          <button
            type="submit"
            className="btn"
            disabled={addMutation.isPending || displayName.trim().length === 0}
          >
            {addMutation.isPending ? 'Adding…' : 'Add entry'}
          </button>
          {addMutation.isError && (
            <p className="error" role="alert">
              {formatError(addMutation.error)}
            </p>
          )}
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
    <div className="org-entry">
      <p>
        <strong>{entry.displayName}</strong>{' '}
        <span className="muted">({entryStatusLabel[entry.status]})</span>
      </p>
      {(canRename || canWithdraw || canExclude) && (
        <div className="org-entry__actions">
          {canRename && (
            <form
              className="org-form org-form--inline"
              onSubmit={(event: FormEvent) => {
                event.preventDefault()
                if (name.trim().length === 0 || pending) {
                  return
                }
                renameMutation.mutate()
              }}
            >
              <label>
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
                className="btn"
                disabled={pending || name.trim().length === 0}
              >
                {renameMutation.isPending ? busyLabel : 'Rename'}
              </button>
            </form>
          )}
          {canWithdraw && (
            <button
              type="button"
              className="btn"
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
              {withdrawMutation.isPending ? busyLabel : 'Withdraw'}
            </button>
          )}
          {canExclude && (
            <button
              type="button"
              className="btn"
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
              {excludeMutation.isPending ? busyLabel : 'Exclude'}
            </button>
          )}
        </div>
      )}
      {mutationError && (
        <p className="error" role="alert">
          {formatError(mutationError)}
        </p>
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
    <section>
      <h2 className="section-title">Regulation</h2>
      <ul className="plain-list">
        <li>
          Teams:{' '}
          <span className="muted">
            {regulation.minimumTeams}–{regulation.maximumTeams}
          </span>
        </li>
        <li>
          Match:{' '}
          <span className="muted">
            {regulation.numberOfPeriods}×{regulation.durationPerPeriod}
          </span>
        </li>
        <li>
          Points:{' '}
          <span className="muted">
            W{regulation.winPoints} / D{regulation.drawPoints} / L
            {regulation.lossPoints}
          </span>
        </li>
      </ul>

      {canReplace && (
        <form
          className="org-form"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            if (mutation.isPending) {
              return
            }
            mutation.mutate()
          }}
        >
          <fieldset className="org-form__fieldset" disabled={mutation.isPending}>
            <legend className="org-form__legend">Replace regulation</legend>
            <div className="org-form__row">
              <label>
                Minimum teams
                <input
                  type="number"
                  value={form.minimumTeams}
                  onChange={(event) => setNumber('minimumTeams')(event.target.value)}
                  required
                />
              </label>
              <label>
                Maximum teams
                <input
                  type="number"
                  value={form.maximumTeams}
                  onChange={(event) => setNumber('maximumTeams')(event.target.value)}
                  required
                />
              </label>
            </div>
            <div className="org-form__row">
              <label>
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
              <label>
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
              <label>
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
            <div className="org-form__row">
              <label>
                Win points
                <input
                  type="number"
                  value={form.winPoints}
                  onChange={(event) => setNumber('winPoints')(event.target.value)}
                  required
                />
              </label>
              <label>
                Draw points
                <input
                  type="number"
                  value={form.drawPoints}
                  onChange={(event) => setNumber('drawPoints')(event.target.value)}
                  required
                />
              </label>
              <label>
                Loss points
                <input
                  type="number"
                  value={form.lossPoints}
                  onChange={(event) => setNumber('lossPoints')(event.target.value)}
                  required
                />
              </label>
            </div>
          </fieldset>
          <button type="submit" className="btn" disabled={mutation.isPending}>
            {mutation.isPending ? 'Saving…' : 'Save regulation'}
          </button>
          {mutation.isError && (
            <p className="error" role="alert">
              {formatError(mutation.error)}
            </p>
          )}
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
    <section>
      <h2 className="section-title">Structure</h2>
      <ul className="plain-list">
        <li>
          Format:{' '}
          <span className="muted">
            {formatKind
              ? structureFormatKindLabel[formatKind]
              : data.format.label}
          </span>
        </li>
        <li>
          Groups / rounds / matchdays / slots:{' '}
          <span className="muted">
            {data.structure.groupCount} / {data.structure.roundCount} /{' '}
            {data.structure.matchdayCount} / {data.structure.slotCount}
          </span>
        </li>
        {data.structure.numberOfPots != null && (
          <li>
            Pots:{' '}
            <span className="muted">{data.structure.numberOfPots}</span>
          </li>
        )}
      </ul>

      {primaryStageId && (
        <p>
          <Link to={`/stages/${primaryStageId}`}>
            Open stage{' '}
            {data.format.primaryStageName
              ? `“${data.format.primaryStageName}”`
              : ''}
            {data.format.primaryStageStatus
              ? ` (${stageStatusLabel[data.format.primaryStageStatus]})`
              : ''}{' '}
            →
          </Link>
        </p>
      )}

      {canConfigure && (
        <form
          className="org-form"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            if (mutation.isPending) {
              return
            }
            mutation.mutate()
          }}
        >
          <fieldset className="org-form__fieldset" disabled={mutation.isPending}>
            <legend className="org-form__legend">Configure structure</legend>
            <label>
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
            <label>
              Stage name (optional)
              <input
                value={stageName}
                onChange={(event) => setStageName(event.target.value)}
              />
            </label>
            {format === 'Championship' && (
              <label>
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
              <div className="org-form__row">
                <label>
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
                <label>
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
              <label>
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
          <button type="submit" className="btn" disabled={mutation.isPending}>
            {mutation.isPending ? 'Configuring…' : 'Configure structure'}
          </button>
          {mutation.isError && (
            <p className="error" role="alert">
              {formatError(mutation.error)}
            </p>
          )}
        </form>
      )}
    </section>
  )
}
