import { lazy, Suspense } from 'react'
import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './AppLayout'
import { LoadingState } from './ui'
import { CompetitionPage } from './pages/CompetitionPage'
import { CompetitionWorkspacePage } from './pages/CompetitionWorkspacePage'
import { CompetitionsPage } from './pages/CompetitionsPage'
import { HomePage } from './pages/HomePage'
import { MatchHubPage } from './pages/MatchHubPage'
import { MatchPage } from './pages/MatchPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { OrganisationPage } from './pages/OrganisationPage'
import { StageMatchesPage } from './pages/StageMatchesPage'
import { StagePage } from './pages/StagePage'

const FoundationsPlayground = lazy(async () => {
  const module = await import('./dev/FoundationsPlayground')
  return { default: module.FoundationsPlayground }
})

/**
 * Route table only.
 *
 * /dev/foundations is outside AppLayout: 14.5 validation terrain,
 * not organizer chrome. Lazy so Plex/Inter and DS CSS stay off the 13.5 bundle.
 *
 * Nested under AppLayout so Outlet swaps page content while the 14.6 shell stays.
 * Params (:competitionId, :stageId, :matchId) are opaque ids — not business fields.
 */
export default function App() {
  return (
    <Routes>
      <Route
        path="/dev/foundations"
        element={
          <Suspense fallback={<LoadingState />}>
            <FoundationsPlayground />
          </Suspense>
        }
      />
      <Route element={<AppLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/competitions" element={<CompetitionsPage />} />
        <Route
          path="/competitions/:competitionId"
          element={<CompetitionWorkspacePage />}
        />
        <Route
          path="/competitions/:competitionId/organisation"
          element={<OrganisationPage />}
        />
        <Route
          path="/competitions/:competitionId/overview"
          element={<CompetitionPage />}
        />
        <Route
          path="/competitions/:competitionId/matches"
          element={<MatchHubPage />}
        />
        <Route path="/stages/:stageId" element={<StagePage />} />
        <Route
          path="/stages/:stageId/matches"
          element={<StageMatchesPage />}
        />
        <Route path="/matches/:matchId" element={<MatchPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
