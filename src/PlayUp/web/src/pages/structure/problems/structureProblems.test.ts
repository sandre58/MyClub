import { describe, expect, it } from 'vitest';

import { ApiError } from '../../../api';
import enCommon from '../../../i18n/locales/en/common.json';
import enStructure from '../../../i18n/locales/en/structure.json';
import frStructure from '../../../i18n/locales/fr/structure.json';
import type { StructureStageHubSummary } from '../../../types';
import { formatStructureMutationMessage } from './formatStructureMutationMessage';
import {
  LIFECYCLE_CHECKLIST_WIRE_CODES,
  MATCHS_LIFECYCLE_WIRE_CODES,
  STAGE_COMPLETION_BLOCKED,
} from './constants';
import { resolveLifecycleMutationMessageKey } from './lifecycleMutationMessageKey';
import {
  shouldShowDraftPrepareDisabledTooltip,
  shouldShowPrimaryBlockMessage,
  shouldShowRevertDisabledParagraph,
} from './matchsColumnVisibility';
import { normalizeLifecycleMutationError } from './normalizeLifecycleMutation';
import {
  resolveDraftPrepareBodyMessage,
  resolveLifecycleAnticipatedMessage,
  resolveLifecycleMutationMessage,
} from './resolveStructureProblemMessage';

const t = (
  key: string,
  opts?: { defaultValue?: string } & Record<string, unknown>,
) => {
  const parts = key.split('.');
  let node: unknown = enStructure;
  for (const part of parts) {
    if (node == null || typeof node !== 'object') {
      return opts?.defaultValue ?? key;
    }
    node = (node as Record<string, unknown>)[part];
  }
  if (typeof node === 'string') {
    let text = node;
    if (opts) {
      for (const [name, value] of Object.entries(opts)) {
        if (name === 'defaultValue') {
          continue;
        }
        text = text.replaceAll(`{{${name}}}`, String(value));
      }
    }
    return text;
  }
  return opts?.defaultValue ?? '';
};

const tCommon = (key: string, opts?: Record<string, unknown>) => {
  if (key === 'errorWithStatus' && opts) {
    return enCommon.errorWithStatus
      .replace('{{message}}', String(opts.message))
      .replace('{{status}}', String(opts.status));
  }
  return key;
};

describe('formatStructureMutationMessage', () => {
  it('lists ready downstream consumer phase names on D1 refusal', () => {
    const downstream: StructureStageHubSummary = {
      stageId: '11111111-1111-1111-1111-111111111111',
      name: 'Final',
      status: 'Ready',
      teamCount: 2,
      matchCount: 0,
      matchdayCount: 0,
      slotCount: 0,
      numberOfPeriods: 2,
      durationPerPeriod: 45,
      hasExtraTime: false,
      hasPenaltyShootout: false,
      hasDrawRules: false,
      hasQualificationRules: false,
      qualificationPathCount: 0,
      hasProgressionRules: false,
      progressionPathCount: 0,
      hasTieFormat: false,
    };
    const msg = formatStructureMutationMessage(
      t,
      tCommon,
      new ApiError(
        409,
        'blocked',
        undefined,
        'Application.ReadyConsumerWouldBreak',
        [downstream.stageId],
      ),
      [downstream],
    );
    expect(msg).toContain('Final');
    expect(msg).toContain('Affected phases');
    expect(msg).toContain('409');
  });
});

describe('matchsColumnVisibility', () => {
  it('hides open-matches primary copy on running phase', () => {
    expect(shouldShowPrimaryBlockMessage('Running', 'OpenMatches')).toBe(false);
  });

  it('hides construction frozen and unmaterialized inline copy', () => {
    expect(shouldShowPrimaryBlockMessage('Draft', 'ConstructionFrozen')).toBe(false);
    expect(
      shouldShowPrimaryBlockMessage('Ready', 'UnmaterializedConfrontations'),
    ).toBe(false);
  });

  it('hides draft prepare tooltip for ConstructionFrozen', () => {
    expect(
      shouldShowDraftPrepareDisabledTooltip('ConstructionFrozen', false),
    ).toBe(false);
    expect(shouldShowDraftPrepareDisabledTooltip('Places', false)).toBe(true);
  });

  it('keeps revert disable on tooltip-only codes out of paragraph', () => {
    expect(shouldShowRevertDisabledParagraph('ProductionPresent')).toBe(false);
    expect(shouldShowRevertDisabledParagraph('CompetitionClosed')).toBe(true);
  });
});

describe('resolveLifecycleAnticipatedMessage', () => {
  it('resolves Attribution prepare gate copy', () => {
    expect(resolveLifecycleAnticipatedMessage(t, 'Attribution')).toContain(
      'attribution',
    );
  });

  it('resolves known wire codes from problems.lifecycle', () => {
    expect(resolveLifecycleAnticipatedMessage(t, 'Places')).toContain('place');
    expect(resolveLifecycleAnticipatedMessage(t, 'NotInInventory')).toContain(
      'unavailable',
    );
  });

  it('uses problem canon when lifecycle transport is an aggregate', () => {
    const msg = resolveLifecycleAnticipatedMessage(t, 'StructureGraphInvalid', {
      canonCode: 'CycleDetected',
      semanticLevel: 'Gate',
      blockedActions: ['PrepareStage'],
      transportCode: 'StructureGraphInvalid',
    });
    expect(msg).toBe(enStructure.problems.lifecycle.CycleDetected);
  });
});

