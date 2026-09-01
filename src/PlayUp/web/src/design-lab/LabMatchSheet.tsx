import { labMatchEvents, labRounds, type LabLifecycle } from './labData'
import { ClockIcon, PinIcon } from './LabIcons'
import { CalendarIcon } from '../design-system/icons/overviewIcons'
import { Crest, PanelHead } from './LabShared'

/**
 * Fiche match — la surface la plus sportive du produit.
 *
 * Match Hero (hypothèse C, tranchée) : un grand en-tête d'objet qui porte
 * tout ce qui identifie le match — équipes, score géant, état, méta — et
 * dont le centre change avec l'état :
 *   Scheduled : l'heure du coup d'envoi est le héros, action « Démarrer »
 *   Live      : le score est le héros, saisie collée au score
 *   Finished  : le score final est le héros, action de correction en retrait
 */
export function LabMatchSheet({ lifecycle }: { lifecycle: LabLifecycle }) {
  const match = labRounds
    .flatMap((r) => r.matches)
    .find((m) => m.id === 'j3m2')

  if (!match) return null

  const heroState: 'scheduled' | 'live' | 'finished' =
    lifecycle === 'preparation'
      ? 'scheduled'
      : lifecycle === 'done'
        ? 'finished'
        : 'live'

  return (
    <div className="dlab-page">
      <div className="dlab-page__title-row">
        <h2 className="dlab-page__title">
          {match.home.name} — {match.away.name}
        </h2>
      </div>

      <section className="ds-panel dlab-match-hero">
        <div className="dlab-match-hero__top">
          <span className="dlab-eyebrow">Journée 3 · Phase principale</span>
          <HeroStatus state={heroState} minute={match.minute} />
        </div>

        <div className="dlab-match-hero__stage">
          <div className="dlab-match-hero__team">
            <Crest team={match.home} />
            <span className="dlab-match-hero__name">{match.home.name}</span>
            {heroState === 'live' ? (
              <div className="dlab-score-actions">
                <button type="button" className="dlab-score-btn" aria-label="But domicile">
                  +
                </button>
                <button type="button" className="dlab-score-btn" aria-label="Retirer un but domicile">
                  −
                </button>
              </div>
            ) : null}
          </div>

          <div className="dlab-match-hero__center">
            {heroState === 'scheduled' ? (
              <>
                <span className="dlab-match-hero__score dlab-num" data-pending="true">
                  15:00
                </span>
                <button type="button" className="ds-btn ds-btn--primary">
                  Démarrer le match
                </button>
              </>
            ) : (
              <>
                <span className="dlab-match-hero__score dlab-num">
                  {match.homeScore}
                  <span className="dlab-match-hero__sep">–</span>
                  {match.awayScore}
                </span>
                {heroState === 'live' ? (
                  <button type="button" className="ds-btn ds-btn--primary">
                    Terminer le match
                  </button>
                ) : (
                  <button type="button" className="ds-btn ds-btn--ghost">
                    Corriger le résultat
                  </button>
                )}
              </>
            )}
          </div>

          <div className="dlab-match-hero__team">
            <Crest team={match.away} />
            <span className="dlab-match-hero__name">{match.away.name}</span>
            {heroState === 'live' ? (
              <div className="dlab-score-actions">
                <button type="button" className="dlab-score-btn" aria-label="But extérieur">
                  +
                </button>
                <button type="button" className="dlab-score-btn" aria-label="Retirer un but extérieur">
                  −
                </button>
              </div>
            ) : null}
          </div>
        </div>

        <div className="dlab-match-hero__meta">
          <span className="dlab-match-hero__meta-item">
            <CalendarIcon size="sm" />
            Sam. 26 sept. 2026
          </span>
          <span className="dlab-match-hero__meta-item">
            <ClockIcon size="sm" />
            15:00
          </span>
          <span className="dlab-match-hero__meta-item">
            <PinIcon size="sm" />
            Stade des Chênes
          </span>
          <span className="dlab-match-hero__meta-item">
            Arbitre : M. Charpin
          </span>
        </div>
      </section>

      {heroState === 'scheduled' ? (
        <ScheduledBody />
      ) : (
        <FactsBody finished={heroState === 'finished'} />
      )}
    </div>
  )
}

function HeroStatus({
  state,
  minute,
}: {
  state: 'scheduled' | 'live' | 'finished'
  minute?: string
}) {
  if (state === 'live') {
    return (
      <span className="dlab-status-live">
        <span className="dlab-live-dot" />
        {minute} · En cours
      </span>
    )
  }
  if (state === 'finished') {
    return (
      <span className="ds-status ds-status--context ds-status--rounded ds-status--soft ds-status--tone-neutral">
        Terminé · Résultat officiel
      </span>
    )
  }
  return (
    <span className="ds-status ds-status--context ds-status--rounded ds-status--soft ds-status--tone-info">
      À venir
    </span>
  )
}

function ScheduledBody() {
  return (
    <div className="dlab-grid-2--major dlab-grid-2">
      <section className="ds-panel">
        <PanelHead title="Feuille de match" aside="À compléter" />
        <p className="dlab-situation__label">
          Les compositions peuvent être saisies dès maintenant : 11 titulaires
          et jusqu'à 3 remplaçants par équipe.
        </p>
        <button type="button" className="ds-btn ds-btn--secondary">
          Préparer la feuille de match
        </button>
      </section>

      <section className="ds-panel">
        <PanelHead title="Faits de match" />
        <div className="dlab-empty">
          <span className="dlab-empty__title">Le match n'a pas commencé</span>
          <span className="dlab-empty__body">
            Buts, cartons et remplacements se saisiront ici pendant la
            rencontre.
          </span>
        </div>
      </section>
    </div>
  )
}

function FactsBody({ finished }: { finished: boolean }) {
  return (
    <div className="dlab-grid-2--major dlab-grid-2">
      <section className="ds-panel">
        <PanelHead
          title="Faits de match"
          aside={`${labMatchEvents.length} événements`}
        />
        <div className="dlab-timeline">
          {labMatchEvents.map((event, i) => (
            <div key={i} className="dlab-timeline__row" data-kind={event.kind}>
              <span className="dlab-timeline__minute dlab-num">
                {event.minute}
              </span>
              <span>{event.text}</span>
            </div>
          ))}
        </div>
        {finished ? null : (
          <button type="button" className="ds-btn ds-btn--secondary">
            Ajouter un fait de match
          </button>
        )}
      </section>

      <section className="ds-panel">
        <PanelHead title="Feuille de match" aside="Complète" />
        <p className="dlab-situation__label">
          11 titulaires et 3 remplaçants déclarés de chaque côté. Capitaines :
          K. Ferrand (ROC), A. Meunier (VER).
        </p>
        <button type="button" className="ds-btn ds-btn--ghost">
          Voir la feuille complète →
        </button>
        {finished ? (
          <div className="dlab-empty">
            <span className="dlab-empty__title">Résultat intégré</span>
            <span className="dlab-empty__body">
              Le classement est à jour. Prochaine action : préparer la
              journée 4.
            </span>
          </div>
        ) : (
          <div className="dlab-empty">
            <span className="dlab-empty__title">Et après ?</span>
            <span className="dlab-empty__body">
              À la fin du match, le résultat alimente directement le classement
              — la prochaine action vous sera proposée ici même.
            </span>
          </div>
        )}
      </section>
    </div>
  )
}
