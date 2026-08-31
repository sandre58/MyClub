import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { CompetitionsPage } from './CompetitionsPage'

describe('CompetitionsPage', () => {
  it('redirects legacy /competitions to Accueil hub /', () => {
    render(
      <MemoryRouter initialEntries={['/competitions']}>
        <Routes>
          <Route path="/competitions" element={<CompetitionsPage />} />
          <Route path="/" element={<p>Accueil hub</p>} />
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('Accueil hub')).toBeInTheDocument()
  })
})
