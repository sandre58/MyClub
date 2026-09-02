/*
 * Design Lab — données fictives.
 *
 * Tout est statique et local : le lab est une référence visuelle,
 * pas une surface produit. Aucun appel API.
 */

export type LabLifecycle = 'preparation' | 'live' | 'done'

export type LabTeam = {
  id: string
  name: string
  short: string
  hue: string
}

export const labTeams: LabTeam[] = [
  { id: 'mon', name: 'AS Montval', short: 'MON', hue: '#1c67c4' },
  { id: 'roc', name: 'FC Rocheville', short: 'ROC', hue: '#b4232c' },
  { id: 'ver', name: 'US Verneuil', short: 'VER', hue: '#1b6b44' },
  { id: 'bri', name: 'Étoile de Brissac', short: 'BRI', hue: '#8a5200' },
  { id: 'cha', name: 'Olympique Charmes', short: 'CHA', hue: '#215a86' },
  { id: 'sab', name: 'Racing Sablons', short: 'SAB', hue: '#5a6270' },
]

const team = (id: string): LabTeam => {
  const found = labTeams.find((t) => t.id === id)
  if (!found) throw new Error(`unknown lab team: ${id}`)
  return found
}

export type LabMatchState =
  | 'upcoming'
  | 'live'
  | 'played'
  | 'needsResult'
  | 'postponed'

export type LabMatch = {
  id: string
  home: LabTeam
  away: LabTeam
  state: LabMatchState
  homeScore?: number
  awayScore?: number
  minute?: string
  time?: string
  venue?: string
}

export type LabRound = {
  id: string
  label: string
  date: string
  state: 'done' | 'current' | 'upcoming' | 'partial'
  matches: LabMatch[]
}

export const labRounds: LabRound[] = [
  {
    id: 'j1',
    label: 'Journée 1',
    date: 'Sam. 12 sept.',
    state: 'done',
    matches: [
      { id: 'j1m1', home: team('mon'), away: team('sab'), state: 'played', homeScore: 3, awayScore: 0 },
      { id: 'j1m2', home: team('roc'), away: team('bri'), state: 'played', homeScore: 2, awayScore: 1 },
      { id: 'j1m3', home: team('ver'), away: team('cha'), state: 'played', homeScore: 1, awayScore: 1 },
    ],
  },
  {
    id: 'j2',
    label: 'Journée 2',
    date: 'Sam. 19 sept.',
    state: 'partial',
    matches: [
      { id: 'j2m1', home: team('bri'), away: team('mon'), state: 'played', homeScore: 0, awayScore: 2 },
      { id: 'j2m2', home: team('cha'), away: team('roc'), state: 'played', homeScore: 1, awayScore: 2 },
      { id: 'j2m3', home: team('sab'), away: team('ver'), state: 'needsResult' },
    ],
  },
  {
    id: 'j3',
    label: 'Journée 3',
    date: 'Sam. 26 sept.',
    state: 'current',
    matches: [
      { id: 'j3m1', home: team('mon'), away: team('cha'), state: 'played', homeScore: 1, awayScore: 0 },
      { id: 'j3m2', home: team('roc'), away: team('ver'), state: 'live', homeScore: 2, awayScore: 1, minute: "67'" },
      { id: 'j3m3', home: team('bri'), away: team('sab'), state: 'upcoming', time: '17:30', venue: 'Stade des Aulnes' },
    ],
  },
  {
    id: 'j4',
    label: 'Journée 4',
    date: 'Sam. 3 oct.',
    state: 'upcoming',
    matches: [
      { id: 'j4m1', home: team('ver'), away: team('mon'), state: 'upcoming', time: '15:00', venue: 'Complexe R. Delorme' },
      { id: 'j4m2', home: team('sab'), away: team('roc'), state: 'upcoming', time: '15:00', venue: 'Stade municipal' },
      { id: 'j4m3', home: team('cha'), away: team('bri'), state: 'postponed' },
    ],
  },
  {
    id: 'j5',
    label: 'Journée 5',
    date: 'Sam. 10 oct.',
    state: 'upcoming',
    matches: [
      { id: 'j5m1', home: team('mon'), away: team('roc'), state: 'upcoming', time: '15:00' },
      { id: 'j5m2', home: team('bri'), away: team('ver'), state: 'upcoming', time: '15:00' },
      { id: 'j5m3', home: team('cha'), away: team('sab'), state: 'upcoming', time: '17:30' },
    ],
  },
]

