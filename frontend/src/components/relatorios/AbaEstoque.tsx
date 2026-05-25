import type { RelatorioEstoqueResponse } from '@/types/relatorios'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioEstoqueResponse }

export default function AbaEstoque({ dados }: Props) {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Valor total em estoque', valor: fmt(dados.valorTotalEstoque), destaque: false },
          { label: 'Produtos ativos', valor: dados.produtosAtivos.toString(), destaque: false },
          { label: 'Com estoque baixo', valor: dados.produtosEstoqueBaixo.toString(), destaque: dados.produtosEstoqueBaixo > 0 },
        ].map(item => (
          <div key={item.label} className="rounded-lg border bg-card p-4">
            <p className="text-sm text-muted-foreground">{item.label}</p>
            <p className={`text-xl font-bold ${item.destaque ? 'text-red-600' : ''}`}>{item.valor}</p>
          </div>
        ))}
      </div>

      <div className="grid lg:grid-cols-2 gap-4">
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Giro de estoque (período)</p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="py-2 text-left font-medium text-muted-foreground">Produto</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Saídas</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Entradas</th>
              </tr>
            </thead>
            <tbody>
              {dados.giroProdutos.map((p, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2">{p.nome}</td>
                  <td className="py-2 text-right">{p.saidas}</td>
                  <td className="py-2 text-right">{p.entradas}</td>
                </tr>
              ))}
              {dados.giroProdutos.length === 0 && (
                <tr><td colSpan={3} className="py-6 text-center text-muted-foreground">Sem movimentações no período</td></tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Produtos sem movimentação</p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="py-2 text-left font-medium text-muted-foreground">Produto</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Estoque</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Valor</th>
              </tr>
            </thead>
            <tbody>
              {dados.semMovimentacao.map((p, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2">{p.nome}</td>
                  <td className="py-2 text-right">{p.estoqueAtual}</td>
                  <td className="py-2 text-right">{fmt(p.valorEmEstoque)}</td>
                </tr>
              ))}
              {dados.semMovimentacao.length === 0 && (
                <tr><td colSpan={3} className="py-6 text-center text-muted-foreground">Todos os produtos tiveram movimentação</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
