export function normalizeCnpj(value: string) {
  return value.replace(/[^A-Z0-9]/gi, '').toUpperCase().slice(0, 14)
}

export function maskCnpj(value: string) {
  return normalizeCnpj(value)
    .replace(/^([A-Z0-9]{2})([A-Z0-9])/, '$1.$2')
    .replace(/^([A-Z0-9]{2})\.([A-Z0-9]{3})([A-Z0-9])/, '$1.$2.$3')
    .replace(/\.([A-Z0-9]{3})([A-Z0-9])/, '.$1/$2')
    .replace(/\/([A-Z0-9]{4})([A-Z0-9])/, '/$1-$2')
}

export function isValidCnpj(value: string) {
  const cnpj = value.trim().replace(/[./-]/g, '').toUpperCase()
  if (!/^[A-Z0-9]{12}\d{2}$/.test(cnpj) || /^(\d)\1{13}$/.test(cnpj)) return false

  const digit = (length: number) => {
    let sum = 0
    let weight = length - 7
    for (let i = 0; i < length; i++) {
      sum += (cnpj.charCodeAt(i) - 48) * weight--
      if (weight < 2) weight = 9
    }
    const remainder = sum % 11
    return remainder < 2 ? 0 : 11 - remainder
  }

  return digit(12) === Number(cnpj[12]) && digit(13) === Number(cnpj[13])
}
