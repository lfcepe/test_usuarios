import { useCallback, useEffect, useState } from 'react'
import { ErrorApi } from '../api/clienteHttp'
import { catalogosApi, clientesApi } from '../api/servicios'
import { Alerta } from '../componentes/Alerta'
import { Cargando } from '../componentes/Cargando'
import { FormularioCliente } from '../componentes/FormularioCliente'
import { Modal } from '../componentes/Modal'
import { Paginador } from '../componentes/Paginador'
import { fechaCorta } from '../componentes/Formato'
import type { Catalogo, Cliente, ClienteFormulario } from '../tipos'

export function Clientes() {
  const [clientes, setClientes] = useState<Cliente[]>([])
  const [tiposDocumento, setTiposDocumento] = useState<Catalogo[]>([])
  const [generos, setGeneros] = useState<Catalogo[]>([])

  const [busqueda, setBusqueda] = useState('')
  const [pagina, setPagina] = useState(1)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [total, setTotal] = useState(0)

  const [cargando, setCargando] = useState(true)
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<{ mensaje: string; detalles: string[] } | null>(null)
  const [exito, setExito] = useState<string | null>(null)

  const [modalAbierto, setModalAbierto] = useState(false)
  const [clienteEditado, setClienteEditado] = useState<Cliente | null>(null)

  const cargar = useCallback(async () => {
    setCargando(true)

    try {
      const resultado = await clientesApi.listar(busqueda, pagina)
      setClientes(resultado.items)
      setTotalPaginas(resultado.totalPaginas)
      setTotal(resultado.total)
    } catch (fallo) {
      mostrarError(fallo, setError)
    } finally {
      setCargando(false)
    }
  }, [busqueda, pagina])

  useEffect(() => {
    void cargar()
  }, [cargar])

  useEffect(() => {
    // Los catalogos cambian muy poco; se piden una sola vez al montar la pantalla.
    async function cargarCatalogos() {
      try {
        const [documentos, generosCatalogo] = await Promise.all([
          catalogosApi.obtener('TIPO_DOCUMENTO'),
          catalogosApi.obtener('GENERO'),
        ])

        setTiposDocumento(documentos)
        setGeneros(generosCatalogo)
      } catch (fallo) {
        mostrarError(fallo, setError)
      }
    }

    void cargarCatalogos()
  }, [])

  function abrirAlta() {
    setClienteEditado(null)
    setModalAbierto(true)
  }

  function abrirEdicion(cliente: Cliente) {
    setClienteEditado(cliente)
    setModalAbierto(true)
  }

  async function guardar(datos: ClienteFormulario) {
    setEnviando(true)
    setError(null)

    try {
      if (clienteEditado) {
        await clientesApi.actualizar(clienteEditado.id, datos)
        setExito(`Cliente ${clienteEditado.clienteId} actualizado.`)
      } else {
        const creado = await clientesApi.crear(datos)
        setExito(`Cliente ${creado.clienteId} creado.`)
      }

      setModalAbierto(false)
      await cargar()
    } catch (fallo) {
      mostrarError(fallo, setError)
    } finally {
      setEnviando(false)
    }
  }

  async function alternarEstado(cliente: Cliente) {
    setError(null)

    try {
      await clientesApi.cambiarEstado(cliente.id, !cliente.estado)
      await cargar()
    } catch (fallo) {
      mostrarError(fallo, setError)
    }
  }

  async function eliminar(cliente: Cliente) {
    if (!window.confirm(`Dar de baja al cliente ${cliente.nombreCompleto}?`)) {
      return
    }

    setError(null)

    try {
      await clientesApi.eliminar(cliente.id)
      setExito(`Cliente ${cliente.clienteId} dado de baja.`)
      await cargar()
    } catch (fallo) {
      mostrarError(fallo, setError)
    }
  }

  return (
    <section>
      <div className="seccion-cabecera">
        <div>
          <h1>Clientes</h1>
          <p className="seccion-descripcion">
            Alta, consulta, edicion y baja. Cada cliente hereda los datos de una persona.
          </p>
        </div>
        <button type="button" className="boton-primario" onClick={abrirAlta}>
          Nuevo cliente
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
        <input
          type="search"
          placeholder="Buscar por nombre, apellido, documento o codigo"
          value={busqueda}
          onChange={(evento) => {
            setPagina(1)
            setBusqueda(evento.target.value)
          }}
        />
      </div>

      {cargando ? (
        <Cargando mensaje="Cargando clientes" />
      ) : (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead>
              <tr>
                <th>Codigo</th>
                <th>Nombre</th>
                <th>Documento</th>
                <th>Celular</th>
                <th>Edad</th>
                <th>Cuentas</th>
                <th>Estado</th>
                <th>Creado</th>
                <th aria-label="Acciones" />
              </tr>
            </thead>
            <tbody>
              {clientes.length === 0 && (
                <tr>
                  <td colSpan={9} className="tabla-vacia">
                    No hay clientes que coincidan con la busqueda.
                  </td>
                </tr>
              )}

              {clientes.map((cliente) => (
                <tr key={cliente.id}>
                  <td className="mono">{cliente.clienteId}</td>
                  <td>{cliente.nombreCompleto}</td>
                  <td className="mono">{cliente.numeroDocumento}</td>
                  <td className="mono">{cliente.numeroCelular}</td>
                  <td>{cliente.edad ?? '-'}</td>
                  <td>{cliente.totalCuentas}</td>
                  <td>
                    <span className={cliente.estado ? 'etiqueta activa' : 'etiqueta inactiva'}>
                      {cliente.estadoDescripcion ?? (cliente.estado ? 'ACTIVO' : 'INACTIVO')}
                    </span>
                  </td>
                  <td>{fechaCorta(cliente.fechaCreacion)}</td>
                  <td className="celda-acciones">
                    <button type="button" onClick={() => abrirEdicion(cliente)}>
                      Editar
                    </button>
                    <button type="button" onClick={() => alternarEstado(cliente)}>
                      {cliente.estado ? 'Desactivar' : 'Activar'}
                    </button>
                    <button type="button" className="peligro" onClick={() => eliminar(cliente)}>
                      Eliminar
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Paginador
        pagina={pagina}
        totalPaginas={totalPaginas}
        total={total}
        onCambiar={setPagina}
      />

      <Modal
        titulo={clienteEditado ? `Editar ${clienteEditado.clienteId}` : 'Nuevo cliente'}
        abierto={modalAbierto}
        onCerrar={() => setModalAbierto(false)}
      >
        <FormularioCliente
          cliente={clienteEditado}
          tiposDocumento={tiposDocumento}
          generos={generos}
          enviando={enviando}
          onGuardar={guardar}
          onCancelar={() => setModalAbierto(false)}
        />
      </Modal>
    </section>
  )
}

/// Extrae el mensaje y los errores por campo de cualquier fallo de las APIs.
export function mostrarError(
  fallo: unknown,
  asignar: (valor: { mensaje: string; detalles: string[] }) => void,
) {
  if (fallo instanceof ErrorApi) {
    asignar({ mensaje: fallo.message, detalles: fallo.mensajesPorCampo })
    return
  }

  asignar({
    mensaje: (fallo as Error)?.message ?? 'Ocurrio un error inesperado.',
    detalles: [],
  })
}
