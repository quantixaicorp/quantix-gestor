import { useEffect } from 'react'
import { ToastProvider, ToastViewport, Toast, ToastClose, ToastDescription } from './toast'
import { useToastStore, registerToastHandler } from '@/hooks/useToast'

export function Toaster() {
  const { toasts, addToast, removeToast } = useToastStore()

  useEffect(() => {
    registerToastHandler(addToast)
  }, [addToast])

  return (
    <ToastProvider swipeDirection="right">
      {toasts.map(t => (
        <Toast key={t.id} variant={t.variant} onOpenChange={open => { if (!open) removeToast(t.id) }}>
          <ToastDescription>{t.message}</ToastDescription>
          <ToastClose />
        </Toast>
      ))}
      <ToastViewport />
    </ToastProvider>
  )
}
