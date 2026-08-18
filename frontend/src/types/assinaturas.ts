// frontend/src/types/assinaturas.ts
export interface PlanoItem {
  id: string
  description: string
  serviceId: string | null
  quantityPerCycle: number
  type: 'Servico' | 'Desconto' | 'Beneficio'
  discountPercentage: number | null
}

export interface PlanoAssinaturaListItem {
  id: string
  name: string
  niche: string
  price: number
  frequency: string
  isActive: boolean
  bestSeller: boolean
  totalAssinantes: number
}

export interface PlanoAssinaturaResponse extends PlanoAssinaturaListItem {
  description: string | null
  items: PlanoItem[]
  createdAt: string
}

export interface NichoTemplateItem {
  id: string
  description: string
  quantityPerCycle: number
  type: string
  discountPercentage: number | null
}

export interface NichoTemplate {
  id: string
  niche: string
  planName: string
  description: string | null
  suggestedPrice: number
  bestSeller: boolean
  frequency: string
  items: NichoTemplateItem[]
}

export interface AssinaturaListItem {
  id: string
  customerName: string
  planNome: string
  status: string
  renewalDate: string
  currentCycle: number
}
