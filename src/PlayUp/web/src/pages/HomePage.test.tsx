import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { HomePage } from './HomePage'

function renderHome() {
  render(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/competitions" element={<p>Competitions list</p>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('HomePage', () => {
  it('exposes Competition list as the product entry point', () => {
    renderHome()

    expect(
      screen.getByRole('heading', { name: 'Bienvenue' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: /Liste des compétitions/i }),
    ).toHaveAttribute('href', '/competitions')
  })

  it('navigates to the competition list', async () => {
    const user = userEvent.setup()
    renderHome()

    await user.click(
      screen.getByRole('link', { name: /Liste des compétitions/i }),
    )

    expect(screen.getByText('Competitions list')).toBeInTheDocument()
  })
})
