import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import { ChoiceTile } from '../design-system/components/ChoiceTile';
import { Field } from '../design-system/components/Field';
import { InputNumber } from '../design-system/components/InputNumber';
import { ToggleButtonGroup } from '../design-system/components/ToggleButtonGroup';
import { matchGenerationFormatLabel } from '../i18n/enumLabels';
import type { MatchGenerationFormat } from '../types';
import type { SkeletonFormState } from './structureSkeletonForm';

/** Cup bracket sizes — Application bound [2, 64] powers of two. */
const CUP_BRACKET_SIZES = [2, 4, 8, 16, 32, 64] as const;

type Props = {
  state: SkeletonFormState;
  onChange: (next: SkeletonFormState) => void;
};

/**
 * Type-specific birth params for AddPhaseDialog (flat flow, DS controls only).
 * Championship MatchdayCount is not part of StructureIntent (skeleton seed is internal).
 */
export function AddPhaseFormatParams({ state, onChange }: Props) {
  const { t } = useTranslation('structure');
  const groupsId = useId();
  const placesId = useId();
  const swissId = useId();

  const set = <K extends keyof SkeletonFormState>(
    key: K,
    value: SkeletonFormState[K],
  ) => onChange({ ...state, [key]: value });

  const matchGeneration = (
    <Field label={t('structure.matchGenerationFormat')}>
      <ToggleButtonGroup
        aria-label={t('structure.matchGenerationFormat')}
        value={state.matchGenerationFormat}
        onChange={(value) =>
          set('matchGenerationFormat', value as MatchGenerationFormat)
        }
        options={[
          {
            value: 'SingleRoundRobin',
            label: matchGenerationFormatLabel('SingleRoundRobin'),
          },
          {
            value: 'DoubleRoundRobin',
            label: matchGenerationFormatLabel('DoubleRoundRobin'),
          },
        ]}
      />
    </Field>
  );

  switch (state.format) {
    case 'Championship':
      return matchGeneration;

    case 'Groups':
      return (
        <>
          <div className="ds-form--inline">
            <Field
              label={t('structure.groupCount')}
              htmlFor={groupsId}
              required
            >
              <InputNumber
                id={groupsId}
                min={2}
                value={state.groupCount}
                onChange={(value) => set('groupCount', value ?? 2)}
              />
            </Field>
            <Field
              label={t('structure.participantsPerGroup')}
              htmlFor={placesId}
              required
            >
              <InputNumber
                id={placesId}
                min={2}
                value={state.participantsPerGroup}
                onChange={(value) => set('participantsPerGroup', value ?? 2)}
              />
            </Field>
          </div>
          {matchGeneration}
        </>
      );

    case 'Cup':
      return (
        <Field label={t('structure.bracketSize')} required>
          <div
            className="structure-add-phase__bracket-tiles"
            role="radiogroup"
            aria-label={t('structure.bracketSize')}
          >
            {CUP_BRACKET_SIZES.map((size) => (
              <ChoiceTile
                key={size}
                label={String(size)}
                selected={state.bracketSize === size}
                onChange={(selected) => {
                  if (!selected) return;
                  set('bracketSize', size);
                }}
              />
            ))}
          </div>
        </Field>
      );

    case 'Swiss':
      return (
        <Field
          label={t('structure.swissRoundCount')}
          htmlFor={swissId}
          required
          message={t('structure.swissRoundHint')}
        >
          <InputNumber
            id={swissId}
            min={1}
            value={state.swissRoundCount}
            onChange={(value) => set('swissRoundCount', value ?? 1)}
          />
        </Field>
      );

    default:
      return null;
  }
}
