import React from 'react'
import { useParams } from 'react-router-dom'
import { usePublicBooking } from './usePublicBooking'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

const STEPS = ['Serviço', 'Profissional', 'Data', 'Horário', 'Seus dados']
const stepIndex = (s: string) => ['servico', 'profissional', 'data', 'horario', 'dados'].indexOf(s)

const dadosSchema = z.object({
  nome: z.string().min(2, 'Nome obrigatório'),
  telefone: z.string().min(10, 'Telefone inválido'),
})
type DadosForm = z.infer<typeof dadosSchema>

const fmtData = (iso: string) =>
  new Date(iso).toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })
const fmtHora = (iso: string) =>
  new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
const fmtPreco = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

function buildCalendar(year: number, month: number, diasDisponiveis: number[]) {
  const first = new Date(year, month, 1).getDay()
  const days = new Date(year, month + 1, 0).getDate()
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const cells: Array<{ day: number; date: string; disabled: boolean } | null> = Array(first).fill(null)
  for (let d = 1; d <= days; d++) {
    const date = new Date(year, month, d)
    const dow = date.getDay()
    const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`
    cells.push({ day: d, date: dateStr, disabled: date < today || !diasDisponiveis.includes(dow) })
  }
  return cells
}

export default function AgendamentoPublico() {
  const { slug } = useParams<{ slug: string }>()
  const bk = usePublicBooking(slug!)

  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<DadosForm>({ resolver: zodResolver(dadosSchema) })

  const cor = bk.info?.corPrimaria ?? '#3B82F6'

  const today = new Date()
  const [calYear, setCalYear] = React.useState(today.getFullYear())
  const [calMonth, setCalMonth] = React.useState(today.getMonth())
  const cells = buildCalendar(calYear, calMonth, bk.diasDisponiveis)

  if (bk.loading && !bk.info) {
    return (
      <div className="flex h-screen items-center justify-center">
        <p className="text-gray-500">Carregando...</p>
      </div>
    )
  }

  if (bk.error && !bk.info) {
    return (
      <div className="flex h-screen items-center justify-center">
        <p className="text-red-500">{bk.error}</p>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header com branding */}
      <div className="shadow-sm" style={{ backgroundColor: cor }}>
        <div className="max-w-lg mx-auto px-4 py-4 flex items-center gap-3">
          {bk.info?.logoUrl && (
            <img src={bk.info.logoUrl} alt="Logo" className="h-10 w-10 rounded-full object-cover" />
          )}
          <div>
            <h1 className="text-white font-bold text-lg leading-tight">{bk.info?.nome}</h1>
            {bk.info?.descricao && (
              <p className="text-white/80 text-xs">{bk.info.descricao}</p>
            )}
          </div>
        </div>
      </div>

      <div className="max-w-lg mx-auto px-4 py-6 space-y-6">
        {/* Barra de progresso */}
        {bk.step !== 'confirmado' && (
          <div className="flex gap-1">
            {STEPS.map((label, i) => (
              <div key={label} className="flex-1">
                <div
                  className="h-1 rounded-full transition-colors"
                  style={{ backgroundColor: i <= stepIndex(bk.step) ? cor : '#E5E7EB' }}
                />
                <p className="text-xs mt-1 text-center text-gray-500 hidden sm:block">{label}</p>
              </div>
            ))}
          </div>
        )}

        {bk.error && bk.step !== 'confirmado' && (
          <p className="text-red-500 text-sm bg-red-50 rounded-md px-3 py-2">{bk.error}</p>
        )}

        {/* Step: Serviço */}
        {bk.step === 'servico' && (
          <div className="space-y-3">
            <h2 className="font-semibold text-gray-800">Escolha o serviço</h2>
            {bk.servicos.map(s => (
              <button
                key={s.id}
                onClick={() => void bk.selecionarServico(s)}
                className="w-full text-left rounded-xl border-2 border-gray-200 p-4 hover:border-current transition-colors"
                style={{ '--tw-ring-color': cor } as React.CSSProperties}
              >
                <div className="flex justify-between items-start">
                  <div>
                    <p className="font-medium text-gray-900">{s.nome}</p>
                    {s.duracaoMinutos && (
                      <p className="text-sm text-gray-500">{s.duracaoMinutos} min</p>
                    )}
                  </div>
                  <p className="font-semibold" style={{ color: cor }}>{fmtPreco(s.preco)}</p>
                </div>
              </button>
            ))}
          </div>
        )}

        {/* Step: Profissional */}
        {bk.step === 'profissional' && (
          <div className="space-y-3">
            <h2 className="font-semibold text-gray-800">Escolha o profissional</h2>
            {bk.profissionais.map(p => (
              <button
                key={p.id}
                onClick={() => void bk.selecionarProfissional(p)}
                className="w-full text-left rounded-xl border-2 border-gray-200 p-4 hover:border-blue-300 transition-colors"
              >
                <p className="font-medium text-gray-900">{p.nome}</p>
              </button>
            ))}
            <button onClick={bk.voltar} className="text-sm text-gray-500 underline">← Voltar</button>
          </div>
        )}

        {/* Step: Data */}
        {bk.step === 'data' && (
          <div className="space-y-3">
            <h2 className="font-semibold text-gray-800">Escolha a data</h2>
            <div className="rounded-xl border bg-white p-4">
              <div className="flex items-center justify-between mb-3">
                <button
                  onClick={() => { if (calMonth === 0) { setCalYear(y => y - 1); setCalMonth(11) } else setCalMonth(m => m - 1) }}
                  className="p-1 hover:bg-gray-100 rounded"
                >‹</button>
                <p className="font-medium text-sm">
                  {new Date(calYear, calMonth).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })}
                </p>
                <button
                  onClick={() => { if (calMonth === 11) { setCalYear(y => y + 1); setCalMonth(0) } else setCalMonth(m => m + 1) }}
                  className="p-1 hover:bg-gray-100 rounded"
                >›</button>
              </div>
              <div className="grid grid-cols-7 gap-1 text-center text-xs text-gray-500 mb-1">
                {['D','S','T','Q','Q','S','S'].map((d, i) => <span key={i}>{d}</span>)}
              </div>
              <div className="grid grid-cols-7 gap-1">
                {cells.map((cell, i) =>
                  cell === null ? <div key={i} /> : (
                    <button
                      key={cell.date}
                      disabled={cell.disabled}
                      onClick={() => void bk.selecionarData(cell.date)}
                      className={`rounded-lg py-2 text-sm font-medium transition-colors ${
                        cell.disabled
                          ? 'text-gray-300 cursor-not-allowed'
                          : 'hover:text-white'
                      } ${bk.dataSelecionada === cell.date ? 'text-white' : ''}`}
                      style={!cell.disabled ? {
                        backgroundColor: bk.dataSelecionada === cell.date ? cor : undefined,
                      } : undefined}
                    >
                      {cell.day}
                    </button>
                  )
                )}
              </div>
            </div>
            <button onClick={bk.voltar} className="text-sm text-gray-500 underline">← Voltar</button>
          </div>
        )}

        {/* Step: Horário */}
        {bk.step === 'horario' && (
          <div className="space-y-3">
            <h2 className="font-semibold text-gray-800">Escolha o horário</h2>
            {bk.slots.length === 0 ? (
              <p className="text-gray-500 text-sm">Nenhum horário disponível para esta data.</p>
            ) : (
              <div className="grid grid-cols-3 gap-2">
                {bk.slots.map(slot => (
                  <button
                    key={slot}
                    onClick={() => bk.selecionarSlot(slot)}
                    className="rounded-lg border-2 py-2 text-sm font-medium transition-colors hover:text-white"
                    style={{ borderColor: cor }}
                    onMouseEnter={e => (e.currentTarget.style.backgroundColor = cor)}
                    onMouseLeave={e => (e.currentTarget.style.backgroundColor = '')}
                  >
                    {fmtHora(slot)}
                  </button>
                ))}
              </div>
            )}
            <button onClick={bk.voltar} className="text-sm text-gray-500 underline">← Voltar</button>
          </div>
        )}

        {/* Step: Dados do cliente */}
        {bk.step === 'dados' && (
          <div className="space-y-4">
            <h2 className="font-semibold text-gray-800">Seus dados</h2>
            <div className="rounded-xl bg-white border p-4 text-sm space-y-1 text-gray-600">
              <p><span className="font-medium">Serviço:</span> {bk.servicoSelecionado?.nome}</p>
              <p><span className="font-medium">Profissional:</span> {bk.profissionalSelecionado?.nome}</p>
              <p><span className="font-medium">Data:</span> {bk.dataSelecionada && fmtData(bk.dataSelecionada + 'T12:00:00')}</p>
              <p><span className="font-medium">Horário:</span> {bk.slotSelecionado && fmtHora(bk.slotSelecionado)}</p>
            </div>
            <form onSubmit={handleSubmit(d => void bk.confirmar(d.nome, d.telefone))} className="space-y-3">
              <div>
                <label className="text-sm font-medium text-gray-700 block mb-1">Nome completo</label>
                <input {...register('nome')} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2" style={{ '--tw-ring-color': cor } as React.CSSProperties} />
                {errors.nome && <p className="text-xs text-red-500 mt-1">{errors.nome.message}</p>}
              </div>
              <div>
                <label className="text-sm font-medium text-gray-700 block mb-1">Telefone (WhatsApp)</label>
                <input {...register('telefone')} type="tel" placeholder="(11) 99999-0000" className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2" />
                {errors.telefone && <p className="text-xs text-red-500 mt-1">{errors.telefone.message}</p>}
              </div>
              <button
                type="submit"
                disabled={isSubmitting || bk.loading}
                className="w-full py-3 rounded-xl text-white font-semibold transition-opacity disabled:opacity-60"
                style={{ backgroundColor: cor }}
              >
                {bk.loading ? 'Confirmando...' : 'Confirmar Agendamento'}
              </button>
            </form>
            <button onClick={bk.voltar} className="text-sm text-gray-500 underline">← Voltar</button>
          </div>
        )}

        {/* Step: Confirmado */}
        {bk.step === 'confirmado' && bk.confirmado && (
          <div className="text-center space-y-4 py-8">
            <div className="text-5xl">✅</div>
            <h2 className="text-xl font-bold text-gray-800">Solicitação enviada!</h2>
            <p className="text-gray-500 text-sm">
              Seu agendamento foi recebido e está aguardando confirmação da empresa.
              Em breve você receberá um retorno.
            </p>
            <div className="rounded-xl bg-white border p-4 text-sm space-y-1 text-gray-600 text-left">
              <p><span className="font-medium">Serviço:</span> {bk.confirmado.servicoNome}</p>
              <p><span className="font-medium">Profissional:</span> {bk.confirmado.profissionalNome}</p>
              <p><span className="font-medium">Data:</span> {fmtData(bk.confirmado.dataHoraInicio)}</p>
              <p><span className="font-medium">Horário:</span> {fmtHora(bk.confirmado.dataHoraInicio)}</p>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
