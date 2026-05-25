import {
  AreaChart, Area, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer
} from 'recharts'
import type { RelatorioVendasResponse } from '@/types/relatorios'

const CORES = ['hsl(var(--primary))', 'hsl(142 76% 36%)', 'hsl(38 92% 50%)', 'hsl(0 72% 51%)']
const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioVendasResponse }

export default function AbaVendas({ dados }: Props) {
  const tendencia = dados.tendencia.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    total: d.total,
  }))

  return (
    <div className="space-y-6">
      <div className="rounded-lg border bg-card p-4">
        <p className="text-sm font-medium mb-4">Tendência de vendas</p>
        <ResponsiveContainer width="100%" height={220}>
          <AreaChart data={tendencia}>
            <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
            <XAxis dataKey="dia" tick={{ fontSize: 12 }} />
            <YAxis tick={{ fontSize: 12 }} tickFormatter={v => fmt(v as number)} />
            <Tooltip formatter={(v: unknown) => [fmt(v as number), 'Total']} />
            <Area type="monotone" dataKey="total" fill="hsl(var(--primary) / 0.2)"
              stroke="hsl(var(--primary))" strokeWidth={2} />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Top produtos</p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="py-2 text-left font-medium text-muted-foreground">Produto</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Qtd</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Total</th>
              </tr>
            </thead>
            <tbody>
              {dados.topProdutos.map((p, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2">{p.nome}</td>
                  <td className="py-2 text-right">{p.quantidade}</td>
                  <td className="py-2 text-right font-medium">{fmt(p.total)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Por forma de pagamento</p>
          {dados.porFormaPagamento.length > 0 ? (
            <ResponsiveContainer width="100%" height={200}>
              <PieChart>
                <Pie data={dados.porFormaPagamento} dataKey="total"
                  nameKey="formaPagamento" cx="50%" cy="50%" outerRadius={80}
                  label={(props) => {
                    const p = props as unknown as { formaPagamento: string; percent: number }
                    return `${p.formaPagamento} ${(p.percent * 100).toFixed(0)}%`
                  }}>
                  {dados.porFormaPagamento.map((_entry, i) => (
                    <Cell key={i} fill={CORES[i % CORES.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(v: unknown) => [fmt(v as number)]} />
              </PieChart>
            </ResponsiveContainer>
          ) : <p className="text-sm text-muted-foreground py-8 text-center">Sem dados</p>}
        </div>
      </div>

      {dados.topClientes.length > 0 && (
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Top clientes</p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="py-2 text-left font-medium text-muted-foreground">Cliente</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Compras</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Total</th>
              </tr>
            </thead>
            <tbody>
              {dados.topClientes.map((c, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2">{c.nome}</td>
                  <td className="py-2 text-right">{c.compras}</td>
                  <td className="py-2 text-right font-medium">{fmt(c.total)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
