import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { Select } from './Select';

describe('Select', () => {
  it('portals the listbox and dismisses on outside click', async () => {
    const user = userEvent.setup();
    render(
      <Select
        aria-label="Stage type"
        options={[
          { value: 'a', label: 'Alpha' },
          { value: 'b', label: 'Beta' },
        ]}
      />,
    );

    await user.click(screen.getByRole('combobox', { name: 'Stage type' }));
    const listbox = screen.getByRole('listbox');
    expect(listbox).toBeInTheDocument();
    expect(listbox.parentElement).toBe(document.body);
    expect(listbox).toHaveAttribute('data-side', 'below');
    expect(listbox).toHaveStyle({ position: 'fixed' });

    await user.click(document.body);
    await waitFor(() => {
      expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    });
  });

  it('sets data-nav to keyboard after arrow navigation and pointer after mouse enter', async () => {
    const user = userEvent.setup();
    render(
      <Select
        aria-label="Stage type"
        options={[
          { value: 'a', label: 'Alpha' },
          { value: 'b', label: 'Beta' },
        ]}
      />,
    );

    const combobox = screen.getByRole('combobox', { name: 'Stage type' });
    combobox.focus();
    await user.keyboard('{ArrowDown}');
    const listbox = screen.getByRole('listbox');
    expect(listbox).toHaveAttribute('data-nav', 'keyboard');

    await user.hover(screen.getByRole('option', { name: 'Beta' }));
    expect(listbox).toHaveAttribute('data-nav', 'pointer');
  });
});
