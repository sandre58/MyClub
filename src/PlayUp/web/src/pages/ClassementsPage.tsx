import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { fetchConsultation, fetchStructureView } from '../api';
import { MatchRow, MatchRowScore } from '../design-system/components/MatchRow';
import { TextLink } from '../design-system/components/TextLink';
import { PageHead } from '../design-system/components/PageHead';
import { PanelHead } from '../design-system/components/PanelHead';
import { TeamCrest } from '../design-system/TeamCrest';
import { RegulationIcon } from '../design-system/icons/overviewIcons';
import {
  ClassementsNavIcon,
  MatchesNavIcon,
} from '../design-system/icons/shellIcons';
import {
  completionModeLabel,
  competitionStatusLabel,
  structureFormatKindLabel,
} from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import type {
  ConsultationResult,
  ConsultationStandingRow,
  ConsultationStandingTable,
  ConsultationStandingsSection,
  ConsultationView,
  StructureRegulationSummary,
} from '../types';
import { EmptyState, ErrorState, LoadingState } from '../ui';
import './classements.css';

/**
 * Classements workspace — GET /consultation (+ structure for standing barème).
 * Presents Read facts only. Never recalculates rank or invents points.
 */
export function ClassementsPage() {
  const { competitionId = '' } = useParams();
  const { t } = useTranslation('classements');

  const query = useQuery({
    queryKey: queryKeys.competitions.consultation(competitionId),
    queryFn: () => fetchConsultation(competitionId),
    enabled: competitionId.length > 0,
  });

  return (
    <main id="main" className="page page--classements">
      {query.isPending && !query.data && <LoadingState label={t('loading')} />}
      {query.isError && !query.data && <ErrorState error={query.error} />}
      {query.data && <ClassementsView data={query.data} />}
    </main>
  );
}

function ClassementsView({ data }: { data: ConsultationView }) {
  const { t } = useTranslation('classements');
  const structureHref = `/competitions/${data.competitionId}/structure`;

  const orgQuery = useQuery({
    queryKey: queryKeys.competitions.structure(data.competitionId),
    queryFn: () => fetchStructureView(data.competitionId),
  });

  return (
    <div className="ds-page classements">
      <PageHead title={t('title')} />

      <ContextBand data={data} />

      <StandingsSection standings={data.standings} />

      <div className="ds-grid-2 ds-grid-2--major classements__bottom">
        <LastMatchdayPanel
          results={data.results}
          matchesHref={`/competitions/${data.competitionId}/matches`}
        />
        <RegulationPanel
          href={structureHref}
          regulation={orgQuery.data?.regulation ?? null}
          loading={orgQuery.isPending}
        />
      </div>
    </div>
  );
}

function ContextBand({ data }: { data: ConsultationView }) {
  const { t } = useTranslation('classements');
  const formatText =
    data.formatKind != null
      ? structureFormatKindLabel(data.formatKind)
      : data.formatLabel;

  return (
    <ul className="classements-band" aria-label={data.name}>
      <li className="classements-band__chip">{data.name}</li>
      <li className="classements-band__chip classements-band__chip--status">
        {competitionStatusLabel(data.status)}
      </li>
      <li className="classements-band__chip classements-band__chip--muted">
        {t('context.format')}: {formatText}
      </li>
      {data.completionMode != null ? (
        <li className="classements-band__chip classements-band__chip--muted">
          {completionModeLabel(data.completionMode)}
        </li>
      ) : null}
    </ul>
  );
}

function StandingsSection({
  standings,
}: {
  standings: ConsultationStandingsSection;
}) {
  const { t } = useTranslation('classements');

  if (!standings.applicable) {
    const reasonKey = standings.notApplicableReason;
    const reason =
      reasonKey === 'CupFormat' || reasonKey === 'NoStructure'
        ? t(`notApplicable.${reasonKey}`)
        : t('notApplicable.unknown');

    return (
      <section className="ds-panel" aria-labelledby="classements-na">
        <PanelHead
          id="classements-na"
          title={t('notApplicable.title')}
          icon={<ClassementsNavIcon size="md" />}
        />
        <EmptyState>{reason}</EmptyState>
      </section>
    );
  }

  if (standings.tables.length === 0) {
    return (
      <section className="ds-panel" aria-labelledby="classements-empty">
        <PanelHead
          id="classements-empty"
          title={t('table.overall')}
          icon={<ClassementsNavIcon size="md" />}
        />
        <EmptyState title={t('empty')}>{t('emptyHint')}</EmptyState>
      </section>
    );
  }

  return (
    <div className="classements__tables" data-testid="standings-tables">
      {standings.tables.map((table) => (
        <StandingTableBlock key={tableKey(table)} table={table} />
      ))}
    </div>
  );
}

