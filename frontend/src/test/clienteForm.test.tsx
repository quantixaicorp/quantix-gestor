import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import ClienteForm from '@/components/clientes/ClienteForm'

function setup() {
  const onSubmit = vi.fn().mockResolvedValue(undefined)
  render(<ClienteForm onSubmit={onSubmit} onCancel={() => {}} />)
  fireEvent.change(screen.getByPlaceholderText('Nome completo'), { target: { value: 'Maria' } })
  fireEvent.change(screen.getByPlaceholderText('11999990000'), { target: { value: '11999990001' } })
  return onSubmit
}

describe('ClienteForm — CPF/CNPJ opcional', () => {
  it('começa em Pessoa Física e salva sem documento', async () => {
    const onSubmit = setup()

    expect(screen.getByText('CPF (opcional)')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Salvar' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalled())
    expect(onSubmit.mock.calls[0][0]).toMatchObject({ personType: null, cnpjCpf: null })
  })

  it('mascara o CPF e envia sem máscara', async () => {
    const onSubmit = setup()

    fireEvent.change(screen.getByPlaceholderText('000.000.000-00'), { target: { value: '12345678909' } })
    expect(screen.getByPlaceholderText('000.000.000-00')).toHaveValue('123.456.789-09')

    fireEvent.click(screen.getByRole('button', { name: 'Salvar' }))
    await waitFor(() => expect(onSubmit).toHaveBeenCalled())
    expect(onSubmit.mock.calls[0][0]).toMatchObject({ personType: 'PF', cnpjCpf: '12345678909' })
  })

  it('troca para Pessoa Jurídica e exige CNPJ válido', async () => {
    const onSubmit = setup()

    fireEvent.click(screen.getByRole('button', { name: 'Pessoa Jurídica' }))
    expect(screen.getByText('CNPJ (opcional)')).toBeTruthy()

    fireEvent.change(screen.getByPlaceholderText('00.000.000/0000-00'), { target: { value: '12345678909' } })
    fireEvent.click(screen.getByRole('button', { name: 'Salvar' }))

    await waitFor(() => expect(screen.getByText('CNPJ inválido')).toBeTruthy())
    expect(onSubmit).not.toHaveBeenCalled()
  })
})
