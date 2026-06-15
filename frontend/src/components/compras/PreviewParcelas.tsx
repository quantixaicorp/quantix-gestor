import type { ParcelaPersonalizadaRequest } from '@/types/compras'
import { Input } from '@/components/ui/input'

interface ParcelaPreview {
  numero: number
  dataVencimento: string
  valor: number
}

interface Props {
  parcelas: ParcelaPreview[]
  editavel?: boolean
  onChangeData?: (idx: number, data: string) => void
  parcelasPersonalizadas?: ParcelaPersonalizadaRequest[]
  onChangePersonalizada?: (idx: number, campo: keyof ParcelaPersonalizadaRequest, valor: string | number) => void
}

export default function PreviewParcelas({ parcelas, editavel, onChangeData }: Props) {
  if (parcelas.length === 0) return null

  return (
    <div className="rounded-md border overflow-hidden">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b bg-muted/50">
            <th className="px-3 py-2 text-left font-medium w-16">Parcela</th>
            <th className="px-3 py-2 text-left font-medium">Vencimento</th>
            <th className="px-3 py-2 text-right font-medium">Valor</th>
          </tr>
        </thead>
        <tbody>
          {parcelas.map((p, idx) => (
            <tr key={idx} className="border-b last:border-0">
              <td className="px-3 py-2 text-muted-foreground">{p.numero}/{parcelas.length}</td>
              <td className="px-3 py-2">
                {editavel && onChangeData
                  ? <Input
                      type="date"
                      value={p.dataVencimento.slice(0, 10)}
                      onChange={e => onChangeData(idx, e.target.value)}
                      className="h-7 w-36"
                    />
                  : new Date(p.dataVencimento).toLocaleDateString('pt-BR')
                }
              </td>
              <td className="px-3 py-2 text-right font-medium">
                {p.valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
