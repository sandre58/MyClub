import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { DropDownButton } from './DropDownButton';

describe('DropDownButton', () => {
  it('applies button size and variant on the trigger, never on the root', () => {
    const { container } = render(
      <DropDownButton
        size="sm"
        variant="secondary"
        label="Add"
        aria-label="Add item"
        items={[{ value: 'a', label: 'Alpha' }]}
        onSelect={vi.fn()}
      />,
    );
    const root = container.firstElementChild;
    expect(root).toHaveClass('ds-dropdown-button');
    expect(root).not.toHaveClass('ds-btn');
    expect(root).not.toHaveClass('ds-btn--sm');
    expect(root).not.toHaveClass('ds-btn--secondary');
    const trigger = screen.getByRole('button', { name: 'Add item' });
    expect(trigger).toHaveClass('ds-btn--sm');
    expect(trigger).toHaveClass('ds-btn--secondary');
    expect(trigger).toHaveClass('ds-dropdown-button__trigger');
  });

  it('keeps layout className on the root and never styles the trigger from it', () => {
    const { container } = render(
      <DropDownButton
        className="ds-btn--sm dropdown-layout-hook"
        label="Add"
        aria-label="Add item"
        items={[{ value: 'a', label: 'Alpha' }]}
        onSelect={vi.fn()}
      />,
    );
    const root = container.firstElementChild;
    expect(root).toHaveClass('dropdown-layout-hook');
    expect(root).toHaveClass('ds-btn--sm');
    const trigger = screen.getByRole('button', { name: 'Add item' });
    expect(trigger).not.toHaveClass('ds-btn--sm');
    expect(trigger).toHaveClass('ds-btn--primary');
  });

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

  it('keeps the menu end-aligned with intrinsic width between 12rem and 22rem', async () => {
    const user = userEvent.setup();
    const style = document.createElement('style');
    style.textContent = '.dropdown-test-full-width { width: 20rem; }';
    document.head.appendChild(style);

    const { container } = render(
      <DropDownButton
        label="Add something with a very long label that should ellipsize"
        aria-label="Add item"
        align="end"
        className="dropdown-test-full-width"
        items={[
          { value: 'a', label: 'Alpha', secondary: '1/4' },
          { value: 'b', label: 'Beta with a long caption' },
        ]}
        onSelect={vi.fn()}
      />,
    );

    const root = container.firstElementChild as HTMLElement;
    const trigger = screen.getByRole('button', { name: 'Add item' });
    expect(trigger.querySelector('.ds-dropdown-button__label')).toHaveClass(
      'ds-dropdown-button__label',
    );
    expect(trigger.offsetWidth).toBe(root.offsetWidth);

    await user.click(trigger);
    const menu = await screen.findByRole('menu');
    expect(menu).toHaveAttribute('data-align', 'end');
    const remPx = Number.parseFloat(
      getComputedStyle(document.documentElement).fontSize,
    );
    await waitFor(() => {
      const menuWidth = Number.parseFloat(menu.style.width);
      expect(menuWidth).toBeGreaterThanOrEqual(12 * remPx - 1);
      expect(menuWidth).toBeLessThanOrEqual(22 * remPx + 1);
      expect(menuWidth).toBeLessThan(20 * remPx);
    });

    document.head.removeChild(style);
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
