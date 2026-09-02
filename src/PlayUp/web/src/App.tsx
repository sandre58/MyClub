import { lazy, Suspense } from 'react'
import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './AppLayout'
import { LoadingState } from './ui'
import { CompetitionOverviewPage } from './pages/CompetitionOverviewPage'
import { CompetitionsPage } from './pages/CompetitionsPage'
import { ClassementsPage } from './pages/ClassementsPage'
import { HomePage } from './pages/HomePage'
import { MatchHubPage } from './pages/MatchHubPage'
import { MatchPage } from './pages/MatchPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { EntryRosterPage } from './pages/EntryRosterPage'
import { OrganisationPage } from './pages/OrganisationPage'
import { StageMatchesPage } from './pages/StageMatchesPage'
import { StagePage } from './pages/StagePage'

const FoundationsPlayground = lazy(async () => {
  const module = await import('./dev/FoundationsPlayground')
  return { default: module.FoundationsPlayground }
})

const DesignLabPage = lazy(async () => {
  const module = await import('./design-lab/DesignLabPage')
  return { default: module.DesignLabPage }
})

/**
 * Route table only.
 *
 * / and /competitions (legacy redirect) are outside AppLayout: Accueil hub
 * (pré-compétition). Shell V1 starts at /competitions/:id….
 *
 * /dev/foundations is outside AppLayout: 14.5 validation terrain,
 * not organizer chrome. Lazy so Plex and DS CSS stay off the 13.5 bundle.
 *
 * /design-lab is outside AppLayout too: direction artistique prototype
 * (audit Phase 20) — static data, own shell, no product surface touched.
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
      <Route
        path="/design-lab"
        element={
          <Suspense fallback={<LoadingState />}>
            <DesignLabPage />
          </Suspense>
        }
      />
      <Route path="/" element={<HomePage />} />
      <Route path="/competitions" element={<CompetitionsPage />} />
      <Route element={<AppLayout />}>
        <Route
          path="/competitions/:competitionId"
          element={<CompetitionOverviewPage />}
        />
        <Route
          path="/competitions/:competitionId/organisation/entries/:entryId"
          element={<EntryRosterPage />}
        />
        <Route
          path="/competitions/:competitionId/organisation"
          element={<OrganisationPage />}
        />
        <Route
          path="/competitions/:competitionId/classements"
          element={<ClassementsPage />}
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
