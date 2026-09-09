import {useQuery} from '@tanstack/react-query'
import {
    ArrowLeftRight,
    ArrowRight,
    ArrowUpRight,
    Clock3,
    Dices,
    Goal,
    Handshake,
    Layers,
    MapPin,
    PlayingCardsFan,
    Shuffle,
    Sigma,
    ShieldBan,
    Timer,
    Podium,
    Trophy,
    Volleyball,
    type LucideIcon as LucideGlyph,
} from 'lucide-react'
import {useState, type ReactNode} from 'react'
import {useTranslation} from 'react-i18next'
import {useParams} from 'react-router-dom'
import {fetchOrganisationView} from '../api'
import {Alert} from '../design-system/components/Alert'
import {Chip, type ChipTone} from '../design-system/components/Chip'
import {Meter, type MeterTone} from '../design-system/components/Meter'
import {FormSection} from '../design-system/components/FormSection'
import {PageHead} from '../design-system/components/PageHead'
import {TextLink} from '../design-system/components/TextLink'
import {LucideIcon} from '../design-system/icons/Icon'
import {PencilIcon, PersonIcon} from '../design-system/icons/overviewIcons'
import {queryKeys} from '../queryKeys'
import {ErrorState, LoadingState, StageStatusBadge, StatusBadge} from '../ui'
import type {
    DisciplinaryType,
    OrganisationPlacementAward,
    OrganisationRegulationSummary,
    OrganisationStageHubSummary,
    RankingCriterion,
    StructureFormatKind,
} from '../types'
import {RegulationEditorDialog} from './RegulationEditorDialog'
import {
    isPartOverridden,
    isStagePersonalized,
} from './regulationImpact'
import './regulation.css'
import {stageStatusLabel} from "../i18n/enumLabels.ts";

type Translate = (key: string, options?: Record<string, unknown>) => string

type PhaseFlow = {
    key: string
    content: ReactNode
    ariaLabel: string
}

/**
 * Règlement — hub lecture + édition cadre.
 * Vocabulaire mockup riche, tokens DS (surface, brand, typo).
 */

// —— Page ——

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
    const showStandingTile = stages.some((stage) => stage.hasStandingRules === true)

    return (
        <main id="main" className="page page--regulation">
            <div className="regulation">
                <PageHead
                    title={t('title')}
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
                            onClick={() => setEditorOpen(true)}
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
                    <div className="regulation-frame__stack">
                        <EntriesTile regulation={regulation}/>
                        <DisciplineTile regulation={regulation}/>
                    </div>
                    <MatchTile regulation={regulation}/>
                    {showStandingTile ? (
                        <StandingTile regulation={regulation}/>
                    ) : null}
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


// —— Cadre: shared card chrome ——

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
        <FormSection icon={icon} title={title}>
            {children}
        </FormSection>
    )
}


// —— Cadre: Equipes ——

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
            <Alert tone="info" role="status">
                {t('capacity.notice', {min, max})}
            </Alert>
        </FrameCard>
    )
}


// —— Cadre: Match ——

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

                        <div className="regulation-match__extra">
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
            </div>
        </FrameCard>
    )
}


// —— Cadre: Disciplinaire ——

function DisciplineTile({
                            regulation,
                        }: {
    regulation: OrganisationRegulationSummary
}) {
    const {t} = useTranslation('regulation')
    const types = regulation.allowedTypes ?? []

    return (
        <FrameCard
            icon={<LucideIcon icon={PlayingCardsFan} size="md"/>}
            title={t('families.discipline')}
        >
            <p className="regulation-discipline__subtitle">{t('discipline.subtitle')}</p>
            {types.length === 0 ? (
                <p className="regulation-discipline__empty" role="status">
                    {t('discipline.empty')}
                </p>
            ) : (
                <ul className="regulation-cards" aria-label={t('discipline.aria')}>
                    {types.map((type) => (
                        <li key={type} className="regulation-cards__item">
                            <span
                                className={`regulation-card-token regulation-card-token--${type.toLowerCase()}`}
                                title={disciplineLabel(type, t)}
                                aria-label={disciplineLabel(type, t)}
                                role="img"
                            >
                <span className="regulation-card-token__face" aria-hidden="true"/>
              </span>
                        </li>
                    ))}
                </ul>
            )}
        </FrameCard>
    )
}


