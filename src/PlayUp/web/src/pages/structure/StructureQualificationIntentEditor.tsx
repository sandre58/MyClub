// -----------------------------------------------------------------------
// Qualification intent editor — scope / positions / destination place map.
// Extracted from StructureQualificationDialog (C7, constant behavior).
// -----------------------------------------------------------------------

import { useQuery } from '@tanstack/react-query';
import { useId, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { fetchStageSchematic } from '../../api';
import { ChoiceTile } from '../../design-system/components/ChoiceTile';
import { Field } from '../../design-system/components/Field';
import { InputNumber } from '../../design-system/components/InputNumber';
import { Select } from '../../design-system/components/Select';
import { Tooltip } from '../../design-system/components/Tooltip';
import { ToastToneIcon } from '../../design-system/icons/toastIcons';
import {
  ChampionshipFormatIcon,
  CupFormatIcon,
  GroupsFormatIcon,
  ListMinusIcon,
  ListPlusIcon,
  StandingRulesIcon,
  StructureIcon,
  SwissFormatIcon,
} from '../../design-system/icons/contentIcons';
import { queryKeys } from '../../queryKeys';
import type {
  QualificationIntentSourceKind,
  StructureFormatKind,
  StructureStageHubSummary,
  StructureView,
} from '../../types';
import { DestinationDraftMeter } from './DestinationDraftMeter';
import { SortiesWhoWhereFlow } from './SortiesWhoWhereFlow';
import {
  areProgressionPlacesLabeled,
  listLabeledPlaces,
  placeGrainForFormat,
} from './structurePlaceLabel';
import { expectedPopulationWithQualDraft } from './structurePopulationVolume';
import {
  expandOccurrences,
  fillEmptyPlaceKeysAllowingReuse,
  fillEmptyPlaceSlotKeys,
  occurrenceLabel,
  ordinalRank,
  parsePositiveInt,
  resizeDestinationSlotKeys,
  applyDestinationToDraft,
  applyTargetKindToDraft,
  isFormOnlyDestination,
  showsPopulationPlaceChoice,
  type QualIntentDraft,
  type QualTargetKind,
} from './structureQualificationDraft';

function scopeKindIcon(
  kind: QualificationIntentSourceKind,
  size: 'sm' | 'md' | 'lg' = 'sm',
) {
  switch (kind) {
    case 'AcrossGroups':
      return <SwissFormatIcon size={size} />;
    case 'Overall':
      return <StandingRulesIcon size={size} />;
    case 'EachGroup':
    case 'SingleGroup':
    default:
      return <GroupsFormatIcon size={size} />;
  }
}

function stageFormatIcon(kind?: StructureFormatKind | null) {
  switch (kind) {
    case 'Cup':
      return <CupFormatIcon size="sm" aria-hidden="true" />;
    case 'Championship':
      return <ChampionshipFormatIcon size="sm" aria-hidden="true" />;
    case 'Groups':
      return <GroupsFormatIcon size="sm" aria-hidden="true" />;
    case 'Swiss':
      return <SwissFormatIcon size="sm" aria-hidden="true" />;
    default:
      return <StructureIcon size="sm" aria-hidden="true" />;
  }
}

function PositionFields({
  draft,
  fromId,
  toId,
  onChange,
  singleLabel,
  rangeLabel,
  message,
}: {
  draft: QualIntentDraft;
  fromId: string;
  toId: string;
  onChange: (next: QualIntentDraft) => void;
  singleLabel?: string;
  rangeLabel?: string;
  message?: string;
}) {
  const { t } = useTranslation('structure');
  const isRange = draft.positionFrom !== draft.positionTo;

  return (
    <div className="structure-qualification__positions">
      <div className="structure-qualification__range">
        <Field
          label={
            isRange
              ? (rangeLabel ?? t('qualification.positions'))
              : (singleLabel ?? t('qualification.position'))
          }
          htmlFor={fromId}
          width="sm"
        >
          <InputNumber
            id={fromId}
            min={1}
            value={parsePositiveInt(draft.positionFrom)}
            controlsLayout="split"
            onChange={(value) => {
              const next = value != null ? String(value) : '';
              onChange({
                ...draft,
                positionFrom: next,
                ...(isRange ? {} : { positionTo: next }),
              });
            }}
          />
        </Field>
        {isRange ? (
          <>
            <span className="structure-qualification__range-sep" aria-hidden>
              {t('qualification.rangeTo')}
            </span>
            <Field label={'\u00a0'} htmlFor={toId} width="sm">
              <InputNumber
                id={toId}
                min={1}
                value={parsePositiveInt(draft.positionTo)}
                controlsLayout="split"
                onChange={(value) =>
                  onChange({
                    ...draft,
                    positionTo: value != null ? String(value) : '',
                  })
                }
              />
            </Field>
            <Tooltip content={t('qualification.removePositionRange')}>
              <button
                type="button"
                className="ds-btn ds-btn--ghost ds-icon-button structure-qualification__range-toggle"
                aria-label={t('qualification.removePositionRange')}
                aria-pressed="true"
                onClick={() =>
                  onChange({ ...draft, positionTo: draft.positionFrom })
                }
              >
                <ListMinusIcon size="sm" />
              </button>
            </Tooltip>
          </>
        ) : (
          <Tooltip content={t('qualification.addPositionRange')}>
            <button
              type="button"
              className="ds-btn ds-btn--ghost ds-icon-button structure-qualification__range-toggle"
              aria-label={t('qualification.addPositionRange')}
              aria-pressed="false"
              onClick={() => {
                const from = parsePositiveInt(draft.positionFrom) ?? 1;
                onChange({
                  ...draft,
                  positionTo: String(from + 1),
                });
              }}
            >
              <ListPlusIcon size="sm" />
            </button>
          </Tooltip>
        )}
      </div>
      {message ? (
        <p className="structure-qualification__field-hint" role="status">
          {message}
        </p>
      ) : null}
    </div>
  );
}

export function StructureQualificationIntentEditor({
  draft,
  data,
  sourceStageId,
  groups,
  hasGroups,
  peerStages,
  draftEntriesByDestination,
  locale,
  onChange,
}: {
  draft: QualIntentDraft;
  data: StructureView;
  sourceStageId: string;
  groups: { id: string; name: string }[];
  hasGroups: boolean;
  peerStages: StructureStageHubSummary[];
  draftEntriesByDestination: Map<string, number>;
  locale: string;
  onChange: (next: QualIntentDraft) => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const fromId = useId();
  const toId = useId();
  const acrossId = useId();
  const pointsId = useId();
  const placeFillHintId = useId();

  const destStageId = draft.destinationStageId.trim();
  const destPeer = peerStages.find((p) => p.stageId === destStageId);
  const destFormat = destPeer?.formatKind;
  const formOnly = isFormOnlyDestination(destFormat);
  const showKindChoice = showsPopulationPlaceChoice(destFormat);

  const destSchematicQuery = useQuery({
    queryKey: queryKeys.stages.schematic(destStageId),
    queryFn: () => fetchStageSchematic(destStageId),
    enabled: draft.targetKind === 'place' && !formOnly && !!destStageId,
  });
  const placesLabeled =
    formOnly || areProgressionPlacesLabeled(destSchematicQuery.data);
  const labeledPlaces = useMemo(
    () => listLabeledPlaces(destSchematicQuery.data, t),
    [destSchematicQuery.data, t],
  );
  /** UX Place grain from dest schematic (Groups / Cup / Form). */
  const placeGrain = formOnly
    ? 'form'
    : (labeledPlaces[0]?.grain ??
      placeGrainForFormat(destSchematicQuery.data?.formatKind) ??
      placeGrainForFormat(destFormat) ??
      'slot');

  function setDestination(peer: StructureStageHubSummary) {
    onChange(applyDestinationToDraft(draft, peer.stageId, peer.formatKind));
  }

  function setTargetKind(kind: QualTargetKind) {
    if (formOnly) return;
    onChange(applyTargetKindToDraft(draft, kind));
  }

  const placeUnavailable =
    showKindChoice &&
    draft.targetKind === 'place' &&
    !placesLabeled &&
    !destSchematicQuery.isLoading;
  const placeOccurrences = useMemo(
    () =>
      draft.targetKind === 'place' && !formOnly
        ? expandOccurrences(draft, groups)
        : [],
    [draft, groups, formOnly],
  );
  const placeSlotKeys = useMemo(() => {
    const source =
      placeGrain === 'group'
        ? draft.destinationGroupIds
        : draft.destinationSlotKeys;
    return resizeDestinationSlotKeys(source, placeOccurrences.length);
  }, [
    draft.destinationGroupIds,
    draft.destinationSlotKeys,
    placeGrain,
    placeOccurrences.length,
  ]);

  function applyPlaceKeys(next: string[]) {
    onChange({
      ...draft,
      targetKind: 'place',
      destinationForm: false,
      destinationSlotKeys: placeGrain === 'slot' ? next : [],
      destinationGroupIds: placeGrain === 'group' ? next : [],
    });
  }

  const scopeOptions: {
    value: QualificationIntentSourceKind;
    labelKey: string;
    descriptionKey: string;
  }[] = hasGroups
    ? [
        {
          value: 'EachGroup',
          labelKey: 'scopeGroupTile',
          descriptionKey: 'scopeGroupHint',
        },
        {
          value: 'AcrossGroups',
          labelKey: 'scopeAcrossTile',
          descriptionKey: 'scopeAcrossHint',
        },
        ...(draft.sourceKind === 'Overall'
          ? [
              {
                value: 'Overall' as const,
                labelKey: 'scopeOverallTile',
                descriptionKey: 'scopeOverallHint',
              },
            ]
          : []),
      ]
    : [
        {
          value: 'Overall',
          labelKey: 'scopeOverallTile',
          descriptionKey: 'scopeOverallHint',
        },
      ];

  const groupTileSelected =
    draft.sourceKind === 'EachGroup' || draft.sourceKind === 'SingleGroup';

  return (
    <div className="structure-qualification__editor">
      <SortiesWhoWhereFlow
        sourceLabel={t('qualification.who')}
        destinationLabel={t('qualification.where')}
        feedsLabel={t('qualification.feeds')}
        source={
          <>
            <div
              className="structure-qualification__scope-tiles"
              data-count={String(scopeOptions.length)}
              role="radiogroup"
              aria-label={t('qualification.scope')}
            >
              {scopeOptions.map(({ value, labelKey, descriptionKey }) => {
                const selected =
                  value === 'EachGroup'
                    ? groupTileSelected
                    : draft.sourceKind === value;
                return (
                  <ChoiceTile
                    key={value}
                    label={t(`qualification.${labelKey}`)}
                    description={t(`qualification.${descriptionKey}`)}
                    leading={scopeKindIcon(value)}
                    selected={selected}
                    onChange={(sel) => {
                      if (!sel) return;
                      if (value === 'EachGroup') {
                        onChange({
                          ...draft,
                          sourceKind: 'EachGroup',
                          groupId: '',
                          groupName: '',
                        });
                      } else {
                        onChange({
                          ...draft,
                          sourceKind: value,
                          groupId: '',
                          groupName: '',
                        });
                      }
                    }}
                  />
                );
              })}
            </div>

            {draft.sourceKind === 'AcrossGroups' ? (
              <div className="structure-qualification__fields">
                <Field
                  label={t('qualification.acrossPlace')}
                  message={t('qualification.acrossPlaceHint', {
                    place: ordinalRank(
                      Math.max(
                        parsePositiveInt(draft.acrossGroupsPosition) ?? 1,
                        1,
                      ),
                      locale,
                    ),
                  })}
                  htmlFor={acrossId}
                  className="structure-qualification__control-sm"
                >
                  <InputNumber
                    id={acrossId}
                    min={1}
                    value={parsePositiveInt(draft.acrossGroupsPosition)}
                    controlsLayout="split"
                    onChange={(value) =>
                      onChange({
                        ...draft,
                        acrossGroupsPosition:
                          value != null ? String(value) : '',
                      })
                    }
                  />
                </Field>
                <PositionFields
                  draft={draft}
                  fromId={fromId}
                  toId={toId}
                  onChange={onChange}
                  singleLabel={t('qualification.position')}
                  rangeLabel={t('qualification.positions')}
                  message={
                    draft.positionFrom === draft.positionTo
                      ? t('qualification.acrossAmongHintOne', {
                          rank: ordinalRank(
                            Math.max(
                              parsePositiveInt(draft.positionFrom) ?? 1,
                              1,
                            ),
                            locale,
                          ),
                        })
                      : t('qualification.acrossAmongHintRange', {
                          from: ordinalRank(
                            Math.max(
                              parsePositiveInt(draft.positionFrom) ?? 1,
                              1,
                            ),
                            locale,
                          ),
                          to: ordinalRank(
                            Math.max(
                              parsePositiveInt(draft.positionTo) ?? 1,
                              1,
                            ),
                            locale,
                          ),
                        })
                  }
                />
              </div>
            ) : groupTileSelected && hasGroups ? (
              <div className="structure-qualification__fields">
                <Field label={t('qualification.group')}>
                  <Select
                    options={[
                      {
                        value: '',
                        label: t('qualification.eachGroup'),
                      },
                      ...groups.map((g) => ({ value: g.id, label: g.name })),
                    ]}
                    value={
                      draft.sourceKind === 'SingleGroup'
                        ? draft.groupId || null
                        : ''
                    }
                    placeholder={t('qualification.eachGroup')}
                    onChange={(value) => {
                      const id = value ?? '';
                      if (!id) {
                        onChange({
                          ...draft,
                          sourceKind: 'EachGroup',
                          groupId: '',
                          groupName: '',
                        });
                        return;
                      }
                      onChange({
                        ...draft,
                        sourceKind: 'SingleGroup',
                        groupId: id,
                        groupName: groups.find((g) => g.id === id)?.name ?? '',
                      });
                    }}
                  />
                </Field>
                <PositionFields
                  draft={draft}
                  fromId={fromId}
                  toId={toId}
                  onChange={onChange}
                />
              </div>
            ) : (
              <PositionFields
                draft={draft}
                fromId={fromId}
                toId={toId}
                onChange={onChange}
              />
            )}

            <div className="structure-qualification__fields">
              <Field label={t('qualification.condition')}>
                <Select
                  options={[
                    { value: 'none', label: t('qualification.conditionNone') },
                    {
                      value: 'points',
                      label: t('qualification.conditionPoints'),
                    },
                  ]}
                  value={draft.conditionKind}
                  onChange={(value) =>
                    onChange({
                      ...draft,
                      conditionKind: (value as 'none' | 'points') ?? 'none',
                      minimumPoints:
                        value === 'points' ? draft.minimumPoints || '0' : '',
                    })
                  }
                />
              </Field>
              {draft.conditionKind === 'points' ? (
                <Field
                  label={t('qualification.minimumPoints')}
                  htmlFor={pointsId}
                  width="sm"
                >
                  <InputNumber
                    id={pointsId}
                    min={0}
                    value={
                      draft.minimumPoints.trim() === ''
                        ? null
                        : Number(draft.minimumPoints)
                    }
                    controlsLayout="split"
                    onChange={(value) =>
                      onChange({
                        ...draft,
                        minimumPoints: value != null ? String(value) : '',
                      })
                    }
                  />
                </Field>
              ) : null}
            </div>
          </>
        }
        destination={
          <>
            {peerStages.length === 0 ? (
              <p className="structure-qualification__field-hint" role="status">
                {t('qualification.emptyNoPeerBody')}
              </p>
            ) : (
              <div
                className="structure-qualification__scope-tiles"
                data-count={String(Math.min(peerStages.length, 3))}
                role="radiogroup"
                aria-label={t('qualification.destinationPhase')}
              >
                {peerStages.map((peer) => {
                  const draftTotal =
                    draftEntriesByDestination.get(peer.stageId) ?? 0;
                  const draftThisIntent =
                    draft.destinationStageId.trim() === peer.stageId
                      ? expandOccurrences(draft, groups).length
                      : 0;
                  const capacity = peer.compositionCapacity;
                  const expected =
                    capacity != null && capacity > 0
                      ? expectedPopulationWithQualDraft({
                          data,
                          destination: peer,
                          sourceStageId,
                          draftQualVolume: draftTotal,
                        })
                      : null;
                  const description =
                    expected != null && capacity != null && capacity > 0 ? (
                      <DestinationDraftMeter
                        count={expected}
                        capacity={capacity}
                        draft={draftThisIntent}
                        label={t('qualification.tileContribution', {
                          count: expected,
                          capacity,
                        })}
                        draftLabel={t('qualification.tileContributionDraft', {
                          count: draftThisIntent,
                        })}
                        ariaLabel={t('qualification.tileContributionAria', {
                          phase: peer.name,
                          count: expected,
                          capacity,
                          draft: draftThisIntent,
                        })}
                      />
                    ) : draftThisIntent > 0 ? (
                      t('qualification.tileContributionEntries', {
                        count: draftThisIntent,
                      })
                    ) : undefined;
                  return (
                    <ChoiceTile
                      key={peer.stageId}
                      label={peer.name}
                      description={description}
                      leading={stageFormatIcon(peer.formatKind)}
                      selected={draft.destinationStageId === peer.stageId}
                      onChange={(selected) => {
                        if (!selected) return;
                        setDestination(peer);
                      }}
                    />
                  );
                })}
              </div>
            )}

            {showKindChoice && destStageId ? (
              <div
                className="structure-qualification__scope-tiles"
                data-count="2"
                role="radiogroup"
                aria-label={t('qualification.destinationKind')}
              >
                <ChoiceTile
                  label={t('qualification.kindPopulation')}
                  description={t('qualification.kindPopulationHint')}
                  leading={<StructureIcon size="sm" />}
                  selected={draft.targetKind === 'population'}
                  onChange={(selected) => {
                    if (!selected) return;
                    setTargetKind('population');
                  }}
                />
                <ChoiceTile
                  label={t('qualification.kindPlace')}
                  description={t('qualification.kindPlaceHint')}
                  leading={<CupFormatIcon size="sm" />}
                  selected={draft.targetKind === 'place'}
                  onChange={(selected) => {
                    if (!selected) return;
                    setTargetKind('place');
                  }}
                />
              </div>
            ) : null}

            {draft.targetKind === 'place' &&
            !formOnly &&
            destStageId &&
            placeGrain !== 'form' ? (
              destSchematicQuery.isLoading ? (
                <ul
                  className="structure-qualification__place-map structure-qualification__place-map--skeleton"
                  aria-busy="true"
                  aria-label={tCommon('loading')}
                >
                  {Array.from({
                    length: Math.max(placeOccurrences.length, 3),
                  }).map((_, index) => (
                    <li key={`skel-${index}`} aria-hidden="true">
                      <span className="structure-qualification__place-map-skel-label" />
                      <span className="structure-qualification__place-map-skel-control" />
                    </li>
                  ))}
                </ul>
              ) : placeUnavailable || !placesLabeled ? (
                <p
                  className="structure-qualification__field-hint"
                  role="status"
                >
                  {t('qualification.emptyNoPlacePeerBody')}
                </p>
              ) : labeledPlaces.length === 0 ? (
                <p
                  className="structure-qualification__field-hint"
                  role="status"
                >
                  {t('qualification.placeEmpty')}
                </p>
              ) : placeOccurrences.length === 0 ? (
                <p
                  className="structure-qualification__field-hint"
                  role="status"
                >
                  {t('qualification.placeMapEmptySelection')}
                </p>
              ) : (
                <div className="structure-qualification__place-map-block">
                  <div className="structure-qualification__place-map-toolbar">
                    {peerStages.length > 0 ? (
                      <p className="structure-qualification__place-map-heading">
                        {t('qualification.placeMapHeading')}
                      </p>
                    ) : null}
                    <span className="structure-qualification__place-fill">
                      <span id={placeFillHintId} className="ds-visually-hidden">
                        {t('qualification.placeFillHint')}
                      </span>
                      <Tooltip content={t('qualification.placeFillHint')}>
                        <button
                          type="button"
                          className="ds-btn ds-btn--ghost"
                          aria-describedby={placeFillHintId}
                          disabled={(() => {
                            const emptyCount = placeSlotKeys.filter(
                              (k) => !k.trim(),
                            ).length;
                            if (emptyCount === 0) return true;
                            if (labeledPlaces.length === 0) return true;
                            if (placeGrain === 'group') return false;
                            const used = new Set(
                              placeSlotKeys
                                .map((k) => k.trim())
                                .filter((k) => k.length > 0),
                            );
                            return !labeledPlaces.some(
                              (place) => !used.has(place.apiIdentity),
                            );
                          })()}
                          onClick={() => {
                            const ids = labeledPlaces.map(
                              (place) => place.apiIdentity,
                            );
                            const next =
                              placeGrain === 'group'
                                ? fillEmptyPlaceKeysAllowingReuse(
                                    placeSlotKeys,
                                    ids,
                                  )
                                : fillEmptyPlaceSlotKeys(placeSlotKeys, ids);
                            applyPlaceKeys(next);
                          }}
                        >
                          <ListPlusIcon size="sm" />
                          {t('qualification.placeFillEmpties')}
                        </button>
                      </Tooltip>
                    </span>
                  </div>
                  <ul
                    className="structure-qualification__place-map"
                    aria-label={t('qualification.placeMapAria')}
                  >
                    {placeOccurrences.map((occ, index) => {
                      const selected = placeSlotKeys[index]?.trim() || null;
                      const rowId = `qual-place-${draft.id}-${index}`;
                      const rowInvalid = !selected;
                      return (
                        <li
                          key={`${occ.scope}:${occ.groupId ?? ''}:${occ.position}:${occ.acrossGroupsPosition ?? ''}:${index}`}
                          data-invalid={rowInvalid ? 'true' : 'false'}
                        >
                          <label
                            className="structure-qualification__place-map-label"
                            htmlFor={rowId}
                          >
                            {occurrenceLabel(
                              occ,
                              (n) => ordinalRank(n, locale),
                              t,
                            )}
                          </label>
                          <div className="structure-qualification__place-map-control">
                            <Select
                              id={rowId}
                              options={labeledPlaces.map((place) => ({
                                value: place.apiIdentity,
                                label: place.label,
                                disabled:
                                  placeGrain === 'slot' &&
                                  placeSlotKeys.some(
                                    (key, j) =>
                                      j !== index &&
                                      key.trim() === place.apiIdentity,
                                  ),
                              }))}
                              value={selected}
                              invalid={rowInvalid}
                              placeholder={t(
                                'qualification.placeSlotPlaceholder',
                              )}
                              allowClear
                              aria-label={t('qualification.placeMapRowAria', {
                                source: occurrenceLabel(
                                  occ,
                                  (n) => ordinalRank(n, locale),
                                  t,
                                ),
                              })}
                              onChange={(value) => {
                                const next = placeSlotKeys.slice();
                                next[index] = value?.trim() ?? '';
                                applyPlaceKeys(next);
                              }}
                            />
                            {rowInvalid ? (
                              <span
                                className="structure-qualification__place-map-error"
                                aria-hidden="true"
                              >
                                <ToastToneIcon tone="attention" size="sm" />
                              </span>
                            ) : null}
                          </div>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              )
            ) : null}
          </>
        }
      />
    </div>
  );
}
