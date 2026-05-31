import { useState } from 'react'
import { FileText, Settings } from 'lucide-react'
import ListaNotas from './ListaNotas'
import ConfiguracaoTab from './ConfiguracaoTab'

type Tab = 'notas' | 'configuracao'

const TABS: { id: Tab; label: string; Icon: typeof FileText }[] = [
  { id: 'notas',        label: 'Notas Fiscais', Icon: FileText  },
  { id: 'configuracao', label: 'Configuração',  Icon: Settings  },
]

export default function Fiscal() {
  const [tab, setTab] = useState<Tab>('notas')

  return (
    <div className="flex flex-col gap-5">
      <h1 className="text-xl font-bold">Fiscal</h1>

      <div className="flex gap-1 border-b">
        {TABS.map(({ id, label, Icon }) => (
          <button
            key={id}
            onClick={() => setTab(id)}
            className={[
              'flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors',
              tab === id
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground',
            ].join(' ')}
          >
            <Icon size={15} />
            {label}
          </button>
        ))}
      </div>

      {tab === 'notas' ? <ListaNotas /> : <ConfiguracaoTab />}
    </div>
  )
}
