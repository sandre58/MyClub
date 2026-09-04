import { useEffect, useRef } from 'react'
import { pushDismissLayer } from './dismissStack'

export type UseDismissLayerOptions = {
  /**
   * When false, the layer stays registered but is skipped when resolving
   * Escape. Prefer leaving enabled and no-opping `onDismiss` when Escape
   * must be consumed without side effects (e.g. Dialog `closeDisabled`).
   */
  enabled?: boolean
}

/**
 * Registers an Escape-dismissible layer while `active`.
 * Last registered active layer wins (LIFO). Single shared document listener.
 */
export function useDismissLayer(
  active: boolean,
  onDismiss: () => void,
  options?: UseDismissLayerOptions,
) {
  const onDismissRef = useRef(onDismiss)
  onDismissRef.current = onDismiss

  const enabled = options?.enabled !== false
  const handleRef = useRef<ReturnType<typeof pushDismissLayer> | null>(null)

  // Keep enabled in sync without re-pushing (preserves LIFO order).
  if (handleRef.current) {
    handleRef.current.setEnabled(enabled)
  }

  useEffect(() => {
    if (!active) {
      return
    }

    const handle = pushDismissLayer(() => {
      onDismissRef.current()
    }, { enabled })
    handleRef.current = handle

    return () => {
      handle.unregister()
      if (handleRef.current === handle) {
        handleRef.current = null
      }
    }
    // `enabled` is synced via setEnabled above — re-pushing would reorder LIFO.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- active only
  }, [active])
}
