export type AutomacaoTipoEvento =
  | 'Lembrete3dAntes'
  | 'Lembrete1dAntes'
  | 'LembreteNoDia'
  | 'Lembrete1dDepois'
  | 'Lembrete3dDepois'
  | 'Lembrete7dDepois'
  | 'CobrancaGerada'

export const EVENTO_LABEL: Record<AutomacaoTipoEvento, string> = {
  Lembrete3dAntes:  '3 dias antes',
  Lembrete1dAntes:  '1 dia antes',
  LembreteNoDia:    'No dia',
  Lembrete1dDepois: '1 dia depois',
  Lembrete3dDepois: '3 dias depois',
  Lembrete7dDepois: '7 dias depois',
  CobrancaGerada:   'Cobrança gerada',
}

export interface AutomacaoLogItem {
  id: string
  enviadoEm: string
  customerName: string
  reference: string
  eventType: AutomacaoTipoEvento
  success: boolean
  errorMessage: string | null
}
