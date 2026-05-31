import { useState, useCallback, createElement } from 'react'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import type { ConfirmDialogProps } from '@/components/ui/confirm-dialog'

type ConfirmOptions = Omit<ConfirmDialogProps, 'open' | 'onConfirm' | 'onCancel'>

export function useConfirm() {
  const [state, setState] = useState<{
    open: boolean
    options: ConfirmOptions
    resolve: (value: boolean) => void
  } | null>(null)

  const confirm = useCallback((options: ConfirmOptions): Promise<boolean> => {
    return new Promise(resolve => {
      setState({ open: true, options, resolve })
    })
  }, [])

  const handleConfirm = useCallback(() => {
    state?.resolve(true)
    setState(null)
  }, [state])

  const handleCancel = useCallback(() => {
    state?.resolve(false)
    setState(null)
  }, [state])

  const ConfirmDialogNode = state
    ? createElement(ConfirmDialog, {
        open: state.open,
        ...state.options,
        onConfirm: handleConfirm,
        onCancel: handleCancel,
      })
    : null

  return { confirm, ConfirmDialogNode }
}
