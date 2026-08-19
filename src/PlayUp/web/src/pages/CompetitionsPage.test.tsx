import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, fetchCompetitions } from '../api'
import type { CompetitionListItem } from '../types'
import { CompetitionsPage } from './CompetitionsPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchCompetitions: vi.fn(),
  }
})

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'

function listItem(
  overrides: Partial<CompetitionListItem> = {},
): CompetitionListItem {
  return {
    id: competitionId,
    name: 'Spring Cup',
    status: 'Draft',
    ...overrides,
  }
}

function renderCompetitionsPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/competitions']}>
        <Routes>
          <Route path="/competitions" element={<CompetitionsPage />} />
          <Route
            path="/competitions/:competitionId"
            element={<p>Workspace route</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('CompetitionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading while the list is pending', () => {
    vi.mocked(fetchCompetitions).mockReturnValue(new Promise(() => {}))

    renderCompetitionsPage()

    expect(screen.getByRole('status')).toHaveTextContent('Chargement…')
  })

  it('shows an empty state when the Host returns no competitions', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([])

    renderCompetitionsPage()

    expect(
      await screen.findByText(/No competitions yet/i),
    ).toBeInTheDocument()
  })

  it('renders competition rows with string status labels', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([
      listItem({ status: 'Running' }),
      listItem({
        id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        name: 'Autumn League',
        status: 'Ready',
      }),
    ])

    renderCompetitionsPage()

    expect(
      await screen.findByRole('link', { name: /Spring Cup/i }),
    ).toBeInTheDocument()
    expect(screen.getByText('Running')).toBeInTheDocument()
    expect(screen.getByText('Autumn League')).toBeInTheDocument()
    expect(screen.getByText('Ready')).toBeInTheDocument()
  })

  it('navigates to the competition workspace on row click', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitions).mockResolvedValue([listItem()])

    renderCompetitionsPage()

    await user.click(
      await screen.findByRole('link', { name: /Spring Cup/i }),
    )

    expect(screen.getByText('Workspace route')).toBeInTheDocument()
  })

  it('shows an error when the list read fails', async () => {
    vi.mocked(fetchCompetitions).mockRejectedValue(
      new ApiError(500, 'Host unavailable'),
    )

    renderCompetitionsPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Host unavailable (500)',
    )
  })
})
