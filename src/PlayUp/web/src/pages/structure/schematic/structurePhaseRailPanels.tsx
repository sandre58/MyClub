// Phase rail panels — exit editor menu + confrontation readout.
import { useRef, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Popover } from '../../../design-system/components/Popover';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  AggregateIcon,
  ArrowRightIcon,
  LegsStatIcon,
  PenaltiesIcon,
  PencilIcon,
  TimerIcon,
} from '../../../design-system/icons/contentIcons';
import { PinIcon } from '../../../design-system/icons/metaIcons';
import type {
  StructureConfrontationSegment,
  StructureStageHubSummary,
} from '../../../types';
import { type ExitKind } from '../composition/structurePhaseFeeds';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

/** Compact pencil → single action, or Qualif | Prog menu when both apply. */
export function ExitKindMenu({
  label,
  options,
}: {
  label: string;
  options: { kind: ExitKind; label: string; onSelect: () => void }[];
}) {
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);
  if (options.length === 0) return null;

  if (options.length === 1) {
    const only = options[0]!;
    return (
      <Tooltip content={label}>
        <button
          type="button"
          className={compactIcon}
          aria-label={label}
          onClick={only.onSelect}
        >
          <PencilIcon size="sm" />
        </button>
      </Tooltip>
    );
  }

  return (
    <>
      <Tooltip content={label}>
        <button
          ref={anchorRef}
          type="button"
          className={compactIcon}
          aria-label={label}
          aria-haspopup="menu"
          aria-expanded={open}
          onClick={() => setOpen((value) => !value)}
        >
          <PencilIcon size="sm" />
        </button>
      </Tooltip>
      <Popover
        open={open}
        onOpenChange={setOpen}
        anchorRef={anchorRef}
        role="menu"
        align="end"
        width={220}
        aria-label={label}
      >
        <ul className="structure-overflow-menu">
          {options.map((option) => (
            <li key={option.kind} role="none">
              <button
                type="button"
                role="menuitem"
                className="structure-overflow-menu__item"
                onClick={() => {
                  setOpen(false);
                  option.onSelect();
                }}
              >
                {option.label}
              </button>
            </li>
          ))}
        </ul>
      </Popover>
    </>
  );
}
export function ConfrontationPanel({
  stage,
}: {
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('regulation');
  const segments = [...(stage.confrontationSegments ?? [])].sort((a, b) => {
    const ao = a.rounds[0]?.sortOrder ?? 0;
    const bo = b.rounds[0]?.sortOrder ?? 0;
    return ao - bo;
  });

  const renderItems = (seg: StructureConfrontationSegment, prefix: string) => {
    const twoLegs = seg.numberOfLegs > 1;
    const items: { key: string; label: string; icon: ReactNode }[] = [
      {
        key: `${prefix}-legs`,
        label: twoLegs ? t('tokens.tieTwoLegs') : t('tokens.tieOneLeg'),
        icon: twoLegs ? (
          <LegsStatIcon size="sm" />
        ) : (
          <ArrowRightIcon size="sm" />
        ),
      },
    ];
    if (seg.aggregateScoring) {
      items.push({
        key: `${prefix}-agg`,
        label: t('tokens.tieAggregate'),
        icon: <AggregateIcon size="sm" />,
      });
    }
    if (seg.hasAwayGoalsRule) {
      items.push({
        key: `${prefix}-away`,
        label: t('tokens.tieAwayGoals'),
        icon: <PinIcon size="sm" />,
      });
    }
    if (seg.hasTieExtraTime) {
      items.push({
        key: `${prefix}-et`,
        label: t('tokens.tieExtraTime'),
        icon: <TimerIcon size="sm" />,
      });
    }
    if (seg.hasTiePenaltyShootout) {
      items.push({
        key: `${prefix}-tab`,
        label: t('tokens.tiePenalties'),
        icon: <PenaltiesIcon size="sm" />,
      });
    }
    return items;
  };

  if (segments.length === 0) {
    const fallback: StructureConfrontationSegment = {
      rounds: [],
      numberOfLegs: stage.numberOfLegs ?? 1,
      aggregateScoring: stage.aggregateScoring ?? false,
      hasAwayGoalsRule: stage.hasAwayGoalsRule ?? false,
      hasTieExtraTime: stage.hasTieExtraTime ?? false,
      hasTiePenaltyShootout: stage.hasTiePenaltyShootout ?? false,
    };
    const items = renderItems(fallback, 'default');
    return (
      <ul className="regulation-rule-list structure-confrontation-list">
        {items.map((item) => (
          <li key={item.key} className="regulation-rule-list__item">
            <span className="regulation-rule-list__mark" aria-hidden="true">
              {item.icon}
            </span>
            <span className="regulation-rule-list__label">{item.label}</span>
          </li>
        ))}
      </ul>
    );
  }

  return (
    <div className="structure-confrontation-segments">
      {segments.map((seg, index) => {
        const title =
          seg.rounds
            .map((r) => r.name)
            .filter(Boolean)
            .join(' / ') || `§${index + 1}`;
        const items = renderItems(seg, `seg-${index}`);
        return (
          <div key={title + index} className="structure-confrontation-segment">
            {segments.length > 1 ? (
              <p className="structure-confrontation-segment__title">{title}</p>
            ) : null}
            <ul className="regulation-rule-list structure-confrontation-list">
              {items.map((item) => (
                <li key={item.key} className="regulation-rule-list__item">
                  <span
                    className="regulation-rule-list__mark"
                    aria-hidden="true"
                  >
                    {item.icon}
                  </span>
                  <span className="regulation-rule-list__label">
                    {item.label}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        );
      })}
    </div>
  );
}
