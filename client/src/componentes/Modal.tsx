import type { ReactNode } from 'react'

interface Props {
  titulo: string
  abierto: boolean
  onCerrar: () => void
  children: ReactNode
}

export function Modal({ titulo, abierto, onCerrar, children }: Props) {
  if (!abierto) {
    return null
  }

  return (
    <div className="modal-fondo" onClick={onCerrar}>
      {/* Se detiene la propagacion para que un clic dentro del cuadro no lo cierre. */}
      <div className="modal" role="dialog" aria-modal="true" onClick={(evento) => evento.stopPropagation()}>
        <header className="modal-cabecera">
          <h2>{titulo}</h2>
          <button type="button" onClick={onCerrar} aria-label="Cerrar">x</button>
        </header>
        <div className="modal-contenido">{children}</div>
      </div>
    </div>
  )
}
