import { initializeApp, type FirebaseApp } from 'firebase/app'
import { getAuth, type Auth } from 'firebase/auth'

const configuracion = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
  appId: import.meta.env.VITE_FIREBASE_APP_ID,
  // Opcional: lo emite la consola de Firebase para Google Analytics. La
  // autenticacion no lo necesita, asi que se pasa tal cual llegue.
  measurementId: import.meta.env.VITE_FIREBASE_MEASUREMENT_ID,
}

// Sin API key no tiene sentido inicializar: Firebase lanzaria una excepcion al
// arrancar y la pantalla quedaria en blanco. Es preferible detectarlo aqui y que
// la pantalla de login ofrezca el modo demostracion.
export const firebaseConfigurado = Boolean(configuracion.apiKey && configuracion.projectId)

let aplicacion: FirebaseApp | null = null
let autenticacion: Auth | null = null

if (firebaseConfigurado) {
  aplicacion = initializeApp(configuracion)
  autenticacion = getAuth(aplicacion)
}

export function obtenerAuth(): Auth {
  if (!autenticacion) {
    throw new Error(
      'Firebase no esta configurado. Copie client/.env.example como client/.env y complete las variables.',
    )
  }

  return autenticacion
}
