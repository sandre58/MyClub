import type { ReactNode } from 'react'
import type { LabMatch, LabTeam } from './labData'

/** Crest d'équipe — initiales sur teinte d'équipe (données fictives). */
export function Crest({ team, className }: { team: LabTeam; className?: string }) {
  return (
    <span
      className={['dlab-crest', className].filter(Boolean).join(' ')}
      style={{ ['--crest' as string]: team.hue }}
      aria-hidden="true"
    >
      {team.short}
    </span>
  )
}

/**
 * En-tête de panneau : [icône neutre] + titre + méta à droite (recette V9).
 * L'icône est optionnelle : uniquement quand elle encode la famille du
 * contenu (équipes, règlement, calendrier…), jamais décorative.
 */
export function PanelHead({
  title,
  aside,
  icon,
}: {
  title: string
  aside?: ReactNode
  icon?: ReactNode
}) {
  return (
    <div className="dlab-panel-head">
      <h3>
        {icon ? (
          <span className="dlab-panel-head__icon" aria-hidden="true">
            {icon}
          </span>
        ) : null}
        {title}
      </h3>
      {aside ? <span className="dlab-eyebrow">{aside}</span> : null}
    </div>
  )
}

/**
 * Ligne match état-driven : état normal implicite (score seul),
 * état notable explicite (pill / heure / minute).
 */
export function MatchRow({ match }: { match: LabMatch }) {
  return (
    <div className="dlab-match-row">
      <span className="dlab-match-row__team dlab-match-row__team--home">
        <span className="dlab-match-row__name">{match.home.name}</span>
        <Crest team={match.home} />
      </span>

      <ScoreCell match={match} />

      <span className="dlab-match-row__team dlab-match-row__team--away">
        <Crest team={match.away} />
        <span className="dlab-match-row__name">{match.away.name}</span>
      </span>

      <span className="dlab-match-row__aside">
        <MatchAside match={match} />
      </span>
    </div>
  )
}

function ScoreCell({ match }: { match: LabMatch }) {
  if (match.state === 'played' || match.state === 'live') {
    return (
      <span className="dlab-match-row__score dlab-num">
        {match.homeScore}
        <span aria-hidden="true">–</span>
        {match.awayScore}
      </span>
    )
  }
  return (
    <span className="dlab-match-row__score" data-muted="true">
      {match.state === 'upcoming' ? (match.time ?? '—') : 'vs'}
    </span>
  )
}

function MatchAside({ match }: { match: LabMatch }) {
  switch (match.state) {
    case 'live':
      return (
        <span className="dlab-status-live">
          <span className="dlab-live-dot" />
          {match.minute}
        </span>
      )
    case 'needsResult':
      return (
        <span className="ds-status ds-status--context ds-status--rounded ds-status--soft ds-status--tone-attention">
          Résultat à saisir
        </span>
      )
    case 'postponed':
      return (
        <span className="ds-status ds-status--context ds-status--rounded ds-status--soft ds-status--tone-neutral">
          Reporté
        </span>
      )
    case 'upcoming':
      return <span>{match.venue ?? ''}</span>
    default:
      return <span aria-hidden="true">›</span>
  }
}
