// -----------------------------------------------------------------------
// Progression intent editor — round × outcome → destination place map.
// Extracted from StructureProgressionDialog (C7, constant behavior).
// -----------------------------------------------------------------------

import { useQuery } from '@tanstack/react-query';
import { useId, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { fetchStageSchematic } from '../../api';
import { ChoiceTile } from '../../design-system/components/ChoiceTile';
import { Field } from '../../design-system/components/Field';
import { Select } from '../../design-system/components/Select';
import { Tooltip } from '../../design-system/components/Tooltip';
import { ToastToneIcon } from '../../design-system/icons/toastIcons';
import {
  ChampionshipFormatIcon,
  CupFormatIcon,
  EmptySelectionIcon,
  GroupsFormatIcon,
  ListPlusIcon,
  StructureIcon,
  SwissFormatIcon,
  TrophyIcon,
} from '../../design-system/icons/contentIcons';
import { queryKeys } from '../../queryKeys';
import type {
  ProgressionOutcome,
  StageBracketPair,
  StageFixture,
  StructureFormatKind,
  StructureStageHubSummary,
  StructureView,
} from '../../types';
import { EmptyState } from '../../ui';
import { DestinationDraftMeter } from './DestinationDraftMeter';
import { SortiesWhoWhereFlow } from './SortiesWhoWhereFlow';
import {
  areProgressionPlacesLabeled,
  listLabeledPlaces,
  placeGrainForFormat,
} from './structurePlaceLabel';
import { expectedPopulationWithProgDraft } from './structurePopulationVolume';
import {
  expandContribution,
  fillEmptyPlaceKeysAllowingReuse,
  fillEmptyPlaceSlotKeys,
  resizeDestinationSlotKeys,
  applyDestinationToDraft,
  applyTargetKindToDraft,
  isFormOnlyDestination,
  showsPopulationPlaceChoice,
  type ProgIntentDraft,
  type ProgTargetKind,
} from './structureProgressionDraft';

export type ProgRoundOption = {
  id: string;
  name: string;
  fixtures: StageFixture[];
};

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

function pairPlaceMapLabel(
  pairKey: string,
  slotAKey: string,
  slotBKey: string,
): string {
  return `${pairKey} · ${slotAKey} vs ${slotBKey}`;
}

function fixturePlaceMapLabel(
  fixture: StageFixture,
  index: number,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const base = t('progression.matchOrdinal', { n: index + 1 });
  const a = fixture.slotAKey?.trim();
  const b = fixture.slotBKey?.trim();
  if (!a && !b) return base;
  return `${base} · ${a || '—'} vs ${b || '—'}`;
}

export function StructureProgressionIntentEditor({
  draft,
  data,
  sourceStage,
  peerStages,
  rounds,
  playableRounds,
  championshipTerminal,
  bracketPairs,
  roundsLoading,
  draftEntriesByDestination,
  onChange,
}: {
  draft: ProgIntentDraft;
  data: StructureView;
  sourceStage: StructureStageHubSummary;
  peerStages: StructureStageHubSummary[];
  rounds: ProgRoundOption[];
  playableRounds: ProgRoundOption[];
  championshipTerminal: ProgRoundOption | null;
  bracketPairs: readonly StageBracketPair[];
  roundsLoading: boolean;
  draftEntriesByDestination: Map<string, number>;
  onChange: (next: ProgIntentDraft) => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
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
  /** UX Place grain from dest schematic (Groups A1 vs Cup slots). */
  const placeGrain = formOnly
    ? 'form'
    : (labeledPlaces[0]?.grain ??
      placeGrainForFormat(destSchematicQuery.data?.formatKind) ??
      placeGrainForFormat(destFormat) ??
      'slot');

  function setDestination(peer: StructureStageHubSummary) {
    onChange(applyDestinationToDraft(draft, peer.stageId, peer.formatKind));
  }

  function setTargetKind(kind: ProgTargetKind) {
    if (formOnly) return;
    onChange(applyTargetKindToDraft(draft, kind));
  }

  const placeUnavailable =
    showKindChoice &&
    draft.targetKind === 'place' &&
    !placesLabeled &&
    !destSchematicQuery.isLoading;

  const selectedRound = useMemo(
    () => rounds.find((r) => r.id === draft.roundId) ?? null,
    [draft.roundId, rounds],
  );
  const orderedPairs = useMemo(
    () => [...bracketPairs].sort((a, b) => a.pairKey.localeCompare(b.pairKey)),
    [bracketPairs],
  );
  const expandRows: Array<{
    key: string;
    label: string;
  }> = useMemo(() => {
    if (orderedPairs.length > 0) {
      return orderedPairs.map((p) => ({
        key: p.pairKey,
        label: pairPlaceMapLabel(p.pairKey, p.slotAKey, p.slotBKey),
      }));
    }
    const fixtures = selectedRound?.fixtures ?? [];
    return fixtures.map((fixture, index) => ({
      key: fixture.id,
      label: fixturePlaceMapLabel(fixture, index, t),
    }));
  }, [orderedPairs, selectedRound?.fixtures, t]);
  const placeSlotKeys = useMemo(() => {
    const source =
      placeGrain === 'group'
        ? draft.destinationGroupIds
        : draft.destinationSlotKeys;
    return resizeDestinationSlotKeys(source, expandRows.length);
  }, [
    draft.destinationGroupIds,
    draft.destinationSlotKeys,
    placeGrain,
    expandRows.length,
  ]);

  function applyPlaceKeys(next: string[]) {
    const aligned = resizeDestinationSlotKeys(next, expandRows.length);
    onChange({
      ...draft,
      targetKind: 'place',
      destinationForm: false,
      destinationSlotKeys: placeGrain === 'slot' ? aligned : [],
      destinationGroupIds: placeGrain === 'group' ? aligned : [],
    });
  }

  const showRoundField = rounds.length > 1;
  const roundSelectDisabled =
    roundsLoading || draft.outcome === 'Winner' || playableRounds.length === 0;

  const roundSelectOptions = useMemo(() => {
    const source = playableRounds.length > 0 ? playableRounds : rounds;
    const options = [
      { value: '', label: t('progression.chooseRound') },
      ...source.map((o) => ({
        value: o.id,
        label:
          o.fixtures.length > 0
            ? t('progression.roundOption', {
                name: o.name,
                count: o.fixtures.length,
              })
            : t('progression.roundOptionEmpty', { name: o.name }),
      })),
    ];
    if (draft.roundId && !source.some((o) => o.id === draft.roundId)) {
      options.push({
        value: draft.roundId,
        label:
          draft.roundName ||
          t('progression.roundFallback', {
            id: draft.roundId.slice(0, 8),
          }),
      });
    }
    return options;
  }, [draft.roundId, draft.roundName, playableRounds, rounds, t]);

  function applyOutcome(outcome: ProgressionOutcome) {
    if (outcome === 'Winner') {
      if (!championshipTerminal) {
        onChange({ ...draft, outcome });
        return;
      }
      onChange({
        ...draft,
        outcome,
        roundId: championshipTerminal.id,
        roundName: championshipTerminal.name,
        expandedPathCount:
          orderedPairs.length > 0
            ? orderedPairs.length
            : championshipTerminal.fixtures.length,
      });
      return;
    }
    onChange({ ...draft, outcome });
  }

  function destinationTileDescription(dest: StructureStageHubSummary) {
    const draftTotal = draftEntriesByDestination.get(dest.stageId) ?? 0;
    const draftThisIntent =
      draft.destinationStageId.trim() === dest.stageId
        ? expandContribution(draft)
        : 0;
    const capacity = dest.compositionCapacity;
    const expected =
      capacity != null && capacity > 0
        ? expectedPopulationWithProgDraft({
            data,
            destination: dest,
            sourceStageId: sourceStage.stageId,
            draftProgVolume: draftTotal,
          })
        : null;
    if (expected != null && capacity != null && capacity > 0) {
      return (
        <DestinationDraftMeter
          count={expected}
          capacity={capacity}
          draft={draftThisIntent}
          label={t('progression.tileContribution', {
            count: expected,
            capacity,
          })}
          draftLabel={t('progression.tileContributionDraft', {
            count: draftThisIntent,
          })}
          ariaLabel={t('progression.tileContributionAria', {
            phase: dest.name,
            count: expected,
            capacity,
            draft: draftThisIntent,
          })}
        />
      );
    }
    if (draftThisIntent > 0) {
      return t('progression.tileContributionEntries', {
        count: draftThisIntent,
      });
    }
    return undefined;
  }

  return (
    <div className="structure-qualification__editor">
      <SortiesWhoWhereFlow
        sourceLabel={t('progression.who')}
        destinationLabel={t('progression.where')}
        feedsLabel={t('progression.feeds')}
        source={
          <>
            <div
              className="structure-qualification__scope-tiles"
              data-count="2"
              role="radiogroup"
              aria-label={t('progression.outcome')}
            >
              <ChoiceTile
                label={t('progression.outcomeWinner')}
                description={t('progression.outcomeWinnerHint')}
                leading={<TrophyIcon size="sm" />}
                selected={draft.outcome === 'Winner'}
                onChange={(selected) => {
                  if (!selected) return;
                  applyOutcome('Winner');
                }}
              />
              <ChoiceTile
                label={t('progression.outcomeLoser')}
                description={t('progression.outcomeLoserHint')}
                leading={<CupFormatIcon size="sm" />}
                selected={draft.outcome === 'Loser'}
                onChange={(selected) => {
                  if (!selected) return;
                  applyOutcome('Loser');
                }}
              />
            </div>

            {showRoundField ? (
              <>
                <Field label={t('progression.round')}>
                  <Select
                    options={roundSelectOptions}
                    value={draft.roundId || null}
                    placeholder={t('progression.chooseRound')}
                    disabled={roundSelectDisabled}
                    onChange={(value) => {
                      if (draft.outcome === 'Winner') return;
                      const id = value ?? '';
                      const round =
                        playableRounds.find((r) => r.id === id) ??
                        rounds.find((r) => r.id === id);
                      onChange({
                        ...draft,
                        roundId: id,
                        roundName: round?.name ?? '',
                        expandedPathCount:
                          orderedPairs.length > 0
                            ? orderedPairs.length
                            : (round?.fixtures.length ?? 0),
                      });
                    }}
                  />
                </Field>
                {playableRounds.length === 0 ? (
                  <p
                    className="structure-qualification__field-hint"
                    role="status"
                  >
                    {t('progression.roundNeedsFixturesHint')}
                  </p>
                ) : draft.outcome === 'Winner' ? (
                  <p
                    className="structure-qualification__field-hint"
                    role="status"
                  >
                    {t('progression.roundWinnerLockedHint')}
                  </p>
                ) : null}
              </>
            ) : null}
          </>
        }
        destination={
          <>
            {peerStages.length === 0 ? (
              <p className="structure-qualification__field-hint" role="status">
                {t('progression.emptyNoPeerBody')}
              </p>
            ) : (
              <div
                className="structure-qualification__scope-tiles"
                data-count={String(Math.min(peerStages.length, 3))}
                role="radiogroup"
                aria-label={t('progression.destinationPhase')}
              >
                {peerStages.map((peer) => (
                  <ChoiceTile
                    key={peer.stageId}
                    label={peer.name}
                    description={destinationTileDescription(peer)}
                    leading={stageFormatIcon(peer.formatKind)}
                    selected={draft.destinationStageId === peer.stageId}
                    onChange={(selected) => {
                      if (!selected) return;
                      setDestination(peer);
                    }}
                  />
                ))}
              </div>
            )}

            {showKindChoice && destStageId ? (
              <div
                className="structure-qualification__scope-tiles"
                data-count="2"
                role="radiogroup"
                aria-label={t('progression.destinationKind')}
              >
                <ChoiceTile
                  label={t('progression.kindPopulation')}
                  description={t('progression.kindPopulationHint')}
                  leading={<StructureIcon size="sm" />}
                  selected={draft.targetKind === 'population'}
                  onChange={(selected) => {
                    if (!selected) return;
                    setTargetKind('population');
                  }}
                />
                <ChoiceTile
                  label={t('progression.kindPlace')}
                  description={t('progression.kindPlaceHint')}
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
                    length: Math.max(expandRows.length, 3),
                  }).map((_, index) => (
                    <li key={`skel-${index}`} aria-hidden="true">
                      <span className="structure-qualification__place-map-skel-label" />
                      <span className="structure-qualification__place-map-skel-control" />
                    </li>
                  ))}
                </ul>
              ) : placeUnavailable || !placesLabeled ? (
                <EmptyState
                  icon={<EmptySelectionIcon size="lg" />}
                  title={t('progression.emptyNoPlacePeerTitle')}
                >
                  {t('progression.emptyNoPlacePeerBody')}
                </EmptyState>
              ) : labeledPlaces.length === 0 ? (
                <EmptyState
                  icon={<EmptySelectionIcon size="lg" />}
                  title={t('progression.placeEmpty')}
                >
                  {t('progression.placeEmpty')}
                </EmptyState>
              ) : expandRows.length === 0 ? (
                <EmptyState
                  icon={<EmptySelectionIcon size="lg" />}
                  title={t('progression.placeMapEmptySelection')}
                >
                  {t('progression.roundNeedsFixturesHint')}
                </EmptyState>
              ) : (
                <div className="structure-qualification__place-map-block">
                  <div className="structure-qualification__place-map-toolbar">
                    {peerStages.length > 0 ? (
                      <p className="structure-qualification__place-map-heading">
                        {t('progression.placeMapHeading')}
                      </p>
                    ) : null}
                    <span className="structure-qualification__place-fill">
                      <span id={placeFillHintId} className="ds-visually-hidden">
                        {t('progression.placeFillHint')}
                      </span>
                      <Tooltip content={t('progression.placeFillHint')}>
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
                          {t('progression.placeFillEmpties')}
                        </button>
                      </Tooltip>
                    </span>
                  </div>
                  <ul
                    className="structure-qualification__place-map"
                    aria-label={t('progression.placeMapAria')}
                  >
                    {expandRows.map((row, index) => {
                      const selected = placeSlotKeys[index]?.trim() || null;
                      const rowId = `prog-place-${draft.id}-${index}`;
                      const rowInvalid = !selected;
                      const sourceLabel = row.label;
                      return (
                        <li
                          key={row.key}
                          data-invalid={rowInvalid ? 'true' : 'false'}
                        >
                          <label
                            className="structure-qualification__place-map-label"
                            htmlFor={rowId}
                          >
                            {sourceLabel}
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
                                'progression.placeSlotPlaceholder',
                              )}
                              allowClear
                              aria-label={t('progression.placeMapRowAria', {
                                source: sourceLabel,
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
                                <ToastToneIcon tone="error" size="sm" />
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
