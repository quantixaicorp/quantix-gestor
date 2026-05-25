import {
  ComposedChart, Bar, Line, XAxis, YAxis,
  CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  PieChart, Pie, Cell
} from 'recharts'
import type { RelatorioFinanceiroResponse } from '@/types/relatorios'

const CORES = ['hsl(0 72% 51%)', 'hsl(38 92% 50%)', 'hsl(262 80% 50%)', 'hsl(142 76% 36%)', 'hsl(200 98% 39%)']
const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioFinanceiroResponse }

export default function AbaFinanceiro({ dados }: Props) {
  const fluxo = dados.fluxoPorDia.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    receitas: d.receitas,
    despesas: d.despesas,
    saldo: d.saldo,
  }))

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Total Receitas', valor: fmt(dados.totalReceitas), cor: 'text-green-600' },
          { label: 'Total Despesas', valor: fmt(dados.totalDespesas), cor: 'text-red-600' },
          { label: 'Saldo', valor: fmt(dados.saldo), cor: dados.saldo >= 0 ? 'text-green-600' : 'text-red-600' },
        ].map(item => (
          <div key={item.label} className="rounded-lg border bg-card p-4">
            <p className="text-sm text-muted-foreground">{item.label}</p>
            <p className={`text-xl font-bold ${item.cor}`}>{item.valor}</p>
          </div>
        ))}
      </div>

      <div className="rounded-lg border bg-card p-4">
        <p className="text-sm font-medium mb-4">Fluxo de caixa por dia</p>
        <ResponsiveContainer width="100%" height={240}>
          <ComposedChart data={fluxo}>
            <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
            <XAxis dataKey="dia" tick={{ fontSize: 12 }} />
            <YAxis tick={{ fontSize: 12 }} tickFormatter={v => fmt(v as number)} />
            <Tooltip formatter={(v: unknown) => [fmt(v as number)]} />
            <Legend />
            <Bar dataKey="receitas" name="Receitas" fill="hsl(142 76% 36%)" />
            <Bar dataKey="despesas" name="Despesas" fill="hsl(0 72% 51%)" />
            <Line type="monotone" dataKey="saldo" name="Saldo"
              stroke="hsl(var(--primary))" strokeWidth={2} dot={false} />
          </ComposedChart>
        </ResponsiveContainer>
      </div>

      {dados.categoriasDespesas.length > 0 && (
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-4">Despesas por categoria</p>
          <div className="grid lg:grid-cols-2 gap-4 items-center">
            <ResponsiveContainer width="100%" height={200}>
              <PieChart>
                <Pie data={dados.categoriasDespesas} dataKey="total"
                  nameKey="categoria" cx="50%" cy="50%" outerRadius={80}>
                  {dados.categoriasDespesas.map((_entry, i) => (
                    <Cell key={i} fill={CORES[i % CORES.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(v: unknown) => [fmt(v as number)]} />
              </PieChart>
            </ResponsiveContainer>
            <table className="text-sm w-full">
              <tbody>
                {dados.categoriasDespesas.map((c, i) => (
                  <tr key={i} className="border-b last:border-0">
                    <td className="py-1.5 flex items-center gap-2">
                      <span className="w-3 h-3 rounded-full shrink-0"
                        style={{ background: CORES[i % CORES.length] }} />
                      {c.categoria}
                    </td>
                    <td className="py-1.5 text-right font-medium">{fmt(c.total)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
