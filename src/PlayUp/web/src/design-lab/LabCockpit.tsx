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
import { AttentionGroup } from '../design-system/components/AttentionGroup'
import { AttentionRow } from '../design-system/components/AttentionRow'
import {
  CockpitSituation,
  CockpitSituationNum,
} from '../design-system/components/CockpitSituation'
import { CockpitNextAction } from '../design-system/components/CockpitNextAction'
import { CockpitPodium } from '../design-system/components/CockpitPodium'
import { CycleLine } from '../design-system/components/CycleLine'
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

function CockpitPreparation() {
  return (
    <div className="ds-page">
      <div className="ds-cockpit-hero">
        <CockpitSituation
          id="lab-cockpit-situation"
          title="Où en est-on ?"
          reading={
            <CockpitSituationNum suffix=" / 6 équipes">4</CockpitSituationNum>
          }
          label="La structure est définie. Il manque 2 équipes pour compléter le plateau et générer le calendrier."
          cycle={{ current: 'Préparation' }}
        />

        <CockpitNextAction
          eyebrow="Prochaine action"
          title="Ajouter des équipes"
          why="2 places restantes dans le championnat à 6."
          action={
            <button type="button" className="ds-btn ds-btn--primary">
              Ajouter une équipe
            </button>
          }
        />
      </div>

      <div className="ds-grid-2">
        <section className="ds-panel">
          <PanelHead title="Équipes" aside="4 inscrites" icon={<TeamsIcon />} />
          <table className="ds-cockpit-mini-table">
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
          <p className="ds-cockpit-situation__label">
            Victoire 3 pts · nul 1 pt · départage par différence de buts puis
            confrontation directe.
          </p>
          <div className="ds-empty">
            <span className="ds-empty__title">Aucun match pour l’instant</span>
            <span className="ds-empty__body">
              Les matchs apparaîtront ici dès que le plateau sera complet et le
              calendrier créé. Rien à faire côté règlement.
            </span>
          </div>
        </section>
      </div>
    </div>
  )
}

function CockpitLive() {
  const currentRound = labRounds.find((r) => r.state === 'current')
  const liveMatch = currentRound?.matches.find((m) => m.state === 'live')

  return (
    <div className="ds-page">
      <div className="ds-cockpit-hero">
        <CockpitSituation
          id="lab-cockpit-situation"
          title="Où en est-on ?"
          reading={
            <>
              <CockpitSituationNum suffix=" / 5">J3</CockpitSituationNum>
              {liveMatch ? (
                <span className="ds-status-live">
                  <span className="ds-live-dot" />1 match en cours
                </span>
              ) : null}
            </>
          }
          label="La journée 3 se joue aujourd’hui : 1 match terminé, 1 en cours, 1 à venir. Un résultat de la journée 2 reste à saisir."
          cycle={{
            current: 'En cours',
            done: ['Préparation', 'Calendrier'],
          }}
        />

        <CockpitNextAction
          eyebrow="Prochaine action"
          title="Saisir un résultat"
          why="Racing Sablons — US Verneuil (J2) est terminé sans score."
          action={
            <button type="button" className="ds-btn ds-btn--primary">
              Saisir le résultat
            </button>
          }
        />
      </div>

      <AttentionGroup label="À traiter" count={labAttention.length}>
        {labAttention.map((item) => (
          <AttentionRow
            key={item.id}
            count={item.count}
            tone={item.tone === 'info' ? 'info' : 'attention'}
            icon={
              item.id === 'postponed' ? (
                <CalendarIcon size="sm" />
              ) : (
                <OverviewAttentionIcon size="sm" />
              )
            }
            title={item.title}
            detail={item.detail}
            action={
              <button type="button" className="ds-btn ds-btn--secondary">
                {item.action}
              </button>
            }
          />
        ))}
      </AttentionGroup>

      <div className="ds-grid-2 ds-grid-2--major">
        <section className="ds-panel">
          <PanelHead
            title="Journée 3"
            aside="1 / 3 terminé"
            icon={<CalendarIcon />}
          />
          <div>
            {currentRound?.matches.map((m) => (
              <MatchRow key={m.id} match={m} />
            ))}
          </div>
        </section>

        <section className="ds-panel">
          <PanelHead
            title="Classement"
            aside="Provisoire — avant J3"
            icon={<ClassementsNavIcon />}
          />
          <table className="ds-cockpit-mini-table">
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

function CockpitDone() {
  const podium = [labStandings[1], labStandings[0], labStandings[2]]

  return (
    <div className="ds-page">
      <div className="ds-cockpit-hero">
        <CockpitSituation
          id="lab-cockpit-done"
          title="Compétition terminée"
          reading={
            <span className="ds-cockpit-situation__num">AS Montval</span>
          }
          label="Champion avec 9 points et une différence de buts de +6. 15 matchs joués, aucun forfait."
          cycle={{
            current: 'Terminée',
            done: ['Préparation', 'Calendrier', 'En cours'],
          }}
        />

        <section className="ds-panel">
          <PanelHead title="Podium" />
          <CockpitPodium
            steps={podium.map((row, i) => ({
              rank: ([2, 1, 3] as const)[i],
              name: row.team.name,
            }))}
          />
        </section>
      </div>

      <AttentionGroup label="À traiter">
        <div className="ds-empty" style={{ padding: 'var(--space-8) 0' }}>
          <span className="ds-empty__body">
            Rien à traiter. La compétition est archivable quand vous le
            souhaitez.
          </span>
        </div>
      </AttentionGroup>
    </div>
  )
}

export { CycleLine }
