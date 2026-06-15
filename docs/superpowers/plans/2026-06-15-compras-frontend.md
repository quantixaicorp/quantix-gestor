# Módulo de Compras — Frontend — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar o frontend completo do módulo de Compras: types, hooks, páginas, componentes, navegação e relatórios.

**Architecture:** Segue o padrão estabelecido no projeto — hooks para data fetching, pages como containers, components como unidades reutilizáveis, types para DTOs. O formulário de nova compra usa steps locais (useState) sem biblioteca de wizard.

**Tech Stack:** React 18, TypeScript, Vite, React Router DOM, Tailwind CSS, Lucide Icons, Recharts (já usado no dashboard existente).

**Pré-requisito:** Backend do plano `2026-06-15-compras-backend.md` implementado e rodando em `http://localhost:5002`.

---

## Mapa de Arquivos

### Criar
- `src/types/compras.ts`
- `src/types/parcelamentos.ts`
- `src/hooks/useCompras.ts`
- `src/hooks/usePedidosCompra.ts`
- `src/hooks/useParcelamentos.ts`
- `src/hooks/useComprasDashboard.ts`
- `src/components/compras/ItensCompraTable.tsx`
- `src/components/compras/PreviewParcelas.tsx`
- `src/pages/compras/Compras.tsx`
- `src/pages/compras/NovaCompra.tsx`
- `src/pages/compras/DetalheCompra.tsx`
- `src/pages/compras/PedidosCompra.tsx`
- `src/pages/compras/NovoPedidoCompra.tsx`
- `src/pages/compras/DetalhePedidoCompra.tsx`
- `src/pages/compras/DashboardCompras.tsx`
- `src/components/relatorios/AbaCompras.tsx`

### Modificar
- `src/types/fornecedores.ts` — adicionar novos campos
- `src/components/fornecedores/FornecedorForm.tsx` — adicionar campos novos
- `src/router/index.tsx` — adicionar 7 rotas novas
- `src/components/layout/Sidebar.tsx` — expandir grupo Compras
- `src/pages/relatorios/Relatorios.tsx` — adicionar aba Compras

---

## Task 1: Types

**Files:**
- Modify: `src/types/fornecedores.ts`
- Create: `src/types/compras.ts`
- Create: `src/types/parcelamentos.ts`

- [ ] **Step 1: Atualizar src/types/fornecedores.ts**

Substituir `FornecedorResponse` e `CreateFornecedorRequest` e `UpdateFornecedorRequest` pelos tipos abaixo (manter o restante do arquivo intacto):

```typescript
export interface FornecedorResponse {
  id: string
  nome: string
  razaoSocial?: string
  nomeFantasia?: string
  cnpjCpf?: string
  inscricaoEstadual?: string
  telefone?: string
  whatsapp?: string
  email?: string
  logradouro?: string
  cidade?: string
  uf?: string
  cep?: string
  contato?: string
  observacoes?: string
  status: 'Ativo' | 'Inativo'
  dataCadastro: string
}

export interface CreateFornecedorRequest {
  nome: string
  razaoSocial?: string
  nomeFantasia?: string
  cnpjCpf?: string
  inscricaoEstadual?: string
  telefone?: string
  whatsapp?: string
  email?: string
  logradouro?: string
  cidade?: string
  uf?: string
  cep?: string
  contato?: string
  observacoes?: string
}

export interface UpdateFornecedorRequest extends CreateFornecedorRequest {
  status?: string
}
```

- [ ] **Step 2: Criar src/types/compras.ts**

```typescript
export type StatusCompra = 'Rascunho' | 'Confirmada' | 'Cancelada'
export type StatusPedidoCompra =
  | 'Rascunho' | 'AguardandoAprovacao' | 'Aprovado'
  | 'RecebidoParcialmente' | 'RecebidoTotalmente' | 'Cancelado'
export type DestinoCompra = 'EstoqueParaVenda' | 'ConsumoInterno' | 'AtivoImobilizado'

export interface ItemCompraRequest {
  produtoId?: string
  descricao: string
  destinoCompra: DestinoCompra
  quantidade: number
  valorUnitario: number
  desconto: number
  freteRateado: number
  impostos: number
  categoriaFinanceira?: string
  centroCusto?: string
}

export interface ParcelaPersonalizadaRequest {
  numero: number
  dataVencimento: string
}

export interface CreateCompraRequest {
  fornecedorId: string
  data: string
  tipoCompra: string
  numeroNota?: string
  condicaoPagamento: string
  formaPagamento: string
  qtdParcelas?: number
  parcelasPersonalizadas?: ParcelaPersonalizadaRequest[]
  pedidoCompraId?: string
  observacoes?: string
  itens: ItemCompraRequest[]
}

export interface UpdateCompraRequest extends CreateCompraRequest {}

export interface ItemCompraResponse {
  id: string
  produtoId?: string
  descricao: string
  destinoCompra: string
  quantidade: number
  valorUnitario: number
  desconto: number
  freteRateado: number
  impostos: number
  valorTotal: number
  categoriaFinanceira?: string
  centroCusto?: string
}

export interface CompraResponse {
  id: string
  numero: number
  data: string
  fornecedorId: string
  fornecedorNome: string
  pedidoCompraId?: string
  tipoCompra: string
  numeroNota?: string
  condicaoPagamento: string
  formaPagamento: string
  status: StatusCompra
  valorTotal: number
  observacoes?: string
  criadaEm: string
  itens: ItemCompraResponse[]
  parcelamentoId?: string
}

export interface CompraResumoResponse {
  totalMes: number
  qtdComprasMes: number
  contasAPagarGeradas: number
  fornecedoresAtivos: number
}

export interface ItemPedidoCompraRequest {
  produtoId?: string
  descricao: string
  quantidade: number
  valorEstimado: number
}

export interface CreatePedidoCompraRequest {
  fornecedorId: string
  data: string
  observacoes?: string
  itens: ItemPedidoCompraRequest[]
}

export interface UpdatePedidoCompraRequest extends CreatePedidoCompraRequest {}

export interface ItemPedidoCompraResponse {
  id: string
  produtoId?: string
  descricao: string
  quantidade: number
  valorEstimado: number
}

export interface PedidoCompraResponse {
  id: string
  numero: number
  data: string
  fornecedorId: string
  fornecedorNome: string
  status: StatusPedidoCompra
  valorEstimado: number
  observacoes?: string
  criadoEm: string
  itens: ItemPedidoCompraResponse[]
}
```

- [ ] **Step 3: Criar src/types/parcelamentos.ts**

```typescript
export type StatusParcelamento = 'EmAberto' | 'PagoParcialmente' | 'PagoTotal' | 'Cancelado'

export interface ParcelaResponse {
  id: string
  numeroParcela: number
  valor: number
  dataVencimento: string
  dataPagamento?: string
  status: string
}

export interface ParcelamentoResponse {
  id: string
  compraId?: string
  descricao: string
  valorTotal: number
  qtdParcelas: number
  status: StatusParcelamento
  parcelas: ParcelaResponse[]
}
```

- [ ] **Step 4: Build TypeScript**

```bash
cd gestorai-erp/frontend && npx tsc --noEmit
```
Esperado: sem erros críticos (warnings em código existente são aceitáveis).

- [ ] **Step 5: Commit**

```bash
git add gestorai-erp/frontend/src/types/
git commit -m "feat(compras): add frontend types for compras, pedidos and parcelamentos"
```

---

## Task 2: Hooks

**Files:**
- Create: `src/hooks/useCompras.ts`
- Create: `src/hooks/usePedidosCompra.ts`
- Create: `src/hooks/useParcelamentos.ts`
- Create: `src/hooks/useComprasDashboard.ts`

- [ ] **Step 1: Criar src/hooks/useCompras.ts**

