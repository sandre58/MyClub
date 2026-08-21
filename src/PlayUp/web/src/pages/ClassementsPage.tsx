import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { fetchConsultation } from '../api'
import {
  completionModeLabel,
  competitionStatusLabel,
  structureFormatKindLabel,
} from '../i18n/enumLabels'
import { queryKeys } from '../queryKeys'
import type {
  ConsultationStandingRow,
  ConsultationStandingTable,
  ConsultationStandingsSection,
  ConsultationView,
} from '../types'
import {
  CompetitionStatusBadge,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
} from '../ui'

/**
 * Classements workspace — GET /consultation, display standings only.
 * Domain → Read projects rows; React never recalculates or reorders.
 */
export function ClassementsPage() {
  const { competitionId = '' } = useParams()
  const { t } = useTranslation('classements')

  const query = useQuery({
    queryKey: queryKeys.competitions.consultation(competitionId),
    queryFn: () => fetchConsultation(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('eyebrow')}
        title={t('title')}
        back={
          competitionId
            ? {
                to: `/competitions/${competitionId}`,
                label: t('back'),
              }
            : undefined
        }
        badges={
          query.data && <CompetitionStatusBadge status={query.data.status} />
        }
      />

      {query.isPending && <LoadingState label={t('loading')} />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <ClassementsView data={query.data} />}
    </main>
  )
}

function ClassementsView({ data }: { data: ConsultationView }) {
  return (
    <div className="section-stack">
      <CompetitionContext data={data} />
      <StandingsSection standings={data.standings} />
    </div>
  )
}

function CompetitionContext({ data }: { data: ConsultationView }) {
  const { t } = useTranslation('classements')
  const formatText =
    data.formatKind != null
      ? structureFormatKindLabel(data.formatKind)
      : data.formatLabel

  return (
    <section className="card card--condensed" aria-label={data.name}>
      <div className="card__head">
        <h2 className="card__title">{data.name}</h2>
        <p className="card__subtitle">
          {competitionStatusLabel(data.status)}
          {data.completionMode != null
            ? ` · ${completionModeLabel(data.completionMode)}`
            : null}
          {' · '}
          {t('context.format')}: {formatText}
        </p>
      </div>
    </section>
  )
}

function StandingsSection({
  standings,
}: {
  standings: ConsultationStandingsSection
}) {
  const { t } = useTranslation('classements')

  if (!standings.applicable) {
    const reasonKey = standings.notApplicableReason
    const reason =
      reasonKey === 'CupFormat' || reasonKey === 'NoStructure'
        ? t(`notApplicable.${reasonKey}`)
        : t('notApplicable.unknown')

    return <EmptyState title={t('notApplicable.title')}>{reason}</EmptyState>
  }

  if (standings.tables.length === 0) {
    return <EmptyState title={t('empty')}>{t('emptyHint')}</EmptyState>
  }

  return (
    <div className="section-stack" data-testid="standings-tables">
      {standings.tables.map((table) => (
        <StandingTableBlock key={tableKey(table)} table={table} />
      ))}
    </div>
  )
}

function tableKey(table: ConsultationStandingTable): string {
  return `${table.stageId}:${table.groupId ?? table.scope}`
}

function StandingTableBlock({ table }: { table: ConsultationStandingTable }) {
  const { t } = useTranslation('classements')
  const heading =
    table.scope === 'Group' && table.groupName
      ? t('table.group', { name: table.groupName })
      : table.scope === 'Overall'
        ? t('table.overall')
        : t('table.stage', { name: table.stageName })

  return (
    <section className="card standings-block">
      <div className="card__head">
        <h3 className="card__title">{heading}</h3>
        {table.stageName ? (
          <p className="card__subtitle">{table.stageName}</p>
        ) : null}
      </div>

      {table.rows.length === 0 ? (
        <EmptyState title={t('empty')}>{t('emptyHint')}</EmptyState>
      ) : (
        <div className="standings-table-wrap">
          <table className="standings-table">
            <thead>
              <tr>
                <th scope="col">{t('columns.position')}</th>
                <th scope="col">{t('columns.team')}</th>
                <th scope="col">{t('columns.played')}</th>
                <th scope="col">{t('columns.wins')}</th>
                <th scope="col">{t('columns.draws')}</th>
                <th scope="col">{t('columns.losses')}</th>
                <th scope="col">{t('columns.goalsFor')}</th>
                <th scope="col">{t('columns.goalsAgainst')}</th>
                <th scope="col">{t('columns.goalDifference')}</th>
                <th scope="col">{t('columns.points')}</th>
              </tr>
            </thead>
            <tbody>
              {table.rows.map((row) => (
                <StandingRow key={row.entryId} row={row} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

function StandingRow({ row }: { row: ConsultationStandingRow }) {
  return (
    <tr data-testid={`standing-row-${row.entryId}`}>
      <td className="standings-table__num">{row.position}</td>
      <td>{row.displayName}</td>
      <td className="standings-table__num">{row.played}</td>
      <td className="standings-table__num">{row.wins}</td>
      <td className="standings-table__num">{row.draws}</td>
      <td className="standings-table__num">{row.losses}</td>
      <td className="standings-table__num">{row.goalsFor}</td>
      <td className="standings-table__num">{row.goalsAgainst}</td>
      <td className="standings-table__num">
        {formatSigned(row.goalDifference)}
      </td>
      <td className="standings-table__pts">{row.points}</td>
    </tr>
  )
}

/** Display helper only — does not recompute Diff from BP/BC. */
function formatSigned(value: number): string {
  if (value > 0) {
    return `+${value}`
  }
  return String(value)
}
