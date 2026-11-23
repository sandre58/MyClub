import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRef, useState } from 'react';
import { afterEach, describe, expect, it } from 'vitest';
import { clearToasts, notify } from '../toastStore';
import { Dialog } from './Dialog';
import { Toaster } from './Toaster';

function DialogHarness({
  closeDisabled = false,
  size = 'sm' as const,
}: {
  closeDisabled?: boolean;
  size?: 'sm' | 'md';
}) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);

  return (
    <div className="ds-root" data-font="plex" data-palette="slate">
      <button ref={triggerRef} type="button" onClick={() => setOpen(true)}>
        Ouvrir
      </button>
      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        title="Titre dialog"
        closeDisabled={closeDisabled}
        returnFocusRef={triggerRef}
        size={size}
        footer={
          <button type="button" className="ds-btn ds-btn--primary">
            Enregistrer
          </button>
        }
      >
        <label>
          Nom
          <input aria-label="Nom" />
        </label>
      </Dialog>
    </div>
  );
}

describe('Dialog', () => {
  afterEach(() => {
    clearToasts();
  });

  it('opens with heading, focuses the first body control, and closes on Escape', async () => {
    const user = userEvent.setup();
    render(<DialogHarness />);

    const trigger = screen.getByRole('button', { name: 'Ouvrir' });
    await user.click(trigger);

    const dialog = await screen.findByRole('dialog');
    expect(
      within(dialog).getByRole('heading', { name: 'Titre dialog' }),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(within(dialog).getByLabelText('Nom')).toHaveFocus();
    });
    expect(
      within(dialog).getByRole('button', { name: 'Fermer' }),
    ).toBeInTheDocument();

    await user.keyboard('{Escape}');
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(trigger).toHaveFocus();
    expect(document.body.style.overflow).not.toBe('hidden');
  });

  it('does not close while closeDisabled', async () => {
    const user = userEvent.setup();
    render(<DialogHarness closeDisabled />);

    await user.click(screen.getByRole('button', { name: 'Ouvrir' }));
    const dialog = await screen.findByRole('dialog');

    await user.keyboard('{Escape}');
    expect(dialog).toBeInTheDocument();
    expect(
      within(dialog).getByRole('button', { name: 'Fermer' }),
    ).toBeDisabled();
  });

  it('focuses the primary footer action when the body has no controls', async () => {
    const user = userEvent.setup();

    function FooterOnlyHarness() {
      const [open, setOpen] = useState(false);
      return (
        <div className="ds-root" data-font="plex" data-palette="slate">
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog
            open={open}
            onClose={() => setOpen(false)}
            title="Confirmer"
            footer={
              <>
                <button type="button" className="ds-btn ds-btn--ghost">
                  Annuler
                </button>
                <button type="button" className="ds-btn ds-btn--primary">
                  OK
                </button>
              </>
            }
          >
            <p>Message seulement</p>
          </Dialog>
        </div>
      );
    }

    render(<FooterOnlyHarness />);
    await user.click(screen.getByRole('button', { name: 'Ouvrir' }));
    const dialog = await screen.findByRole('dialog');
    await waitFor(() => {
      expect(within(dialog).getByRole('button', { name: 'OK' })).toHaveFocus();
    });
  });

  it('renders footer status left of actions without putting it in the body', async () => {
    const user = userEvent.setup();

    function StatusHarness() {
      const [open, setOpen] = useState(false);
      return (
        <div className="ds-root" data-font="plex" data-palette="slate">
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog
            open={open}
            onClose={() => setOpen(false)}
            title="Édition"
            footerStatus={
              <p className="ds-notice ds-notice--warning" role="status">
                Trop de places
              </p>
            }
            footer={
              <button type="button" className="ds-btn ds-btn--primary">
                Enregistrer
              </button>
            }
          >
            <p>Corps</p>
          </Dialog>
        </div>
      );
    }

    render(<StatusHarness />);
    await user.click(screen.getByRole('button', { name: 'Ouvrir' }));
    const dialog = await screen.findByRole('dialog');
    const status = within(dialog).getByRole('status');
    expect(status).toHaveTextContent('Trop de places');
    const footer = status.closest('.ds-dialog__footer');
    expect(footer).toBeTruthy();
    expect(footer).toContainElement(
      within(dialog).getByRole('button', { name: 'Enregistrer' }),
    );
    expect(status.closest('.ds-dialog__body')).toBeNull();
  });

  it('focuses the primary footer action before a destructive one', async () => {
    const user = userEvent.setup();

    function DestructiveFirstHarness() {
      const [open, setOpen] = useState(false);
      return (
        <div className="ds-root" data-font="plex" data-palette="slate">
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog
            open={open}
            onClose={() => setOpen(false)}
            title="Confirmer"
            footer={
              <>
                <button type="button" className="ds-btn ds-btn--destructive">
                  Supprimer
                </button>
                <button type="button" className="ds-btn ds-btn--primary">
                  Garder
                </button>
              </>
            }
          >
            <p>Message seulement</p>
          </Dialog>
        </div>
      );
    }

    render(<DestructiveFirstHarness />);
    await user.click(screen.getByRole('button', { name: 'Ouvrir' }));
    const dialog = await screen.findByRole('dialog');
    await waitFor(() => {
      expect(
        within(dialog).getByRole('button', { name: 'Garder' }),
      ).toHaveFocus();
    });
  });

  it('focuses the footer before the body when initialFocus is footer', async () => {
    const user = userEvent.setup();

    function FooterFirstHarness() {
      const [open, setOpen] = useState(false);
      return (
        <div className="ds-root" data-font="plex" data-palette="slate">
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog
            open={open}
            onClose={() => setOpen(false)}
            title="Tirage"
            initialFocus="footer"
            footer={
              <button type="button" className="ds-btn ds-btn--primary">
                Valider
              </button>
            }
          >
            <button type="button">Libérer</button>
          </Dialog>
        </div>
      );
    }

    render(<FooterFirstHarness />);
    await user.click(screen.getByRole('button', { name: 'Ouvrir' }));
    const dialog = await screen.findByRole('dialog');
    await waitFor(() => {
      expect(
        within(dialog).getByRole('button', { name: 'Valider' }),
      ).toHaveFocus();
    });
  });

  it('focuses a neutral footer action before a destructive one', async () => {
    const user = userEvent.setup();

    function NeutralBeforeDestructive() {
      const [open, setOpen] = useState(false);
      return (
        <div className="ds-root" data-font="plex" data-palette="slate">
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog
            open={open}
            onClose={() => setOpen(false)}
            title="Confirmer"
            footer={
              <>
                <button type="button" className="ds-btn ds-btn--ghost">
                  Annuler
                </button>
                <button type="button" className="ds-btn ds-btn--destructive">
                  Supprimer
                </button>
              </>
            }
          >
            <p>Message seulement</p>
          </Dialog>
        </div>
      );
    }

    render(<NeutralBeforeDestructive />);
    await user.click(screen.getByRole('button', { name: 'Ouvrir' }));
    const dialog = await screen.findByRole('dialog');
    await waitFor(() => {
      expect(
        within(dialog).getByRole('button', { name: 'Annuler' }),
      ).toHaveFocus();
    });
  });

  it('focuses the header close when the footer action is only destructive', async () => {
    const user = userEvent.setup();

    function DestructiveOnly() {
      const [open, setOpen] = useState(false);
      return (
        <div className="ds-root" data-font="plex" data-palette="slate">
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog
            open={open}
            onClose={() => setOpen(false)}
            title="Confirmer"
            closeLabel="Fermer"
            initialFocus="footer"
            footer={
              <button type="button" className="ds-btn ds-btn--destructive">
                Supprimer
              </button>
            }
          >
            <button type="button">Dans le corps</button>
          </Dialog>
        </div>
      );
    }

    render(<DestructiveOnly />);
    await user.click(screen.getByRole('button', { name: 'Ouvrir' }));
    const dialog = await screen.findByRole('dialog');
    await waitFor(() => {
      expect(
        within(dialog).getByRole('button', { name: 'Fermer' }),
      ).toHaveFocus();
    });
    expect(
      within(dialog).getByRole('button', { name: 'Dans le corps' }),
    ).not.toHaveFocus();
    expect(
      within(dialog).getByRole('button', { name: 'Supprimer' }),
    ).not.toHaveFocus();
  });

  it('portals the dialog root to document.body, outside the host tree', async () => {
    const user = userEvent.setup();
    const { container } = render(
      <div className="shell__workspace">
        <DialogHarness />
      </div>,
    );

    await user.click(within(container).getByRole('button', { name: 'Ouvrir' }));
    await screen.findByRole('dialog');

    const dialogRoot = document.body.querySelector('.ds-dialog');
    expect(dialogRoot).toBeTruthy();
    expect(container.querySelector('.ds-dialog')).toBeNull();
    expect(dialogRoot?.parentElement).toBe(document.body);
  });

  it('inerts the app host but not portaled toaster, tooltip, popover, or select', async () => {
    const user = userEvent.setup();

    function ChromeHarness() {
      const [open, setOpen] = useState(false);
      return (
        <div className="ds-root" data-font="plex" data-palette="slate">
          <header className="fake-shell-header">Header</header>
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog open={open} onClose={() => setOpen(false)} title="Test">
            <p>Corps</p>
          </Dialog>
        </div>
      );
    }

    document.body.insertAdjacentHTML(
      'beforeend',
      '<div class="ds-toaster" id="fixture-toaster"></div>',
    );
    const toaster = document.getElementById('fixture-toaster') as HTMLElement;

    const { container: hostContainer } = render(<ChromeHarness />);
    await user.click(screen.getByRole('button', { name: 'Ouvrir' }));
    await screen.findByRole('dialog');

    expect(hostContainer.inert).toBe(true);
    expect(toaster.inert).not.toBe(true);

    toaster.remove();
  });

  it('keeps toast accessible and Tab trapped when a toast is shown over the dialog', async () => {
    const user = userEvent.setup();

    function DialogToastHarness() {
      const [open, setOpen] = useState(false);
      return (
        <div className="ds-root" data-font="plex" data-palette="slate">
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog
            open={open}
            onClose={() => setOpen(false)}
            title="Titre dialog"
          >
            <input aria-label="Champ" />
          </Dialog>
          <Toaster />
        </div>
      );
    }

    render(<DialogToastHarness />);
    await user.click(screen.getByRole('button', { name: 'Ouvrir' }));
    const dialog = await screen.findByRole('dialog');
    const field = within(dialog).getByLabelText('Champ');
    await waitFor(() => {
      expect(field).toHaveFocus();
    });

    notify.success('Enregistré');
    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent('Enregistré');
    const toasterHost = document.body.querySelector(
      '.ds-toaster',
    ) as HTMLElement | null;
    expect(toasterHost?.inert).not.toBe(true);

    await user.tab();
    const dialogFocusable = within(dialog).getAllByRole('button', {
      hidden: true,
    });
    const fieldStillFocused = field === document.activeElement;
    const focusOnDialogControl = dialogFocusable.some(
      (el) => el === document.activeElement,
    );
    expect(fieldStillFocused || focusOnDialogControl).toBe(true);
    expect(document.activeElement?.closest('.ds-toast')).toBeNull();
  });
});
