import type { CompositionQualifierRule } from './structureCompositionQualifierFeeds';

const BADGE_MAX_LENGTH = 30;

/**
 * Compact provenance chip in Équipes band (inter-phase inbound only).
 * Place + group: `1er · Gr. A`. Progression: `Huitièmes · P1` (phase·PairKey; outcome = chip tone + icon).
 */
export function compactCompositionProvenanceBadge(
  peerName: string,
  rule: Pick<
    CompositionQualifierRule,
    'badge' | 'context' | 'extra' | 'family'
  >,
): string {
  const extra = rule.extra?.trim() ?? '';
  const peer = shortenPeerName(peerName);
  const core =
    rule.family === 'result'
      ? formatResultProvenance(peer, rule.badge, rule.context)
      : formatPlaceProvenance(peer, rule.badge, rule.context);
  const joined = extra ? `${core} · ${extra}` : core;
  return capLength(joined, BADGE_MAX_LENGTH);
}

function formatPlaceProvenance(
  _peer: string,
  badge: string,
  context: string | null | undefined,
): string {
  const rank = badge.trim();
  const group = compactGroupLabel(context);
  if (group) {
    return `${rank} · ${group}`;
  }
  const ctx = shortenPlaceContext(context);
  if (rank && ctx) {
    return `${rank} · ${ctx}`;
  }
  return rank || ctx;
}

function formatResultProvenance(
  peer: string,
  _badge: string,
  context: string | null | undefined,
): string {
  const pairKey = extractPairKeyContext(context);
  return pairKey ? gluePhaseAndPair(peer, pairKey) : peer;
}

/** Tooltip: `Vainqueur · {phase} · P1` (outcome before phase). */
export function fullInterPhaseProvenanceLabel(
  peerName: string,
  rule: Pick<
    CompositionQualifierRule,
    'badge' | 'context' | 'extra' | 'family'
  >,
): string {
  const peer = peerName.trim();
  const context = rule.context?.trim() ?? '';
  const extra = rule.extra?.trim() ?? '';
  if (rule.family === 'result') {
    return [rule.badge.trim(), peer, context, extra].filter(Boolean).join(' · ');
  }
  const pathParts = [rule.badge, rule.context, rule.extra].filter(Boolean);
  const pathLabel = pathParts.join(' · ');
  return pathLabel ? `${peer} · ${pathLabel}` : peer;
}

export function progressionOutcomeFromRule(
  rule: Pick<CompositionQualifierRule, 'badge' | 'progressionOutcome' | 'family'>,
): 'Winner' | 'Loser' | null {
  if (rule.family !== 'result') {
    return null;
  }
  if (rule.progressionOutcome === 'Loser') {
    return 'Loser';
  }
  if (rule.progressionOutcome === 'Winner') {
    return 'Winner';
  }
  const badge = rule.badge.trim();
  if (badge.startsWith('Perdant') || /^loser/i.test(badge)) {
    return 'Loser';
  }
  if (badge.startsWith('Vainqueur') || /^winner/i.test(badge)) {
    return 'Winner';
  }
  return null;
}

function gluePhaseAndPair(phase: string, pairKey: string): string {
  if (!phase) {
    return pairKey;
  }
  if (!pairKey) {
    return phase;
  }
  return `${phase} · ${pairKey}`;
}

function compactGroupLabel(context: string | null | undefined): string | null {
  const trimmed = context?.trim() ?? '';
  const groupLike = trimmed.match(/^(?:Groupe|Poule|Group)\s+(.+)$/i);
  if (groupLike?.[1]) {
    const id = groupLike[1].trim();
    return id ? `Gr. ${id}` : null;
  }
  return null;
}

function shortenPlaceContext(context: string | null | undefined): string {
  const trimmed = context?.trim() ?? '';
  if (!trimmed) {
    return '';
  }
  if (trimmed === 'Classement' || /^overall/i.test(trimmed)) {
    return 'Cls.';
  }
  const meilleurs = trimmed.match(/^Meilleurs?\s+(\S+)/i);
  if (meilleurs?.[1]) {
    return `Mlr. ${meilleurs[1]}`;
  }
  return trimmed.length > 10 ? `${trimmed.slice(0, 9)}…` : trimmed;
}

function extractPairKeyContext(context: string | null | undefined): string {
  const trimmed = context?.trim() ?? '';
  if (!trimmed) {
    return '';
  }
  const pairAndMatch = trimmed.match(/^(.*?)\s*·\s*Match\s*#(\d+)$/i);
  if (pairAndMatch) {
    return pairAndMatch[1].trim();
  }
  const matchOnly = trimmed.match(/Match\s*#(\d+)/i);
  if (matchOnly) {
    const beforeSep = trimmed.split('·')[0]?.trim() ?? '';
    if (beforeSep && !/match/i.test(beforeSep)) {
      return beforeSep;
    }
    return '';
  }
  if (trimmed.includes('·')) {
    return trimmed
      .split('·')
      .map((part) => extractPairKeyContext(part.trim()))
      .filter(Boolean)
      .join('·');
  }
  return trimmed;
}

/** Shared mono- and multi-peer abbreviation (strip leading "Phase de"). */
export function shortenPeerName(peerName: string): string {
  const trimmed = peerName.trim();
  if (!trimmed) {
    return '';
  }
  const withoutPhase = trimmed.replace(/^Phase\s+(?:de\s+)?/i, '');
  const candidate = withoutPhase.length > 0 ? withoutPhase : trimmed;
  return candidate.length > 9 ? `${candidate.slice(0, 8)}…` : candidate;
}

function capLength(value: string, max: number): string {
  if (value.length <= max) {
    return value;
  }
  const pairMatch = value.match(/^(.*)·(P\d+.*)$/);
  if (pairMatch) {
    const [, phase, pair] = pairMatch;
    const suffix = `·${pair}`;
    const budget = max - suffix.length;
    if (budget > 0) {
      const shortPhase =
        phase.length > budget ? `${phase.slice(0, budget - 1)}…` : phase;
      const attempt = `${shortPhase}${suffix}`;
      if (attempt.length <= max) {
        return attempt;
      }
    }
  }
  return `${value.slice(0, max - 1)}…`;
}
