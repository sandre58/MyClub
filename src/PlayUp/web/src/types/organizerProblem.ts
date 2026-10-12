/** Step 2 read-model semantics (Application pilot — optional on wire). */
export interface OrganizerProblemRead {
  canonCode: string;
  semanticLevel: 'Gate' | 'Signal' | 'Attention' | 'Information';
  blockedActions: string[];
  transportCode?: string | null;
  suggestedActionCode?: string | null;
}
