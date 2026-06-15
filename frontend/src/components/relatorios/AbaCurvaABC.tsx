import { useState } from 'react'
import {
  ComposedChart, Bar, Line, XAxis, YAxis,
  CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import type { CurvaAbcResponse } from '@/types/relatorios'
import { cn } from '@/lib/utils'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtPct = (v: number) => `${v.toFixed(1)}%`

const COR_CLASSE: Record<string, string> = {
  A: 'bg-green-100 text-green-800 dark:bg-green-950/40 dark:text-green-400',
  B: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-950/40 dark:text-yellow-400',
  C: 'bg-red-100 text-red-800 dark:bg-red-950/40 dark:text-red-400',
}

interface Props {
  produtos: CurvaAbcResponse
  clientes: CurvaAbcResponse
}

export default function AbaCurvaABC({ produtos, clientes }: Props) {
  const [visao, setVisao] = useState<'produtos' | 'clientes'>('produtos')

  const dados = visao === 'produtos' ? produtos : clientes
  const labelItem = visao === 'produtos' ? 'Produto' : 'Cliente'
  const labelQtd = visao === 'produtos' ? 'Qtd vendida' : 'Compras'

  const countA = dados.itens.filter(i => i.classe === 'A').length
  const countB = dados.itens.filter(i => i.classe === 'B').length
  const countC = dados.itens.filter(i => i.classe === 'C').length

  // Pareto chart: top 20 for readability
  const chartData = dados.itens.slice(0, 20).map(i => ({
    nome: i.nome.length > 16 ? i.nome.slice(0, 14) + '…' : i.nome,
    participacao: i.percentual,
    acumulado: i.percentualAcumulado,
  }))

  return (
    <div className="space-y-6">
      {/* Toggle */}
      <div className="flex gap-1 rounded-lg border bg-muted/30 p-1 w-fit">
        {(['produtos', 'clientes'] as const).map(v => (
          <button
            key={v}
            onClick={() => setVisao(v)}
            className={cn(
              'px-4 py-1.5 rounded-md text-sm font-medium transition-colors',
              visao === v ? 'bg-background shadow-sm text-foreground' : 'text-muted-foreground hover:text-foreground'
            )}
          >
            {v === 'produtos' ? 'Por Produto' : 'Por Cliente'}
          </button>
        ))}
      </div>

      {/* Resumo por classe */}
      <div className="grid grid-cols-3 gap-2 sm:gap-3">
        {[
          { classe: 'A', count: countA, desc: '80% do faturamento', cor: 'border-green-300 bg-green-50 dark:bg-green-950/20' },
          { classe: 'B', count: countB, desc: 'Entre 80% e 95%', cor: 'border-yellow-300 bg-yellow-50 dark:bg-yellow-950/20' },
          { classe: 'C', count: countC, desc: 'Abaixo de 95%', cor: 'border-red-300 bg-red-50 dark:bg-red-950/20' },
        ].map(({ classe, count, desc, cor }) => (
          <div key={classe} className={cn('rounded-lg border p-4', cor)}>
            <div className="flex items-center justify-between">
              <span className={cn('text-xs font-bold px-2 py-0.5 rounded', COR_CLASSE[classe])}>Classe {classe}</span>
              <span className="text-2xl font-bold">{count}</span>
            </div>
            <p className="text-xs text-muted-foreground mt-1">{desc}</p>
          </div>
        ))}
      </div>

      {/* Gráfico de Pareto (top 20) */}
      {chartData.length > 0 && (
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-4">Curva de Pareto — Top {chartData.length}</p>
          <ResponsiveContainer width="100%" height={260}>
            <ComposedChart data={chartData} margin={{ left: 10, right: 30 }}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis dataKey="nome" tick={{ fontSize: 11 }} interval={0} angle={-30} textAnchor="end" height={50} />
              <YAxis yAxisId="left" tick={{ fontSize: 11 }} tickFormatter={v => `${v}%`} />
              <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 11 }} tickFormatter={v => `${v}%`} domain={[0, 100]} />
              <Tooltip
                formatter={(v: unknown, name: unknown) =>
                  [`${(v as number).toFixed(1)}%`, name === 'participacao' ? 'Participação' : 'Acumulado']}
              />
              <Legend formatter={v => v === 'participacao' ? 'Participação (%)' : 'Acumulado (%)'} />
              <Bar yAxisId="left" dataKey="participacao" name="participacao" fill="hsl(var(--primary))" opacity={0.8} />
              <Line yAxisId="right" type="monotone" dataKey="acumulado" name="acumulado"
                stroke="hsl(0 72% 51%)" strokeWidth={2} dot={false} />
            </ComposedChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Tabela completa */}
      <div className="rounded-lg border bg-card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="text-left px-3 py-2.5 font-medium text-muted-foreground w-8">#</th>
                <th className="text-left px-3 py-2.5 font-medium text-muted-foreground">{labelItem}</th>
                <th className="text-right px-3 py-2.5 font-medium text-muted-foreground hidden sm:table-cell">{labelQtd}</th>
                <th className="text-right px-3 py-2.5 font-medium text-muted-foreground">Faturamento</th>
                <th className="text-right px-3 py-2.5 font-medium text-muted-foreground hidden md:table-cell">Part. %</th>
                <th className="text-right px-3 py-2.5 font-medium text-muted-foreground hidden md:table-cell">Acum. %</th>
                <th className="text-center px-3 py-2.5 font-medium text-muted-foreground">Classe</th>
              </tr>
            </thead>
            <tbody>
              {dados.itens.map((item, idx) => (
                <tr key={idx} className="border-t hover:bg-muted/30 transition-colors">
                  <td className="px-3 py-2 text-muted-foreground">{idx + 1}</td>
                  <td className="px-3 py-2 font-medium">{item.nome}</td>
                  <td className="px-3 py-2 text-right hidden sm:table-cell">{item.quantidade.toLocaleString('pt-BR')}</td>
                  <td className="px-3 py-2 text-right">{fmt(item.total)}</td>
                  <td className="px-3 py-2 text-right hidden md:table-cell">{fmtPct(item.percentual)}</td>
                  <td className="px-3 py-2 text-right hidden md:table-cell">{fmtPct(item.percentualAcumulado)}</td>
                  <td className="px-3 py-2 text-center">
                    <span className={cn('text-xs font-bold px-2 py-0.5 rounded', COR_CLASSE[item.classe])}>
                      {item.classe}
                    </span>
                  </td>
                </tr>
              ))}
              {dados.itens.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhuma venda no período
                  </td>
                </tr>
              )}
            </tbody>
            {dados.totalGeral > 0 && (
              <tfoot className="bg-muted/50 font-medium border-t">
                <tr>
                  <td colSpan={3} className="px-3 py-2.5">Total</td>
                  <td className="px-3 py-2.5 text-right">{fmt(dados.totalGeral)}</td>
                  <td className="px-3 py-2.5 text-right hidden md:table-cell">100%</td>
                  <td colSpan={2} />
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      </div>
    </div>
  )
}
