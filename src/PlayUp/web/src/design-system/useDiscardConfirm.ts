import { useCallback, useState } from 'react'

/**
 * Dirty-dialog discard confirm — shared by identity / add dialogs.
 * When discard is open, parent Dialog should set trapFocus={false} and disable close.
 */
export function useDiscardConfirm(isDirty: boolean, onDiscard: () => void) {
  const [discardOpen, setDiscardOpen] = useState(false)

  const requestClose = useCallback(
    (busy?: boolean) => {
      if (busy) {
        return
      }
      if (isDirty) {
        setDiscardOpen(true)
        return
      }
      onDiscard()
    },
    [isDirty, onDiscard],
  )

  const cancelDiscard = useCallback(() => {
    setDiscardOpen(false)
  }, [])

  const confirmDiscard = useCallback(() => {
    setDiscardOpen(false)
    onDiscard()
  }, [onDiscard])

  const resetDiscard = useCallback(() => {
    setDiscardOpen(false)
  }, [])

  return {
    discardOpen,
    requestClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  }
}
