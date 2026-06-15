export interface AvailableModule {
  slug: string
  label: string
}

// Source of truth for all sidebar groups that can be toggled per company.
// Slugs here must match module slugs registered in quantix-admin.
// Dashboard (Geral) is always visible and has no slug.
export const AVAILABLE_MODULES: AvailableModule[] = [
  { slug: 'vendas',        label: 'Vendas' },
  { slug: 'agenda',        label: 'Agenda' },
  { slug: 'estoque',       label: 'Estoque' },
  { slug: 'financeiro',    label: 'Financeiro' },
  { slug: 'clientes',      label: 'Clientes / Relatórios' },
  { slug: 'compras',       label: 'Compras' },
  { slug: 'fiscal',        label: 'Fiscal' },
  { slug: 'contratos',     label: 'Contratos' },
  { slug: 'configuracoes', label: 'Configurações' },
  { slug: 'automacao',     label: 'Automação' },
  { slug: 'assinaturas',   label: 'Assinaturas' },
]
