import i18n from './index'
import type {
  CompetitionStatus,
  CompletionMode,
  DrawResolutionKind,
  DrawResolutionState,
  DrawStatus,
  EntryStatus,
  MatchStatus,
  ResultType,
  StageStatus,
  StructureFormatKind,
} from '../types'

/** Wire enum member name → user-facing label (enums namespace). */
function enumLabel<T extends string>(group: string, code: T): string {
  return i18n.t(`${group}.${code}`, { ns: 'enums', defaultValue: code })
}

export function competitionStatusLabel(status: CompetitionStatus): string {
  return enumLabel('competitionStatus', status)
}

export function stageStatusLabel(status: StageStatus): string {
  return enumLabel('stageStatus', status)
}

export function matchStatusLabel(status: MatchStatus): string {
  return enumLabel('matchStatus', status)
}

export function entryStatusLabel(status: EntryStatus): string {
  return enumLabel('entryStatus', status)
}

export function drawStatusLabel(status: DrawStatus): string {
  return enumLabel('drawStatus', status)
}

export function drawResolutionStateLabel(state: DrawResolutionState): string {
  return enumLabel('drawResolutionState', state)
}

export function drawResolutionKindLabel(kind: DrawResolutionKind): string {
  return enumLabel('drawResolutionKind', kind)
}

export function structureFormatKindLabel(kind: StructureFormatKind): string {
  return enumLabel('structureFormatKind', kind)
}

export function resultTypeLabel(type: ResultType): string {
  return enumLabel('resultType', type)
}

export function completionModeLabel(mode: CompletionMode): string {
  return enumLabel('completionMode', mode)
}

export function attentionSourceLabel(source: string): string {
  return enumLabel('attentionSource', source)
}

export function attentionTargetTypeLabel(targetType: string): string {
  return enumLabel('attentionTargetType', targetType)
}
