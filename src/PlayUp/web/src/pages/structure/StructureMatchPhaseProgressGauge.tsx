import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import type { StageStatus } from '../../types';
import { StructureOperationalRatioGauge } from './StructureOperationalGauge';
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

function allMatchesFinished(
  finished: number,
  total: number,
  open: number,
): boolean {
  return total > 0 && open === 0 && finished >= total;
}

function exploitationAsideTone(
  finished: number,
  total: number,
  open: number,
  phaseStatus?: StageStatus,
): 'brand' | 'complete' | 'attention' {
  if (allMatchesFinished(finished, total, open)) {
    return 'complete';
  }
  if (phaseStatus === 'Suspended') {
    return 'attention';
  }
  return 'brand';
}

/** Match exploitation progress — same gauge pattern as production, not hero stats. */
export function StructureMatchPhaseProgressGauge({
  finished,
  total,
  open,
  cancelled: _cancelled,
  layout = 'full',
  phaseStatus,
}: {
  finished: number;
  total: number;
  open: number;
  cancelled: number;
  /** Running/Suspended/Completed: finished-vs-total gauge labels. */
  layout?: 'full' | 'finishedTotal';
  phaseStatus?: StageStatus;
}) {
  const { t } = useTranslation('structure');
  const exploitationGauge = layout === 'finishedTotal';

  const ratioLabel = exploitationGauge
    ? t('lifecycle.matchProgress.ratioLabel')
    : t('fiche.operational.production.ratioLabel', { count: finished });

  const everyMatchFinished = allMatchesFinished(finished, total, open);
  const asideTone = exploitationGauge
    ? exploitationAsideTone(finished, total, open, phaseStatus)
    : 'neutral';

  let caption: ReactNode;
  if (everyMatchFinished) {
    caption = (
      <StripOperationalMessage kind="success">
        {t('lifecycle.matchProgress.allComplete')}
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
        {t('lifecycle.matchProgress.compact', { finished, total })}
      </StripOperationalMessage>
    );
  }

  if (total <= 0) {
    return (
      <StripOperationalMessage kind="info">
        {t('lifecycle.matchProgress.none')}
      </StripOperationalMessage>
    );
  }

  return (
    <MetricRow
      aside={
        <RatioAside
          current={finished}
          capacity={total}
          label={ratioLabel}
          poolTone={asideTone}
        />
      }
      meter={
        <>
          {caption}
          <StructureOperationalRatioGauge
            current={finished}
            capacity={total}
            caption={t('lifecycle.statusBand.matchStatsAria')}
            ariaLabel={t('lifecycle.statusBand.matchStatsAria')}
            poolTone={asideTone}
            fillVariant={
              exploitationGauge
                ? phaseStatus === 'Suspended' && !everyMatchFinished
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
