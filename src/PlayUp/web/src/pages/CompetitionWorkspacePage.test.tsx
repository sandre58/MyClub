import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, fetchCompetitionWorkspace } from '../api'
import type { WorkspaceSummary } from '../types'
import { CompetitionWorkspacePage } from './CompetitionWorkspacePage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchCompetitionWorkspace: vi.fn(),
  }
})

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'

function workspace(
  overrides: Partial<WorkspaceSummary> = {},
): WorkspaceSummary {
  return {
    id: competitionId,
    name: 'Spring Cup',
    status: 'Draft',
    nextActionCode: 'ContinueOrganisation',
    nextActionLabel: 'Continuer la préparation',
    attentionCount: 0,
    completionMode: null,
    canCompleteNormally: false,
    completionBlockers: null,
    ...overrides,
  }
}

function renderWorkspacePage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/competitions/${competitionId}`]}>
        <Routes>
          <Route
            path="/competitions/:competitionId"
            element={<CompetitionWorkspacePage />}
          />
          <Route
            path="/competitions/:competitionId/overview"
            element={<p>Overview route</p>}
          />
          <Route
            path="/competitions/:competitionId/organisation"
            element={<p>Organisation route</p>}
          />
          <Route path="/competitions" element={<p>List route</p>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('CompetitionWorkspacePage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading while the workspace is pending', () => {
    vi.mocked(fetchCompetitionWorkspace).mockReturnValue(new Promise(() => {}))

    renderWorkspacePage()

    expect(screen.getByRole('status')).toHaveTextContent('Loading…')
  })

  it('renders the workspace summary from the Host DTO', async () => {
    vi.mocked(fetchCompetitionWorkspace).mockResolvedValue(
      workspace({ attentionCount: 2 }),
    )

    renderWorkspacePage()

    expect(
      await screen.findByRole('heading', { name: 'Spring Cup' }),
    ).toBeInTheDocument()
    expect(screen.getByText('Draft')).toBeInTheDocument()
    expect(screen.getByText('Continuer la préparation')).toBeInTheDocument()
    expect(screen.getByText('2')).toBeInTheDocument()
  })

  it('shows an error when the workspace read fails', async () => {
    vi.mocked(fetchCompetitionWorkspace).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    )

    renderWorkspacePage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Not found. Check the id in the URL.',
    )
  })

  it('navigates to competition overview from Continue', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitionWorkspace).mockResolvedValue(workspace())

    renderWorkspacePage()

    await user.click(
      await screen.findByRole('link', { name: /Stages & entries/i }),
    )

    expect(screen.getByText('Overview route')).toBeInTheDocument()
  })

  it('navigates to organisation hub from Continue', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitionWorkspace).mockResolvedValue(workspace())

    renderWorkspacePage()

    await user.click(
      await screen.findByRole('link', { name: /Organisation/i }),
    )

    expect(screen.getByText('Organisation route')).toBeInTheDocument()
  })

  it('navigates back to the competition list', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitionWorkspace).mockResolvedValue(workspace())

    renderWorkspacePage()

    await user.click(
      await screen.findByRole('link', { name: /Back to competitions/i }),
    )

    expect(screen.getByText('List route')).toBeInTheDocument()
  })
})
