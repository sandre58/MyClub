/**
 * GET /competitions/{id}/attention — Needs Attention hub.
 */

/** GET /competitions/{id}/attention — derived Needs Attention hub. */
export interface NeedsAttention {
  competitionId: string;
  items: NeedsAttentionItem[];
  /** Host also exposes Count; prefer items.length when omitted. */
  count?: number;
}

export interface NeedsAttentionItem {
  source: string;
  severity: string;
  targetType: string | null;
  targetId: string | null;
  /** Optional structured params for SPA templates (e.g. activeCount, minimumTeams). */
  params?: Record<string, string> | null;
}