```typescript
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  CompraResponse, CompraResumoResponse,
  CreateCompraRequest, UpdateCompraRequest,
} from '@/types/compras'

export function useCompras() {
  const [compras, setCompras] = useState<CompraResponse[]>([])
  const [resumo, setResumo] = useState<CompraResumoResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (params?: {
    status?: string; fornecedorId?: string; de?: string; ate?: string
  }) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams(
        Object.entries(params ?? {}).filter(([, v]) => v) as [string, string][]
      ).toString()
      const data = await api.get<CompraResponse[]>(`/api/compras${qs ? `?${qs}` : ''}`)
      setCompras(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar compras')
    } finally {
      setLoading(false)
    }
  }, [])

  const getResumo = useCallback(async () => {
    const data = await api.get<CompraResumoResponse>('/api/compras/resumo')
    setResumo(data)
  }, [])

  const get = useCallback(
    (id: string) => api.get<CompraResponse>(`/api/compras/${id}`),
    []
  )

  const create = useCallback(async (req: CreateCompraRequest) => {
    const result = await api.post<CompraResponse>('/api/compras', req)
    setCompras(prev => [result, ...prev])
    return result
  }, [])

  const update = useCallback(async (id: string, req: UpdateCompraRequest) => {
    const result = await api.put<CompraResponse>(`/api/compras/${id}`, req)
    setCompras(prev => prev.map(c => c.id === id ? result : c))
    return result
  }, [])

  const confirmar = useCallback(async (id: string) => {
    const result = await api.post<CompraResponse>(`/api/compras/${id}/confirmar`, {})
    setCompras(prev => prev.map(c => c.id === id ? result : c))
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<CompraResponse>(`/api/compras/${id}/cancelar`, {})
    setCompras(prev => prev.map(c => c.id === id ? result : c))
    return result
  }, [])

  const remove = useCallback(async (id: string) => {
    await api.delete(`/api/compras/${id}`)
    setCompras(prev => prev.filter(c => c.id !== id))
  }, [])

  return { compras, resumo, loading, error, list, getResumo, get, create, update, confirmar, cancelar, remove }
}
```

- [ ] **Step 2: Criar src/hooks/usePedidosCompra.ts**

```typescript
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  PedidoCompraResponse, CreatePedidoCompraRequest,
  UpdatePedidoCompraRequest, CompraResponse,
} from '@/types/compras'

export function usePedidosCompra() {
  const [pedidos, setPedidos] = useState<PedidoCompraResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (params?: { status?: string; fornecedorId?: string }) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams(
        Object.entries(params ?? {}).filter(([, v]) => v) as [string, string][]
      ).toString()
      const data = await api.get<PedidoCompraResponse[]>(`/api/pedidos-compra${qs ? `?${qs}` : ''}`)
      setPedidos(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar pedidos')
    } finally {
      setLoading(false)
    }
  }, [])

  const get = useCallback(
    (id: string) => api.get<PedidoCompraResponse>(`/api/pedidos-compra/${id}`),
    []
  )

  const create = useCallback(async (req: CreatePedidoCompraRequest) => {
    const result = await api.post<PedidoCompraResponse>('/api/pedidos-compra', req)
    setPedidos(prev => [result, ...prev])
    return result
  }, [])

  const update = useCallback(async (id: string, req: UpdatePedidoCompraRequest) => {
    const result = await api.put<PedidoCompraResponse>(`/api/pedidos-compra/${id}`, req)
    setPedidos(prev => prev.map(p => p.id === id ? result : p))
    return result
  }, [])

  const aprovar = useCallback(async (id: string) => {
    const result = await api.post<PedidoCompraResponse>(`/api/pedidos-compra/${id}/aprovar`, {})
    setPedidos(prev => prev.map(p => p.id === id ? result : p))
    return result
  }, [])

  const converter = useCallback(
    (id: string) => api.post<CompraResponse>(`/api/pedidos-compra/${id}/converter`, {}),
    []
  )

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<PedidoCompraResponse>(`/api/pedidos-compra/${id}/cancelar`, {})
    setPedidos(prev => prev.map(p => p.id === id ? result : p))
    return result
  }, [])

  return { pedidos, loading, error, list, get, create, update, aprovar, converter, cancelar }
}
```

- [ ] **Step 3: Criar src/hooks/useParcelamentos.ts**

```typescript
import { useCallback } from 'react'
import { api } from '@/services/api'
import type { ParcelamentoResponse } from '@/types/parcelamentos'

export function useParcelamentos() {
  const get = useCallback(
    (id: string) => api.get<ParcelamentoResponse>(`/api/parcelamentos/${id}`),
    []
  )

  const listByCompra = useCallback(
    (compraId: string) =>
      api.get<ParcelamentoResponse[]>(`/api/parcelamentos?compraId=${compraId}`),
    []
  )

  return { get, listByCompra }
}
```

- [ ] **Step 4: Criar src/hooks/useComprasDashboard.ts**

```typescript
import { useState, useCallback } from 'react'
import { api } from '@/services/api'

export interface EvolucaoMensalCompraItem { mes: string; total: number; qtdCompras: number }
export interface ComprasPorFornecedorItem { fornecedor: string; total: number }
export interface TopProdutoCompradoItem { produto: string; quantidade: number }

export interface ComprasDashboardData {
  totalMes: number
  totalAno: number
  ticketMedio: number
  qtdCompras: number
  fornecedoresAtivos: number
  evolucaoMensal: EvolucaoMensalCompraItem[]
  porFornecedor: ComprasPorFornecedorItem[]
  topProdutos: TopProdutoCompradoItem[]
}

export function useComprasDashboard() {
  const [data, setData] = useState<ComprasDashboardData | null>(null)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async (de: string, ate: string) => {
    setLoading(true)
    try {
      const result = await api.get<ComprasDashboardData>(
        `/api/compras/dashboard?de=${de}&ate=${ate}`
      )
      setData(result)
    } finally {
      setLoading(false)
    }
  }, [])

  return { data, loading, load }
}
```

- [ ] **Step 5: Build TypeScript**

```bash
cd gestorai-erp/frontend && npx tsc --noEmit
```
Esperado: sem erros nos novos arquivos.

- [ ] **Step 6: Commit**

```bash
git add gestorai-erp/frontend/src/hooks/useCompras.ts \
        gestorai-erp/frontend/src/hooks/usePedidosCompra.ts \
        gestorai-erp/frontend/src/hooks/useParcelamentos.ts \
        gestorai-erp/frontend/src/hooks/useComprasDashboard.ts
git commit -m "feat(compras): add frontend hooks"
```

---

## Task 3: Router + Sidebar

**Files:**
- Modify: `src/router/index.tsx`
- Modify: `src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Adicionar imports no router/index.tsx**

Adicionar após a última linha de import existente:

```typescript
import Compras from '@/pages/compras/Compras'
import NovaCompra from '@/pages/compras/NovaCompra'
import DetalheCompra from '@/pages/compras/DetalheCompra'
import PedidosCompra from '@/pages/compras/PedidosCompra'
import NovoPedidoCompra from '@/pages/compras/NovoPedidoCompra'
import DetalhePedidoCompra from '@/pages/compras/DetalhePedidoCompra'
import DashboardCompras from '@/pages/compras/DashboardCompras'
```

- [ ] **Step 2: Adicionar rotas no router/index.tsx**

Dentro do array `children`, após `{ path: '/fornecedores', element: <Fornecedores /> },`, adicionar:

```typescript
      { path: '/compras', element: <Compras /> },
      { path: '/compras/nova', element: <NovaCompra /> },
      { path: '/compras/:id', element: <DetalheCompra /> },
      { path: '/compras/pedidos', element: <PedidosCompra /> },
      { path: '/compras/pedidos/novo', element: <NovoPedidoCompra /> },
      { path: '/compras/pedidos/:id', element: <DetalhePedidoCompra /> },
      { path: '/compras/dashboard', element: <DashboardCompras /> },
```

- [ ] **Step 3: Expandir grupo Compras no Sidebar.tsx**

Localizar o bloco:
```typescript
  {
    label: 'Compras',
    moduleSlug: 'compras',
    items: [
      { icon: Truck, label: 'Fornecedores', path: '/fornecedores' },
    ],
  },
```

Substituir por:
```typescript
  {
    label: 'Compras',
    moduleSlug: 'compras',
    items: [
      { icon: Truck,         label: 'Fornecedores', path: '/fornecedores' },
      { icon: ClipboardList, label: 'Pedidos',       path: '/compras/pedidos' },
      { icon: ShoppingCart,  label: 'Compras',       path: '/compras' },
      { icon: BarChart3,     label: 'Dashboard',     path: '/compras/dashboard' },
    ],
  },
```

Os ícones `ClipboardList`, `ShoppingCart` e `BarChart3` já estão importados no Sidebar.tsx.

- [ ] **Step 4: Build**

```bash
cd gestorai-erp/frontend && npx tsc --noEmit
```
Esperado: erros de módulo não encontrado para os pages ainda não criados — normal, serão corrigidos nas próximas tasks.

- [ ] **Step 5: Commit**

```bash
git add gestorai-erp/frontend/src/router/index.tsx \
        gestorai-erp/frontend/src/components/layout/Sidebar.tsx
git commit -m "feat(compras): add routes and sidebar navigation for compras module"
```

---

## Task 4: Componentes Compartilhados

**Files:**
- Create: `src/components/compras/ItensCompraTable.tsx`
- Create: `src/components/compras/PreviewParcelas.tsx`

- [ ] **Step 1: Criar ItensCompraTable.tsx**

```tsx
import { Trash2, Plus } from 'lucide-react'
import type { ItemCompraRequest, DestinoCompra } from '@/types/compras'

