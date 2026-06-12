// frontend/src/pages/orcamentos/NovoOrcamento.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Trash2 } from 'lucide-react'
import { useOrcamentos } from '@/hooks/useOrcamentos'
import { useEstoque } from '@/hooks/useEstoque'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { OrcamentoItemRequest } from '@/types/orcamento'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function NovoOrcamento() {
  const navigate = useNavigate()
  const { create } = useOrcamentos()
  const { produtos, listProdutos } = useEstoque()
  const { clientes, list: listClientes } = useClientes()

  const [titulo, setTitulo] = useState('')
  const [clienteId, setClienteId] = useState('')
  const [dataValidade, setDataValidade] = useState('')
  const [observacao, setObservacao] = useState('')
  const [itens, setItens] = useState<OrcamentoItemRequest[]>([])
  const [salvando, setSalvando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  useEffect(() => {
    void listProdutos()
    void listClientes()
  }, [listProdutos, listClientes])

  function adicionarProduto(produtoId: string) {
    if (!produtoId) return
    const produto = produtos.find(p => p.id === produtoId)
    if (!produto) return
    const jaExiste = itens.find(i => i.produtoId === produtoId)
    if (jaExiste) return
    setItens(prev => [...prev, {
      tipo: 'Produto',
      produtoId,
      descricao: produto.nome,
      quantidade: 1,
      valorUnitario: produto.precoVenda,
    }])
  }

  function adicionarLivre() {
    setItens(prev => [...prev, { tipo: 'Livre', descricao: '', quantidade: 1, valorUnitario: 0 }])
  }

  function atualizarItem(index: number, campo: keyof OrcamentoItemRequest, valor: string | number) {
    setItens(prev => prev.map((item, i) =>
      i === index ? { ...item, [campo]: valor } : item
    ))
  }

  function removerItem(index: number) {
    setItens(prev => prev.filter((_, i) => i !== index))
  }

  const total = itens.reduce((acc, i) => acc + i.quantidade * i.valorUnitario, 0)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!titulo.trim()) { setErro('Título obrigatório'); return }
    if (!dataValidade) { setErro('Data de validade obrigatória'); return }
    if (itens.length === 0) { setErro('Adicione pelo menos um item'); return }
    setErro(null)
    setSalvando(true)
    try {
      const result = await create({
        clienteId: clienteId || undefined,
        titulo: titulo.trim(),
        dataValidade,
        observacao: observacao.trim() || undefined,
        itens,
      })
      navigate(`/orcamentos/${result.id}`)
    } catch (e) {
      setErro(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSalvando(false)
    }
  }

  return (
    <div className="max-w-3xl space-y-6">
      <h1 className="text-2xl font-bold">Novo Orçamento</h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="rounded-xl border bg-card p-6 space-y-4">
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">Dados do Orçamento</p>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="grid gap-2 sm:col-span-2">
              <Label>Título *</Label>
              <Input value={titulo} onChange={e => setTitulo(e.target.value)}
                placeholder="Ex: Orçamento - Corte e Escova" />
            </div>
            <div className="grid gap-2">
              <Label>Cliente (opcional)</Label>
              <select value={clienteId} onChange={e => setClienteId(e.target.value)}
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
                <option value="">Sem cliente</option>
                {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
            </div>
            <div className="grid gap-2">
              <Label>Válido até *</Label>
              <Input type="date" value={dataValidade} onChange={e => setDataValidade(e.target.value)} />
            </div>
            <div className="grid gap-2 sm:col-span-2">
              <Label>Observação (opcional)</Label>
              <textarea
                value={observacao}
                onChange={e => setObservacao(e.target.value)}
                placeholder="Informações adicionais para o cliente"
                rows={3}
                className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring resize-none"
              />
            </div>
          </div>
        </div>

        <div className="rounded-xl border bg-card p-6 space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">Itens</p>
            <div className="flex gap-2">
              <select onChange={e => { adicionarProduto(e.target.value); e.target.value = '' }}
                className="h-9 rounded-md border border-input bg-transparent px-3 text-sm">
                <option value="">+ Produto do estoque</option>
                {produtos.filter(p => p.ativo).map(p =>
                  <option key={p.id} value={p.id}>{p.nome} — {fmt(p.precoVenda)}</option>
                )}
              </select>
              <Button type="button" variant="outline" size="sm" onClick={adicionarLivre}>
                <Plus size={14} className="mr-1" /> Item livre
              </Button>
            </div>
          </div>

          {itens.length === 0 && (
            <p className="py-4 text-center text-sm text-muted-foreground">
              Nenhum item adicionado. Use os botões acima para adicionar.
            </p>
          )}

          <div className="overflow-x-auto rounded-md border">
            {itens.map((item, i) => (
              <div key={i} className="flex items-center gap-3 border-b px-4 py-3 last:border-0">
                <span className="w-16 rounded bg-muted px-2 py-0.5 text-center text-xs font-medium">
                  {item.tipo}
                </span>
                <Input
                  className="flex-1"
                  value={item.descricao}
                  onChange={e => atualizarItem(i, 'descricao', e.target.value)}
                  placeholder="Descrição"
                  disabled={item.tipo === 'Produto'}
                />
                <Input
                  className="w-20"
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={item.quantidade}
                  onChange={e => atualizarItem(i, 'quantidade', parseFloat(e.target.value) || 0)}
                />
                <Input
                  className="w-28"
                  type="number"
                  min="0"
                  step="0.01"
                  value={item.valorUnitario}
                  onChange={e => atualizarItem(i, 'valorUnitario', parseFloat(e.target.value) || 0)}
                />
                <span className="w-28 text-right text-sm font-medium">
                  {fmt(item.quantidade * item.valorUnitario)}
                </span>
                <Button type="button" variant="ghost" size="sm" onClick={() => removerItem(i)}>
                  <Trash2 size={14} />
                </Button>
              </div>
            ))}
            {itens.length > 0 && (
              <div className="flex justify-end px-4 py-3 font-semibold">
                Total: {fmt(total)}
              </div>
            )}
          </div>
        </div>

        {erro && <p className="text-sm text-destructive">{erro}</p>}

        <div className="flex gap-3">
          <Button type="button" variant="outline" onClick={() => navigate('/orcamentos')}>
            Cancelar
          </Button>
          <Button type="submit" disabled={salvando}>
            {salvando ? 'Salvando...' : 'Salvar Rascunho'}
          </Button>
        </div>
      </form>
    </div>
  )
}
