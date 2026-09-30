// -----------------------------------------------------------------------
// Place awards — tile per confrontation (immutable fixture).
// Domain: 0..2 paths per card.
// -----------------------------------------------------------------------

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { fetchStageOverview, replaceStagePlacementAwardRules } from '../api';
import { Alert } from '../design-system/components/Alert';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
import { DropDownButton } from '../design-system/components/DropDownButton';
import { InputNumber } from '../design-system/components/InputNumber';
import { Tooltip } from '../design-system/components/Tooltip';
import {
  CheckIcon,
  EmptySelectionIcon,
  PlusIcon,
  StructureIcon,
  TrashIcon,
} from '../design-system/icons/contentIcons';
import { CloseIcon } from '../design-system/icons/shellIcons';
import { ToastToneIcon } from '../design-system/icons/toastIcons';
import { notify } from '../design-system/toastStore';
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import { queryKeys } from '../queryKeys';
import type { StructureStageHubSummary, StructureView } from '../types';
import { EmptyState, LoadingState, MutationError, PendingLabel } from '../ui';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { ordinalRankSuffix } from './structureQualificationDraft';
import {
  areCardsComplete,
  awardedRankCount,
  cardsFromApiPaths,
  cardsToApiPaths,
  createNextPlacementCard,
  hasNonContiguousRanks,
  incompleteCardReason,
  listPlacementSourceOptions,
  parseOptionalRank,
  serializeCards,
  type PlacementAwardCardDraft,
  type PlacementIncompleteReason,
} from './structurePlacementAwardDraft';

type StructurePlacementAwardDialogProps = {
  data: StructureView;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
};

