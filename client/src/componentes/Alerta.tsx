type Tono = 'error' | 'exito' | 'aviso'

interface Props {
  tono: Tono
  mensaje: string
  detalles?: string[]
  onCerrar?: () => void
}

export function Alerta({ tono, mensaje, detalles, onCerrar }: Props) {
  return (
    <div className={`alerta alerta-${tono}`} role={tono === 'error' ? 'alert' : 'status'}>
      <div className="alerta-cuerpo">
        <strong>{mensaje}</strong>
        {detalles && detalles.length > 0 && (
          <ul>
            {detalles.map((detalle) => (
              <li key={detalle}>{detalle}</li>
            ))}
          </ul>
        )}
      </div>
      {onCerrar && (
        <button type="button" className="alerta-cerrar" onClick={onCerrar} aria-label="Cerrar">
          x
        </button>
      )}
    </div>
  )
}
