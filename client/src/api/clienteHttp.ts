import type { ProblemaApi } from '../tipos'

// Todas las llamadas salen contra rutas relativas. En desarrollo las reparte el
// proxy de Vite y en produccion nginx; el codigo de la aplicacion no necesita
// saber en que puerto vive cada microservicio.
const BASE = '/api'

/// Error de negocio devuelto por las APIs, ya interpretado.
export class ErrorApi extends Error {
  constructor(
    mensaje: string,
    readonly estado: number,
    readonly codigo?: string,
    readonly errores?: Record<string, string[]>,
  ) {
    super(mensaje)
    this.name = 'ErrorApi'
  }

  /// Mensajes por campo, listos para pintar bajo cada input del formulario.
  get mensajesPorCampo(): string[] {
    if (!this.errores) {
      return []
    }

    return Object.values(this.errores).flat()
  }
}

type ProveedorToken = () => Promise<string | null>

let proveedorToken: ProveedorToken = async () => null

/// Conecta el cliente HTTP con la sesion de Firebase sin acoplarlo a React.
export function registrarProveedorToken(proveedor: ProveedorToken) {
  proveedorToken = proveedor
}

async function peticion<T>(ruta: string, opciones: RequestInit = {}): Promise<T> {
  const token = await proveedorToken()

  const cabeceras: Record<string, string> = {
    Accept: 'application/json',
    ...(opciones.headers as Record<string, string> | undefined),
  }

  if (opciones.body) {
    cabeceras['Content-Type'] = 'application/json'
  }

  if (token) {
    cabeceras.Authorization = `Bearer ${token}`
  }

  const respuesta = await fetch(`${BASE}${ruta}`, { ...opciones, headers: cabeceras })

  if (respuesta.status === 204) {
    return undefined as T
  }

  const texto = await respuesta.text()
  const cuerpo = texto ? JSON.parse(texto) : null

  if (!respuesta.ok) {
    const problema = cuerpo as ProblemaApi | null

    throw new ErrorApi(
      problema?.detail || problema?.title || `Error ${respuesta.status}`,
      respuesta.status,
      problema?.codigo,
      problema?.errors,
    )
  }

  return cuerpo as T
}

export const http = {
  get: <T>(ruta: string) => peticion<T>(ruta),
  post: <T>(ruta: string, cuerpo: unknown) =>
    peticion<T>(ruta, { method: 'POST', body: JSON.stringify(cuerpo) }),
  put: <T>(ruta: string, cuerpo: unknown) =>
    peticion<T>(ruta, { method: 'PUT', body: JSON.stringify(cuerpo) }),
  patch: <T>(ruta: string, cuerpo: unknown) =>
    peticion<T>(ruta, { method: 'PATCH', body: JSON.stringify(cuerpo) }),
  delete: <T>(ruta: string) => peticion<T>(ruta, { method: 'DELETE' }),
}

/// Convierte un objeto en cadena de consulta omitiendo los valores vacios.
export function consulta(parametros: Record<string, string | number | boolean | undefined | null>): string {
  const partes = Object.entries(parametros)
    .filter(([, valor]) => valor !== undefined && valor !== null && valor !== '')
    .map(([clave, valor]) => `${encodeURIComponent(clave)}=${encodeURIComponent(String(valor))}`)

  return partes.length > 0 ? `?${partes.join('&')}` : ''
}
