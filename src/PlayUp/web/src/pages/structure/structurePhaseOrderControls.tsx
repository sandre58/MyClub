import type { MouseEvent, PointerEvent } from 'react';
import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Popover } from '../../design-system/components/Popover';
import { Tooltip } from '../../design-system/components/Tooltip';
import { PlusIcon, TrashIcon } from '../../design-system/icons/contentIcons';
import {
  ChevronDownIcon,
  ChevronUpIcon,
} from '../../design-system/icons/shellIcons';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';
const compactDangerIcon =
  'ds-btn ds-btn--destructive ds-icon-button ds-icon-button--compact';

function stopControlEvent(event: MouseEvent | PointerEvent) {
  event.stopPropagation();
}

/**
 * Removes a phase from the list. Sibling of the select button.
 */
export function StructurePhaseRemoveControl({
  stageName,
  onRemove,
}: {
  stageName: string;
  onRemove: () => void;
}) {
  const { t } = useTranslation('structure');
  const label = t('graph.removePhaseName', { name: stageName });

  return (
    <div
      className="structure-topology__remove"
      onClick={stopControlEvent}
      onPointerDown={stopControlEvent}
    >
      <Tooltip content={label}>
        <button
          type="button"
          className={compactDangerIcon}
          aria-label={label}
          onClick={onRemove}
        >
          <TrashIcon size="sm" />
        </button>
      </Tooltip>
    </div>
  );
}

/**
 * Single-phase birth: append is the primary action, insert is the menu.
 */
export function StructureSinglePhaseBirth({
  stageName,
  disabled = false,
  onAppend,
  onInsertBefore,
  onInsertAfter,
}: {
  stageName: string;
  disabled?: boolean;
  onAppend: () => void;
  onInsertBefore: () => void;
  onInsertAfter: () => void;
}) {
  const { t } = useTranslation('structure');
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);
  const menuLabel = t('graph.insertPhaseMenu');

  return (
    <div className="structure-phase-birth">
      <button
        type="button"
        className="ds-btn ds-btn--primary structure-phase-birth__main"
        disabled={disabled}
        onClick={onAppend}
      >
        <PlusIcon size="sm" />
        <span>{t('graph.addPhase')}</span>
      </button>
      <button
        ref={anchorRef}
        type="button"
        className="ds-btn ds-btn--primary structure-phase-birth__more"
        aria-label={menuLabel}
        aria-haspopup="menu"
        aria-expanded={open}
        disabled={disabled}
        onClick={() => setOpen((current) => !current)}
      >
        <ChevronDownIcon size="sm" />
      </button>
      <Popover
        open={open}
        onOpenChange={setOpen}
        anchorRef={anchorRef}
        role="menu"
        align="end"
        width={240}
        aria-label={menuLabel}
      >
        <ul className="structure-overflow-menu">
          <li role="none">
            <button
              type="button"
              role="menuitem"
              className="structure-overflow-menu__item"
              onClick={() => {
                setOpen(false);
                onInsertBefore();
              }}
            >
              {t('graph.insertPhaseBefore', { name: stageName })}
            </button>
          </li>
          <li role="none">
            <button
              type="button"
              role="menuitem"
              className="structure-overflow-menu__item"
              onClick={() => {
                setOpen(false);
                onInsertAfter();
              }}
            >
              {t('graph.insertPhaseAfter', { name: stageName })}
            </button>
          </li>
        </ul>
      </Popover>
    </div>
  );
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
