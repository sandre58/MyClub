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
    document.body.innerHTML = `
      <div class="schematic-slot" data-slot-key="Champ">
        <button type="button" class="structure-freshness-badge__action">Libérer</button>
      </div>`;
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
    document.body.innerHTML = '';
  });

  it('highlights the slot and removes the class after 2.5s', () => {
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

  it('consumes once immediately, keeps halo through rerenders, then clears halo', () => {
    const onConsumed = vi.fn();

    const { rerender } = renderHook(
      ({ slotKey, ready, consumed }) =>
        useStructureSlotFocus(slotKey, ready, consumed),
      {
        initialProps: {
          slotKey: 'Champ',
          ready: true,
          consumed: onConsumed,
        },
      },
    );

    const slot = document.querySelector('[data-slot-key="Champ"]');
    expect(slot).toHaveClass('schematic-slot--focus');
    expect(onConsumed).toHaveBeenCalledTimes(1);

    const consumedFirst = onConsumed.mock.calls.length;
    rerender({ slotKey: 'Champ', ready: true, consumed: vi.fn() });
    expect(slot).toHaveClass('schematic-slot--focus');
    expect(onConsumed).toHaveBeenCalledTimes(consumedFirst);

    act(() => {
      vi.advanceTimersByTime(1_000);
    });
    rerender({ slotKey: 'Champ', ready: true, consumed: vi.fn() });
    expect(slot).toHaveClass('schematic-slot--focus');

    act(() => {
      vi.advanceTimersByTime(1_600);
    });
    expect(slot).not.toHaveClass('schematic-slot--focus');
    expect(onConsumed).toHaveBeenCalledTimes(consumedFirst);
  });
});
