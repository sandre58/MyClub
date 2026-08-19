import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ApiError } from '../api'
import {
  competitionStatusLabel,
  matchStatusLabel,
} from '../i18n/enumLabels'
import i18n from '../i18n'
import { ErrorState, LoadingState } from '../ui'
import { renderWithI18n } from './renderWithI18n'

describe('i18n foundation', () => {
  it('uses French as the default locale', () => {
    expect(i18n.language).toBe('fr')
    expect(document.documentElement.lang).toBe('fr')
  })

  it('resolves common.loading in French', () => {
    expect(i18n.t('common:loading')).toBe('Chargement…')
  })

  it('resolves common.skipToContent in French', () => {
    expect(i18n.t('common:skipToContent')).toBe('Passer au contenu')
  })

  it('renders LoadingState with common.loading by default', () => {
    renderWithI18n(<LoadingState />)
    expect(screen.getByRole('status')).toHaveTextContent('Chargement…')
  })
})

describe('shell attention trigger plurals', () => {
  it('uses trigger_zero when count is 0', () => {
    expect(i18n.t('shell:attention.trigger', { count: 0 })).toBe(
      'À traiter, aucun élément',
    )
  })

  it('uses trigger_one when count is 1', () => {
    expect(i18n.t('shell:attention.trigger', { count: 1 })).toBe(
      'À traiter, 1 élément',
    )
  })

  it('uses trigger_other when count is 2', () => {
    expect(i18n.t('shell:attention.trigger', { count: 2 })).toBe(
      'À traiter, 2 éléments',
    )
  })
})

describe('enum labels', () => {
  it('maps competition status wire codes to French labels', () => {
    expect(competitionStatusLabel('Draft')).toBe('Brouillon')
    expect(competitionStatusLabel('Running')).toBe('En cours')
  })

  it('maps match status wire codes to French labels', () => {
    expect(matchStatusLabel('Scheduled')).toBe('Planifié')
    expect(matchStatusLabel('Live')).toBe('En direct')
  })

  it('falls back to the wire code for unknown enum values', () => {
    expect(competitionStatusLabel('Unexpected' as 'Draft')).toBe('Unexpected')
  })
})

describe('ui ErrorState', () => {
  it('shows the localized not-found message for 404 ApiError', () => {
    renderWithI18n(<ErrorState error={new ApiError(404, 'missing')} />)
    expect(screen.getByRole('alert')).toHaveTextContent(
      "Introuvable. Vérifiez l'identifiant dans l'URL.",
    )
  })
})
