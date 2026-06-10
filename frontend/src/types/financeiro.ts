export interface LancamentoResponse {
  id: string
  tipo: 'Receita' | 'Despesa'
  descricao: string
  valor: number
  dataVencimento: string
  dataPagamento: string | null
  status: 'Pendente' | 'Pago' | 'Cancelado'
  categoria: string
  vendaId: string | null
  observacao: string | null
  vencido: boolean
}

export interface CreateLancamentoRequest {
  tipo: 'Receita' | 'Despesa'
  descricao: string
  valor: number
  dataVencimento: string
  categoria: string
  observacao?: string
}

export interface PagarLancamentoRequest {
  dataPagamento: string
}

export interface FluxoCaixaItemResponse {
  data: string
  receitas: number
  despesas: number
  saldo: number
}

export interface FluxoCaixaResponse {
  totalReceitas: number
  totalDespesas: number
  saldoFinal: number
  itens: FluxoCaixaItemResponse[]
}

export interface LancamentoResumo {
  totalReceitasMes: number
  totalDespesasMes: number
  saldoMes: number
  totalPendente: number
}

export interface UpdateLancamentoRequest {
  tipo: 'Receita' | 'Despesa'
  descricao: string
  valor: number
  dataVencimento: string
  categoria: string
  observacao?: string
}
