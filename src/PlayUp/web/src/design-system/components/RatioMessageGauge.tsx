import type { ReactNode } from 'react';
import {
  CheckCircleIcon,
  InfoIcon,
  OverviewAttentionIcon,
  StructureIssueIcon,
} from '../icons/contentIcons';

export type RatioMessageGaugeSize = 'default' | 'compact';
export type RatioMessageGaugeMessageKind =
  | 'info'
  | 'success'
  | 'attention'
  | 'danger';
export type RatioMessageGaugeRatioTone =
  | 'neutral'
  | 'brand'
  | 'attention'
  | 'danger'
  | 'complete'
  | 'partial';

function messageIcon(kind: RatioMessageGaugeMessageKind) {
  switch (kind) {
    case 'success':
      return <CheckCircleIcon size="sm" aria-hidden="true" />;
    case 'attention':
      return <OverviewAttentionIcon size="sm" aria-hidden="true" />;
    case 'danger':
      return <StructureIssueIcon size="sm" aria-hidden="true" />;
    default:
      return <InfoIcon size="sm" aria-hidden="true" />;
  }
}

function meterTone(
  kind: RatioMessageGaugeMessageKind,
  overCapacity: boolean,
): 'brand' | 'success' | 'attention' | 'error' | 'neutral' {
  if (overCapacity) {
    return 'error';
  }
  switch (kind) {
    case 'success':
      return 'success';
    case 'attention':
      return 'attention';
    case 'danger':
      return 'error';
    default:
      return 'brand';
  }
}

function fillRatio(current: number, capacity: number): number {
  if (capacity <= 0) {
    return current > 0 ? 1 : 0;
  }
  return Math.min(1, Math.max(0, current / capacity));
}

/**
 * Ratio + status message + progress track (operational strip / composition band).
 */
export function RatioMessageGauge({
  size = 'default',
  current,
  capacity,
  ratioCaption,
  message,
  messageKind = 'info',
  ratioTone = 'neutral',
  ariaLabel,
  loading = false,
  loadingMessage,
  className,
}: {
  size?: RatioMessageGaugeSize;
  current: number | null;
  capacity: number | null;
  /** Caption under the ratio (e.g. « Résolues / Qualifiées »). */
  ratioCaption: string;
  message: ReactNode;
  messageKind?: RatioMessageGaugeMessageKind;
  ratioTone?: RatioMessageGaugeRatioTone;
  ariaLabel: string;
  loading?: boolean;
  loadingMessage?: string;
  className?: string;
}) {
  const unknown =
    capacity == null || capacity <= 0 || current == null || loading;
  const safeCurrent = current ?? 0;
  const safeCapacity = capacity ?? 0;
  const overCapacity =
    !unknown && safeCapacity > 0 && safeCurrent > safeCapacity;
  const trackTone = meterTone(messageKind, overCapacity);
  const width = unknown ? 0 : fillRatio(safeCurrent, safeCapacity) * 100;

  const classes = [
    'ds-ratio-message-gauge',
    size === 'compact' ? 'ds-ratio-message-gauge--compact' : null,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={classes} role="status" aria-label={ariaLabel}>
      <div
        className="ds-ratio-message-gauge__aside"
        data-pool-tone={ratioTone}
      >
        {loading ? (
          <span className="ds-ratio-message-gauge__ratio ds-ratio-message-gauge__ratio--loading">
            …
          </span>
        ) : unknown ? (
          <span className="ds-ratio-message-gauge__ratio ds-ratio-message-gauge__ratio--unknown">
            —
          </span>
        ) : (
          <span className="ds-ratio-message-gauge__ratio">
            <span className="ds-ratio-message-gauge__ratio-filled">
              {safeCurrent}
            </span>
            <span className="ds-ratio-message-gauge__ratio-sep">/</span>
            <span className="ds-ratio-message-gauge__ratio-cap">
              {safeCapacity}
            </span>
          </span>
        )}
        <span className="ds-ratio-message-gauge__ratio-caption">
          {ratioCaption}
        </span>
      </div>
      <div className="ds-ratio-message-gauge__main">
        {loading && loadingMessage ? (
          <p className="ds-ratio-message-gauge__message ds-ratio-message-gauge__message--loading">
            {loadingMessage}
          </p>
        ) : (
          <div
            className="ds-ratio-message-gauge__message"
            data-kind={messageKind}
          >
            <span
              className="ds-ratio-message-gauge__message-icon"
              aria-hidden="true"
            >
              {messageIcon(messageKind)}
            </span>
            <span className="ds-ratio-message-gauge__message-text">
              {message}
            </span>
          </div>
        )}
        <div
          className="ds-meter ds-ratio-message-gauge__meter"
          data-tone={trackTone}
          aria-hidden="true"
        >
          <span
            className="ds-meter__fill"
            style={{ width: `${width}%` }}
          />
        </div>
      </div>
    </div>
  );
}
