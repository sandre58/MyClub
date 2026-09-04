import {
  MatchRow as DsMatchRow,
  MatchRowScore,
} from '../design-system/components/MatchRow'
import { PanelHead } from '../design-system/components/PanelHead'
import { TeamCrest } from '../design-system/TeamCrest'
import type { LabMatch, LabTeam } from './labData'

export { PanelHead }

/** Crest d'équipe lab — délègue à TeamCrest (pas de 2ᵉ implémentation). */
export function Crest({ team, className }: { team: LabTeam; className?: string }) {
  return (
    <TeamCrest
      name={team.name}
      primaryColor={team.hue}
      className={className}
      size="sm"
    />
  )
}

/**
 * Ligne match lab — wraps DS MatchRow with fictive data.
 */
export function MatchRow({ match }: { match: LabMatch }) {
  return (
    <DsMatchRow
      home={{ name: match.home.name, crest: <Crest team={match.home} /> }}
      away={{ name: match.away.name, crest: <Crest team={match.away} /> }}
      score={<LabScoreCell match={match} />}
      aside={<LabMatchAside match={match} />}
    />
  )
}

function LabScoreCell({ match }: { match: LabMatch }) {
  if (match.state === 'played' || match.state === 'live') {
    return (
      <MatchRowScore home={match.homeScore ?? 0} away={match.awayScore ?? 0} />
    )
  }
  return (
    <MatchRowScore
      home={match.state === 'upcoming' ? (match.time ?? '—') : 'vs'}
      away=""
      muted
    />
  )
}

function LabMatchAside({ match }: { match: LabMatch }) {
  switch (match.state) {
    case 'live':
      return (
        <span className="ds-status-live">
          <span className="ds-live-dot" />
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
      return null
  }
}
