import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { Switch } from './Switch';
import { ChoiceTile, ChoiceSwatch } from './ChoiceTile';
import { ToggleButtonGroup } from './ToggleButtonGroup';

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

describe('ToggleButtonGroup', () => {
  it('selects an option exclusively', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <ToggleButtonGroup
        value="light"
        onChange={onChange}
        aria-label="Thème"
        options={[
          { value: 'system', label: 'Système' },
          { value: 'light', label: 'Clair' },
          { value: 'dark', label: 'Sombre' },
        ]}
      />,
    );

    expect(screen.getByRole('radio', { name: 'Clair' })).toHaveAttribute(
      'aria-checked',
      'true',
    );

    await user.click(screen.getByRole('radio', { name: 'Sombre' }));
    expect(onChange).toHaveBeenCalledWith('dark');
  });
});
