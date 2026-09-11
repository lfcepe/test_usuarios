import { useEffect, useState, type FormEvent } from 'react'
import type { Catalogo, Cliente, ClienteFormulario } from '../tipos'

interface Props {
  cliente: Cliente | null
  tiposDocumento: Catalogo[]
  generos: Catalogo[]
  enviando: boolean
  onGuardar: (datos: ClienteFormulario) => void
  onCancelar: () => void
}

function formularioVacio(tiposDocumento: Catalogo[], generos: Catalogo[]): ClienteFormulario {
  return {
    primerNombre: '',
    segundoNombre: '',
    primerApellido: '',
    segundoApellido: '',
    idTipoDocumento: tiposDocumento[0]?.id ?? 0,
    numeroDocumento: '',
    idGenero: generos[0]?.id ?? 0,
    direccionDomicilio: '',
    numeroCelular: '',
    email: '',
    fechaNacimiento: '',
    contrasenia: '',
    estado: true,
  }
}

export function FormularioCliente({
  cliente,
  tiposDocumento,
  generos,
  enviando,
  onGuardar,
  onCancelar,
}: Props) {
  const [datos, setDatos] = useState<ClienteFormulario>(() =>
    formularioVacio(tiposDocumento, generos),
  )

  useEffect(() => {
    if (!cliente) {
      setDatos(formularioVacio(tiposDocumento, generos))
      return
    }

    setDatos({
      primerNombre: cliente.primerNombre,
      segundoNombre: cliente.segundoNombre ?? '',
      primerApellido: cliente.primerApellido,
      segundoApellido: cliente.segundoApellido ?? '',
      idTipoDocumento: cliente.idTipoDocumento,
      numeroDocumento: cliente.numeroDocumento,
      idGenero: cliente.idGenero,
      direccionDomicilio: cliente.direccionDomicilio,
      numeroCelular: cliente.numeroCelular,
      email: cliente.email,
      fechaNacimiento: cliente.fechaNacimiento,
      // En edicion se deja en blanco: vacio significa "no cambiar la clave".
      contrasenia: '',
      estado: cliente.estado,
    })
  }, [cliente, tiposDocumento, generos])

  function actualizar<K extends keyof ClienteFormulario>(campo: K, valor: ClienteFormulario[K]) {
    setDatos((anterior) => ({ ...anterior, [campo]: valor }))
  }

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    onGuardar(datos)
  }

  return (
    <form className="formulario formulario-rejilla" onSubmit={enviar}>
      <label>
        Primer nombre
        <input
          value={datos.primerNombre}
          onChange={(evento) => actualizar('primerNombre', evento.target.value)}
          maxLength={64}
          required
        />
      </label>

      <label>
        Segundo nombre
        <input
          value={datos.segundoNombre}
          onChange={(evento) => actualizar('segundoNombre', evento.target.value)}
          maxLength={64}
        />
      </label>

      <label>
        Primer apellido
        <input
          value={datos.primerApellido}
          onChange={(evento) => actualizar('primerApellido', evento.target.value)}
          maxLength={64}
          required
        />
      </label>

      <label>
        Segundo apellido
        <input
          value={datos.segundoApellido}
          onChange={(evento) => actualizar('segundoApellido', evento.target.value)}
          maxLength={64}
        />
      </label>

      <label>
        Tipo de documento
        <select
          value={datos.idTipoDocumento}
          onChange={(evento) => actualizar('idTipoDocumento', Number(evento.target.value))}
        >
          {tiposDocumento.map((tipo) => (
            <option key={tipo.id} value={tipo.id}>
              {tipo.item}
            </option>
          ))}
        </select>
      </label>

      <label>
        Numero de documento
        <input
          value={datos.numeroDocumento}
          onChange={(evento) => actualizar('numeroDocumento', evento.target.value)}
          minLength={5}
          maxLength={20}
          required
        />
      </label>

      <label>
        Genero
        <select
          value={datos.idGenero}
          onChange={(evento) => actualizar('idGenero', Number(evento.target.value))}
        >
          {generos.map((genero) => (
            <option key={genero.id} value={genero.id}>
              {genero.item}
            </option>
          ))}
        </select>
      </label>

      <label>
        Fecha de nacimiento
        <input
          type="date"
          value={datos.fechaNacimiento}
          onChange={(evento) => actualizar('fechaNacimiento', evento.target.value)}
          required
        />
      </label>

      <label className="ancho-doble">
        Direccion
        <input
          value={datos.direccionDomicilio}
          onChange={(evento) => actualizar('direccionDomicilio', evento.target.value)}
          maxLength={1000}
          required
        />
      </label>

      <label>
        Celular
        <input
          value={datos.numeroCelular}
          onChange={(evento) => actualizar('numeroCelular', evento.target.value)}
          pattern="[0-9]{10}"
          title="Diez digitos"
          required
        />
      </label>

      <label>
        Correo electronico
        <input
          type="email"
          value={datos.email}
          onChange={(evento) => actualizar('email', evento.target.value)}
          maxLength={500}
          required
        />
      </label>

      <label>
        Contrasenia
        <input
          type="password"
          value={datos.contrasenia}
          onChange={(evento) => actualizar('contrasenia', evento.target.value)}
          minLength={cliente ? 0 : 4}
          maxLength={64}
          required={!cliente}
          placeholder={cliente ? 'Dejar en blanco para conservarla' : ''}
        />
      </label>

      <label className="casilla">
        <input
          type="checkbox"
          checked={datos.estado}
          onChange={(evento) => actualizar('estado', evento.target.checked)}
        />
        Cliente activo
      </label>

      <div className="formulario-acciones ancho-doble">
        <button type="button" className="boton-secundario" onClick={onCancelar}>
          Cancelar
        </button>
        <button type="submit" className="boton-primario" disabled={enviando}>
          {enviando ? 'Guardando' : 'Guardar'}
        </button>
      </div>
    </form>
  )
}
