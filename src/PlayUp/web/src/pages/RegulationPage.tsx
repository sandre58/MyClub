import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeftRight,
  ArrowRight,
  ArrowUpRight,
  Clock3,
  Goal,
  Info,
  Medal,
  Shuffle,
  Trophy,
} from 'lucide-react'
import { Fragment, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { fetchOrganisationView } from '../api'
import { LucideIcon } from '../design-system/icons/Icon'
import { PersonIcon } from '../design-system/icons/overviewIcons'
import { queryKeys } from '../queryKeys'
import { ErrorState, LoadingState } from '../ui'
import type {
  OrganisationRegulationSummary,
  OrganisationStageHubSummary,
  RankingCriterion,
} from '../types'
import { RegulationEditorDialog } from './RegulationEditorDialog'
import './regulation.css'

type Translate = (key: string, options?: Record<string, unknown>) => string

type RuleToken = {
  key: string
  icon: ReactNode
  label: string
}

/**
 * Règlement — hub lecture + édition cadre.
 * Vocabulaire mockup riche, tokens DS (surface, brand, typo).
 */
export function RegulationPage() {
  const { competitionId = '' } = useParams()
  const { t } = useTranslation('regulation')
  const [editorOpen, setEditorOpen] = useState(false)

  const organisationQuery = useQuery({
    queryKey: queryKeys.competitions.organisation(competitionId),
    queryFn: () => fetchOrganisationView(competitionId),
    enabled: competitionId.length > 0,
  })

  if (organisationQuery.isPending) {
    return (
      <main id="main" className="page page--regulation">
        <LoadingState />
      </main>
    )
  }

  if (organisationQuery.isError || !organisationQuery.data) {
    return (
      <main id="main" className="page page--regulation">
        <ErrorState message={t('loadError')} />
      </main>
    )
  }

  const data = organisationQuery.data
  const { regulation, stages } = data
  const canReplace = data.actions.includes('ReplaceRegulation')
  const openEditor = () => setEditorOpen(true)

  return (
    <main id="main" className="page page--regulation">
      <div className="regulation">
        <header className="regulation-header">
          <h1 className="regulation-header__title">{t('title')}</h1>
          <p className="regulation-header__subtitle">{t('subtitle')}</p>
          <span className="regulation-header__accent" aria-hidden="true" />
        </header>

        <section
          className="regulation-frame"
          aria-label={t('frameHeading')}
        >
          <EntriesTile
            regulation={regulation}
            canEdit={canReplace}
            onEdit={openEditor}
          />
          <MatchTile
            regulation={regulation}
            canEdit={canReplace}
            onEdit={openEditor}
          />
          <StandingTile
            regulation={regulation}
            canEdit={canReplace}
            onEdit={openEditor}
          />
        </section>

        <section
          className="regulation-section"
          aria-labelledby="regulation-phases-heading"
        >
          <h2
            id="regulation-phases-heading"
            className="regulation-section__label"
          >
            {t('phasesHeading')}
          </h2>
          {stages.length === 0 ? (
            <p className="regulation-empty">{t('emptyPhases')}</p>
          ) : (
            <div className="regulation-phases">
              {stages.map((stage, index) => (
                <Fragment key={stage.stageId}>
                  {index > 0 ? (
                    <div
                      className="regulation-phases__bridge"
                      aria-hidden="true"
                    >
                      <span className="regulation-phases__bridge-line" />
                      {stages[index - 1]?.hasQualificationRules ? (
                        <span className="regulation-phases__bridge-label">
                          {t('qualificationBridge')}
                        </span>
                      ) : null}
                      <span className="regulation-phases__bridge-line" />
                    </div>
                  ) : null}
                  <PhaseTile stage={stage} ordinal={index + 1} />
                </Fragment>
              ))}
            </div>
          )}
        </section>

        <p className="regulation-note" role="note">
          <LucideIcon icon={Info} size="sm" />
          <span>{t('frameNote')}</span>
        </p>
      </div>

      <RegulationEditorDialog
        data={data}
        open={editorOpen}
        onClose={() => setEditorOpen(false)}
      />
    </main>
  )
}

function EditAction({
  canEdit,
  onEdit,
}: {
  canEdit: boolean
  onEdit: () => void
}) {
  const { t } = useTranslation('regulation')
  return (
    <button
      type="button"
      className="regulation-edit"
      disabled={!canEdit}
      title={canEdit ? undefined : t('editUnavailable')}
      onClick={onEdit}
    >
      {t('edit')}
    </button>
  )
}

function FrameCard({
  icon,
  title,
  canEdit,
  onEdit,
  children,
}: {
  icon: ReactNode
  title: string
  canEdit: boolean
  onEdit: () => void
  children: ReactNode
}) {
  return (
    <article className="regulation-card">
      <header className="regulation-card__head">
        <span className="regulation-card__icon" aria-hidden="true">
          {icon}
        </span>
        <h3 className="regulation-card__title">{title}</h3>
      </header>
      <div className="regulation-card__body">{children}</div>
      <div className="regulation-card__footer">
        <EditAction canEdit={canEdit} onEdit={onEdit} />
      </div>
    </article>
  )
}

function EntriesTile({
  regulation,
  canEdit,
  onEdit,
}: {
  regulation: OrganisationRegulationSummary
  canEdit: boolean
  onEdit: () => void
}) {
  const { t } = useTranslation('regulation')
  const min = regulation.minimumTeams
  const max = regulation.maximumTeams

  return (
    <FrameCard
      icon={<PersonIcon size="md" />}
      title={t('families.entries')}
      canEdit={canEdit}
      onEdit={onEdit}
    >
      <div
        className="regulation-capacity"
        aria-label={t('capacity.aria', { min, max })}
      >
        <div className="regulation-capacity__scale" aria-hidden="true">
          <span className="regulation-capacity__knob" />
          <span className="regulation-capacity__rail" />
          <span className="regulation-capacity__knob regulation-capacity__knob--max" />
        </div>
        <div className="regulation-capacity__nums">
          <div className="regulation-capacity__bound">
            <span className="regulation-capacity__value">{min}</span>
            <span className="regulation-capacity__cap">{t('capacity.min')}</span>
          </div>
          <div className="regulation-capacity__bound regulation-capacity__bound--max">
            <span className="regulation-capacity__value">{max}</span>
            <span className="regulation-capacity__cap">{t('capacity.max')}</span>
          </div>
        </div>
      </div>
    </FrameCard>
  )
}

function MatchTile({
  regulation,
  canEdit,
  onEdit,
}: {
  regulation: OrganisationRegulationSummary
  canEdit: boolean
  onEdit: () => void
}) {
  const { t } = useTranslation('regulation')
  const halfTime = regulation.halfTimeDuration ?? 0
  const regulationPieces = buildPlayClockPieces({
    periodCount: regulation.numberOfPeriods,
    minutesPerPeriod: regulation.durationPerPeriod,
    breakMinutes: halfTime,
    tone: 'regulation',
    periodLabel: (n) => periodLabel(n, t),
    breakLabel: t('matchTimeline.break'),
  })
  const extraPieces =
    regulation.hasExtraTime &&
    (regulation.extraTimeNumberOfPeriods ?? 0) > 0
      ? buildPlayClockPieces({
          periodCount: regulation.extraTimeNumberOfPeriods ?? 0,
          minutesPerPeriod: regulation.extraTimeDurationPerPeriod ?? 0,
          breakMinutes: 0,
          tone: 'extra',
          periodLabel: (n) => t('matchTimeline.extra', { n }),
          breakLabel: t('matchTimeline.break'),
        })
      : []
  const regulationPlayMinutes =
    regulation.numberOfPeriods * regulation.durationPerPeriod
  const extraPlayMinutes =
    (regulation.extraTimeNumberOfPeriods ?? 0) *
    (regulation.extraTimeDurationPerPeriod ?? 0)
  const kicks = regulation.penaltyInitialKicksPerTeam ?? null

  return (
    <FrameCard
      icon={<LucideIcon icon={Goal} size="md" />}
      title={t('families.match')}
      canEdit={canEdit}
      onEdit={onEdit}
    >
      <div className="regulation-match" aria-label={t('matchTimeline.aria')}>
        <MatchClockBlock
          heading={t('matchTimeline.regulationHeading')}
          pieces={regulationPieces}
          totalLabel={t('matchTotal', { minutes: regulationPlayMinutes })}
        />

        {extraPieces.length > 0 ? (
          <MatchClockBlock
            heading={t('matchTimeline.extraHeading')}
            pieces={extraPieces}
            totalLabel={t('matchExtraTotal', { minutes: extraPlayMinutes })}
          />
        ) : null}

        {regulation.hasPenaltyShootout ? (
          <div className="regulation-tab">
            <p className="regulation-match__heading">{t('matchTimeline.tabHeading')}</p>
            <div
              className="regulation-tab__kicks"
              aria-label={
                kicks != null
                  ? t('matchTimeline.tabAria', { count: kicks })
                  : t('matchTimeline.tabHeading')
              }
            >
              {Array.from({ length: Math.min(Math.max(kicks ?? 5, 1), 8) }, (_, i) => (
                <span key={i} className="regulation-tab__kick" aria-hidden="true" />
              ))}
            </div>
            <p className="regulation-tab__meta">
              {kicks != null
                ? t('matchTimeline.tabMeta', { count: kicks })
                : t('matchFlags.penalties')}
            </p>
          </div>
        ) : null}
      </div>
    </FrameCard>
  )
}

type MatchClockPiece =
  | {
      key: string
      kind: 'play'
      label: string
      minutes: number
      tone: 'regulation' | 'extra'
      alt: boolean
    }
  | {
      key: string
      kind: 'break'
      label: string
      minutes: number
    }

function buildPlayClockPieces({
  periodCount,
  minutesPerPeriod,
  breakMinutes,
  tone,
  periodLabel: labelFor,
  breakLabel,
}: {
  periodCount: number
  minutesPerPeriod: number
  breakMinutes: number
  tone: 'regulation' | 'extra'
  periodLabel: (n: number) => string
  breakLabel: string
}): MatchClockPiece[] {
  const count = Math.max(periodCount, 0)
  const pieces: MatchClockPiece[] = []
  for (let i = 0; i < count; i++) {
    pieces.push({
      key: `${tone}-play-${i}`,
      kind: 'play',
      label: labelFor(i + 1),
      minutes: minutesPerPeriod,
      tone,
      alt: i % 2 === 1,
    })
    if (i < count - 1 && breakMinutes > 0) {
      pieces.push({
        key: `${tone}-break-${i}`,
        kind: 'break',
        label: breakLabel,
        minutes: breakMinutes,
      })
    }
  }
  return pieces
}

function MatchClockBlock({
  heading,
  pieces,
  totalLabel,
}: {
  heading: string
  pieces: MatchClockPiece[]
  totalLabel: string
}) {
  if (pieces.length === 0) {
    return null
  }

  return (
    <div className="regulation-clock">
      <p className="regulation-match__heading">{heading}</p>
      <div className="regulation-clock__row">
        {pieces.map((piece) =>
          piece.kind === 'break' ? (
            <div
              key={piece.key}
              className="regulation-clock__piece regulation-clock__piece--break"
              style={{ flex: `${Math.max(piece.minutes, 1)} 1 0` }}
            >
              <span className="regulation-clock__label">{piece.label}</span>
              <span className="regulation-clock__bar" aria-hidden="true" />
              <span className="regulation-clock__mins">{piece.minutes}′</span>
            </div>
          ) : (
            <div
              key={piece.key}
              className={[
                'regulation-clock__piece',
                `regulation-clock__piece--${piece.tone}`,
                piece.alt ? 'regulation-clock__piece--alt' : '',
              ]
                .filter(Boolean)
                .join(' ')}
              style={{ flex: `${Math.max(piece.minutes, 1)} 1 0` }}
            >
              <span className="regulation-clock__label">{piece.label}</span>
              <span className="regulation-clock__bar" aria-hidden="true" />
              <span className="regulation-clock__mins">{piece.minutes}′</span>
            </div>
          ),
        )}
      </div>
      <p className="regulation-clock__total">{totalLabel}</p>
    </div>
  )
}

function StandingTile({
  regulation,
  canEdit,
  onEdit,
}: {
  regulation: OrganisationRegulationSummary
  canEdit: boolean
  onEdit: () => void
}) {
  const { t } = useTranslation('regulation')
  const maxPts = Math.max(
    regulation.winPoints,
    regulation.drawPoints,
    regulation.lossPoints,
    1,
  )
  const criteria = regulation.rankingCriteria ?? []

  return (
    <FrameCard
      icon={<LucideIcon icon={Trophy} size="md" />}
      title={t('families.standing')}
      canEdit={canEdit}
      onEdit={onEdit}
    >
      <div className="regulation-standing">
        <div className="regulation-standing__points">
          <p className="regulation-standing__heading">{t('points.heading')}</p>
          <ul className="regulation-gauges" aria-label={t('points.aria')}>
            <PointGauge
              value={regulation.winPoints}
              max={maxPts}
              label={t('points.win')}
              tone="win"
            />
            <PointGauge
              value={regulation.drawPoints}
              max={maxPts}
              label={t('points.draw')}
              tone="draw"
            />
            <PointGauge
              value={regulation.lossPoints}
              max={maxPts}
              label={t('points.loss')}
              tone="loss"
            />
          </ul>
        </div>

        {criteria.length > 0 ? (
          <div className="regulation-standing__criteria">
            <p className="regulation-standing__heading">
              {t('criteria.heading')}
            </p>
            <ol className="regulation-criteria" aria-label={t('criteria.aria')}>
              {criteria.map((criterion, index) => (
                <li key={criterion} className="regulation-criteria__item">
                  <span className="regulation-criteria__n" aria-hidden="true">
                    {index + 1}
                  </span>
                  <span>{criterionLabel(criterion, t)}</span>
                </li>
              ))}
            </ol>
          </div>
        ) : null}
      </div>
    </FrameCard>
  )
}

function PointGauge({
  value,
  max,
  label,
  tone,
}: {
  value: number
  max: number
  label: string
  tone: 'win' | 'draw' | 'loss'
}) {
  const width = `${Math.max(value === 0 ? 0 : 12, (value / max) * 100)}%`
  return (
    <li className={`regulation-gauge regulation-gauge--${tone}`}>
      <div className="regulation-gauge__top">
        <span className="regulation-gauge__label">{label}</span>
        <span className="regulation-gauge__value">
          {value}
          <small> pts</small>
        </span>
      </div>
      <span className="regulation-gauge__track" aria-hidden="true">
        <span className="regulation-gauge__fill" style={{ width }} />
      </span>
    </li>
  )
}

function PhaseTile({
  stage,
  ordinal,
}: {
  stage: OrganisationStageHubSummary
  ordinal: number
}) {
  const { t } = useTranslation('regulation')
  const structureHref = `/stages/${stage.stageId}`
  const tokens = buildRuleTokens(stage, t)

  return (
    <article className="regulation-phase">
      <header className="regulation-phase__head">
        <span className="regulation-phase__ordinal" aria-hidden="true">
          {String(ordinal).padStart(2, '0')}
        </span>
        <div className="regulation-phase__titles">
          <h3 className="regulation-phase__name">{stage.name}</h3>
          <p
            className="regulation-phase__topology"
            aria-label={t('topology.aria', {
              teams: stage.teamCount,
              matches: stage.matchCount,
            })}
          >
            <span className="regulation-fact">
              <PersonIcon size="sm" />
              {t('topology.teams', { count: stage.teamCount })}
            </span>
            <span className="regulation-fact">
              <LucideIcon icon={Goal} size="sm" />
              {t('topology.matches', { count: stage.matchCount })}
            </span>
          </p>
        </div>
      </header>

      <div className="regulation-phase__body">
        <PhaseSchematic stage={stage} />
        {tokens.length > 0 ? (
          <ul className="regulation-phase__rules" aria-label={t('tokens.aria')}>
            {tokens.map((token) => (
              <li key={token.key} className="regulation-token">
                {token.icon}
                <span>{token.label}</span>
              </li>
            ))}
          </ul>
        ) : null}
      </div>

      <Link className="regulation-phase__cta" to={structureHref}>
        {t('openInStructure')}
      </Link>
    </article>
  )
}

function PhaseSchematic({ stage }: { stage: OrganisationStageHubSummary }) {
  const { t } = useTranslation('regulation')
  const groupCount = stage.groupCount ?? 0
  const roundCount = stage.roundCount ?? 0

  if (groupCount > 0) {
    const shown = Math.min(groupCount, 4)
    const perGroup = Math.max(
      1,
      Math.round(stage.teamCount / Math.max(groupCount, 1)),
    )
    return (
      <div
        className="regulation-schematic regulation-schematic--groups"
        aria-label={t('schematic.groups', { count: groupCount })}
      >
        <div className="regulation-schematic__groups">
          {Array.from({ length: shown }, (_, i) => (
            <div key={i} className="regulation-schematic__group">
              <span className="regulation-schematic__group-label">
                {String.fromCharCode(65 + i)}
              </span>
              <div className="regulation-schematic__dots">
                {Array.from({ length: Math.min(perGroup, 4) }, (_, j) => (
                  <span key={j} className="regulation-schematic__dot" />
                ))}
              </div>
            </div>
          ))}
        </div>
        {stage.hasQualificationRules && stage.qualificationPathCount > 0 ? (
          <div className="regulation-schematic__out">
            <span className="regulation-schematic__arrow" aria-hidden="true">
              →
            </span>
            <span className="regulation-schematic__chip">
              {t('schematic.qualify', { count: stage.qualificationPathCount })}
            </span>
          </div>
        ) : null}
      </div>
    )
  }

  if (roundCount > 0 || stage.hasTieFormat) {
    const nodes = Math.min(Math.max(stage.teamCount, 2), 8)
    return (
      <div
        className="regulation-schematic regulation-schematic--bracket"
        aria-label={t('schematic.bracket')}
      >
        <div className="regulation-schematic__bracket-col">
          {Array.from({ length: nodes }, (_, i) => (
            <span key={i} className="regulation-schematic__node" />
          ))}
        </div>
        <div className="regulation-schematic__bracket-col regulation-schematic__bracket-col--mid">
          {Array.from({ length: Math.ceil(nodes / 2) }, (_, i) => (
            <span key={i} className="regulation-schematic__node" />
          ))}
        </div>
        <span className="regulation-schematic__trophy" aria-hidden="true">
          <LucideIcon icon={Trophy} size="md" />
        </span>
      </div>
    )
  }

  return (
    <div
      className="regulation-schematic regulation-schematic--flat"
      aria-label={t('topology.aria', {
        teams: stage.teamCount,
        matches: stage.matchCount,
      })}
    >
      <div className="regulation-schematic__dots">
        {Array.from(
          { length: Math.min(Math.max(stage.teamCount, 0), 8) },
          (_, i) => (
            <span key={i} className="regulation-schematic__dot" />
          ),
        )}
      </div>
    </div>
  )
}

function buildRuleTokens(
  stage: OrganisationStageHubSummary,
  t: Translate,
): RuleToken[] {
  const tokens: RuleToken[] = []

  let matchLabel = t('tokens.match', {
    periods: stage.numberOfPeriods,
    minutes: stage.durationPerPeriod,
  })
  if (stage.hasExtraTime) {
    matchLabel += t('tokens.extraTimeSuffix')
  }
  if (stage.hasPenaltyShootout) {
    matchLabel += t('tokens.penaltiesSuffix')
  }
  tokens.push({
    key: 'match',
    icon: <LucideIcon icon={Clock3} size="sm" />,
    label: matchLabel,
  })

  tokens.push({
    key: 'standing',
    icon: <LucideIcon icon={Trophy} size="sm" />,
    label: t('tokens.standing', {
      win: stage.winPoints,
      draw: stage.drawPoints,
      loss: stage.lossPoints,
    }),
  })

  if (stage.hasQualificationRules && stage.qualificationPathCount > 0) {
    tokens.push({
      key: 'qualification',
      icon: <LucideIcon icon={ArrowUpRight} size="sm" />,
      label: t('tokens.qualification', {
        count: stage.qualificationPathCount,
      }),
    })
  }

  if (stage.hasDrawRules) {
    tokens.push({
      key: 'draw',
      icon: <LucideIcon icon={Shuffle} size="sm" />,
      label: t('tokens.drawAuto'),
    })
  }

  if (stage.hasProgressionRules && stage.progressionPathCount > 0) {
    tokens.push({
      key: 'progression',
      icon: <LucideIcon icon={ArrowRight} size="sm" />,
      label: String(stage.progressionPathCount),
    })
  }

  if (stage.hasTieFormat && stage.numberOfLegs != null) {
    tokens.push({
      key: 'tie',
      icon:
        stage.numberOfLegs > 1 ? (
          <LucideIcon icon={ArrowLeftRight} size="sm" />
        ) : (
          <LucideIcon icon={Medal} size="sm" />
        ),
      label:
        stage.numberOfLegs > 1
          ? t('tokens.tieTwoLegs')
          : t('tokens.tieOneLeg'),
    })
  }

  return tokens
}

function criterionLabel(criterion: RankingCriterion, t: Translate) {
  return t(`criteria.${criterion}`, { defaultValue: criterion })
}

function periodLabel(n: number, t: Translate) {
  if (n === 1) {
    return t('matchTimeline.periodFirst')
  }
  if (n === 2) {
    return t('matchTimeline.periodSecond')
  }
  return t('matchTimeline.periodNth', { n })
}
