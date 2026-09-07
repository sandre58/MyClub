import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { ChevronRightIcon } from '../icons/shellIcons'

/**
 * Cross-surface text CTA — label + trailing chevron.
 * Prefer over page-local overview/classements link clones.
 */
export function TextLink({
  to,
  children,
  className,
  showArrow = true,
}: {
  to: string
  children: ReactNode
  className?: string
  showArrow?: boolean
}) {
  const classes = ['ds-text-link', className].filter(Boolean).join(' ')

  return (
    <Link className={classes} to={to}>
      <span>{children}</span>
      {showArrow ? (
        <span className="ds-text-link__arrow" aria-hidden="true">
          <ChevronRightIcon size="sm" />
        </span>
      ) : null}
    </Link>
  )
}
