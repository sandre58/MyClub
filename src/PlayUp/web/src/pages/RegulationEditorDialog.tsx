import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { useId, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { replaceCompetitionRegulation } from '../api'
import { Dialog } from '../design-system/components/Dialog'
import { queryKeys } from '../queryKeys'
import { MutationError, PendingLabel } from '../ui'
import type {
  DisciplinaryType,
  OrganisationView,
  ReplaceRegulationRequest,
} from '../types'

export async function invalidateAfterOrganisationMutation(
  queryClient: QueryClient,
  competitionId: string,
) {
  await Promise.all([
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.organisation(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.detail(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.workspace(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.overview(competitionId),
    }),
  ])
}

/** Shared ReplaceRegulation dialog — Organisation hub + Règlement hub. */
export function RegulationEditorDialog({
  data,
  open,
  onClose,
}: {
  data: OrganisationView
  open: boolean
  onClose: () => void
}) {
  const { t } = useTranslation('organisation')
  const { t: tCommon } = useTranslation('common')
  const queryClient = useQueryClient()
  const formId = useId()
  const regulation = data.regulation
  const [form, setForm] = useState<ReplaceRegulationRequest>({
    minimumTeams: regulation.minimumTeams,
    maximumTeams: regulation.maximumTeams,
    durationPerPeriod: regulation.durationPerPeriod,
    numberOfPeriods: regulation.numberOfPeriods,
    halfTimeDuration: 15,
    winPoints: regulation.winPoints,
    drawPoints: regulation.drawPoints,
    lossPoints: regulation.lossPoints,
    forfeitWinnerGoals: 3,
    forfeitLoserGoals: 0,
    allowedTypes: [...(regulation.allowedTypes ?? [])],
  })

  const mutation = useMutation({
    mutationFn: () => replaceCompetitionRegulation(data.competitionId, form),
    onSuccess: async () => {
      await invalidateAfterOrganisationMutation(
        queryClient,
        data.competitionId,
      )
    },
  })

  const setNumber =
    (key: keyof ReplaceRegulationRequest) =>
    (value: string) => {
      const parsed = Number(value)
      setForm((current) => ({
        ...current,
        [key]: Number.isFinite(parsed) ? parsed : current[key],
      }))
    }

  function toggleAllowedType(type: DisciplinaryType) {
    setForm((current) => {
      const selected = current.allowedTypes ?? []
      const next = selected.includes(type)
        ? selected.filter((item) => item !== type)
        : [...selected, type]
      return { ...current, allowedTypes: next }
    })
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('regulation.replaceLegend')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      size="md"
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={mutation.isPending}
            onClick={onClose}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('regulation.saving')}</PendingLabel>
            ) : (
              t('regulation.save')
            )}
          </button>
        </>
      }
    >
      <form
        id={formId}
        className="form form--wide"
        onSubmit={(event: FormEvent) => {
          event.preventDefault()
          if (mutation.isPending) {
            return
          }
          mutation.mutate()
        }}
      >
        <fieldset className="fieldset" disabled={mutation.isPending}>
          <legend className="fieldset__legend">
            {t('regulation.replaceLegend')}
          </legend>
          <div className="form-row">
            <label className="field">
              {t('regulation.minimumTeams')}
              <input
                type="number"
                value={form.minimumTeams}
                onChange={(event) =>
                  setNumber('minimumTeams')(event.target.value)
                }
                required
              />
            </label>
            <label className="field">
              {t('regulation.maximumTeams')}
              <input
                type="number"
                value={form.maximumTeams}
                onChange={(event) =>
                  setNumber('maximumTeams')(event.target.value)
                }
                required
              />
            </label>
          </div>
          <div className="form-row">
            <label className="field">
              {t('regulation.durationPerPeriod')}
              <input
                type="number"
                value={form.durationPerPeriod}
                onChange={(event) =>
                  setNumber('durationPerPeriod')(event.target.value)
                }
                required
              />
            </label>
            <label className="field">
              {t('regulation.numberOfPeriods')}
              <input
                type="number"
                value={form.numberOfPeriods}
                onChange={(event) =>
                  setNumber('numberOfPeriods')(event.target.value)
                }
                required
              />
            </label>
            <label className="field">
              {t('regulation.halfTimeDuration')}
              <input
                type="number"
                value={form.halfTimeDuration}
                onChange={(event) =>
                  setNumber('halfTimeDuration')(event.target.value)
                }
                required
              />
            </label>
          </div>
          <div className="form-row">
            <label className="field">
              {t('regulation.winPoints')}
              <input
                type="number"
                value={form.winPoints}
                onChange={(event) => setNumber('winPoints')(event.target.value)}
                required
              />
            </label>
            <label className="field">
              {t('regulation.drawPoints')}
              <input
                type="number"
                value={form.drawPoints}
                onChange={(event) =>
                  setNumber('drawPoints')(event.target.value)
                }
                required
              />
            </label>
            <label className="field">
              {t('regulation.lossPoints')}
              <input
                type="number"
                value={form.lossPoints}
                onChange={(event) =>
                  setNumber('lossPoints')(event.target.value)
                }
                required
              />
            </label>
          </div>
          <fieldset className="fieldset fieldset--nested">
            <legend className="fieldset__legend">
              {t('regulation.allowedTypesLegend')}
            </legend>
            <p className="organisation-panel__muted">
              {t('regulation.allowedTypesHint')}
            </p>
            <div className="form-row">
              {(['Yellow', 'Red', 'White'] as const).map((type) => (
                <label key={type} className="field field--checkbox">
                  <input
                    type="checkbox"
                    checked={(form.allowedTypes ?? []).includes(type)}
                    onChange={() => toggleAllowedType(type)}
                  />
                  {t(`regulation.type.${type}`)}
                </label>
              ))}
            </div>
          </fieldset>
        </fieldset>
        <p className="caption">{t('regulation.saveHint')}</p>
        {mutation.isError && <MutationError error={mutation.error} />}
      </form>
    </Dialog>
  )
}
