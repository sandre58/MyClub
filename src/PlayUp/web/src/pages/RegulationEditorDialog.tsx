import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { replaceCompetitionRegulation } from '../api'
import { Alert } from '../design-system/components/Alert'
import { ConfirmDialog } from '../design-system/components/ConfirmDialog'
import { Dialog } from '../design-system/components/Dialog'
import { Field } from '../design-system/components/Field'
import { InputNumber } from '../design-system/components/InputNumber'
import { queryKeys } from '../queryKeys'
import { MutationError, PendingLabel } from '../ui'
import type {
  DisciplinaryType,
  OrganisationView,
  RankingCriterion,
  ReplaceRegulationRequest,
} from '../types'
import './regulation.css'

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

export type RegulationEditorSection =
  | 'entries'
  | 'match'
  | 'standing'
  | 'discipline'

const ALL_CRITERIA: RankingCriterion[] = [
  'Points',
  'GoalDifference',
  'GoalsFor',
  'GoalsAgainst',
  'Wins',
  'HeadToHead',
]

const DEFAULT_CRITERIA: RankingCriterion[] = [
  'Points',
  'GoalDifference',
  'GoalsFor',
  'HeadToHead',
]

function formFromView(data: OrganisationView): ReplaceRegulationRequest {
  const regulation = data.regulation
  return {
    minimumTeams: regulation.minimumTeams,
    maximumTeams: regulation.maximumTeams,
    durationPerPeriod: regulation.durationPerPeriod,
    numberOfPeriods: regulation.numberOfPeriods,
    halfTimeDuration: regulation.halfTimeDuration ?? 15,
    winPoints: regulation.winPoints,
    drawPoints: regulation.drawPoints,
    lossPoints: regulation.lossPoints,
    forfeitWinnerGoals: regulation.forfeitWinnerGoals ?? 3,
    forfeitLoserGoals: regulation.forfeitLoserGoals ?? 0,
    allowedTypes: [...(regulation.allowedTypes ?? [])],
    rankingCriteria: [...(regulation.rankingCriteria ?? DEFAULT_CRITERIA)],
    hasExtraTime: regulation.hasExtraTime === true,
    extraTimeDurationPerPeriod: regulation.extraTimeDurationPerPeriod ?? 15,
    extraTimeNumberOfPeriods: regulation.extraTimeNumberOfPeriods ?? 2,
    hasPenaltyShootout: regulation.hasPenaltyShootout === true,
    penaltyInitialKicksPerTeam: regulation.penaltyInitialKicksPerTeam ?? 5,
  }
}

