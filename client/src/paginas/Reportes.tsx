import { useEffect, useState, type FormEvent } from 'react'
import { clientesApi, reportesApi } from '../api/servicios'
import { Alerta } from '../componentes/Alerta'
import { Cargando } from '../componentes/Cargando'
import { fechaHora, hoyIso, moneda, primerDiaDelMesIso } from '../componentes/Formato'
import type { Cliente, ReporteEstadoCuenta } from '../tipos'
import { mostrarError } from './Clientes'

export function Reportes() {
  const [clientes, setClientes] = useState<Cliente[]>([])
  const [clienteId, setClienteId] = useState('')
  const [fechaInicio, setFechaInicio] = useState(primerDiaDelMesIso())
  const [fechaFin, setFechaFin] = useState(hoyIso())

  const [reporte, setReporte] = useState<ReporteEstadoCuenta | null>(null)
  const [json, setJson] = useState<string | null>(null)
  const [cargando, setCargando] = useState(false)
  const [error, setError] = useState<{ mensaje: string; detalles: string[] } | null>(null)

  useEffect(() => {
    async function cargarClientes() {
      try {
        const resultado = await clientesApi.listar('', 1, 100)
        setClientes(resultado.items)
        setClienteId((actual) => actual || String(resultado.items[0]?.id ?? ''))
      } catch (fallo) {
        mostrarError(fallo, setError)
      }
    }

    void cargarClientes()
  }, [])

  async function generar(evento: FormEvent) {
    evento.preventDefault()
    setCargando(true)
    setError(null)
    setJson(null)

    try {
      // Se piden las dos representaciones a la vez: la agrupada alimenta la tabla
      // y la plana es la que el enunciado exige entregar en JSON.
      const [agrupado, plano] = await Promise.all([
        reportesApi.estadoCuenta(clienteId, fechaInicio, fechaFin),
        reportesApi.plano(clienteId, fechaInicio, fechaFin),
      ])

      setReporte(agrupado)
      setJson(JSON.stringify(plano, null, 2))
    } catch (fallo) {
      mostrarError(fallo, setError)
      setReporte(null)
    } finally {
      setCargando(false)
    }
  }

  return (
    <section>
      <div className="seccion-cabecera">
        <div>
          <h1>Estado de cuenta</h1>
          <p className="seccion-descripcion">
            Reporte por cliente y rango de fechas. Se muestran las cuentas con sus saldos y el
            detalle de movimientos.
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

      <form className="panel formulario-linea" onSubmit={generar}>
        <label className="crecer">
          Cliente
          <select value={clienteId} onChange={(evento) => setClienteId(evento.target.value)} required>
            {clientes.map((cliente) => (
              <option key={cliente.id} value={cliente.id}>
                {cliente.clienteId} - {cliente.nombreCompleto}
              </option>
            ))}
          </select>
        </label>

        <label>
          Desde
          <input
            type="date"
            value={fechaInicio}
            onChange={(evento) => setFechaInicio(evento.target.value)}
            required
          />
        </label>

        <label>
          Hasta
          <input
            type="date"
            value={fechaFin}
            onChange={(evento) => setFechaFin(evento.target.value)}
            required
          />
        </label>

        <button type="submit" className="boton-primario" disabled={cargando || !clienteId}>
          {cargando ? 'Generando' : 'Generar'}
        </button>
      </form>

      {cargando && <Cargando mensaje="Generando el reporte" />}

      {reporte && !cargando && (
        <>
          <div className="resumen-cuenta">
            <div>
              <span>Cliente</span>
              <strong>{reporte.cliente}</strong>
            </div>
            <div>
              <span>Cuentas</span>
              <strong>{reporte.totalCuentas}</strong>
            </div>
            <div>
              <span>Movimientos</span>
              <strong>{reporte.totalMovimientos}</strong>
            </div>
          </div>

          {reporte.cuentas.map((cuenta) => (
            <div key={cuenta.idCuenta} className="panel">
              <div className="panel-cabecera">
                <h2>
                  Cuenta {cuenta.numeroCuenta} <small>{cuenta.tipoCuenta}</small>
                </h2>
                <div className="panel-totales">
                  <span>
                    Saldo inicial <strong>{moneda(cuenta.saldoInicial)}</strong>
                  </span>
                  <span>
                    Creditos <strong className="positiva">{moneda(cuenta.totalCreditos)}</strong>
                  </span>
                  <span>
                    Debitos <strong className="negativa">{moneda(cuenta.totalDebitos)}</strong>
                  </span>
                  <span>
                    Disponible <strong className="destacada">{moneda(cuenta.saldoDisponible)}</strong>
                  </span>
                </div>
              </div>

              <div className="tabla-contenedor">
                <table className="tabla">
                  <thead>
                    <tr>
                      <th>Fecha</th>
                      <th>Tipo</th>
                      <th className="numerica">Movimiento</th>
                      <th className="numerica">Saldo</th>
                      <th>Descripcion</th>
                    </tr>
                  </thead>
                  <tbody>
                    {cuenta.movimientos.length === 0 && (
                      <tr>
                        <td colSpan={5} className="tabla-vacia">
                          Sin movimientos en el rango seleccionado.
                        </td>
                      </tr>
                    )}

                    {cuenta.movimientos.map((movimiento) => (
                      <tr key={movimiento.id}>
                        <td>{fechaHora(movimiento.fecha)}</td>
                        <td>{movimiento.tipoMovimiento}</td>
                        <td
                          className={
                            movimiento.valor < 0 ? 'numerica negativa' : 'numerica positiva'
                          }
                        >
                          {moneda(movimiento.valor)}
                        </td>
                        <td className="numerica">{moneda(movimiento.saldo)}</td>
                        <td>{movimiento.descripcion ?? '-'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ))}

          {json && (
            <details className="panel">
              <summary>Respuesta JSON del endpoint /api/reportes</summary>
              <pre className="json">{json}</pre>
            </details>
          )}
        </>
      )}
    </section>
  )
}
