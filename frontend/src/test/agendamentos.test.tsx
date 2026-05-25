// frontend/src/test/agendamentos.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'

const mockCreate = vi.fn()
const mockSlots = vi.fn()

vi.mock('@/hooks/useAgendamentos', () => ({
  useAgendamentos: () => ({
    agendamentos: [
      {
        id: '1',
        profissionalId: 'p1',
        profissionalNome: 'Ana',
        clienteNome: 'João',
        servicoNome: 'Corte',
        dataHoraInicio: '2026-05-25T10:00:00Z',
        dataHoraFim: '2026-05-25T10:30:00Z',
        status: 'Agendado',
      },
      {
        id: '2',
        profissionalId: 'p2',
        profissionalNome: 'Carlos',
        clienteNome: 'Maria',
        servicoNome: 'Manicure',
        dataHoraInicio: '2026-05-25T11:00:00Z',
        dataHoraFim: '2026-05-25T11:30:00Z',
        status: 'Confirmado',
      },
    ],
    agendamento: null,
    loading: false,
    error: null,
    list: vi.fn(),
    get: vi.fn(),
    create: mockCreate,
    confirmar: vi.fn(),
    concluir: vi.fn(),
    cancelar: vi.fn(),
    slots: mockSlots,
  }),
}))

vi.mock('@/hooks/useProfissionais', () => ({
  useProfissionais: () => ({
    profissionais: [
      { id: 'p1', nome: 'Ana', telefone: null, ativo: true },
      { id: 'p2', nome: 'Carlos', telefone: null, ativo: true },
    ],
    loading: false,
    error: null,
    list: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
    getDisponibilidade: vi.fn().mockResolvedValue([]),
    saveDisponibilidade: vi.fn(),
    listBloqueios: vi.fn(),
    criarBloqueio: vi.fn(),
    deleteBloqueio: vi.fn(),
  }),
}))

vi.mock('@/hooks/useEstoque', () => ({
  useEstoque: () => ({
    produtos: [
      { id: 'srv1', nome: 'Corte', precoVenda: 40, duracaoMinutos: 30, ativo: true },
    ],
    listProdutos: vi.fn(),
  }),
}))

vi.mock('@/hooks/useClientes', () => ({
  useClientes: () => ({ clientes: [], list: vi.fn() }),
}))

import Agendamentos from '@/pages/agendamentos/Agendamentos'
import NovoAgendamento from '@/pages/agendamentos/NovoAgendamento'
import Profissionais from '@/pages/profissionais/Profissionais'

describe('Agendamentos grid', () => {
  it('renderiza uma coluna por profissional ativo', () => {
    render(<MemoryRouter><Agendamentos /></MemoryRouter>)
    expect(screen.getByText('Ana')).toBeTruthy()
    expect(screen.getByText('Carlos')).toBeTruthy()
  })

  it('exibe cards dos agendamentos do dia', () => {
    render(<MemoryRouter><Agendamentos /></MemoryRouter>)
    expect(screen.getByText('João')).toBeTruthy()
    expect(screen.getByText('Maria')).toBeTruthy()
  })

  it('exibe badges de status com texto correto', () => {
    render(<MemoryRouter><Agendamentos /></MemoryRouter>)
    expect(screen.getByText('Agendado')).toBeTruthy()
    expect(screen.getByText('Confirmado')).toBeTruthy()
  })

  it('exibe botão Novo Agendamento', () => {
    render(<MemoryRouter><Agendamentos /></MemoryRouter>)
    expect(screen.getByRole('button', { name: /Novo Agendamento/i })).toBeTruthy()
  })
})

describe('NovoAgendamento form', () => {
  beforeEach(() => { mockCreate.mockReset(); mockSlots.mockResolvedValue([]) })

  it('renderiza selects de profissional e serviço', () => {
    render(<MemoryRouter><NovoAgendamento /></MemoryRouter>)
    expect(screen.getByText(/Profissional \*/i)).toBeTruthy()
    expect(screen.getByText(/Serviço \*/i)).toBeTruthy()
  })

  it('exibe serviços com duracaoMinutos > 0', () => {
    render(<MemoryRouter><NovoAgendamento /></MemoryRouter>)
    expect(screen.getByText(/Corte \(30 min\)/i)).toBeTruthy()
  })

  it('mostra erros ao tentar agendar sem campos obrigatórios', () => {
    render(<MemoryRouter><NovoAgendamento /></MemoryRouter>)
    fireEvent.click(screen.getByRole('button', { name: /Agendar/i }))
    expect(screen.getByText('Profissional obrigatório')).toBeTruthy()
    expect(screen.getByText('Telefone obrigatório')).toBeTruthy()
  })
})

describe('Profissionais page', () => {
  it('renderiza tabela com profissionais', () => {
    render(<MemoryRouter><Profissionais /></MemoryRouter>)
    expect(screen.getByText('Ana')).toBeTruthy()
    expect(screen.getByText('Carlos')).toBeTruthy()
  })

  it('exibe botão Novo Profissional', () => {
    render(<MemoryRouter><Profissionais /></MemoryRouter>)
    expect(screen.getByRole('button', { name: /Novo Profissional/i })).toBeTruthy()
  })

  it('exibe formulário ao clicar em Novo Profissional', () => {
    render(<MemoryRouter><Profissionais /></MemoryRouter>)
    fireEvent.click(screen.getByRole('button', { name: /Novo Profissional/i }))
    expect(screen.getByPlaceholderText(/Nome completo/i)).toBeTruthy()
  })
})
