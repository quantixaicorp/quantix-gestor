import type { DreResponse } from '@/types/relatorios'
import { cn } from '@/lib/utils'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtPct = (v: number) => `${v.toFixed(1)}%`

interface Props { dados: DreResponse }

function Linha({
  label, valor, variante = 'normal', indent = false, destaque = false,
}: {
  label: string
  valor: number
  variante?: 'positivo' | 'negativo' | 'normal' | 'subtotal' | 'resultado'
  indent?: boolean
  destaque?: boolean
}) {
  const valorCor =
    variante === 'positivo' ? 'text-green-600' :
    variante === 'negativo' ? 'text-red-600' :
    variante === 'resultado' ? (valor >= 0 ? 'text-green-600' : 'text-red-600') :
    ''

  return (
    <tr className={cn(
      'border-b last:border-0',
      destaque && 'bg-muted/40 font-semibold',
    )}>
      <td className={cn('py-2.5 text-sm', indent && 'pl-8 text-muted-foreground')}>
        {label}
      </td>
      <td className={cn('py-2.5 text-sm text-right tabular-nums', valorCor)}>
        {variante === 'negativo' ? `(${fmt(Math.abs(valor))})` : fmt(valor)}
      </td>
    </tr>
  )
}

function Separador({ label }: { label: string }) {
  return (
    <tr>
      <td colSpan={2} className="pt-5 pb-1">
        <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">{label}</p>
      </td>
    </tr>
  )
}

export default function AbaDRE({ dados }: Props) {
  const totalGeral = dados.receitaBrutaVendas + dados.outrasReceitas

  return (
    <div className="space-y-4">
      {/* KPI rápidos */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {[
          { label: 'Receita Líquida', valor: fmt(dados.receitaLiquida), cor: 'text-foreground' },
          { label: 'Lucro Bruto', valor: `${fmt(dados.lucroBruto)} (${fmtPct(dados.margemBruta)})`, cor: dados.lucroBruto >= 0 ? 'text-green-600' : 'text-red-600' },
          { label: 'Resultado Op.', valor: `${fmt(dados.resultadoOperacional)} (${fmtPct(dados.margemOperacional)})`, cor: dados.resultadoOperacional >= 0 ? 'text-green-600' : 'text-red-600' },
          { label: 'CMV / Receita', valor: dados.receitaLiquida > 0 ? fmtPct(dados.cmv / dados.receitaLiquida * 100) : '—', cor: 'text-foreground' },
        ].map(kpi => (
          <div key={kpi.label} className="rounded-lg border bg-card p-4">
            <p className="text-xs text-muted-foreground">{kpi.label}</p>
            <p className={cn('text-base font-bold mt-0.5 leading-tight', kpi.cor)}>{kpi.valor}</p>
          </div>
        ))}
      </div>

      {/* DRE estruturada */}
      <div className="rounded-lg border bg-card overflow-hidden">
        <div className="px-4 py-3 border-b bg-muted/30">
          <p className="text-sm font-semibold">Demonstração do Resultado do Exercício</p>
        </div>
        <div className="px-4">
          <table className="w-full">
            <tbody>
              <Separador label="Receitas" />
              <Linha label="Vendas de Produtos/Serviços" valor={dados.receitaBrutaVendas} variante="positivo" indent />
              {dados.outrasReceitas > 0 && (
                <Linha label="Outras Receitas" valor={dados.outrasReceitas} variante="positivo" indent />
              )}
              {dados.totalDescontos > 0 && (
                <Linha label="Descontos Concedidos" valor={dados.totalDescontos} variante="negativo" indent />
              )}
              <Linha label="(=) Receita Líquida" valor={dados.receitaLiquida} destaque variante="normal" />

              <Separador label="Custos" />
              <Linha label="Custo das Mercadorias Vendidas (CMV)" valor={dados.cmv} variante="negativo" indent />
              <Linha label="(=) Lucro Bruto" valor={dados.lucroBruto}
                destaque variante={dados.lucroBruto >= 0 ? 'positivo' : 'negativo'} />

              {dados.despesasOperacionais.length > 0 && (
                <>
                  <Separador label="Despesas Operacionais" />
                  {dados.despesasOperacionais.map((d, i) => (
                    <Linha key={i} label={d.descricao} valor={d.valor} variante="negativo" indent />
                  ))}
                  <Linha label="Total Despesas Operacionais" valor={dados.totalDespesasOperacionais}
                    variante="negativo" destaque />
                </>
              )}

              <Separador label="Resultado" />
              <Linha
                label="(=) Resultado Operacional"
                valor={dados.resultadoOperacional}
                destaque
                variante="resultado"
              />
            </tbody>
          </table>
        </div>

        {/* Rodapé com margens */}
        <div className="px-4 py-3 border-t bg-muted/20 flex flex-wrap gap-6 text-sm">
          <div>
            <span className="text-muted-foreground">Margem Bruta: </span>
            <span className={cn('font-semibold', dados.margemBruta >= 0 ? 'text-green-600' : 'text-red-600')}>
              {fmtPct(dados.margemBruta)}
            </span>
          </div>
          <div>
            <span className="text-muted-foreground">Margem Operacional: </span>
            <span className={cn('font-semibold', dados.margemOperacional >= 0 ? 'text-green-600' : 'text-red-600')}>
              {fmtPct(dados.margemOperacional)}
            </span>
          </div>
          {totalGeral > 0 && (
            <div>
              <span className="text-muted-foreground">Faturamento Bruto: </span>
              <span className="font-semibold">{fmt(totalGeral)}</span>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
