export interface LancamentoResponse {
  id: string
  type: 'Receita' | 'Despesa'
  description: string
  amount: number
  dueDate: string
  paymentDate: string | null
  status: 'Pendente' | 'Pago' | 'Cancelado'
  category: string
  saleId: string | null
  notes: string | null
  vencido: boolean
  installmentPlanId: string | null
  installmentNumber: number | null
}

export interface CreateLancamentoRequest {
  type: 'Receita' | 'Despesa'
  description: string
  amount: number
  dueDate: string
  category: string
  notes?: string
}

export interface PagarLancamentoRequest {
  paymentDate: string
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
  items: FluxoCaixaItemResponse[]
}

export interface LancamentoResumo {
  totalReceitasMes: number
  totalDespesasMes: number
  saldoMes: number
  totalPendente: number
}

export interface UpdateLancamentoRequest {
  type: 'Receita' | 'Despesa'
  description: string
  amount: number
  dueDate: string
  category: string
  notes?: string
}

export interface CategoriaLancamentoResponse {
  id: string
  name: string
  type: 'Receita' | 'Despesa'
}

export interface CreateCategoriaLancamentoRequest {
  name: string
  type: 'Receita' | 'Despesa'
}

export interface UpdateCategoriaLancamentoRequest {
  name: string
}
