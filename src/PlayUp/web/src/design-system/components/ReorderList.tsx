import {
  useRef,
  useState,
  type DragEvent,
  type KeyboardEvent,
  type ReactNode,
} from 'react';
import { CloseIcon } from '../icons/shellIcons';
import { GripIcon } from '../icons/overviewIcons';
import { Tooltip } from './Tooltip';

export type ReorderListProps<T> = {
  items: T[];
  getKey: (item: T) => string;
  onReorder: (next: T[]) => void;
  onRemove?: (item: T, index: number) => void;
  /** When false, row cannot be dragged or keyboard-moved (e.g. pinned Points). */
  canDrag?: (item: T, index: number) => boolean;
  /** When false, hide remove control for that row. */
  canRemove?: (item: T, index: number) => boolean;
  /**
   * Lowest index a movable item may occupy (default 0).
   * Use 1 to keep a pinned head slot (e.g. Points always #1).
   */
  minMoveIndex?: number;
  renderContent: (item: T, index: number) => ReactNode;
  removeLabel?: string;
  dragLabel?: string;
  'aria-label'?: string;
};

function moveItem<T>(list: T[], from: number, to: number): T[] {
  if (
    from === to ||
    from < 0 ||
    to < 0 ||
    from >= list.length ||
    to >= list.length
  ) {
    return list;
  }
  const next = [...list];
  const [item] = next.splice(from, 1);
  next.splice(to, 0, item);
  return next;
}

/**
 * Ordered list with drag handle + remove. HTML5 DnD (no extra lib).
 * Keyboard: Alt+ArrowUp / Alt+ArrowDown on the row.
 */
export function ReorderList<T>({
  items,
  getKey,
  onReorder,
  onRemove,
  canDrag = () => true,
  canRemove = () => true,
  minMoveIndex = 0,
  renderContent,
  removeLabel = 'Retirer',
  dragLabel = 'Réordonner',
  'aria-label': ariaLabel,
}: ReorderListProps<T>) {
  const dragFrom = useRef<number | null>(null);
  const [dropIndex, setDropIndex] = useState<number | null>(null);

  function clampTarget(index: number): number {
    return Math.max(minMoveIndex, Math.min(index, items.length - 1));
  }

  function tryMove(from: number, rawTo: number) {
    if (!canDrag(items[from], from)) {
      return;
    }
    const to = clampTarget(rawTo);
    if (to === from) {
      return;
    }
    onReorder(moveItem(items, from, to));
  }

  function handleDragStart(index: number, event: DragEvent) {
    if (!canDrag(items[index], index)) {
      event.preventDefault();
      return;
    }
    dragFrom.current = index;
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', String(index));
  }

  function handleDragOver(index: number, event: DragEvent) {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
    if (dropIndex !== index) {
      setDropIndex(index);
    }
  }

  function handleDrop(index: number, event: DragEvent) {
    event.preventDefault();
    const from = dragFrom.current;
    dragFrom.current = null;
    setDropIndex(null);
    if (from == null) {
      return;
    }
    tryMove(from, index);
  }

  function handleDragEnd() {
    dragFrom.current = null;
    setDropIndex(null);
  }

  function handleKeyDown(index: number, event: KeyboardEvent) {
    if (!event.altKey || !canDrag(items[index], index)) {
      return;
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      tryMove(index, index - 1);
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      tryMove(index, index + 1);
    }
  }

  return (
    <ul className="ds-reorder" aria-label={ariaLabel}>
      {items.map((item, index) => {
        const key = getKey(item);
        const draggable = canDrag(item, index);
        const removable = onRemove != null && canRemove(item, index);
        return (
          <li
            key={key}
            className="ds-reorder__item"
            data-drop={dropIndex === index ? 'true' : 'false'}
            data-pinned={draggable ? 'false' : 'true'}
            draggable={draggable}
            onDragStart={(event) => handleDragStart(index, event)}
            onDragOver={(event) => handleDragOver(index, event)}
            onDrop={(event) => handleDrop(index, event)}
            onDragEnd={handleDragEnd}
            onKeyDown={(event) => handleKeyDown(index, event)}
            tabIndex={0}
          >
            {draggable ? (
              <Tooltip content={dragLabel}>
                <span
                  className="ds-reorder__handle"
                  aria-hidden="true"
                  data-disabled="false"
                >
                  <GripIcon size="sm" />
                </span>
              </Tooltip>
            ) : (
              <span
                className="ds-reorder__handle"
                aria-hidden="true"
                data-disabled="true"
              >
                <GripIcon size="sm" />
              </span>
            )}
            <span className="ds-reorder__index" aria-hidden="true">
              {index + 1}
            </span>
            <div className="ds-reorder__content">
              {renderContent(item, index)}
            </div>
            {removable ? (
              <Tooltip content={removeLabel}>
                <button
                  type="button"
                  className="ds-reorder__remove"
                  aria-label={removeLabel}
                  onClick={() => onRemove(item, index)}
                >
                  <CloseIcon size="sm" aria-hidden="true" />
                </button>
              </Tooltip>
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}