describe('resolveDraftPrepareBodyMessage', () => {
  it('uses draft detail for StageNotReady block', () => {
    const msg = resolveDraftPrepareBodyMessage(t, true, 'StageNotReady');
    expect(msg).toBe(enStructure.lifecycle.state.Draft.detail);
  });

  it('uses specific code when not StageNotReady', () => {
    const msg = resolveDraftPrepareBodyMessage(t, true, 'Places');
    expect(msg).toBe(enStructure.problems.lifecycle.Places);
  });
});

describe('normalizeLifecycleMutationError', () => {
  it('maps stage completion blocked to OpenMatches key', () => {
    const normalized = normalizeLifecycleMutationError(
      new ApiError(409, 'blocked', undefined, STAGE_COMPLETION_BLOCKED),
    );
    expect(normalized.messageKey).toBe('problems.lifecycle.OpenMatches');
    expect(normalized.wireCode).toBe(STAGE_COMPLETION_BLOCKED);
  });

  it('preserves unknown api wire code on normalized shape', () => {
    const normalized = normalizeLifecycleMutationError(
      new ApiError(400, 'x', undefined, 'Application.SomeFutureCode'),
    );
    expect(normalized.wireCode).toBe('Application.SomeFutureCode');
    expect(normalized.messageKey).toBe('problems.mutation.Application.SomeFutureCode');
  });

  it('maps prepare gate Application codes to problems.lifecycle copy', () => {
    const normalized = normalizeLifecycleMutationError(
      new ApiError(
        400,
        'cycle',
        undefined,
        'Application.StructureCycleDetected',
      ),
    );
    expect(normalized.messageKey).toBe('problems.lifecycle.CycleDetected');
  });
});

describe('resolveLifecycleMutationMessageKey', () => {
  it('maps known lifecycle Application failures to lifecycle keys', () => {
    expect(resolveLifecycleMutationMessageKey('Application.StartBlockedUnmaterialized')).toBe(
      'problems.lifecycle.UnmaterializedConfrontations',
    );
    expect(resolveLifecycleMutationMessageKey('Application.StageExploitationClosed')).toBe(
      'problems.lifecycle.CompetitionClosed',
    );
    expect(resolveLifecycleMutationMessageKey('Application.Attribution')).toBe(
      'problems.lifecycle.Attribution',
    );
    expect(resolveLifecycleMutationMessageKey('Application.ReadyConsumerWouldBreak')).toBe(
      'problems.lifecycle.ReadyConsumerWouldBreak',
    );
  });
});

describe('resolveLifecycleMutationMessage', () => {
  it('uses OpenMatches copy for completion blocked POST', () => {
    const msg = resolveLifecycleMutationMessage(
      t,
      new ApiError(409, 'blocked', undefined, STAGE_COMPLETION_BLOCKED),
    );
    expect(msg).toBe(enStructure.problems.lifecycle.OpenMatches);
  });

  it('falls back to mutation unknown when api code has no mapping', () => {
    const msg = resolveLifecycleMutationMessage(
      t,
      new ApiError(400, 'x', undefined, 'Application.SomeFutureCode'),
    );
    expect(msg).toBe(enStructure.problems.mutation._unknown);
  });

  it('uses lifecycle copy for mapped prepare POST failures', () => {
    const msg = resolveLifecycleMutationMessage(
      t,
      new ApiError(400, 'cycle', undefined, 'Application.StructureCycleDetected'),
    );
    expect(msg).toBe(enStructure.problems.lifecycle.CycleDetected);
  });

  it('falls back to errors namespace when lifecycle and mutation keys are absent', () => {
    const msg = resolveLifecycleMutationMessage(
      t,
      new ApiError(400, 'feed', undefined, 'Application.DanglingFeedTarget'),
    );
    expect(msg).not.toBe(enStructure.problems.mutation._unknown);
    expect(msg.length).toBeGreaterThan(0);
  });

  it('returns client Error message for non-ApiError', () => {
    expect(resolveLifecycleMutationMessage(t, new Error('network'))).toBe('network');
  });
});

describe('problems i18n parity', () => {
  it('has matching FR/EN keys for Matchs lifecycle inventory', () => {
    for (const code of MATCHS_LIFECYCLE_WIRE_CODES) {
      expect(enStructure.problems.lifecycle).toHaveProperty(code);
      expect(frStructure.problems.lifecycle).toHaveProperty(code);
    }
    for (const code of LIFECYCLE_CHECKLIST_WIRE_CODES) {
      expect(enStructure.problems.lifecycle).toHaveProperty(code);
      expect(frStructure.problems.lifecycle).toHaveProperty(code);
    }
    expect(enStructure.problems.lifecycle._unknown).toBeTruthy();
    expect(frStructure.problems.lifecycle._unknown).toBeTruthy();
    expect(enStructure.problems.mutation._unknown).toBeTruthy();
    expect(frStructure.problems.mutation._unknown).toBeTruthy();
  });
});
