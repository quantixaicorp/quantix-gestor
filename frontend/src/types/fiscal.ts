export interface NotaFiscalItemResponse {
  id: string
  productName: string
  ncm: string | null
  cfop: string | null
  quantity: number
  unitPrice: number
  total: number
}

export interface NotaFiscalResponse {
  id: string
  saleId: string
  model: 'NFe' | 'NFCe'
  number: number | null
  series: number | null
  status: 'Pendente' | 'Processando' | 'Autorizada' | 'Rejeitada' | 'Cancelada'
  accessKey: string | null
  protocol: string | null
  xmlUrl: string | null
  pdfUrl: string | null
  errorMessage: string | null
  authorizedAt: string | null
  canceledAt: string | null
  createdAt: string
  items: NotaFiscalItemResponse[]
}

export interface EmitirNotaFiscalRequest {
  saleId: string
  type: 'NFe' | 'NFCe'
}

export interface CancelarNotaFiscalRequest {
  reason: string
}

export interface ConfiguracaoEmpresaResponse {
  id: string
  razaoSocial: string | null
  nomeFantasia: string | null
  cnpj: string | null
  inscricaoEstadual: string | null
  inscricaoMunicipal: string | null
  phone: string | null
  email: string | null
  logradouro: string | null
  numero: string | null
  complemento: string | null
  bairro: string | null
  codigoMunicipio: string | null
  municipio: string | null
  uf: string | null
  cep: string | null
  regimeTributario: number | null
  ambiente: number | null
  serieNfe: number | null
  serieNfce: number | null
  temToken: boolean
  slug: string | null
  logoUrl: string | null
  primaryColor: string | null
  publicDescription: string | null
  asaasApiKey: string | null
  asaasSandbox: boolean
  clickSignApiKey: string | null
  clickSignSandbox: boolean
  evolutionApiUrl: string | null
  temEvolutionKey: boolean
  evolutionInstance: string | null
  reminder3DaysBefore: boolean
  reminder1DayBefore: boolean
  reminderOnDueDate: boolean
  reminder1DayAfter: boolean
  reminder3DaysAfter: boolean
  reminder7DaysAfter: boolean
  customDomain: string | null
  autoApprove: boolean
  depositAmount: number | null
  cancellationLimitHours: number | null
  tipoNegocio: string
}

export interface AtualizarConfiguracaoEmpresaRequest {
  razaoSocial?: string
  nomeFantasia?: string
  cnpj?: string
  inscricaoEstadual?: string
  inscricaoMunicipal?: string
  phone?: string
  email?: string
  logradouro?: string
  numero?: string
  complemento?: string
  bairro?: string
  codigoMunicipio?: string
  municipio?: string
  uf?: string
  cep?: string
  regimeTributario?: number
  cscId?: string
  cscToken?: string
  ambiente?: number
  serieNfe?: number
  serieNfce?: number
  focusNfeToken?: string
  tipoNegocio?: string
}
