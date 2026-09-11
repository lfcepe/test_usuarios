import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/ContextoAuth'
import { Alerta } from '../componentes/Alerta'

export function Login() {
  const { iniciarSesionCorreo, iniciarSesionGoogle, entrarEnModoDemostracion, firebaseDisponible } =
    useAuth()

  const navegar = useNavigate()
  const ubicacion = useLocation()
  const destino = (ubicacion.state as { destino?: string } | null)?.destino ?? '/clientes'

  const [correo, setCorreo] = useState('')
  const [contrasenia, setContrasenia] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    setError(null)
    setEnviando(true)

    try {
      await iniciarSesionCorreo(correo, contrasenia)
      navegar(destino, { replace: true })
    } catch (fallo) {
      setError(traducirErrorFirebase(fallo))
    } finally {
      setEnviando(false)
    }
  }

  async function entrarConGoogle() {
    setError(null)

    try {
      await iniciarSesionGoogle()
      navegar(destino, { replace: true })
    } catch (fallo) {
      setError(traducirErrorFirebase(fallo))
    }
  }

  function entrarSinAutenticacion() {
    entrarEnModoDemostracion()
    navegar(destino, { replace: true })
  }

  return (
    <div className="login-pantalla">
      <div className="login-tarjeta">
        <div className="login-marca">
          <span className="cabecera-logo">SB</span>
          <h1>Sistema Bancario</h1>
          <p>Gestion de clientes, cuentas y movimientos</p>
        </div>

        {error && <Alerta tono="error" mensaje={error} onCerrar={() => setError(null)} />}

        {!firebaseDisponible && (
          <Alerta
            tono="aviso"
            mensaje="Firebase no esta configurado"
            detalles={[
              'Copie client/.env.example como client/.env y complete las variables VITE_FIREBASE_*.',
              'Mientras tanto puede entrar en modo demostracion.',
            ]}
          />
        )}

        <form onSubmit={enviar} className="formulario">
          <label>
            Correo electronico
            <input
              type="email"
              value={correo}
              onChange={(evento) => setCorreo(evento.target.value)}
              disabled={!firebaseDisponible}
              required
              autoComplete="username"
            />
          </label>

          <label>
            Contrasenia
            <input
              type="password"
              value={contrasenia}
              onChange={(evento) => setContrasenia(evento.target.value)}
              disabled={!firebaseDisponible}
              required
              autoComplete="current-password"
            />
          </label>

          <button type="submit" className="boton-primario" disabled={!firebaseDisponible || enviando}>
            {enviando ? 'Entrando' : 'Iniciar sesion'}
          </button>
        </form>

        <button
          type="button"
          className="boton-secundario ancho-completo"
          onClick={entrarConGoogle}
          disabled={!firebaseDisponible}
        >
          Continuar con Google
        </button>

        <button type="button" className="boton-enlace" onClick={entrarSinAutenticacion}>
          Entrar en modo demostracion
        </button>
      </div>
    </div>
  )
}

/// Traduce los codigos de Firebase a mensajes que le sirvan a la persona.
function traducirErrorFirebase(fallo: unknown): string {
  const codigo = (fallo as { code?: string })?.code ?? ''

  switch (codigo) {
    case 'auth/invalid-credential':
    case 'auth/wrong-password':
    case 'auth/user-not-found':
      // Se responde lo mismo en los tres casos a proposito: distinguirlos
      // permitiria averiguar que correos estan registrados.
      return 'Las credenciales no son validas.'
    case 'auth/too-many-requests':
      return 'Demasiados intentos fallidos. Espere unos minutos antes de reintentar.'
    case 'auth/network-request-failed':
      return 'No se pudo contactar con Firebase. Revise su conexion.'
    case 'auth/popup-closed-by-user':
      return 'Se cerro la ventana de Google antes de terminar.'
    default:
      return (fallo as Error)?.message ?? 'No se pudo iniciar sesion.'
  }
}
