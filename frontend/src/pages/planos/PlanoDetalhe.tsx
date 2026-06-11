// frontend/src/pages/planos/PlanoDetalhe.tsx
import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { toast } from '@/hooks/useToast'
import type { PlanoAssinaturaResponse } from '@/types/assinaturas'

export default function PlanoDetalhe() {
  const { id } = useParams<{ id: string }>()
  const [plano, setPlano] = useState<PlanoAssinaturaResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [slug, setSlug] = useState('')

  useEffect(() => {
    Promise.all([
      api.get<PlanoAssinaturaResponse>(`/api/planos-assinatura/${id}`),
      api.get<{ slug: string | null }>('/api/configuracao-empresa'),
    ]).then(([p, cfg]) => {
      setPlano(p)
      setSlug(cfg.slug ?? '')
    }).finally(() => setLoading(false))
  }, [id])

  if (loading) return <p className="text-sm text-muted-foreground">Carregando...</p>
  if (!plano) return <p className="text-red-500">Plano não encontrado.</p>

  const checkoutUrl = `${window.location.origin}/assinar/${slug}/${plano.id}`

  async function toggleAtivo() {
    try {
      await api.put(`/api/planos-assinatura/${plano!.id}`, { ...plano, ativo: !plano!.ativo })
      setPlano(p => p ? { ...p, ativo: !p.ativo } : p)
      toast.success(plano!.ativo ? 'Plano desativado' : 'Plano ativado')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    }
  }

  return (
    <div className="max-w-xl space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <Link to="/planos" className="text-sm text-muted-foreground hover:underline">← Planos</Link>
          <h1 className="text-2xl font-bold">{plano.nome}</h1>
          <span className="text-sm bg-muted px-2 py-0.5 rounded-full">{plano.nicho}</span>
        </div>
        <Button variant={plano.ativo ? 'outline' : 'default'} size="sm" onClick={() => void toggleAtivo()}>
          {plano.ativo ? 'Desativar' : 'Ativar'}
        </Button>
      </div>

      <div className="rounded-lg border p-4 space-y-2">
        <p className="text-3xl font-bold">
          R$ {plano.preco.toFixed(2).replace('.', ',')}
          <span className="text-sm font-normal text-muted-foreground">/{plano.periodicidade.toLowerCase()}</span>
        </p>
        {plano.descricao && <p className="text-sm text-muted-foreground">{plano.descricao}</p>}
        <p className="text-sm">{plano.totalAssinantes} assinante(s) ativo(s)</p>
      </div>

      {plano.itens.length > 0 && (
        <div className="space-y-2">
          <p className="font-medium">Itens do plano</p>
          <ul className="space-y-1">
            {plano.itens.map(i => (
              <li key={i.id} className="flex items-center gap-2 text-sm">
                <span className="text-green-500">✓</span>
                {i.quantidadePorCiclo === 0 ? `${i.descricao} (ilimitado)` : `${i.quantidadePorCiclo}x ${i.descricao}`}
                {i.tipo === 'Desconto' && i.percentualDesconto && ` — ${i.percentualDesconto}% desconto`}
              </li>
            ))}
          </ul>
        </div>
      )}

      {slug && (
        <div className="rounded-lg border p-4 space-y-2">
          <p className="font-medium text-sm">Link público de assinatura</p>
          <div className="flex items-center gap-2">
            <code className="text-xs bg-muted px-2 py-1 rounded flex-1 break-all">{checkoutUrl}</code>
            <Button size="sm" variant="outline"
              onClick={() => { void navigator.clipboard.writeText(checkoutUrl); toast.success('Copiado!') }}>
              Copiar
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
