import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AppLayout } from '../AppLayout'
import { HomePage } from '../pages/HomePage'

function renderWithShell(initialEntry: string) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route element={<AppLayout />}>
            <Route path="/" element={<HomePage />} />
            <Route
              path="/competitions/:competitionId"
              element={<p>Workspace page</p>}
            />
            <Route path="/matches/:matchId" element={<p>Match deep link</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('AppShell', () => {
  it('renders the matched page through Outlet', () => {
    renderWithShell('/')

    expect(
      screen.getByRole('heading', { name: 'Welcome' }),
    ).toBeInTheDocument()
  })

  it('exposes the main landmark from the page content', () => {
    renderWithShell('/')

    expect(document.getElementById('main')).toBeInTheDocument()
  })

  it('exposes primary navigation and skip link', () => {
    renderWithShell('/')

    expect(
      screen.getByRole('navigation', { name: 'Primary' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Skip to content' }),
    ).toHaveAttribute('href', '#main')
  })

  it('toggles sidebar expanded/collapsed state', async () => {
    const user = userEvent.setup()
    renderWithShell('/')

    const toggle = screen.getByRole('button', { name: 'Collapse sidebar' })
    expect(toggle).toHaveAttribute('aria-expanded', 'true')

    await user.click(toggle)

    expect(
      screen.getByRole('button', { name: 'Expand sidebar' }),
    ).toHaveAttribute('aria-expanded', 'false')
  })

  it('continues to render deep-link routes inside the shell', () => {
    renderWithShell('/matches/11111111-1111-1111-1111-111111111111')

    expect(screen.getByText('Match deep link')).toBeInTheDocument()
    expect(document.getElementById('main')).not.toBeInTheDocument()
  })
})
