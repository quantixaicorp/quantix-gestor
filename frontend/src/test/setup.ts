import '@testing-library/jest-dom'

// Node.js 26 defines a native `localStorage` getter that returns undefined
// unless --localstorage-file is passed. Override it with the jsdom-provided
// Storage so vitest tests can use localStorage normally.
if (typeof localStorage === 'undefined' || localStorage === null) {
  const storage: Record<string, string> = {}
  Object.defineProperty(globalThis, 'localStorage', {
    value: {
      getItem: (key: string) => storage[key] ?? null,
      setItem: (key: string, value: string) => { storage[key] = String(value) },
      removeItem: (key: string) => { delete storage[key] },
      clear: () => { Object.keys(storage).forEach(k => delete storage[k]) },
      get length() { return Object.keys(storage).length },
      key: (index: number) => Object.keys(storage)[index] ?? null,
    },
    configurable: true,
    writable: true,
  })
}
