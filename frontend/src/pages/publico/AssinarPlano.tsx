// frontend/src/pages/publico/AssinarPlano.tsx
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import type { PlanoAssinaturaResponse } from '@/types/assinaturas'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

interface EmpresaInfo { nomeFantasia: string | null; logoUrl: string | null; corPrimaria: string | null }
interface AssinarResponse { assinaturaId: string; pixQrCode: string | null; boletoUrl: string | null; valor: number; vencimento: string }

export default function AssinarPlano() {
  const { slug, planoId } = useParams<{ slug: string; planoId: string }>()
  const [info, setInfo] = useState<EmpresaInfo | null>(null)
  const [plano, setPlano] = useState<PlanoAssinaturaResponse | null>(null)
  const [form, setForm] = useState({ nome: '', whatsapp: '', email: '' })
  const [resultado, setResultado] = useState<AssinarResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [erro, setErro] = useState('')

  const cor = info?.corPrimaria ?? '#3B82F6'

  useEffect(() => {
    Promise.all([
      fetch(`${API_BASE}/public/${slug}/info`).then(r => r.json() as Promise<EmpresaInfo>),
      fetch(`${API_BASE}/public/${slug}/planos/${planoId}`).then(r => r.json() as Promise<PlanoAssinaturaResponse>),
    ]).then(([i, p]) => { setInfo(i); setPlano(p) })
      .catch(() => setErro('Plano não encontrado.'))
      .finally(() => setLoading(false))
  }, [slug, planoId])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setErro('')
    try {
      const res = await fetch(`${API_BASE}/public/${slug}/planos/${planoId}/assinar`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ nome: form.nome, whatsapp: form.whatsapp, email: form.email || null }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => null) as { error?: string } | null
        throw new Error(body?.error ?? 'Erro ao processar assinatura.')
      }
      setResultado(await res.json() as AssinarResponse)
    } catch (e) {
      setErro(e instanceof Error ? e.message : 'Erro')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <div className="flex h-screen items-center justify-center"><p>Carregando...</p></div>
  if (!plano) return <div className="flex h-screen items-center justify-center"><p className="text-red-500">{erro || 'Plano não encontrado.'}</p></div>

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="py-4 shadow-sm" style={{ backgroundColor: cor }}>
        <div className="max-w-lg mx-auto px-4 flex items-center gap-3">
          {info?.logoUrl && <img src={info.logoUrl} alt="Logo" className="h-8 object-contain" />}
          <p className="text-white font-semibold">{info?.nomeFantasia ?? ''}</p>
        </div>
      </div>

      <div className="max-w-lg mx-auto px-4 py-8 space-y-6">
        <div className="rounded-xl border bg-white p-5 space-y-2">
          <p className="font-bold text-lg">{plano.nome}</p>
          {plano.descricao && <p className="text-sm text-gray-500">{plano.descricao}</p>}
          <p className="text-2xl font-extrabold" style={{ color: cor }}>
            R$ {plano.preco.toFixed(2).replace('.', ',')}
            <span className="text-sm font-normal text-gray-500">/{plano.periodicidade.toLowerCase()}</span>
          </p>
          <ul className="text-sm space-y-1 pt-1">
            {plano.itens.map(i => (
              <li key={i.id} className="flex gap-1">
                <span style={{ color: cor }}>✓</span>
                {i.quantidadePorCiclo === 0 ? `${i.descricao} (ilimitado)` : `${i.quantidadePorCiclo}x ${i.descricao}`}
              </li>
            ))}
          </ul>
        </div>

        {!resultado ? (
          <form onSubmit={e => void handleSubmit(e)} className="bg-white rounded-xl border p-5 space-y-4">
            <h2 className="font-semibold">Seus dados</h2>
            <div className="space-y-3">
              <div>
                <label className="text-sm font-medium block mb-1">Nome completo</label>
                <input required value={form.nome} onChange={e => setForm(f => ({ ...f, nome: e.target.value }))}
                  className="w-full border rounded-lg px-3 py-2.5 text-base outline-none focus:ring-2" style={{ fontSize: '16px' }}
                  placeholder="João Silva" />
              </div>
              <div>
                <label className="text-sm font-medium block mb-1">WhatsApp</label>
                <input required value={form.whatsapp} onChange={e => setForm(f => ({ ...f, whatsapp: e.target.value }))}
                  className="w-full border rounded-lg px-3 py-2.5 text-base outline-none focus:ring-2" style={{ fontSize: '16px' }}
                  placeholder="11999990000" type="tel" />
              </div>
              <div>
                <label className="text-sm font-medium block mb-1">E-mail (opcional)</label>
                <input value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                  className="w-full border rounded-lg px-3 py-2.5 text-base outline-none focus:ring-2" style={{ fontSize: '16px' }}
                  placeholder="joao@email.com" type="email" />
              </div>
            </div>
            {erro && <p className="text-red-500 text-sm">{erro}</p>}
            <button type="submit" disabled={submitting}
              className="w-full py-3 rounded-lg text-white font-semibold text-base disabled:opacity-60"
              style={{ backgroundColor: cor, minHeight: '48px' }}>
              {submitting ? 'Processando...' : 'Confirmar Assinatura'}
            </button>
          </form>
        ) : (
          <div className="bg-white rounded-xl border p-5 space-y-4">
            <div className="text-center space-y-1">
              <p className="text-2xl">🎉</p>
              <h2 className="font-bold text-lg">Assinatura confirmada!</h2>
              <p className="text-sm text-gray-500">Realize o pagamento para ativar seu plano.</p>
            </div>

            {resultado.pixQrCode && (
              <div className="space-y-3">
                <p className="font-medium text-center text-sm">Pague via PIX</p>
                <div className="flex justify-center">
                  <img
                    src={`data:image/png;base64,${resultado.pixQrCode}`}
                    alt="QR Code PIX"
                    className="w-48 h-48 rounded border p-2"
                  />
                </div>
                <button onClick={() => void navigator.clipboard.writeText(resultado.pixQrCode ?? '')}
                  className="w-full border rounded-lg py-2.5 text-sm font-medium"
                  style={{ minHeight: '44px' }}>
                  Copiar código PIX
                </button>
              </div>
            )}

            {resultado.boletoUrl && (
              <a href={resultado.boletoUrl} target="_blank" rel="noopener noreferrer"
                className="block text-center text-sm underline text-gray-500">
                Pagar com Boleto
              </a>
            )}

            <p className="text-xs text-center text-gray-400">
              Vencimento: {new Date(resultado.vencimento).toLocaleDateString('pt-BR')} •
              R$ {resultado.valor.toFixed(2).replace('.', ',')}
            </p>
          </div>
        )}
      </div>
    </div>
  )
}
