import { labCompetitions } from './labData'
import { HomeBrand } from '../design-system/components/HomeBrand'

/**
 * Accueil hors Shell — hub ops avec l'atmosphère décidée le 2026-09-01 :
 * halos brand/info désaturés en haut, retour au canvas en bas.
 * La marque est pleinement exprimée ici : monogramme dégradé + wordmark.
 */
export function LabHome({ empty }: { empty: boolean }) {
  return (
    <div className="ds-home">
      <main className="ds-home__main">
        <HomeBrand
          lede={
            empty
              ? undefined
              : 'Choisissez une compétition ou créez-en une nouvelle.'
          }
        />

        {empty ? (
          <div className="ds-empty">
            <span className="ds-empty__title">
              Votre première compétition commence ici
            </span>
            <span className="ds-empty__body">
              Donnez-lui un nom — vous organiserez les équipes, le règlement et
              le calendrier juste après.
            </span>
            <button type="button" className="ds-btn ds-btn--primary">
              + Créer une compétition
            </button>
          </div>
        ) : (
          <section className="ds-group">
            <div className="ds-home__section-head">
              <span className="ds-eyebrow">Vos compétitions</span>
              <button type="button" className="ds-btn ds-btn--primary">
                + Créer
              </button>
            </div>
            <div className="ds-home__panel">
              {labCompetitions.map((competition) => (
                <button key={competition.id} type="button" className="ds-home__row">
                  <span className="ds-home__row-name">{competition.name}</span>
                  <span className="ds-home__row-aside">
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
