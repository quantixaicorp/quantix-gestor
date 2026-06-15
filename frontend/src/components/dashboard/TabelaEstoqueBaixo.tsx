import type { EstoqueBaixoDetalheItem } from '@/types/dashboard'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtN = (v: number) => v.toLocaleString('pt-BR', { maximumFractionDigits: 1 })

interface Props { dados: EstoqueBaixoDetalheItem[] }

export default function TabelaEstoqueBaixo({ dados }: Props) {
  return (
    <div className="rounded-lg border bg-card overflow-hidden">
      <div className="px-4 py-3 border-b bg-muted/30 flex items-center justify-between">
        <p className="text-sm font-semibold">Produtos com Estoque Baixo</p>
        {dados?.length > 0 && (
          <span className="text-xs bg-yellow-100 text-yellow-700 dark:bg-yellow-950/30 dark:text-yellow-400 px-2 py-0.5 rounded-full font-medium">
            {dados.length} produto{dados.length !== 1 ? 's' : ''}
          </span>
        )}
      </div>
      <table className="w-full text-sm">
        <thead className="bg-muted/40">
          <tr>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground">Produto</th>
            <th className="text-right px-4 py-2 font-medium text-muted-foreground">Atual</th>
            <th className="text-right px-4 py-2 font-medium text-muted-foreground">Mínimo</th>
            <th className="text-right px-4 py-2 font-medium text-muted-foreground hidden md:table-cell">Preço</th>
          </tr>
        </thead>
        <tbody>
          {(dados ?? []).map((p, i) => (
            <tr key={i} className="border-t hover:bg-muted/20">
              <td className="px-4 py-2 font-medium">{p.nome}</td>
              <td className="px-4 py-2 text-right text-red-600 font-medium">{fmtN(p.estoqueAtual)}</td>
              <td className="px-4 py-2 text-right text-muted-foreground">{fmtN(p.estoqueMinimo)}</td>
              <td className="px-4 py-2 text-right hidden md:table-cell">{fmt(p.precoVenda)}</td>
            </tr>
          ))}
          {!dados?.length && (
            <tr><td colSpan={4} className="px-4 py-6 text-center text-muted-foreground">Nenhum produto abaixo do mínimo</td></tr>
          )}
        </tbody>
      </table>
    </div>
  )
}
