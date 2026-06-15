import { Package, PackageX, TrendingDown } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { RelatorioEstoqueResponse } from '@/types/relatorios'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtN = (v: number) => v.toLocaleString('pt-BR')

interface Props { dados: RelatorioEstoqueResponse }

export default function AbaEstoque({ dados }: Props) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        <KpiCard titulo="Valor em estoque" valor={fmt(dados.valorTotalEstoque)} icon={Package} />
        <KpiCard titulo="Produtos ativos" valor={fmtN(dados.produtosAtivos)} icon={Package} />
        <KpiCard titulo="Com estoque baixo" valor={fmtN(dados.produtosEstoqueBaixo)} icon={PackageX}
          cor={dados.produtosEstoqueBaixo > 0 ? 'red' : 'default'} />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="rounded-lg border bg-card overflow-hidden">
          <div className="px-4 py-3 border-b bg-muted/30">
            <p className="text-sm font-semibold">Giro de Estoque no Período</p>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/50">
                <tr>
                  <th className="text-left px-4 py-2 font-medium text-muted-foreground">Produto</th>
                  <th className="text-right px-4 py-2 font-medium text-muted-foreground">Saídas</th>
                  <th className="text-right px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Entradas</th>
                </tr>
              </thead>
              <tbody>
                {dados.giroProdutos.map((p, i) => (
                  <tr key={i} className="border-t hover:bg-muted/30">
                    <td className="px-4 py-2">{p.nome}</td>
                    <td className="px-4 py-2 text-right font-medium">{fmtN(p.saidas)}</td>
                    <td className="px-4 py-2 text-right text-muted-foreground hidden sm:table-cell">{fmtN(p.entradas)}</td>
                  </tr>
                ))}
                {!dados.giroProdutos.length && (
                  <tr><td colSpan={3} className="px-4 py-6 text-center text-muted-foreground">Sem movimentações no período</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="rounded-lg border bg-card overflow-hidden">
          <div className="px-4 py-3 border-b bg-muted/30">
            <p className="text-sm font-semibold flex items-center gap-1.5">
              <TrendingDown size={14} className="text-muted-foreground" />
              Sem Movimentação
            </p>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/50">
                <tr>
                  <th className="text-left px-4 py-2 font-medium text-muted-foreground">Produto</th>
                  <th className="text-right px-4 py-2 font-medium text-muted-foreground">Qtd</th>
                  <th className="text-right px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Valor</th>
                </tr>
              </thead>
              <tbody>
                {dados.semMovimentacao.map((p, i) => (
                  <tr key={i} className="border-t hover:bg-muted/30">
                    <td className="px-4 py-2">{p.nome}</td>
                    <td className="px-4 py-2 text-right">{fmtN(p.estoqueAtual)}</td>
                    <td className="px-4 py-2 text-right text-muted-foreground hidden sm:table-cell">{fmt(p.valorEmEstoque)}</td>
                  </tr>
                ))}
                {!dados.semMovimentacao.length && (
                  <tr><td colSpan={3} className="px-4 py-6 text-center text-muted-foreground">Todos os produtos tiveram movimentação</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  )
}
