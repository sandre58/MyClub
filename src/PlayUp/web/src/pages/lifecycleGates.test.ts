import { describe, expect, it } from 'vitest';
import {
  allowsCompetitionLifecycleMutation,
  allowsStageDraftOrReadyMutation,
  canCancelDrawExecution,
  isDrawExecutionCancellable,
} from './lifecycleGates';

describe('lifecycleGates', () => {
  it('allowsCompetitionLifecycleMutation mirrors Host (blocks Completed|Archived)', () => {
    expect(allowsCompetitionLifecycleMutation('Draft')).toBe(true);
    expect(allowsCompetitionLifecycleMutation('Ready')).toBe(true);
    expect(allowsCompetitionLifecycleMutation('Running')).toBe(true);
    expect(allowsCompetitionLifecycleMutation('Suspended')).toBe(true);
    expect(allowsCompetitionLifecycleMutation('Completed')).toBe(false);
    expect(allowsCompetitionLifecycleMutation('Archived')).toBe(false);
  });

  it('allowsStageDraftOrReadyMutation mirrors Domain EnsureDraftOrReady', () => {
    expect(allowsStageDraftOrReadyMutation('Draft')).toBe(true);
    expect(allowsStageDraftOrReadyMutation('Ready')).toBe(true);
    expect(allowsStageDraftOrReadyMutation('Running')).toBe(false);
    expect(allowsStageDraftOrReadyMutation('Suspended')).toBe(false);
    expect(allowsStageDraftOrReadyMutation('Completed')).toBe(false);
  });

  it('isDrawExecutionCancellable accepts Draft|Published only', () => {
    expect(isDrawExecutionCancellable('Draft')).toBe(true);
    expect(isDrawExecutionCancellable('Published')).toBe(true);
    expect(isDrawExecutionCancellable('Cancelled')).toBe(false);
  });

  it('canCancelDrawExecution requires all three gates (D); keeps post-Apply (A)', () => {
    expect(
      canCancelDrawExecution({
        competitionStatus: 'Running',
        stageStatus: 'Draft',
        drawStatus: 'Published',
      }),
    ).toBe(true);

    expect(
      canCancelDrawExecution({
        competitionStatus: 'Completed',
        stageStatus: 'Draft',
        drawStatus: 'Draft',
      }),
    ).toBe(false);

    expect(
      canCancelDrawExecution({
        competitionStatus: 'Running',
        stageStatus: 'Running',
        drawStatus: 'Draft',
      }),
    ).toBe(false);

    expect(
      canCancelDrawExecution({
        competitionStatus: 'Running',
        stageStatus: 'Draft',
        drawStatus: 'Cancelled',
      }),
    ).toBe(false);
  });
});