/** Shared ReplaceRegulation dialog — Règlement hub + Organisation hub (F3). */
export function RegulationEditorDialog({
  data,
  open,
  onClose,
  initialSection = 'entries',
}: {
  data: OrganisationView
  open: boolean
  onClose: () => void
  initialSection?: RegulationEditorSection
}) {
  const { t } = useTranslation('regulation')
  const { t: tCommon } = useTranslation('common')
  const queryClient = useQueryClient()
  const formId = useId()
  const [form, setForm] = useState(() => formFromView(data))
  const [confirmOpen, setConfirmOpen] = useState(false)
  const entriesRef = useRef<HTMLElement>(null)
  const matchRef = useRef<HTMLElement>(null)
  const standingRef = useRef<HTMLElement>(null)
  const disciplineRef = useRef<HTMLElement>(null)

  useEffect(() => {
    if (!open) {
      return
    }
    setForm(formFromView(data))
    setConfirmOpen(false)
  }, [open, data])

  useEffect(() => {
    if (!open) {
      return
    }
    const refs: Record<RegulationEditorSection, typeof entriesRef> = {
      entries: entriesRef,
      match: matchRef,
      standing: standingRef,
      discipline: disciplineRef,
    }
    refs[initialSection].current?.scrollIntoView({
      block: 'start',
      behavior: 'smooth',
    })
  }, [open, initialSection])

  const eligibleStages = data.stages.filter(
    (stage) => stage.status === 'Draft' || stage.status === 'Ready',
  )
  const needsReadyConfirm = data.status === 'Ready'

  const mutation = useMutation({
    mutationFn: () => {
      const body: ReplaceRegulationRequest = {
        ...form,
        extraTimeDurationPerPeriod: form.hasExtraTime
          ? form.extraTimeDurationPerPeriod
          : null,
        extraTimeNumberOfPeriods: form.hasExtraTime
          ? form.extraTimeNumberOfPeriods
          : null,
        penaltyInitialKicksPerTeam: form.hasPenaltyShootout
          ? form.penaltyInitialKicksPerTeam
          : null,
      }
      return replaceCompetitionRegulation(data.competitionId, body)
    },
    onSuccess: async () => {
      await invalidateAfterOrganisationMutation(
        queryClient,
        data.competitionId,
      )
      setConfirmOpen(false)
      onClose()
    },
  })

  const setNumber =
    (key: keyof ReplaceRegulationRequest) => (value: number | null) => {
      if (value == null || !Number.isFinite(value)) {
        return
      }
      setForm((current) => ({ ...current, [key]: value }))
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

  function moveCriterion(index: number, direction: -1 | 1) {
    setForm((current) => {
      const list = [...(current.rankingCriteria ?? [])]
      const target = index + direction
      if (target < 0 || target >= list.length) {
        return current
      }
      ;[list[index], list[target]] = [list[target], list[index]]
      return { ...current, rankingCriteria: list }
    })
  }

  function toggleCriterion(criterion: RankingCriterion) {
    setForm((current) => {
      const list = [...(current.rankingCriteria ?? [])]
      if (list.includes(criterion)) {
        if (list.length <= 1) {
          return current
        }
        return {
          ...current,
          rankingCriteria: list.filter((item) => item !== criterion),
        }
      }
      return { ...current, rankingCriteria: [...list, criterion] }
    })
  }

  function requestSave() {
    if (mutation.isPending) {
      return
    }
    if (needsReadyConfirm) {
      setConfirmOpen(true)
      return
    }
    mutation.mutate()
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={onClose}
        title={t('editor.title')}
        closeLabel={tCommon('close')}
        closeDisabled={mutation.isPending || confirmOpen}
        trapFocus={!confirmOpen}
        size="lg"
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
                <PendingLabel>{t('editor.saving')}</PendingLabel>
              ) : (
                t('editor.save')
              )}
            </button>
          </>
        }
      >
        <form
          id={formId}
          className="form form--wide regulation-editor"
          onSubmit={(event: FormEvent) => {
            event.preventDefault()
            requestSave()
          }}
        >
          {eligibleStages.length > 0 ? (
            <Alert tone="info" role="status">
              {t('editor.syncBanner')}
            </Alert>
          ) : null}

          <section
            ref={entriesRef}
            className="regulation-editor__section"
            aria-labelledby={`${formId}-entries`}
          >
            <h3 id={`${formId}-entries`} className="regulation-editor__heading">
              {t('families.entries')}
            </h3>
            <div className="form-row">
              <Field label={t('editor.minimumTeams')} required>
                <InputNumber
                  value={form.minimumTeams}
                  min={1}
                  onChange={setNumber('minimumTeams')}
                  required
                />
              </Field>
              <Field label={t('editor.maximumTeams')} required>
                <InputNumber
                  value={form.maximumTeams}
                  min={1}
                  onChange={setNumber('maximumTeams')}
                  required
                />
              </Field>
            </div>
          </section>

          <section
            ref={matchRef}
            className="regulation-editor__section"
            aria-labelledby={`${formId}-match`}
          >
            <h3 id={`${formId}-match`} className="regulation-editor__heading">
              {t('families.match')}
            </h3>
            <p className="regulation-editor__subheading">
              {t('editor.regulationTime')}
            </p>
            <div className="form-row">
              <Field label={t('editor.numberOfPeriods')} required>
                <InputNumber
                  value={form.numberOfPeriods}
                  min={1}
                  onChange={setNumber('numberOfPeriods')}
                  required
                />
              </Field>
              <Field label={t('editor.durationPerPeriod')} required>
                <InputNumber
                  value={form.durationPerPeriod}
                  min={1}
                  onChange={setNumber('durationPerPeriod')}
                  required
                />
              </Field>
              <Field label={t('editor.halfTimeDuration')} required>
                <InputNumber
                  value={form.halfTimeDuration}
                  min={0}
                  onChange={setNumber('halfTimeDuration')}
                  required
                />
              </Field>
            </div>

            <p className="regulation-editor__subheading">
              {t('editor.extraTime')}
            </p>
            <label className="field field--checkbox">
              <input
                type="checkbox"
                checked={form.hasExtraTime === true}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    hasExtraTime: event.target.checked,
                  }))
                }
              />
              {t('editor.enableExtraTime')}
            </label>
            {form.hasExtraTime ? (
              <div className="form-row">
                <Field label={t('editor.extraTimePeriods')} required>
                  <InputNumber
                    value={form.extraTimeNumberOfPeriods ?? 2}
                    min={1}
                    onChange={setNumber('extraTimeNumberOfPeriods')}
                  />
                </Field>
                <Field label={t('editor.extraTimeDuration')} required>
                  <InputNumber
                    value={form.extraTimeDurationPerPeriod ?? 15}
                    min={1}
                    onChange={setNumber('extraTimeDurationPerPeriod')}
                  />
                </Field>
              </div>
            ) : null}

            <p className="regulation-editor__subheading">{t('editor.shootout')}</p>
            <label className="field field--checkbox">
              <input
                type="checkbox"
                checked={form.hasPenaltyShootout === true}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    hasPenaltyShootout: event.target.checked,
                  }))
                }
              />
              {t('editor.enableShootout')}
            </label>
            {form.hasPenaltyShootout ? (
              <Field label={t('editor.shootoutKicks')} required>
                <InputNumber
                  value={form.penaltyInitialKicksPerTeam ?? 5}
                  min={1}
                  onChange={setNumber('penaltyInitialKicksPerTeam')}
                />
              </Field>
            ) : null}

            <p className="regulation-editor__subheading">{t('forfeit.heading')}</p>
            <div className="form-row">
              <Field label={t('editor.forfeitWinner')} required>
                <InputNumber
                  value={form.forfeitWinnerGoals ?? 3}
                  min={0}
                  onChange={setNumber('forfeitWinnerGoals')}
                />
              </Field>
              <Field label={t('editor.forfeitLoser')} required>
                <InputNumber
                  value={form.forfeitLoserGoals ?? 0}
                  min={0}
                  onChange={setNumber('forfeitLoserGoals')}
                />
              </Field>
            </div>
          </section>

          <section
            ref={standingRef}
            className="regulation-editor__section"
            aria-labelledby={`${formId}-standing`}
          >
            <h3
              id={`${formId}-standing`}
              className="regulation-editor__heading"
            >
              {t('families.standing')}
            </h3>
            <div className="form-row">
              <Field label={t('editor.winPoints')} required>
                <InputNumber
                  value={form.winPoints}
                  onChange={setNumber('winPoints')}
                  required
                />
              </Field>
              <Field label={t('editor.drawPoints')} required>
                <InputNumber
                  value={form.drawPoints}
                  onChange={setNumber('drawPoints')}
                  required
                />
              </Field>
              <Field label={t('editor.lossPoints')} required>
                <InputNumber
                  value={form.lossPoints}
                  onChange={setNumber('lossPoints')}
                  required
                />
              </Field>
            </div>
            <p className="regulation-editor__subheading">
              {t('criteria.heading')}
            </p>
            <ol className="regulation-editor__criteria">
              {(form.rankingCriteria ?? []).map((criterion, index) => (
                <li key={criterion}>
                  <span>{t(`criteria.${criterion}`)}</span>
                  <span className="regulation-editor__criteria-actions">
                    <button
                      type="button"
                      className="ds-btn ds-btn--ghost"
                      disabled={index === 0}
                      onClick={() => moveCriterion(index, -1)}
                    >
                      ↑
                    </button>
                    <button
                      type="button"
                      className="ds-btn ds-btn--ghost"
                      disabled={
                        index === (form.rankingCriteria?.length ?? 0) - 1
                      }
                      onClick={() => moveCriterion(index, 1)}
                    >
                      ↓
                    </button>
                  </span>
                </li>
              ))}
            </ol>
            <div className="form-row">
              {ALL_CRITERIA.map((criterion) => (
                <label key={criterion} className="field field--checkbox">
                  <input
                    type="checkbox"
                    checked={(form.rankingCriteria ?? []).includes(criterion)}
                    onChange={() => toggleCriterion(criterion)}
                  />
                  {t(`criteria.${criterion}`)}
                </label>
              ))}
            </div>
          </section>

          <section
            ref={disciplineRef}
            className="regulation-editor__section"
            aria-labelledby={`${formId}-discipline`}
          >
            <h3
              id={`${formId}-discipline`}
              className="regulation-editor__heading"
            >
              {t('families.discipline')}
            </h3>
            <p className="regulation-editor__subheading">{t('editor.disciplineHint')}</p>
            <div className="form-row">
              {(['Yellow', 'Red', 'White'] as const).map((type) => (
                <label key={type} className="field field--checkbox">
                  <input
                    type="checkbox"
                    checked={(form.allowedTypes ?? []).includes(type)}
                    onChange={() => toggleAllowedType(type)}
                  />
                  {t(`discipline.${type}`)}
                </label>
              ))}
            </div>
          </section>

          {mutation.isError && <MutationError error={mutation.error} />}
        </form>
      </Dialog>

      <ConfirmDialog
        open={confirmOpen}
        title={t('editor.readyConfirmTitle')}
        message={t('editor.readyConfirmMessage')}
        confirmLabel={t('editor.readyConfirmAction')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        confirmPending={mutation.isPending}
        confirmPendingLabel={t('editor.saving')}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={() => mutation.mutate()}
      />
    </>
  )
}
