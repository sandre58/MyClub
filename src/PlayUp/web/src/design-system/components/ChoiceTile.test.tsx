import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ChoiceTile } from './ChoiceTile';

describe('ChoiceTile', () => {
  it('keeps default checkbox rendering', () => {
    render(<ChoiceTile label="Alpha" selected={false} onChange={() => {}} />);
    expect(screen.getByRole('checkbox', { name: 'Alpha' })).toBeInTheDocument();
  });

  it('supports radio role', () => {
    render(
      <ChoiceTile label="Alpha" selected role="radio" onChange={() => {}} />,
    );
    expect(screen.getByRole('radio', { name: 'Alpha' })).toHaveAttribute(
      'aria-checked',
      'true',
    );
  });

  it('marks unavailable tiles aria-disabled and ignores click but stays focusable', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <ChoiceTile
        label="Alpha"
        selected={false}
        unavailable
        onChange={onChange}
      />,
    );
    const tile = screen.getByRole('checkbox', { name: 'Alpha' });
    expect(tile).toHaveAttribute('aria-disabled', 'true');
    expect(tile).not.toBeDisabled();
    await user.click(tile);
    expect(onChange).not.toHaveBeenCalled();
  });
});
