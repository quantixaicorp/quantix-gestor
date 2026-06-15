export interface KpiItem {
  label: string
  value: string
  color?: string
}

export function KpiRow({ items }: { items: KpiItem[] }) {
  return (
    <div className="rounded-xl border bg-card p-4">
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {items.map(k => (
          <div key={k.label} className="rounded-lg border bg-background p-3">
            <p className="text-xs text-muted-foreground">{k.label}</p>
            <p className={`text-xl font-bold mt-1 ${k.color ?? ''}`}>{k.value}</p>
          </div>
        ))}
      </div>
    </div>
  )
}
