// frontend/src/test/orcamentos.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'

const mockCreate = vi.fn()
const mockEnviar = vi.fn()
const mockConverter = vi.fn()

vi.mock('@/hooks/useOrcamentos', () => ({
  useOrcamentos: () => ({
    orcamentos: [
      {
        id: '1', numero: 1, titulo: 'Orçamento Teste',
        clienteNome: 'Maria', dataValidade: '2026-06-01',
        status: 'Rascunho', total: 150,
      },
      {
        id: '2', numero: 2, titulo: 'Outro Orçamento',
        clienteNome: null, dataValidade: '2026-05-20',
        status: 'Aprovado', total: 300,
      },
    ],
    orcamento: null,
    loading: false,
    error: null,
    list: vi.fn(),
    get: vi.fn(),
    create: mockCreate,
    enviar: mockEnviar,
    aprovar: vi.fn(),
    rejeitar: vi.fn(),
    cancelar: vi.fn(),
    converter: mockConverter,
  }),
}))

vi.mock('@/hooks/useEstoque', () => ({
  useEstoque: () => ({
    produtos: [{ id: 'p1', nome: 'Shampoo', precoVenda: 50, ativo: true }],
    listProdutos: vi.fn(),
  }),
}))

vi.mock('@/hooks/useClientes', () => ({
  useClientes: () => ({ clientes: [], list: vi.fn() }),
}))

import Orcamentos from '@/pages/orcamentos/Orcamentos'
import NovoOrcamento from '@/pages/orcamentos/NovoOrcamento'

describe('Orcamentos list', () => {
  it('renderiza tabela com orçamentos', () => {
    render(<MemoryRouter><Orcamentos /></MemoryRouter>)
    expect(screen.getByText('Orçamento Teste')).toBeTruthy()
    expect(screen.getByText('Outro Orçamento')).toBeTruthy()
  })

  it('mostra botão Enviar para Rascunho e Converter para Aprovado', () => {
    render(<MemoryRouter><Orcamentos /></MemoryRouter>)
    expect(screen.getByRole('button', { name: /Enviar/i })).toBeTruthy()
    expect(screen.getByRole('button', { name: /Converter/i })).toBeTruthy()
  })

  it('exibe chips de filtro de status', () => {
    render(<MemoryRouter><Orcamentos /></MemoryRouter>)
    expect(screen.getAllByText('Rascunho').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Aprovado').length).toBeGreaterThan(0)
    expect(screen.getByText('Expirado')).toBeTruthy()
  })
})

describe('NovoOrcamento form', () => {
  beforeEach(() => { mockCreate.mockReset() })

  it('renderiza campos obrigatórios', () => {
    render(<MemoryRouter><NovoOrcamento /></MemoryRouter>)
    expect(screen.getByPlaceholderText(/Orçamento/i)).toBeTruthy()
    expect(screen.getByText('Válido até *')).toBeTruthy()
  })

  it('adiciona item livre ao clicar no botão', () => {
    render(<MemoryRouter><NovoOrcamento /></MemoryRouter>)
    const btn = screen.getByText(/Item livre/i)
    fireEvent.click(btn)
    expect(screen.getByText('Livre')).toBeTruthy()
  })

  it('mostra erro ao tentar salvar sem título', async () => {
    render(<MemoryRouter><NovoOrcamento /></MemoryRouter>)
    const submitBtn = screen.getByText(/Salvar Rascunho/i)
    fireEvent.click(submitBtn)
    expect(screen.getByText('Título obrigatório')).toBeTruthy()
  })

  it('adiciona item produto ao selecionar no dropdown', () => {
    render(<MemoryRouter><NovoOrcamento /></MemoryRouter>)
    const selects = document.querySelectorAll('select')
    const produtoSelect = Array.from(selects).find(s =>
      Array.from(s.options).some(o => o.text.includes('Produto do estoque'))
    )!
    fireEvent.change(produtoSelect, { target: { value: 'p1' } })
    expect(screen.getByDisplayValue('Shampoo')).toBeTruthy()
  })

  it('mostra totalizador após adicionar item livre', () => {
    render(<MemoryRouter><NovoOrcamento /></MemoryRouter>)
    const btn = screen.getByText(/Item livre/i)
    fireEvent.click(btn)
    expect(screen.getByText(/Total:/i)).toBeTruthy()
  })
})
