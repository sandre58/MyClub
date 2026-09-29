import type { MatchGenerationFormat, StructureFormatKind } from '../types';
import {
  matchGenerationFormatLabel,
  structureFormatKindLabel,
} from '../i18n/enumLabels';

export type SkeletonFormState = {
  format: StructureFormatKind;
  matchdayCount: number;
  groupCount: number;
  participantsPerGroup: number;
  bracketSize: number;
  swissRoundCount: number;
  matchGenerationFormat: MatchGenerationFormat;
};

export function defaultSkeletonForm(
  format: StructureFormatKind = 'Championship',
): SkeletonFormState {
  return {
    format,
    matchdayCount: 1,
    groupCount: 2,
    participantsPerGroup: 2,
    bracketSize: 4,
    swissRoundCount: 3,
    matchGenerationFormat: 'SingleRoundRobin',
  };
}

export function skeletonPayload(state: SkeletonFormState) {
  return {
    matchdayCount:
      state.format === 'Championship' ? state.matchdayCount : null,
    groupCount: state.format === 'Groups' ? state.groupCount : null,
    participantsPerGroup:
      state.format === 'Groups' ? state.participantsPerGroup : null,
    bracketSize: state.format === 'Cup' ? state.bracketSize : null,
    swissRoundCount: state.format === 'Swiss' ? state.swissRoundCount : null,
    matchGenerationFormat:
      state.format === 'Championship' || state.format === 'Groups'
        ? state.matchGenerationFormat
        : null,
  };
}

export function isPowerOfTwo(value: number): boolean {
  return value > 0 && (value & (value - 1)) === 0;
}

export function skeletonStepValid(state: SkeletonFormState): boolean {
  switch (state.format) {
    case 'Championship':
      return state.matchdayCount >= 1;
    case 'Groups':
      return state.groupCount >= 2 && state.participantsPerGroup >= 2;
    case 'Cup':
      return (
        state.bracketSize >= 2 &&
        state.bracketSize <= 64 &&
        isPowerOfTwo(state.bracketSize)
      );
    case 'Swiss':
      return state.swissRoundCount >= 1;
    default:
      return false;
  }
}

type Translate = (key: string) => string;

/** Skeleton fields for AddPhase / EditSkeleton / ConfigureStructure. */
export function SkeletonFields({
  state,
  onChange,
  t,
  formatLocked,
  showFormatImmutableHint = true,
  omitFormatField = false,
}: {
  state: SkeletonFormState;
  onChange: (next: SkeletonFormState) => void;
  t: Translate;
  formatLocked?: boolean;
  /** When format is locked, show the immutability caption (EditSkeleton). */
  showFormatImmutableHint?: boolean;
  /** Skip format select/locked line — AddPhase shows type via ChoiceTiles. */
  omitFormatField?: boolean;
}) {
  const set = <K extends keyof SkeletonFormState>(
    key: K,
    value: SkeletonFormState[K],
  ) => onChange({ ...state, [key]: value });

  return (
    <>
      {!omitFormatField ? (
        !formatLocked ? (
          <label className="ds-field">
            <span className="ds-field__label">{t('structure.format')}</span>
            <select
              className="ds-input"
              value={state.format}
              onChange={(event) =>
                set('format', event.target.value as StructureFormatKind)
              }
            >
              <option value="Championship">
                {structureFormatKindLabel('Championship')}
              </option>
              <option value="Groups">{structureFormatKindLabel('Groups')}</option>
              <option value="Cup">{structureFormatKindLabel('Cup')}</option>
              <option value="Swiss">{structureFormatKindLabel('Swiss')}</option>
            </select>
          </label>
        ) : (
          <p className="structure-skeleton__format-locked">
            <span className="ds-field__label">{t('structure.format')}</span>
            <strong>{structureFormatKindLabel(state.format)}</strong>
            {showFormatImmutableHint ? (
              <span className="caption">{t('skeleton.formatImmutable')}</span>
            ) : null}
          </p>
        )
      ) : null}

      {(state.format === 'Championship' || state.format === 'Groups') && (
        <label className="ds-field">
          <span className="ds-field__label">
            {t('structure.matchGenerationFormat')}
          </span>
          <select
            className="ds-input"
            value={state.matchGenerationFormat}
            onChange={(event) =>
              set(
                'matchGenerationFormat',
                event.target.value as MatchGenerationFormat,
              )
            }
          >
            <option value="SingleRoundRobin">
              {matchGenerationFormatLabel('SingleRoundRobin')}
            </option>
            <option value="DoubleRoundRobin">
              {matchGenerationFormatLabel('DoubleRoundRobin')}
            </option>
          </select>
          <span className="caption">{t('structure.matchGenerationHint')}</span>
        </label>
      )}

      {state.format === 'Championship' && (
        <label className="ds-field">
          <span className="ds-field__label">{t('structure.matchdayCount')}</span>
          <input
            className="ds-input"
            type="number"
            min={1}
            value={state.matchdayCount}
            onChange={(event) =>
              set('matchdayCount', Number(event.target.value) || 1)
            }
            required
          />
        </label>
      )}

      {state.format === 'Groups' && (
        <div className="form-row">
          <label className="ds-field">
            <span className="ds-field__label">{t('structure.groupCount')}</span>
            <input
              className="ds-input"
              type="number"
              min={2}
              value={state.groupCount}
              onChange={(event) =>
                set('groupCount', Number(event.target.value) || 2)
              }
              required
            />
          </label>
          <label className="ds-field">
            <span className="ds-field__label">
              {t('structure.participantsPerGroup')}
            </span>
            <input
              className="ds-input"
              type="number"
              min={2}
              value={state.participantsPerGroup}
              onChange={(event) =>
                set('participantsPerGroup', Number(event.target.value) || 2)
              }
              required
            />
          </label>
        </div>
      )}

      {state.format === 'Cup' && (
        <label className="ds-field">
          <span className="ds-field__label">{t('structure.bracketSize')}</span>
          <input
            className="ds-input"
            type="number"
            min={2}
            max={64}
            step={1}
            value={state.bracketSize}
            onChange={(event) =>
              set('bracketSize', Number(event.target.value) || 2)
            }
            required
          />
          <span className="caption">{t('structure.bracketHint')}</span>
        </label>
      )}

      {state.format === 'Swiss' && (
        <label className="ds-field">
          <span className="ds-field__label">{t('structure.swissRoundCount')}</span>
          <input
            className="ds-input"
            type="number"
            min={1}
            value={state.swissRoundCount}
            onChange={(event) =>
              set('swissRoundCount', Number(event.target.value) || 1)
            }
            required
          />
          <span className="caption">{t('structure.swissRoundHint')}</span>
        </label>
      )}
    </>
  );
}
