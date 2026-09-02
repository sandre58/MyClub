import { labMatchEvents, labRounds, type LabLifecycle } from './labData'
import { ClockIcon, PinIcon } from '../design-system/icons/metaIcons'
import { CalendarIcon } from '../design-system/icons/overviewIcons'
import {
  MatchHero,
  MatchHeroMetaItem,
  MatchHeroScore,
} from '../design-system/components/MatchHero'
import { Status } from '../design-system/components/Status'
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
    <div className="ds-page">
      <div className="ds-page__title-row">
        <h2 className="ds-page__title">
          {match.home.name} — {match.away.name}
        </h2>
      </div>

      <MatchHero
        eyebrow="Journée 3 · Phase principale"
        status={<HeroStatus state={heroState} minute={match.minute} />}
        home={{
          name: match.home.name,
          crest: <Crest team={match.home} />,
          scoreActions:
            heroState === 'live' ? (
              <div className="ds-match-hero__score-actions">
                <button type="button" className="ds-match-hero__score-btn" aria-label="But domicile">
                  +
                </button>
                <button type="button" className="ds-match-hero__score-btn" aria-label="Retirer un but domicile">
                  −
                </button>
              </div>
            ) : undefined,
        }}
        away={{
          name: match.away.name,
          crest: <Crest team={match.away} />,
          scoreActions:
            heroState === 'live' ? (
              <div className="ds-match-hero__score-actions">
                <button type="button" className="ds-match-hero__score-btn" aria-label="But extérieur">
                  +
                </button>
                <button type="button" className="ds-match-hero__score-btn" aria-label="Retirer un but extérieur">
                  −
                </button>
              </div>
            ) : undefined,
        }}
        center={
          heroState === 'scheduled' ? (
            <>
              <MatchHeroScore pending>15:00</MatchHeroScore>
              <button type="button" className="ds-btn ds-btn--primary">
                Démarrer le match
              </button>
            </>
          ) : (
            <>
              <MatchHeroScore>
                {match.homeScore}
                <span className="ds-match-hero__sep" aria-hidden="true">
                  –
                </span>
                {match.awayScore}
              </MatchHeroScore>
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
          )
        }
        meta={
          <>
            <MatchHeroMetaItem icon={<CalendarIcon size="sm" />}>
              Sam. 26 sept. 2026
            </MatchHeroMetaItem>
            <MatchHeroMetaItem icon={<ClockIcon size="sm" />}>15:00</MatchHeroMetaItem>
            <MatchHeroMetaItem icon={<PinIcon size="sm" />}>Stade des Chênes</MatchHeroMetaItem>
            <MatchHeroMetaItem>Arbitre : M. Charpin</MatchHeroMetaItem>
          </>
        }
      />

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
      <span className="ds-status-live">
        <span className="ds-live-dot" />
        {minute} · En cours
      </span>
    )
  }
  if (state === 'finished') {
    return (
      <Status density="context" tone="neutral" variant="soft" shape="rounded">
        Terminé · Résultat officiel
      </Status>
    )
  }
  return (
    <Status density="context" tone="info" variant="soft" shape="rounded">
      À venir
    </Status>
  )
}

function ScheduledBody() {
  return (
    <div className="ds-grid-2 ds-grid-2--major">
      <section className="ds-panel">
        <PanelHead title="Feuille de match" aside="À compléter" />
        <p className="ds-overview-situation__label">
          Les compositions peuvent être saisies dès maintenant : 11 titulaires
          et jusqu'à 3 remplaçants par équipe.
        </p>
        <button type="button" className="ds-btn ds-btn--secondary">
          Préparer la feuille de match
        </button>
      </section>

      <section className="ds-panel">
        <PanelHead title="Faits de match" />
        <div className="ds-empty">
          <span className="ds-empty__title">Le match n'a pas commencé</span>
          <span className="ds-empty__body">
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
    <div className="ds-grid-2 ds-grid-2--major">
      <section className="ds-panel">
        <PanelHead
          title="Faits de match"
          aside={`${labMatchEvents.length} événements`}
        />
        <div className="ds-match-timeline">
          {labMatchEvents.map((event, i) => (
            <div key={i} className="ds-match-timeline__row" data-kind={event.kind}>
              <span className="ds-match-timeline__minute ds-num">{event.minute}</span>
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
        <p className="ds-overview-situation__label">
          11 titulaires et 3 remplaçants déclarés de chaque côté. Capitaines :
          K. Ferrand (ROC), A. Meunier (VER).
        </p>
        <button type="button" className="ds-btn ds-btn--ghost">
          Voir la feuille complète →
        </button>
        {finished ? (
          <div className="ds-empty">
            <span className="ds-empty__title">Résultat intégré</span>
            <span className="ds-empty__body">
              Le classement est à jour. Prochaine action : préparer la
              journée 4.
            </span>
          </div>
        ) : (
          <div className="ds-empty">
            <span className="ds-empty__title">Et après ?</span>
            <span className="ds-empty__body">
              À la fin du match, le résultat alimente directement le classement
              — la prochaine action vous sera proposée ici même.
            </span>
          </div>
        )}
      </section>
    </div>
  )
}
