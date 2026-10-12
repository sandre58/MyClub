import { describe, expect, it } from 'vitest';

import { ApiError } from '../../../api';
import enStructure from '../../../i18n/locales/en/structure.json';
import frStructure from '../../../i18n/locales/fr/structure.json';
import { MATCHS_LIFECYCLE_WIRE_CODES, STAGE_COMPLETION_BLOCKED } from './constants';
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

const t = (key: string, opts?: { defaultValue?: string }) => {
  const parts = key.split('.');
  let node: unknown = enStructure;
  for (const part of parts) {
    if (node == null || typeof node !== 'object') {
      return opts?.defaultValue ?? key;
    }
    node = (node as Record<string, unknown>)[part];
  }
  if (typeof node === 'string') {
    return node;
  }
  return opts?.defaultValue ?? '';
};

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
  it('resolves known wire codes from problems.lifecycle', () => {
    expect(resolveLifecycleAnticipatedMessage(t, 'Places')).toContain('place');
    expect(resolveLifecycleAnticipatedMessage(t, 'NotInInventory')).toContain(
      'unavailable',
    );
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
    expect(enStructure.problems.lifecycle._unknown).toBeTruthy();
    expect(frStructure.problems.lifecycle._unknown).toBeTruthy();
    expect(enStructure.problems.mutation._unknown).toBeTruthy();
    expect(frStructure.problems.mutation._unknown).toBeTruthy();
  });
});
