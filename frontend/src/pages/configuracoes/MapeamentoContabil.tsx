import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { toast } from '@/hooks/useToast'
import { useContabilidade } from '@/hooks/useContabilidade'
import type { ChartOfAccountResponse, AccountMappingResponse, AccountingSystem } from '@/types/contabilidade'

function flattenAccounts(list: ChartOfAccountResponse[]): ChartOfAccountResponse[] {
  return list.flatMap(a => [a, ...flattenAccounts(a.children)]).filter(a => a.isActive)
}

export default function MapeamentoContabil() {
  const { listAccounts, listMappings, saveMappings, getSettings, updateSettings } = useContabilidade()
  const [accounts, setAccounts] = useState<ChartOfAccountResponse[]>([])
  const [mappings, setMappings] = useState<AccountMappingResponse[]>([])
  const [draft, setDraft] = useState<Record<string, string>>({})
  const [cashAccountId, setCashAccountId] = useState<string>('')
  const [preferredSystem, setPreferredSystem] = useState<AccountingSystem | ''>('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  const EMPTY_GUID = '00000000-0000-0000-0000-000000000000'

  useEffect(() => {
    Promise.all([listAccounts(), listMappings(), getSettings()])
      .then(([accs, maps, settings]) => {
        setAccounts(accs)
        setMappings(maps)
        const d: Record<string, string> = {}
        maps.forEach(m => {
          if (m.accountId && m.accountId !== EMPTY_GUID) {
            d[m.categoryName] = m.accountId
          }
        })
        setDraft(d)
        setCashAccountId(settings.defaultCashAccountId ?? '')
        setPreferredSystem(settings.preferredAccountingSystem ?? '')
      })
      .catch(() => toast.error('Erro ao carregar dados'))
      .finally(() => setLoading(false))
  }, [])

  const flatAccounts = flattenAccounts(accounts)
  const categories = mappings.map(m => m.categoryName)

  const handleSave = async () => {
    setSaving(true)
    try {
      const mappingItems = Object.entries(draft)
        .filter(([, accountId]) => Boolean(accountId))
        .map(([categoryName, accountId]) => ({ categoryName, accountId }))
      await saveMappings(mappingItems)
      await updateSettings({
        defaultCashAccountId: cashAccountId || null,
        preferredAccountingSystem: (preferredSystem as AccountingSystem) || null,
      })
      toast.success('Mapeamento salvo com sucesso')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar mapeamento')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="p-6 text-muted-foreground">Carregando...</div>

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Mapeamento Contábil</h1>
        <Button onClick={handleSave} disabled={saving}>
          {saving ? 'Salvando...' : 'Salvar Mapeamento'}
        </Button>
      </div>

      <div className="border rounded-lg p-4 mb-6 space-y-4">
        <h2 className="font-semibold">Configurações Gerais</h2>
        <div>
          <Label>Conta Padrão de Caixa/Banco</Label>
          <select
            value={cashAccountId}
            onChange={e => setCashAccountId(e.target.value)}
            className="mt-1 w-full border rounded px-3 py-2 text-sm bg-background"
          >
            <option value="">Selecione...</option>
            {flatAccounts.map(a => (
              <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
            ))}
          </select>
        </div>
        <div>
          <Label>Sistema Contábil Preferido</Label>
          <select
            value={preferredSystem}
            onChange={e => setPreferredSystem(e.target.value as AccountingSystem | '')}
            className="mt-1 w-full border rounded px-3 py-2 text-sm bg-background"
          >
            <option value="">Nenhum</option>
            <option value="Dominio">Domínio (Thomson Reuters)</option>
            <option value="Fortes">Fortes (Fortes Tecnologia)</option>
          </select>
        </div>
      </div>

      <div className="border rounded-lg overflow-hidden">
        <table className="w-full">
          <thead className="bg-muted/50">
            <tr className="text-xs text-muted-foreground">
              <th className="py-2 px-4 text-left">Categoria</th>
              <th className="py-2 px-4 text-left">Conta Contábil</th>
            </tr>
          </thead>
          <tbody>
            {categories.length === 0 ? (
              <tr>
                <td colSpan={2} className="py-8 text-center text-muted-foreground text-sm">
                  Nenhuma categoria cadastrada
                </td>
              </tr>
            ) : categories.map(cat => (
              <tr key={cat} className="border-t">
                <td className="py-2 px-4 text-sm">
                  <div className="flex items-center gap-2">
                    {cat}
                    {!draft[cat] && (
                      <Badge
                        variant="outline"
                        className="text-yellow-600 border-yellow-400 text-xs"
                      >
                        Sem mapeamento
                      </Badge>
                    )}
                  </div>
                </td>
                <td className="py-2 px-4">
                  <select
                    value={draft[cat] ?? ''}
                    onChange={e => setDraft(d => ({ ...d, [cat]: e.target.value }))}
                    className="w-full border rounded px-2 py-1 text-sm bg-background"
                  >
                    <option value="">Selecione...</option>
                    {flatAccounts.map(a => (
                      <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
                    ))}
                  </select>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
