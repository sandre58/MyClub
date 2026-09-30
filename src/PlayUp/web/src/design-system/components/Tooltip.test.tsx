import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  Tooltip,
  TOOLTIP_DELAY_CLOSE_MS,
  TOOLTIP_DELAY_OPEN_MS,
  TOOLTIP_LONG_PRESS_MS,
  TOOLTIP_MOBILE_DISMISS_MS,
} from './Tooltip';

function mockPointer(fine: boolean) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches:
        fine &&
        query.includes('hover: hover') &&
        query.includes('pointer: fine'),
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    }),
  });
}

describe('Tooltip', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    mockPointer(true);
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  it('opens on hover after delay and exposes role=tooltip', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <Tooltip content="Hint text">
        <span>Token</span>
      </Tooltip>,
    );

    await user.hover(screen.getByText('Token'));
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();

    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_DELAY_OPEN_MS);
    });

    const tip = await screen.findByRole('tooltip');
    expect(tip).toHaveTextContent('Hint text');
    expect(
      screen.getByText('Token').closest('[aria-describedby]'),
    ).toBeTruthy();
  });

  it('opens immediately on keyboard focus', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <Tooltip content="Focus tip">
        <button type="button">Action</button>
      </Tooltip>,
    );

    await user.tab();
    expect(screen.getByRole('button', { name: 'Action' })).toHaveFocus();
    expect(await screen.findByRole('tooltip')).toHaveTextContent('Focus tip');
  });

  it('closes on Escape', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <Tooltip content="Esc tip">
        <button type="button">Action</button>
      </Tooltip>,
    );

    await user.tab();
    expect(await screen.findByRole('tooltip')).toBeInTheDocument();
    await user.keyboard('{Escape}');
    await waitFor(() => {
      expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    });
  });

  it('closes after leave delay', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <Tooltip content="Leave tip">
        <span>Token</span>
      </Tooltip>,
    );

    await user.hover(screen.getByText('Token'));
    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_DELAY_OPEN_MS);
    });
    expect(await screen.findByRole('tooltip')).toBeInTheDocument();

    await user.unhover(screen.getByText('Token'));
    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_DELAY_CLOSE_MS);
    });
    await waitFor(() => {
      expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    });
  });

  it('keeps only one tooltip open', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <>
        <Tooltip content="First">
          <button type="button">One</button>
        </Tooltip>
        <Tooltip content="Second">
          <button type="button">Two</button>
        </Tooltip>
      </>,
    );

    await user.tab();
    expect(await screen.findByRole('tooltip')).toHaveTextContent('First');
    await user.tab();
    expect(await screen.findByRole('tooltip')).toHaveTextContent('Second');
    expect(screen.queryAllByRole('tooltip')).toHaveLength(1);
  });

  it('wraps disabled buttons so the tip remains reachable', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <Tooltip content="Why disabled">
        <button type="button" disabled>
          Add
        </button>
      </Tooltip>,
    );

    const trigger = screen.getByText('Add').closest('.ds-tooltip-trigger');
    expect(trigger).toBeTruthy();
    await user.hover(trigger!);
    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_DELAY_OPEN_MS);
    });
    expect(await screen.findByRole('tooltip')).toHaveTextContent(
      'Why disabled',
    );
  });

  it('opens on long-press when coarse pointer and primary action', async () => {
    mockPointer(false);
    render(
      <Tooltip content="Hold me" activation="long-press">
        <button type="button">Edit</button>
      </Tooltip>,
    );

    const trigger = screen.getByText('Edit').closest('.ds-tooltip-trigger')!;
    await act(async () => {
      trigger.dispatchEvent(
        new PointerEvent('pointerdown', {
          bubbles: true,
          button: 0,
          clientX: 10,
          clientY: 10,
        }),
      );
      vi.advanceTimersByTime(TOOLTIP_LONG_PRESS_MS);
    });

    expect(await screen.findByRole('tooltip')).toHaveTextContent('Hold me');

    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_MOBILE_DISMISS_MS);
    });
    await waitFor(() => {
      expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    });
  });

  it('toggles on tap when coarse pointer and no primary action', async () => {
    mockPointer(false);
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <Tooltip content="Chip tip" activation="tap">
        <span>3 pts</span>
      </Tooltip>,
    );

    await user.click(screen.getByText('3 pts'));
    expect(await screen.findByRole('tooltip')).toHaveTextContent('Chip tip');
    await user.click(screen.getByText('3 pts'));
    await waitFor(() => {
      expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    });
  });
});
