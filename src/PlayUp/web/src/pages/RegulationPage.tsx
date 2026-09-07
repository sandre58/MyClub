import {useQuery} from '@tanstack/react-query'
import {
    ArrowLeftRight,
    ArrowRight,
    ArrowUpRight,
    Clock3,
    Info,
    Medal,
    Shuffle,
    Timer,
    Trophy,
    Volleyball,
} from 'lucide-react'
import {useState, type ReactNode} from 'react'
import {useTranslation} from 'react-i18next'
import {useParams} from 'react-router-dom'
import {fetchOrganisationView} from '../api'
import {Alert} from '../design-system/components/Alert'
import {Meter, type MeterTone} from '../design-system/components/Meter'
import {PageHead} from '../design-system/components/PageHead'
import {TextLink} from '../design-system/components/TextLink'
import {LucideIcon} from '../design-system/icons/Icon'
import {PencilIcon, PersonIcon} from '../design-system/icons/overviewIcons'
import {queryKeys} from '../queryKeys'
import {ErrorState, LoadingState, StatusBadge} from '../ui'
import type {
    OrganisationRegulationSummary,
    OrganisationStageHubSummary,
    RankingCriterion,
} from '../types'
import {RegulationEditorDialog} from './RegulationEditorDialog'
import './regulation.css'

type Translate = (key: string, options?: Record<string, unknown>) => string

type RuleToken = {
    key: string
    content: ReactNode
    ariaLabel: string
}

/**
 * Règlement — hub lecture + édition cadre.
 * Vocabulaire mockup riche, tokens DS (surface, brand, typo).
 */
export function RegulationPage() {
    const {competitionId = ''} = useParams()
    const {t} = useTranslation('regulation')
    const [editorOpen, setEditorOpen] = useState(false)

    const organisationQuery = useQuery({
        queryKey: queryKeys.competitions.organisation(competitionId),
        queryFn: () => fetchOrganisationView(competitionId),
        enabled: competitionId.length > 0,
    })

    if (organisationQuery.isPending) {
        return (
            <main id="main" className="page page--regulation">
                <LoadingState/>
            </main>
        )
    }

    if (organisationQuery.isError || !organisationQuery.data) {
        return (
            <main id="main" className="page page--regulation">
                <ErrorState message={t('loadError')}/>
            </main>
        )
    }

    const data = organisationQuery.data
    const {regulation, stages} = data
    const canReplace = data.actions.includes('ReplaceRegulation')
    const openEditor = () => setEditorOpen(true)
    const showStandingTile = stages.some((stage) => stage.hasStandingRules === true)

    return (
        <main id="main" className="page page--regulation">
            <div className="regulation">
                <PageHead
                    title={t('title')}
                    note={
                        <p className="regulation-note" role="note">
                            <LucideIcon icon={Info} size="sm"/>
                            <span>{t('frameNote')}</span>
                        </p>
                    }
                    actions={
                        <button
                            type="button"
                            className="ds-btn ds-btn--primary"
                            disabled={!canReplace}
                            title={
                                canReplace
                                    ? t('editRegulation')
                                    : t('editRegulationDisabledHint')
                            }
                            aria-label={t('editRegulation')}
                            onClick={openEditor}
                        >
                            <PencilIcon size="sm"/>
                            <span>{t('editRegulation')}</span>
                        </button>
                    }
                />

                <section
                    className={[
                        'regulation-frame',
                        showStandingTile ? 'regulation-frame--with-standing' : '',
                    ]
                        .filter(Boolean)
                        .join(' ')}
                    aria-label={t('frameHeading')}
                >
                    <EntriesTile regulation={regulation}/>
                    <MatchTile regulation={regulation}/>
                    {showStandingTile ? <StandingTile regulation={regulation}/> : null}
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
                                <PhaseTile
                                    key={stage.stageId}
                                    stage={stage}
                                    ordinal={index + 1}
                                />
                            ))}
                        </div>
                    )}
                </section>
            </div>

            <RegulationEditorDialog
                data={data}
                open={editorOpen}
                onClose={() => setEditorOpen(false)}
            />
        </main>
    )
}

function FrameCard({
                       icon,
                       title,
                       children,
                   }: {
    icon: ReactNode
    title: string
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
        </article>
    )
}

