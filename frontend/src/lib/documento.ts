import { isValidCnpj, maskCnpj, normalizeCnpj } from '@/lib/cnpj'
import { isValidCpf, maskCpf, normalizeCpf } from '@/lib/cpf'

/** CPF (pessoa física) ou CNPJ (pessoa jurídica) — o cliente escolhe no cadastro. */
export type TipoPessoa = 'PF' | 'PJ'

export function normalizeDocumento(tipo: TipoPessoa, value: string) {
  return tipo === 'PF' ? normalizeCpf(value) : normalizeCnpj(value)
}

export function maskDocumento(tipo: TipoPessoa, value: string) {
  return tipo === 'PF' ? maskCpf(value) : maskCnpj(value)
}

export function isValidDocumento(tipo: TipoPessoa, value: string) {
  return tipo === 'PF' ? isValidCpf(value) : isValidCnpj(value)
}

export function labelDocumento(tipo: TipoPessoa) {
  return tipo === 'PF' ? 'CPF' : 'CNPJ'
}

export function placeholderDocumento(tipo: TipoPessoa) {
  return tipo === 'PF' ? '000.000.000-00' : '00.000.000/0000-00'
}
