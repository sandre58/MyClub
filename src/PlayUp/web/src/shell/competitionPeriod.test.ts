import { describe, expect, it } from 'vitest'
import { declaredSchedule, formatCompetitionPeriod } from './competitionPeriod'

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

describe('declaredSchedule', () => {
  it('returns null when neither date is set', () => {
    expect(declaredSchedule(null, undefined)).toBeNull()
  })

  it('returns both bounds for a declared range', () => {
    expect(declaredSchedule('2026-09-12', '2027-03-28')).toEqual({
      kind: 'both',
      start: expect.stringMatching(/12/),
      end: expect.stringMatching(/28/),
    })
  })

  it('returns start-only and end-only kinds', () => {
    expect(declaredSchedule('2026-10-04', null)?.kind).toBe('start')
    expect(declaredSchedule(null, '2027-03-28')?.kind).toBe('end')
  })
})
