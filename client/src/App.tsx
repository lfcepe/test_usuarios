import { useEffect } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { ProveedorAuth, useAuth } from './auth/ContextoAuth'
import { RutaProtegida } from './auth/RutaProtegida'
import { registrarProveedorToken } from './api/clienteHttp'
import { Layout } from './componentes/Layout'
import { Clientes } from './paginas/Clientes'
import { Cuentas } from './paginas/Cuentas'
import { Login } from './paginas/Login'
import { Movimientos } from './paginas/Movimientos'
import { Reportes } from './paginas/Reportes'

/// Conecta el cliente HTTP con la sesion activa.
/// Se hace en un componente y no en el modulo para poder leer el contexto.
function PuenteToken() {
  const { obtenerToken } = useAuth()

  useEffect(() => {
    registrarProveedorToken(obtenerToken)
  }, [obtenerToken])

  return null
}

export default function App() {
  return (
    <ProveedorAuth>
      <PuenteToken />

      <Routes>
        <Route path="/login" element={<Login />} />

        <Route
          element={
            <RutaProtegida>
              <Layout />
            </RutaProtegida>
          }
        >
          <Route path="/clientes" element={<Clientes />} />
          <Route path="/cuentas" element={<Cuentas />} />
          <Route path="/movimientos" element={<Movimientos />} />
          <Route path="/reportes" element={<Reportes />} />
        </Route>

        <Route path="*" element={<Navigate to="/clientes" replace />} />
      </Routes>
    </ProveedorAuth>
  )
}
