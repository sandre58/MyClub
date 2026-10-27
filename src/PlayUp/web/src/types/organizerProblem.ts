/** Organizer-problem read-model semantics (optional on the wire). */
export interface OrganizerProblemRead {
  canonCode: string;
  semanticLevel: 'Gate' | 'Signal' | 'Attention' | 'Information';
  blockedActions: string[];
  transportCode?: string | null;
  suggestedActionCode?: string | null;
}
