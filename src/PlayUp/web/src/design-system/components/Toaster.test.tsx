import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { Toaster } from './Toaster';
import {
  clearToasts,
  getToastsSnapshot,
  notify,
  TOAST_MAX_VISIBLE,
} from '../toastStore';

function ToastHarness() {
  return (
    <div
      className="ds-root"
      data-font="plex"
      data-palette="slate"
      style={{ position: 'relative', minHeight: 240 }}
    >
      <button type="button" onClick={() => notify.success('Publié')}>
        Success
      </button>
      <button type="button" onClick={() => notify.error('Échec métier')}>
        Error
      </button>
      <Toaster />
    </div>
  );
}

describe('Toaster', () => {
  afterEach(() => {
    clearToasts();
    vi.useRealTimers();
  });

  it('shows a success toast without stealing focus from the trigger', async () => {
    const user = userEvent.setup();
    render(<ToastHarness />);

    const trigger = screen.getByRole('button', { name: 'Success' });
    await user.click(trigger);

    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent('Publié');
    expect(status.querySelector('.ds-toast__icon')).toBeTruthy();
    expect(status.querySelector('.ds-toast__progress')).toBeTruthy();
    expect(trigger).toHaveFocus();
  });

  it('dismisses on close control', async () => {
    const user = userEvent.setup();
    render(<ToastHarness />);

    await user.click(screen.getByRole('button', { name: 'Success' }));
    expect(await screen.findByRole('status')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Fermer' }));
    await waitFor(() => {
      expect(screen.queryByRole('status')).not.toBeInTheDocument();
    });
  });

  it('uses alert role for error toasts', async () => {
    const user = userEvent.setup();
    render(<ToastHarness />);

    await user.click(screen.getByRole('button', { name: 'Error' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Échec métier');
  });

  it('keeps at most three toasts in the queue', () => {
    notify.success('1');
    notify.success('2');
    notify.success('3');
    notify.success('4');
    expect(getToastsSnapshot()).toHaveLength(TOAST_MAX_VISIBLE);
    expect(getToastsSnapshot().map((t) => t.message)).toEqual(['2', '3', '4']);
  });

  it('auto-dismisses success after its duration', () => {
    vi.useFakeTimers();
    render(<ToastHarness />);

    act(() => {
      notify.success('Auto');
    });
    expect(screen.getByRole('status')).toHaveTextContent('Auto');

    act(() => {
      vi.advanceTimersByTime(4000);
    });
    act(() => {
      vi.advanceTimersByTime(160);
    });

    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });
});
