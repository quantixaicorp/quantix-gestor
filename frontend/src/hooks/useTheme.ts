import { useState, useEffect } from 'react'

export type Theme = 'light' | 'dark' | 'system'

function getSystemTheme(): 'light' | 'dark' {
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function applyTheme(theme: Theme) {
  const resolved = theme === 'system' ? getSystemTheme() : theme
  document.documentElement.classList.toggle('dark', resolved === 'dark')
}

export function useTheme() {
  const [theme, setThemeState] = useState<Theme>(() => {
    try {
      return (localStorage.getItem('theme') as Theme) ?? 'system'
    } catch {
      return 'system'
    }
  })

  useEffect(() => {
    applyTheme(theme)
    try { localStorage.setItem('theme', theme) } catch { /* ignore */ }
  }, [theme])

  useEffect(() => {
    if (theme !== 'system') return
    const mq = window.matchMedia('(prefers-color-scheme: dark)')
    const handler = () => applyTheme('system')
    mq.addEventListener('change', handler)
    return () => mq.removeEventListener('change', handler)
  }, [theme])

  const setTheme = (t: Theme) => setThemeState(t)
  const toggleTheme = () =>
    setThemeState((prev) => {
      const resolved = prev === 'system' ? getSystemTheme() : prev
      return resolved === 'dark' ? 'light' : 'dark'
    })

  const resolved = theme === 'system' ? getSystemTheme() : theme

  return { theme, resolved, setTheme, toggleTheme }
}
