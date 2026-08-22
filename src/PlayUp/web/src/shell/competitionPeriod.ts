/**
 * Formats competition period for shell chrome when Read exposes dates.
 * Returns null when neither boundary is available — UI omits the segment.
 */
export function formatCompetitionPeriod(
  start?: string | null,
  end?: string | null,
  locale = 'fr-FR',
): string | null {
  const startDate = parseDateOnly(start)
  const endDate = parseDateOnly(end)

  if (!startDate && !endDate) {
    return null
  }

  const formatter = new Intl.DateTimeFormat(locale, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })

  if (startDate && endDate) {
    return `${formatter.format(startDate)} → ${formatter.format(endDate)}`
  }

  if (startDate) {
    return formatter.format(startDate)
  }

  return formatter.format(endDate!)
}

function parseDateOnly(value?: string | null): Date | null {
  if (!value) {
    return null
  }

  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? null : parsed
}
