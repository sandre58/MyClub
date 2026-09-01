import { labRounds, labStandings } from './labData'
import { Crest, MatchRow, PanelHead } from './LabShared'
import { TrendIcon } from './LabWordmark'

/**
 * Classements — consultation sportive. Grille légitime (gate passé) :
 * lignes homogènes, colonnes comparables, scan. Zones = règlement,
 * couleur en confirmation (barre latérale + légende).
 */
export function LabStandings() {
  const lastDone = [...labRounds].reverse().find((r) => r.state === 'done')

  return (
    <div className="dlab-page">
      <div className="dlab-page__title-row">
        <h2 className="dlab-page__title">Classements</h2>
        <span className="dlab-eyebrow">
          Phase principale · Championnat · 3/5 journées
        </span>
      </div>

      <section className="ds-panel">
        <PanelHead title="Classement général" aside="Mis à jour à l’instant" />

        <div className="dlab-legend" style={{ marginBottom: 'var(--space-12)' }}>
          <span className="dlab-legend__item">
            <span
              className="dlab-legend__swatch"
              style={{ background: 'var(--color-brand)' }}
            />
            Qualification tournoi régional
          </span>
          <span className="dlab-legend__item">
            <span
              className="dlab-legend__swatch"
              style={{ background: 'var(--color-error)' }}
            />
            Relégation
          </span>
        </div>

        <table className="dlab-standings">
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
                <td className="rank dlab-num">{row.rank}</td>
                <td className="team">
                  <span className="dlab-standings__team">
                    <Crest team={row.team} />
                    <span className="dlab-standings__team-name">
                      {row.team.name}
                    </span>
                    {row.penalty ? (
                      <span className="dlab-penalty">
                        Pénalité −{row.penalty} pt
                      </span>
                    ) : null}
                  </span>
                </td>
                <td>
                  <TrendIcon direction={row.trend} />
                </td>
                <td>{row.played}</td>
                <td>{row.won}</td>
                <td>{row.drawn}</td>
                <td>{row.lost}</td>
                <td>
                  {row.goalDiff > 0 ? `+${row.goalDiff}` : row.goalDiff}
                </td>
                <td className="pts">{row.points}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <div className="dlab-grid-2 dlab-grid-2--major">
        <section className="ds-panel">
          <PanelHead
            title={lastDone ? `Dernière journée · ${lastDone.label}` : 'Dernière journée'}
          />
          <div>
            {lastDone?.matches.map((m) => <MatchRow key={m.id} match={m} />)}
          </div>
          <button type="button" className="ds-btn ds-btn--ghost">
            Voir tous les matchs →
          </button>
        </section>

        <section className="ds-panel">
          <PanelHead title="Règlement du classement" />
          <p className="dlab-situation__label">
            Victoire <strong className="dlab-num">3 pts</strong> · nul{' '}
            <strong className="dlab-num">1 pt</strong> · défaite{' '}
            <strong className="dlab-num">0 pt</strong>. Départage : différence
            de buts, puis confrontation directe. Le premier est qualifié pour
            le tournoi régional ; le dernier est relégué.
          </p>
          <button type="button" className="ds-btn ds-btn--ghost">
            Voir le règlement complet →
          </button>
        </section>
      </div>
    </div>
  )
}
