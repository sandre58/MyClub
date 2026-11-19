import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { DropDownButton } from './DropDownButton';

describe('DropDownButton', () => {
  it('opens a menu and selects an item', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    render(
      <DropDownButton
        label="Add"
        aria-label="Add item"
        items={[
          { value: 'a', label: 'Alpha' },
          { value: 'b', label: 'Beta' },
        ]}
        onSelect={onSelect}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Add item' }));
    expect(screen.getByRole('menu')).toBeInTheDocument();

    await user.click(screen.getByRole('menuitem', { name: 'Beta' }));
    expect(onSelect).toHaveBeenCalledWith('b');
    await waitFor(() => {
      expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    });
  });

  it('sets data-nav to keyboard after arrow navigation and pointer on mouse enter', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    render(
      <DropDownButton
        label="Add"
        aria-label="Add item"
        items={[
          { value: 'a', label: 'Alpha' },
          { value: 'b', label: 'Beta' },
        ]}
        onSelect={onSelect}
      />,
    );

    const trigger = screen.getByRole('button', { name: 'Add item' });
    trigger.focus();
    await user.keyboard('{ArrowDown}');
    const list = screen
      .getByRole('menu')
      .querySelector('.ds-dropdown-button__list');
    expect(list).toHaveAttribute('data-nav', 'keyboard');

    await user.hover(screen.getByRole('menuitem', { name: 'Beta' }));
    expect(list).toHaveAttribute('data-nav', 'pointer');
  });

  it('renders secondary text and respects disabled items', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    render(
      <DropDownButton
        label="Add"
        aria-label="Add item"
        items={[
          { value: 'a', label: 'Alpha', secondary: '1/4' },
          { value: 'b', label: 'Beta', secondary: '2/4', disabled: true },
        ]}
        onSelect={onSelect}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Add item' }));
    expect(screen.getByText('1/4')).toBeInTheDocument();
    await user.click(screen.getByRole('menuitem', { name: 'Beta' }));
    expect(onSelect).not.toHaveBeenCalled();
  });

  it('exposes aria-activedescendant while keyboard navigating', async () => {
    const user = userEvent.setup();
    render(
      <DropDownButton
        label="Add"
        aria-label="Add item"
        items={[
          { value: 'a', label: 'Alpha' },
          { value: 'b', label: 'Beta' },
        ]}
        onSelect={vi.fn()}
      />,
    );

    const trigger = screen.getByRole('button', { name: 'Add item' });
    trigger.focus();
    await user.keyboard('{ArrowDown}');
    expect(trigger).toHaveAttribute('aria-activedescendant');
    expect(trigger.getAttribute('aria-activedescendant')).toMatch(/option-/);
  });

  it('supports controlled open state', async () => {
    const { rerender } = render(
      <DropDownButton
        label="Add"
        aria-label="Add item"
        open={false}
        onOpenChange={vi.fn()}
        items={[{ value: 'a', label: 'Alpha' }]}
        onSelect={vi.fn()}
      />,
    );
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    rerender(
      <DropDownButton
        label="Add"
        aria-label="Add item"
        open
        onOpenChange={vi.fn()}
        items={[{ value: 'a', label: 'Alpha' }]}
        onSelect={vi.fn()}
      />,
    );
    expect(screen.getByRole('menu')).toBeInTheDocument();
  });
});
