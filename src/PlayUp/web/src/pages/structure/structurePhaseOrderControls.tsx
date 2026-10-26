import type { MouseEvent, PointerEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Tooltip } from '../../design-system/components/Tooltip';
import { PlusIcon } from '../../design-system/icons/contentIcons';
import {
  ChevronDownIcon,
  ChevronUpIcon,
} from '../../design-system/icons/shellIcons';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

function stopControlEvent(event: MouseEvent | PointerEvent) {
  event.stopPropagation();
}

/**
 * Quiet birth control for a phase gap. Icon only; the accessible name
 * carries before/after so identical slots stay distinct.
 */
export function StructurePhaseInsertSlot({
  stageName,
  position,
  disabled = false,
  onInsert,
}: {
  stageName: string;
  position: 'before' | 'after';
  disabled?: boolean;
  onInsert: () => void;
}) {
  const { t } = useTranslation('structure');
  const accessibleName = t(
    position === 'before'
      ? 'graph.insertPhaseBefore'
      : 'graph.insertPhaseAfter',
    { name: stageName },
  );

  return (
    <div className="structure-topology__insert">
      <Tooltip content={accessibleName}>
        <button
          type="button"
          className={`${compactIcon} structure-topology__insert-button`}
          aria-label={accessibleName}
          disabled={disabled}
          onClick={onInsert}
        >
          <PlusIcon size="sm" />
        </button>
      </Tooltip>
    </div>
  );
}

/**
 * Move toolbar at the bottom end of the tile, beside the select hit area.
 * Sibling of the select button so the chevrons are not nested in it.
 */
export function StructurePhaseMoveControls({
  stageName,
  disabled = false,
  pending = null,
  onMoveUp,
  onMoveDown,
  moveUpDisabled = false,
  moveDownDisabled = false,
}: {
  stageName: string;
  disabled?: boolean;
  pending?: 'up' | 'down' | null;
  onMoveUp: () => void;
  onMoveDown: () => void;
  moveUpDisabled?: boolean;
  moveDownDisabled?: boolean;
}) {
  const { t } = useTranslation('structure');

  return (
    <div
      className="structure-topology__move"
      role="toolbar"
      aria-orientation="horizontal"
      aria-label={t('graph.moveToolbar', { name: stageName })}
      onClick={stopControlEvent}
      onPointerDown={stopControlEvent}
    >
      <Tooltip content={t('graph.moveUp', { name: stageName })}>
        <button
          type="button"
          className={compactIcon}
          aria-label={t('graph.moveUp', { name: stageName })}
          disabled={disabled || moveUpDisabled}
          onClick={onMoveUp}
        >
          {pending === 'up' ? (
            <span className="ds-spinner" aria-hidden="true" />
          ) : (
            <ChevronUpIcon size="sm" />
          )}
        </button>
      </Tooltip>
      <Tooltip content={t('graph.moveDown', { name: stageName })}>
        <button
          type="button"
          className={compactIcon}
          aria-label={t('graph.moveDown', { name: stageName })}
          disabled={disabled || moveDownDisabled}
          onClick={onMoveDown}
        >
          {pending === 'down' ? (
            <span className="ds-spinner" aria-hidden="true" />
          ) : (
            <ChevronDownIcon size="sm" />
          )}
        </button>
      </Tooltip>
    </div>
  );
}
