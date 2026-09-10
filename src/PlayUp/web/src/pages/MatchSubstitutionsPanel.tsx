import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query';
import { useState, type SubmitEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  correctRecordedSubstitution,
  recordSubstitution,
  removeRecordedSubstitution,
} from '../api';
import { queryKeys } from '../queryKeys';
import { EmptyState, MutationError, PendingLabel } from '../ui';
import type {
  DeclaredParticipation,
  MatchDetail,
  MatchSide,
  RecordedSubstitution,
  RecordSubstitutionRequest,
} from '../types';
import {
  canMutateRecordedSubstitutions,
  deriveOnFieldMembers,
} from './matchSubstitutionsHelpers';

/**
 * Nominative substitutions panel (Lot 1 Remplacements).
 * Faits ≠ feuille déclarative ≠ RunningScore ≠ Finish.
 * Présence dérivée (baseline Starter + journal) = pickers only.
 */
export function MatchSubstitutionsPanel({ match }: { match: MatchDetail }) {
  const { t } = useTranslation('matches');
  const { t: tc } = useTranslation('common');
  const queryClient = useQueryClient();
  const canMutate = canMutateRecordedSubstitutions(match);
  const sheet = match.declaredParticipations ?? [];
  const substitutions = match.recordedSubstitutions ?? [];

  const [side, setSide] = useState<MatchSide>('Home');
  const [outMemberId, setOutMemberId] = useState('');
  const [inMemberId, setInMemberId] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [pendingRemoveId, setPendingRemoveId] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: (request: RecordSubstitutionRequest) =>
      recordSubstitution(match.matchId, request),
    onSuccess: async () => {
      resetForm();
      await invalidateMatchSubs(queryClient, match);
    },
  });

  const correctMutation = useMutation({
    mutationFn: ({
      substitutionId,
      request,
    }: {
      substitutionId: string;
      request: RecordSubstitutionRequest;
    }) => correctRecordedSubstitution(match.matchId, substitutionId, request),
    onSuccess: async () => {
      setEditingId(null);
      resetForm();
      await invalidateMatchSubs(queryClient, match);
    },
  });

  const removeMutation = useMutation({
    mutationFn: (substitutionId: string) =>
      removeRecordedSubstitution(match.matchId, substitutionId),
    onSuccess: async () => {
      setPendingRemoveId(null);
      await invalidateMatchSubs(queryClient, match);
    },
    onError: () => {
      setPendingRemoveId(null);
    },
  });

  function resetForm() {
    setSide('Home');
    setOutMemberId('');
    setInMemberId('');
  }

  function beginEdit(sub: RecordedSubstitution) {
    setPendingRemoveId(null);
    setEditingId(sub.substitutionId);
    setSide(sub.side);
    setOutMemberId(sub.outMemberId);
    setInMemberId(sub.inMemberId);
  }

  const editingIndex =
    editingId == null
      ? -1
      : substitutions.findIndex((row) => row.substitutionId === editingId);

  const presenceUpTo = editingIndex >= 0 ? editingIndex : substitutions.length;

  const onField = deriveOnFieldMembers(
    sheet,
    substitutions,
    side,
    presenceUpTo,
  );
  const sideSheet = sheet.filter((row) => row.side === side);
  const outOptions = sideSheet.filter((row) => onField.has(row.memberId));
  const inOptions = sideSheet.filter(
    (row) => !onField.has(row.memberId) && row.memberId !== outMemberId,
  );

  const hasStarters = sheet.some((row) => row.compositionStatus === 'Starter');
  const mutationError =
    createMutation.error ?? correctMutation.error ?? removeMutation.error;
  const busy =
    createMutation.isPending ||
    correctMutation.isPending ||
    removeMutation.isPending;

  return (
    <section className="ds-panel" aria-labelledby="subs-heading">
      <h2 className="matches-panel__head" id="subs-heading">
        {t('subs.heading')}
      </h2>
      <p className="matches-panel__meta">{t('subs.hint')}</p>

      {!canMutate && (
        <p className="ds-notice ds-notice--info">{t('subs.readOnly')}</p>
      )}

      {canMutate && sheet.length === 0 && (
        <p className="ds-notice ds-notice--info">{t('subs.needSheet')}</p>
      )}

      {canMutate && sheet.length > 0 && !hasStarters && (
        <p className="ds-notice ds-notice--info">{t('subs.needStarters')}</p>
      )}

      {substitutions.length === 0 ? (
        <EmptyState title={t('subs.emptyTitle')}>
          {t('subs.emptyBody')}
        </EmptyState>
      ) : (
        <ul className="match-subs__list">
          {substitutions.map((sub) => (
            <li key={sub.substitutionId}>
              <div className="match-subs__row">
                <div className="match-subs__identity">
                  <span className="match-subs__pair">
                    {sub.outDisplayName ?? sub.outMemberId}
                    {' → '}
                    {sub.inDisplayName ?? sub.inMemberId}
                  </span>
                  <span className="match-subs__meta">
                    {sub.side === 'Home' ? t('detail.home') : t('detail.away')}
                  </span>
                </div>

                {canMutate &&
                  editingId !== sub.substitutionId &&
                  pendingRemoveId !== sub.substitutionId && (
                    <div className="match-subs__row-actions">
                      <button
                        type="button"
                        className="matches-action"
                        disabled={busy}
                        onClick={() => beginEdit(sub)}
                      >
                        {t('subs.correct')}
                      </button>
                      <button
                        type="button"
                        className="matches-action"
                        disabled={busy}
                        onClick={() => {
                          setEditingId(null);
                          setPendingRemoveId(sub.substitutionId);
                        }}
                      >
                        {t('subs.remove')}
                      </button>
                    </div>
                  )}

                {canMutate && editingId === sub.substitutionId && (
                  <SubstitutionForm
                    side={side}
                    outMemberId={outMemberId}
                    inMemberId={inMemberId}
                    outOptions={outOptions}
                    inOptions={inOptions}
                    pending={correctMutation.isPending}
                    submitLabel={t('subs.saveCorrect')}
                    pendingLabel={t('subs.saving')}
                    onSideChange={(next) => {
                      setSide(next);
                      setOutMemberId('');
                      setInMemberId('');
                    }}
                    onOutChange={(id) => {
                      setOutMemberId(id);
                      if (inMemberId === id) {
                        setInMemberId('');
                      }
                    }}
                    onInChange={setInMemberId}
                    onCancel={() => {
                      setEditingId(null);
                      resetForm();
                    }}
                    onSubmit={(request) =>
                      correctMutation.mutate({
                        substitutionId: sub.substitutionId,
                        request,
                      })
                    }
                  />
                )}

                {canMutate && pendingRemoveId === sub.substitutionId && (
                  <div
                    className="match-subs__confirm ds-notice ds-notice--warning"
                    role="group"
                  >
                    <p>
                      {t('subs.removeConsequence', {
                        out: sub.outDisplayName ?? sub.outMemberId,
                        in: sub.inDisplayName ?? sub.inMemberId,
                      })}
                    </p>
                    <div className="match-subs__confirm-actions">
                      <button
                        type="button"
                        className="ds-btn ds-btn--destructive"
                        disabled={removeMutation.isPending}
                        onClick={() =>
                          removeMutation.mutate(sub.substitutionId)
                        }
                      >
                        {removeMutation.isPending ? (
                          <PendingLabel>{t('subs.removing')}</PendingLabel>
                        ) : (
                          t('subs.confirmRemove')
                        )}
                      </button>
                      <button
                        type="button"
                        className="ds-btn ds-btn--ghost"
                        disabled={removeMutation.isPending}
                        onClick={() => setPendingRemoveId(null)}
                      >
                        {tc('cancel')}
                      </button>
                    </div>
                  </div>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      {mutationError && <MutationError error={mutationError} />}

      {canMutate && sheet.length > 0 && hasStarters && editingId === null && (
        <SubstitutionForm
          side={side}
          outMemberId={outMemberId}
          inMemberId={inMemberId}
          outOptions={outOptions}
          inOptions={inOptions}
          pending={createMutation.isPending}
          submitLabel={t('subs.addAction')}
          pendingLabel={t('subs.adding')}
          onSideChange={(next) => {
            setSide(next);
            setOutMemberId('');
            setInMemberId('');
          }}
          onOutChange={(id) => {
            setOutMemberId(id);
            if (inMemberId === id) {
              setInMemberId('');
            }
          }}
          onInChange={setInMemberId}
          onSubmit={(request) => createMutation.mutate(request)}
        />
      )}
    </section>
  );
}

async function invalidateMatchSubs(
  queryClient: QueryClient,
  match: MatchDetail,
) {
  await Promise.all([
    queryClient.invalidateQueries({
      queryKey: queryKeys.matches.detail(match.matchId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.matches.byStage(match.stageId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.overview(match.competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.consultation(match.competitionId),
    }),
  ]);
}

function SubstitutionForm({
  side,
  outMemberId,
  inMemberId,
  outOptions,
  inOptions,
  pending,
  submitLabel,
  pendingLabel,
  onSideChange,
  onOutChange,
  onInChange,
  onCancel,
  onSubmit,
}: {
  side: MatchSide;
  outMemberId: string;
  inMemberId: string;
  outOptions: DeclaredParticipation[];
  inOptions: DeclaredParticipation[];
  pending: boolean;
  submitLabel: string;
  pendingLabel: string;
  onSideChange: (side: MatchSide) => void;
  onOutChange: (memberId: string) => void;
  onInChange: (memberId: string) => void;
  onCancel?: () => void;
  onSubmit: (request: RecordSubstitutionRequest) => void;
}) {
  const { t } = useTranslation('matches');
  const { t: tc } = useTranslation('common');

  return (
    <form
      className="form match-subs__form"
      onSubmit={(event: SubmitEvent) => {
        event.preventDefault();
        if (outMemberId.length === 0 || inMemberId.length === 0 || pending) {
          return;
        }
        onSubmit({
          outMemberId,
          inMemberId,
          side,
        });
      }}
    >
      <fieldset className="match-subs__side-fieldset">
        <legend>{t('subs.side')}</legend>
        <label className="match-subs__radio">
          <input
            type="radio"
            name="sub-side"
            checked={side === 'Home'}
            onChange={() => onSideChange('Home')}
            disabled={pending}
          />
          {t('detail.home')}
        </label>
        <label className="match-subs__radio">
          <input
            type="radio"
            name="sub-side"
            checked={side === 'Away'}
            onChange={() => onSideChange('Away')}
            disabled={pending}
          />
          {t('detail.away')}
        </label>
      </fieldset>

      <label className="field">
        {t('subs.out')}
        <select
          value={outMemberId}
          onChange={(event) => onOutChange(event.target.value)}
          disabled={pending}
          required
        >
          <option value="">{t('subs.outPlaceholder')}</option>
          {outOptions.map((row) => (
            <option key={row.memberId} value={row.memberId}>
              {row.displayName ?? row.memberId}
            </option>
          ))}
        </select>
      </label>

      <label className="field">
        {t('subs.in')}
        <select
          value={inMemberId}
          onChange={(event) => onInChange(event.target.value)}
          disabled={pending || outMemberId.length === 0}
          required
        >
          <option value="">{t('subs.inPlaceholder')}</option>
          {inOptions.map((row) => (
            <option key={row.memberId} value={row.memberId}>
              {row.displayName ?? row.memberId}
            </option>
          ))}
        </select>
      </label>

      {outOptions.length === 0 && (
        <p className="match-subs__hint">{t('subs.noOutEligible')}</p>
      )}
      {outMemberId.length > 0 && inOptions.length === 0 && (
        <p className="match-subs__hint">{t('subs.noInEligible')}</p>
      )}

      <div className="match-subs__confirm-actions">
        <button
          type="submit"
          className="ds-btn ds-btn--primary"
          disabled={
            pending || outMemberId.length === 0 || inMemberId.length === 0
          }
        >
          {pending ? <PendingLabel>{pendingLabel}</PendingLabel> : submitLabel}
        </button>
        {onCancel && (
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={pending}
            onClick={onCancel}
          >
            {tc('cancel')}
          </button>
        )}
      </div>
    </form>
  );
}
