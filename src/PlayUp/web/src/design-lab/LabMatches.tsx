import { labRounds, labStandings } from './labData'
import { OverviewAttentionIcon } from '../design-system/icons/overviewIcons'
import { MatchRow, PanelHead } from './LabShared'

/**
 * Matchs — calendrier sportif par journée. Un seul panneau, journées en
 * sections typographiques (pas de boîtes imbriquées). Gate grille passé.
 */
export function LabMatches() {
  const leader = labStandings[0]

  return (
    <div className="dlab-page">
      <div className="dlab-page__title-row">
        <h2 className="dlab-page__title">Matchs</h2>
        <span className="dlab-eyebrow">
          15 matchs · 5 journées · Phase principale
        </span>
      </div>

      <section className="ds-panel">
        <PanelHead title="Calendrier des matchs" aside="Journée 3 en cours" />
        <div>
          {labRounds.map((round) => (
            <section key={round.id} className="dlab-round">
              <div className="dlab-round__head">
                <span className="dlab-round__label">{round.label}</span>
                <span className="dlab-round__date">{round.date}</span>
                <RoundStatus state={round.state} />
              </div>
              {round.state === 'current' ? (
                <p className="dlab-round__sub">
                  1 terminé · 1 en cours · 1 à venir
                </p>
              ) : null}
              <div>
                {round.matches.map((m) => (
                  <MatchRow key={m.id} match={m} />
                ))}
              </div>
            </section>
          ))}
        </div>
      </section>

      <div className="dlab-grid-2 dlab-grid-2--major">
        <section className="ds-panel">
          <PanelHead title="Résultats à saisir" aside="1" />
          <div className="dlab-attention__row">
            <span className="dlab-attention__count dlab-num">1</span>
            <span className="dlab-attention__icon" aria-hidden="true">
              <OverviewAttentionIcon size="sm" />
            </span>
            <span className="dlab-attention__text">
              <span className="dlab-attention__title">
                Racing Sablons — US Verneuil
              </span>
              <span className="dlab-attention__detail">
                Journée 2 · terminé sans score saisi
              </span>
            </span>
            <button type="button" className="ds-btn ds-btn--primary">
              Saisir
            </button>
          </div>
        </section>

        <section className="ds-panel">
          <PanelHead title="Classement actuel" />
          <p className="dlab-situation__label">
            <strong>{leader.team.name}</strong> en tête ·{' '}
            <span className="dlab-num">{leader.points} pts</span> — avant J3,
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

function RoundStatus({
  state,
}: {
  state: 'done' | 'current' | 'upcoming' | 'partial'
}) {
  switch (state) {
    case 'current':
      return (
        <span className="dlab-status-live">
          <span className="dlab-live-dot" />
          En cours
        </span>
      )
    case 'partial':
      return (
        <span className="ds-status ds-status--context ds-status--rounded ds-status--soft ds-status--tone-attention">
          Partiellement jouée
        </span>
      )
    case 'done':
      return (
        <span className="ds-status ds-status--context ds-status--rounded ds-status--soft ds-status--tone-neutral">
          Terminée
        </span>
      )
    default:
      return (
        <span className="ds-status ds-status--dense ds-status--tone-neutral">
          À venir
        </span>
      )
  }
}
