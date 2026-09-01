import { labCompetitions } from './labData'
import { LabWordmark } from './LabWordmark'
import { PlayUpMark } from '../design-system/PlayUpMark'

/**
 * Accueil hors Shell — hub ops avec l'atmosphère décidée le 2026-09-01 :
 * halos brand/info désaturés en haut, retour au canvas en bas.
 * La marque est pleinement exprimée ici : monogramme dégradé + wordmark.
 */
export function LabHome({ empty }: { empty: boolean }) {
  return (
    <div className="dlab-home">
      <main className="dlab-home__main">
        <header className="dlab-home__brand">
          <div className="dlab-home__mark-row">
            <PlayUpMark size={44} variant="gradient" />
            <LabWordmark className="dlab-home__wordmark" />
          </div>
          {!empty && (
            <p className="dlab-home__lede">
              Choisissez une compétition ou créez-en une nouvelle.
            </p>
          )}
        </header>

        {empty ? (
          <div className="dlab-empty">
            <span className="dlab-empty__title">
              Votre première compétition commence ici
            </span>
            <span className="dlab-empty__body">
              Donnez-lui un nom — vous organiserez les équipes, le règlement et
              le calendrier juste après.
            </span>
            <button type="button" className="ds-btn ds-btn--primary">
              + Créer une compétition
            </button>
          </div>
        ) : (
          <section className="ds-group">
            <div className="dlab-home__section-head">
              <span className="dlab-eyebrow">Vos compétitions</span>
              <button type="button" className="ds-btn ds-btn--primary">
                + Créer
              </button>
            </div>
            <div className="dlab-home__panel">
              {labCompetitions.map((competition) => (
                <button key={competition.id} type="button" className="dlab-home__row">
                  <span className="dlab-home__row-name">{competition.name}</span>
                  <span className="dlab-home__row-aside">
                    {competition.status}
                    <span aria-hidden="true">→</span>
                  </span>
                </button>
              ))}
            </div>
          </section>
        )}
      </main>
    </div>
  )
}
