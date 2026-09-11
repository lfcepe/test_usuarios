import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { catalogosApi, clientesApi, cuentasApi } from '../api/servicios'
import { Alerta } from '../componentes/Alerta'
import { Cargando } from '../componentes/Cargando'
import { Modal } from '../componentes/Modal'
import { Paginador } from '../componentes/Paginador'
import { moneda } from '../componentes/Formato'
import type { Catalogo, Cliente, Cuenta } from '../tipos'
import { mostrarError } from './Clientes'

export function Cuentas() {
  const [cuentas, setCuentas] = useState<Cuenta[]>([])
  const [clientes, setClientes] = useState<Cliente[]>([])
  const [tiposCuenta, setTiposCuenta] = useState<Catalogo[]>([])

  const [filtroCliente, setFiltroCliente] = useState<number | ''>('')
  const [pagina, setPagina] = useState(1)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [total, setTotal] = useState(0)

  const [cargando, setCargando] = useState(true)
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<{ mensaje: string; detalles: string[] } | null>(null)
  const [exito, setExito] = useState<string | null>(null)
  const [modalAbierto, setModalAbierto] = useState(false)

  const [formulario, setFormulario] = useState({
    idCliente: 0,
    numeroCuenta: '',
    idTipoCuenta: 0,
    saldoInicial: '0',
    estado: true,
  })

  const cargar = useCallback(async () => {
    setCargando(true)

    try {
      const resultado = await cuentasApi.listar(
        filtroCliente === '' ? undefined : filtroCliente,
        pagina,
      )

      setCuentas(resultado.items)
      setTotalPaginas(resultado.totalPaginas)
      setTotal(resultado.total)
    } catch (fallo) {
      mostrarError(fallo, setError)
    } finally {
      setCargando(false)
    }
  }, [filtroCliente, pagina])

  useEffect(() => {
    void cargar()
  }, [cargar])

  useEffect(() => {
    async function cargarApoyo() {
      try {
        // El selector de titular se alimenta del microservicio de Clientes y el de
        // tipos del de Cuentas: es la vista la que junta los dos dominios.
        const [listaClientes, tipos] = await Promise.all([
          clientesApi.listar('', 1, 100),
          catalogosApi.obtener('TIPO_CUENTA'),
        ])

        setClientes(listaClientes.items)
        setTiposCuenta(tipos)
        setFormulario((anterior) => ({
          ...anterior,
          idCliente: listaClientes.items[0]?.id ?? 0,
          idTipoCuenta: tipos[0]?.id ?? 0,
        }))
      } catch (fallo) {
        mostrarError(fallo, setError)
      }
    }

    void cargarApoyo()
  }, [])

  async function crear(evento: FormEvent) {
    evento.preventDefault()
    setEnviando(true)
    setError(null)

    try {
      const creada = await cuentasApi.crear({
        idCliente: formulario.idCliente,
        numeroCuenta: formulario.numeroCuenta,
        idTipoCuenta: formulario.idTipoCuenta,
        saldoInicial: Number(formulario.saldoInicial),
        estado: formulario.estado,
      })

      setExito(`Cuenta ${creada.numeroCuenta} aperturada.`)
      setModalAbierto(false)
      setFormulario((anterior) => ({ ...anterior, numeroCuenta: '', saldoInicial: '0' }))
      await cargar()
    } catch (fallo) {
      mostrarError(fallo, setError)
    } finally {
      setEnviando(false)
    }
  }

  async function alternarEstado(cuenta: Cuenta) {
    setError(null)

    try {
      await cuentasApi.cambiarEstado(cuenta.id, !cuenta.estado)
      await cargar()
    } catch (fallo) {
      mostrarError(fallo, setError)
    }
  }

  return (
    <section>
      <div className="seccion-cabecera">
        <div>
          <h1>Cuentas</h1>
          <p className="seccion-descripcion">
            El saldo disponible solo cambia registrando movimientos, nunca editando la cuenta.
          </p>
        </div>
        <button type="button" className="boton-primario" onClick={() => setModalAbierto(true)}>
          Nueva cuenta
        </button>
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

      <div className="barra-filtros">
        <label className="filtro-inline">
          Titular
          <select
            value={filtroCliente}
            onChange={(evento) => {
              setPagina(1)
              setFiltroCliente(evento.target.value === '' ? '' : Number(evento.target.value))
            }}
          >
            <option value="">Todos</option>
            {clientes.map((cliente) => (
              <option key={cliente.id} value={cliente.id}>
                {cliente.nombreCompleto}
              </option>
            ))}
          </select>
        </label>
      </div>

      {cargando ? (
        <Cargando mensaje="Cargando cuentas" />
      ) : (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead>
              <tr>
                <th>Numero</th>
                <th>Tipo</th>
                <th>Titular</th>
                <th className="numerica">Saldo inicial</th>
                <th className="numerica">Saldo disponible</th>
                <th>Estado</th>
                <th aria-label="Acciones" />
              </tr>
            </thead>
            <tbody>
              {cuentas.length === 0 && (
                <tr>
                  <td colSpan={7} className="tabla-vacia">
                    No hay cuentas registradas.
                  </td>
                </tr>
              )}

              {cuentas.map((cuenta) => (
                <tr key={cuenta.id}>
                  <td className="mono">{cuenta.numeroCuenta}</td>
                  <td>{cuenta.tipoCuenta}</td>
                  <td>{cuenta.cliente ?? `Cliente ${cuenta.idCliente}`}</td>
                  <td className="numerica">{moneda(cuenta.saldoInicial)}</td>
                  <td className="numerica destacada">{moneda(cuenta.saldoDisponible)}</td>
                  <td>
                    <span className={cuenta.estado ? 'etiqueta activa' : 'etiqueta inactiva'}>
                      {cuenta.estadoDescripcion ?? (cuenta.estado ? 'ACTIVA' : 'INACTIVA')}
                    </span>
                  </td>
                  <td className="celda-acciones">
                    <button type="button" onClick={() => alternarEstado(cuenta)}>
                      {cuenta.estado ? 'Desactivar' : 'Activar'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Paginador pagina={pagina} totalPaginas={totalPaginas} total={total} onCambiar={setPagina} />

      <Modal titulo="Nueva cuenta" abierto={modalAbierto} onCerrar={() => setModalAbierto(false)}>
        <form className="formulario formulario-rejilla" onSubmit={crear}>
          <label className="ancho-doble">
            Titular
            <select
              value={formulario.idCliente}
              onChange={(evento) =>
                setFormulario({ ...formulario, idCliente: Number(evento.target.value) })
              }
              required
            >
              {clientes.map((cliente) => (
                <option key={cliente.id} value={cliente.id}>
                  {cliente.clienteId} - {cliente.nombreCompleto}
                </option>
              ))}
            </select>
          </label>

          <label>
            Numero de cuenta
            <input
              value={formulario.numeroCuenta}
              onChange={(evento) =>
                setFormulario({ ...formulario, numeroCuenta: evento.target.value })
              }
              pattern="[0-9]{4,20}"
              title="Entre 4 y 20 digitos"
              required
            />
          </label>

          <label>
            Tipo de cuenta
            <select
              value={formulario.idTipoCuenta}
              onChange={(evento) =>
                setFormulario({ ...formulario, idTipoCuenta: Number(evento.target.value) })
              }
            >
              {tiposCuenta.map((tipo) => (
                <option key={tipo.id} value={tipo.id}>
                  {tipo.item}
                </option>
              ))}
            </select>
          </label>

          <label>
            Saldo inicial
            <input
              type="number"
              step="0.01"
              min="0"
              value={formulario.saldoInicial}
              onChange={(evento) =>
                setFormulario({ ...formulario, saldoInicial: evento.target.value })
              }
              required
            />
          </label>

          <label className="casilla">
            <input
              type="checkbox"
              checked={formulario.estado}
              onChange={(evento) => setFormulario({ ...formulario, estado: evento.target.checked })}
            />
            Cuenta activa
          </label>

          <div className="formulario-acciones ancho-doble">
            <button
              type="button"
              className="boton-secundario"
              onClick={() => setModalAbierto(false)}
            >
              Cancelar
            </button>
            <button type="submit" className="boton-primario" disabled={enviando}>
              {enviando ? 'Guardando' : 'Aperturar'}
            </button>
          </div>
        </form>
      </Modal>
    </section>
  )
}
