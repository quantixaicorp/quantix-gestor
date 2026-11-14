export function normalizeCpf(value: string) {
  return value.replace(/\D/g, '').slice(0, 11)
}

export function maskCpf(value: string) {
  return normalizeCpf(value)
    .replace(/^(\d{3})(\d)/, '$1.$2')
    .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d{1,2})$/, '.$1-$2')
}

export function isValidCpf(value: string) {
  const cpf = normalizeCpf(value)
  if (cpf.length !== 11 || /^(\d)\1{10}$/.test(cpf)) return false

  const digit = (length: number) => {
    let sum = 0
    for (let i = 0; i < length; i++) sum += Number(cpf[i]) * (length + 1 - i)
    const remainder = (sum * 10) % 11
    return remainder === 10 ? 0 : remainder
  }

  return digit(9) === Number(cpf[9]) && digit(10) === Number(cpf[10])
}
