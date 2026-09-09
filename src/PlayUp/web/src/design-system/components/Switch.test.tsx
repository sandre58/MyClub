import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { Switch } from './Switch';
import { ChoiceTile, ChoiceSwatch } from './ChoiceTile';

describe('Switch', () => {
  it('toggles checked state', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <Switch checked={false} onChange={onChange} label="Prolongations" />,
    );

    await user.click(screen.getByRole('switch', { name: 'Prolongations' }));
    expect(onChange).toHaveBeenCalledWith(true);
  });
});

describe('ChoiceTile', () => {
  it('toggles selection', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <ChoiceTile
        label="Jaune"
        selected={false}
        leading={<ChoiceSwatch color="#F5C518" />}
        onChange={onChange}
      />,
    );

    await user.click(screen.getByRole('checkbox', { name: 'Jaune' }));
    expect(onChange).toHaveBeenCalledWith(true);
  });
});
