import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { cuentasApi, movimientosApi } from '../api/servicios'
import { Alerta } from '../componentes/Alerta'
import { Cargando } from '../componentes/Cargando'
import { Paginador } from '../componentes/Paginador'
import { fechaHora, moneda } from '../componentes/Formato'
import type { Cuenta, Movimiento } from '../tipos'
import { mostrarError } from './Clientes'

export function Movimientos() {
  const [cuentas, setCuentas] = useState<Cuenta[]>([])
  const [movimientos, setMovimientos] = useState<Movimiento[]>([])

  const [numeroCuenta, setNumeroCuenta] = useState('')
  const [pagina, setPagina] = useState(1)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [total, setTotal] = useState(0)

  const [cargando, setCargando] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<{ mensaje: string; detalles: string[] } | null>(null)
  const [exito, setExito] = useState<string | null>(null)

  const [valor, setValor] = useState('')
  const [descripcion, setDescripcion] = useState('')

  const cuentaSeleccionada = cuentas.find((cuenta) => cuenta.numeroCuenta === numeroCuenta)

  const cargarMovimientos = useCallback(async () => {
    if (!numeroCuenta) {
      setMovimientos([])
      setTotal(0)
      return
    }

    setCargando(true)

    try {
      const resultado = await movimientosApi.listar(numeroCuenta, pagina)
      setMovimientos(resultado.items)
      setTotalPaginas(resultado.totalPaginas)
      setTotal(resultado.total)
    } catch (fallo) {
      mostrarError(fallo, setError)
    } finally {
      setCargando(false)
    }
  }, [numeroCuenta, pagina])

  useEffect(() => {
    void cargarMovimientos()
  }, [cargarMovimientos])

  const recargarCuentas = useCallback(async () => {
    try {
      const resultado = await cuentasApi.listar(undefined, 1, 100)
      setCuentas(resultado.items)

      setNumeroCuenta((actual) => actual || resultado.items[0]?.numeroCuenta || '')
    } catch (fallo) {
      mostrarError(fallo, setError)
    }
  }, [])

  useEffect(() => {
    void recargarCuentas()
  }, [recargarCuentas])

  async function registrar(evento: FormEvent) {
    evento.preventDefault()
    setEnviando(true)
    setError(null)
    setExito(null)

    try {
      const movimiento = await movimientosApi.registrar({
        numeroCuenta,
        valor: Number(valor),
        descripcion,
      })

      setExito(
        `Movimiento registrado por ${moneda(movimiento.valor)}. ` +
          `Saldo resultante: ${moneda(movimiento.saldo)}.`,
      )

      setValor('')
      setDescripcion('')
      setPagina(1)

      // Se recargan las cuentas para que el saldo del encabezado quede al dia.
      await Promise.all([cargarMovimientos(), recargarCuentas()])
    } catch (fallo) {
      mostrarError(fallo, setError)
    } finally {
      setEnviando(false)
    }
  }

  return (
    <section>
      <div className="seccion-cabecera">
        <div>
          <h1>Movimientos</h1>
          <p className="seccion-descripcion">
            Un valor positivo deposita y uno negativo retira. Si el retiro supera el saldo, la API
            responde "Saldo no disponible".
          </p>
        </div>
      </div>

      {error && (
        <Alerta
          tono="error"
          mensaje={error.mensaje}
          detalles={error.detalles}
          onCerrar={() => setError(null)}
        />
      )}

      {exito && <Alerta tono="exito" mensaje={exito} onCerrar={() => setExito(null)} />}

      <form className="panel formulario-linea" onSubmit={registrar}>
        <label>
          Cuenta
          <select
            value={numeroCuenta}
            onChange={(evento) => {
              setPagina(1)
              setNumeroCuenta(evento.target.value)
            }}
            required
          >
            {cuentas.map((cuenta) => (
              <option key={cuenta.id} value={cuenta.numeroCuenta}>
                {cuenta.numeroCuenta} - {cuenta.tipoCuenta} - {cuenta.cliente}
              </option>
            ))}
          </select>
        </label>

        <label>
          Valor
          <input
            type="number"
            step="0.01"
            value={valor}
            onChange={(evento) => setValor(evento.target.value)}
            placeholder="600 o -575"
            required
          />
        </label>

        <label className="crecer">
          Descripcion
          <input
            value={descripcion}
            onChange={(evento) => setDescripcion(evento.target.value)}
            maxLength={256}
            placeholder="Opcional"
          />
        </label>

        <button type="submit" className="boton-primario" disabled={enviando || !numeroCuenta}>
          {enviando ? 'Registrando' : 'Registrar'}
        </button>
      </form>

      {cuentaSeleccionada && (
        <div className="resumen-cuenta">
          <div>
            <span>Saldo inicial</span>
            <strong>{moneda(cuentaSeleccionada.saldoInicial)}</strong>
          </div>
          <div>
            <span>Saldo disponible</span>
            <strong className="destacada">{moneda(cuentaSeleccionada.saldoDisponible)}</strong>
          </div>
          <div>
            <span>Estado</span>
            <strong>{cuentaSeleccionada.estadoDescripcion}</strong>
          </div>
        </div>
      )}

      {cargando ? (
        <Cargando mensaje="Cargando movimientos" />
      ) : (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Tipo</th>
                <th className="numerica">Valor</th>
                <th className="numerica">Saldo</th>
                <th>Descripcion</th>
              </tr>
            </thead>
            <tbody>
              {movimientos.length === 0 && (
                <tr>
                  <td colSpan={5} className="tabla-vacia">
                    La cuenta todavia no tiene movimientos.
                  </td>
                </tr>
              )}

              {movimientos.map((movimiento) => (
                <tr key={movimiento.id}>
                  <td>{fechaHora(movimiento.fecha)}</td>
                  <td>{movimiento.tipoMovimiento}</td>
                  <td className={movimiento.valor < 0 ? 'numerica negativa' : 'numerica positiva'}>
                    {moneda(movimiento.valor)}
                  </td>
                  <td className="numerica">{moneda(movimiento.saldo)}</td>
                  <td>{movimiento.descripcion ?? '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Paginador pagina={pagina} totalPaginas={totalPaginas} total={total} onCambiar={setPagina} />
    </section>
  )
}
