import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { StrictMode, useState, type ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  __dismissStackDepthForTests,
  __resetDismissStackForTests,
} from '../dismissStack'
import { useDismissLayer } from '../useDismissLayer'
import { Dialog } from './Dialog'
import { ConfirmDialog } from './ConfirmDialog'
import { Select } from './Select'

afterEach(() => {
  __resetDismissStackForTests()
})

function DsRoot({ children }: { children: ReactNode }) {
  return (
    <div className="ds-root" data-font="plex" data-palette="slate">
      {children}
    </div>
  )
}

describe('dismiss stack', () => {
  it('keeps a single stack entry under Strict Mode remount', () => {
    function Probe({ active }: { active: boolean }) {
      useDismissLayer(active, () => undefined)
      return null
    }

    const { rerender, unmount } = render(
      <StrictMode>
        <Probe active />
      </StrictMode>,
    )

    expect(__dismissStackDepthForTests()).toBe(1)

    rerender(
      <StrictMode>
        <Probe active={false} />
      </StrictMode>,
    )
    expect(__dismissStackDepthForTests()).toBe(0)

    unmount()
    expect(__dismissStackDepthForTests()).toBe(0)
  })

  it('calls the latest onDismiss callback', async () => {
    const user = userEvent.setup()
    const first = vi.fn()
    const second = vi.fn()

    function Probe({ onDismiss }: { onDismiss: () => void }) {
      useDismissLayer(true, onDismiss)
      return <button type="button">focus</button>
    }

    const { rerender } = render(
      <DsRoot>
        <Probe onDismiss={first} />
      </DsRoot>,
    )

    rerender(
      <DsRoot>
        <Probe onDismiss={second} />
      </DsRoot>,
    )

    await user.keyboard('{Escape}')
    expect(first).not.toHaveBeenCalled()
    expect(second).toHaveBeenCalledTimes(1)
  })

  it('does not call an inactive layer', async () => {
    const user = userEvent.setup()
    const inactive = vi.fn()
    const active = vi.fn()

    function Probe() {
      useDismissLayer(false, inactive)
      useDismissLayer(true, active)
      return <button type="button">focus</button>
    }

    render(
      <DsRoot>
        <Probe />
      </DsRoot>,
    )

    await user.keyboard('{Escape}')
    expect(inactive).not.toHaveBeenCalled()
    expect(active).toHaveBeenCalledTimes(1)
  })
})

describe('Dialog dismiss stack', () => {
  function DialogHarness({
    closeDisabled = false,
  }: {
    closeDisabled?: boolean
  }) {
    const [open, setOpen] = useState(false)

    return (
      <DsRoot>
        <button type="button" onClick={() => setOpen(true)}>
          Ouvrir
        </button>
        <Dialog
          open={open}
          onClose={() => setOpen(false)}
          title="Titre dialog"
          closeDisabled={closeDisabled}
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
      </DsRoot>
    )
  }

  it('closes on Escape from an input and restores focus to the opener', async () => {
    const user = userEvent.setup()
    render(<DialogHarness />)

    const trigger = screen.getByRole('button', { name: 'Ouvrir' })
    await user.click(trigger)

    const dialog = await screen.findByRole('dialog')
    const input = within(dialog).getByLabelText('Nom')
    await waitFor(() => {
      expect(input).toHaveFocus()
    })

    await user.type(input, 'x')
    await user.keyboard('{Escape}')

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
    expect(trigger).toHaveFocus()
  })

  it('does not close while closeDisabled but still consumes Escape', async () => {
    const user = userEvent.setup()
    const pageDismiss = vi.fn()

    function Harness() {
      const [open, setOpen] = useState(false)
      useDismissLayer(true, pageDismiss)

      return (
        <DsRoot>
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog
            open={open}
            onClose={() => setOpen(false)}
            title="Titre dialog"
            closeDisabled
            footer={
              <button type="button" className="ds-btn ds-btn--primary">
                Enregistrer
              </button>
            }
          >
            <p>Verrouillé</p>
          </Dialog>
        </DsRoot>
      )
    }

    render(<Harness />)

    await user.click(screen.getByRole('button', { name: 'Ouvrir' }))
    await screen.findByRole('dialog')
    await user.keyboard('{Escape}')

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(pageDismiss).not.toHaveBeenCalled()
  })

  it('closes only the Select on first Escape, then the Dialog', async () => {
    const user = userEvent.setup()

    function Harness() {
      const [open, setOpen] = useState(false)
      const [format, setFormat] = useState<string | null>('a')

      return (
        <DsRoot>
          <button type="button" onClick={() => setOpen(true)}>
            Ouvrir
          </button>
          <Dialog
            open={open}
            onClose={() => setOpen(false)}
            title="Avec Select"
          >
            <Select
              aria-label="Format"
              options={[
                { value: 'a', label: 'Alpha' },
                { value: 'b', label: 'Beta' },
              ]}
              value={format}
              onChange={setFormat}
            />
          </Dialog>
        </DsRoot>
      )
    }

    render(<Harness />)
    await user.click(screen.getByRole('button', { name: 'Ouvrir' }))
    await screen.findByRole('dialog')

    await user.click(screen.getByRole('combobox', { name: 'Format' }))
    expect(screen.getByRole('listbox')).toBeInTheDocument()

    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
    })
    expect(screen.getByRole('dialog')).toBeInTheDocument()

    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })

  it('closes only ConfirmDialog when stacked over a Dialog', async () => {
    const user = userEvent.setup()

    function Harness() {
      const [parentOpen, setParentOpen] = useState(true)
      const [confirmOpen, setConfirmOpen] = useState(true)

      return (
        <DsRoot>
          <Dialog
            open={parentOpen}
            onClose={() => setParentOpen(false)}
            title="Parent"
            closeDisabled={confirmOpen}
            trapFocus={!confirmOpen}
          >
            <p>Corps parent</p>
          </Dialog>
          <ConfirmDialog
            open={confirmOpen}
            title="Confirmer"
            message="Sûr ?"
            confirmLabel="OK"
            cancelLabel="Annuler"
            onCancel={() => setConfirmOpen(false)}
            onConfirm={() => setConfirmOpen(false)}
          />
        </DsRoot>
      )
    }

    render(<Harness />)
    expect(
      await screen.findByRole('dialog', { name: 'Confirmer' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: 'Parent' })).toBeInTheDocument()

    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', { name: 'Confirmer' }),
      ).not.toBeInTheDocument()
    })
    expect(screen.getByRole('dialog', { name: 'Parent' })).toBeInTheDocument()
  })
})

