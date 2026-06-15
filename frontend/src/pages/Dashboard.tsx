import { useEffect } from 'react'
import { useDashboard } from '@/hooks/useDashboard'
import { useDashboardLayout } from '@/hooks/useDashboardLayout'
import { useModuleDashboard } from '@/hooks/useModuleDashboard'
import { useDashboardExtras } from '@/hooks/useDashboardExtras'
import { useAuth } from '@/contexts/AuthContext'
import { useConfiguracaoEmpresa } from '@/hooks/useConfiguracaoEmpresa'
import { groupWidgets, renderWidget } from '@/components/dashboard/widgetRegistry'
import type { WidgetId } from '@/types/dashboard'
import type { DashboardResponse, ModulosDashboardResponse, DashboardExtrasResponse } from '@/types/dashboard'

function KpiGrid({ ids, data, modulos, extras }: {
  ids: WidgetId[]
  data: DashboardResponse
  modulos: ModulosDashboardResponse | null
  extras: DashboardExtrasResponse | null
}) {
  if (!ids.length) return null
  const cols =
    ids.length === 1 ? 'grid-cols-1 max-w-xs' :
    ids.length === 2 ? 'grid-cols-2' :
    ids.length <= 4 ? 'grid-cols-2 sm:grid-cols-4' :
    'grid-cols-2 sm:grid-cols-3 lg:grid-cols-4'
  return (
    <div className={`grid gap-3 ${cols}`}>
      {ids.map(id => (
        <div key={id}>{renderWidget(id, data, modulos, extras)}</div>
      ))}
    </div>
  )
}

function ChartGrid({ ids, data, modulos, extras }: {
  ids: WidgetId[]
  data: DashboardResponse
  modulos: ModulosDashboardResponse | null
  extras: DashboardExtrasResponse | null
}) {
  if (!ids.length) return null
  return (
    <div className={`grid gap-4 ${ids.length === 1 ? 'grid-cols-1' : 'md:grid-cols-2'}`}>
      {ids.map(id => (
        <div key={id}>{renderWidget(id, data, modulos, extras)}</div>
      ))}
    </div>
  )
}

export default function Dashboard() {
  const { data, loading, error, load } = useDashboard()
  const { widgets, load: loadLayout } = useDashboardLayout()
  const { data: modulos, load: loadModulos } = useModuleDashboard()
  const { data: extras, load: loadExtras } = useDashboardExtras()
  const { userName } = useAuth()
  const { config, obter } = useConfiguracaoEmpresa()

  useEffect(() => { void load() }, [load])
  useEffect(() => { void loadLayout() }, [loadLayout])
  useEffect(() => { void loadModulos() }, [loadModulos])
  useEffect(() => { void loadExtras() }, [loadExtras])
  useEffect(() => { void obter() }, [obter])

  if (loading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-muted-foreground">Carregando dashboard...</p>
      </div>
    )
  }

  if (error || !data) {
    return (
      <div className="flex h-64 items-center justify-center flex-col gap-2">
        <p className="text-destructive font-medium">Erro ao carregar dashboard</p>
        <p className="text-sm text-muted-foreground">{error ?? 'Sem dados'}</p>
        <button onClick={() => void load()} className="text-sm text-primary underline">Tentar novamente</button>
      </div>
    )
  }

  const sections = groupWidgets(widgets)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h1 className="text-2xl font-bold">
            {config?.nomeFantasia ?? config?.razaoSocial ?? 'Dashboard'}
          </h1>
          {userName && (
            <p className="text-sm text-muted-foreground mt-0.5">Olá, {userName}</p>
          )}
        </div>
        <p className="text-sm text-muted-foreground pt-1">
          {new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })}
        </p>
      </div>

      {/* Seções */}
      {sections.map((section) => {
        const hasContent = section.kpis.length || section.charts.length || section.singles.length
        if (!hasContent) return null

        return (
          <div key={section.label} className="rounded-xl border bg-muted/20 p-3 sm:p-5 space-y-4">
            <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">
              {section.label}
            </p>

            <KpiGrid ids={section.kpis} data={data} modulos={modulos} extras={extras} />
            <ChartGrid ids={section.charts} data={data} modulos={modulos} extras={extras} />

            {section.singles.map(id => {
              const rendered = renderWidget(id, data, modulos, extras)
              if (!rendered) return null
              return <div key={id}>{rendered}</div>
            })}
          </div>
        )
      })}
    </div>
  )
}
