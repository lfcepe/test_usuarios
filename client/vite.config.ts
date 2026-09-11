import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// En desarrollo el navegador habla con Vite y Vite reenvia a cada microservicio.
// En produccion ese papel lo hace nginx (ver client/nginx.conf), de modo que el
// codigo de la aplicacion siempre llama a rutas relativas /api/... y no necesita
// saber en que puerto vive cada servicio.
const CLIENTES_API = process.env.CLIENTES_API ?? 'http://localhost:8081'
const CUENTAS_API = process.env.CUENTAS_API ?? 'http://localhost:8082'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api/clientes': CLIENTES_API,
      '/api/personas': CLIENTES_API,
      '/api/catalogos': CLIENTES_API,
      '/api/cuentas': CUENTAS_API,
      '/api/movimientos': CUENTAS_API,
      '/api/reportes': CUENTAS_API,
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
  },
})
