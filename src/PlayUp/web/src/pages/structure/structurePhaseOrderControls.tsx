import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Popover } from '../../design-system/components/Popover';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  ChevronDownIcon,
  ChevronUpIcon,
  OverflowMenuIcon,
} from '../../design-system/icons/shellIcons';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

/**
 * Explicit phase order controls: move up/down and insert before/after.
 * No drag-and-drop. Insert opens the birth dialog; move calls the order route.
 */
export function StructurePhaseOrderControls({
  stageName,
  disabled = false,
  pending = null,
  onMoveUp,
  onMoveDown,
  moveUpDisabled = false,
  moveDownDisabled = false,
  onInsertBefore,
  onInsertAfter,
}: {
  stageName: string;
  disabled?: boolean;
  pending?: 'up' | 'down' | null;
  onMoveUp?: () => void;
  onMoveDown?: () => void;
  moveUpDisabled?: boolean;
  moveDownDisabled?: boolean;
  onInsertBefore: () => void;
  onInsertAfter: () => void;
}) {
  const { t } = useTranslation('structure');
  const [menuOpen, setMenuOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);
  const showMove = onMoveUp != null && onMoveDown != null;
  const menuLabel = t('graph.orderMenu', { name: stageName });

  return (
    <div className="structure-topology__order">
      {showMove ? (
        <>
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
        </>
      ) : null}
      <Tooltip content={menuLabel}>
        <button
          ref={anchorRef}
          type="button"
          className={compactIcon}
          aria-label={menuLabel}
          aria-haspopup="menu"
          aria-expanded={menuOpen}
          disabled={disabled}
          onClick={() => setMenuOpen((open) => !open)}
        >
          <OverflowMenuIcon size="sm" />
        </button>
      </Tooltip>
      <Popover
        open={menuOpen}
        onOpenChange={setMenuOpen}
        anchorRef={anchorRef}
        role="menu"
        align="end"
        width={220}
        aria-label={menuLabel}
      >
        <ul className="structure-overflow-menu">
          <li role="none">
            <button
              type="button"
              role="menuitem"
              className="structure-overflow-menu__item"
              onClick={() => {
                setMenuOpen(false);
                onInsertBefore();
              }}
            >
              {t('graph.insertBefore')}
            </button>
          </li>
          <li role="none">
            <button
              type="button"
              role="menuitem"
              className="structure-overflow-menu__item"
              onClick={() => {
                setMenuOpen(false);
                onInsertAfter();
              }}
            >
              {t('graph.insertAfter')}
            </button>
          </li>
        </ul>
      </Popover>
    </div>
  );
}
