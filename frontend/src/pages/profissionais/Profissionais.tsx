import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Trash2 } from 'lucide-react'
import { useProfissionais } from '@/hooks/useProfissionais'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import type { ProfissionalResponse } from '@/types/agendamento'
import { KpiRow } from '@/components/ui/KpiRow'

export default function Profissionais() {
  const { profissionais, loading, error, list, create, update, remove } = useProfissionais()
  const navigate = useNavigate()
  const [showForm, setShowForm] = useState(false)
  const [editando, setEditando] = useState<ProfissionalResponse | null>(null)
  const [nome, setNome] = useState('')
  const [telefone, setTelefone] = useState('')
  const [ativo, setAtivo] = useState(true)
  const [saving, setSaving] = useState(false)
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => { void list() }, [list])

  function abrirNovo() {
    setEditando(null)
    setNome('')
    setTelefone('')
    setAtivo(true)
    setShowForm(true)
  }

  function abrirEditar(p: ProfissionalResponse) {
    setEditando(p)
    setNome(p.nome)
    setTelefone(p.telefone ?? '')
    setAtivo(p.ativo)
    setShowForm(true)
  }

  async function salvar() {
    if (!nome.trim()) return
    setSaving(true)
    try {
      if (editando) {
        await update(editando.id, { nome: nome.trim(), telefone: telefone.trim() || undefined, ativo })
      } else {
        await create({ nome: nome.trim(), telefone: telefone.trim() || undefined })
      }
      setShowForm(false)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  async function excluir(id: string) {
    const ok = await confirm({ title: 'Excluir profissional?', variant: 'destructive' })
    if (!ok) return
    try { await remove(id) } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro ao excluir') }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>
  if (error) return <p className="text-destructive">{error}</p>

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Profissionais</h1>
        <Button onClick={abrirNovo}>Novo Profissional</Button>
      </div>

      <KpiRow items={[
        { label: 'Total', value: String(profissionais.length) },
        { label: 'Ativos', value: String(profissionais.filter(p => p.ativo).length), color: 'text-green-600 dark:text-green-400' },
        { label: 'Inativos', value: String(profissionais.filter(p => !p.ativo).length), color: 'text-muted-foreground' },
        { label: 'Com telefone', value: String(profissionais.filter(p => p.telefone).length) },
      ]} />

      {showForm && (
        <div className="rounded-md border p-4 space-y-4 max-w-md">
          <h2 className="font-semibold">{editando ? 'Editar' : 'Novo'} Profissional</h2>
          <div className="space-y-2">
            <Label>Nome *</Label>
            <Input value={nome} onChange={e => setNome(e.target.value)} placeholder="Nome completo" />
          </div>
          <div className="space-y-2">
            <Label>Telefone / WhatsApp</Label>
            <Input value={telefone} onChange={e => setTelefone(e.target.value)} placeholder="(11) 99999-9999" />
          </div>
          {editando && (
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="ativo"
                checked={ativo}
                onChange={e => setAtivo(e.target.checked)}
                className="h-4 w-4"
              />
              <Label htmlFor="ativo">Ativo</Label>
            </div>
          )}
          <div className="flex gap-2">
            <Button onClick={salvar} disabled={saving || !nome.trim()}>
              {saving ? '...' : 'Salvar'}
            </Button>
            <Button variant="ghost" onClick={() => setShowForm(false)}>Cancelar</Button>
          </div>
        </div>
      )}

      {profissionais.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhum profissional cadastrado.</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {profissionais.map(p => (
              <div key={p.id} className="rounded-lg border bg-card p-4 space-y-3">
                <div className="flex items-center justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <p className="font-medium truncate">{p.nome}</p>
                    {p.telefone && <p className="text-sm text-muted-foreground">{p.telefone}</p>}
                  </div>
                  <span className={p.ativo
                    ? 'rounded-full bg-green-100 px-2 py-0.5 text-xs text-green-700 shrink-0'
                    : 'rounded-full bg-gray-100 px-2 py-0.5 text-xs text-gray-500 shrink-0'}>
                    {p.ativo ? 'Ativo' : 'Inativo'}
                  </span>
                </div>
                <div className="flex gap-1">
                  <Button variant="outline" size="sm" className="flex-1" onClick={() => abrirEditar(p)}>Editar</Button>
                  <Button variant="outline" size="sm" className="flex-1"
                    onClick={() => navigate(`/profissionais/${p.id}/disponibilidade`)}>
                    Disponibilidade
                  </Button>
                  <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive hover:bg-destructive/10"
                    onClick={() => excluir(p.id)}>
                    <Trash2 size={14} />
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
                  <th className="px-4 py-3 text-left font-medium">Telefone</th>
                  <th className="px-4 py-3 text-left font-medium">Status</th>
                  <th className="px-4 py-3 text-left font-medium">Ações</th>
                </tr>
              </thead>
              <tbody>
                {profissionais.map(p => (
                  <tr key={p.id} className="border-b">
                    <td className="px-4 py-3 font-medium">{p.nome}</td>
                    <td className="px-4 py-3 text-muted-foreground">{p.telefone ?? '—'}</td>
                    <td className="px-4 py-3">
                      <span className={p.ativo
                        ? 'rounded-full bg-green-100 px-2 py-0.5 text-xs text-green-700'
                        : 'rounded-full bg-gray-100 px-2 py-0.5 text-xs text-gray-500'}>
                        {p.ativo ? 'Ativo' : 'Inativo'}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-2">
                        <Button variant="ghost" size="sm" onClick={() => abrirEditar(p)}>Editar</Button>
                        <Button variant="ghost" size="sm"
                          onClick={() => navigate(`/profissionais/${p.id}/disponibilidade`)}>
                          Disponibilidade
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => excluir(p.id)}>Excluir</Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
      {ConfirmDialogNode}
    </div>
  )
}
