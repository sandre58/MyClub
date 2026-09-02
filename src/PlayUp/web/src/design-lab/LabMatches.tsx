import { labRounds, labStandings } from './labData'
import { OverviewAttentionIcon } from '../design-system/icons/overviewIcons'
import { AttentionRow } from '../design-system/components/AttentionRow'
import { MatchRound } from '../design-system/components/MatchRound'
import { MatchRoundStatus } from '../design-system/components/MatchRoundStatus'
import { MatchRow, PanelHead } from './LabShared'

/**
 * Matchs — calendrier sportif par journée. Un seul panneau, journées en
 * sections typographiques (pas de boîtes imbriquées). Gate grille passé.
 */
export function LabMatches() {
  const leader = labStandings[0]

  return (
    <div className="ds-page">
      <div className="ds-page__title-row">
        <h2 className="ds-page__title">Matchs</h2>
        <span className="ds-eyebrow">
          15 matchs · 5 journées · Phase principale
        </span>
      </div>

      <section className="ds-panel">
        <PanelHead title="Calendrier des matchs" aside="Journée 3 en cours" />
        <div>
          {labRounds.map((round) => (
            <MatchRound
              key={round.id}
              label={round.label}
              date={round.date}
              status={
                <MatchRoundStatus
                  state={round.state}
                  labels={{
                    current: 'En cours',
                    partial: 'Partiellement jouée',
                    done: 'Terminée',
                    upcoming: 'À venir',
                  }}
                />
              }
              sub={
                round.state === 'current'
                  ? '1 terminé · 1 en cours · 1 à venir'
                  : undefined
              }
            >
              {round.matches.map((m) => (
                <MatchRow key={m.id} match={m} />
              ))}
            </MatchRound>
          ))}
        </div>
      </section>

      <div className="ds-grid-2 ds-grid-2--major">
        <section className="ds-panel">
          <PanelHead title="Résultats à saisir" aside="1" />
          <AttentionRow
            count={1}
            icon={<OverviewAttentionIcon size="sm" />}
            title="Racing Sablons — US Verneuil"
            detail="Journée 2 · terminé sans score saisi"
            action={
              <button type="button" className="ds-btn ds-btn--primary">
                Saisir
              </button>
            }
          />
        </section>

        <section className="ds-panel">
          <PanelHead title="Classement actuel" />
          <p className="ds-overview-situation__label">
            <strong>{leader.team.name}</strong> en tête ·{' '}
            <span className="ds-num">{leader.points} pts</span> — avant J3,
            provisoire.
          </p>
          <button type="button" className="ds-btn ds-btn--ghost">
            Voir le classement →
          </button>
        </section>
      </div>
    </div>
  )
}
