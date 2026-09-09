import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRef, useState } from 'react';
import { describe, expect, it } from 'vitest';
import { Popover } from './Popover';

function PopoverHarness() {
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);

  return (
    <div>
      <button
        ref={anchorRef}
        type="button"
        aria-expanded={open}
        onClick={() => setOpen((current) => !current)}
      >
        Open
      </button>
      <Popover
        open={open}
        onOpenChange={setOpen}
        anchorRef={anchorRef}
        aria-label="Demo panel"
        width={240}
      >
        <p>Panel body</p>
      </Popover>
    </div>
  );
}

describe('Popover', () => {
  it('portals the panel and dismisses on outside click', async () => {
    const user = userEvent.setup();
    render(<PopoverHarness />);

    await user.click(screen.getByRole('button', { name: 'Open' }));
    const panel = screen.getByRole('dialog', { name: 'Demo panel' });
    expect(panel).toBeInTheDocument();
    expect(panel).toHaveAttribute('data-side', 'below');

    await user.click(document.body);
    expect(
      screen.queryByRole('dialog', { name: 'Demo panel' }),
    ).not.toBeInTheDocument();
  });
});
