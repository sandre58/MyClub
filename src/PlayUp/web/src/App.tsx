import { useTranslation } from 'react-i18next';
import { type ComponentType, lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from './AppLayout';
import { WaitMark } from './design-system/components/WaitMark';
import { HomePage } from './pages/HomePage';

function lazyNamedPage<
  Module extends Record<string, ComponentType>,
  Key extends keyof Module & string,
>(loader: () => Promise<Module>, exportName: Key) {
  return lazy(async () => {
    const module = await loader();
    return { default: module[exportName] };
  });
}

const ClassementsPage = lazyNamedPage(
  () => import('./pages/classements/ClassementsPage'),
  'ClassementsPage',
);
const CompetitionOverviewPage = lazyNamedPage(
  () => import('./pages/competition/CompetitionOverviewPage'),
  'CompetitionOverviewPage',
);
const MatchHubPage = lazyNamedPage(
  () => import('./pages/match/MatchHubPage'),
  'MatchHubPage',
);
const MatchPage = lazyNamedPage(
  () => import('./pages/match/MatchPage'),
  'MatchPage',
);
const NotFoundPage = lazyNamedPage(
  () => import('./pages/NotFoundPage'),
  'NotFoundPage',
);
const RegulationPage = lazyNamedPage(
  () => import('./pages/regulation/RegulationPage'),
  'RegulationPage',
);
const StageMatchesPage = lazyNamedPage(
  () => import('./pages/stage/StageMatchesPage'),
  'StageMatchesPage',
);
const StageLegacyRedirect = lazyNamedPage(
  () => import('./pages/stage/StageLegacyRedirect'),
  'StageLegacyRedirect',
);
const StructurePage = lazyNamedPage(
  () => import('./pages/structure/StructurePage'),
  'StructurePage',
);
const TeamsPage = lazyNamedPage(
  () => import('./pages/teams/TeamsPage'),
  'TeamsPage',
);

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
 * Product pages are lazy-loaded; AppShell wraps Outlet in Suspense.
 * Params (:competitionId, :stageId, :matchId, :entryId) are opaque ids — not business fields.
 */
function RouteFallback() {
  const { t } = useTranslation('common');
  return <WaitMark>{t('loading')}</WaitMark>;
}

export default function App() {
  return (
    <Routes>
      <Route
        path="/dev/foundations"
        element={
          <Suspense fallback={<RouteFallback />}>
            <FoundationsPlayground />
          </Suspense>
        }
      />
      <Route
        path="/design-lab"
        element={
          <Suspense fallback={<RouteFallback />}>
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
        <Route path="/stages/:stageId/matches" element={<StageMatchesPage />} />
        <Route path="/stages/:stageId" element={<StageLegacyRedirect />} />
        <Route path="/matches/:matchId" element={<MatchPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
