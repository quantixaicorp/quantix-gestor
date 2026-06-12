import { useEffect, useState } from 'react'
import { Plus, Trash2, Pencil } from 'lucide-react'
import { useCategoriasLancamento } from '@/hooks/useCategoriasLancamento'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import type { CategoriaLancamentoResponse } from '@/types/financeiro'

type Aba = 'Despesa' | 'Receita'

export default function Categorias() {
  const { list, create, update, remove } = useCategoriasLancamento()
  const { confirm, ConfirmDialogNode } = useConfirm()

  const [aba, setAba] = useState<Aba>('Despesa')
  const [categorias, setCategorias] = useState<CategoriaLancamentoResponse[]>([])
  const [loading, setLoading] = useState(false)

  const [modalAberto, setModalAberto] = useState(false)
  const [editando, setEditando] = useState<CategoriaLancamentoResponse | null>(null)
  const [nome, setNome] = useState('')
  const [salvando, setSalvando] = useState(false)
  const [excluindo, setExcluindo] = useState<string | null>(null)

  async function carregar(tipo: Aba) {
    setLoading(true)
    try {
      const cats = await list(tipo)
      setCategorias(cats)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void carregar(aba) }, [aba])

  function abrirCriar() {
    setEditando(null)
    setNome('')
    setModalAberto(true)
  }

  function abrirEditar(cat: CategoriaLancamentoResponse) {
    setEditando(cat)
    setNome(cat.nome)
    setModalAberto(true)
  }

  function fecharModal() {
    setModalAberto(false)
    setEditando(null)
    setNome('')
  }

  async function handleSalvar() {
    if (!nome.trim()) return
    setSalvando(true)
    try {
      if (editando) {
        await update(editando.id, { nome: nome.trim() })
        toast.success('Categoria atualizada')
      } else {
        await create({ nome: nome.trim(), tipo: aba })
        toast.success('Categoria criada')
      }
      fecharModal()
      void carregar(aba)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSalvando(false)
    }
  }

  async function handleExcluir(cat: CategoriaLancamentoResponse) {
    const ok = await confirm({
      title: 'Excluir categoria?',
      description: `"${cat.nome}" será removida. Lançamentos existentes com esta categoria não poderão ser excluídos enquanto vinculados.`,
      variant: 'destructive',
    })
    if (!ok) return
    setExcluindo(cat.id)
    try {
      await remove(cat.id)
      toast.success('Categoria excluída')
      void carregar(aba)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir')
    } finally {
      setExcluindo(null) }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-2">
        <h1 className="text-2xl font-bold">Categorias de Lançamento</h1>
        <Button onClick={abrirCriar}>
          <Plus size={16} className="mr-2" /> Nova Categoria
        </Button>
      </div>

      <div className="flex gap-1 border-b">
        {(['Despesa', 'Receita'] as Aba[]).map(t => (
          <button
            key={t}
            onClick={() => setAba(t)}
            className={`px-4 py-2 text-sm font-medium transition-colors border-b-2 -mb-px ${
              aba === t
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {t}
          </button>
        ))}
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : categorias.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhuma categoria encontrada</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {categorias.map(cat => (
              <div key={cat.id} className="rounded-lg border bg-card p-4 flex items-center justify-between gap-3">
                <div className="min-w-0 flex-1">
                  <p className="font-medium truncate">{cat.nome}</p>
                  <p className="text-sm text-muted-foreground">{cat.tipo}</p>
                </div>
                <div className="flex gap-1 shrink-0">
                  <Button size="sm" variant="ghost" onClick={() => abrirEditar(cat)}>
                    <Pencil size={14} />
                  </Button>
                  <Button size="sm" variant="ghost" disabled={excluindo === cat.id}
                    onClick={() => void handleExcluir(cat)}
                    className="text-destructive hover:text-destructive hover:bg-destructive/10">
                    {excluindo === cat.id ? '...' : <Trash2 size={14} />}
                  </Button>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop: table */}
          <div className="hidden md:block overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left font-medium">Nome</th>
                  <th className="px-4 py-3 text-left font-medium">Tipo</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {categorias.map(cat => (
                  <tr key={cat.id} className="border-b">
                    <td className="px-4 py-3 font-medium">{cat.nome}</td>
                    <td className="px-4 py-3 text-muted-foreground">{cat.tipo}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2 justify-end">
                        <Button size="sm" variant="ghost" onClick={() => abrirEditar(cat)}>
                          <Pencil size={14} />
                        </Button>
                        <Button size="sm" variant="ghost" disabled={excluindo === cat.id}
                          onClick={() => void handleExcluir(cat)}
                          className="text-destructive hover:text-destructive hover:bg-destructive/10">
                          {excluindo === cat.id ? '...' : <Trash2 size={14} />}
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      <Dialog open={modalAberto} onOpenChange={open => { if (!open) fecharModal() }}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>{editando ? 'Editar Categoria' : 'Nova Categoria'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-1.5">
              <Label>Tipo</Label>
              <p className="text-sm text-muted-foreground">{editando ? editando.tipo : aba}</p>
            </div>
            <div className="space-y-1.5">
              <Label>Nome</Label>
              <Input
                value={nome}
                onChange={e => setNome(e.target.value)}
                placeholder="Ex: Aluguel"
                onKeyDown={e => { if (e.key === 'Enter') void handleSalvar() }}
                autoFocus
              />
            </div>
            <div className="flex gap-2 pt-2">
              <Button disabled={salvando || !nome.trim()} onClick={() => void handleSalvar()}>
                {salvando ? 'Salvando...' : 'Salvar'}
              </Button>
              <Button variant="outline" onClick={fecharModal}>Cancelar</Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {ConfirmDialogNode}
    </div>
  )
}
