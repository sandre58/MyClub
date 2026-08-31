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
            path="/competitions/:competitionId/organisation/entries/:entryId"
            element={<p>Roster page</p>}
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
            path="/competitions/:competitionId/classements"
            element={<p>Classements page</p>}
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
      screen.getByRole('heading', { name: 'Bienvenue' }),
    ).toBeInTheDocument()
  })

  it('exposes the main landmark from the page content', () => {
    renderWithShell('/')

    expect(document.querySelectorAll('#main')).toHaveLength(1)
  })

  it('exposes primary navigation and skip link', () => {
    renderWithShell('/')

    expect(
      screen.getByRole('navigation', { name: 'Navigation principale' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Passer au contenu' }),
    ).toHaveAttribute('href', '#main')
  })

  it('renders the four sidebar destinations', () => {
    renderWithShell('/')

    expect(screen.getByRole('link', { name: "Vue d'ensemble" })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Organisation' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Matchs' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Classements' })).toBeInTheDocument()
  })

  it('keeps sidebar nav links outside content-link scope', () => {
    renderWithShell('/')

    const navLink = screen.getByRole('link', { name: "Vue d'ensemble" })
    expect(navLink).toHaveClass('shell-sidebar__link')
    expect(navLink).toHaveAttribute('data-active')
    expect(navLink.closest('.shell-sidebar')).not.toBeNull()
    expect(navLink.closest('.shell-main')).toBeNull()
  })

  it('marks Vue d\'ensemble active for workspace routes', () => {
    renderWithShell('/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')

    expect(screen.getByRole('link', { name: "Vue d'ensemble" })).toHaveAttribute(
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

  it('marks Organisation active for nested roster routes', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/organisation/entries/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    )

    expect(screen.getByRole('link', { name: 'Organisation' })).toHaveAttribute(
      'aria-current',
      'page',
    )
    expect(screen.getByText('Roster page')).toBeInTheDocument()
  })

  it('maps stage deep links to Matchs', () => {
    renderWithShell('/stages/stage-id')

    expect(screen.getByRole('link', { name: 'Matchs' })).toHaveAttribute(
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

  it('marks Classements active for classements routes', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/classements',
    )

    expect(screen.getByRole('link', { name: 'Classements' })).toHaveAttribute(
      'aria-current',
      'page',
    )
  })

  it('does not mark a sidebar destination for overview', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/overview',
    )

    expect(screen.queryByRole('link', { current: 'page' })).not.toBeInTheDocument()
  })

  it('toggles sidebar expanded/collapsed state', async () => {
    const user = userEvent.setup()
    renderWithShell('/')

    const toggle = screen.getByRole('button', { name: 'Réduire la barre latérale' })
    expect(toggle).toHaveAttribute('aria-expanded', 'true')

    await user.click(toggle)

    expect(
      screen.getByRole('button', { name: 'Développer la barre latérale' }),
    ).toHaveAttribute('aria-expanded', 'false')
    expect(screen.getByRole('link', { name: "Vue d'ensemble" })).toBeInTheDocument()
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
