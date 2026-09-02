/**
 * Formats competition period for shell chrome when Read exposes dates.
 * Returns null when neither boundary is available — UI omits the segment.
 */
export function formatCompetitionPeriod(
  start?: string | null,
  end?: string | null,
  locale = 'fr-FR',
): string | null {
  const parts = declaredSchedule(start, end, locale)
  if (!parts) {
    return null
  }

  if (parts.kind === 'both') {
    return `${parts.start} → ${parts.end}`
  }

  return parts.date
}

/**
 * Declared schedule (ScheduledStart / ScheduledEnd) for Accueil rows.
 * Distinct from Cockpit operational min/max kickoff.
 */
export type DeclaredSchedule =
  | { kind: 'both'; start: string; end: string }
  | { kind: 'start'; date: string }
  | { kind: 'end'; date: string }

export function declaredSchedule(
  start?: string | null,
  end?: string | null,
  locale = 'fr-FR',
): DeclaredSchedule | null {
  const startDate = parseDateOnly(start)
  const endDate = parseDateOnly(end)

  if (!startDate && !endDate) {
    return null
  }

  if (startDate && endDate) {
    return {
      kind: 'both',
      start: formatScheduleDate(startDate, locale),
      end: formatScheduleDate(endDate, locale),
    }
  }

  if (startDate) {
    return { kind: 'start', date: formatScheduleDate(startDate, locale) }
  }

  return { kind: 'end', date: formatScheduleDate(endDate!, locale) }
}

function formatScheduleDate(value: Date, locale: string): string {
  return new Intl.DateTimeFormat(locale, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(value)
}

function parseDateOnly(value?: string | null): Date | null {
  if (!value) {
    return null
  }

  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? null : parsed
}
