import type { CompositionQualifierRule } from './structureCompositionQualifierFeeds';

const BADGE_MAX_LENGTH = 30;

/** Compact provenance chip in Équipes band; `full` stays exhaustive (tooltip). */
export function compactCompositionProvenanceBadge(
  peerName: string,
  rule: Pick<
    CompositionQualifierRule,
    'badge' | 'context' | 'extra' | 'family'
  >,
  distinctPeerCount: number,
): string {
  const seat = compactSeatPath(rule);
  const extra = rule.extra?.trim() ?? '';
  const peer =
    distinctPeerCount > 1 ? shortenPeerName(peerName) : null;
  const parts = [peer, seat, extra].filter(Boolean);
  const joined = parts.join(' · ');
  if (!joined) {
    return peerName.trim() || seat || extra;
  }
  return capLength(joined, BADGE_MAX_LENGTH);
}

function compactSeatPath(
  rule: Pick<CompositionQualifierRule, 'badge' | 'context' | 'family'>,
): string {
  const badge = shortenOutcomeBadge(rule.badge, rule.family);
  const context = shortenRuleContext(rule.context);
  if (badge && context) {
    if (rule.family === 'place' && context.length <= 6) {
      return `${badge}·${context}`;
    }
    return `${badge} · ${context}`;
  }
  return badge || context;
}

function shortenOutcomeBadge(badge: string, family: string | undefined): string {
  const trimmed = badge.trim();
  if (family !== 'result') {
    return trimmed;
  }
  if (trimmed.startsWith('Vainqueur')) {
    return 'Vain.';
  }
  if (trimmed.startsWith('Perdant')) {
    return 'Perd.';
  }
  return trimmed;
}

function shortenRuleContext(context: string | null | undefined): string {
  const trimmed = context?.trim() ?? '';
  if (!trimmed) {
    return '';
  }
  const pairAndMatch = trimmed.match(/^(.*?)\s*·\s*Match\s*#(\d+)$/i);
  if (pairAndMatch) {
    const pair = pairAndMatch[1].trim();
    return pair ? `${pair} #${pairAndMatch[2]}` : `#${pairAndMatch[2]}`;
  }
  if (trimmed === 'Classement') {
    return 'Cls.';
  }

  const groupLike = trimmed.match(/^(?:Groupe|Poule)\s+(.+)$/i);
  if (groupLike?.[1]) {
    return groupLike[1].trim();
  }

  const matchOnly = trimmed.match(/Match\s*#(\d+)/i);
  if (matchOnly) {
    const beforeSep = trimmed.split('·')[0]?.trim() ?? '';
    const pair =
      beforeSep && !/match/i.test(beforeSep) && beforeSep.length <= 8
        ? beforeSep
        : null;
    return pair ? `${pair} #${matchOnly[1]}` : `#${matchOnly[1]}`;
  }

  if (trimmed.includes('·')) {
    return trimmed
      .split('·')
      .map((part) => shortenRuleContext(part.trim()))
      .filter(Boolean)
      .join('·');
  }

  return trimmed.length > 10 ? `${trimmed.slice(0, 9)}…` : trimmed;
}

function shortenPeerName(peerName: string): string {
  const trimmed = peerName.trim();
  const withoutPhase = trimmed.replace(/^Phase\s+(?:de\s+)?/i, '');
  const candidate = withoutPhase.length > 0 ? withoutPhase : trimmed;
  return candidate.length > 12 ? `${candidate.slice(0, 11)}…` : candidate;
}

function capLength(value: string, max: number): string {
  if (value.length <= max) {
    return value;
  }
  return `${value.slice(0, max - 1)}…`;
}
