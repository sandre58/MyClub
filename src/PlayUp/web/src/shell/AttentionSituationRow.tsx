import { Link } from 'react-router-dom'
import { ChevronRightIcon } from '../design-system/icons/shellIcons'
import { attentionTargetTypeLabel } from '../i18n/enumLabels'
import { situationDescription, situationTitle } from '../i18n/situationCopy'
import { situationHref } from '../pages/overviewNavigation'
import type { OverviewSituation } from '../types'

type AttentionSituationRowProps = {
  item: OverviewSituation
  competitionId: string
  onNavigate?: () => void
}

/**
 * Shared À traiter row (drawer + Vue d'ensemble preview).
 * Hover A via `.ds-interactive-row` — no rest fill, no pills.
 * Meta: prefer situation description when available; else targetType label.
 */
export function AttentionSituationRow({
  item,
  competitionId,
  onNavigate,
}: AttentionSituationRowProps) {
  const href = situationHref(item, competitionId)
  const isBlocking = item.nature === 'Blocking'
  const description = situationDescription(item.source, item.params)
  const targetLabel = item.targetType
    ? attentionTargetTypeLabel(item.targetType)
    : null
  const meta = description ?? targetLabel
  const toneClass = isBlocking
    ? 'shell-attention-drawer__row--blocking'
    : 'shell-attention-drawer__row--attention'

  const content = (
    <>
      <span className="shell-attention-drawer__row-main">
        <span className="shell-attention-drawer__row-title">
          {situationTitle(item.source, item.params)}
        </span>
        {meta ? (
          <span className="shell-attention-drawer__row-meta ds-meta">
            {meta}
          </span>
        ) : null}
      </span>
      {href ? (
        <ChevronRightIcon
          size="md"
          className="ds-interactive-row__chevron"
          aria-hidden="true"
        />
      ) : null}
    </>
  )

  return (
    <li className="shell-attention-drawer__item">
      {href ? (
        <Link
          className={`ds-interactive-row shell-attention-drawer__row ${toneClass}`}
          to={href}
          onClick={onNavigate}
        >
          {content}
        </Link>
      ) : (
        <div className={`shell-attention-drawer__row ${toneClass}`}>
          {content}
        </div>
      )}
    </li>
  )
}

export function partitionAttentionItems(items: OverviewSituation[]): {
  blocking: OverviewSituation[]
  attention: OverviewSituation[]
} {
  const blocking: OverviewSituation[] = []
  const attention: OverviewSituation[] = []

  for (const item of items) {
    if (item.nature === 'Blocking') {
      blocking.push(item)
    } else {
      attention.push(item)
    }
  }

  return { blocking, attention }
}