function EntriesTile({
                         regulation,
                     }: {
    regulation: OrganisationRegulationSummary
}) {
    const {t} = useTranslation('regulation')
    const min = regulation.minimumTeams
    const max = regulation.maximumTeams

    return (
        <FrameCard
            icon={<PersonIcon size="md"/>}
            title={t('families.entries')}
        >
            <Alert tone="info" role="status">
                {t('capacity.notice', {min, max})}
            </Alert>
            <div
                className="regulation-capacity"
                aria-label={t('capacity.aria', {min, max})}
            >
                <div className="regulation-capacity__scale" aria-hidden="true">
          <span className="regulation-capacity__pill regulation-capacity__pill--min">
            {min}
          </span>
                    <span className="regulation-capacity__rail">
            <span className="regulation-capacity__rail-fill"/>
            <span className="regulation-capacity__rail-glow"/>
          </span>
                    <span className="regulation-capacity__pill regulation-capacity__pill--max">
            {max}
          </span>
                </div>
                <div className="regulation-capacity__caps">
          <span className="regulation-capacity__cap regulation-capacity__cap--min">
            {t('capacity.min')}
          </span>
                    <span className="regulation-capacity__cap regulation-capacity__cap--max">
            {t('capacity.max')}
          </span>
                </div>
            </div>
        </FrameCard>
    )
}

function MatchTile({
                       regulation,
                   }: {
    regulation: OrganisationRegulationSummary
}) {
    const {t} = useTranslation('regulation')
    const halfTime = regulation.halfTimeDuration ?? 0
    const regulationPieces = buildPlayClockPieces({
        periodCount: regulation.numberOfPeriods,
        minutesPerPeriod: regulation.durationPerPeriod,
        breakMinutes: halfTime,
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
                periodLabel: (n) => t('matchTimeline.extra', {n}),
                breakLabel: t('matchTimeline.break'),
            })
            : []
    const regulationPlayMinutes =
        regulation.numberOfPeriods * regulation.durationPerPeriod
    const extraPlayMinutes =
        (regulation.extraTimeNumberOfPeriods ?? 0) *
        (regulation.extraTimeDurationPerPeriod ?? 0)
    const hasExtra = extraPieces.length > 0
    const maxMinutes = regulationPlayMinutes + (hasExtra ? extraPlayMinutes : 0)
    const kicks = regulation.penaltyInitialKicksPerTeam ?? null

    return (
        <FrameCard
            icon={<LucideIcon icon={Volleyball} size="md"/>}
            title={t('families.match')}
        >
            <div className="regulation-match" aria-label={t('matchTimeline.aria')}>
                <div className="regulation-match__layout">
                    <MaxDurationRing minutes={maxMinutes}/>
                    <div className="regulation-match__timelines">
                        <div className="regulation-clock">
                            <p className="regulation-match__heading">
                                {t('matchTimeline.regulationHeading')}
                            </p>
                            <ClockRow pieces={regulationPieces}/>
                        </div>

                        {hasExtra ? (
                            <div className="regulation-match-line">
                <span className="regulation-match-line__label">
                  <LucideIcon icon={Timer} size="sm"/>
                  <span>{t('matchTimeline.extraHeading')}</span>
                </span>
                                <div className="regulation-match-line__visual regulation-clock--compact">
                                    <ClockRow pieces={extraPieces}/>
                                </div>
                            </div>
                        ) : null}

                        {regulation.hasPenaltyShootout ? (
                            <div
                                className="regulation-match-line"
                                aria-label={
                                    kicks != null
                                        ? t('matchTimeline.tabAria', {count: kicks})
                                        : t('matchTimeline.tabHeading')
                                }
                            >
                <span className="regulation-match-line__label">
                  <LucideIcon icon={Volleyball} size="sm"/>
                  <span>{t('matchTimeline.tabHeading')}</span>
                </span>
                                <div className="regulation-match-line__visual">
                                    {kicks != null ? (
                                        <span className="regulation-tab-row__dots" aria-hidden="true">
                      {Array.from(
                          {length: Math.min(Math.max(kicks, 1), 8)},
                          (_, i) => (
                              <span key={i} className="regulation-tab__kick"/>
                          ),
                      )}
                    </span>
                                    ) : null}
                                </div>
                            </div>
                        ) : null}
                    </div>
                </div>
            </div>
        </FrameCard>
    )
}

