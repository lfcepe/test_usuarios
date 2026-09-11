// Espejo de los DTO que exponen los microservicios. Se mantienen a mano y no se
// generan desde Swagger para que cualquier cambio de contrato rompa aqui, en
// tiempo de compilacion, y no en el navegador del usuario.

export interface ResultadoPaginado<T> {
  items: T[]
  pagina: number
  tamanio: number
  total: number
  totalPaginas: number
}

export interface Catalogo {
  id: number
  detalleCatalogo: string | null
  item: string | null
  idRaiz: number | null
}

export interface Cliente {
  id: number
  clienteId: string
  primerNombre: string
  segundoNombre: string | null
  primerApellido: string
  segundoApellido: string | null
  nombreCompleto: string
  idTipoDocumento: number
  tipoDocumento: string | null
  numeroDocumento: string
  idGenero: number
  genero: string | null
  direccionDomicilio: string
  numeroCelular: string
  email: string
  fechaNacimiento: string
  edad: number | null
  estado: boolean
  idEstadoCliente: number
  estadoDescripcion: string | null
  totalCuentas: number
  fechaCreacion: string
  fechaModificacion: string | null
}

export interface ClienteFormulario {
  primerNombre: string
  segundoNombre: string
  primerApellido: string
  segundoApellido: string
  idTipoDocumento: number
  numeroDocumento: string
  idGenero: number
  direccionDomicilio: string
  numeroCelular: string
  email: string
  fechaNacimiento: string
  contrasenia: string
  estado: boolean
}

export interface Cuenta {
  id: number
  idCliente: number
  clienteId: string | null
  cliente: string | null
  numeroCuenta: string
  idTipoCuenta: number
  tipoCuenta: string | null
  saldoInicial: number
  saldoDisponible: number
  estado: boolean
  idEstadoCuenta: number
  estadoDescripcion: string | null
  fechaCreacion: string
  fechaModificacion: string | null
}

export interface CuentaFormulario {
  idCliente: number
  numeroCuenta: string
  idTipoCuenta: number
  saldoInicial: number
  estado: boolean
}

export interface Movimiento {
  id: number
  idCuenta: number
  numeroCuenta: string
  fecha: string
  idTipoMovimiento: number
  tipoMovimiento: string | null
  valor: number
  saldo: number
  descripcion: string | null
  idEstadoMovimiento: number
  estadoDescripcion: string | null
}

export interface MovimientoFormulario {
  numeroCuenta: string
  valor: number
  descripcion: string
}

export interface ReporteCuenta {
  idCuenta: number
  numeroCuenta: string
  tipoCuenta: string | null
  saldoInicial: number
  saldoDisponible: number
  estado: boolean
  totalDebitos: number
  totalCreditos: number
  movimientos: Movimiento[]
}

export interface ReporteEstadoCuenta {
  idCliente: number
  clienteId: string | null
  cliente: string
  fechaInicio: string
  fechaFin: string
  totalCuentas: number
  totalMovimientos: number
  cuentas: ReporteCuenta[]
}

/// Error normalizado que devuelven las dos APIs (RFC 7807 mas el campo codigo).
export interface ProblemaApi {
  title: string
  status: number
  detail?: string
  codigo?: string
  traceId?: string
  errors?: Record<string, string[]>
}
