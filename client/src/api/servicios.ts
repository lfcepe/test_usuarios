import { consulta, http } from './clienteHttp'
import type {
  Catalogo,
  Cliente,
  ClienteFormulario,
  Cuenta,
  CuentaFormulario,
  Movimiento,
  MovimientoFormulario,
  ReporteEstadoCuenta,
  ResultadoPaginado,
} from '../tipos'

export const clientesApi = {
  listar: (busqueda: string, pagina: number, tamanio = 10) =>
    http.get<ResultadoPaginado<Cliente>>(`/clientes${consulta({ busqueda, pagina, tamanio })}`),

  obtener: (id: number) => http.get<Cliente>(`/clientes/${id}`),

  crear: (datos: ClienteFormulario) => http.post<Cliente>('/clientes', datos),

  actualizar: (id: number, datos: ClienteFormulario) =>
    http.put<Cliente>(`/clientes/${id}`, datos),

  cambiarEstado: (id: number, estado: boolean) =>
    http.patch<Cliente>(`/clientes/${id}/estado`, { estado }),

  eliminar: (id: number) => http.delete<void>(`/clientes/${id}`),
}

export const cuentasApi = {
  listar: (clienteId: number | undefined, pagina: number, tamanio = 10) =>
    http.get<ResultadoPaginado<Cuenta>>(`/cuentas${consulta({ clienteId, pagina, tamanio })}`),

  crear: (datos: CuentaFormulario) => http.post<Cuenta>('/cuentas', datos),

  actualizar: (id: number, idTipoCuenta: number, estado: boolean) =>
    http.put<Cuenta>(`/cuentas/${id}`, { idTipoCuenta, estado }),

  cambiarEstado: (id: number, estado: boolean) =>
    http.patch<Cuenta>(`/cuentas/${id}/estado`, { estado }),
}

export const movimientosApi = {
  listar: (numeroCuenta: string, pagina: number, tamanio = 10) =>
    http.get<ResultadoPaginado<Movimiento>>(
      `/movimientos${consulta({ numeroCuenta, pagina, tamanio })}`,
    ),

  registrar: (datos: MovimientoFormulario) =>
    http.post<Movimiento>('/movimientos', {
      numeroCuenta: datos.numeroCuenta,
      valor: datos.valor,
      descripcion: datos.descripcion || null,
    }),

  actualizar: (id: number, valor: number, descripcion: string) =>
    http.put<Movimiento>(`/movimientos/${id}`, { valor, descripcion: descripcion || null }),
}

export const reportesApi = {
  estadoCuenta: (clienteId: string, fechaInicio: string, fechaFin: string) =>
    http.get<ReporteEstadoCuenta>(
      `/reportes/estado-cuenta${consulta({ clienteId, fechaInicio, fechaFin })}`,
    ),

  /// Version plana, con el formato literal del enunciado.
  plano: (cliente: string, fechaInicio: string, fechaFin: string) =>
    http.get<Record<string, unknown>[]>(
      `/reportes${consulta({ cliente, fecha: `${fechaInicio},${fechaFin}` })}`,
    ),
}

export const catalogosApi = {
  obtener: (raiz: string) => http.get<Catalogo[]>(`/catalogos${consulta({ raiz })}`),
}
