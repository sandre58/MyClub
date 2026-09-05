import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { ConfirmDialog } from './ConfirmDialog'

function Harness() {
  const [open, setOpen] = useState(false)
  const [confirmed, setConfirmed] = useState(false)

  return (
    <div className="ds-root" data-font="plex" data-palette="slate">
      <button type="button" onClick={() => setOpen(true)}>
        Ouvrir
      </button>
      {confirmed ? <p>Confirmé</p> : null}
      <ConfirmDialog
        open={open}
        title="Quitter sans enregistrer ?"
        message="Les modifications seront perdues."
        confirmLabel="Abandonner"
        cancelLabel="Annuler"
        onCancel={() => setOpen(false)}
        onConfirm={() => {
          setOpen(false)
          setConfirmed(true)
        }}
      />
    </div>
  )
}

describe('ConfirmDialog', () => {
  it('focuses the primary action so Enter confirms and Escape cancels', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    await user.click(screen.getByRole('button', { name: 'Ouvrir' }))
    const dialog = await screen.findByRole('dialog', {
      name: 'Quitter sans enregistrer ?',
    })
    const confirm = within(dialog).getByRole('button', { name: 'Abandonner' })
    await waitFor(() => {
      expect(confirm).toHaveFocus()
    })

    await user.keyboard('{Enter}')
    expect(await screen.findByText('Confirmé')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Ouvrir' }))
    await screen.findByRole('dialog', { name: 'Quitter sans enregistrer ?' })
    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })

  it('confirms and cancels', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    await user.click(screen.getByRole('button', { name: 'Ouvrir' }))
    const dialog = await screen.findByRole('dialog', {
      name: 'Quitter sans enregistrer ?',
    })
    expect(
      within(dialog).getByText('Les modifications seront perdues.'),
    ).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: 'Annuler' }))
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Ouvrir' }))
    await user.click(
      within(
        await screen.findByRole('dialog', {
          name: 'Quitter sans enregistrer ?',
        }),
      ).getByRole('button', { name: 'Abandonner' }),
    )
    expect(await screen.findByText('Confirmé')).toBeInTheDocument()
  })

  it('calls onCancel on Escape', async () => {
    const user = userEvent.setup()
    const onCancel = vi.fn()
    render(
      <div className="ds-root" data-font="plex" data-palette="slate">
        <ConfirmDialog
          open
          title="Confirm"
          message="Body"
          confirmLabel="OK"
          cancelLabel="Annuler"
          onCancel={onCancel}
          onConfirm={() => undefined}
        />
      </div>,
    )

    await screen.findByRole('dialog')
    await user.keyboard('{Escape}')
    expect(onCancel).toHaveBeenCalled()
  })

  it('shows a spinner on the confirm action while pending', () => {
    render(
      <div className="ds-root" data-font="plex" data-palette="slate">
        <ConfirmDialog
          open
          title="Retirer ?"
          message="Cette inscription sera perdue."
          confirmLabel="Retirer"
          cancelLabel="Annuler"
          confirmPending
          confirmPendingLabel="Retrait…"
          danger
          onCancel={() => undefined}
          onConfirm={() => undefined}
        />
      </div>,
    )

    const dialog = screen.getByRole('dialog', { name: 'Retirer ?' })
    const confirm = within(dialog).getByRole('button', { name: 'Retrait…' })
    expect(confirm).toBeDisabled()
    expect(confirm.querySelector('.ds-spinner')).not.toBeNull()
    expect(within(dialog).getByRole('button', { name: 'Annuler' })).toBeDisabled()
  })
})
