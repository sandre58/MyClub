import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import type { StageStatus } from '../../types';
import { StructureOperationalRatioGauge } from './StructureOperationalGauge';
import {
  matchProgressAllClosed,
  matchProgressClosed,
} from './structurePhaseStatusBandModel';
import {
  formatSwissMatchProgressCaption,
  type SwissMatchsGuidanceView,
} from './swissMatchsGuidance';
import { StripOperationalMessage } from './structureOperationalStripUi';

function RatioAside({
  current,
  capacity,
  label,
  poolTone,
}: {
  current: number;
  capacity: number;
  label: string;
  poolTone: 'neutral' | 'partial' | 'complete' | 'brand' | 'attention';
}) {
  return (
    <div
      className="structure-operational-strip__aside"
      data-pool-tone={poolTone}
    >
      <span className="structure-operational-strip__ratio">
        <span className="structure-operational-strip__ratio-filled">{current}</span>
        <span className="structure-operational-strip__ratio-sep">/</span>
        <span className="structure-operational-strip__ratio-cap">{capacity}</span>
      </span>
      <span className="structure-operational-strip__ratio-label">{label}</span>
    </div>
  );
}

function MetricRow({ aside, meter }: { aside: ReactNode; meter: ReactNode }) {
  return (
    <div className="structure-operational-strip__metric-row">
      {aside}
      <div className="structure-operational-strip__metric-main">{meter}</div>
    </div>
  );
}

function exploitationAsideTone(
  total: number,
  open: number,
  phaseStatus?: StageStatus,
): 'brand' | 'complete' | 'attention' {
  if (total > 0 && open === 0) {
    return 'complete';
  }
  if (phaseStatus === 'Suspended') {
    return 'attention';
  }
  return 'brand';
}

/** Match exploitation progress — terminés / total (finished + cancelled), same gauge pattern as production. */
export function StructureMatchPhaseProgressGauge({
  finished,
  total,
  open,
  cancelled,
  layout = 'full',
  phaseStatus,
  swissProgressCaption,
}: {
  finished: number;
  total: number;
  open: number;
  cancelled: number;
  /** Running/Suspended/Completed: closed-vs-total gauge labels. */
  layout?: 'full' | 'finishedTotal';
  phaseStatus?: StageStatus;
  /** When set, replaces generic lifecycle copy above the gauge (Swiss guidance). */
  swissProgressCaption?: SwissMatchsGuidanceView | null;
}) {
  const { t } = useTranslation('structure');
  const closed = matchProgressClosed(finished, cancelled);
  const exploitationGauge = layout === 'finishedTotal';
  const allClosed = matchProgressAllClosed(total, finished, cancelled, open);

  const swissCaptionText =
    swissProgressCaption != null
      ? formatSwissMatchProgressCaption(t, swissProgressCaption)
      : null;

  const ratioLabel = exploitationGauge
    ? t('lifecycle.matchProgress.ratioLabel')
    : t('fiche.operational.production.ratioLabel', { count: finished });

  const asideTone = exploitationGauge
    ? exploitationAsideTone(total, open, phaseStatus)
    : 'neutral';

  let caption: ReactNode;
  if (swissCaptionText != null) {
    caption = (
      <StripOperationalMessage
        kind={
          allClosed && swissProgressCaption?.kind !== 'D'
            ? 'success'
            : phaseStatus === 'Suspended'
              ? 'attention'
              : 'info'
        }
      >
        {swissCaptionText}
      </StripOperationalMessage>
    );
  } else if (allClosed) {
    caption = (
      <StripOperationalMessage kind="success">
        {t('lifecycle.matchProgress.allClosed')}
      </StripOperationalMessage>
    );
  } else if (exploitationGauge) {
    caption = (
      <StripOperationalMessage
        kind={phaseStatus === 'Suspended' ? 'attention' : 'info'}
      >
        {phaseStatus === 'Suspended'
          ? t('lifecycle.matchProgress.inProgressSuspended')
          : t('lifecycle.matchProgress.inProgress')}
      </StripOperationalMessage>
    );
  } else {
    caption = (
      <StripOperationalMessage kind="info">
        {t('lifecycle.matchProgress.compact', { closed, total })}
      </StripOperationalMessage>
    );
  }

  if (total <= 0) {
    return (
      <StripOperationalMessage kind="info">
        {swissCaptionText ?? t('lifecycle.matchProgress.none')}
      </StripOperationalMessage>
    );
  }

  const gaugeCurrent = exploitationGauge ? closed : finished;

  return (
    <MetricRow
      aside={
        <RatioAside
          current={gaugeCurrent}
          capacity={total}
          label={ratioLabel}
          poolTone={asideTone}
        />
      }
      meter={
        <>
          {caption}
          <StructureOperationalRatioGauge
            current={gaugeCurrent}
            capacity={total}
            caption={t('lifecycle.statusBand.matchStatsAria')}
            ariaLabel={t('lifecycle.statusBand.matchStatsAria')}
            poolTone={asideTone}
            fillVariant={
              exploitationGauge
                ? phaseStatus === 'Suspended' && !allClosed
                  ? 'attention'
                  : 'brand'
                : 'default'
            }
            layout="trackOnly"
          />
        </>
      }
    />
  );
}
