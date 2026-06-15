import {
  AreaChart, Area, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts'
import type { RelatorioVendasResponse } from '@/types/relatorios'

const CORES = ['hsl(var(--primary))', 'hsl(142 76% 36%)', 'hsl(38 92% 50%)', 'hsl(0 72% 51%)']
const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtN = (v: number) => v.toLocaleString('pt-BR')

interface Props { dados: RelatorioVendasResponse }

function Panel({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border bg-muted/20 p-3 sm:p-5 space-y-4">
      <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">{titulo}</p>
      {children}
    </div>
  )
}

export default function AbaVendas({ dados }: Props) {
  const tendencia = dados.tendencia.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    total: d.total,
    quantidade: d.quantidade,
  }))

  return (
    <div className="space-y-5">

      {/* ── Tendência ────────────────────────────────────────── */}
      <Panel titulo="Tendência de Vendas">
        {tendencia.length > 0 ? (
          <div className="rounded-lg bg-card border p-4">
            <ResponsiveContainer width="100%" height={220}>
              <AreaChart data={tendencia} margin={{ left: 0, right: 4 }}>
                <defs>
                  <linearGradient id="gradArea" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.2} />
                    <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
                <XAxis dataKey="dia" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
                <YAxis tick={{ fontSize: 11 }} tickLine={false} axisLine={false}
                  tickFormatter={v => `R$${((v as number) / 1000).toFixed(0)}k`} />
                <Tooltip formatter={(v: unknown) => [fmt(v as number), 'Total']}
                  contentStyle={{ fontSize: 12, borderRadius: 8 }} />
                <Area type="monotone" dataKey="total" stroke="hsl(var(--primary))"
                  strokeWidth={2} fill="url(#gradArea)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <div className="rounded-lg bg-card border p-4 flex h-28 items-center justify-center text-sm text-muted-foreground">
            Sem vendas no período
          </div>
        )}
      </Panel>

      {/* ── Rankings ─────────────────────────────────────────── */}
      <Panel titulo="Rankings">
        <div className="grid gap-4 lg:grid-cols-2">
          {/* Top Produtos */}
          <div className="rounded-lg bg-card border overflow-hidden">
            <div className="px-4 py-3 border-b bg-muted/30">
              <p className="text-sm font-semibold">Top Produtos</p>
            </div>
            <table className="w-full text-sm">
              <thead className="bg-muted/40">
                <tr>
                  <th className="text-left px-4 py-2 font-medium text-muted-foreground">Produto</th>
                  <th className="text-right px-4 py-2 font-medium text-muted-foreground">Qtd</th>
                  <th className="text-right px-4 py-2 font-medium text-muted-foreground">Total</th>
                </tr>
              </thead>
              <tbody>
                {dados.topProdutos.map((p, i) => (
                  <tr key={i} className="border-t hover:bg-muted/20">
                    <td className="px-4 py-2">{p.nome}</td>
                    <td className="px-4 py-2 text-right text-muted-foreground">{fmtN(p.quantidade)}</td>
                    <td className="px-4 py-2 text-right font-medium">{fmt(p.total)}</td>
                  </tr>
                ))}
                {!dados.topProdutos.length && (
                  <tr><td colSpan={3} className="px-4 py-6 text-center text-muted-foreground">Sem dados</td></tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Por Forma de Pagamento */}
          <div className="rounded-lg bg-card border p-4">
            <p className="text-sm font-semibold mb-3">Por Forma de Pagamento</p>
            {dados.porFormaPagamento.length > 0 ? (
              <ResponsiveContainer width="100%" height={200}>
                <PieChart>
                  <Pie data={dados.porFormaPagamento} dataKey="total"
                    nameKey="formaPagamento" cx="50%" cy="50%" outerRadius={75}
                    label={(props) => {
                      const p = props as unknown as { formaPagamento: string; percent: number }
                      return `${p.formaPagamento} ${(p.percent * 100).toFixed(0)}%`
                    }}>
                    {dados.porFormaPagamento.map((_e, i) => (
                      <Cell key={i} fill={CORES[i % CORES.length]} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(v: unknown) => [fmt(v as number)]}
                    contentStyle={{ fontSize: 12, borderRadius: 8 }} />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">Sem dados</div>
            )}
          </div>
        </div>

        {/* Top Clientes */}
        {dados.topClientes.length > 0 && (
          <div className="rounded-lg bg-card border overflow-hidden">
            <div className="px-4 py-3 border-b bg-muted/30">
              <p className="text-sm font-semibold">Top Clientes</p>
            </div>
            <table className="w-full text-sm">
              <thead className="bg-muted/40">
                <tr>
                  <th className="text-left px-4 py-2 font-medium text-muted-foreground">Cliente</th>
                  <th className="text-right px-4 py-2 font-medium text-muted-foreground">Compras</th>
                  <th className="text-right px-4 py-2 font-medium text-muted-foreground">Total</th>
                </tr>
              </thead>
              <tbody>
                {dados.topClientes.map((c, i) => (
                  <tr key={i} className="border-t hover:bg-muted/20">
                    <td className="px-4 py-2">{c.nome}</td>
                    <td className="px-4 py-2 text-right text-muted-foreground">{fmtN(c.compras)}</td>
                    <td className="px-4 py-2 text-right font-medium">{fmt(c.total)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Panel>

    </div>
  )
}
