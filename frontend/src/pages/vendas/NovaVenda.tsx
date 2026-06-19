import { useEffect, useState } from 'react'
import { useConfiguracaoEmpresa } from '@/hooks/useConfiguracaoEmpresa'
import NovaVendaPDV from './NovaVendaPDV'
import NovaVendaOS from './NovaVendaOS'

export default function NovaVenda() {
  const { obter } = useConfiguracaoEmpresa()
  const [tipoNegocio, setTipoNegocio] = useState<string | null>(null)

  useEffect(() => {
    void obter().then(c => setTipoNegocio(c?.tipoNegocio ?? 'Lojista'))
  }, [obter])

  if (tipoNegocio === null)
    return <div className="flex h-64 items-center justify-center text-muted-foreground">Carregando...</div>

  if (tipoNegocio === 'Prestador')
    return <NovaVendaOS />

  return <NovaVendaPDV />
}
