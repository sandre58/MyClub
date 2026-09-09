import { describe, expect, it } from 'vitest'
import { pointsBaremeWarning } from './pointsBaremeWarning'

const MESSAGE = 'Barème inhabituel : on attend Victoire ≥ Nul ≥ Défaite.'

describe('pointsBaremeWarning', () => {
  it('is silent for a standard barème', () => {
    expect(pointsBaremeWarning(3, 1, 0, MESSAGE)).toBeUndefined()
  })

  it('warns when win < draw', () => {
    expect(pointsBaremeWarning(1, 3, 0, MESSAGE)).toBe(MESSAGE)
  })

  it('warns when draw < loss', () => {
    expect(pointsBaremeWarning(3, 0, 1, MESSAGE)).toBe(MESSAGE)
  })
})
