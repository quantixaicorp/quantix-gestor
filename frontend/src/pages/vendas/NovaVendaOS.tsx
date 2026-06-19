import { useState, useEffect, useRef } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { CheckCircle2, ArrowLeft, Plus } from 'lucide-react'
import { useVendas } from '@/hooks/useVendas'
import { useClientes } from '@/hooks/useClientes'
import { useProfissionais } from '@/hooks/useProfissionais'
import { useEstoque } from '@/hooks/useEstoque'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ClienteForm from '@/components/clientes/ClienteForm'
import ResumoPedido from '@/components/vendas/ResumoPedido'
import { toast } from '@/hooks/useToast'
import type { ItemCarrinho, CreateVendaRequest, FecharVendaRequest } from '@/types/vendas'
import type { CreateClienteRequest } from '@/types/clientes'

const FORMAS = [
  { value: 'Pix',      label: 'Pix',     emoji: '⚡' },
  { value: 'Dinheiro', label: 'Dinheiro', emoji: '💵' },
  { value: 'Cartao',   label: 'Cartão',   emoji: '💳' },
  { value: 'Outro',    label: 'Outro',    emoji: '🔖' },
] as const

type FormaPagamento = typeof FORMAS[number]['value']

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function NovaVendaOS() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const vendaIdParam = searchParams.get('vendaId')
  const origemParam = searchParams.get('origem')

  const { create, fechar, get } = useVendas()
  const { clientes, list: listClientes, create: createCliente } = useClientes()
  const { profissionais, list: listProfissionais } = useProfissionais()
  const { produtos, listProdutos } = useEstoque()

  const [itens, setItens] = useState<ItemCarrinho[]>([])
  const [desconto, setDesconto] = useState(0)
  const [clienteId, setClienteId] = useState('')
  const [profissionalId, setProfissionalId] = useState('')
  const [observacaoOS, setObservacaoOS] = useState('')
  const [formaPagamento, setFormaPagamento] = useState<FormaPagamento>('Pix')
  const [parcelas, setParcelas] = useState(1)
  const [salvando, setSalvando] = useState(false)
  const [vendaFinalizada, setVendaFinalizada] = useState<{ id: string; total: number } | null>(null)
  const [carregando, setCarregando] = useState(false)
  const [modalNovoCliente, setModalNovoCliente] = useState(false)
  const vendaCarregadaRef = useRef<import('@/types/vendas').VendaResponse | null>(null)
  const [busca, setBusca] = useState('')
  const hoje = new Date().toISOString().slice(0, 10)
  const [dataExecucao, setDataExecucao] = useState(hoje)

  const servicos = produtos.filter(p => p.ativo && p.tipo === 'Servico')
  const servicosFiltrados = servicos.filter(p =>
    !busca || p.nome.toLowerCase().includes(busca.toLowerCase()))

  useEffect(() => {
    void listClientes()
    void listProfissionais()
    void listProdutos()
    if (!vendaIdParam) return
    setCarregando(true)
    void get(vendaIdParam).then(venda => {
      vendaCarregadaRef.current = venda
      setItens(venda.itens.map(i => ({
        produtoId: i.produtoId,
        produtoNome: i.produtoNome,
        precoUnitario: i.precoUnitario,
        quantidade: i.quantidade,
        desconto: 0,
        total: i.precoUnitario * i.quantidade,
      })))
      if (venda.clienteId) setClienteId(venda.clienteId)
    }).finally(() => setCarregando(false))
  }, [vendaIdParam, get, listClientes, listProfissionais, listProdutos])

  useEffect(() => {
    if (!vendaIdParam || profissionais.length === 0 || !vendaCarregadaRef.current) return
    const nome = vendaCarregadaRef.current.profissionalNome
    if (!nome) return
    const prof = profissionais.find(p => p.nome === nome)
    if (prof) setProfissionalId(prof.id)
  }, [profissionais, vendaIdParam])

  function adicionarItem(produtoId: string) {
    const p = produtos.find(x => x.id === produtoId)
    if (!p) return
    setItens(prev => {
      const existente = prev.find(i => i.produtoId === p.id)
      if (existente)
        return prev.map(i => i.produtoId === p.id
          ? { ...i, quantidade: i.quantidade + 1, total: i.precoUnitario * (i.quantidade + 1) }
          : i)
      return [...prev, {
        produtoId: p.id, produtoNome: p.nome,
        precoUnitario: p.precoVenda, quantidade: 1,
        desconto: 0, total: p.precoVenda,
      }]
    })
    setBusca('')
  }

  async function handleCriarCliente(data: CreateClienteRequest) {
    try {
      const novoCliente = await createCliente(data)
      setClienteId(novoCliente.id)
      setModalNovoCliente(false)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao criar cliente')
    }
  }

  async function confirmarOS() {
    if (itens.length === 0) { toast.error('Adicione ao menos um serviço'); return }
    if (!clienteId) { toast.error('Selecione um cliente'); return }
    setSalvando(true)
    try {
      if (vendaIdParam) {
        const req: FecharVendaRequest = {
          formaPagamento,
          parcelas: formaPagamento === 'Cartao' ? parcelas : undefined,
          observacao: observacaoOS || undefined,
        }
        const result = await fechar(vendaIdParam, req)
        setVendaFinalizada({ id: result.id, total: result.total })
      } else {
        const req: CreateVendaRequest = {
          clienteId,
          itens: itens.map(i => ({ produtoId: i.produtoId, quantidade: i.quantidade, desconto: 0 })),
          desconto,
          formaPagamento,
          parcelas: formaPagamento === 'Cartao' ? parcelas : undefined,
          observacaoOS: observacaoOS || undefined,
          profissionalId: profissionalId || undefined,
          dataHora: dataExecucao !== hoje
            ? new Date(dataExecucao + 'T12:00:00').toISOString()
            : undefined,
        }
        const result = await create(req)
        setVendaFinalizada({ id: result.id, total: result.total })
      }
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao finalizar OS')
    } finally {
      setSalvando(false)
    }
  }

  const subtotal = itens.reduce((s, i) => s + i.precoUnitario * i.quantidade, 0)
  const total = Math.max(0, subtotal - desconto)

  if (carregando)
    return <div className="flex h-64 items-center justify-center text-muted-foreground">Carregando...</div>

  if (vendaFinalizada) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="text-center space-y-5 max-w-sm">
          <CheckCircle2 size={64} className="mx-auto text-green-500" />
          <div>
            <h2 className="text-2xl font-bold">OS finalizada!</h2>
            <p className="text-muted-foreground mt-1">
              Total cobrado: <span className="font-semibold text-foreground">{fmt(vendaFinalizada.total)}</span>
            </p>
          </div>
          <div className="flex flex-col gap-2">
            <Button onClick={() => {
              setItens([]); setDesconto(0); setClienteId('')
              setProfissionalId(''); setObservacaoOS(''); setVendaFinalizada(null)
            }}>
              Nova OS
            </Button>
            <Button variant="outline" onClick={() => navigate('/vendas')}>
              Ver histórico
            </Button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-4 h-full">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate('/vendas')}
          className="text-muted-foreground hover:text-foreground transition-colors">
          <ArrowLeft size={20} />
        </button>
        <h1 className="text-xl font-bold">
          {!vendaIdParam ? 'Nova Ordem de Serviço'
            : origemParam === 'agendamento' ? 'Finalizar Agendamento'
            : 'Finalizar Orçamento'}
        </h1>
      </div>

      {vendaIdParam && (
        <div className="rounded-lg border border-blue-200 bg-blue-50 dark:bg-blue-950/30 dark:border-blue-800 px-4 py-2.5 text-sm text-blue-700 dark:text-blue-300">
          {origemParam === 'agendamento'
            ? 'Venda gerada a partir de agendamento — confirme o pagamento para concluir.'
            : 'Venda gerada a partir de orçamento — confirme o pagamento para concluir.'}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-[1fr_380px] gap-5 items-start">

        {/* Coluna esquerda: seletor de serviços + carrinho */}
        <div className="space-y-4">
          {!vendaIdParam && (
            <div className="rounded-xl border bg-card shadow-sm p-4 space-y-3">
              <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
                Adicionar serviço
              </p>
              <input
                type="text"
                placeholder="Buscar serviço..."
                value={busca}
                onChange={e => setBusca(e.target.value)}
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 text-sm outline-none placeholder:text-muted-foreground"
              />
              {busca && (
                <div className="rounded-md border divide-y max-h-48 overflow-y-auto">
                  {servicosFiltrados.length === 0
                    ? <p className="px-4 py-3 text-sm text-muted-foreground">Nenhum serviço encontrado</p>
                    : servicosFiltrados.map(p => (
                        <button key={p.id} type="button"
                          className="w-full flex items-center justify-between px-4 py-2 text-left hover:bg-accent text-sm"
                          onClick={() => adicionarItem(p.id)}>
                          <span>{p.nome}</span>
                          <span className="text-muted-foreground">{fmt(p.precoVenda)}</span>
                        </button>
                      ))}
                </div>
              )}
            </div>
          )}

          <div className="rounded-xl border bg-card shadow-sm p-4">
            <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-3">
              Serviços
            </p>
            <ResumoPedido
              itens={itens}
              desconto={desconto}
              onChangeQuantidade={(produtoId, quantidade) => {
                if (quantidade <= 0) return
                setItens(prev => prev.map(i => i.produtoId === produtoId
                  ? { ...i, quantidade, total: i.precoUnitario * quantidade }
                  : i))
              }}
              onRemover={id => setItens(prev => prev.filter(i => i.produtoId !== id))}
              onChangeDesconto={setDesconto}
            />
          </div>
        </div>

        {/* Coluna direita: OS + pagamento */}
        <div className="rounded-xl border bg-card shadow-sm p-5 space-y-5 lg:sticky lg:top-4">
          <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
            Ordem de Serviço
          </p>

          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">Cliente *</Label>
            <div className="flex items-center gap-2">
              <select value={clienteId} onChange={e => setClienteId(e.target.value)}
                className="flex h-9 flex-1 rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
                <option value="">Selecione o cliente...</option>
                {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
              <Button type="button" variant="outline" size="icon"
                title="Criar novo cliente"
                onClick={() => setModalNovoCliente(true)}>
                <Plus size={16} />
              </Button>
            </div>
            {!clienteId && (
              <p className="text-xs text-destructive">Cliente obrigatório</p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">Profissional responsável</Label>
            <select value={profissionalId} onChange={e => setProfissionalId(e.target.value)}
              className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
              <option value="">Selecione o profissional...</option>
              {profissionais.filter(p => p.ativo).map(p =>
                <option key={p.id} value={p.id}>{p.nome}</option>
              )}
            </select>
          </div>

          {!vendaIdParam && (
            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground">Data de execução</Label>
              <input
                type="date"
                value={dataExecucao}
                max={hoje}
                onChange={e => setDataExecucao(e.target.value)}
                className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm"
              />
            </div>
          )}

          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">Observação do serviço (opcional)</Label>
            <textarea
              value={observacaoOS}
              onChange={e => setObservacaoOS(e.target.value)}
              placeholder="Diagnóstico, procedimento realizado..."
              rows={2}
              className="flex w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm resize-none placeholder:text-muted-foreground outline-none focus:ring-1 focus:ring-ring"
            />
          </div>

          <hr className="border-border" />

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

          <div className="rounded-lg bg-primary/5 border border-primary/20 px-4 py-3 flex justify-between items-center">
            <span className="text-sm font-medium text-muted-foreground">Total</span>
            <span className="text-3xl font-bold text-primary tabular-nums">{fmt(total)}</span>
          </div>

          <Button
            className="w-full h-12 text-base font-semibold"
            disabled={itens.length === 0 || !clienteId || salvando}
            onClick={confirmarOS}>
            {salvando ? 'Finalizando...' : '✓ Finalizar OS'}
          </Button>

          {itens.length === 0 && (
            <p className="text-xs text-center text-muted-foreground">
              Adicione ao menos um serviço para finalizar
            </p>
          )}
        </div>
      </div>

      <Dialog open={modalNovoCliente} onOpenChange={setModalNovoCliente}>
        <DialogContent>
          <DialogHeader><DialogTitle>Novo Cliente</DialogTitle></DialogHeader>
          <ClienteForm
            onSubmit={handleCriarCliente}
            onCancel={() => setModalNovoCliente(false)}
          />
        </DialogContent>
      </Dialog>
    </div>
  )
}