// —— Cadre: Match clock helpers ——

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


// —— Cadre: Classement ——

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
    const forfeitWinner = regulation.forfeitWinnerGoals
    const forfeitLoser = regulation.forfeitLoserGoals
    const showForfeit = forfeitWinner != null && forfeitLoser != null

    return (
        <FrameCard
            icon={<LucideIcon icon={Podium} size="md"/>}
            title={t('families.standing')}
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

                <div className="regulation-standing__side">
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

                    {showForfeit ? (
                        <div
                            className="regulation-forfeit-score"
                            aria-label={t('forfeit.aria', {
                                winner: forfeitWinner,
                                loser: forfeitLoser,
                            })}
                        >
                            <p className="regulation-standing__heading">{t('forfeit.heading')}</p>
                            <p className="regulation-forfeit-score__hint">{t('forfeit.hint')}</p>
                            <div className="regulation-forfeit-score__board" aria-hidden="true">
                <span className="regulation-forfeit-score__goals regulation-forfeit-score__goals--win">
                  {forfeitWinner}
                </span>
                                <span className="regulation-forfeit-score__sep">–</span>
                                <span className="regulation-forfeit-score__goals">
                  {forfeitLoser}
                </span>
                            </div>
                        </div>
                    ) : null}
                </div>
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
            <Meter ratio={ratio} tone={meterTone} size="lg" clip aria-hidden="true"/>
        </li>
    )
}


// —— Phases: tile ——

