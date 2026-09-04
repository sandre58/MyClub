import type { ReactNode } from 'react'
import { Dialog } from './Dialog'

export type ConfirmDialogProps = {
  open: boolean
  title: string
  /** Body copy under the title. */
  message: ReactNode
  confirmLabel: string
  cancelLabel: string
  closeLabel?: string
  /** Primary confirm uses danger styling when true (destructive). Default false. */
  danger?: boolean
  confirmDisabled?: boolean
  onConfirm: () => void
  onCancel: () => void
}

/**
 * Stacked confirmation over another Dialog or page — branded replace for
 * `window.confirm`. Parent Dialog should set `trapFocus={false}` and typically
 * `closeDisabled` while this is open.
 */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  cancelLabel,
  closeLabel = 'Fermer',
  danger = false,
  confirmDisabled = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onCancel}
      title={title}
      size="sm"
      closeLabel={closeLabel}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            onClick={onCancel}
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            className={
              danger ? 'ds-btn ds-btn--destructive' : 'ds-btn ds-btn--primary'
            }
            disabled={confirmDisabled}
            onClick={onConfirm}
          >
            {confirmLabel}
          </button>
        </>
      }
    >
      {typeof message === 'string' ? (
        <p className="ds-body">{message}</p>
      ) : (
        message
      )}
    </Dialog>
  )
}