const DESTINOS: { value: DestinoCompra; label: string }[] = [
  { value: 'EstoqueParaVenda', label: 'Estoque para Venda' },
  { value: 'ConsumoInterno', label: 'Consumo Interno' },
  { value: 'AtivoImobilizado', label: 'Ativo Imobilizado' },
]

interface Props {
  itens: ItemCompraRequest[]
  onChange: (itens: ItemCompraRequest[]) => void
  readonly?: boolean
}

const ITEM_VAZIO: ItemCompraRequest = {
  descricao: '', destinoCompra: 'EstoqueParaVenda',
  quantidade: 1, valorUnitario: 0, desconto: 0,
  freteRateado: 0, impostos: 0,
}

export function ItensCompraTable({ itens, onChange, readonly }: Props) {
  const addItem = () => onChange([...itens, { ...ITEM_VAZIO }])

  const removeItem = (i: number) => onChange(itens.filter((_, idx) => idx !== i))

  const updateItem = (i: number, field: keyof ItemCompraRequest, value: unknown) => {
    const updated = [...itens]
    updated[i] = { ...updated[i], [field]: value }
    onChange(updated)
  }

  const totalItem = (item: ItemCompraRequest) =>
    item.quantidade * item.valorUnitario - item.desconto + item.freteRateado + item.impostos

  const fmt = (v: number) =>
    v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  return (
    <div className="space-y-2">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-muted-foreground border-b">
              <th className="py-2 pr-2">Descrição</th>
              <th className="py-2 pr-2">Destino</th>
              <th className="py-2 pr-2 text-right">Qtd</th>
              <th className="py-2 pr-2 text-right">Valor Unit.</th>
              <th className="py-2 pr-2 text-right">Desconto</th>
              <th className="py-2 pr-2 text-right">Frete</th>
              <th className="py-2 pr-2 text-right">Impostos</th>
              <th className="py-2 text-right">Total</th>
              {!readonly && <th />}
            </tr>
          </thead>
          <tbody>
            {itens.map((item, i) => (
              <tr key={i} className="border-b last:border-0">
                <td className="py-1 pr-2">
                  {readonly ? item.descricao : (
                    <input
                      className="border rounded px-2 py-1 w-full bg-background"
                      value={item.descricao}
                      onChange={e => updateItem(i, 'descricao', e.target.value)}
                      placeholder="Nome do item"
                    />
                  )}
                </td>
                <td className="py-1 pr-2">
                  {readonly ? DESTINOS.find(d => d.value === item.destinoCompra)?.label : (
                    <select
                      className="border rounded px-2 py-1 bg-background"
                      value={item.destinoCompra}
                      onChange={e => updateItem(i, 'destinoCompra', e.target.value as DestinoCompra)}
                    >
                      {DESTINOS.map(d => (
                        <option key={d.value} value={d.value}>{d.label}</option>
                      ))}
                    </select>
                  )}
                </td>
                {(['quantidade', 'valorUnitario', 'desconto', 'freteRateado', 'impostos'] as const).map(field => (
                  <td key={field} className="py-1 pr-2 text-right">
                    {readonly ? (item[field] as number).toLocaleString('pt-BR') : (
                      <input
                        type="number"
                        min={0}
                        step={field === 'quantidade' ? 1 : 0.01}
                        className="border rounded px-2 py-1 w-20 text-right bg-background"
                        value={item[field] as number}
                        onChange={e => updateItem(i, field, parseFloat(e.target.value) || 0)}
                      />
                    )}
                  </td>
                ))}
                <td className="py-1 text-right font-medium">{fmt(totalItem(item))}</td>
                {!readonly && (
                  <td className="py-1 pl-2">
                    <button onClick={() => removeItem(i)}
                      className="text-destructive hover:text-destructive/80">
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <td colSpan={readonly ? 7 : 7} className="pt-2 text-right font-semibold">
                Total:
              </td>
              <td className="pt-2 text-right font-bold">
                {fmt(itens.reduce((acc, item) => acc + totalItem(item), 0))}
              </td>
              {!readonly && <td />}
            </tr>
          </tfoot>
        </table>
      </div>
      {!readonly && (
        <button onClick={addItem}
          className="flex items-center gap-1 text-sm text-primary hover:underline">
          <Plus className="h-4 w-4" /> Adicionar item
        </button>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Criar PreviewParcelas.tsx**

```tsx
import type { ParcelaPersonalizadaRequest } from '@/types/compras'

interface Props {
  condicao: string
  dataBase: string
  qtdParcelas: number
  valorTotal: number
  personalizadas: ParcelaPersonalizadaRequest[]
  onPersonalizadasChange?: (p: ParcelaPersonalizadaRequest[]) => void
}

function calcularVencimentos(condicao: string, dataBase: string, qtd: number): string[] {
  const base = new Date(dataBase)
  if (!dataBase) return []
  switch (condicao) {
    case 'AVista': return [base.toISOString().slice(0, 10)]
    case '30d': return [addDays(base, 30)]
    case '30_60_90d': return [addDays(base, 30), addDays(base, 60), addDays(base, 90)]
    case 'Parcelado': return Array.from({ length: qtd }, (_, i) => addMonths(base, i + 1))
    default: return []
  }
}

function addDays(d: Date, n: number) {
  const r = new Date(d); r.setDate(r.getDate() + n)
  return r.toISOString().slice(0, 10)
}

function addMonths(d: Date, n: number) {
  const r = new Date(d); r.setMonth(r.getMonth() + n)
  return r.toISOString().slice(0, 10)
}

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export function PreviewParcelas({ condicao, dataBase, qtdParcelas, valorTotal, personalizadas, onPersonalizadasChange }: Props) {
  const isPersonalizado = condicao === 'Personalizado'
  const vencimentos = isPersonalizado
    ? personalizadas.map(p => p.dataVencimento)
    : calcularVencimentos(condicao, dataBase, qtdParcelas)

  if (vencimentos.length === 0 && !isPersonalizado) return null

  const n = isPersonalizado ? personalizadas.length : vencimentos.length
  const valorParcela = n > 0 ? valorTotal / n : 0

  return (
    <div className="rounded-lg border p-3 bg-muted/30 space-y-2">
      <p className="text-sm font-medium text-muted-foreground">Preview das parcelas</p>
      <div className="space-y-1">
        {(isPersonalizado ? personalizadas.map(p => p.dataVencimento) : vencimentos).map((v, i) => (
          <div key={i} className="flex items-center gap-2 text-sm">
            <span className="w-6 text-muted-foreground">{i + 1}.</span>
            {isPersonalizado && onPersonalizadasChange ? (
              <input
                type="date"
                className="border rounded px-2 py-0.5 bg-background text-sm"
                value={v}
                onChange={e => {
                  const updated = [...personalizadas]
                  updated[i] = { numero: i + 1, dataVencimento: e.target.value }
                  onPersonalizadasChange(updated)
                }}
              />
            ) : (
              <span>{new Date(v + 'T00:00:00').toLocaleDateString('pt-BR')}</span>
            )}
            <span className="ml-auto font-medium">{fmt(valorParcela)}</span>
          </div>
        ))}
      </div>
      {isPersonalizado && onPersonalizadasChange && (
        <button
          type="button"
          onClick={() => onPersonalizadasChange([
            ...personalizadas,
            { numero: personalizadas.length + 1, dataVencimento: '' }
          ])}
          className="text-xs text-primary hover:underline"
        >
          + Adicionar parcela
        </button>
      )}
    </div>
  )
}
```

- [ ] **Step 3: Commit**

```bash
git add gestorai-erp/frontend/src/components/compras/
git commit -m "feat(compras): add ItensCompraTable and PreviewParcelas components"
```

---

## Task 5: Página de Lista de Compras

**Files:**
- Create: `src/pages/compras/Compras.tsx`

- [ ] **Step 1: Criar Compras.tsx**

```tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, ShoppingCart, TrendingDown, Receipt } from 'lucide-react'
import { useCompras } from '@/hooks/useCompras'
import { KpiCard } from '@/components/dashboard/KpiCard'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'

const STATUS_LABELS: Record<string, string> = {
  Rascunho: 'Rascunho',
  Confirmada: 'Confirmada',
  Cancelada: 'Cancelada',
}

const STATUS_VARIANT: Record<string, 'default' | 'secondary' | 'destructive'> = {
  Rascunho: 'secondary',
  Confirmada: 'default',
  Cancelada: 'destructive',
}

export default function Compras() {
  const navigate = useNavigate()
  const { compras, resumo, loading, list, getResumo } = useCompras()
  const [filtroStatus, setFiltroStatus] = useState('')

  useEffect(() => {
    list({ status: filtroStatus || undefined })
    getResumo()
  }, [filtroStatus])

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  return (
    <div className="p-4 md:p-6 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Compras</h1>
        <Button onClick={() => navigate('/compras/nova')}>
          <Plus className="h-4 w-4 mr-2" /> Nova Compra
        </Button>
      </div>

      {resumo && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <KpiCard title="Total no Mês" value={fmt(resumo.totalMes)} icon={ShoppingCart} />
          <KpiCard title="Qtd de Compras" value={String(resumo.qtdComprasMes)} icon={Receipt} />
          <KpiCard title="Contas a Pagar" value={fmt(resumo.contasAPagarGeradas)} icon={TrendingDown} />
          <KpiCard title="Fornecedores Ativos" value={String(resumo.fornecedoresAtivos)} icon={ShoppingCart} />
        </div>
      )}

      <div className="flex gap-2">
        {['', 'Rascunho', 'Confirmada', 'Cancelada'].map(s => (
          <button
            key={s}
            onClick={() => setFiltroStatus(s)}
            className={`px-3 py-1 rounded-full text-sm border transition-colors ${
              filtroStatus === s
                ? 'bg-primary text-primary-foreground border-primary'
                : 'border-border hover:bg-muted'
            }`}
          >
            {s || 'Todos'}
          </button>
        ))}
      </div>

      <div className="rounded-lg border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left p-3">Nº</th>
              <th className="text-left p-3">Data</th>
              <th className="text-left p-3">Fornecedor</th>
              <th className="text-right p-3">Valor</th>
              <th className="text-center p-3">Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan={5} className="p-4 text-center text-muted-foreground">Carregando...</td></tr>
            )}
            {!loading && compras.length === 0 && (
              <tr><td colSpan={5} className="p-4 text-center text-muted-foreground">Nenhuma compra encontrada.</td></tr>
            )}
            {compras.map(c => (
              <tr
                key={c.id}
                className="border-t hover:bg-muted/30 cursor-pointer"
                onClick={() => navigate(`/compras/${c.id}`)}
              >
                <td className="p-3 font-mono">#{c.numero}</td>
                <td className="p-3">{new Date(c.data).toLocaleDateString('pt-BR')}</td>
                <td className="p-3">{c.fornecedorNome}</td>
                <td className="p-3 text-right font-medium">{fmt(c.valorTotal)}</td>
                <td className="p-3 text-center">
                  <Badge variant={STATUS_VARIANT[c.status]}>
                    {STATUS_LABELS[c.status]}
                  </Badge>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Testar no browser**

```bash
cd gestorai-erp/frontend && npm run dev
```
Navegar para `http://localhost:5173/compras`. Deve exibir a página com KPIs e tabela vazia.

- [ ] **Step 3: Commit**

```bash
git add gestorai-erp/frontend/src/pages/compras/Compras.tsx
git commit -m "feat(compras): add Compras list page"
```

---

## Task 6: Formulário Nova Compra (multi-step)

**Files:**
- Create: `src/pages/compras/NovaCompra.tsx`

- [ ] **Step 1: Criar NovaCompra.tsx**

```tsx
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronRight, ChevronLeft, Check } from 'lucide-react'
import { useCompras } from '@/hooks/useCompras'
import { useFornecedores } from '@/hooks/useFornecedores'
import { useEstoque } from '@/hooks/useEstoque'
import { ItensCompraTable } from '@/components/compras/ItensCompraTable'
import { PreviewParcelas } from '@/components/compras/PreviewParcelas'
import { Button } from '@/components/ui/button'
import { useToast } from '@/hooks/useToast'
import type { CreateCompraRequest, ItemCompraRequest, ParcelaPersonalizadaRequest } from '@/types/compras'

const STEPS = ['Cabeçalho', 'Itens', 'Pagamento', 'Revisão']

const CONDICOES = [
  { value: 'AVista', label: 'À Vista' },
  { value: '30d', label: '30 dias' },
  { value: '30_60_90d', label: '30/60/90 dias' },
  { value: 'Parcelado', label: 'Parcelado (Nx)' },
  { value: 'Personalizado', label: 'Personalizado' },
]

const FORMAS = [
  'Dinheiro', 'PIX', 'Boleto', 'CartaoCredito', 'CartaoDebito', 'Transferencia',
]

export default function NovaCompra() {
  const navigate = useNavigate()
  const { create, confirmar } = useCompras()
  const { fornecedores, list: listFornecedores } = useFornecedores()
  const { toast } = useToast()
  const [step, setStep] = useState(0)
  const [saving, setSaving] = useState(false)

  const [cabecalho, setCabecalho] = useState({
    fornecedorId: '', data: new Date().toISOString().slice(0, 10),
    tipoCompra: 'Mercadoria', numeroNota: '', observacoes: '',
  })
  const [itens, setItens] = useState<ItemCompraRequest[]>([])
  const [pagamento, setPagamento] = useState({
    condicaoPagamento: 'AVista', formaPagamento: 'PIX', qtdParcelas: 1,
  })
  const [personalizadas, setPersonalizadas] = useState<ParcelaPersonalizadaRequest[]>([])

  // carrega fornecedores ao montar
  useState(() => { listFornecedores() })

  const valorTotal = itens.reduce(
    (acc, i) => acc + i.quantidade * i.valorUnitario - i.desconto + i.freteRateado + i.impostos, 0
  )

  const buildRequest = (): CreateCompraRequest => ({
    ...cabecalho,
    ...pagamento,
    parcelasPersonalizadas: pagamento.condicaoPagamento === 'Personalizado' ? personalizadas : undefined,
    itens,
  })

  const handleSave = async (confirmarImediato: boolean) => {
    if (!cabecalho.fornecedorId) { toast({ title: 'Selecione um fornecedor', variant: 'destructive' }); return }
    if (itens.length === 0) { toast({ title: 'Adicione pelo menos um item', variant: 'destructive' }); return }
    setSaving(true)
    try {
      const compra = await create(buildRequest())
      if (confirmarImediato) await confirmar(compra.id)
      toast({ title: confirmarImediato ? 'Compra confirmada!' : 'Rascunho salvo!' })
      navigate(`/compras/${compra.id}`)
    } catch (e) {
      toast({ title: 'Erro ao salvar compra', variant: 'destructive' })
    } finally {
      setSaving(false)
    }
  }

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  return (
    <div className="p-4 md:p-6 max-w-4xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold">Nova Compra</h1>

      {/* Stepper */}
      <div className="flex items-center gap-1">
        {STEPS.map((s, i) => (
          <div key={s} className="flex items-center gap-1">
            <div className={`flex items-center justify-center w-7 h-7 rounded-full text-xs font-bold border-2 transition-colors ${
              i < step ? 'bg-primary border-primary text-primary-foreground'
                : i === step ? 'border-primary text-primary'
                  : 'border-border text-muted-foreground'
            }`}>
              {i < step ? <Check className="h-3 w-3" /> : i + 1}
            </div>
            <span className={`text-sm hidden sm:block ${i === step ? 'font-semibold' : 'text-muted-foreground'}`}>{s}</span>
            {i < STEPS.length - 1 && <div className="w-4 h-px bg-border mx-1" />}
          </div>
        ))}
      </div>

      {/* Step 0 — Cabeçalho */}
      {step === 0 && (
        <div className="grid gap-4">
          <div className="grid gap-2">
            <label className="text-sm font-medium">Fornecedor *</label>
            <select
              className="border rounded px-3 py-2 bg-background"
              value={cabecalho.fornecedorId}
              onChange={e => setCabecalho(p => ({ ...p, fornecedorId: e.target.value }))}
            >
              <option value="">Selecione...</option>
              {fornecedores.map(f => <option key={f.id} value={f.id}>{f.nome}</option>)}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <label className="text-sm font-medium">Data *</label>
              <input type="date" className="border rounded px-3 py-2 bg-background"
                value={cabecalho.data}
                onChange={e => setCabecalho(p => ({ ...p, data: e.target.value }))} />
            </div>
            <div className="grid gap-2">
              <label className="text-sm font-medium">Tipo de Compra</label>
              <select className="border rounded px-3 py-2 bg-background"
                value={cabecalho.tipoCompra}
                onChange={e => setCabecalho(p => ({ ...p, tipoCompra: e.target.value }))}>
                {['Mercadoria', 'Matéria-prima', 'Serviço', 'Ativo', 'Outros'].map(t =>
                  <option key={t}>{t}</option>)}
              </select>
            </div>
          </div>
          <div className="grid gap-2">
            <label className="text-sm font-medium">Número da Nota Fiscal</label>
            <input className="border rounded px-3 py-2 bg-background" placeholder="Opcional"
              value={cabecalho.numeroNota}
              onChange={e => setCabecalho(p => ({ ...p, numeroNota: e.target.value }))} />
          </div>
          <div className="grid gap-2">
            <label className="text-sm font-medium">Observações</label>
            <textarea rows={2} className="border rounded px-3 py-2 bg-background resize-none"
              value={cabecalho.observacoes}
              onChange={e => setCabecalho(p => ({ ...p, observacoes: e.target.value }))} />
          </div>
        </div>
      )}

      {/* Step 1 — Itens */}
      {step === 1 && (
        <div className="space-y-4">
          <ItensCompraTable itens={itens} onChange={setItens} />
          {itens.length === 0 && (
            <p className="text-sm text-muted-foreground text-center py-4">
              Clique em "Adicionar item" para começar.
            </p>
          )}
        </div>
      )}

      {/* Step 2 — Pagamento */}
      {step === 2 && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <label className="text-sm font-medium">Condição de Pagamento</label>
              <select className="border rounded px-3 py-2 bg-background"
                value={pagamento.condicaoPagamento}
                onChange={e => setPagamento(p => ({ ...p, condicaoPagamento: e.target.value }))}>
                {CONDICOES.map(c => <option key={c.value} value={c.value}>{c.label}</option>)}
              </select>
            </div>
            <div className="grid gap-2">
              <label className="text-sm font-medium">Forma de Pagamento</label>
              <select className="border rounded px-3 py-2 bg-background"
                value={pagamento.formaPagamento}
                onChange={e => setPagamento(p => ({ ...p, formaPagamento: e.target.value }))}>
                {FORMAS.map(f => <option key={f}>{f}</option>)}
              </select>
            </div>
          </div>
          {pagamento.condicaoPagamento === 'Parcelado' && (
            <div className="grid gap-2">
              <label className="text-sm font-medium">Número de Parcelas</label>
              <input type="number" min={2} max={48} className="border rounded px-3 py-2 bg-background w-32"
                value={pagamento.qtdParcelas}
                onChange={e => setPagamento(p => ({ ...p, qtdParcelas: parseInt(e.target.value) || 2 }))} />
            </div>
          )}
          <PreviewParcelas
            condicao={pagamento.condicaoPagamento}
            dataBase={cabecalho.data}
            qtdParcelas={pagamento.qtdParcelas}
            valorTotal={valorTotal}
            personalizadas={personalizadas}
            onPersonalizadasChange={setPersonalizadas}
          />
        </div>
      )}

      {/* Step 3 — Revisão */}
      {step === 3 && (
        <div className="space-y-4">
          <div className="rounded-lg border p-4 space-y-2 text-sm">
            <div className="flex justify-between"><span className="text-muted-foreground">Fornecedor</span>
              <span>{fornecedores.find(f => f.id === cabecalho.fornecedorId)?.nome}</span></div>
            <div className="flex justify-between"><span className="text-muted-foreground">Data</span>
              <span>{new Date(cabecalho.data + 'T00:00:00').toLocaleDateString('pt-BR')}</span></div>
            <div className="flex justify-between"><span className="text-muted-foreground">Nº Nota</span>
              <span>{cabecalho.numeroNota || '—'}</span></div>
            <div className="flex justify-between"><span className="text-muted-foreground">Itens</span>
              <span>{itens.length}</span></div>
            <div className="flex justify-between font-bold text-base border-t pt-2">
              <span>Total</span><span>{fmt(valorTotal)}</span></div>
          </div>
          <ItensCompraTable itens={itens} onChange={() => {}} readonly />
          <PreviewParcelas
            condicao={pagamento.condicaoPagamento}
            dataBase={cabecalho.data}
            qtdParcelas={pagamento.qtdParcelas}
            valorTotal={valorTotal}
            personalizadas={personalizadas}
          />
        </div>
      )}

      {/* Navigation */}
      <div className="flex justify-between pt-4 border-t">
        <Button variant="outline" onClick={() => step > 0 ? setStep(s => s - 1) : navigate('/compras')}>
          <ChevronLeft className="h-4 w-4 mr-1" /> {step === 0 ? 'Cancelar' : 'Voltar'}
        </Button>
        <div className="flex gap-2">
          {step < STEPS.length - 1 && (
            <Button onClick={() => setStep(s => s + 1)}>
              Próximo <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          )}
          {step === STEPS.length - 1 && (
            <>
              <Button variant="outline" onClick={() => handleSave(false)} disabled={saving}>
                Salvar Rascunho
              </Button>
              <Button onClick={() => handleSave(true)} disabled={saving}>
                Confirmar Compra
              </Button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Testar no browser**

Navegar para `http://localhost:5173/compras/nova`. Verificar:
- Steps navegam corretamente
- Tabela de itens adiciona/remove linhas
- Preview de parcelas aparece no step 3
- Revisão mostra os dados do formulário

- [ ] **Step 3: Commit**

```bash
git add gestorai-erp/frontend/src/pages/compras/NovaCompra.tsx
git commit -m "feat(compras): add NovaCompra multi-step form"
```

---

## Task 7: Detalhe da Compra

**Files:**
- Create: `src/pages/compras/DetalheCompra.tsx`

- [ ] **Step 1: Criar DetalheCompra.tsx**

```tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, CheckCircle, XCircle } from 'lucide-react'
import { useCompras } from '@/hooks/useCompras'
import { useParcelamentos } from '@/hooks/useParcelamentos'
import { ItensCompraTable } from '@/components/compras/ItensCompraTable'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useConfirm } from '@/hooks/useConfirm'
import { useToast } from '@/hooks/useToast'
import type { CompraResponse } from '@/types/compras'
import type { ParcelamentoResponse } from '@/types/parcelamentos'

const STATUS_VARIANT: Record<string, 'default' | 'secondary' | 'destructive'> = {
  Rascunho: 'secondary', Confirmada: 'default', Cancelada: 'destructive',
}

export default function DetalheCompra() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { get, confirmar, cancelar } = useCompras()
  const { listByCompra } = useParcelamentos()
  const { confirm } = useConfirm()
  const { toast } = useToast()
  const [compra, setCompra] = useState<CompraResponse | null>(null)
  const [parcelamento, setParcelamento] = useState<ParcelamentoResponse | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    get(id).then(c => {
      setCompra(c)
      if (c.parcelamentoId) {
        listByCompra(c.id).then(list => setParcelamento(list[0] ?? null))
      }
      setLoading(false)
    })
  }, [id])

  const handleConfirmar = async () => {
    if (!compra || !await confirm('Confirmar compra?', 'Esta ação irá atualizar o estoque e gerar as contas a pagar.')) return
    const updated = await confirmar(compra.id)
    setCompra(updated)
    if (updated.parcelamentoId) {
      const list = await listByCompra(updated.id)
      setParcelamento(list[0] ?? null)
    }
    toast({ title: 'Compra confirmada!' })
  }

  const handleCancelar = async () => {
    if (!compra || !await confirm('Cancelar compra?', 'Esta ação reverterá o estoque e cancelará parcelas pendentes.')) return
    const updated = await cancelar(compra.id)
    setCompra(updated)
    toast({ title: 'Compra cancelada.' })
  }

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  if (loading) return <div className="p-6 text-muted-foreground">Carregando...</div>
  if (!compra) return <div className="p-6">Compra não encontrada.</div>

  return (
    <div className="p-4 md:p-6 max-w-4xl mx-auto space-y-6">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate('/compras')} className="text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-5 w-5" />
        </button>
        <h1 className="text-2xl font-bold">Compra #{compra.numero}</h1>
        <Badge variant={STATUS_VARIANT[compra.status]}>{compra.status}</Badge>
        <div className="ml-auto flex gap-2">
          {compra.status === 'Rascunho' && (
            <>
              <Button onClick={handleConfirmar}>
                <CheckCircle className="h-4 w-4 mr-1" /> Confirmar
              </Button>
              <Button variant="destructive" onClick={handleCancelar}>
                <XCircle className="h-4 w-4 mr-1" /> Cancelar
              </Button>
            </>
          )}
          {compra.status === 'Confirmada' && (
            <Button variant="destructive" onClick={handleCancelar}>
              <XCircle className="h-4 w-4 mr-1" /> Cancelar
            </Button>
          )}
        </div>
      </div>

      <div className="grid md:grid-cols-2 gap-4 rounded-lg border p-4 text-sm">
        <div className="space-y-1">
          <p><span className="text-muted-foreground">Fornecedor:</span> <strong>{compra.fornecedorNome}</strong></p>
          <p><span className="text-muted-foreground">Data:</span> {new Date(compra.data).toLocaleDateString('pt-BR')}</p>
          <p><span className="text-muted-foreground">Tipo:</span> {compra.tipoCompra}</p>
          <p><span className="text-muted-foreground">Nota Fiscal:</span> {compra.numeroNota || '—'}</p>
        </div>
        <div className="space-y-1">
          <p><span className="text-muted-foreground">Condição de Pgto:</span> {compra.condicaoPagamento}</p>
          <p><span className="text-muted-foreground">Forma de Pgto:</span> {compra.formaPagamento}</p>
          <p><span className="text-muted-foreground">Valor Total:</span> <strong>{fmt(compra.valorTotal)}</strong></p>
          {compra.observacoes && <p><span className="text-muted-foreground">Obs:</span> {compra.observacoes}</p>}
        </div>
      </div>

      <div>
        <h2 className="font-semibold mb-3">Itens</h2>
        <ItensCompraTable itens={compra.itens.map(i => ({
          produtoId: i.produtoId, descricao: i.descricao,
          destinoCompra: i.destinoCompra as any, quantidade: i.quantidade,
          valorUnitario: i.valorUnitario, desconto: i.desconto,
          freteRateado: i.freteRateado, impostos: i.impostos,
          categoriaFinanceira: i.categoriaFinanceira, centroCusto: i.centroCusto,
        }))} onChange={() => {}} readonly />
      </div>

      {parcelamento && (
        <div>
          <h2 className="font-semibold mb-3">Parcelamento — {parcelamento.status}</h2>
          <div className="rounded-lg border overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/50">
                <tr>
                  <th className="text-left p-3">Parcela</th>
                  <th className="text-left p-3">Vencimento</th>
                  <th className="text-right p-3">Valor</th>
                  <th className="text-center p-3">Status</th>
                  <th className="text-left p-3">Pagamento</th>
                </tr>
              </thead>
              <tbody>
                {parcelamento.parcelas.map(p => (
                  <tr key={p.id} className="border-t">
                    <td className="p-3">{p.numeroParcela}/{parcelamento.qtdParcelas}</td>
                    <td className="p-3">{new Date(p.dataVencimento).toLocaleDateString('pt-BR')}</td>
                    <td className="p-3 text-right">{fmt(p.valor)}</td>
                    <td className="p-3 text-center">
                      <Badge variant={p.status === 'Pago' ? 'default' : p.status === 'Cancelado' ? 'destructive' : 'secondary'}>
                        {p.status}
                      </Badge>
                    </td>
                    <td className="p-3 text-muted-foreground">
                      {p.dataPagamento ? new Date(p.dataPagamento).toLocaleDateString('pt-BR') : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Testar no browser**

Criar uma compra via `/compras/nova`, confirmar, verificar que:
- Detalhe mostra itens e parcelamento
- Botão Confirmar gera o parcelamento
- Botão Cancelar desativa corretamente

- [ ] **Step 3: Commit**

```bash
git add gestorai-erp/frontend/src/pages/compras/DetalheCompra.tsx
git commit -m "feat(compras): add DetalheCompra page with parcelamento view"
```

---

## Task 8: Pedidos de Compra

**Files:**
- Create: `src/pages/compras/PedidosCompra.tsx`
- Create: `src/pages/compras/NovoPedidoCompra.tsx`
- Create: `src/pages/compras/DetalhePedidoCompra.tsx`

- [ ] **Step 1: Criar PedidosCompra.tsx**

```tsx
import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { usePedidosCompra } from '@/hooks/usePedidosCompra'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useToast } from '@/hooks/useToast'

const STATUS_VARIANT: Record<string, 'default' | 'secondary' | 'destructive'> = {
  Rascunho: 'secondary', AguardandoAprovacao: 'secondary',
  Aprovado: 'default', RecebidoParcialmente: 'default',
  RecebidoTotalmente: 'default', Cancelado: 'destructive',
}

export default function PedidosCompra() {
  const navigate = useNavigate()
  const { pedidos, loading, list, aprovar, converter } = usePedidosCompra()
  const { toast } = useToast()

  useEffect(() => { list() }, [])

  const handleConverter = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation()
    const compra = await converter(id)
    toast({ title: 'Pedido convertido em compra!' })
    navigate(`/compras/${compra.id}`)
  }

  const handleAprovar = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation()
    await aprovar(id)
    toast({ title: 'Pedido aprovado!' })
  }

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  return (
    <div className="p-4 md:p-6 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Pedidos de Compra</h1>
        <Button onClick={() => navigate('/compras/pedidos/novo')}>
          <Plus className="h-4 w-4 mr-2" /> Novo Pedido
        </Button>
      </div>
      <div className="rounded-lg border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left p-3">Nº</th>
              <th className="text-left p-3">Data</th>
              <th className="text-left p-3">Fornecedor</th>
              <th className="text-right p-3">Valor Est.</th>
              <th className="text-center p-3">Status</th>
              <th className="p-3" />
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={6} className="p-4 text-center text-muted-foreground">Carregando...</td></tr>}
            {!loading && pedidos.length === 0 && <tr><td colSpan={6} className="p-4 text-center text-muted-foreground">Nenhum pedido.</td></tr>}
            {pedidos.map(p => (
              <tr key={p.id} className="border-t hover:bg-muted/30 cursor-pointer"
                onClick={() => navigate(`/compras/pedidos/${p.id}`)}>
                <td className="p-3 font-mono">#{p.numero}</td>
                <td className="p-3">{new Date(p.data).toLocaleDateString('pt-BR')}</td>
                <td className="p-3">{p.fornecedorNome}</td>
                <td className="p-3 text-right">{fmt(p.valorEstimado)}</td>
                <td className="p-3 text-center">
                  <Badge variant={STATUS_VARIANT[p.status]}>{p.status}</Badge>
                </td>
                <td className="p-3 text-right space-x-2">
                  {p.status === 'Rascunho' && (
                    <button className="text-xs text-primary hover:underline"
                      onClick={e => handleAprovar(p.id, e)}>Aprovar</button>
                  )}
                  {p.status === 'Aprovado' && (
                    <button className="text-xs text-primary hover:underline"
                      onClick={e => handleConverter(p.id, e)}>Converter em Compra</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Criar NovoPedidoCompra.tsx**

```tsx
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { usePedidosCompra } from '@/hooks/usePedidosCompra'
import { useFornecedores } from '@/hooks/useFornecedores'
import { Button } from '@/components/ui/button'
import { useToast } from '@/hooks/useToast'
import type { ItemPedidoCompraRequest } from '@/types/compras'
import { Plus, Trash2 } from 'lucide-react'

export default function NovoPedidoCompra() {
  const navigate = useNavigate()
  const { create } = usePedidosCompra()
  const { fornecedores, list } = useFornecedores()
  const { toast } = useToast()
  const [fornecedorId, setFornecedorId] = useState('')
  const [data, setData] = useState(new Date().toISOString().slice(0, 10))
  const [observacoes, setObservacoes] = useState('')
  const [itens, setItens] = useState<ItemPedidoCompraRequest[]>([{ descricao: '', quantidade: 1, valorEstimado: 0 }])
  const [saving, setSaving] = useState(false)

  useState(() => { list() })

  const addItem = () => setItens(prev => [...prev, { descricao: '', quantidade: 1, valorEstimado: 0 }])
  const removeItem = (i: number) => setItens(prev => prev.filter((_, idx) => idx !== i))
  const updateItem = (i: number, field: keyof ItemPedidoCompraRequest, value: unknown) =>
    setItens(prev => prev.map((item, idx) => idx === i ? { ...item, [field]: value } : item))

  const handleSave = async () => {
    if (!fornecedorId) { toast({ title: 'Selecione um fornecedor', variant: 'destructive' }); return }
    setSaving(true)
    try {
      const pedido = await create({ fornecedorId, data, observacoes, itens })
      toast({ title: 'Pedido criado!' })
      navigate(`/compras/pedidos/${pedido.id}`)
    } catch {
      toast({ title: 'Erro ao criar pedido', variant: 'destructive' })
    } finally {
      setSaving(false)
    }
  }

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const total = itens.reduce((a, i) => a + i.quantidade * i.valorEstimado, 0)

  return (
    <div className="p-4 md:p-6 max-w-3xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold">Novo Pedido de Compra</h1>
      <div className="grid gap-4">
        <div className="grid gap-2">
          <label className="text-sm font-medium">Fornecedor *</label>
          <select className="border rounded px-3 py-2 bg-background" value={fornecedorId}
            onChange={e => setFornecedorId(e.target.value)}>
            <option value="">Selecione...</option>
            {fornecedores.map(f => <option key={f.id} value={f.id}>{f.nome}</option>)}
          </select>
        </div>
        <div className="grid gap-2">
          <label className="text-sm font-medium">Data</label>
          <input type="date" className="border rounded px-3 py-2 bg-background w-48"
            value={data} onChange={e => setData(e.target.value)} />
        </div>
        <div>
          <div className="flex items-center justify-between mb-2">
            <label className="text-sm font-medium">Itens</label>
            <button onClick={addItem} className="flex items-center gap-1 text-sm text-primary hover:underline">
              <Plus className="h-4 w-4" /> Adicionar
            </button>
          </div>
          <div className="space-y-2">
            {itens.map((item, i) => (
              <div key={i} className="flex gap-2 items-center">
                <input className="border rounded px-2 py-1.5 flex-1 bg-background text-sm" placeholder="Descrição"
                  value={item.descricao} onChange={e => updateItem(i, 'descricao', e.target.value)} />
                <input type="number" min={1} className="border rounded px-2 py-1.5 w-20 bg-background text-sm text-right"
                  value={item.quantidade} onChange={e => updateItem(i, 'quantidade', parseFloat(e.target.value) || 1)} />
                <input type="number" min={0} step={0.01} className="border rounded px-2 py-1.5 w-28 bg-background text-sm text-right"
                  value={item.valorEstimado} onChange={e => updateItem(i, 'valorEstimado', parseFloat(e.target.value) || 0)} />
                <button onClick={() => removeItem(i)} className="text-destructive"><Trash2 className="h-4 w-4" /></button>
              </div>
            ))}
          </div>
          <p className="text-right text-sm mt-2 font-medium">Total estimado: {fmt(total)}</p>
        </div>
        <div className="grid gap-2">
          <label className="text-sm font-medium">Observações</label>
          <textarea rows={2} className="border rounded px-3 py-2 bg-background resize-none"
            value={observacoes} onChange={e => setObservacoes(e.target.value)} />
        </div>
      </div>
      <div className="flex justify-end gap-2 border-t pt-4">
        <Button variant="outline" onClick={() => navigate('/compras/pedidos')}>Cancelar</Button>
        <Button onClick={handleSave} disabled={saving}>Criar Pedido</Button>
      </div>
    </div>
  )
}
```

- [ ] **Step 3: Criar DetalhePedidoCompra.tsx**

```tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { usePedidosCompra } from '@/hooks/usePedidosCompra'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useToast } from '@/hooks/useToast'
import type { PedidoCompraResponse } from '@/types/compras'

export default function DetalhePedidoCompra() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { get, aprovar, converter, cancelar } = usePedidosCompra()
  const { toast } = useToast()
  const [pedido, setPedido] = useState<PedidoCompraResponse | null>(null)

  useEffect(() => { if (id) get(id).then(setPedido) }, [id])

  const handleAprovar = async () => {
    if (!pedido) return
    const updated = await aprovar(pedido.id)
    setPedido(updated)
    toast({ title: 'Pedido aprovado!' })
  }

  const handleConverter = async () => {
    if (!pedido) return
    const compra = await converter(pedido.id)
    toast({ title: 'Convertido em compra!' })
    navigate(`/compras/${compra.id}`)
  }

  const handleCancelar = async () => {
    if (!pedido) return
    const updated = await cancelar(pedido.id)
    setPedido(updated)
    toast({ title: 'Pedido cancelado.' })
  }

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  if (!pedido) return <div className="p-6 text-muted-foreground">Carregando...</div>

  return (
    <div className="p-4 md:p-6 max-w-3xl mx-auto space-y-6">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate('/compras/pedidos')} className="text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-5 w-5" />
        </button>
        <h1 className="text-2xl font-bold">Pedido #{pedido.numero}</h1>
        <Badge variant={pedido.status === 'Cancelado' ? 'destructive' : pedido.status === 'Aprovado' ? 'default' : 'secondary'}>
          {pedido.status}
        </Badge>
        <div className="ml-auto flex gap-2">
          {pedido.status === 'Rascunho' && <Button onClick={handleAprovar}>Aprovar</Button>}
          {pedido.status === 'Aprovado' && <Button onClick={handleConverter}>Converter em Compra</Button>}
          {!['Cancelado', 'RecebidoTotalmente'].includes(pedido.status) && (
            <Button variant="destructive" onClick={handleCancelar}>Cancelar</Button>
          )}
        </div>
      </div>

      <div className="rounded-lg border p-4 text-sm space-y-1">
        <p><span className="text-muted-foreground">Fornecedor:</span> <strong>{pedido.fornecedorNome}</strong></p>
        <p><span className="text-muted-foreground">Data:</span> {new Date(pedido.data).toLocaleDateString('pt-BR')}</p>
        <p><span className="text-muted-foreground">Valor Estimado:</span> <strong>{fmt(pedido.valorEstimado)}</strong></p>
        {pedido.observacoes && <p><span className="text-muted-foreground">Obs:</span> {pedido.observacoes}</p>}
      </div>

      <div>
        <h2 className="font-semibold mb-3">Itens</h2>
        <div className="rounded-lg border overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="text-left p-3">Descrição</th>
                <th className="text-right p-3">Qtd</th>
                <th className="text-right p-3">Valor Est.</th>
                <th className="text-right p-3">Total</th>
              </tr>
            </thead>
            <tbody>
              {pedido.itens.map(item => (
                <tr key={item.id} className="border-t">
                  <td className="p-3">{item.descricao}</td>
                  <td className="p-3 text-right">{item.quantidade}</td>
                  <td className="p-3 text-right">{fmt(item.valorEstimado)}</td>
                  <td className="p-3 text-right font-medium">{fmt(item.quantidade * item.valorEstimado)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
```

- [ ] **Step 4: Testar no browser**

Navegar para `/compras/pedidos/novo`, criar pedido, aprovar, converter em compra.

- [ ] **Step 5: Commit**

```bash
git add gestorai-erp/frontend/src/pages/compras/PedidosCompra.tsx \
        gestorai-erp/frontend/src/pages/compras/NovoPedidoCompra.tsx \
        gestorai-erp/frontend/src/pages/compras/DetalhePedidoCompra.tsx
git commit -m "feat(compras): add PedidosCompra pages"
```

---

## Task 9: Dashboard de Compras

**Files:**
- Create: `src/pages/compras/DashboardCompras.tsx`

- [ ] **Step 1: Criar DashboardCompras.tsx**

```tsx
import { useEffect, useState } from 'react'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, BarChart, Bar, Legend,
} from 'recharts'
import { useComprasDashboard } from '@/hooks/useComprasDashboard'
import { KpiCard } from '@/components/dashboard/KpiCard'
import { ShoppingCart, TrendingDown, Users, Receipt } from 'lucide-react'

const COLORS = ['#6366f1', '#8b5cf6', '#a78bfa', '#c4b5fd', '#ddd6fe']

export default function DashboardCompras() {
  const { data, loading, load } = useComprasDashboard()
  const [periodo, setPeriodo] = useState('ano')

  useEffect(() => {
    const hoje = new Date()
    const ate = hoje.toISOString().slice(0, 10)
    const de = periodo === 'mes'
      ? new Date(hoje.getFullYear(), hoje.getMonth(), 1).toISOString().slice(0, 10)
      : new Date(hoje.getFullYear(), 0, 1).toISOString().slice(0, 10)
    load(de, ate)
  }, [periodo])

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  if (loading || !data) return <div className="p-6 text-muted-foreground">Carregando dashboard...</div>

  return (
    <div className="p-4 md:p-6 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Dashboard de Compras</h1>
        <div className="flex gap-2">
          {['mes', 'ano'].map(p => (
            <button key={p} onClick={() => setPeriodo(p)}
              className={`px-3 py-1 rounded-full text-sm border transition-colors ${
                periodo === p ? 'bg-primary text-primary-foreground border-primary' : 'border-border hover:bg-muted'
              }`}>
              {p === 'mes' ? 'Este Mês' : 'Este Ano'}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        <KpiCard title="Total no Mês" value={fmt(data.totalMes)} icon={ShoppingCart} />
        <KpiCard title="Total no Ano" value={fmt(data.totalAno)} icon={TrendingDown} />
        <KpiCard title="Ticket Médio" value={fmt(data.ticketMedio)} icon={Receipt} />
        <KpiCard title="Qtd Compras" value={String(data.qtdCompras)} icon={Receipt} />
        <KpiCard title="Fornecedores Ativos" value={String(data.fornecedoresAtivos)} icon={Users} />
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        <div className="rounded-lg border p-4">
          <h2 className="font-semibold mb-4">Evolução Mensal</h2>
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={data.evolucaoMensal}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} tickFormatter={v => `R$${(v / 1000).toFixed(0)}k`} />
              <Tooltip formatter={(v: number) => fmt(v)} />
              <Line type="monotone" dataKey="total" stroke="#6366f1" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </div>

        <div className="rounded-lg border p-4">
          <h2 className="font-semibold mb-4">Compras por Fornecedor</h2>
          {data.porFornecedor.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Sem dados</p>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <PieChart>
                <Pie data={data.porFornecedor} dataKey="total" nameKey="fornecedor"
                  cx="50%" cy="50%" outerRadius={80} label={({ fornecedor }) => fornecedor}>
                  {data.porFornecedor.map((_, i) => (
                    <Cell key={i} fill={COLORS[i % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(v: number) => fmt(v)} />
              </PieChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      <div className="rounded-lg border p-4">
        <h2 className="font-semibold mb-4">Top Produtos Comprados</h2>
        {data.topProdutos.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-4">Sem dados</p>
        ) : (
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={data.topProdutos} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis type="number" tick={{ fontSize: 11 }} />
              <YAxis dataKey="produto" type="category" tick={{ fontSize: 11 }} width={120} />
              <Tooltip />
              <Bar dataKey="quantidade" fill="#6366f1" radius={[0, 4, 4, 0]} />
            </BarChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Testar no browser**

Navegar para `/compras/dashboard`. Confirmar que KPIs e gráficos renderizam (mesmo que vazios inicialmente).

- [ ] **Step 3: Commit**

```bash
git add gestorai-erp/frontend/src/pages/compras/DashboardCompras.tsx
git commit -m "feat(compras): add DashboardCompras page with KPIs and charts"
```

---

## Task 10: Aba Compras em Relatórios + Atualizar FornecedorForm

**Files:**
- Create: `src/components/relatorios/AbaCompras.tsx`
- Modify: `src/pages/relatorios/Relatorios.tsx`
- Modify: `src/components/fornecedores/FornecedorForm.tsx`

- [ ] **Step 1: Criar AbaCompras.tsx**

```tsx
import { useEffect, useState } from 'react'
import { useCompras } from '@/hooks/useCompras'
import { useFornecedores } from '@/hooks/useFornecedores'
import type { CompraResponse } from '@/types/compras'

export function AbaCompras() {
  const { compras, list } = useCompras()
  const { fornecedores, list: listForn } = useFornecedores()
  const [de, setDe] = useState(new Date(new Date().getFullYear(), 0, 1).toISOString().slice(0, 10))
  const [ate, setAte] = useState(new Date().toISOString().slice(0, 10))
  const [fornecedorId, setFornecedorId] = useState('')

  useEffect(() => { listForn() }, [])
  useEffect(() => {
    list({ status: 'Confirmada', fornecedorId: fornecedorId || undefined, de, ate })
  }, [de, ate, fornecedorId])

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const total = compras.reduce((a, c) => a + c.valorTotal, 0)

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-3">
        <div className="flex items-center gap-2">
          <label className="text-sm">De:</label>
          <input type="date" className="border rounded px-2 py-1 text-sm bg-background"
            value={de} onChange={e => setDe(e.target.value)} />
        </div>
        <div className="flex items-center gap-2">
          <label className="text-sm">Até:</label>
          <input type="date" className="border rounded px-2 py-1 text-sm bg-background"
            value={ate} onChange={e => setAte(e.target.value)} />
        </div>
        <div className="flex items-center gap-2">
          <label className="text-sm">Fornecedor:</label>
          <select className="border rounded px-2 py-1 text-sm bg-background"
            value={fornecedorId} onChange={e => setFornecedorId(e.target.value)}>
            <option value="">Todos</option>
            {fornecedores.map(f => <option key={f.id} value={f.id}>{f.nome}</option>)}
          </select>
        </div>
      </div>

      <div className="rounded-lg border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left p-3">Nº</th>
              <th className="text-left p-3">Data</th>
              <th className="text-left p-3">Fornecedor</th>
              <th className="text-left p-3">Tipo</th>
              <th className="text-right p-3">Valor</th>
            </tr>
          </thead>
          <tbody>
            {compras.length === 0 && (
              <tr><td colSpan={5} className="p-4 text-center text-muted-foreground">Nenhuma compra no período.</td></tr>
            )}
            {compras.map((c: CompraResponse) => (
              <tr key={c.id} className="border-t">
                <td className="p-3 font-mono">#{c.numero}</td>
                <td className="p-3">{new Date(c.data).toLocaleDateString('pt-BR')}</td>
                <td className="p-3">{c.fornecedorNome}</td>
                <td className="p-3">{c.tipoCompra}</td>
                <td className="p-3 text-right font-medium">{fmt(c.valorTotal)}</td>
              </tr>
            ))}
          </tbody>
          {compras.length > 0 && (
            <tfoot>
              <tr className="border-t bg-muted/30">
                <td colSpan={4} className="p-3 text-right font-semibold">Total:</td>
                <td className="p-3 text-right font-bold">{fmt(total)}</td>
              </tr>
            </tfoot>
          )}
        </table>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Adicionar aba Compras em Relatorios.tsx**

Localizar o array de abas no arquivo `src/pages/relatorios/Relatorios.tsx` (procurar por `tabs` ou por onde os componentes de aba são importados). Adicionar:

```typescript
import { AbaCompras } from '@/components/relatorios/AbaCompras'
```

E adicionar a aba ao array de tabs existente (seguindo o padrão do arquivo):
```typescript
{ id: 'compras', label: 'Compras', component: <AbaCompras /> }
```

- [ ] **Step 3: Atualizar FornecedorForm.tsx**

Localizar `src/components/fornecedores/FornecedorForm.tsx` e adicionar os campos novos após os existentes:

```tsx
// Após o campo "Nome", adicionar:
<div className="grid gap-1.5">
  <Label htmlFor="razaoSocial">Razão Social</Label>
  <Input id="razaoSocial" value={form.razaoSocial ?? ''} onChange={e => setForm(p => ({ ...p, razaoSocial: e.target.value }))} />
</div>
<div className="grid gap-1.5">
  <Label htmlFor="nomeFantasia">Nome Fantasia</Label>
  <Input id="nomeFantasia" value={form.nomeFantasia ?? ''} onChange={e => setForm(p => ({ ...p, nomeFantasia: e.target.value }))} />
</div>

// Após CNPJ/CPF, adicionar:
<div className="grid gap-1.5">
  <Label htmlFor="inscricaoEstadual">Inscrição Estadual</Label>
  <Input id="inscricaoEstadual" value={form.inscricaoEstadual ?? ''} onChange={e => setForm(p => ({ ...p, inscricaoEstadual: e.target.value }))} />
</div>

// Após Telefone, adicionar:
<div className="grid gap-1.5">
  <Label htmlFor="whatsapp">WhatsApp</Label>
  <Input id="whatsapp" value={form.whatsapp ?? ''} onChange={e => setForm(p => ({ ...p, whatsapp: e.target.value }))} />
</div>
```

Garantir que o estado `form` inclua os novos campos (adicionar ao estado inicial e ao `reset`).

- [ ] **Step 4: Build TypeScript final**

```bash
cd gestorai-erp/frontend && npx tsc --noEmit
```
Esperado: sem erros nos arquivos do módulo compras.

- [ ] **Step 5: Testar fluxo completo no browser**

1. Abrir `http://localhost:5173`
2. Sidebar → Compras → Pedidos: criar pedido → aprovar → converter em compra
3. Sidebar → Compras → Compras: ver a compra criada, abrir detalhe → confirmar
4. Sidebar → Compras → Dashboard: verificar KPIs e gráficos atualizados
5. Sidebar → Relatórios → aba Compras: verificar tabela com filtros

- [ ] **Step 6: Commit final**

```bash
git add gestorai-erp/frontend/src/components/relatorios/AbaCompras.tsx \
        gestorai-erp/frontend/src/pages/relatorios/Relatorios.tsx \
        gestorai-erp/frontend/src/components/fornecedores/FornecedorForm.tsx
git commit -m "feat(compras): add AbaCompras to relatorios and update FornecedorForm"
```

---

## Self-Review

**Cobertura da spec:**
- ✅ Sidebar expandida: Fornecedores, Pedidos, Compras, Dashboard
- ✅ Rotas: `/compras`, `/compras/nova`, `/compras/:id`, `/compras/pedidos`, `/compras/pedidos/novo`, `/compras/pedidos/:id`, `/compras/dashboard`
- ✅ Lista de Compras com KPIs (total mês, qtd, contas a pagar, fornecedores ativos)
- ✅ Nova Compra em 4 steps: cabeçalho, itens, pagamento, revisão
- ✅ Detalhe da Compra com tabela de itens + parcelamento com parcelas
- ✅ Botões Confirmar / Cancelar no detalhe
- ✅ Pedidos de Compra: lista, novo, detalhe, aprovar, converter, cancelar
- ✅ Dashboard com 5 KPIs + 3 gráficos (linha, pizza, barra horizontal)
- ✅ Relatórios: aba Compras com filtros por período e fornecedor
- ✅ FornecedorForm atualizado com RazaoSocial, NomeFantasia, IE, WhatsApp
- ✅ PreviewParcelas mostra parcelas calculadas com datas editáveis para Personalizado
- ✅ ItensCompraTable com destino (EstoqueParaVenda / ConsumoInterno / AtivoImobilizado)
