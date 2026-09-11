import {
  labAttention,
  labRounds,
  labStandings,
  type LabLifecycle,
} from './labData';
import {
  CalendarIcon,
  OverviewAttentionIcon,
  RegulationIcon,
  TeamsIcon,
} from '../design-system/icons/contentIcons';
import { ClassementsNavIcon } from '../design-system/icons/shellIcons';
import { AttentionGroup } from '../design-system/components/AttentionGroup';
import { AttentionRow } from '../design-system/components/AttentionRow';
import {
  OverviewReading,
  OverviewReadingNum,
} from '../design-system/components/OverviewReading';
import { OverviewNextAction } from '../design-system/components/OverviewNextAction';
import { OverviewPodium } from '../design-system/components/OverviewPodium';
import { Crest, MatchRow, PanelHead } from './LabShared';
import { TrendIcon } from '../design-system/TrendIcon';

/**
 * Vue d'ensemble — composition émergente selon le cycle.
 * Intensité B : le caractère vient de l'échelle et de la hiérarchie,
 * pas du remplissage.
 */
export function LabOverview({ lifecycle }: { lifecycle: LabLifecycle }) {
  switch (lifecycle) {
    case 'preparation':
      return <OverviewPreparation />;
    case 'done':
      return <OverviewDone />;
    default:
      return <OverviewLive />;
  }
}

function OverviewPreparation() {
  return (
    <div className="ds-page">
      <div className="ds-overview-hero">
        <OverviewReading
          id="lab-overview-situation"
          title="Où en est-on ?"
          reading={
            <OverviewReadingNum suffix=" / 6 équipes">4</OverviewReadingNum>
          }
          label="La structure est définie. Il manque 2 équipes pour compléter le plateau et générer le calendrier."
          cycle={{ current: 'Préparation' }}
        />

        <OverviewNextAction
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
          <table className="ds-overview-mini-table">
            <tbody>
              {labStandings.slice(0, 4).map((row) => (
                <tr key={row.team.id}>
                  <td>
                    <span className="ds-standings__team">
                      <Crest team={row.team} />
                      <span className="ds-standings__team-name">
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
          <p className="ds-overview-situation__label">
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
  );
}

function OverviewLive() {
  const currentRound = labRounds.find((r) => r.state === 'current');
  const liveMatch = currentRound?.matches.find((m) => m.state === 'live');

  return (
    <div className="ds-page">
      <div className="ds-overview-hero">
        <OverviewReading
          id="lab-overview-situation"
          title="Où en est-on ?"
          reading={
            <>
              <OverviewReadingNum suffix=" / 5">J3</OverviewReadingNum>
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

        <OverviewNextAction
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
          <table className="ds-overview-mini-table">
            <tbody>
              {labStandings.slice(0, 5).map((row) => (
                <tr key={row.team.id}>
                  <td className="num" style={{ width: '1.5rem' }}>
                    {row.rank}
                  </td>
                  <td>
                    <span className="ds-standings__team">
                      <Crest team={row.team} />
                      <span className="ds-standings__team-name">
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
  );
}

function OverviewDone() {
  const podium = [labStandings[1], labStandings[0], labStandings[2]];

  return (
    <div className="ds-page">
      <div className="ds-overview-hero">
        <OverviewReading
          id="lab-overview-done"
          title="Compétition terminée"
          reading={
            <span className="ds-overview-situation__num">AS Montval</span>
          }
          label="Champion avec 9 points et une différence de buts de +6. 15 matchs joués, aucun forfait."
          cycle={{
            current: 'Terminée',
            done: ['Préparation', 'Calendrier', 'En cours'],
          }}
        />

        <section className="ds-panel">
          <PanelHead title="Podium" />
          <OverviewPodium
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
  );
}