function MaxDurationRing({minutes}: { minutes: number }) {
    const {t} = useTranslation('regulation')
    const size = 96
    const stroke = 7
    const radius = (size - stroke) / 2
    const circumference = 2 * Math.PI * radius
    const ratio = Math.min(0.92, Math.max(0.55, minutes / 135))
    const dash = circumference * ratio

    return (
        <div
            className="regulation-max"
            aria-label={t('matchTimeline.maxAria', {minutes})}
            title={t('matchTimeline.maxHint')}
        >
            <svg
                className="regulation-max__svg"
                width={size}
                height={size}
                viewBox={`0 0 ${size} ${size}`}
                aria-hidden="true"
            >
                <circle
                    className="regulation-max__track"
                    cx={size / 2}
                    cy={size / 2}
                    r={radius}
                    strokeWidth={stroke}
                    fill="none"
                />
                <circle
                    className="regulation-max__fill"
                    cx={size / 2}
                    cy={size / 2}
                    r={radius}
                    strokeWidth={stroke}
                    fill="none"
                    strokeDasharray={`${dash} ${circumference - dash}`}
                    strokeLinecap="round"
                    transform={`rotate(-90 ${size / 2} ${size / 2})`}
                />
            </svg>
            <div className="regulation-max__center">
                <span className="regulation-max__value">{minutes}</span>
                <span className="regulation-max__unit">{t('matchTimeline.maxUnit')}</span>
                <span className="regulation-max__label">{t('matchTimeline.maxLabel')}</span>
            </div>
        </div>
    )
}

type MatchClockPiece =
    | {
    key: string
    kind: 'play'
    label: string
    minutes: number
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
                                  periodLabel: labelFor,
                                  breakLabel,
                              }: {
    periodCount: number
    minutesPerPeriod: number
    breakMinutes: number
    periodLabel: (n: number) => string
    breakLabel: string
}): MatchClockPiece[] {
    const count = Math.max(periodCount, 0)
    const pieces: MatchClockPiece[] = []
    for (let i = 0; i < count; i++) {
        pieces.push({
            key: `play-${i}`,
            kind: 'play',
            label: labelFor(i + 1),
            minutes: minutesPerPeriod,
            alt: i % 2 === 1,
        })
        if (i < count - 1 && breakMinutes > 0) {
            pieces.push({
                key: `break-${i}`,
                kind: 'break',
                label: breakLabel,
                minutes: breakMinutes,
            })
        }
    }
    return pieces
}

function ClockRow({pieces}: { pieces: MatchClockPiece[] }) {
    if (pieces.length === 0) {
        return null
    }

    return (
        <div className="regulation-clock__row">
            {pieces.map((piece) =>
                piece.kind === 'break' ? (
                    <div
                        key={piece.key}
                        className="regulation-clock__piece regulation-clock__piece--break"
                        style={{flex: `${Math.max(piece.minutes, 1)} 1 0`}}
                    >
                        <span className="regulation-clock__label">{piece.label}</span>
                        <span className="regulation-clock__bar" aria-hidden="true"/>
                        <span className="regulation-clock__mins">{piece.minutes}′</span>
                    </div>
                ) : (
                    <div
                        key={piece.key}
                        className={[
                            'regulation-clock__piece',
                            piece.alt ? 'regulation-clock__piece--alt' : '',
                        ]
                            .filter(Boolean)
                            .join(' ')}
                        style={{flex: `${Math.max(piece.minutes, 1)} 1 0`}}
                    >
                        <span className="regulation-clock__label">{piece.label}</span>
                        <span className="regulation-clock__bar" aria-hidden="true"/>
                        <span className="regulation-clock__mins">{piece.minutes}′</span>
                    </div>
                ),
            )}
        </div>
    )
}