function tableKey(table: ConsultationStandingTable): string {
  return `${table.stageId}:${table.groupId ?? table.scope}`;
}

function StandingTableBlock({ table }: { table: ConsultationStandingTable }) {
  const { t } = useTranslation('classements');
  const heading =
    table.scope === 'Group' && table.groupName
      ? t('table.group', { name: table.groupName })
      : table.scope === 'Overall'
        ? t('table.overall')
        : t('table.stage', { name: table.stageName });

  return (
    <section
      className="ds-panel"
      aria-labelledby={`standings-${tableKey(table)}`}
    >
      <PanelHead
        id={`standings-${tableKey(table)}`}
        title={heading}
        icon={<ClassementsNavIcon size="md" />}
      />
      {table.stageName && table.scope === 'Group' ? (
        <p className="classements-panel__muted">{table.stageName}</p>
      ) : null}

      {table.rows.length === 0 ? (
        <EmptyState title={t('empty')}>{t('emptyHint')}</EmptyState>
      ) : (
        <div className="ds-standings-wrap">
          <table className="ds-standings">
            <thead>
              <tr>
                <th scope="col" aria-label={t('columns.position')} />
                <th scope="col" className="team">
                  {t('columns.team')}
                </th>
                <th scope="col" className="ds-num">
                  {t('columns.played')}
                </th>
                <th scope="col" className="ds-num">
                  {t('columns.wins')}
                </th>
                <th scope="col" className="ds-num">
                  {t('columns.draws')}
                </th>
                <th scope="col" className="ds-num">
                  {t('columns.losses')}
                </th>
                <th scope="col" className="ds-num">
                  {t('columns.goalsFor')}
                </th>
                <th scope="col" className="ds-num">
                  {t('columns.goalsAgainst')}
                </th>
                <th scope="col" className="ds-num">
                  {t('columns.goalDifference')}
                </th>
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
  );
}

function StandingRow({ row }: { row: ConsultationStandingRow }) {
  return (
    <tr data-rank={row.position} data-testid={`standing-row-${row.entryId}`}>
      <td className="rank ds-num">{row.position}</td>
      <td className="team">
        <span className="ds-standings__team">
          <span className="ds-standings__team-name">{row.displayName}</span>
        </span>
      </td>
      <td className="ds-num">{row.played}</td>
      <td className="ds-num">{row.wins}</td>
      <td className="ds-num">{row.draws}</td>
      <td className="ds-num">{row.losses}</td>
      <td className="ds-num">{row.goalsFor}</td>
      <td className="ds-num">{row.goalsAgainst}</td>
      <td className="ds-num">{formatSigned(row.goalDifference)}</td>
      <td className="pts ds-num ds-num-pts">{row.points}</td>
    </tr>
  );
}

function LastMatchdayPanel({
  results,
  matchesHref,
}: {
  results: ConsultationResult[];
  matchesHref: string;
}) {
  const { t } = useTranslation('classements');
  const slice = selectLastMatchday(results);
  const heading =
    slice == null
      ? t('lastMatchday.title')
      : (slice.contextLabel ??
        (slice.matchdayNumber != null
          ? t('lastMatchday.matchday', { n: slice.matchdayNumber })
          : t('lastMatchday.title')));

  return (
    <section className="ds-panel" aria-labelledby="classements-last-matchday">
      <PanelHead
        id="classements-last-matchday"
        title={heading}
        icon={<MatchesNavIcon size="md" />}
      />

      {!slice || slice.matches.length === 0 ? (
        <EmptyState>{t('lastMatchday.empty')}</EmptyState>
      ) : (
        <div>
          {slice.matches.map((match) => (
            <ConsultationMatchRow key={match.matchId} match={match} />
          ))}
        </div>
      )}

      <div className="classements-panel__footer classements-panel__footer--start">
        <TextLink to={matchesHref}>{t('lastMatchday.openMatches')}</TextLink>
      </div>
    </section>
  );
}

function ConsultationMatchRow({ match }: { match: ConsultationResult }) {
  const { t } = useTranslation('classements');
  const homeName = match.home.displayName?.trim() || '—';
  const awayName = match.away.displayName?.trim() || '—';
  const note =
    match.resultType === 'Forfeit' || match.resultType === 'WalkOver'
      ? t(`lastMatchday.resultType.${match.resultType}`)
      : null;
  const scoreLabel =
    match.score != null
      ? `${match.score.homeGoals}–${match.score.awayGoals}`
      : t('lastMatchday.pending');

  const score =
    match.score != null ? (
      <MatchRowScore
        home={match.score.homeGoals}
        away={match.score.awayGoals}
      />
    ) : (
      <MatchRowScore home={t('lastMatchday.pending')} away="" muted />
    );

  return (
    <MatchRow
      to={`/matches/${match.matchId}`}
      ariaLabel={`${homeName} – ${awayName}, ${scoreLabel}`}
      scoreMuted={match.score == null}
      home={{
        name: homeName,
        crest: (
          <TeamCrest
            name={homeName}
            logoMediaId={match.home.logoMediaId}
            primaryColor={match.home.primaryColor}
            size="sm"
          />
        ),
      }}
      away={{
        name: awayName,
        crest: (
          <TeamCrest
            name={awayName}
            logoMediaId={match.away.logoMediaId}
            primaryColor={match.away.primaryColor}
            size="sm"
          />
        ),
      }}
      score={score}
      aside={note ? <span className="ds-match-row__note">{note}</span> : null}
    />
  );
}

function RegulationPanel({
  href,
  regulation,
  loading,
}: {
  href: string;
  regulation: StructureRegulationSummary | null;
  loading: boolean;
}) {
  const { t } = useTranslation('classements');

  return (
    <section className="ds-panel" aria-labelledby="classements-regulation">
      <PanelHead
        id="classements-regulation"
        title={t('regulation.title')}
        icon={<RegulationIcon size="md" />}
      />

      <p className="classements-panel__lede">{t('regulation.lede')}</p>

      {regulation ? (
        <>
          <p className="classements-panel__section-label">
            {t('regulation.pointsHeading')}
          </p>
          <ul className="classements-chips">
            <PointsChip
              tone="win"
              value={regulation.winPoints}
              label={t('regulation.pointsWin')}
            />
            <PointsChip
              tone="draw"
              value={regulation.drawPoints}
              label={t('regulation.pointsDraw')}
            />
            <PointsChip
              tone="loss"
              value={regulation.lossPoints}
              label={t('regulation.pointsLoss')}
            />
          </ul>
          <dl className="classements-facts">
            <div className="classements-fact">
              <dt>{t('regulation.match')}</dt>
              <dd>
                {t('regulation.matchValue', {
                  periods: regulation.numberOfPeriods,
                  duration: regulation.durationPerPeriod,
                })}
              </dd>
            </div>
            <div className="classements-fact">
              <dt>{t('regulation.teams')}</dt>
              <dd>
                {t('regulation.teamsValue', {
                  min: regulation.minimumTeams,
                  max: regulation.maximumTeams,
                })}
              </dd>
            </div>
          </dl>
          <p className="classements-panel__muted">
            {t('regulation.rankingHint')}
          </p>
        </>
      ) : loading ? (
        <p className="classements-panel__muted" role="status">
          {t('regulation.loading')}
        </p>
      ) : (
        <p className="classements-panel__muted">
          {t('regulation.unavailable')}
        </p>
      )}

      <div className="classements-panel__footer">
        <TextLink to={href}>{t('regulation.openStructure')}</TextLink>
      </div>
    </section>
  );
}

function PointsChip({
  value,
  label,
  tone,
}: {
  value: number;
  label: string;
  tone: 'win' | 'draw' | 'loss';
}) {
  const { t } = useTranslation('classements');

  return (
    <li className={`classements-chip classements-chip--${tone}`}>
      <span className="classements-chip__value">
        {t('regulation.pointsValue', { value })}
      </span>
      <span className="classements-chip__label">{label}</span>
    </li>
  );
}

/**
 * Last matchday slice — presentation only.
 * Uses the highest matchdayNumber from the Read payload when present;
 * otherwise groups by the last contextLabel in Read order.
 */
function selectLastMatchday(results: ConsultationResult[]): {
  matchdayNumber: number | null;
  contextLabel: string | null;
  matches: ConsultationResult[];
} | null {
  if (results.length === 0) {
    return null;
  }

  const numbered = results.filter((r) => r.matchdayNumber != null);
  if (numbered.length > 0) {
    const maxDay = Math.max(...numbered.map((r) => r.matchdayNumber as number));
    const matches = results.filter((r) => r.matchdayNumber === maxDay);
    return {
      matchdayNumber: maxDay,
      contextLabel: matches.find((m) => m.contextLabel)?.contextLabel ?? null,
      matches,
    };
  }

  const last = results[results.length - 1];
  const label = last.contextLabel;
  if (label) {
    return {
      matchdayNumber: null,
      contextLabel: label,
      matches: results.filter((r) => r.contextLabel === label),
    };
  }

  return {
    matchdayNumber: null,
    contextLabel: null,
    matches: results,
  };
}

/** Display helper only — does not recompute Diff from BP/BC. */
function formatSigned(value: number): string {
  if (value > 0) {
    return `+${value}`;
  }
  return String(value);
}
