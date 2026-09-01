import {
  labAttention,
  labRounds,
  labStandings,
  type LabLifecycle,
} from './labData'
import {
  CalendarIcon,
  OverviewAttentionIcon,
  RegulationIcon,
  TeamsIcon,
} from '../design-system/icons/overviewIcons'
import { ClassementsNavIcon } from '../design-system/icons/shellIcons'
import { Crest, MatchRow, PanelHead } from './LabShared'
import { TrendIcon } from './LabWordmark'

/**
 * Vue d'ensemble — cockpit unique, composition émergente selon le cycle.
 * Intensité B : le caractère vient de l'échelle et de la hiérarchie,
 * pas du remplissage.
 */
export function LabCockpit({ lifecycle }: { lifecycle: LabLifecycle }) {
  switch (lifecycle) {
    case 'preparation':
      return <CockpitPreparation />
    case 'done':
      return <CockpitDone />
    default:
      return <CockpitLive />
  }
}

/* ------------------------------------------------------------------ */
/* Préparation — état vide assumé + prochaine action                    */
/* ------------------------------------------------------------------ */

function CockpitPreparation() {
  return (
    <div className="dlab-page">
      <div className="dlab-hero">
        <section className="ds-panel dlab-situation">
          <PanelHead title="Où en est-on ?" />
          <div className="dlab-situation__reading">
            <span className="dlab-situation__num dlab-num">
              4<small> / 6 équipes</small>
            </span>
          </div>
          <p className="dlab-situation__label">
            La structure est définie. Il manque 2 équipes pour compléter le
            plateau et générer le calendrier.
          </p>
          <CycleLine current="Préparation" />
        </section>

        <section className="ds-panel dlab-next-action">
          <div className="dlab-next-action__body">
            <span className="dlab-eyebrow">Prochaine action</span>
            <span className="dlab-next-action__title">Ajouter des équipes</span>
            <span className="dlab-next-action__why">
              2 places restantes dans le championnat à 6.
            </span>
          </div>
          <button type="button" className="ds-btn ds-btn--primary">
            Ajouter une équipe
          </button>
        </section>
      </div>

      <div className="dlab-grid-2">
        <section className="ds-panel">
          <PanelHead title="Équipes" aside="4 inscrites" icon={<TeamsIcon />} />
          <table className="dlab-mini-table">
            <tbody>
              {labStandings.slice(0, 4).map((row) => (
                <tr key={row.team.id}>
                  <td>
                    <span className="dlab-standings__team">
                      <Crest team={row.team} />
                      <span className="dlab-standings__team-name">
                        {row.team.name}
                      </span>
                    </span>
                  </td>
                  <td className="num">Effectif : 14</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <section className="ds-panel">
          <PanelHead title="Règlement" aside="Prêt" icon={<RegulationIcon />} />
          <p className="dlab-situation__label">
            Victoire 3 pts · nul 1 pt · départage par différence de buts puis
            confrontation directe.
          </p>
          <div className="dlab-empty">
            <span className="dlab-empty__title">Aucun match pour l’instant</span>
            <span className="dlab-empty__body">
              Les matchs apparaîtront ici dès que le plateau sera complet et le
              calendrier créé. Rien à faire côté règlement.
            </span>
          </div>
        </section>
      </div>
    </div>
  )
}

/* ------------------------------------------------------------------ */
/* En cours — action requise, live, classement compact                  */
/* ------------------------------------------------------------------ */

function CockpitLive() {
  const currentRound = labRounds.find((r) => r.state === 'current')
  const liveMatch = currentRound?.matches.find((m) => m.state === 'live')

  return (
    <div className="dlab-page">
      <div className="dlab-hero">
        <section className="ds-panel dlab-situation">
          <PanelHead title="Où en est-on ?" />
          <div className="dlab-situation__reading">
            <span className="dlab-situation__num dlab-num">
              J3<small> / 5</small>
            </span>
            {liveMatch ? (
              <span className="dlab-status-live">
                <span className="dlab-live-dot" />1 match en cours
              </span>
            ) : null}
          </div>
          <p className="dlab-situation__label">
            La journée 3 se joue aujourd’hui : 1 match terminé, 1 en cours,
            1 à venir. Un résultat de la journée 2 reste à saisir.
          </p>
          <CycleLine current="En cours" done={['Préparation', 'Calendrier']} />
        </section>

        <section className="ds-panel dlab-next-action">
          <div className="dlab-next-action__body">
            <span className="dlab-eyebrow">Prochaine action</span>
            <span className="dlab-next-action__title">Saisir un résultat</span>
            <span className="dlab-next-action__why">
              Racing Sablons — US Verneuil (J2) est terminé sans score.
            </span>
          </div>
          <button type="button" className="ds-btn ds-btn--primary">
            Saisir le résultat
          </button>
        </section>
      </div>

      <section className="ds-group dlab-attention" aria-label="À traiter">
        <span className="dlab-eyebrow">À traiter · 2</span>
        {labAttention.map((item) => (
          <div
            key={item.id}
            className="dlab-attention__row"
            data-tone={item.tone}
          >
            <span className="dlab-attention__count dlab-num">
              {item.count}
            </span>
            <span className="dlab-attention__icon" aria-hidden="true">
              {item.id === 'postponed' ? (
                <CalendarIcon size="sm" />
              ) : (
                <OverviewAttentionIcon size="sm" />
              )}
            </span>
            <span className="dlab-attention__text">
              <span className="dlab-attention__title">{item.title}</span>
              <span className="dlab-attention__detail">{item.detail}</span>
            </span>
            <button type="button" className="ds-btn ds-btn--secondary">
              {item.action}
            </button>
          </div>
        ))}
      </section>

      <div className="dlab-grid-2 dlab-grid-2--major">
        <section className="ds-panel">
          <PanelHead
            title="Journée 3"
            aside="1 / 3 terminé"
            icon={<CalendarIcon />}
          />
          <div>
            {currentRound?.matches.map((m) => <MatchRow key={m.id} match={m} />)}
          </div>
        </section>

        <section className="ds-panel">
          <PanelHead
            title="Classement"
            aside="Provisoire — avant J3"
            icon={<ClassementsNavIcon />}
          />
          <table className="dlab-mini-table">
            <tbody>
              {labStandings.slice(0, 5).map((row) => (
                <tr key={row.team.id}>
                  <td className="num" style={{ width: '1.5rem' }}>
                    {row.rank}
                  </td>
                  <td>
                    <span className="dlab-standings__team">
                      <Crest team={row.team} />
                      <span className="dlab-standings__team-name">
                        {row.team.name}
                      </span>
                    </span>
                  </td>
                  <td className="num">
                    <TrendIcon direction={row.trend} />
                  </td>
                  <td className="num">{row.played} j.</td>
                  <td className="num pts">{row.points}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      </div>
    </div>
  )
}

/* ------------------------------------------------------------------ */
/* Terminée — podium + lecture                                          */
/* ------------------------------------------------------------------ */

function CockpitDone() {
  const podium = [labStandings[1], labStandings[0], labStandings[2]]

  return (
    <div className="dlab-page">
      <div className="dlab-hero">
        <section className="ds-panel dlab-situation">
          <PanelHead title="Compétition terminée" />
          <div className="dlab-situation__reading">
            <span className="dlab-situation__num">AS Montval</span>
          </div>
          <p className="dlab-situation__label">
            Champion avec 9 points et une différence de buts de +6. 15 matchs
            joués, aucun forfait.
          </p>
          <CycleLine
            current="Terminée"
            done={['Préparation', 'Calendrier', 'En cours']}
          />
        </section>

        <section className="ds-panel">
          <PanelHead title="Podium" />
          <div className="dlab-podium">
            {podium.map((row, i) => (
              <div
                key={row.team.id}
                className="dlab-podium__step"
                data-rank={[2, 1, 3][i]}
              >
                <div className="dlab-podium__block dlab-num">
                  {[2, 1, 3][i]}
                </div>
                <span className="dlab-podium__name">{row.team.name}</span>
              </div>
            ))}
          </div>
        </section>
      </div>

      <section className="ds-group dlab-attention" aria-label="À traiter">
        <span className="dlab-eyebrow">À traiter</span>
        <div className="dlab-empty" style={{ padding: 'var(--space-8) 0' }}>
          <span className="dlab-empty__body">
            Rien à traiter. La compétition est archivable quand vous le
            souhaitez.
          </span>
        </div>
      </section>
    </div>
  )
}

/* ------------------------------------------------------------------ */

function CycleLine({
  current,
  done = [],
}: {
  current: string
  done?: string[]
}) {
  const steps = ['Préparation', 'Calendrier', 'En cours', 'Terminée']
  return (
    <div className="dlab-cycle" aria-label="Cycle de vie">
      {steps.map((step) => (
        <span
          key={step}
          data-state={
            step === current ? 'current' : done.includes(step) ? 'done' : 'todo'
          }
        >
          {step}
        </span>
      ))}
    </div>
  )
}
