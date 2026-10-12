import { describe, expect, it } from 'vitest';

import { ApiError } from '../api';
import { attentionLoadErrorMessage } from './attentionLoadError';

const t = (key: string) => key;

describe('attentionLoadErrorMessage', () => {
  it('maps 403 to auth copy', () => {
    expect(
      attentionLoadErrorMessage(t, new ApiError(403, 'forbidden')),
    ).toBe('attention.loadErrorAuth');
  });

  it('maps 500 to server copy', () => {
    expect(
      attentionLoadErrorMessage(t, new ApiError(500, 'error')),
    ).toBe('attention.loadErrorServer');
  });

  it('falls back to generic load error', () => {
    expect(attentionLoadErrorMessage(t, new Error('network'))).toBe(
      'attention.loadError',
    );
  });
});
