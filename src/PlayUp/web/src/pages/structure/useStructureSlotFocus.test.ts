import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';

import { useStructureSlotFocus } from './useStructureSlotFocus';

describe('useStructureSlotFocus', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.stubGlobal('requestAnimationFrame', (callback: FrameRequestCallback) => {
      callback(0);
      return 0;
    });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
    document.body.innerHTML = '';
  });

  it('highlights the slot and removes the class after 2.5s', () => {
    document.body.innerHTML = `
      <div class="schematic-slot" data-slot-key="Champ">
        <button type="button" class="structure-freshness-badge__action">Libérer</button>
      </div>`;

    renderHook(
      ({ slotKey, ready }) =>
        useStructureSlotFocus(slotKey, ready, () => undefined),
      { initialProps: { slotKey: 'Champ', ready: true } },
    );

    const slot = document.querySelector('[data-slot-key="Champ"]');
    expect(slot).toHaveClass('schematic-slot--focus');

    act(() => {
      vi.advanceTimersByTime(2_600);
    });

    expect(slot).not.toHaveClass('schematic-slot--focus');
  });
});
