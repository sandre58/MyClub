import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query';
import { useState, type SubmitEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  correctRecordedDisciplinaryEvent,
  fetchStructureView,
  recordDisciplinaryEvent,
  removeRecordedDisciplinaryEvent,
} from '../../api';
import { queryKeys } from '../../queryKeys';
import {
  EmptyState,
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
} from '../../ui';
import type {
  DeclaredParticipation,
  DisciplinaryType,
  MatchDetail,
  RecordDisciplinaryEventRequest,
  RecordedDisciplinaryEvent,
} from '../../types';
import {
  CheckIcon,
  PlusIcon,
  TrashIcon,
} from '../../design-system/icons/contentIcons';
import { CloseIcon } from '../../design-system/icons/shellIcons';
import { canMutateRecordedDisciplinaryEvents } from './matchDisciplinaryHelpers';

/**
 * Nominative discipline panel — facts ≠ score ≠ presence ≠ consequences.
 * Types = Structure AllowedTypes; target = anyone on the sheet.
 */
export function MatchDisciplinaryPanel({ match }: { match: MatchDetail }) {
  const { t } = useTranslation('matches');
  const { t: tc } = useTranslation('common');
  const queryClient = useQueryClient();
  const canMutate = canMutateRecordedDisciplinaryEvents(match);
  const sheet = match.declaredParticipations ?? [];
  const events = match.recordedDisciplinaryEvents ?? [];

  const structureQuery = useQuery({
    queryKey: queryKeys.competitions.structure(match.competitionId),
    queryFn: () => fetchStructureView(match.competitionId),
  });

  const allowedTypes = structureQuery.data?.regulation.allowedTypes ?? [];
  const catalogueReady = structureQuery.isSuccess;
  const noneAllowed = catalogueReady && allowedTypes.length === 0;
  const canCreate =
    canMutate && sheet.length > 0 && catalogueReady && allowedTypes.length > 0;

  const [memberId, setMemberId] = useState('');
  const [type, setType] = useState<DisciplinaryType | ''>('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [pendingRemoveId, setPendingRemoveId] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: (request: RecordDisciplinaryEventRequest) =>
      recordDisciplinaryEvent(match.matchId, request),
    onSuccess: async () => {
      resetForm();
      await invalidateMatchDiscipline(queryClient, match);
    },
  });

  const correctMutation = useMutation({
    mutationFn: ({
      disciplinaryEventId,
      request,
    }: {
      disciplinaryEventId: string;
      request: RecordDisciplinaryEventRequest;
    }) =>
      correctRecordedDisciplinaryEvent(
        match.matchId,
        disciplinaryEventId,
        request,
      ),
    onSuccess: async () => {
      setEditingId(null);
      resetForm();
      await invalidateMatchDiscipline(queryClient, match);
    },
  });

  const removeMutation = useMutation({
    mutationFn: (disciplinaryEventId: string) =>
      removeRecordedDisciplinaryEvent(match.matchId, disciplinaryEventId),
    onSuccess: async () => {
      setPendingRemoveId(null);
      await invalidateMatchDiscipline(queryClient, match);
    },
    onError: () => {
      setPendingRemoveId(null);
    },
  });

  function resetForm() {
    setMemberId('');
    setType('');
  }

  function beginEdit(evt: RecordedDisciplinaryEvent) {
    setPendingRemoveId(null);
    setEditingId(evt.disciplinaryEventId);
    setMemberId(evt.memberId);
    setType(evt.type);
  }

  const mutationError =
    createMutation.error ?? correctMutation.error ?? removeMutation.error;
  const busy =
    createMutation.isPending ||
    correctMutation.isPending ||
    removeMutation.isPending;

  return (
    <section className="ds-panel" aria-labelledby="discipline-heading">
      <h2 className="matches-panel__head" id="discipline-heading">
        {t('discipline.heading')}
      </h2>
      <p className="matches-panel__meta">{t('discipline.hint')}</p>

      {!canMutate && (
        <p className="ds-notice ds-notice--info">{t('discipline.readOnly')}</p>
      )}

      {structureQuery.isPending && <LoadingState size="region" />}
      {structureQuery.isError && <ErrorState error={structureQuery.error} />}

      {canMutate && catalogueReady && sheet.length === 0 && (
        <p className="ds-notice ds-notice--info">{t('discipline.needSheet')}</p>
      )}

      {canMutate && noneAllowed && (
        <p className="ds-notice ds-notice--info">
          {t('discipline.noneAllowed')}{' '}
          <Link to={`/competitions/${match.competitionId}/structure`}>
            {t('discipline.configureRegulation')}
          </Link>
        </p>
      )}

      {events.length === 0 ? (
        <EmptyState title={t('discipline.emptyTitle')}>
          {t('discipline.emptyBody')}
        </EmptyState>
      ) : (
        <ul className="match-discipline__list">
          {events.map((evt) => {
            const participation = sheet.find(
              (row) => row.memberId === evt.memberId,
            );
            return (
              <li key={evt.disciplinaryEventId}>
                <div className="match-discipline__row">
                  <div className="match-discipline__identity">
                    <span className="match-discipline__fact">
                      {typeLabel(t, evt.type)}
                      {' · '}
                      {evt.memberDisplayName ?? evt.memberId}
                    </span>
                    {participation && (
                      <span className="match-discipline__meta">
                        {participation.side === 'Home'
                          ? t('detail.home')
                          : t('detail.away')}
                      </span>
                    )}
                  </div>

                  {canMutate &&
                    !noneAllowed &&
                    editingId !== evt.disciplinaryEventId &&
                    pendingRemoveId !== evt.disciplinaryEventId && (
                      <div className="match-discipline__row-actions">
                        <button
                          type="button"
                          className="matches-action"
                          disabled={busy}
                          onClick={() => beginEdit(evt)}
                        >
                          {t('discipline.correct')}
                        </button>
                        <button
                          type="button"
                          className="matches-action"
                          disabled={busy}
                          onClick={() => {
                            setEditingId(null);
                            setPendingRemoveId(evt.disciplinaryEventId);
                          }}
                        >
                          {t('discipline.remove')}
                        </button>
                      </div>
                    )}

                  {canMutate &&
                    !noneAllowed &&
                    editingId === evt.disciplinaryEventId && (
                      <DisciplinaryForm
                        memberId={memberId}
                        type={type}
                        sheet={sheet}
                        allowedTypes={allowedTypes}
                        pending={correctMutation.isPending}
                        submitLabel={t('discipline.saveCorrect')}
                        submitIcon="check"
                        pendingLabel={t('discipline.saving')}
                        onMemberChange={setMemberId}
                        onTypeChange={setType}
                        onCancel={() => {
                          setEditingId(null);
                          resetForm();
                        }}
                        onSubmit={(request) =>
                          correctMutation.mutate({
                            disciplinaryEventId: evt.disciplinaryEventId,
                            request,
                          })
                        }
                      />
                    )}

                  {canMutate && pendingRemoveId === evt.disciplinaryEventId && (
                    <div
                      className="match-discipline__confirm ds-notice ds-notice--warning"
                      role="group"
                    >
                      <p>
                        {t('discipline.removeConsequence', {
                          type: typeLabel(t, evt.type),
                          name: evt.memberDisplayName ?? evt.memberId,
                        })}
                      </p>
                      <div className="match-discipline__confirm-actions">
                        <button
                          type="button"
                          className="ds-btn ds-btn--destructive"
                          disabled={removeMutation.isPending}
                          onClick={() =>
                            removeMutation.mutate(evt.disciplinaryEventId)
                          }
                        >
                          {removeMutation.isPending ? (
                            <PendingLabel>
                              {t('discipline.removing')}
                            </PendingLabel>
                          ) : (
                            <>
                              <TrashIcon size="sm" />
                              {t('discipline.confirmRemove')}
                            </>
                          )}
                        </button>
                        <button
                          type="button"
                          className="ds-btn ds-btn--ghost"
                          disabled={removeMutation.isPending}
                          onClick={() => setPendingRemoveId(null)}
                        >
                          <CloseIcon size="sm" />
                          {tc('cancel')}
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}

      {mutationError && <MutationError error={mutationError} />}

      {canCreate && editingId === null && (
        <DisciplinaryForm
          memberId={memberId}
          type={type}
          sheet={sheet}
          allowedTypes={allowedTypes}
          pending={createMutation.isPending}
          submitLabel={t('discipline.addAction')}
          submitIcon="plus"
          pendingLabel={t('discipline.adding')}
          onMemberChange={setMemberId}
          onTypeChange={setType}
          onSubmit={(request) => createMutation.mutate(request)}
        />
      )}
    </section>
  );
}

function typeLabel(t: (key: string) => string, type: DisciplinaryType): string {
  switch (type) {
    case 'Yellow':
      return t('discipline.typeYellow');
    case 'Red':
      return t('discipline.typeRed');
    case 'White':
      return t('discipline.typeWhite');
  }
}

async function invalidateMatchDiscipline(
  queryClient: QueryClient,
  match: MatchDetail,
) {
  await queryClient.invalidateQueries({
    queryKey: queryKeys.matches.detail(match.matchId),
  });
}

function DisciplinaryForm({
  memberId,
  type,
  sheet,
  allowedTypes,
  pending,
  submitLabel,
  submitIcon,
  pendingLabel,
  onMemberChange,
  onTypeChange,
  onCancel,
  onSubmit,
}: {
  memberId: string;
  type: DisciplinaryType | '';
  sheet: DeclaredParticipation[];
  allowedTypes: DisciplinaryType[];
  pending: boolean;
  submitLabel: string;
  submitIcon: 'plus' | 'check';
  pendingLabel: string;
  onMemberChange: (id: string) => void;
  onTypeChange: (type: DisciplinaryType | '') => void;
  onCancel?: () => void;
  onSubmit: (request: RecordDisciplinaryEventRequest) => void;
}) {
  const { t } = useTranslation('matches');
  const { t: tc } = useTranslation('common');
  const SubmitIcon = submitIcon === 'plus' ? PlusIcon : CheckIcon;

  function handleSubmit(event: SubmitEvent) {
    event.preventDefault();
    if (!memberId || !type) {
      return;
    }

    onSubmit({ memberId, type });
  }

  return (
    <form className="match-discipline__form" onSubmit={handleSubmit}>
      <label className="field">
        <span className="field__label">{t('discipline.member')}</span>
        <select
          className="field__control"
          value={memberId}
          required
          disabled={pending}
          onChange={(e) => onMemberChange(e.target.value)}
        >
          <option value="">{t('discipline.memberPlaceholder')}</option>
          {sheet.map((row) => (
            <option key={row.memberId} value={row.memberId}>
              {row.displayName ?? row.memberId}
              {' · '}
              {row.side === 'Home' ? t('detail.home') : t('detail.away')}
            </option>
          ))}
        </select>
      </label>

      <label className="field">
        <span className="field__label">{t('discipline.type')}</span>
        <select
          className="field__control"
          value={type}
          required
          disabled={pending}
          onChange={(e) =>
            onTypeChange(e.target.value as DisciplinaryType | '')
          }
        >
          <option value="">{t('discipline.typePlaceholder')}</option>
          {allowedTypes.map((allowed) => (
            <option key={allowed} value={allowed}>
              {typeLabel(t, allowed)}
            </option>
          ))}
        </select>
      </label>

      <div className="button-row">
        <button
          type="submit"
          className="ds-btn ds-btn--primary"
          disabled={pending || !memberId || !type}
        >
          {pending ? (
            <PendingLabel>{pendingLabel}</PendingLabel>
          ) : (
            <>
              <SubmitIcon size="sm" />
              {submitLabel}
            </>
          )}
        </button>
        {onCancel && (
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={pending}
            onClick={onCancel}
          >
            <CloseIcon size="sm" />
            {tc('cancel')}
          </button>
        )}
      </div>
    </form>
  );
}
