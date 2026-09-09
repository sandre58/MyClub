import { describe, expect, it } from 'vitest'
import { pointsBaremeWarning } from './pointsBaremeWarning'

describe('pointsBaremeWarning', () => {
  it('is silent for a standard barème', () => {
    expect(pointsBaremeWarning(3, 1, 0)).toBeUndefined()
  })

  it('warns when win < draw', () => {
    expect(pointsBaremeWarning(1, 3, 0)).toMatch(/Victoire/)
  })

  it('warns when draw < loss', () => {
    expect(pointsBaremeWarning(3, 0, 1)).toMatch(/Victoire/)
  })
})
