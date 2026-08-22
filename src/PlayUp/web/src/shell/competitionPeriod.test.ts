import { describe, expect, it } from 'vitest'
import { formatCompetitionPeriod } from './competitionPeriod'

describe('formatCompetitionPeriod', () => {
  it('returns null when no boundary is available', () => {
    expect(formatCompetitionPeriod(null, undefined)).toBeNull()
  })

  it('formats a start and end range', () => {
    const label = formatCompetitionPeriod('2026-03-01', '2026-06-15')
    expect(label).toMatch(/1/)
    expect(label).toMatch(/→/)
    expect(label).toMatch(/15/)
  })

  it('formats a single start date', () => {
    expect(formatCompetitionPeriod('2026-03-01', null)).toMatch(/1/)
  })
})
