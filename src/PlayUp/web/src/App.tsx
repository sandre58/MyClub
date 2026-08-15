import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './AppLayout'
import { CompetitionPage } from './pages/CompetitionPage'
import { HomePage } from './pages/HomePage'
import { MatchPage } from './pages/MatchPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { StageMatchesPage } from './pages/StageMatchesPage'
import { StagePage } from './pages/StagePage'

/**
 * Route table only.
 *
 * Route = URL pattern → element to render.
 * Nested under AppLayout so Outlet swaps page content while the shell stays.
 * Params (:competitionId, :stageId, :matchId) are opaque ids — not business fields.
 */
export default function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route
          path="/competitions/:competitionId"
          element={<CompetitionPage />}
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
