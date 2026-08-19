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

  return render(
    <QueryClientProvider client={queryClient}>
      <AppShellRoutes initialEntry={initialEntry} />
    </QueryClientProvider>,
  )
}

function AppShellRoutes({ initialEntry }: { initialEntry: string }) {
  return (
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/competitions" element={<p>Competition list</p>} />
          <Route
            path="/competitions/:competitionId"
            element={<p>Workspace page</p>}
          />
          <Route
            path="/competitions/:competitionId/organisation"
            element={<p>Organisation page</p>}
          />
          <Route
            path="/competitions/:competitionId/matches"
            element={<p>Competition matches page</p>}
          />
          <Route
            path="/competitions/:competitionId/overview"
            element={<p>Competition overview page</p>}
          />
          <Route path="/stages/:stageId" element={<p>Stage page</p>} />
          <Route
            path="/stages/:stageId/matches"
            element={<p>Stage matches page</p>}
          />
          <Route
            path="/stages/:stageId/matches"
            element={<p>Stage matches page</p>}
          />
          <Route path="/matches/:matchId" element={<p>Match deep link</p>} />
        </Route>
      </Routes>
    </MemoryRouter>
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

    expect(document.querySelectorAll('#main')).toHaveLength(1)
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

  it('renders the four sidebar destinations', () => {
    renderWithShell('/')

    expect(screen.getByRole('link', { name: 'Cockpit' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Organisation' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Matchs' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Consultation' })).toBeInTheDocument()
  })

  it('marks Cockpit active for workspace routes', () => {
    renderWithShell('/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')

    expect(screen.getByRole('link', { name: 'Cockpit' })).toHaveAttribute(
      'aria-current',
      'page',
    )
    expect(screen.getByRole('link', { name: 'Organisation' })).not.toHaveAttribute(
      'aria-current',
    )
  })

  it('marks Organisation active for organisation routes', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/organisation',
    )

    expect(screen.getByRole('link', { name: 'Organisation' })).toHaveAttribute(
      'aria-current',
      'page',
    )
  })

  it('maps stage deep links to Consultation', () => {
    renderWithShell('/stages/stage-id')

    expect(screen.getByRole('link', { name: 'Consultation' })).toHaveAttribute(
      'aria-current',
      'page',
    )
  })

  it('maps match deep links to Matchs', () => {
    renderWithShell('/matches/match-id')

    expect(screen.getByRole('link', { name: 'Matchs' })).toHaveAttribute(
      'aria-current',
      'page',
    )
  })

  it('sets aria-current on only one destination', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/overview',
    )

    expect(screen.getAllByRole('link', { current: 'page' })).toHaveLength(1)
    expect(screen.getByRole('link', { name: 'Consultation' })).toHaveAttribute(
      'aria-current',
      'page',
    )
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
    expect(screen.getByRole('link', { name: 'Cockpit' })).toBeInTheDocument()
  })

  it('maps stage matches deep links to Matchs', () => {
    renderWithShell('/stages/stage-id/matches')

    expect(screen.getByRole('link', { name: 'Matchs' })).toHaveAttribute(
      'aria-current',
      'page',
    )
  })

  it('continues to render deep-link routes inside the shell', () => {
    renderWithShell('/matches/11111111-1111-1111-1111-111111111111')

    expect(screen.getByText('Match deep link')).toBeInTheDocument()
    expect(document.getElementById('main')).not.toBeInTheDocument()
  })
})
