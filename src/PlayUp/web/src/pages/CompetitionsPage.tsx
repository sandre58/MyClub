import { Navigate } from 'react-router-dom';

/**
 * Legacy list URL — Accueil hub lives on `/`.
 * Keep the path so old bookmarks / shell mid-nav still resolve.
 */
export function CompetitionsPage() {
  return <Navigate to="/" replace />;
}
