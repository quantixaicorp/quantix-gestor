// frontend/src/pages/planos/PlanosList.tsx
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import type { PlanoAssinaturaListItem } from '@/types/assinaturas'

export default function PlanosList() {
  const [planos, setPlanos] = useState<PlanoAssinaturaListItem[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.get<PlanoAssinaturaListItem[]>('/api/planos-assinatura')
      .then(setPlanos)
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <p className="text-sm text-muted-foreground">Carregando...</p>

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Planos de Assinatura</h1>
        <Button asChild><Link to="/planos/novo">Novo Plano</Link></Button>
      </div>

      {planos.length === 0 && (
        <p className="text-muted-foreground text-sm">Nenhum plano criado ainda.</p>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {planos.map(p => (
          <Link key={p.id} to={`/planos/${p.id}`}
            className="rounded-lg border p-4 hover:shadow-md transition-shadow space-y-2">
            <div className="flex items-start justify-between">
              <div>
                <p className="font-semibold">{p.nome}</p>
                <span className="text-xs bg-muted px-2 py-0.5 rounded-full">{p.nicho}</span>
              </div>
              {p.maisVendido && (
                <span className="text-xs bg-amber-100 text-amber-800 px-2 py-0.5 rounded-full">Mais vendido</span>
              )}
            </div>
            <p className="text-2xl font-bold">
              R$ {p.preco.toFixed(2).replace('.', ',')}
              <span className="text-sm font-normal text-muted-foreground">/{p.periodicidade.toLowerCase()}</span>
            </p>
            <p className="text-sm text-muted-foreground">{p.totalAssinantes} assinante(s) ativo(s)</p>
            {!p.ativo && <span className="text-xs text-red-500">Inativo</span>}
          </Link>
        ))}
      </div>
    </div>
  )
}
