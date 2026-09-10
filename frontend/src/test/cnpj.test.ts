import { describe, expect, it } from 'vitest'
import { isValidCnpj, maskCnpj } from '@/lib/cnpj'

describe('CNPJ', () => {
  it('aceita e formata CNPJ alfanumérico oficial', () => {
    expect(isValidCnpj('12.ABC.345/01DE-35')).toBe(true)
    expect(maskCnpj('12abc34501de35')).toBe('12.ABC.345/01DE-35')
  })

  it('mantém compatibilidade com CNPJ numérico', () => {
    expect(isValidCnpj('11.222.333/0001-81')).toBe(true)
  })

  it('rejeita dígitos verificadores inválidos', () => {
    expect(isValidCnpj('12.ABC.345/01DE-00')).toBe(false)
  })
})