export function StructurePlacementAwardDialog({
  data,
  stage,
  open,
  onClose,
}: StructurePlacementAwardDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tReg } = useTranslation('regulation');
  const queryClient = useQueryClient();

  const overviewQuery = useQuery({
    queryKey: queryKeys.stages.detail(stage.stageId),
    queryFn: () => fetchStageOverview(stage.stageId),
    enabled: open,
  });

  const [cards, setCards] = useState<PlacementAwardCardDraft[]>([]);
  const [baselineSerialized, setBaselineSerialized] = useState('');
  const [sessionReady, setSessionReady] = useState(false);

  const rounds = overviewQuery.data?.rounds ?? [];
  const bracketPairs = overviewQuery.data?.bracketPairs ?? [];
  const sourceOptions = useMemo(
    () =>
      listPlacementSourceOptions(bracketPairs, rounds, (n) =>
        t('fiche.rule.matchNumber', { n }),
      ),
    [bracketPairs, rounds, t],
  );
  const knownSourceKeys = useMemo(
    () => new Set(sourceOptions.map((o) => o.id)),
    [sourceOptions],
  );

  const dirty = sessionReady && serializeCards(cards) !== baselineSerialized;
  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(dirty, onClose);

  const mutation = useMutation({
    mutationFn: () => {
      if (cards.length === 0) {
        return replaceStagePlacementAwardRules(stage.stageId, { paths: null });
      }
      return replaceStagePlacementAwardRules(stage.stageId, {
        paths: cardsToApiPaths(cards),
      });
    },
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      notify.success(t('attribution.toastUpdated'));
      onClose();
    },
  });

  useEffect(() => {
    if (!open) return;
    mutation.reset();
    setSessionReady(false);
  }, [open]); // eslint-disable-line react-hooks/exhaustive-deps -- open edge only

  useEffect(() => {
    if (!open) return;
    if (overviewQuery.isLoading) return;
    if (mutation.isPending || mutation.isSuccess) return;

    const existing = stage.placementAwards ?? [];
    const next = existing.length > 0 ? cardsFromApiPaths(existing) : [];
    setCards(next);
    setBaselineSerialized(serializeCards(next));
    setSessionReady(true);
    resetDiscard();
  }, [
    open,
    overviewQuery.isLoading,
    mutation.isPending,
    mutation.isSuccess,
    stage.placementAwards,
    stage.stageId,
    resetDiscard,
  ]);

  const canAuthor = sourceOptions.length > 0;
  const cardsComplete =
    sessionReady && areCardsComplete(cards, knownSourceKeys);
  const canSave = !mutation.isPending && cardsComplete;
  const rankTotal = sessionReady ? awardedRankCount(cards) : 0;
  const nonContiguous =
    sessionReady && cardsComplete && hasNonContiguousRanks(cards);

  const firstIncompleteReason =
    useMemo((): PlacementIncompleteReason | null => {
      if (!sessionReady) return null;
      for (const card of cards) {
        const reason = incompleteCardReason(card, cards, knownSourceKeys);
        if (reason != null) return reason;
      }
      return null;
    }, [cards, knownSourceKeys, sessionReady]);

  const saveBlockedReason =
    !sessionReady || mutation.isPending || mutation.isSuccess
      ? null
      : firstIncompleteReason != null
        ? t(`attribution.incompleteHint${firstIncompleteReason}`)
        : null;

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  function addSource(sourcePairKey: string) {
    setCards((prev) => [
      ...prev,
      createNextPlacementCard(prev, [sourcePairKey]),
    ]);
  }

  function removeCard(id: string) {
    setCards((prev) => prev.filter((c) => c.id !== id));
  }

  function updateCard(next: PlacementAwardCardDraft) {
    setCards((prev) => prev.map((c) => (c.id === next.id ? next : c)));
  }

  const usedSourceKeys = useMemo(() => {
    const used = new Set<string>();
    for (const card of cards) {
      const id = card.sourcePairKey.trim();
      if (id) used.add(id);
    }
    return used;
  }, [cards]);

  const freeSourceItems = useMemo(
    () =>
      sourceOptions
        .filter((o) => !usedSourceKeys.has(o.id))
        .map((o) => ({ value: o.id, label: o.label })),
    [sourceOptions, usedSourceKeys],
  );

  const canAdd =
    sessionReady &&
    canAuthor &&
    freeSourceItems.length > 0 &&
    !mutation.isPending;

  const sourceLabelById = useMemo(() => {
    const map = new Map<string, string>();
    for (const o of sourceOptions) map.set(o.id, o.label);
    return map;
  }, [sourceOptions]);

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('attribution.title', { phase: stage.name })}
        description={t('attribution.hint')}
        size="lg"
        closeLabel={tCommon('close')}
        closeDisabled={mutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        footerStatus={
          mutation.isError || saveBlockedReason || nonContiguous ? (
            <>
              {mutation.isError ? (
                <MutationError error={mutation.error} />
              ) : null}
              {saveBlockedReason ? (
                <Alert tone="danger" role="alert">
                  <p className="structure-qualification__hint-line">
                    {saveBlockedReason}
                  </p>
                </Alert>
              ) : null}
              {nonContiguous && !saveBlockedReason ? (
                <Alert tone="warning" role="status">
                  <p className="structure-qualification__hint-line">
                    {t('attribution.nonContiguousWarning')}
                  </p>
                </Alert>
              ) : null}
            </>
          ) : null
        }
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--secondary"
              disabled={mutation.isPending || discardOpen}
              onClick={requestClose}
            >
              <CloseIcon size="sm" />
              {tCommon('cancel')}
            </button>
            <button
              type="button"
              className="ds-btn ds-btn--primary"
              disabled={!canSave || !dirty}
              onClick={() => mutation.mutate()}
            >
              {mutation.isPending ? (
                <PendingLabel>{t('attribution.saving')}</PendingLabel>
              ) : (
                <>
                  <CheckIcon size="sm" />
                  {t('attribution.save')}
                </>
              )}
            </button>
          </>
        }
      >
        <div className="structure-qualification structure-attribution">
          <div className="structure-qualification__summary" aria-live="polite">
            <div className="structure-qualification__facts">
              <div className="structure-qualification__fact structure-qualification__fact--secondary">
                <span className="structure-qualification__fact-value">
                  {sessionReady ? cards.length : '—'}
                </span>
                <span className="structure-qualification__fact-label">
                  {t('attribution.factCards', { count: cards.length })}
                </span>
              </div>
              <span
                className="structure-qualification__fact-rule"
                aria-hidden="true"
              />
              <div className="structure-qualification__fact structure-qualification__fact--primary">
                <span className="structure-qualification__fact-value">
                  {sessionReady ? rankTotal : '—'}
                </span>
                <span className="structure-qualification__fact-label">
                  {t('attribution.factRanks', { count: rankTotal })}
                </span>
              </div>
            </div>
            <DropDownButton
              label={t('attribution.add')}
              aria-label={t('attribution.add')}
              leadingIcon={<PlusIcon size="sm" />}
              items={freeSourceItems}
              emptyLabel={t('attribution.addEmpty')}
              disabled={!canAdd}
              align="end"
              onSelect={addSource}
            />
          </div>

          {!sessionReady ? (
            <LoadingState size="region" />
          ) : !canAuthor ? (
            <EmptyState
              variant="idle"
              icon={<StructureIcon size="lg" />}
              title={t('attribution.emptyNoFixtureTitle')}
            >
              {t('attribution.emptyNoFixtureBody')}
            </EmptyState>
          ) : cards.length === 0 ? (
            <EmptyState
              variant="idle"
              icon={<EmptySelectionIcon size="lg" />}
              title={t('attribution.emptyTitle')}
            >
              {t('attribution.emptyBody')}
            </EmptyState>
          ) : (
            <div className="structure-attribution__body">
              <ul className="structure-attribution__tiles">
                {cards.map((card) => (
                  <li key={card.id}>
                    <AttributionTile
                      draft={card}
                      all={cards}
                      title={
                        sourceLabelById.get(card.sourcePairKey.trim()) ??
                        t('attribution.unknownFixture')
                      }
                      knownSourceKeys={knownSourceKeys}
                      disabled={mutation.isPending}
                      onChange={updateCard}
                      onRemove={() => removeCard(card.id)}
                    />
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      </Dialog>

      <ConfirmDialog
        open={discardOpen}
        title={tReg('editor.discardTitle')}
        message={tReg('editor.discardMessage')}
        confirmLabel={tReg('editor.discardConfirm')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        danger
        onCancel={cancelDiscard}
        onConfirm={confirmDiscard}
      />
    </>
  );
}

function AttributionTile({
  draft,
  all,
  title,
  knownSourceKeys,
  disabled,
  onChange,
  onRemove,
}: {
  draft: PlacementAwardCardDraft;
  all: PlacementAwardCardDraft[];
  title: string;
  knownSourceKeys: ReadonlySet<string>;
  disabled: boolean;
  onChange: (next: PlacementAwardCardDraft) => void;
  onRemove: () => void;
}) {
  const { t, i18n } = useTranslation('structure');
  const winnerFieldId = useId();
  const loserFieldId = useId();

  const winnerValue = parseOptionalRank(draft.winnerRank);
  const loserValue = parseOptionalRank(draft.loserRank);
  const winnerNum = typeof winnerValue === 'number' ? winnerValue : null;
  const loserNum = typeof loserValue === 'number' ? loserValue : null;

  const incompleteReason = incompleteCardReason(draft, all, knownSourceKeys);
  const statusMessage =
    incompleteReason == null
      ? null
      : t(`attribution.incompleteHint${incompleteReason}`);

  return (
    <div className="structure-attribution__tile">
      <div className="structure-attribution__tile-title-row">
        {statusMessage ? (
          <Tooltip content={statusMessage}>
            <span
              className="structure-qualification__blocking-mark"
              aria-label={statusMessage}
            >
              <ToastToneIcon tone="error" size="sm" />
            </span>
          </Tooltip>
        ) : null}
        <h3 className="structure-attribution__tile-title">{title}</h3>
      </div>

      <div className="structure-attribution__tile-ranks">
        <div className="structure-attribution__rank-control">
          <InputNumber
            id={winnerFieldId}
            min={1}
            value={winnerNum}
            controlsLayout="split"
            tone="success"
            disabled={disabled}
            prefix={t('attribution.winnerChip')}
            suffix={
              winnerNum != null
                ? ordinalRankSuffix(winnerNum, i18n.language)
                : undefined
            }
            aria-label={t('attribution.winnerRank')}
            onChange={(value) =>
              onChange({
                ...draft,
                winnerRank: value != null ? String(value) : '',
              })
            }
          />
        </div>
        <div className="structure-attribution__rank-control">
          <InputNumber
            id={loserFieldId}
            min={1}
            value={loserNum}
            controlsLayout="split"
            tone="danger"
            disabled={disabled}
            prefix={t('attribution.loserChip')}
            suffix={
              loserNum != null
                ? ordinalRankSuffix(loserNum, i18n.language)
                : undefined
            }
            aria-label={t('attribution.loserRank')}
            onChange={(value) =>
              onChange({
                ...draft,
                loserRank: value != null ? String(value) : '',
              })
            }
          />
        </div>
      </div>

      <button
        type="button"
        className="ds-btn ds-btn--ghost ds-btn--destructive ds-icon-button structure-qualification__trash"
        aria-label={t('attribution.remove')}
        disabled={disabled}
        onClick={onRemove}
      >
        <TrashIcon size="sm" />
      </button>
    </div>
  );
}
