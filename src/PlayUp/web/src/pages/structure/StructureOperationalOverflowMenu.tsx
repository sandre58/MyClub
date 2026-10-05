import { useRef, useState, type ReactNode } from 'react';
import { Popover } from '../../design-system/components/Popover';
import { Tooltip } from '../../design-system/components/Tooltip';
import { OverflowMenuIcon } from '../../design-system/icons/shellIcons';

export type StructureOperationalMenuItem = {
  id: string;
  label: string;
  onSelect: () => void;
  disabled?: boolean;
  destructive?: boolean;
  leading?: ReactNode;
};

export function StructureOperationalOverflowMenu({
  items,
  ariaLabel,
}: {
  items: StructureOperationalMenuItem[];
  ariaLabel: string;
}) {
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);

  if (items.length === 0) {
    return null;
  }

  return (
    <>
      <Tooltip content={ariaLabel}>
        <button
          ref={anchorRef}
          type="button"
          className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
          aria-label={ariaLabel}
          aria-haspopup="menu"
          aria-expanded={open}
          onClick={() => setOpen((value) => !value)}
        >
          <OverflowMenuIcon size="sm" />
        </button>
      </Tooltip>
      <Popover
        open={open}
        onOpenChange={setOpen}
        anchorRef={anchorRef}
        role="menu"
        align="end"
        width={240}
        aria-label={ariaLabel}
      >
        <ul className="structure-overflow-menu">
          {items.map((item) => (
            <li key={item.id} role="none">
              <button
                type="button"
                role="menuitem"
                className={[
                  'structure-overflow-menu__item',
                  item.destructive ? 'structure-overflow-menu__item--destructive' : null,
                ]
                  .filter(Boolean)
                  .join(' ')}
                disabled={item.disabled}
                onClick={() => {
                  if (item.disabled) {
                    return;
                  }
                  setOpen(false);
                  item.onSelect();
                }}
              >
                {item.leading ? (
                  <span className="structure-overflow-menu__leading" aria-hidden="true">
                    {item.leading}
                  </span>
                ) : null}
                {item.label}
              </button>
            </li>
          ))}
        </ul>
      </Popover>
    </>
  );
}
