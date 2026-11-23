import { useCallback } from 'react';
import { useNavigate, type NavigateFunction } from 'react-router-dom';
import { ApiError } from '../../api';
import { apiErrorLabel } from '../../i18n/apiErrorLabel';

export const MATCH_NOT_FOUND_NOTICE_KEY = 'matchNotFoundNotice';

export function isMatchNotFound(error: unknown): boolean {
  return (
    error instanceof ApiError &&
    error.status === 404 &&
    error.code === 'Application.MatchNotFound'
  );
}

export function navigateOnMatchNotFound(
  navigate: NavigateFunction,
  stageId: string,
  notice: string,
): void {
  navigate(`/stages/${stageId}/matches`, {
    replace: true,
    state: { [MATCH_NOT_FOUND_NOTICE_KEY]: notice },
  });
}

/** Handles 404 Application.MatchNotFound after match mutations (A'7). */
export function useMatchNotFoundHandler(stageId: string) {
  const navigate = useNavigate();

  return useCallback(
    (error: unknown) => {
      if (isMatchNotFound(error) && error instanceof ApiError) {
        const notice = apiErrorLabel(error) ?? error.message;
        navigateOnMatchNotFound(navigate, stageId, notice);
      }
    },
    [navigate, stageId],
  );
}
