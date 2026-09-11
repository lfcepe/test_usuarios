import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './ContextoAuth'
import { Cargando } from '../componentes/Cargando'

/// Impide entrar a las pantallas de datos sin haber pasado por el login.
export function RutaProtegida({ children }: { children: ReactNode }) {
  const { autenticado, cargando } = useAuth()
  const ubicacion = useLocation()

  if (cargando) {
    return <Cargando mensaje="Verificando la sesion" />
  }

  if (!autenticado) {
    // Se guarda el destino para volver a el despues de iniciar sesion.
    return <Navigate to="/login" state={{ destino: ubicacion.pathname }} replace />
  }

  return <>{children}</>
}
