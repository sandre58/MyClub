import { labRounds, labStandings } from './labData';
import { Crest, MatchRow, PanelHead } from './LabShared';
import { TrendIcon } from '../design-system/TrendIcon';
import { StandingsLegend } from '../design-system/components/StandingsLegend';

/**
 * Standings — sports consultation. Legitimate grid (gate passed):
 * homogeneous rows, comparable columns, scan. Zones = regulation,
 * colour as confirmation (side bar + legend).
 */
export function LabStandings() {
  const lastDone = [...labRounds].reverse().find((r) => r.state === 'done');

  return (
    <div className="ds-page">
      <div className="ds-page__title-row">
        <h2 className="ds-page__title">Classements</h2>
        <span className="ds-eyebrow">
          Phase principale · Championnat · 3/5 journées
        </span>
      </div>

      <section className="ds-panel">
        <PanelHead title="Classement général" aside="Mis à jour à l’instant" />

        <StandingsLegend
          className="ds-standings-legend--spaced"
          items={[
            {
              label: 'Qualification tournoi régional',
              color: 'var(--color-brand)',
            },
            { label: 'Relégation', color: 'var(--color-error)' },
          ]}
        />

        <div className="ds-standings-wrap">
          <table className="ds-standings">
            <thead>
              <tr>
                <th scope="col" aria-label="Rang" />
                <th scope="col" className="team">
                  Équipe
                </th>
                <th scope="col" aria-label="Tendance" />
                <th scope="col">J</th>
                <th scope="col">G</th>
                <th scope="col">N</th>
                <th scope="col">P</th>
                <th scope="col">Diff</th>
                <th scope="col">Pts</th>
              </tr>
            </thead>
            <tbody>
              {labStandings.map((row) => (
                <tr key={row.team.id} data-zone={row.zone} data-rank={row.rank}>
                  <td className="rank ds-num">{row.rank}</td>
                  <td className="team">
                    <span className="ds-standings__team">
                      <Crest team={row.team} />
                      <span className="ds-standings__team-name">
                        {row.team.name}
                      </span>
                      {row.penalty ? (
                        <span className="ds-standings-penalty">
                          Pénalité −{row.penalty} pt
                        </span>
                      ) : null}
                    </span>
                  </td>
                  <td>
                    <TrendIcon direction={row.trend} />
                  </td>
                  <td className="ds-num">{row.played}</td>
                  <td className="ds-num">{row.won}</td>
                  <td className="ds-num">{row.drawn}</td>
                  <td className="ds-num">{row.lost}</td>
                  <td className="ds-num">
                    {row.goalDiff > 0 ? `+${row.goalDiff}` : row.goalDiff}
                  </td>
                  <td className="pts ds-num ds-num-pts">{row.points}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <div className="ds-grid-2 ds-grid-2--major">
        <section className="ds-panel">
          <PanelHead
            title={
              lastDone
                ? `Dernière journée · ${lastDone.label}`
                : 'Dernière journée'
            }
          />
          <div>
            {lastDone?.matches.map((m) => (
              <MatchRow key={m.id} match={m} />
            ))}
          </div>
          <button type="button" className="ds-btn ds-btn--ghost">
            Voir tous les matchs →
          </button>
        </section>

        <section className="ds-panel">
          <PanelHead title="Règlement du classement" />
          <p className="ds-overview-situation__label">
            Victoire <strong className="ds-num">3 pts</strong> · nul{' '}
            <strong className="ds-num">1 pt</strong> · défaite{' '}
            <strong className="ds-num">0 pt</strong>. Départage : différence de
            buts, puis confrontation directe. Le premier est qualifié pour le
            tournoi régional ; le dernier est relégué.
          </p>
          <button type="button" className="ds-btn ds-btn--ghost">
            Voir le règlement complet →
          </button>
        </section>
      </div>
    </div>
  );
}
