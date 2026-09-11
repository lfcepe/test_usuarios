import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import {
  GoogleAuthProvider,
  onAuthStateChanged,
  signInWithEmailAndPassword,
  signInWithPopup,
  signOut,
  type User,
} from 'firebase/auth'
import { firebaseConfigurado, obtenerAuth } from '../firebase/config'

const CLAVE_MODO_DEMO = 'sistema-bancario.modo-demostracion'

interface ValorContexto {
  usuario: User | null
  modoDemostracion: boolean
  cargando: boolean
  autenticado: boolean
  firebaseDisponible: boolean
  iniciarSesionCorreo: (correo: string, contrasenia: string) => Promise<void>
  iniciarSesionGoogle: () => Promise<void>
  entrarEnModoDemostracion: () => void
  cerrarSesion: () => Promise<void>
  obtenerToken: () => Promise<string | null>
}

const ContextoAuth = createContext<ValorContexto | null>(null)

export function ProveedorAuth({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<User | null>(null)
  const [cargando, setCargando] = useState(true)
  const [modoDemostracion, setModoDemostracion] = useState(
    () => sessionStorage.getItem(CLAVE_MODO_DEMO) === 'true',
  )

  useEffect(() => {
    if (!firebaseConfigurado) {
      setCargando(false)
      return
    }

    // onAuthStateChanged tambien restaura la sesion al recargar la pagina, asi
    // que hace de suscripcion y de carga inicial a la vez.
    const cancelar = onAuthStateChanged(obtenerAuth(), (usuarioActual) => {
      setUsuario(usuarioActual)
      setCargando(false)
    })

    return cancelar
  }, [])

  const valor = useMemo<ValorContexto>(() => ({
    usuario,
    modoDemostracion,
    cargando,
    autenticado: usuario !== null || modoDemostracion,
    firebaseDisponible: firebaseConfigurado,

    iniciarSesionCorreo: async (correo, contrasenia) => {
      await signInWithEmailAndPassword(obtenerAuth(), correo, contrasenia)
    },

    iniciarSesionGoogle: async () => {
      await signInWithPopup(obtenerAuth(), new GoogleAuthProvider())
    },

    entrarEnModoDemostracion: () => {
      sessionStorage.setItem(CLAVE_MODO_DEMO, 'true')
      setModoDemostracion(true)
    },

    cerrarSesion: async () => {
      sessionStorage.removeItem(CLAVE_MODO_DEMO)
      setModoDemostracion(false)

      if (firebaseConfigurado) {
        await signOut(obtenerAuth())
      }
    },

    obtenerToken: async () => {
      if (!firebaseConfigurado || !usuario) {
        return null
      }

      // El SDK cachea el token y solo lo renueva cuando esta proximo a expirar,
      // de modo que llamarlo en cada peticion no cuesta una ida a la red.
      return usuario.getIdToken()
    },
  }), [usuario, modoDemostracion, cargando])

  return <ContextoAuth.Provider value={valor}>{children}</ContextoAuth.Provider>
}

export function useAuth(): ValorContexto {
  const contexto = useContext(ContextoAuth)

  if (!contexto) {
    throw new Error('useAuth debe usarse dentro de ProveedorAuth.')
  }

  return contexto
}
