import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  type FormEvent,
  type RefObject,
  useId,
  useRef,
  useState,
} from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { createCompetition, fetchCompetitions } from '../api'
import { queryKeys } from '../queryKeys'
import {
  CompetitionStatusBadge,
  EmptyState,
  ErrorState,
  LoadingState,
  MutationError,
  PageHeader,
  PendingLabel,
} from '../ui'
import {
  COMPETITION_NAME_MAX_LENGTH,
  type CompetitionListItem,
} from '../types'

/**
 * Organizer Competition List — GET /competitions + create (POST).
 * Empty list is no longer a dead end: create → Organisation.
 */
export function CompetitionsPage() {
  const { t } = useTranslation('competitions')
  const formId = useId()
  const nameInputRef = useRef<HTMLInputElement>(null)
  const query = useQuery({
    queryKey: queryKeys.competitions.all,
    queryFn: fetchCompetitions,
  })

  const focusCreateForm = () => {
    nameInputRef.current?.focus()
  }

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('eyebrow')}
        title={t('title')}
        back={{ to: '/', label: t('back') }}
        lede={
          query.data
            ? t('lede', { count: query.data.length })
            : undefined
        }
      />

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && (
        <CompetitionList
          items={query.data}
          onCreateClick={focusCreateForm}
          createFormId={formId}
        />
      )}

      {query.data && (
        <CreateCompetitionForm formId={formId} nameInputRef={nameInputRef} />
      )}
    </main>
  )
}

function CompetitionList({
  items,
  onCreateClick,
  createFormId,
}: {
  items: CompetitionListItem[]
  onCreateClick: () => void
  createFormId: string
}) {
  const { t } = useTranslation('competitions')

  if (items.length === 0) {
    return (
      <EmptyState
        title={t('emptyTitle')}
        action={
          <button
            type="button"
            className="btn btn--primary"
            onClick={onCreateClick}
            aria-controls={createFormId}
          >
            {t('create.cta')}
          </button>
        }
      >
        {t('emptyBody')}
      </EmptyState>
    )
  }

  return (
    <>
      <ul className="row-list">
        {items.map((item) => (
          <li key={item.id}>
            <Link className="row" to={`/competitions/${item.id}`}>
              <span className="row__main">
                <span className="row__title">{item.name}</span>
              </span>
              <span className="row__aside">
                <CompetitionStatusBadge status={item.status} />
                <span className="row__chevron" aria-hidden="true">
                  →
                </span>
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </>
  )
}

function CreateCompetitionForm({
  formId,
  nameInputRef,
}: {
  formId: string
  nameInputRef: RefObject<HTMLInputElement | null>
}) {
  const { t } = useTranslation('competitions')
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [name, setName] = useState('')

  const mutation = useMutation({
    mutationFn: () => createCompetition({ name: name.trim() }),
    onSuccess: async (created) => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.all,
      })
      void navigate(`/competitions/${created.id}/organisation`)
    },
  })

  const trimmed = name.trim()
  const canSubmit =
    trimmed.length > 0 &&
    trimmed.length <= COMPETITION_NAME_MAX_LENGTH &&
    !mutation.isPending

  return (
    <section className="card" aria-labelledby={`${formId}-heading`}>
      <div className="card__head">
        <h2 className="card__title" id={`${formId}-heading`}>
          {t('create.heading')}
        </h2>
        <p className="card__subtitle">{t('create.subtitle')}</p>
      </div>

      <form
        id={formId}
        className="form form--inline"
        onSubmit={(event: FormEvent) => {
          event.preventDefault()
          if (!canSubmit) {
            return
          }
          mutation.mutate()
        }}
      >
        <label className="field">
          {t('create.nameLabel')}
          <input
            ref={nameInputRef}
            value={name}
            onChange={(event) => setName(event.target.value)}
            disabled={mutation.isPending}
            placeholder={t('create.namePlaceholder')}
            maxLength={COMPETITION_NAME_MAX_LENGTH}
            required
            autoComplete="off"
          />
        </label>
        <button
          type="submit"
          className="btn btn--primary"
          disabled={!canSubmit}
        >
          {mutation.isPending ? (
            <PendingLabel>{t('create.submitting')}</PendingLabel>
          ) : (
            t('create.submit')
          )}
        </button>
        {mutation.isError && <MutationError error={mutation.error} />}
      </form>
    </section>
  )
}
