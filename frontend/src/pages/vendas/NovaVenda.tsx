// frontend/src/pages/vendas/NovaVenda.tsx
import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useVendas } from '@/hooks/useVendas'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import SeletorProduto from '@/components/vendas/SeletorProduto'
import ResumoPedido from '@/components/vendas/ResumoPedido'
import type { ItemCarrinho, CreateVendaRequest, FecharVendaRequest } from '@/types/vendas'

type Etapa = 1 | 2 | 3

export default function NovaVenda() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const vendaIdParam = searchParams.get('vendaId')

  const { create, fechar, get } = useVendas()
  const { clientes, list: listClientes } = useClientes()

  const [etapa, setEtapa] = useState<Etapa>(vendaIdParam ? 2 : 1)
  const [itens, setItens] = useState<ItemCarrinho[]>([])
  const [desconto, setDesconto] = useState(0)
  const [clienteId, setClienteId] = useState('')
  const [formaPagamento, setFormaPagamento] = useState<CreateVendaRequest['formaPagamento']>('Pix')
  const [parcelas, setParcelas] = useState(1)
  const [salvando, setSalvando] = useState(false)
  const [carregandoVenda, setCarregandoVenda] = useState(false)
  const [vendaFinalizada, setVendaFinalizada] = useState<{ id: string; total: number } | null>(null)

  useEffect(() => {
    if (!vendaIdParam) return
    setCarregandoVenda(true)
    void get(vendaIdParam).then(venda => {
      setItens(venda.itens.map(i => ({
        produtoId: i.produtoId,
        produtoNome: i.produtoNome,
        precoUnitario: i.precoUnitario,
        quantidade: i.quantidade,
        desconto: 0,
        total: i.precoUnitario * i.quantidade,
      })))
      listClientes()
    }).finally(() => setCarregandoVenda(false))
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

  function removerItem(produtoId: string) {
    setItens(prev => prev.filter(i => i.produtoId !== produtoId))
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
        setEtapa(3)
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
        setEtapa(3)
      }
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao finalizar venda')
    } finally {
      setSalvando(false)
    }
  }

  if (carregandoVenda) return <p className="text-muted-foreground">Carregando venda...</p>

  if (etapa === 3 && vendaFinalizada) {
    const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
    return (
      <div className="max-w-md mx-auto space-y-6 pt-8 text-center">
        <div className="text-6xl">✅</div>
        <h1 className="text-2xl font-bold">Venda finalizada!</h1>
        <p className="text-muted-foreground">Total: <strong>{fmt(vendaFinalizada.total)}</strong></p>
        <div className="flex gap-3 justify-center">
          <Button variant="outline" onClick={() => window.print()}>Imprimir comprovante</Button>
          <Button onClick={() => navigate('/vendas')}>Ver histórico</Button>
          <Button variant="secondary" onClick={() => {
            setItens([]); setDesconto(0); setClienteId(''); setEtapa(1); setVendaFinalizada(null)
          }}>Nova venda</Button>
        </div>
      </div>
    )
  }

  const etapas = vendaIdParam
    ? ['1. Itens do orçamento', '2. Pagamento']
    : ['1. Produtos', '2. Pagamento']

  return (
    <div className="space-y-4">
      {vendaIdParam && (
        <div className="rounded-md border border-blue-200 bg-blue-50 px-4 py-2 text-sm text-blue-700">
          Venda gerada a partir de orçamento. Confirme o pagamento para concluir.
        </div>
      )}

      <div className="flex items-center gap-2 text-sm">
        {etapas.map((label, i) => (
          <span key={label}
            className={`px-3 py-1 rounded-full ${etapa === i + 1
              ? 'bg-primary text-primary-foreground'
              : 'bg-muted text-muted-foreground'}`}>
            {label}
          </span>
        ))}
      </div>

      {etapa === 1 && !vendaIdParam && (
        <div className="grid grid-cols-2 gap-6">
          <div className="space-y-3">
            <h2 className="font-semibold">Adicionar produtos</h2>
            <SeletorProduto onAdd={adicionarItem} />
          </div>
          <div className="space-y-3">
            <h2 className="font-semibold">Pedido</h2>
            <ResumoPedido
              itens={itens} desconto={desconto}
              onChangeQuantidade={alterarQuantidade}
              onRemover={removerItem}
              onChangeDesconto={setDesconto}
            />
            <Button
              className="w-full"
              disabled={itens.length === 0}
              onClick={() => { setEtapa(2); listClientes() }}>
              Próximo →
            </Button>
          </div>
        </div>
      )}

      {etapa === 2 && (
        <div className="max-w-md space-y-4">
          <h2 className="font-semibold text-lg">Forma de pagamento</h2>

          {vendaIdParam && itens.length > 0 && (
            <div className="rounded-md border p-4 space-y-2">
              <p className="text-sm font-medium">Itens da venda</p>
              {itens.map(i => (
                <div key={i.produtoId} className="flex justify-between text-sm text-muted-foreground">
                  <span>{i.produtoNome} × {i.quantidade}</span>
                  <span>{i.total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                </div>
              ))}
            </div>
          )}

          {!vendaIdParam && (
            <div className="grid gap-2">
              <Label>Cliente (opcional)</Label>
              <select
                value={clienteId}
                onChange={e => setClienteId(e.target.value)}
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
                <option value="">Sem cliente (balcão)</option>
                {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
            </div>
          )}

          <div className="grid gap-2">
            <Label>Forma de pagamento</Label>
            <div className="grid grid-cols-4 gap-2">
              {(['Dinheiro', 'Pix', 'Cartao', 'Outro'] as const).map(f => (
                <Button key={f} type="button"
                  variant={formaPagamento === f ? 'default' : 'outline'}
                  onClick={() => setFormaPagamento(f)}>
                  {f}
                </Button>
              ))}
            </div>
          </div>

          {formaPagamento === 'Cartao' && (
            <div className="grid gap-2">
              <Label>Parcelas</Label>
              <Input type="number" min="1" max="12" value={parcelas}
                onChange={e => setParcelas(Number(e.target.value))} />
            </div>
          )}

          <div className="flex gap-3 pt-4">
            {!vendaIdParam && (
              <Button variant="outline" onClick={() => setEtapa(1)}>← Voltar</Button>
            )}
            <Button className="flex-1" onClick={confirmarVenda} disabled={salvando}>
              {salvando ? 'Finalizando...' : '✓ Finalizar venda'}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
