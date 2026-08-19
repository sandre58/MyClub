import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import i18n from '../i18n'
import { LoadingState } from '../ui'
import { renderWithI18n } from './renderWithI18n'

describe('i18n foundation', () => {
  it('uses French as the default locale', () => {
    expect(i18n.language).toBe('fr')
  })

  it('resolves common.loading in French', () => {
    expect(i18n.t('common:loading')).toBe('Chargement…')
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
