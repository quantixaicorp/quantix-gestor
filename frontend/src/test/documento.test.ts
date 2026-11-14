import { describe, expect, it } from 'vitest'
import { isValidCpf, maskCpf, normalizeCpf } from '@/lib/cpf'
import { isValidDocumento, labelDocumento, maskDocumento } from '@/lib/documento'

describe('CPF', () => {
  it('formata progressivamente enquanto digita', () => {
    expect(maskCpf('123')).toBe('123')
    expect(maskCpf('1234')).toBe('123.4')
    expect(maskCpf('1234567')).toBe('123.456.7')
    expect(maskCpf('12345678909')).toBe('123.456.789-09')
  })

  it('ignora caracteres não numéricos e limita a 11 dígitos', () => {
    expect(normalizeCpf('123.456.789-0912')).toBe('12345678909')
  })

  it('valida dígitos verificadores', () => {
    expect(isValidCpf('123.456.789-09')).toBe(true)
    expect(isValidCpf('123.456.789-00')).toBe(false)
  })

  it('rejeita CPF incompleto ou com todos os dígitos iguais', () => {
    expect(isValidCpf('1234567890')).toBe(false)
    expect(isValidCpf('111.111.111-11')).toBe(false)
  })
})

describe('documento por tipo de pessoa', () => {
  it('usa CPF para PF e CNPJ para PJ', () => {
    expect(labelDocumento('PF')).toBe('CPF')
    expect(labelDocumento('PJ')).toBe('CNPJ')
    expect(maskDocumento('PF', '12345678909')).toBe('123.456.789-09')
    expect(maskDocumento('PJ', '11222333000181')).toBe('11.222.333/0001-81')
  })

  it('não aceita CPF válido marcado como PJ', () => {
    expect(isValidDocumento('PF', '12345678909')).toBe(true)
    expect(isValidDocumento('PJ', '12345678909')).toBe(false)
  })
})
