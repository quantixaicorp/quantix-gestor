import { useState, useCallback } from 'react'

export type ToastVariant = 'default' | 'success' | 'destructive'

export interface ToastItem {
  id: string
  message: string
  variant: ToastVariant
}

let externalAddToast: ((message: string, variant: ToastVariant) => void) | null = null

export function registerToastHandler(handler: (message: string, variant: ToastVariant) => void) {
  externalAddToast = handler
}

export function useToastStore() {
  const [toasts, setToasts] = useState<ToastItem[]>([])

  const addToast = useCallback((message: string, variant: ToastVariant = 'default') => {
    const id = Math.random().toString(36).slice(2)
    setToasts(prev => [...prev, { id, message, variant }])
    setTimeout(() => {
      setToasts(prev => prev.filter(t => t.id !== id))
    }, 4000)
  }, [])

  const removeToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id))
  }, [])

  return { toasts, addToast, removeToast }
}

// Global toast functions — work after <Toaster /> mounts and registers the handler
export const toast = (message: string) => externalAddToast?.(message, 'default')
toast.success = (message: string) => externalAddToast?.(message, 'success')
toast.error = (message: string) => externalAddToast?.(message, 'destructive')
