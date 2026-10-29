import type {
  StageDrawAction,
  StageDrawActions,
  StageOverview,
} from '../types';

export type DrawActionsById = Record<string, StageDrawActions>;

export interface WithDrawActionsOptions {
  actions?: DrawActionsById;
  releaseCandidateDrawId?: string | null;
}

export const hiddenAction: StageDrawAction = { state: 'Hidden' };

export function hiddenDrawActions(): StageDrawActions {
  return {
    cancel: hiddenAction,
    release: { state: 'Hidden', releasableCount: 0 },
    apply: hiddenAction,
    publishAndApply: hiddenAction,
    redo: hiddenAction,
  };
}

export function withDrawActions(
  overview: StageOverview,
  options: WithDrawActionsOptions = {},
): StageOverview {
  const draws = overview.draws.map((draw) =>
    draw.actions || options.actions?.[draw.id]
      ? { ...draw, actions: draw.actions ?? options.actions?.[draw.id] }
      : draw,
  );
  return {
    ...overview,
    draws,
    releaseCandidateDrawId: Object.hasOwn(options, 'releaseCandidateDrawId')
      ? options.releaseCandidateDrawId
      : overview.releaseCandidateDrawId,
  };
}

export function drawActions(
  overrides: Partial<StageDrawActions> = {},
): StageDrawActions {
  return {
    ...hiddenDrawActions(),
    ...overrides,
  };
}
