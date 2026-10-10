import type { ComponentPropsWithoutRef, ElementType, ReactNode } from 'react';

type SelectableTileOwnProps<T extends ElementType> = {
  as?: T;
  selected?: boolean;
  className?: string;
  children: ReactNode;
};

export type SelectableTileProps<T extends ElementType = 'button'> =
  SelectableTileOwnProps<T> &
    Omit<ComponentPropsWithoutRef<T>, keyof SelectableTileOwnProps<T>>;

/**
 * Family B — entity tile with shared selection chrome (`.ds-selectable-tile`).
 * Page classes handle layout only; do not restyle selection in feature CSS.
 */
export function SelectableTile<T extends ElementType = 'button'>({
  as,
  selected = false,
  className = '',
  children,
  ...rest
}: SelectableTileProps<T>) {
  const Tag = (as ?? 'button') as ElementType;
  const classes = ['ds-selectable-tile', className].filter(Boolean).join(' ');

  return (
    <Tag className={classes} data-selected={selected ? 'true' : 'false'} {...rest}>
      {children}
    </Tag>
  );
}
