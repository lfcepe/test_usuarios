// El backend guarda y devuelve todo el texto de negocio en mayusculas, asi que la
// interfaz no lo reformatea: mostrar "Jose Lema" cuando en la base dice
// "JOSE LEMA" confundiria a quien despues consulte los datos directamente.

const formatoMoneda = new Intl.NumberFormat('es-EC', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
})

export function moneda(valor: number): string {
  return formatoMoneda.format(valor)
}

export function fechaCorta(valor: string): string {
  const fecha = new Date(valor)

  return Number.isNaN(fecha.getTime())
    ? valor
    : fecha.toLocaleDateString('es-EC', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

export function fechaHora(valor: string): string {
  const fecha = new Date(valor)

  return Number.isNaN(fecha.getTime())
    ? valor
    : fecha.toLocaleString('es-EC', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
}

/// Fecha de hoy en formato ISO corto, que es lo que esperan los input date.
export function hoyIso(): string {
  return new Date().toISOString().slice(0, 10)
}

export function primerDiaDelMesIso(): string {
  const hoy = new Date()

  return new Date(hoy.getFullYear(), hoy.getMonth(), 1).toISOString().slice(0, 10)
}
