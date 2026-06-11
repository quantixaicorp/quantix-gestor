export interface NotaFiscalItemResponse {
  id: string
  nomeProduto: string
  ncm: string | null
  cfop: string | null
  quantidade: number
  precoUnitario: number
  total: number
}

export interface NotaFiscalResponse {
  id: string
  vendaId: string
  modelo: 'NFe' | 'NFCe'
  numero: number | null
  serie: number | null
  status: 'Pendente' | 'Processando' | 'Autorizada' | 'Rejeitada' | 'Cancelada'
  chaveAcesso: string | null
  protocolo: string | null
  xmlUrl: string | null
  pdfUrl: string | null
  mensagemErro: string | null
  autorizadaEm: string | null
  canceladaEm: string | null
  criadaEm: string
  itens: NotaFiscalItemResponse[]
}

export interface EmitirNotaFiscalRequest {
  vendaId: string
  tipo: 'NFe' | 'NFCe'
}

export interface CancelarNotaFiscalRequest {
  motivo: string
}

export interface ConfiguracaoEmpresaResponse {
  id: string
  razaoSocial: string | null
  nomeFantasia: string | null
  cnpj: string | null
  inscricaoEstadual: string | null
  inscricaoMunicipal: string | null
  telefone: string | null
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
  corPrimaria: string | null
  descricaoPublica: string | null
  asaasApiKey: string | null
  asaasSandbox: boolean
  clickSignApiKey: string | null
  clickSignSandbox: boolean
  evolutionApiUrl: string | null
  temEvolutionKey: boolean
  evolutionInstance: string | null
  lembrete3dAntes: boolean
  lembrete1dAntes: boolean
  lembreteNoDia: boolean
  lembrete1dDepois: boolean
  lembrete3dDepois: boolean
  lembrete7dDepois: boolean
  dominioCustomizado: string | null
  aprovarAutomaticamente: boolean
  valorSinal: number | null
  horasLimiteCancelamento: number | null
}

export interface AtualizarConfiguracaoEmpresaRequest {
  razaoSocial?: string
  nomeFantasia?: string
  cnpj?: string
  inscricaoEstadual?: string
  inscricaoMunicipal?: string
  telefone?: string
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
}
