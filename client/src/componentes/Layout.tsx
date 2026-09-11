import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/ContextoAuth'

const SECCIONES = [
  { ruta: '/clientes', etiqueta: 'Clientes' },
  { ruta: '/cuentas', etiqueta: 'Cuentas' },
  { ruta: '/movimientos', etiqueta: 'Movimientos' },
  { ruta: '/reportes', etiqueta: 'Reportes' },
]

export function Layout() {
  const { usuario, modoDemostracion, cerrarSesion } = useAuth()
  const navegar = useNavigate()

  async function salir() {
    await cerrarSesion()
    navegar('/login', { replace: true })
  }

  return (
    <div className="aplicacion">
      <header className="cabecera">
        <div className="cabecera-marca">
          <span className="cabecera-logo">SB</span>
          <div>
            <strong>Sistema Bancario</strong>
            <small>Clientes y cuentas</small>
          </div>
        </div>

        <nav className="cabecera-nav">
          {SECCIONES.map((seccion) => (
            <NavLink
              key={seccion.ruta}
              to={seccion.ruta}
              className={({ isActive }) => (isActive ? 'nav-enlace activo' : 'nav-enlace')}
            >
              {seccion.etiqueta}
            </NavLink>
          ))}
        </nav>

        <div className="cabecera-sesion">
          <span className="cabecera-usuario">
            {usuario?.email ?? (modoDemostracion ? 'Modo demostracion' : 'Sin sesion')}
          </span>
          <button type="button" className="boton-secundario" onClick={salir}>
            Salir
          </button>
        </div>
      </header>

      {modoDemostracion && (
        <div className="cinta-demo">
          Modo demostracion: Firebase no esta configurado y las peticiones salen sin token.
        </div>
      )}

      <main className="contenido">
        <Outlet />
      </main>
    </div>
  )
}
