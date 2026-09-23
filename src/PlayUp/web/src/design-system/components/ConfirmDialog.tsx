import type { ReactNode } from 'react';
import { CheckIcon, TrashIcon } from '../icons/contentIcons';
import { CloseIcon } from '../icons/shellIcons';
import { Dialog } from './Dialog';

export type ConfirmDialogProps = {
  open: boolean;
  title: string;
  /** Body copy under the title. */
  message: ReactNode;
  confirmLabel: string;
  cancelLabel: string;
  closeLabel?: string;
  /** Primary confirm uses danger styling when true (destructive). Default false. */
  danger?: boolean;
  /**
   * Leading icon on the confirm button. Defaults to Trash when `danger`, else Check.
   * Use Close (or another icon) when the action is not a deletion.
   */
  confirmIcon?: ReactNode;
  confirmDisabled?: boolean;
  /**
   * API / mutation in flight on the confirm action.
   * Shows spinner + pending label; blocks cancel, close, and re-submit.
   */
  confirmPending?: boolean;
  /** Label while pending. Defaults to `confirmLabel`. */
  confirmPendingLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
};

/**
 * Stacked confirmation over another Dialog or page — branded replace for
 * `window.confirm`. Parent Dialog should set `trapFocus={false}` and typically
 * `closeDisabled` while this is open.
 *
 * Labels carry leading icons (DS action pattern: icon + text children).
 */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  cancelLabel,
  closeLabel = 'Fermer',
  danger = false,
  confirmIcon,
  confirmDisabled = false,
  confirmPending = false,
  confirmPendingLabel,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const busy = confirmPending;
  const confirmLocked = confirmDisabled || busy;
  const leadingIcon =
    confirmIcon ??
    (danger ? <TrashIcon size="sm" /> : <CheckIcon size="sm" />);

  return (
    <Dialog
      open={open}
      onClose={onCancel}
      title={title}
      size="sm"
      closeLabel={closeLabel}
      closeDisabled={busy}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={busy}
            onClick={onCancel}
          >
            <CloseIcon size="sm" />
            {cancelLabel}
          </button>
          <button
            type="button"
            className={
              danger ? 'ds-btn ds-btn--destructive' : 'ds-btn ds-btn--primary'
            }
            disabled={confirmLocked}
            onClick={onConfirm}
          >
            {busy ? (
              <>
                <span className="ds-spinner" aria-hidden="true" />
                {confirmPendingLabel ?? confirmLabel}
              </>
            ) : (
              <>
                {leadingIcon}
                {confirmLabel}
              </>
            )}
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
  );
}