function PersonalizedBadge() {
    const { t } = useTranslation('regulation')
    return (
        <StatusBadge tone="warn" density="compact">
            {t('personalized')}
        </StatusBadge>
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
    const flows = buildPhaseFlows(stage, t)
    const ruleColumns = buildPhaseRuleColumns(stage, t)
    const personalized = isStagePersonalized(stage)

    return (
        <article className="regulation-phase">
            <header className="regulation-phase__head">
                <div className="regulation-phase__head-main">
                    <span className="regulation-phase__ordinal" aria-hidden="true">
                        {ordinal}
                    </span>
                    <h3 className="regulation-card__title">{stage.name}</h3>
                </div>
                <span className="regulation-phase__head-meta">
                    <StageStatusBadge status={stage.status} density="compact"/>
                    {personalized ? <PersonalizedBadge /> : null}
                </span>
            </header>

            <div
                className="regulation-phase__top"
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

                <div className="regulation-phase__schematic">
                    <PhaseSchematic stage={stage}/>
                </div>

                {flows.length > 0 ? (
                    <ul className="regulation-phase__flows">
                        {flows.map((flow) => (
                            <li
                                key={flow.key}
                                className="regulation-flow"
                                aria-label={flow.ariaLabel}
                            >
                                {flow.content}
                            </li>
                        ))}
                    </ul>
                ) : (
                    <div className="regulation-phase__flows regulation-phase__flows--empty"/>
                )}
            </div>

            {ruleColumns.length > 0 ? (
                <>
                    <hr className="regulation-phase__rule"/>
                    <div
                        className="regulation-phase__bottom"
                        aria-label={t('phaseRegulationAria')}
                    >
                        <div className="regulation-rule-cols" aria-label={t('tokens.aria')}>
                            {ruleColumns.map((column) => (
                                <section key={column.key} className="regulation-rule-col">
                                    <h4 className="regulation-rule-col__title">
                    <span className="regulation-rule-col__icon" aria-hidden="true">
                      <LucideIcon icon={column.icon} size="sm"/>
                    </span>
                                        {column.title}
                                    </h4>
                                    {column.chips && column.chips.length > 0 ? (
                                        <div className="regulation-rule-col__chips">
                                            {column.chips.map((chip) => (
                                                <Chip
                                                    key={chip.key}
                                                    tone={chip.tone}
                                                    title={chip.title}
                                                    className={
                                                        chip.overridden
                                                            ? 'regulation-rule-chip--overridden'
                                                            : undefined
                                                    }
                                                >
                                                    {chip.label}
                                                </Chip>
                                            ))}
                                        </div>
                                    ) : null}
                                    {column.items.length > 0 ? (
                                        <ul className="regulation-rule-list">
                                            {column.items.map((item) => (
                                                <li
                                                    key={item.key}
                                                    className={[
                                                        'regulation-rule-list__item',
                                                        item.overridden
                                                            ? 'regulation-rule-list__item--overridden'
                                                            : null,
                                                    ]
                                                        .filter(Boolean)
                                                        .join(' ')}
                                                    title={item.title}
                                                >
                                                    {item.index != null ? (
                                                        <span
                                                            className="regulation-criteria__n"
                                                            aria-hidden="true"
                                                        >
                                                            {item.index}
                                                        </span>
                                                    ) : item.icon ? (
                                                        <span
                                                            className="regulation-rule-list__mark"
                                                            aria-hidden="true"
                                                        >
                                                            <LucideIcon
                                                                icon={item.icon}
                                                                size="sm"
                                                            />
                                                        </span>
                                                    ) : null}
                                                    <span className="regulation-rule-list__label">
                                                        {item.label}
                                                    </span>
                                                </li>
                                            ))}
                                        </ul>
                                    ) : null}
                                </section>
                            ))}
                        </div>
                    </div>
                </>
            ) : null}

            <div className="regulation-phase__footer">
                <TextLink to={structureHref}>{t('openInStructure')}</TextLink>
            </div>
        </article>
    )
}

type PhaseRuleChip = {
    key: string
    label: ReactNode
    tone?: ChipTone
    title?: string
    /** Unbound heritable part — accent label on the phase tile. */
    overridden?: boolean
}

type PhaseRuleItem = {
    key: string
    label: ReactNode
    title?: string
    /** Numbered ranking-criteria pill when set. */
    index?: number
    icon?: LucideGlyph
    /** Unbound heritable part — accent label on the phase tile. */
    overridden?: boolean
}

type PhaseRuleColumn = {
    key: string
    title: string
    icon: LucideGlyph
    chips?: PhaseRuleChip[]
    items: PhaseRuleItem[]
}


// —— Phases: schematic ——

function inferPhaseKind(
    stage: OrganisationStageHubSummary,
): 'groups' | 'cup' | 'championship' | 'swiss' {
    const kind: StructureFormatKind | null | undefined = stage.formatKind
    if (kind === 'Swiss') {
        return 'swiss'
    }
    if (kind === 'Groups' || (stage.groupCount ?? 0) > 0) {
        return 'groups'
    }
    if (kind === 'Cup' || (stage.roundCount ?? 0) > 0 || stage.hasTieFormat) {
        return 'cup'
    }
    return 'championship'
}

function PhaseSchematic({stage}: { stage: OrganisationStageHubSummary }) {
    const {t} = useTranslation('regulation')
    const kind = inferPhaseKind(stage)
    const groupCount = stage.groupCount ?? 0
    const roundCount = stage.roundCount ?? 0

    if (kind === 'groups' || groupCount > 0) {
        const shown = Math.min(Math.max(groupCount, 1), 4)
        const perGroup = Math.max(
            1,
            Math.round(stage.teamCount / Math.max(groupCount, 1)),
        )
        return (
            <div
                className="regulation-schematic regulation-schematic--groups"
                aria-label={t('schematic.groups', {count: groupCount || shown})}
            >
                <div className="regulation-schematic__cards">
                    {Array.from({length: shown}, (_, i) => (
                        <div
                            key={i}
                            className={`regulation-schematic__card regulation-schematic__card--${i % 4}`}
                        >
              <span className="regulation-schematic__card-label">
                {String.fromCharCode(65 + i)}
              </span>
                            <div className="regulation-schematic__dots">
                                {Array.from({length: Math.min(perGroup, 4)}, (_, j) => (
                                    <span key={j} className="regulation-schematic__dot"/>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        )
    }

    if (kind === 'swiss') {
        const knownRounds = stage.swissRoundCount
        if (knownRounds == null || knownRounds <= 0) {
            return (
                <div
                    className="regulation-schematic regulation-schematic--swiss"
                    aria-label={t('schematic.swissGeneric')}
                >
                    <div className="regulation-schematic__swiss-rounds">
                        <div className="regulation-schematic__swiss-round">
                            <span className="regulation-schematic__swiss-pair"/>
                            <span className="regulation-schematic__swiss-pair"/>
                        </div>
                    </div>
                </div>
            )
        }
        const rounds = Math.min(knownRounds, 5)
        return (
            <div
                className="regulation-schematic regulation-schematic--swiss"
                aria-label={t('schematic.swiss', {rounds: knownRounds})}
            >
                <div className="regulation-schematic__swiss-rounds">
                    {Array.from({length: rounds}, (_, i) => (
                        <div key={i} className="regulation-schematic__swiss-round">
                            <span className="regulation-schematic__swiss-pair"/>
                            <span className="regulation-schematic__swiss-pair"/>
                        </div>
                    ))}
                </div>
            </div>
        )
    }

    if (kind === 'cup' || roundCount > 0 || stage.hasTieFormat) {
        const leaves = Math.min(
            8,
            2 ** Math.max(1, Math.ceil(Math.log2(Math.max(stage.teamCount, 2)))),
        )
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
                    {Array.from({length: leaves}, (_, i) => {
                        const y = 6 + i * (52 / Math.max(leaves - 1, 1))
                        return (
                            <g key={`leaf-${i}`}>
                                <circle
                                    cx="8"
                                    cy={y}
                                    r="2.5"
                                    className="regulation-schematic__wire-node"
                                />
                                <path
                                    d={`M 10.5 ${y} H 36`}
                                    className="regulation-schematic__wire-line"
                                />
                            </g>
                        )
                    })}
                    {Array.from({length: leaves / 2}, (_, i) => {
                        const y1 = 6 + i * 2 * (52 / Math.max(leaves - 1, 1))
                        const y2 = 6 + (i * 2 + 1) * (52 / Math.max(leaves - 1, 1))
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
          <LucideIcon icon={Trophy} size="sm"/>
        </span>
            </div>
        )
    }

    return (
        <div
            className="regulation-schematic regulation-schematic--championship"
            aria-label={t('schematic.championship', {teams: stage.teamCount})}
        >
            <div className="regulation-schematic__league">
                {Array.from(
                    {length: Math.min(Math.max(stage.teamCount, 3), 6)},
                    (_, i) => (
                        <span key={i} className="regulation-schematic__league-row">
              <span className="regulation-schematic__league-rank" aria-hidden="true">
                {i + 1}
              </span>
              <span className="regulation-schematic__league-bar" aria-hidden="true"/>
            </span>
                    ),
                )}
            </div>
        </div>
    )
}

/** Splice helpers into RegulationPage.tsx — builders + labels. */

// —— Phases: flow + rule builders ——

function buildPhaseFlows(
    stage: OrganisationStageHubSummary,
    t: Translate,
): PhaseFlow[] {
    const flows: PhaseFlow[] = []

    if (stage.hasQualificationRules && stage.qualificationPathCount > 0) {
        const tip = t('tokens.qualificationTip', {
            from: stage.teamCount,
            count: stage.qualificationPathCount,
        })
        flows.push({
            key: 'qualification',
            ariaLabel: t('tokens.qualification', {
                count: stage.qualificationPathCount,
            }),
            content: (
                <>
                    <span className="regulation-rule-list__mark" aria-hidden="true">
                        <LucideIcon icon={ArrowUpRight} size="sm"/>
                    </span>
                    <span className="regulation-flow__row" title={tip}>
                        <Chip tone="neutral" title={tip}>
                            {stage.teamCount}
                        </Chip>
                        <ArrowRight size={14} strokeWidth={2.25} aria-hidden="true"/>
                        <Chip tone="accent" title={tip}>
                            {stage.qualificationPathCount}
                        </Chip>
                        <span className="regulation-flow__caption">
                            {t('tokens.qualifyOut')}
                        </span>
                    </span>
                </>
            ),
        })
    }

    if (stage.hasProgressionRules && stage.progressionPathCount > 0) {
        const tip = t('tokens.progressionTip', {
            count: stage.progressionPathCount,
        })
        flows.push({
            key: 'progression',
            ariaLabel: t('tokens.progression', {
                count: stage.progressionPathCount,
            }),
            content: (
                <>
                    <span className="regulation-rule-list__mark" aria-hidden="true">
                        <LucideIcon icon={ArrowRight} size="sm"/>
                    </span>
                    <span className="regulation-flow__row" title={tip}>
                        <Chip tone="accent" title={tip}>
                            {stage.progressionPathCount}
                        </Chip>
                        <span className="regulation-flow__caption">
                            {t('tokens.progressionOut', {
                                count: stage.progressionPathCount,
                            })}
                        </span>
                    </span>
                </>
            ),
        })
    }

    const awards = stage.placementAwards ?? []
    if (
        stage.hasPlacementAwardRules &&
        (awards.length > 0 || (stage.placementAwardCount ?? 0) > 0)
    ) {
        const count = awards.length || stage.placementAwardCount || 0
        flows.push({
            key: 'placement',
            ariaLabel: t('tokens.placement', {count}),
            content: <PlacementAwardsList awards={awards} count={count} t={t}/>,
        })
    }

    return flows
}

function buildPhaseRuleColumns(
    stage: OrganisationStageHubSummary,
    t: Translate,
): PhaseRuleColumn[] {
    const columns: PhaseRuleColumn[] = []
    const matchDurationOverridden = isPartOverridden(stage, 'matchDuration')
    const extraTimeOverridden = isPartOverridden(stage, 'extraTime')
    const penaltiesOverridden = isPartOverridden(stage, 'penaltyShootout')
    const forfeitOverridden = isPartOverridden(stage, 'administrativeResult')
    const pointsOverridden = isPartOverridden(stage, 'points')
    const rankingOverridden = isPartOverridden(stage, 'rankingCriteria')

    const matchItems: PhaseRuleItem[] = [
        {
            key: 'duration',
            label: t('tokens.matchSummary', {
                periods: stage.numberOfPeriods,
                minutes: stage.durationPerPeriod,
            }),
            icon: Clock3,
            overridden: matchDurationOverridden,
        },
    ]
    if (stage.hasExtraTime) {
        const periods = stage.extraTimeNumberOfPeriods
        const minutes = stage.extraTimeDurationPerPeriod
        const hasDetail = periods != null && minutes != null
        matchItems.push({
            key: 'extra-time',
            label: (
                <LabelWithDetail
                    text={t('tokens.extraTime')}
                    detail={
                        hasDetail
                            ? t('tokens.extraTimeDetail', {periods, minutes})
                            : undefined
                    }
                />
            ),
            icon: Timer,
            title: hasDetail
                ? t('tokens.extraTimeTip', {periods, minutes})
                : t('tokens.extraTime'),
            overridden: extraTimeOverridden,
        })
    }
    if (stage.hasPenaltyShootout) {
        const kicks = stage.penaltyInitialKicksPerTeam
        matchItems.push({
            key: 'penalties',
            label: (
                <LabelWithDetail
                    text={t('tokens.penalties')}
                    detail={
                        kicks != null
                            ? t('tokens.penaltiesDetail', {count: kicks})
                            : undefined
                    }
                />
            ),
            icon: Goal,
            title:
                kicks != null
                    ? t('tokens.penaltiesTip', {count: kicks})
                    : t('tokens.penalties'),
            overridden: penaltiesOverridden,
        })
    }
    columns.push({
        key: 'match',
        title: t('columns.match'),
        icon: Volleyball,
        items: matchItems,
    })

    if (stage.hasTieFormat && stage.numberOfLegs != null) {
        const twoLegs = stage.numberOfLegs > 1
        const tieItems: PhaseRuleItem[] = [
            {
                key: 'legs',
                label: twoLegs ? t('tokens.tieTwoLegs') : t('tokens.tieOneLeg'),
                icon: twoLegs ? ArrowLeftRight : ArrowRight,
                title: twoLegs ? t('tokens.tieTwoLegsTip') : t('tokens.tieOneLegTip'),
            },
        ]
        if (stage.aggregateScoring) {
            tieItems.push({
                key: 'aggregate',
                label: t('tokens.tieAggregate'),
                icon: Sigma,
                title: t('tokens.tieAggregateTip'),
            })
        }
        if (stage.hasAwayGoalsRule) {
            tieItems.push({
                key: 'away-goals',
                label: t('tokens.tieAwayGoals'),
                icon: MapPin,
                title: t('tokens.tieAwayGoalsTip'),
            })
        }
        if (stage.hasTieExtraTime) {
            tieItems.push({
                key: 'tie-extra-time',
                label: t('tokens.tieExtraTime'),
                icon: Timer,
                title: t('tokens.tieExtraTimeTip'),
            })
        }
        if (stage.hasTiePenaltyShootout) {
            tieItems.push({
                key: 'tie-penalties',
                label: t('tokens.tiePenalties'),
                icon: Goal,
                title: t('tokens.tiePenaltiesTip'),
            })
        }
        columns.push({
            key: 'tie',
            title: t('columns.tie'),
            icon: Handshake,
            items: tieItems,
        })
    }

    if (stage.hasDrawRules) {
        const pots = stage.numberOfPots
        const seeds = stage.numberOfSeeds
        const constraints = stage.drawConstraints ?? []
        const items: PhaseRuleItem[] = [
            {
                key: 'mode',
                label:
                    stage.drawMode === 'Random' || stage.drawMode == null
                        ? t('tokens.drawRandom')
                        : t(`tokens.drawMode.${stage.drawMode}`, {
                              defaultValue: stage.drawMode,
                          }),
                icon: Dices,
                title: t('tokens.drawRandomTip'),
            },
        ]
        if (pots != null && pots > 0) {
            items.push({
                key: 'pots',
                label: (
                    <span className="regulation-rule-list__label-row">
                        <Chip tone="soft" title={t('tokens.drawPotsTip', {count: pots})}>
                            {pots}
                        </Chip>
                        <span>{t('tokens.drawPotsUnit', {count: pots})}</span>
                    </span>
                ),
                icon: Layers,
                title: t('tokens.drawPotsTip', {count: pots}),
            })
        }
        if (seeds != null && seeds > 0) {
            items.push({
                key: 'seeds',
                label: (
                    <span className="regulation-rule-list__label-row">
                        <Chip tone="soft" title={t('tokens.drawSeedsTip', {count: seeds})}>
                            {seeds}
                        </Chip>
                        <span>{t('tokens.drawSeedsUnit', {count: seeds})}</span>
                    </span>
                ),
                title: t('tokens.drawSeedsTip', {count: seeds}),
            })
        }
        for (const constraint of constraints) {
            const typeLabel = t(`tokens.drawConstraint.${constraint.type}`, {
                defaultValue: constraint.type,
            })
            const enforcementLabel = t(
                `tokens.drawEnforcement.${constraint.enforcement}`,
                {defaultValue: constraint.enforcement},
            )
            const max =
                constraint.maxPerGroup != null
                    ? t('tokens.drawConstraintMax', {count: constraint.maxPerGroup})
                    : null
            items.push({
                key: `constraint-${constraint.type}-${constraint.enforcement}-${constraint.maxPerGroup ?? 'x'}`,
                label: (
                    <span className="regulation-rule-list__label-row">
                        <span>{typeLabel}</span>
                        <span className="regulation-detail">
                            · {enforcementLabel}
                            {max ? ` · ${max}` : ''}
                        </span>
                    </span>
                ),
                title: `${typeLabel} · ${enforcementLabel}${max ? ` · ${max}` : ''}`,
            })
        }
        columns.push({
            key: 'draw',
            title: t('columns.draw'),
            icon: Shuffle,
            items,
        })
    }

    const classifies = stage.hasStandingRules === true
    const forfeitWinner = stage.forfeitWinnerGoals
    const forfeitLoser = stage.forfeitLoserGoals
    const showForfeit =
        classifies && forfeitWinner != null && forfeitLoser != null
    const showStandingPoints =
        classifies &&
        stage.winPoints != null &&
        stage.drawPoints != null &&
        stage.lossPoints != null

    if (showStandingPoints || showForfeit) {
        const criteria = showStandingPoints ? (stage.rankingCriteria ?? []) : []
        const items: PhaseRuleItem[] = criteria.map((criterion, index) => ({
            key: criterion,
            label: criterionLabel(criterion, t),
            index: index + 1,
            overridden: rankingOverridden,
        }))
        if (showForfeit) {
            items.push({
                key: 'forfeit',
                label: (
                    <span className="regulation-rule-list__label-row">
                        <span>&nbsp;{t('forfeit.heading')}</span>
                        <span className="regulation-detail">
                            {forfeitWinner}–{forfeitLoser}
                        </span>
                    </span>
                ),
                icon: ShieldBan,
                title: t('forfeit.aria', {
                    winner: forfeitWinner,
                    loser: forfeitLoser,
                }),
                overridden: forfeitOverridden,
            })
        }
        columns.push({
            key: 'standing',
            title: t('columns.standing'),
            icon: Trophy,
            chips: showStandingPoints
                ? [
                      {
                          key: 'win',
                          label: stage.winPoints,
                          tone: 'win',
                          title: t('tokens.standingWinTip', {value: stage.winPoints}),
                          overridden: pointsOverridden,
                      },
                      {
                          key: 'draw',
                          label: stage.drawPoints,
                          tone: 'draw',
                          title: t('tokens.standingDrawTip', {value: stage.drawPoints}),
                          overridden: pointsOverridden,
                      },
                      {
                          key: 'loss',
                          label: stage.lossPoints,
                          tone: 'loss',
                          title: t('tokens.standingLossTip', {value: stage.lossPoints}),
                          overridden: pointsOverridden,
                      },
                  ]
                : undefined,
            items,
        })
    }

    return columns
}

function PlacementAwardsList({
    awards,
    count,
    t,
}: {
    awards: OrganisationPlacementAward[]
    count: number
    t: Translate
}) {
    if (awards.length === 0) {
        return (
            <span className="regulation-flow__caption" title={t('tokens.placementTip', {count})}>
                {t('tokens.placement', {count})}
            </span>
        )
    }

    return (
        <ul
            className="regulation-placement"
            aria-label={t('tokens.placementTip', {count: awards.length})}
        >
            {awards.map((award) => {
                const outcome = t(`tokens.outcome.${award.outcome}`)
                const aria = t('tokens.placementPathAria', {
                    outcome,
                    rank: award.rank,
                })
                return (
                    <li
                        key={`${award.rank}-${award.outcome}`}
                        className="regulation-placement__item"
                        title={aria}
                        aria-label={aria}
                    >
                        <span className="regulation-criteria__n" aria-hidden="true">
                            {award.rank}
                        </span>
                        <span className="regulation-placement__text">
                            {outcome}
                            <span className="regulation-detail">
                                {t('tokens.placementRank', {rank: award.rank})}
                            </span>
                        </span>
                    </li>
                )
            })}
        </ul>
    )
}


// —— Shared labels ——

function LabelWithDetail({
    text,
    detail,
}: {
    text: string
    detail?: string
}) {
    return (
        <>
            {text}
            {detail ? <span className="regulation-detail">{detail}</span> : null}
        </>
    )
}

function criterionLabel(criterion: RankingCriterion, t: Translate) {
    return t(`criteria.${criterion}`, {defaultValue: criterion})
}

function disciplineLabel(type: DisciplinaryType, t: Translate) {
    return t(`discipline.${type}`, {defaultValue: type})
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
