export interface ContratoTemplateItem {
  id: string
  description: string
  quantity: number
  unitPrice: number
}

export interface ContratoTemplate {
  id: string
  name: string
  subject: string
  chargeType: string
  frequency: string
  dueDay: number
  defaultAmount: number | null
  createdAt: string
  items: ContratoTemplateItem[]
  total: number
}

export interface ContratoTemplateListItem {
  id: string
  name: string
  chargeType: string
  frequency: string
  defaultAmount: number | null
  qtdItens: number
}

export interface CreateContratoTemplateRequest {
  name: string
  subject: string
  chargeType: string
  frequency: string
  dueDay: number
  defaultAmount: number | null
  items: { description: string; quantity: number; unitPrice: number }[]
}
