export function Cargando({ mensaje = 'Cargando' }: { mensaje?: string }) {
  return (
    <div className="cargando" role="status" aria-live="polite">
      <span className="cargando-indicador" aria-hidden="true" />
      <span>{mensaje}</span>
    </div>
  )
}