function StandingTile({
                          regulation,
                      }: {
    regulation: OrganisationRegulationSummary
}) {
    const {t} = useTranslation('regulation')
    const maxPts = Math.max(
        regulation.winPoints,
        regulation.drawPoints,
        regulation.lossPoints,
        1,
    )
    const criteria = regulation.rankingCriteria ?? []

    return (
        <FrameCard
            icon={<LucideIcon icon={Trophy} size="md"/>}
            title={t('families.standing')}
        >
            <Alert tone="info" role="status">
                {t('standingNote')}
            </Alert>
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
    const {t} = useTranslation('regulation')
    const ratio = max > 0 ? Math.max(value === 0 ? 0 : 0.12, value / max) : 0
    const meterTone: MeterTone =
        tone === 'win' ? 'success' : tone === 'draw' ? 'attention' : 'neutral'
    return (
        <li className="regulation-gauge">
            <div className="regulation-gauge__top">
                <span className="regulation-gauge__label">{label}</span>
                <span className="regulation-gauge__value">
          {value}
                    <small> {t('points.unit')}</small>
        </span>
            </div>
            <Meter ratio={ratio} tone={meterTone} size="lg" clip aria-hidden="true" />
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
    const {t} = useTranslation('regulation')
    const structureHref = `/stages/${stage.stageId}`
    const structureTokens = buildStructureTokens(stage, t)
    const regulationTokens = buildRegulationTokens(stage, t)
    const kind = inferPhaseKind(stage)

    return (
        <article className="regulation-phase">
            <header className="regulation-phase__head">
                <div className="regulation-phase__head-main">
          <span className="regulation-phase__ordinal" aria-hidden="true">
            {ordinal}
          </span>
                    <h3 className="regulation-card__title">{stage.name}</h3>
                </div>
                <span className="regulation-phase__badge-wrap">
          <StatusBadge tone="info" density="compact" shape="rounded">
            {t(`phaseKind.${kind}`)}
          </StatusBadge>
        </span>
            </header>

            <div className="regulation-phase__content">
                <div
                    className="regulation-phase__structure"
                    aria-label={t('phaseStructureAria')}
                >
                    <div
                        className="regulation-phase__stats"
                        aria-label={t('topology.aria', {
                            teams: stage.teamCount,
                            matches: stage.matchCount,
                        })}
                    >
                        <div className="regulation-stat">
            <span className="regulation-stat__icon" aria-hidden="true">
              <PersonIcon size="sm"/>
            </span>
                            <span className="regulation-stat__copy">
              <span className="regulation-stat__value">{stage.teamCount}</span>
              <span className="regulation-stat__label">{t('topology.teams')}</span>
            </span>
                        </div>
                        <div className="regulation-stat">
            <span className="regulation-stat__icon" aria-hidden="true">
              <LucideIcon icon={Volleyball} size="sm"/>
            </span>
                            <span className="regulation-stat__copy">
              <span className="regulation-stat__value">{stage.matchCount}</span>
              <span className="regulation-stat__label">{t('topology.matches')}</span>
            </span>
                        </div>
                    </div>

                    <div className="regulation-phase__visual">
                        <PhaseSchematic stage={stage}/>
                        {structureTokens.length > 0 ? (
                            <ul className="regulation-phase__flows">
                                {structureTokens.map((token) => (
                                    <li
                                        key={token.key}
                                        className="regulation-token"
                                        aria-label={token.ariaLabel}
                                    >
                                        {token.content}
                                    </li>
                                ))}
                            </ul>
                        ) : null}
                    </div>
                </div>

                <hr className="regulation-phase__divider"/>

                {regulationTokens.length > 0 ? (
                    <div
                        className="regulation-phase__regulation"
                        aria-label={t('phaseRegulationAria')}
                    >
                        <ul
                            className="regulation-phase__rules regulation-phase__rules--row"
                            aria-label={t('tokens.aria')}
                        >
                            {regulationTokens.map((token) => (
                                <li
                                    key={token.key}
                                    className="regulation-token"
                                    aria-label={token.ariaLabel}
                                >
                                    {token.content}
                                </li>
                            ))}
                        </ul>
                    </div>
                ) : null}
            </div>

            <div className="regulation-phase__footer">
                <TextLink to={structureHref}>{t('openInStructure')}</TextLink>
            </div>
        </article>
    )
}

function inferPhaseKind(
    stage: OrganisationStageHubSummary,
): 'groups' | 'cup' | 'championship' {
    if ((stage.groupCount ?? 0) > 0) {
        return 'groups'
    }
    if ((stage.roundCount ?? 0) > 0 || stage.hasTieFormat) {
        return 'cup'
    }
    return 'championship'
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
        <div className="regulation-schematic__cards">
          {Array.from({ length: shown }, (_, i) => (
            <div
              key={i}
              className={`regulation-schematic__card regulation-schematic__card--${i % 4}`}
            >
              <span className="regulation-schematic__card-label">
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
      </div>
    )
  }

  if (roundCount > 0 || stage.hasTieFormat) {
    // Even count for a clean bracket (round up to next power-of-two look, max 8).
    const leaves = 2 ** Math.ceil(Math.log2(stage.teamCount));
    return (
      <div
        className="regulation-schematic regulation-schematic--bracket"
        aria-label={t('schematic.bracket')}
      >
        <svg
          className="regulation-schematic__wire"
          viewBox="0 0 120 64"
          width="120"
          height="64"
          aria-hidden="true"
        >
          {Array.from({ length: leaves }, (_, i) => {
            const y = 6 + (i * (52 / Math.max(leaves - 1, 1)))
            return (
              <g key={`leaf-${i}`}>
                <circle cx="8" cy={y} r="2.5" className="regulation-schematic__wire-node" />
                <path
                  d={`M 10.5 ${y} H 36`}
                  className="regulation-schematic__wire-line"
                />
              </g>
            )
          })}
          {Array.from({ length: leaves / 2 }, (_, i) => {
            const y1 = 6 + (i * 2 * (52 / Math.max(leaves - 1, 1)))
            const y2 = 6 + ((i * 2 + 1) * (52 / Math.max(leaves - 1, 1)))
            const mid = (y1 + y2) / 2
            return (
              <g key={`q-${i}`}>
                <path
                  d={`M 36 ${y1} V ${y2} M 36 ${mid} H 64`}
                  className="regulation-schematic__wire-line"
                />
                <circle
                  cx="66"
                  cy={mid}
                  r="2.25"
                  className="regulation-schematic__wire-node"
                />
              </g>
            )
          })}
          
        </svg>
        <span className="regulation-schematic__trophy" aria-hidden="true">
          <LucideIcon icon={Trophy} size="sm" />
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
      <div className="regulation-schematic__league">
        {Array.from(
          { length: Math.min(Math.max(stage.teamCount, 0), 6) },
          (_, i) => (
            <span key={i} className="regulation-schematic__league-row" />
          ),
        )}
      </div>
    </div>
  )
}

function buildStructureTokens(
    stage: OrganisationStageHubSummary,
    t: Translate,
): RuleToken[] {
    const tokens: RuleToken[] = []

    if (stage.hasQualificationRules && stage.qualificationPathCount > 0) {
        tokens.push({
            key: 'qualification',
            ariaLabel: t('tokens.qualification', {
                count: stage.qualificationPathCount,
            }),
            content: (
                <>
                    <LucideIcon icon={ArrowUpRight} size="sm"/>
                    <span
                        className="regulation-token__flow"
                        title={t('tokens.qualificationTip', {
                            from: stage.teamCount,
                            count: stage.qualificationPathCount,
                        })}
                    >
            <span className="regulation-chip">{stage.teamCount}</span>
            <ArrowRight size={14} strokeWidth={2.25} aria-hidden="true"/>
            <span className="regulation-chip regulation-chip--accent">
              {stage.qualificationPathCount}
            </span>
            <span className="regulation-token__caption">
              {t('tokens.qualifyOut')}
            </span>
          </span>
                </>
            ),
        })
    }

    if (stage.hasProgressionRules && stage.progressionPathCount > 0) {
        tokens.push({
            key: 'progression',
            ariaLabel: t('tokens.progression', {
                count: stage.progressionPathCount,
            }),
            content: (
                <>
                    <LucideIcon icon={ArrowRight} size="sm"/>
                    <span
                        className="regulation-chip"
                        title={t('tokens.progressionTip', {
                            count: stage.progressionPathCount,
                        })}
                    >
            {t('tokens.progression', {count: stage.progressionPathCount})}
          </span>
                </>
            ),
        })
    }

    return tokens
}

function buildRegulationTokens(
    stage: OrganisationStageHubSummary,
    t: Translate,
): RuleToken[] {
    const tokens: RuleToken[] = []
    const extraPeriods = stage.extraTimeNumberOfPeriods
    const extraMinutes = stage.extraTimeDurationPerPeriod
    const kicks = stage.penaltyInitialKicksPerTeam

    tokens.push({
        key: 'match',
        ariaLabel: t('tokens.matchAria', {
            periods: stage.numberOfPeriods,
            minutes: stage.durationPerPeriod,
        }),
        content: (
            <>
                <LucideIcon icon={Clock3} size="sm"/>
                <span className="regulation-token__chips">
          <span
              className="regulation-chip"
              title={t('tokens.matchChipTip', {
                  periods: stage.numberOfPeriods,
                  minutes: stage.durationPerPeriod,
              })}
          >
            {t('tokens.matchChip', {
                periods: stage.numberOfPeriods,
                minutes: stage.durationPerPeriod,
            })}
          </span>
                    {stage.hasExtraTime ? (
                        <span
                            className="regulation-chip regulation-chip--soft"
                            title={
                                extraPeriods != null && extraMinutes != null
                                    ? t('tokens.extraTimeTip', {
                                        periods: extraPeriods,
                                        minutes: extraMinutes,
                                    })
                                    : t('tokens.extraTimeTipSimple')
                            }
                        >
              {t('tokens.extraTime')}
            </span>
                    ) : null}
                    {stage.hasPenaltyShootout ? (
                        <span
                            className="regulation-chip regulation-chip--soft"
                            title={
                                kicks != null
                                    ? t('tokens.penaltiesTip', {count: kicks})
                                    : t('tokens.penaltiesTipSimple')
                            }
                        >
              {t('tokens.penalties')}
            </span>
                    ) : null}
        </span>
            </>
        ),
    })

    if (
        stage.hasStandingRules !== false &&
        stage.winPoints != null &&
        stage.drawPoints != null &&
        stage.lossPoints != null
    ) {
        tokens.push({
            key: 'standing',
            ariaLabel: t('tokens.standingAria', {
                win: stage.winPoints,
                draw: stage.drawPoints,
                loss: stage.lossPoints,
            }),
            content: (
                <>
                    <LucideIcon icon={Trophy} size="sm"/>
                    <span className="regulation-token__chips">
            <span
                className="regulation-chip regulation-chip--win"
                title={t('tokens.standingWinTip', {value: stage.winPoints})}
            >
              {stage.winPoints}
            </span>
            <span
                className="regulation-chip regulation-chip--draw"
                title={t('tokens.standingDrawTip', {value: stage.drawPoints})}
            >
              {stage.drawPoints}
            </span>
            <span
                className="regulation-chip regulation-chip--loss"
                title={t('tokens.standingLossTip', {value: stage.lossPoints})}
            >
              {stage.lossPoints}
            </span>
          </span>
                </>
            ),
        })
    }

    if (stage.hasDrawRules) {
        tokens.push({
            key: 'draw',
            ariaLabel: t('tokens.drawAutoTip'),
            content: (
                <>
                    <LucideIcon icon={Shuffle} size="sm"/>
                    <span
                        className="regulation-chip regulation-chip--soft"
                        title={t('tokens.drawAutoTip')}
                    >
            {t('tokens.drawAuto')}
          </span>
                </>
            ),
        })
    }

    if (stage.hasTieFormat && stage.numberOfLegs != null) {
        const twoLegs = stage.numberOfLegs > 1
        const label = twoLegs ? t('tokens.tieTwoLegs') : t('tokens.tieOneLeg')
        const tip = twoLegs ? t('tokens.tieTwoLegsTip') : t('tokens.tieOneLegTip')
        tokens.push({
            key: 'tie',
            ariaLabel: tip,
            content: (
                <>
                    {twoLegs ? (
                        <LucideIcon icon={ArrowLeftRight} size="sm"/>
                    ) : (
                        <LucideIcon icon={Medal} size="sm"/>
                    )}
                    <span className="regulation-chip regulation-chip--soft" title={tip}>
            {label}
          </span>
                </>
            ),
        })
    }

    return tokens
}

function criterionLabel(criterion: RankingCriterion, t: Translate) {
    return t(`criteria.${criterion}`, {defaultValue: criterion})
}

function periodLabel(n: number, t: Translate) {
    if (n === 1) {
        return t('matchTimeline.periodFirst')
    }
    if (n === 2) {
        return t('matchTimeline.periodSecond')
    }
    return t('matchTimeline.periodNth', {n})
}
