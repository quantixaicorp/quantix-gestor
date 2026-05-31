import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useProfissionais } from '@/hooks/useProfissionais'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { toast } from '@/hooks/useToast'
import type { DisponibilidadeItem } from '@/types/agendamento'

const DIAS = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb']

export default function DisponibilidadeProfissional() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { getDisponibilidade, saveDisponibilidade } = useProfissionais()
  const [faixas, setFaixas] = useState<DisponibilidadeItem[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    void getDisponibilidade(id).then(data => {
      setFaixas(data)
      setLoading(false)
    }).catch(() => setLoading(false))
  }, [getDisponibilidade, id])

  function adicionarFaixa(dia: number) {
    setFaixas(prev => [...prev, { diaSemana: dia, horaInicio: '08:00', horaFim: '18:00' }])
  }

  function removerFaixa(index: number) {
    setFaixas(prev => prev.filter((_, i) => i !== index))
  }

  function atualizarFaixa(index: number, field: keyof DisponibilidadeItem, value: string | number) {
    setFaixas(prev => prev.map((f, i) => i === index ? { ...f, [field]: value } : f))
  }

  async function salvar() {
    if (!id) return
    setSaving(true)
    try {
      await saveDisponibilidade(id, faixas)
      toast.success('Disponibilidade salva!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>

  return (
    <div className="space-y-6 max-w-2xl">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Disponibilidade Semanal</h1>
        <Button variant="ghost" onClick={() => navigate('/profissionais')}>← Voltar</Button>
      </div>

      <div className="space-y-4">
        {DIAS.map((dia, diaIdx) => {
          const faixasDia = faixas.filter(f => f.diaSemana === diaIdx)
          return (
            <div key={diaIdx} className="rounded-md border p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="font-medium">{dia}</span>
                <Button variant="outline" size="sm" onClick={() => adicionarFaixa(diaIdx)}>
                  + Faixa
                </Button>
              </div>
              {faixasDia.length === 0 && (
                <p className="text-sm text-muted-foreground">Sem horários</p>
              )}
              {faixasDia.map((faixa) => {
                const idx = faixas.indexOf(faixa)
                return (
                  <div key={idx} className="flex items-center gap-2 mt-2">
                    <Input
                      type="time"
                      value={faixa.horaInicio}
                      onChange={e => atualizarFaixa(idx, 'horaInicio', e.target.value)}
                      className="w-32"
                    />
                    <span className="text-muted-foreground">até</span>
                    <Input
                      type="time"
                      value={faixa.horaFim}
                      onChange={e => atualizarFaixa(idx, 'horaFim', e.target.value)}
                      className="w-32"
                    />
                    <Button variant="ghost" size="sm" onClick={() => removerFaixa(idx)}>✕</Button>
                  </div>
                )
              })}
            </div>
          )
        })}
      </div>

      <Button onClick={salvar} disabled={saving}>
        {saving ? 'Salvando...' : 'Salvar Disponibilidade'}
      </Button>
    </div>
  )
}
