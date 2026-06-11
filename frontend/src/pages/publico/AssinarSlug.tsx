// frontend/src/pages/publico/AssinarSlug.tsx
import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { PlanoAssinaturaListItem } from '@/types/assinaturas'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

interface EmpresaInfo {
  nomeFantasia: string | null
  logoUrl: string | null
  corPrimaria: string | null
  descricaoPublica: string | null
}

export default function AssinarSlug() {
  const { slug } = useParams<{ slug: string }>()
  const [info, setInfo] = useState<EmpresaInfo | null>(null)
  const [planos, setPlanos] = useState<PlanoAssinaturaListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState('')

  useEffect(() => {
    Promise.all([
      fetch(`${API_BASE}/public/${slug}/info`).then(r => r.ok ? r.json() as Promise<EmpresaInfo> : Promise.reject()),
      fetch(`${API_BASE}/public/${slug}/planos`).then(r => r.ok ? r.json() as Promise<PlanoAssinaturaListItem[]> : Promise.reject()),
    ]).then(([i, p]) => { setInfo(i); setPlanos(p) })
      .catch(() => setErro('Empresa não encontrada ou sem planos ativos.'))
      .finally(() => setLoading(false))
  }, [slug])

  const cor = info?.corPrimaria ?? '#3B82F6'

  if (loading) return <div className="flex h-screen items-center justify-center"><p className="text-gray-500">Carregando...</p></div>
  if (erro) return <div className="flex h-screen items-center justify-center"><p className="text-red-500">{erro}</p></div>

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="shadow-sm py-5" style={{ backgroundColor: cor }}>
        <div className="max-w-3xl mx-auto px-4 flex items-center gap-3">
          {info?.logoUrl && <img src={info.logoUrl} alt="Logo" className="h-10 object-contain" />}
          <div>
            <h1 className="text-white font-bold text-xl">{info?.nomeFantasia ?? 'Empresa'}</h1>
            {info?.descricaoPublica && <p className="text-white/80 text-sm">{info.descricaoPublica}</p>}
          </div>
        </div>
      </div>

      <div className="max-w-3xl mx-auto px-4 py-8 space-y-6">
        <h2 className="text-xl font-semibold text-center">Escolha seu plano</h2>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {planos.map(p => (
            <div key={p.id} className={`rounded-xl border-2 p-5 space-y-3 bg-white relative ${p.maisVendido ? 'border-primary shadow-md' : 'border-gray-200'}`}>
              {p.maisVendido && (
                <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                  <span className="text-xs bg-primary text-white px-3 py-0.5 rounded-full font-medium">⭐ Mais vendido</span>
                </div>
              )}
              <div>
                <p className="font-bold text-lg">{p.nome}</p>
                <span className="text-xs bg-gray-100 px-2 py-0.5 rounded-full">{p.nicho}</span>
              </div>
              <p className="text-3xl font-extrabold" style={{ color: cor }}>
                R$ {p.preco.toFixed(2).replace('.', ',')}
                <span className="text-sm font-normal text-gray-500">/{p.periodicidade.toLowerCase()}</span>
              </p>
              <Link to={`/assinar/${slug}/${p.id}`}>
                <button className="w-full py-2.5 rounded-lg text-white font-semibold text-sm transition-opacity hover:opacity-90"
                  style={{ backgroundColor: cor }}>
                  Assinar este plano
                </button>
              </Link>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