export type LabStandingRow = {
  rank: number
  team: LabTeam
  played: number
  won: number
  drawn: number
  lost: number
  goalDiff: number
  points: number
  penalty?: number
  trend: 'up' | 'down' | 'flat'
  zone?: 'promotion' | 'relegation'
}

/* Cohérent avec labRounds : matchs joués uniquement (J2 SAB–VER non saisi, J3 ROC–VER en cours). */
export const labStandings: LabStandingRow[] = [
  { rank: 1, team: team('mon'), played: 3, won: 3, drawn: 0, lost: 0, goalDiff: 6, points: 9, trend: 'flat', zone: 'promotion' },
  { rank: 2, team: team('roc'), played: 2, won: 2, drawn: 0, lost: 0, goalDiff: 2, points: 6, trend: 'up', zone: 'promotion' },
  { rank: 3, team: team('ver'), played: 1, won: 0, drawn: 1, lost: 0, goalDiff: 0, points: 1, trend: 'flat' },
  { rank: 4, team: team('cha'), played: 3, won: 0, drawn: 1, lost: 2, goalDiff: -2, points: 0, penalty: 1, trend: 'down' },
  { rank: 5, team: team('bri'), played: 2, won: 0, drawn: 0, lost: 2, goalDiff: -3, points: 0, trend: 'down' },
  { rank: 6, team: team('sab'), played: 1, won: 0, drawn: 0, lost: 1, goalDiff: -3, points: 0, trend: 'down', zone: 'relegation' },
]

export type LabAttentionItem = {
  id: string
  tone: 'attention' | 'info'
  count?: number
  title: string
  detail: string
  action: string
}

export const labAttention: LabAttentionItem[] = [
  {
    id: 'results',
    tone: 'attention',
    count: 1,
    title: 'Résultat à saisir',
    detail: 'Racing Sablons — US Verneuil · Journée 2',
    action: 'Saisir le résultat',
  },
  {
    id: 'postponed',
    tone: 'info',
    count: 1,
    title: 'Match reporté à replanifier',
    detail: 'Olympique Charmes — Étoile de Brissac · Journée 4',
    action: 'Replanifier',
  },
]

export type LabEvent = {
  minute: string
  kind: 'goal' | 'sub' | 'card'
  side: 'home' | 'away'
  text: string
}

export const labMatchEvents: LabEvent[] = [
  { minute: "12'", kind: 'goal', side: 'home', text: 'But — K. Ferrand (1-0)' },
  { minute: "31'", kind: 'card', side: 'away', text: 'Avertissement — T. Lopes' },
  { minute: "44'", kind: 'goal', side: 'away', text: 'But — M. Diallo (1-1)' },
  { minute: "58'", kind: 'goal', side: 'home', text: 'But — K. Ferrand (2-1)' },
  { minute: "63'", kind: 'sub', side: 'home', text: 'Remplacement — J. Bosc ↔ L. Prat' },
]

export const labCompetitions = [
  {
    id: 'c1',
    name: 'Championnat des Vétérans — Automne 2026',
    status: 'En cours',
    statusTone: 'live' as const,
    scheduledStart: '2026-09-12',
    scheduledEnd: '2027-03-28',
  },
  {
    id: 'c2',
    name: 'Coupe du District U15',
    status: 'Préparation',
    statusTone: 'info' as const,
    scheduledStart: '2026-10-04',
  },
  {
    id: 'c3',
    name: 'Tournoi de Pentecôte 2026',
    status: 'Terminée',
    statusTone: 'done' as const,
    scheduledEnd: '2027-03-28',
  },
]
