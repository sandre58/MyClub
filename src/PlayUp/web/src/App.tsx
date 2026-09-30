import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from './AppLayout';
import { LoadingState } from './ui';
import { CompetitionOverviewPage } from './pages/competition/CompetitionOverviewPage';
import { ClassementsPage } from './pages/classements/ClassementsPage';
import { HomePage } from './pages/HomePage';
import { MatchHubPage } from './pages/match/MatchHubPage';
import { MatchPage } from './pages/match/MatchPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { StructurePage } from './pages/structure/StructurePage';
import { StageMatchesPage } from './pages/stage/StageMatchesPage';
import { StagePage } from './pages/stage/StagePage';
import { RegulationPage } from './pages/regulation/RegulationPage';
import { TeamsPage } from './pages/teams/TeamsPage';

const FoundationsPlayground = lazy(async () => {
  const module = await import('./dev/FoundationsPlayground');
  return { default: module.FoundationsPlayground };
});

const DesignLabPage = lazy(async () => {
  const module = await import('./design-lab/DesignLabPage');
  return { default: module.DesignLabPage };
});

/**
 * Route table only.
 *
 * `/` and `/competitions` (redirect to Home) are outside AppLayout: Home hub
 * (pre-competition entry). Competition shell starts at `/competitions/:id…`.
 *
 * `/dev/foundations` is outside AppLayout: foundations validation terrain,
 * not organizer chrome. Lazy so Plex and DS CSS stay off the product bundle.
 *
 * `/design-lab` is outside AppLayout too: design-system prototype
 * with static data, own shell, no product surface touched.
 *
 * Nested under AppLayout so Outlet swaps page content while the shell stays.
 * Params (:competitionId, :stageId, :matchId, :entryId) are opaque ids — not business fields.
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
      <Route path="/competitions" element={<Navigate to="/" replace />} />
      <Route element={<AppLayout />}>
        <Route
          path="/competitions/:competitionId"
          element={<CompetitionOverviewPage />}
        />
        <Route
          path="/competitions/:competitionId/teams/:entryId"
          element={<TeamsPage />}
        />
        <Route
          path="/competitions/:competitionId/teams"
          element={<TeamsPage />}
        />
        <Route
          path="/competitions/:competitionId/regulation"
          element={<RegulationPage />}
        />
        <Route
          path="/competitions/:competitionId/structure"
          element={<StructurePage />}
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
        <Route path="/stages/:stageId/matches" element={<StageMatchesPage />} />
        <Route path="/matches/:matchId" element={<MatchPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