describe('Select keyboard', () => {
  const options = [
    { value: 'a', label: 'Alpha' },
    { value: 'b', label: 'Beta', disabled: true },
    { value: 'c', label: 'Charlie' },
    { value: 'd', label: 'Delta' },
  ]

  function Harness({
    value = 'a',
  }: {
    value?: string | null
  }) {
    const [current, setCurrent] = useState<string | null>(value)
    return (
      <DsRoot>
        <Select
          aria-label="Équipe"
          options={options}
          value={current}
          onChange={setCurrent}
        />
        <p data-testid="value">{current ?? 'null'}</p>
      </DsRoot>
    )
  }

  it('navigates with arrows, skips disabled, selects with Enter, closes with Escape', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    const combobox = screen.getByRole('combobox', { name: 'Équipe' })
    await user.click(combobox)
    expect(screen.getByRole('listbox')).toBeInTheDocument()
    expect(combobox).toHaveAttribute(
      'aria-activedescendant',
      expect.stringContaining('-opt-a'),
    )

    await user.keyboard('{ArrowDown}')
    expect(combobox).toHaveAttribute(
      'aria-activedescendant',
      expect.stringContaining('-opt-c'),
    )

    await user.keyboard('{ArrowDown}')
    expect(combobox).toHaveAttribute(
      'aria-activedescendant',
      expect.stringContaining('-opt-d'),
    )

    await user.keyboard('{ArrowDown}')
    expect(combobox).toHaveAttribute(
      'aria-activedescendant',
      expect.stringContaining('-opt-d'),
    )

    await user.keyboard('{ArrowUp}')
    expect(combobox).toHaveAttribute(
      'aria-activedescendant',
      expect.stringContaining('-opt-c'),
    )

    await user.keyboard('{Enter}')
    expect(screen.getByTestId('value')).toHaveTextContent('c')
    await waitFor(() => {
      expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
    })

    await user.click(combobox)
    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
    })
  })

  it('opens with ArrowDown and seeds highlight from the current value', async () => {
    const user = userEvent.setup()
    render(<Harness value="c" />)

    const combobox = screen.getByRole('combobox', { name: 'Équipe' })
    combobox.focus()
    await user.keyboard('{ArrowDown}')

    expect(screen.getByRole('listbox')).toBeInTheDocument()
    expect(combobox).toHaveAttribute(
      'aria-activedescendant',
      expect.stringContaining('-opt-c'),
    )
  })
})

describe('page selection layer under ConfirmDialog', () => {
  it('lets ConfirmDialog win Escape over a page selection dismiss', async () => {
    const user = userEvent.setup()

    function Harness() {
      const [selected, setSelected] = useState(true)
      const [confirmOpen, setConfirmOpen] = useState(false)

      useDismissLayer(selected, () => {
        setSelected(false)
      })

      return (
        <DsRoot>
          <p data-testid="selection">{selected ? 'yes' : 'no'}</p>
          <button type="button" onClick={() => setConfirmOpen(true)}>
            Supprimer
          </button>
          <ConfirmDialog
            open={confirmOpen}
            title="Supprimer ?"
            message="Irréversible"
            confirmLabel="Supprimer"
            cancelLabel="Annuler"
            danger
            onCancel={() => setConfirmOpen(false)}
            onConfirm={() => setConfirmOpen(false)}
          />
        </DsRoot>
      )
    }

    render(<Harness />)
    expect(screen.getByTestId('selection')).toHaveTextContent('yes')

    await user.click(screen.getByRole('button', { name: 'Supprimer' }))
    await screen.findByRole('dialog', { name: 'Supprimer ?' })

    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
    expect(screen.getByTestId('selection')).toHaveTextContent('yes')

    await user.keyboard('{Escape}')
    expect(screen.getByTestId('selection')).toHaveTextContent('no')
  })
})
