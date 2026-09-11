interface Props {
  pagina: number
  totalPaginas: number
  total: number
  onCambiar: (pagina: number) => void
}

export function Paginador({ pagina, totalPaginas, total, onCambiar }: Props) {
  if (total === 0) {
    return null
  }

  return (
    <div className="paginador">
      <span>
        Pagina {pagina} de {Math.max(totalPaginas, 1)} ({total} registros)
      </span>
      <div className="paginador-botones">
        <button type="button" disabled={pagina <= 1} onClick={() => onCambiar(pagina - 1)}>
          Anterior
        </button>
        <button
          type="button"
          disabled={pagina >= totalPaginas}
          onClick={() => onCambiar(pagina + 1)}
        >
          Siguiente
        </button>
      </div>
    </div>
  )
}
