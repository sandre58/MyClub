import { HomeBrand } from '../design-system/components/HomeBrand'
import { Status } from '../design-system/components/Status'
import { TeamCrest } from '../design-system/TeamCrest'
import { ChevronRightIcon } from '../design-system/icons/shellIcons'
import { PlusIcon } from '../design-system/icons/overviewIcons'
import { declaredSchedule } from '../shell/competitionPeriod'
import { labCompetitions } from './labData'
import { LabWaitAtom, type LabWaitKind } from './LabWait'

/**
 * Accueil hors Shell — hub ops avec l'atmosphère décidée le 2026-09-01 :
 * halos brand/info désaturés en haut, retour au canvas en bas.
 * Lockup Accueil (planche 2026-09-02) + lignes interactives hover A.
 */
export function LabHome({
  empty,
  waiting,
}: {
  empty: boolean
  waiting?: LabWaitKind
}) {
  return (
    <div className="ds-home">
      <main className="ds-home__main">
        <HomeBrand
          lede={
            empty
              ? 'Un nom suffit. Vous configurerez équipes et format ensuite.'
              : 'Choisissez une compétition ou créez-en une nouvelle.'
          }
        />

        {waiting ? (
          <LabWaitAtom kind={waiting} scale="page" />
        ) : empty ? (
          <div className="ds-empty">
            <button type="button" className="ds-btn ds-btn--primary">
              <PlusIcon size="sm" />
              Créer une compétition
            </button>
          </div>
        ) : (
          <section className="ds-group">
            <div className="ds-home__section-head">
              <span className="ds-eyebrow">Vos compétitions</span>
              <button type="button" className="ds-btn ds-btn--primary">
                <PlusIcon size="sm" />
                Créer
              </button>
            </div>
            <div className="ds-home__panel">
              {labCompetitions.map((competition) => {
                const schedule = labScheduleLabel(
                  competition.scheduledStart,
                  competition.scheduledEnd,
                )
                return (
                  <button
                    key={competition.id}
                    type="button"
                    className="ds-interactive-row ds-home__row"
                  >
                    <span className="ds-home__row-main">
                      <TeamCrest name={competition.name} size="md" />
                      <span className="ds-home__row-copy">
                        <span className="ds-home__row-name">
                          {competition.name}
                        </span>
                        {schedule ? (
                          <span className="ds-home__row-meta">{schedule}</span>
                        ) : null}
                      </span>
                    </span>
                    <span className="ds-home__row-aside">
                      <Status density="context" tone={competition.statusTone}>
                        {competition.status}
                      </Status>
                      <span
                        className="ds-interactive-row__chevron"
                        aria-hidden="true"
                      >
                        <ChevronRightIcon size="sm" />
                      </span>
                    </span>
                  </button>
                )
              })}
            </div>
          </section>
        )}
      </main>
    </div>
  )
}

function labScheduleLabel(
  start?: string,
  end?: string,
): string | null {
  const schedule = declaredSchedule(start, end)
  if (!schedule) {
    return null
  }
  if (schedule.kind === 'both') {
    return `${schedule.start} → ${schedule.end}`
  }
  if (schedule.kind === 'start') {
    return `À partir du ${schedule.date}`
  }
  return `Jusqu’au ${schedule.date}`
}
