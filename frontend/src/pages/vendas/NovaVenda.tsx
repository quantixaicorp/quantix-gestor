import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { CheckCircle2, ArrowLeft } from 'lucide-react'
import { useVendas } from '@/hooks/useVendas'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import SeletorProduto from '@/components/vendas/SeletorProduto'
import ResumoPedido from '@/components/vendas/ResumoPedido'
import type { ItemCarrinho, CreateVendaRequest, FecharVendaRequest } from '@/types/vendas'

const FORMAS = [
  { value: 'Pix',      label: 'Pix',     emoji: '⚡' },
  { value: 'Dinheiro', label: 'Dinheiro', emoji: '💵' },
  { value: 'Cartao',   label: 'Cartão',   emoji: '💳' },
  { value: 'Outro',    label: 'Outro',    emoji: '🔖' },
] as const

type FormaPagamento = typeof FORMAS[number]['value']

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function NovaVenda() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const vendaIdParam = searchParams.get('vendaId')

  const { create, fechar, get } = useVendas()
  const { clientes, list: listClientes } = useClientes()

  const [itens, setItens] = useState<ItemCarrinho[]>([])
  const [desconto, setDesconto] = useState(0)
  const [clienteId, setClienteId] = useState('')
  const [formaPagamento, setFormaPagamento] = useState<FormaPagamento>('Pix')
  const [parcelas, setParcelas] = useState(1)
  const [salvando, setSalvando] = useState(false)
  const [vendaFinalizada, setVendaFinalizada] = useState<{ id: string; total: number } | null>(null)
  const [carregando, setCarregando] = useState(false)

  useEffect(() => {
    listClientes()
    if (!vendaIdParam) return
    setCarregando(true)
    void get(vendaIdParam).then(venda => {
      setItens(venda.itens.map(i => ({
        produtoId: i.produtoId,
        produtoNome: i.produtoNome,
        precoUnitario: i.precoUnitario,
        quantidade: i.quantidade,
        desconto: 0,
        total: i.precoUnitario * i.quantidade,
      })))
    }).finally(() => setCarregando(false))
  }, [vendaIdParam, get, listClientes])

  function adicionarItem(item: ItemCarrinho) {
    setItens(prev => {
      const existente = prev.find(i => i.produtoId === item.produtoId)
      if (existente)
        return prev.map(i => i.produtoId === item.produtoId
          ? { ...i, quantidade: i.quantidade + 1, total: i.precoUnitario * (i.quantidade + 1) }
          : i)
      return [...prev, item]
    })
  }

  function alterarQuantidade(produtoId: string, quantidade: number) {
    if (quantidade <= 0) return
    setItens(prev => prev.map(i => i.produtoId === produtoId
      ? { ...i, quantidade, total: i.precoUnitario * quantidade }
      : i))
  }

  async function confirmarVenda() {
    if (itens.length === 0) return
    setSalvando(true)
    try {
      if (vendaIdParam) {
        const req: FecharVendaRequest = {
          formaPagamento,
          parcelas: formaPagamento === 'Cartao' ? parcelas : undefined,
        }
        const result = await fechar(vendaIdParam, req)
        setVendaFinalizada({ id: result.id, total: result.total })
      } else {
        const req: CreateVendaRequest = {
          clienteId: clienteId || undefined,
          itens: itens.map(i => ({ produtoId: i.produtoId, quantidade: i.quantidade, desconto: 0 })),
          desconto,
          formaPagamento,
          parcelas: formaPagamento === 'Cartao' ? parcelas : undefined,
        }
        const result = await create(req)
        setVendaFinalizada({ id: result.id, total: result.total })
      }
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao finalizar venda')
    } finally {
      setSalvando(false)
    }
  }

  const subtotal = itens.reduce((s, i) => s + i.precoUnitario * i.quantidade, 0)
  const total = Math.max(0, subtotal - desconto)

  if (carregando) return (
    <div className="flex h-64 items-center justify-center text-muted-foreground">
      Carregando venda...
    </div>
  )

  /* ── Tela de sucesso ── */
  if (vendaFinalizada) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="text-center space-y-5 max-w-sm">
          <CheckCircle2 size={64} className="mx-auto text-green-500" />
          <div>
            <h2 className="text-2xl font-bold">Venda finalizada!</h2>
            <p className="text-muted-foreground mt-1">
              Total cobrado: <span className="font-semibold text-foreground">{fmt(vendaFinalizada.total)}</span>
            </p>
          </div>
          <div className="flex flex-col gap-2">
            <Button onClick={() => {
              setItens([]); setDesconto(0); setClienteId(''); setVendaFinalizada(null)
            }}>
              Nova venda
            </Button>
            <Button variant="outline" onClick={() => navigate('/vendas')}>
              Ver histórico
            </Button>
          </div>
        </div>
      </div>
    )
  }

  /* ── Layout PDV ── */
  return (
    <div className="flex flex-col gap-4 h-full">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate('/vendas')}
          className="text-muted-foreground hover:text-foreground transition-colors">
          <ArrowLeft size={20} />
        </button>
        <h1 className="text-xl font-bold">
          {vendaIdParam ? 'Finalizar Orçamento' : 'Nova Venda'}
        </h1>
      </div>

      {vendaIdParam && (
        <div className="rounded-lg border border-blue-200 bg-blue-50 dark:bg-blue-950/30 dark:border-blue-800 px-4 py-2.5 text-sm text-blue-700 dark:text-blue-300">
          Venda gerada a partir de orçamento — confirme o pagamento para concluir.
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-[1fr_380px] gap-5 items-start">

        {/* ── Coluna esquerda: produtos + carrinho ── */}
        <div className="space-y-4">
          {!vendaIdParam && (
            <div className="rounded-xl border bg-card shadow-sm p-4 space-y-3">
              <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
                Adicionar item
              </p>
              <SeletorProduto onAdd={adicionarItem} />
            </div>
          )}

          <div className="rounded-xl border bg-card shadow-sm p-4">
            <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-3">
              Carrinho
            </p>
            <ResumoPedido
              itens={itens}
              desconto={desconto}
              onChangeQuantidade={alterarQuantidade}
              onRemover={id => setItens(prev => prev.filter(i => i.produtoId !== id))}
              onChangeDesconto={setDesconto}
            />
          </div>
        </div>

        {/* ── Coluna direita: pagamento ── */}
        <div className="rounded-xl border bg-card shadow-sm p-5 space-y-5 lg:sticky lg:top-4">
          <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
            Pagamento
          </p>

          {!vendaIdParam && (
            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground">Cliente (opcional)</Label>
              <select value={clienteId} onChange={e => setClienteId(e.target.value)}
                className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
                <option value="">Balcão (sem cliente)</option>
                {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
            </div>
          )}

          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">Forma de pagamento</Label>
            <div className="grid grid-cols-2 gap-2">
              {FORMAS.map(f => (
                <button key={f.value} type="button"
                  onClick={() => setFormaPagamento(f.value)}
                  className={`flex items-center gap-2 rounded-lg border px-3 py-2.5 text-sm font-medium transition-all ${
                    formaPagamento === f.value
                      ? 'border-primary bg-primary/10 text-primary shadow-sm'
                      : 'border-border text-muted-foreground hover:border-muted-foreground hover:text-foreground'
                  }`}>
                  <span>{f.emoji}</span>
                  {f.label}
                </button>
              ))}
            </div>
          </div>

          {formaPagamento === 'Cartao' && (
            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground">Parcelas</Label>
              <select value={parcelas} onChange={e => setParcelas(Number(e.target.value))}
                className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
                {Array.from({ length: 12 }, (_, i) => i + 1).map(n => (
                  <option key={n} value={n}>{n}x {n > 1 ? fmt(total / n) : ''}</option>
                ))}
              </select>
            </div>
          )}

          {/* Total em destaque */}
          <div className="rounded-lg bg-primary/5 border border-primary/20 px-4 py-3 flex justify-between items-center">
            <span className="text-sm font-medium text-muted-foreground">Total</span>
            <span className="text-3xl font-bold text-primary tabular-nums">{fmt(total)}</span>
          </div>

          <Button
            className="w-full h-12 text-base font-semibold"
            disabled={itens.length === 0 || salvando}
            onClick={confirmarVenda}>
            {salvando ? 'Finalizando...' : '✓ Finalizar Venda'}
          </Button>

          {itens.length === 0 && (
            <p className="text-xs text-center text-muted-foreground">
              Adicione ao menos um item para finalizar
            </p>
          )}
        </div>
      </div>
    </div>
  )
}
