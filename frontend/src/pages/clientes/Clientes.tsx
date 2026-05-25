import { useEffect, useState } from 'react'
import { Plus, Search } from 'lucide-react'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ClienteForm from '@/components/clientes/ClienteForm'
import type { CreateClienteRequest } from '@/types/clientes'

export default function Clientes() {
  const { clientes, loading, list, create } = useClientes()
  const [busca, setBusca] = useState('')
  const [modalAberto, setModalAberto] = useState(false)

  useEffect(() => { list() }, [list])

  async function handleCreate(data: CreateClienteRequest) {
    await create(data)
    setModalAberto(false)
  }

  const filtrados = clientes.filter(c =>
    c.nome.toLowerCase().includes(busca.toLowerCase()) ||
    c.whatsapp.includes(busca))

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Clientes</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Cliente
        </Button>
      </div>

      <div className="relative max-w-sm">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Buscar por nome ou WhatsApp..."
          value={busca}
          onChange={e => setBusca(e.target.value)}
        />
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nome</th>
                <th className="px-4 py-3 text-left font-medium">WhatsApp</th>
                <th className="px-4 py-3 text-left font-medium">E-mail</th>
                <th className="px-4 py-3 text-left font-medium">Cadastrado em</th>
              </tr>
            </thead>
            <tbody>
              {filtrados.map(c => (
                <tr key={c.id} className="border-b hover:bg-muted/30 cursor-pointer">
                  <td className="px-4 py-3 font-medium">{c.nome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{c.whatsapp}</td>
                  <td className="px-4 py-3 text-muted-foreground">{c.email ?? '—'}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(c.dataCadastro).toLocaleDateString('pt-BR')}
                  </td>
                </tr>
              ))}
              {filtrados.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum cliente encontrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={modalAberto} onOpenChange={setModalAberto}>
        <DialogContent>
          <DialogHeader><DialogTitle>Novo Cliente</DialogTitle></DialogHeader>
          <ClienteForm onSubmit={handleCreate} onCancel={() => setModalAberto(false)} />
        </DialogContent>
      </Dialog>
    </div>
  )
}
