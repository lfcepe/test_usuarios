# Documentacion tecnica

Este es un recorrido completo de la solucion, cada clase relevante y por que se
tomo cada decision. El [README](README.md) cubre como levantar el sistema; este documento
cubre como esta construido.

---

## 1. Decisiones de arquitectura

### 1.1 Dos bases de datos, no una

Cada microservicio es dueno de sus datos. `devsu_clientes` y `devsu_cuentas` viven en el
mismo servidor PostgreSQL por comodidad de despliegue, pero son bases separadas y ninguna
consulta cruza de una a otra.

Compartir una sola base habria sido mas rapido de escribir y habria dejado un monolito
repartido en dos procesos: cualquier cambio de esquema obligaria a desplegar los dos
servicios a la vez, que es justo lo que una arquitectura de microservicios trata de evitar.

El precio es la consistencia eventual, y se paga de forma explicita: Cuentas mantiene una
copia local del cliente en la tabla `ClientesRef`, alimentada por eventos.

### 1.2 Clean Architecture con cuatro proyectos

```
Api  ->  Application  ->  Domain
 |            |
 +----> Infrastructure ---+
```

| Proyecto | Contiene | Depende de |
|---|---|---|
| `Domain` | Entidades, objetos de valor, excepciones, interfaces de repositorio, constantes de catalogo | Nada |
| `Application` | Casos de uso (MediatR), DTOs, validadores, behaviors, interfaces de servicios de infraestructura | Domain, Devsu.Contracts |
| `Infrastructure` | DbContext, mapeos de EF, repositorios, mensajeria, cliente HTTP, hash de contrasenias | Application, Domain |
| `Api` | Controladores, middleware, DI, Swagger, health checks | Application, Infrastructure |

`Domain` no tiene ni una referencia de NuGet. Es la comprobacion mas barata de que la regla
de dependencia se respeta: si alguna vez hiciera falta agregar un paquete ahi, seria senal
de que la logica esta en la capa equivocada.

### 1.3 CQRS con MediatR

Cada operacion es un `Command` o un `Query` con su `Handler` y, cuando aplica, su
`Validator`. El controlador solo traduce HTTP a un mensaje y devuelve el resultado.

Ventaja concreta: los behaviors del pipeline (validacion y trazas) se aplican a todos los
casos de uso sin repetir una linea, y anadir uno nuevo no obliga a tocar los existentes.

Se fijo MediatR en la version 12.4.1, que es la ultima con licencia Apache-2.0. La 13 pasa a
licencia comercial y no tiene sentido introducir esa dependencia en una prueba tecnica.

### 1.4 Mapeo manual en lugar de AutoMapper

Los DTOs se construyen con metodos de extension en `Mapeos.cs`. Son pocas conversiones y el
mapeo queda explicito y verificado por el compilador: si manana se renombra una propiedad de
la entidad, el error salta al compilar y no en una peticion en produccion.

### 1.5 Normalizacion del texto a mayusculas

Todo el texto de negocio se almacena en mayusculas y sin espacios sobrantes. La regla vive en
`Normalizador.ATextoNormalizado` (capa de dominio) y es la misma que aplica
`sp_registrar_catalogo` en la base de datos.

El motivo es practico: sin normalizar, "Jose Lema", "JOSE LEMA" y "jose  lema" conviven como
si fueran tres personas distintas, y las busquedas por nombre pasan a depender de como
escribiera el operador de turno.

Se usa `CultureInfo.InvariantCulture` a proposito. Con la cultura turca, `ToUpper('i')`
devuelve una i con punto y el valor almacenado dejaria de coincidir con el de cualquier otro
servidor.

La unica excepcion es el hash de la contrasenia: va en Base64, distingue mayusculas de
minusculas y quedaria inservible. Para eso existe `Guardas.TextoSensible`, que valida y
limpia espacios sin tocar el caso.

---

## 2. Base de datos

### 2.1 Tablas de `devsu_clientes`

| Tabla | Proposito |
|---|---|
| `Catalogos` | Tabla auto referenciada con los valores parametrizables del sistema |
| `Personas` | Datos personales. Tabla base de la herencia |
| `Cliente` | Datos de cliente. Comparte clave primaria con `Personas` |
| `ResumenCuentasCliente` | Read model con el numero de cuentas activas, alimentado por eventos |
| `OutboxMensajes` | Eventos pendientes de publicar |
| `MensajesProcesados` | Claves de idempotencia de los eventos ya consumidos |

### 2.2 Tablas de `devsu_cuentas`

| Tabla | Proposito |
|---|---|
| `Catalogos` | Copia del catalogo con los mismos identificadores |
| `ClientesRef` | Replica local de solo lectura del cliente |
| `CuentasPersona` | Cuentas bancarias |
| `Movimientos` | Asientos sobre las cuentas |
| `OutboxMensajes` | Eventos pendientes de publicar |
| `MensajesProcesados` | Claves de idempotencia |

### 2.3 La herencia Cliente / Persona

El enunciado pide que `Cliente` herede de `Persona`. En un modelo relacional hay tres formas
de mapear herencia y se eligio **Table-Per-Type**: cada clase tiene su tabla y la hija
comparte la clave primaria con la base.

```sql
CREATE TABLE "Cliente"(
    "Id" INT PRIMARY KEY,
    ...
    CONSTRAINT fk_IdPersona_Personas FOREIGN KEY ("Id") REFERENCES "Personas"("Id") ON DELETE CASCADE
);
```

En EF Core se declara con una sola linea en `PersonaConfiguration`:

```csharp
constructor.UseTptMappingStrategy();
```

Las otras dos opciones se descartaron por motivos concretos. Table-Per-Hierarchy meteria las
columnas de cliente en `Personas` con valores nulos para las personas que no son clientes, y
la restriccion `NOT NULL` sobre la contrasenia dejaria de ser aplicable. Table-Per-Concrete
duplicaria los datos personales en las dos tablas.

Consecuencia a tener presente: las columnas `FechaCreacion` y `FechaModificacion` de la tabla
`Cliente` no se mapean. En TPT, las propiedades declaradas en la clase base pertenecen a la
tabla base, asi que la auditoria se lleva en `Personas`. Las columnas se conservan en el
script por fidelidad al modelo original y las rellena su `DEFAULT`.

### 2.4 Catalogos

`Catalogos` es auto referenciada. Las cabeceras llevan el nombre en `DetalleCatalogo` con
`IdRaiz` nulo; los items llevan el valor en `Item` apuntando a la cabecera.

| Id | DetalleCatalogo | Item | IdRaiz |
|---|---|---|---|
| 1 | TIPO_DOCUMENTO | NULL | NULL |
| 2 | NULL | CEDULA | 1 |
| 12 | ESTADO_CLIENTE | NULL | NULL |
| 13 | NULL | ACTIVO | 12 |
| 16 | NULL | AHORROS | 15 |

La carga se hace con `sp_registrar_catalogo(detalle, item)`, que es la traduccion a
PostgreSQL del procedimiento de carga del modelo original: si la cabecera no existe la crea,
y si ya existe le cuelga el item. Se le anadieron dos cosas al original: la normalizacion a
mayusculas es consistente y la insercion del item comprueba que no exista ya, para que el
script se pueda volver a ejecutar sin duplicar filas.

Los identificadores del 1 al 26 son fijos porque **viajan dentro de los eventos de
integracion**. Si el mismo numero significara cosas distintas en cada base, la
sincronizacion produciria datos incorrectos en silencio. Por eso el script termina con una
comprobacion que aborta la carga si el orden se rompio:

```sql
IF NOT EXISTS (SELECT 1 FROM "Catalogos" WHERE "Id" = 13 AND "Item" = 'ACTIVO') THEN
    RAISE EXCEPTION 'Los identificadores de catalogo no coinciden con CatalogoIds...';
END IF;
```

La clase `CatalogoIds` de cada microservicio es el espejo en codigo de esa lista.

### 2.5 Calculo de la edad

Tres objetos porque son dos problemas distintos:

| Objeto | Que hace |
|---|---|
| `fn_calcular_edad(DATE)` | Funcion pura reutilizable. Devuelve anios cumplidos |
| `trg_personas_calcular_edad()` + `tr_personas_edad` | Trigger `BEFORE INSERT OR UPDATE`. Mantiene `Edad` al dia en cada escritura y rechaza fechas de nacimiento futuras |
| `sp_actualizar_edades()` | Recalcula todas las edades. Pensado para un job diario |

El procedimiento no es redundante: la edad cambia con el paso del tiempo sin que la fila se
modifique, asi que sin el, la edad de una persona que no se edita nunca quedaria congelada en
el valor que tenia el dia del alta.

Una columna `GENERATED ALWAYS AS` no sirve: PostgreSQL exige una expresion inmutable y
`CURRENT_DATE` no lo es.

En EF Core, `Persona.Edad` se declara `ValueGeneratedOnAddOrUpdate()`: la aplicacion nunca la
escribe y Npgsql la recupera con `RETURNING`. Como las pruebas de integracion usan SQLite,
que no tiene el trigger, el `DbContext` detecta el proveedor y calcula la edad en codigo con
`Persona.CalcularEdad`, que replica exactamente lo que hace la funcion SQL.

### 2.6 Comillas dobles en todo el script

PostgreSQL pasa a minusculas todo identificador que no venga entrecomillado, y EF Core con
Npgsql si entrecomilla los nombres del modelo. Sin las comillas, el script crearia la tabla
`personas` y la aplicacion buscaria `"Personas"` sin encontrarla. El fallo solo aparece en
tiempo de ejecucion, lo que lo hace especialmente caro de diagnosticar.

---

## 3. Microservicio de Clientes

### 3.1 Domain

**`Comun/EntidadBase.cs`**
`EntidadBase` aporta el `Id`. `EntidadAuditable` anade `FechaCreacion` y `FechaModificacion`
con `RegistrarCreacion(DateTime)` y `RegistrarModificacion(DateTime)`. Las fechas las fija el
`DbContext` al guardar, no los handlers.

**`Comun/Normalizador.cs`**
- `ATextoNormalizado(string)`: recorta, colapsa espacios internos y pasa a mayusculas.
- `ATextoLimpio(string)`: solo recorta, para valores donde el caso importa.

**`Comun/Guardas.cs`**
Validaciones de invariantes que ademas normalizan.

| Metodo | Que valida |
|---|---|
| `TextoRequerido(valor, campo, max)` | Obligatorio y longitud maxima. Devuelve el texto normalizado |
| `TextoOpcional(...)` | Igual, pero devuelve `null` si viene vacio |
| `TextoSensible(...)` | Valida sin normalizar a mayusculas. Para el hash |
| `Email(valor)` | Formato con expresion regular, sobre el valor ya normalizado |
| `SoloDigitos(valor, campo, longitud)` | Longitud exacta y solo digitos. Para el celular |
| `Documento(valor)` | Minimo 5 caracteres alfanumericos |
| `IdCatalogoValido(valor, campo)` | Entero positivo |

La duplicidad con FluentValidation es intencional y las dos capas tienen papeles distintos:
el validador protege el borde HTTP y devuelve un 422 con el detalle por campo; las guardas
protegen la entidad de cualquier otra via de entrada (un consumidor de eventos, un seed, una
prueba) y garantizan que no exista un `Cliente` invalido en memoria bajo ninguna
circunstancia.

**`Entidades/DatosPersonales.cs`**
Record con los once campos personales. Existe para no arrastrar un constructor de once
parametros posicionales, donde invertir dos cadenas del mismo tipo compila igual y falla en
produccion.

**`Entidades/Persona.cs`**

| Miembro | Descripcion |
|---|---|
| `Crear(datos, activa)` | Fabrica. Valida todas las invariantes |
| `AplicarDatosPersonales(datos)` | Reemplaza los datos validando y normalizando |
| `CambiarEstadoPersona(bool)` | Cambia `IdEstadoPersona` al item de catalogo correspondiente |
| `NombreCompleto` | Concatena los cuatro nombres omitiendo los ausentes |
| `CalcularEdad(nacimiento, hoy)` | Estatico y puro. Replica `fn_calcular_edad` |
| `EstablecerEdad(int)` | Solo lo invoca el `DbContext` en proveedores sin trigger |
| `Edad` | Propiedad de lectura. La escribe la base de datos |

**`Entidades/Cliente.cs`** (la entidad que evalua F5)

| Miembro | Descripcion |
|---|---|
| `Crear(datos, clienteId, contraseniaHash, activo)` | Fabrica. La contrasenia debe llegar ya cifrada |
| `FormatearClienteId(consecutivo)` | Da formato `CLI-000001`. Rechaza consecutivos no positivos |
| `ValidarContraseniaEnClaro(string)` | Minimo de cuatro caracteres. Estatico, porque se valida antes de tener la entidad |
| `CambiarEstado(bool)` | Cambia el estado del cliente y el de su persona a la vez |
| `CambiarContrasenia(hash)` | Reemplaza el hash |
| `Actualizar(datos, activo)` | Reemplaza datos personales conservando la identidad |
| `RequiereHashInicial()` | Indica si la fila arrastra el marcador de `datos-prueba.sql` |
| `Activo` | `IdEstadoCliente == CatalogoIds.EstadoCliente.Activo` |

El dominio **no conoce el algoritmo de hash**. Recibe el resultado ya calculado. Cambiar
PBKDF2 por Argon2 manana no toca ni una linea del dominio.

El minimo de cuatro caracteres para la contrasenia sale del enunciado, que usa claves como
`1234`. En un sistema real seria mucho mas exigente.

**`Excepciones/`**
`ExcepcionDominio` es abstracta y aporta `Codigo`. Derivan `RecursoNoEncontradoException`,
`ReglaNegocioException`, `RecursoDuplicadoException` y `ConflictoConcurrenciaException`. El
codigo permite al consumidor del API reaccionar sin parsear el texto del mensaje.

**`Repositorios/`**
Seis interfaces. Devuelven entidades de dominio, nunca `IQueryable` ni DTOs: exponer
`IQueryable` dejaria escapar detalles del ORM a la capa de aplicacion y haria imposible
sustituir el repositorio en pruebas sin levantar un proveedor real.

`IRepositorioCliente` merece una nota: tiene `ObtenerPorIdAsync` (sin seguimiento, con los
catalogos incluidos, para consultas) y `ObtenerParaEdicionAsync` (con seguimiento, para
comandos). Son dos necesidades distintas y mezclarlas cuesta rendimiento en los listados o
correccion en las escrituras.

### 3.2 Application

**`Comun/IUnitOfWork.cs`**
- `GuardarCambiosAsync(ct)`: confirma todo lo acumulado.
- `EjecutarEnTransaccionAsync<T>(operacion, ct)`: envuelve varios guardados en una
  transaccion. Hace falta cuando el caso de uso necesita guardar dos veces, una para que la
  base genere el identificador y otra para encolar el evento que lo lleva dentro. Sin la
  transaccion, un fallo entre ambas dejaria un cliente creado sin evento publicado.

**`Comun/ValidadorCatalogos.cs`**
Comprueba contra la base que un identificador pertenezca a la raiz correcta. No se resuelve
con FluentValidation porque necesita ir a la base de datos, ni en la entidad porque el
dominio no consulta repositorios. Es una regla de aplicacion.

**`Comportamientos/ValidacionBehavior.cs`**
Ejecuta todos los `IValidator<TPeticion>` registrados antes del handler y agrupa **todos** los
errores en una sola excepcion. Se agrupan en lugar de cortar en el primero para que el
consumidor del API pueda corregir el formulario completo de una vez.

**`Comportamientos/RegistroBehavior.cs`**
Mide la duracion de cada caso de uso y marca como `Warning` los que superan 500 ms, para
poder detectar peticiones lentas en los logs sin activar trazas detalladas en produccion.

**Casos de uso**

| Caso de uso | Que hace |
|---|---|
| `CrearClienteCommand` | Valida catalogos, comprueba documento unico, cifra la clave, reserva el consecutivo, crea, guarda, encola `ClienteCreado`, crea el resumen y relee para devolver las descripciones |
| `ActualizarClienteCommand` | Reemplazo completo. La contrasenia vacia significa "no cambiarla" |
| `CambiarEstadoClienteCommand` | Alta o baja logica. Encola `ClienteEstadoCambiado` |
| `EliminarClienteCommand` | Baja logica por defecto; fisica con `definitivo=true` |
| `ObtenerClientesQuery` | Listado paginado con busqueda libre y filtro por estado |
| `ObtenerClientePorIdQuery` | Detalle |
| `ObtenerClientePorDocumentoQuery` | Busqueda por documento |
| `ObtenerPersonasQuery`, `ObtenerPersonaPorIdQuery` | Consulta de personas |
| `ObtenerCatalogoQuery` | Items de un catalogo por nombre de raiz |
| `SincronizarCuentaCommand` | Aplica los eventos que llegan de Cuentas sobre el read model |

`ObtenerClientesQuery` resuelve los totales de cuentas de toda la pagina con **una sola**
consulta (`ObtenerTotalesAsync` recibe la lista de ids). Con veinte clientes, la diferencia
entre una consulta y veintiuna se nota.

**Por que se relee la entidad despues de crear**
La entidad recien creada solo tiene los identificadores de catalogo; sus navegaciones no
estan cargadas y la `Edad` la acaba de calcular el trigger. Sin la relectura, el 201
devolveria `tipoDocumento: null` y `edad: null`. Cuesta una consulta y evita que el cliente
HTTP tenga que pedir el recurso otra vez.

### 3.3 Infrastructure

**`Persistencia/ClientesDbContext.cs`**
- `SaveChangesAsync` sobrescrito: rellena las fechas de auditoria y, si el proveedor no es
  Npgsql, calcula la edad en codigo.
- `OnModelCreating`: aplica las configuraciones del ensamblado y, para proveedores sin
  trigger, cambia `Edad` a `ValueGeneratedNever()` para que sea escribible.

**`Persistencia/Configuraciones/`**
Un `IEntityTypeConfiguration` por entidad. Todos los nombres de tabla, columna, indice y
restriccion coinciden caracter a caracter con `BaseDatos.sql`. Las relaciones con `Catalogos`
usan `DeleteBehavior.Restrict`: borrar un item de catalogo no debe arrastrar personas.

**`Persistencia/Repositorios/RepositorioCliente.cs`**
Las busquedas usan `ToLower()` y `Contains()` en lugar de `ILike` de PostgreSQL para que la
misma consulta funcione tambien con SQLite en las pruebas.

`ObtenerSiguienteConsecutivoAsync` llama a `nextval('seq_cliente_consecutivo')` en PostgreSQL.
Se usa una secuencia y no `MAX(Id) + 1` porque dos altas simultaneas leerian el mismo maximo
y generarian el mismo codigo, violando `uq_Cliente_ClienteId`. En SQLite cae a `MAX(Id) + 1`,
que no es seguro frente a concurrencia pero en pruebas no hay concurrencia.

**`Persistencia/UnitOfWork.cs`**
`EjecutarEnTransaccionAsync` reutiliza la transaccion si ya hay una abierta, y en caso
contrario la crea a traves de la estrategia de ejecucion (`CreateExecutionStrategy`). Ese
detalle es obligatorio: con reintentos activados, abrir la transaccion por fuera de la
estrategia produce el error "the configured execution strategy does not support user
initiated transactions".

**`Seguridad/ServicioHashContraseniaPbkdf2.cs`**
PBKDF2 con SHA-256, 100 000 iteraciones y salt aleatorio de 16 bytes. Formato almacenado:
`iteraciones.saltBase64.hashBase64`.

Guardar el numero de iteraciones dentro del propio hash permite subirlo en el futuro sin
invalidar las contrasenias existentes: cada una se verifica con las suyas.

La comparacion usa `CryptographicOperations.FixedTimeEquals`. Una comparacion normal tarda
mas cuanto mas coincidan los primeros bytes, y eso es un canal lateral explotable.

**`Persistencia/SeedContrasenias.cs`**
`datos-prueba.sql` no puede calcular PBKDF2, asi que inserta el literal `__PENDIENTE_HASH__`.
Al arrancar, este componente busca esas filas y las cifra con las claves del enunciado
(`CLI-000001` -> `1234`, y asi). Si encontrara una fila pendiente que no esta en esa lista, le
asigna una clave aleatoria: es preferible dejar la cuenta inaccesible antes que asignarle una
clave por defecto conocida, que es como se cuelan credenciales debiles en produccion.

### 3.4 Api

**`Program.cs`**
Serilog primero, para que los fallos de arranque queden registrados. Despues controladores,
Swagger, autenticacion opcional, Application, Infrastructure, health checks y CORS. El
middleware de excepciones va el primero de la cadena para capturar tambien lo que falle en
los middlewares posteriores.

La declaracion `public partial class Program { }` al final permite que
`WebApplicationFactory<Program>` instancie el host desde las pruebas de integracion.

**`Configuracion/ArranqueBaseDatos.cs`**
Espera a que PostgreSQL acepte conexiones, con quince intentos separados tres segundos. No
ejecuta migraciones a proposito: el esquema lo crea `BaseDatos.sql`, que es el entregable del
enunciado y la unica fuente de verdad del modelo. Si tras los quince intentos no hay
conexion, deja arrancar igualmente: los health checks reportaran el problema y el orquestador
podra reiniciar el contenedor sin que se pierdan los logs.

**`Middleware/MiddlewareExcepciones.cs`**
Traduce cualquier excepcion a `ProblemDetails`. Comprueba `Response.HasStarted` antes de
escribir: si ya se envio parte del cuerpo, reescribir la cabecera produciria una respuesta
corrupta, asi que solo registra el fallo.

En produccion, los 500 no exponen el stack trace pero si lo registran con el `traceId`, que
tambien viaja en la respuesta para poder cruzarlo con los logs.

---

## 4. Microservicio de Cuentas

### 4.1 El agregado Cuenta

`Cuenta` es la raiz del agregado: `Movimiento` solo nace y cambia a traves de ella, porque es
la unica que conoce el saldo y puede decidir si la operacion es legitima. Por eso los metodos
de mutacion de `Movimiento` son `internal`.

**`Cuenta.RegistrarMovimiento(valor, fecha, idTipoMovimiento?, descripcion?)`** es donde vive
la funcionalidad F2 y F3 completa:

1. Si la cuenta no esta activa, lanza `CuentaInactivaException`.
2. Redondea el importe a dos decimales.
3. Si el importe es cero, lanza `ReglaNegocioException`.
4. Deduce el tipo del signo. Si se indico uno explicito y no coincide con el signo, lo rechaza.
5. Calcula el saldo resultante.
6. **Si el saldo resultante es negativo, lanza `SaldoNoDisponibleException`.**
7. Crea el movimiento con el saldo posterior y actualiza `SaldoDisponible`.

El orden importa. La excepcion se lanza **antes** de crear el movimiento y de tocar el saldo,
de modo que una operacion rechazada deja la cuenta exactamente como estaba. Hay una prueba
dedicada a eso: `RegistrarMovimiento_RechazadoPorSaldo_NoDejaRastroEnLaCuenta`.

**`Cuenta.ActualizarMovimiento(idMovimiento, nuevoValor, descripcion)`** corrige un asiento y
recalcula la cadena completa de saldos. Cambiar el valor de un movimiento invalida el saldo de
ese y de todos los posteriores, asi que `RecalcularSaldos` recorre la coleccion ordenada por
fecha e id reconstruyendo la secuencia. Si en cualquier punto intermedio el saldo quedara
negativo, la correccion se rechaza entera: no se admite un historico que en algun momento
estuvo en descubierto, aunque el saldo final volviera a ser positivo.

Es una operacion cara a proposito. Corregir un asiento contable no deberia ser barato ni
frecuente.

### 4.2 Movimiento

| Campo | Significado |
|---|---|
| `Valor` | Positivo deposita, negativo retira |
| `Saldo` | Saldo de la cuenta **despues** de aplicar este movimiento |
| `IdTipoMovimiento` | Deducido del signo salvo que se fuerce |
| `IdEstadoMovimiento` | `APLICADO` al crearse |

Guardar el saldo posterior en cada movimiento es lo que permite que el reporte de estado de
cuenta se construya sin recalcular nada, y lo que hace auditable la secuencia.

### 4.3 ClienteRef y el resolutor

`ClienteRef` es la copia local del cliente. No es la fuente de verdad: la mantiene el
consumidor de eventos.

**`ResolutorCliente`** es donde se materializa la consistencia eventual del sistema:

1. Busca en `ClientesRef`. Es el caso normal y no cuesta nada.
2. Si no esta, consulta `GET /api/clientes/{id}` del otro microservicio.
3. Si responde, guarda la referencia para no repetir la llamada.
4. Si tampoco responde, lanza `ClienteNoSincronizadoException`, que el middleware traduce a
   un 409.

Se devuelve 409 y no 500 porque la condicion es transitoria: el evento puede llegar en
cualquier momento y el reintento del cliente tiene sentido. Nunca se inventa el dato del
titular: abrir una cuenta a un cliente que no se ha podido verificar seria peor que fallar.

### 4.4 El reporte (F4)

**`GenerarReporteQueryHandler`**

1. Resuelve el rango de fechas con `RangoFechas.Resolver`.
2. Localiza al cliente por id numerico o por codigo de negocio (`CLI-000002`).
3. Trae sus cuentas.
4. Trae los movimientos de **todas** las cuentas en una sola consulta y los agrupa en memoria.
   Con una consulta por cuenta, un cliente con diez cuentas costaria once viajes a la base
   para el mismo resultado.
5. Construye el DTO agrupado con totales de debitos y creditos.

`Aplanar(reporte)` convierte ese resultado al formato literal del enunciado, con las claves
que llevan espacios (`"Numero Cuenta"`, `"Saldo Inicial"`, `"Saldo Disponible"`), declaradas
con `JsonPropertyName`. No es la convencion del resto del API, que usa camelCase; se respeta
ahi unicamente para que el contrato coincida caracter a caracter con lo solicitado.

La fecha se formatea como `d/M/yyyy` con `CultureInfo.InvariantCulture`, que es lo que produce
`"10/2/2022"` del ejemplo.

**`RangoFechas`** admite las dos formas que un evaluador probaria de manera natural:
`2022-02-01,2022-02-28` y `01/02/2022-28/02/2022`. Tambien acepta los parametros separados
`fechaInicio` y `fechaFin`, que es lo que usa el frontend. Sin rango explicito toma el mes en
curso, para que la consulta no recorra todo el historico por descuido.

`FinUtc` llega hasta el ultimo instante del dia. Si el limite superior fuera medianoche, un
movimiento de las 15:00 del ultimo dia del rango quedaria fuera del reporte sin motivo
aparente.

### 4.5 Bloqueo pesimista en el saldo

`RepositorioCuenta.ObtenerParaMovimientoAsync` ejecuta `SELECT ... FOR UPDATE` antes de cargar
la cuenta.

Sin ese bloqueo, dos retiros simultaneos sobre la misma cuenta pueden leer el mismo saldo
disponible, comprobar los dos que hay fondos suficientes y dejar la cuenta en descubierto. Es
la condicion de carrera clasica en cualquier sistema de saldos y no se detecta con pruebas
secuenciales.

Solo se aplica en PostgreSQL. SQLite no soporta esa sintaxis y ademas serializa las escrituras
por si mismo, asi que en las pruebas la consulta se omite sin perder garantias. La comprobacion
es `_contexto.Database.IsNpgsql()`.

El SQL se envia con `ExecuteSqlInterpolatedAsync` y no con `ExecuteSqlRawAsync`: el primero
parametriza el valor interpolado, el segundo lo concatena y el analizador de EF lo marca como
riesgo de inyeccion.

---

## 5. Comunicacion entre microservicios

### 5.1 Contratos

`Devsu.Contracts` es el unico proyecto compartido. Contiene seis records de evento y la
interfaz `IEventoIntegracion`, que obliga a llevar `IdMensaje` y `OcurridoEn`.

| Evento | Publica | Consume | Efecto |
|---|---|---|---|
| `ClienteCreado` | Clientes | Cuentas | Crea la fila en `ClientesRef` |
| `ClienteActualizado` | Clientes | Cuentas | Refresca nombre y documento |
| `ClienteEstadoCambiado` | Clientes | Cuentas | Propaga el alta o la baja |
| `ClienteEliminado` | Clientes | Cuentas | Marca la referencia inactiva, no la borra |
| `CuentaAperturada` | Cuentas | Clientes | Incrementa `TotalCuentas` |
| `CuentaEstadoCambiado` | Cuentas | Clientes | Ajusta el contador de cuentas activas |

`ClienteEliminado` no borra la fila de `ClientesRef` a proposito: hay cuentas y movimientos
que la referencian y son informacion contable que debe seguir siendo atribuible a un titular.

### 5.2 Transactional Outbox

Publicar directamente contra RabbitMQ desde el handler tiene dos fallos clasicos:

- Si el broker esta caido, el evento se pierde aunque el cambio de negocio si se haya guardado.
- Si la transaccion termina en rollback, el evento ya salio y anuncia algo que nunca ocurrio.

Guardarlo en la misma transaccion elimina las dos ventanas.

`PublicadorEventosOutbox` implementa `IPublicadorEventos` serializando el evento a JSON y
anadiendo una fila a `OutboxMensajes` en el mismo `DbContext`.

`PublicadorOutboxHostedService` es un `BackgroundService` que cada cinco segundos:

1. Crea su propio scope de DI (no puede usar servicios con ambito de peticion).
2. Toma los pendientes en lotes de cincuenta.
3. Resuelve el tipo CLR desde el nombre guardado, contra el ensamblado `Devsu.Contracts`.
4. Publica con `IPublishEndpoint` y marca `FechaProcesado`.
5. Ante error, incrementa `Intentos` y guarda el mensaje en la columna `Error`.

Un tipo desconocido se marca como procesado con el error anotado: nunca se va a poder publicar
y dejarlo pendiente bloquearia la cola indefinidamente.

Un fallo en el ciclo no puede tumbar el host. El `catch` general registra y espera al siguiente
ciclo; el outbox se reintenta solo.

### 5.3 Inbox e idempotencia

RabbitMQ garantiza entrega "al menos una vez", asi que un reintento puede reentregar un evento
ya aplicado. Sin control, `CuentaAperturada` incrementaria el contador dos veces.

Cada consumidor traduce el evento a un comando de MediatR y el handler comprueba
`MensajesProcesados` antes de aplicar nada. Si el `IdMensaje` ya existe, descarta.

Los consumidores no tocan la base directamente. Esa indireccion permite probar la logica de
sincronizacion sin levantar un broker y la somete al mismo pipeline de validacion y trazas que
el resto de casos de uso.

### 5.4 Resiliencia del canal sincrono

El `HttpClient` hacia Clientes se registra con `AddStandardResilienceHandler()`, que aporta
reintentos con espera exponencial, cortacircuitos y limite de concurrencia.

El tiempo limite es de diez segundos, deliberadamente corto: esa llamada esta dentro de una
peticion HTTP que ya tiene a un usuario esperando. Es preferible fallar rapido con un 409
accionable que dejar la peticion colgada.

`ClientesApiClient` declara su propio record de respuesta en lugar de reutilizar el DTO del
otro servicio, precisamente para no acoplarlos: recoge solo los campos que Cuentas necesita y
sobrevive a que el otro anada o reordene propiedades.

---

## 6. Manejo de errores

Todos los errores salen como `application/problem+json` (RFC 7807) con dos campos extra:
`codigo` y `traceId`.

| Excepcion | HTTP | codigo |
|---|---|---|
| `RecursoNoEncontradoException` | 404 | `RECURSO_NO_ENCONTRADO` |
| `RecursoDuplicadoException` | 409 | `RECURSO_DUPLICADO` |
| `ConflictoConcurrenciaException` | 409 | `CONFLICTO_CONCURRENCIA` |
| `CuentaInactivaException` | 409 | `CUENTA_INACTIVA` |
| `ClienteNoSincronizadoException` | 409 | `CLIENTE_NO_SINCRONIZADO` |
| `ClienteInactivoException` | 400 | `CLIENTE_INACTIVO` |
| `SaldoNoDisponibleException` | 400 | `SALDO_NO_DISPONIBLE` |
| `ReglaNegocioException` | 400 | `REGLA_NEGOCIO` |
| `ValidationException` | 422 | `VALIDACION` + diccionario `errors` |
| Cualquier otra | 500 | `ERROR_INTERNO` |

`SaldoNoDisponibleException` recibe trato especial porque es la funcionalidad F3: el `title`
es exactamente `"Saldo no disponible"`, el texto que pide el enunciado, y la respuesta anade
`numeroCuenta`, `saldoDisponible` y `valorSolicitado` como campos estructurados para que el
frontend pueda mostrar el detalle sin parsear el mensaje.

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Saldo no disponible",
  "status": 400,
  "detail": "La cuenta 496825 no cuenta con saldo suficiente. Saldo disponible: 0.00, valor solicitado: -540.00.",
  "codigo": "SALDO_NO_DISPONIBLE",
  "numeroCuenta": "496825",
  "saldoDisponible": 0.00,
  "valorSolicitado": -540,
  "traceId": "00-33eb489510ba6e9b492532a2ef3258eb-358c421ec288bc64-00"
}
```

---

## 7. Pruebas

110 pruebas en total, todas verdes.

### 7.1 Unitarias del dominio (F5)

`ClienteTests` tiene 26 casos sobre la entidad `Cliente`. No hay dobles ni contexto de datos:
la entidad protege sus invariantes por si misma y eso es lo que se comprueba.

Un caso merece mencion:

```csharp
[Fact]
public void Crear_ConDatosValidos_ElClienteEsUnaPersona()
{
    var cliente = DatosDePrueba.Cliente();

    cliente.Should().BeAssignableTo<Persona>();
}
```

El enunciado exige herencia, no composicion. Si alguien cambiara `Cliente` por una clase con
una propiedad `Persona` dentro, todo seguiria compilando y funcionando, pero se habria dejado
de cumplir el requisito. Esta prueba lo detecta.

`CuentaTests` tiene 25 casos sobre las reglas de saldo, incluidos los cuatro movimientos del
caso de uso 4 del enunciado con sus saldos esperados.

### 7.2 Unitarias de aplicacion

Los handlers se prueban con repositorios sustituidos por NSubstitute. El doble de
`IUnitOfWork.EjecutarEnTransaccionAsync` se limita a ejecutar la operacion que recibe: lo que
se prueba es el contenido del caso de uso, no el comportamiento de EF.

### 7.3 Integracion (F6)

`WebApplicationFactory<Program>` levanta la API real y la apunta a SQLite en memoria. Se
recorre la pila completa: enrutado, serializacion, pipeline de MediatR, validadores, entidad
de dominio, EF Core y base relacional. Lo unico sustituido es el broker.

Se eligio SQLite y no el proveedor `InMemory` de EF porque SQLite es relacional de verdad:
respeta claves foraneas, restricciones unicas y transacciones, que es justo lo que estas
pruebas necesitan ejercitar. `InMemory` no valida un UNIQUE y la prueba de documento duplicado
pasaria sin comprobar nada.

La conexion se abre una sola vez y se mantiene abierta durante toda la clase: una base SQLite
en memoria vive mientras exista al menos una conexion abierta.

### 7.4 Un fallo que encontraron las pruebas manuales

Durante la verificacion contra el sistema real aparecio que `POST /api/movimientos` devolvia
`tipoMovimiento: null`. La causa: el DTO se construia desde la entidad recien creada, cuyas
navegaciones de catalogo no estan cargadas. Las pruebas de integracion no lo detectaron
porque no afirmaban sobre ese campo.

Se corrigio releyendo el movimiento tras guardar y se anadio la asercion que faltaba, para que
no vuelva a pasar desapercibido.

---

## 8. Frontend

React 18 con Vite y TypeScript en modo estricto. Cuatro pantallas: Clientes, Cuentas,
Movimientos y Reportes, mas el login.

| Archivo | Responsabilidad |
|---|---|
| `auth/ContextoAuth.tsx` | Sesion de Firebase. `onAuthStateChanged` hace de suscripcion y de carga inicial |
| `auth/RutaProtegida.tsx` | Redirige a login guardando el destino |
| `api/clienteHttp.ts` | `fetch` con token, y traduccion de `ProblemDetails` a `ErrorApi` |
| `api/servicios.ts` | Un objeto por recurso |
| `componentes/` | Layout, tabla, modal, alerta, paginador, formato |
| `paginas/` | Una por seccion |

Todas las llamadas salen contra rutas relativas `/api/...`. En desarrollo las reparte el proxy
de Vite y en produccion nginx, de modo que el codigo nunca sabe en que puerto vive cada
microservicio.

`ErrorApi` expone `mensajesPorCampo`, que aplana el diccionario `errors` del 422 para pintarlo
directamente bajo el formulario.

La interfaz no reformatea el texto que devuelve el API. Mostrar "Jose Lema" cuando en la base
dice "JOSE LEMA" confundiria a quien despues consulte los datos directamente.

El login traduce los codigos de Firebase a mensajes utiles, y responde lo mismo para
`user-not-found` y `wrong-password`: distinguirlos permitiria averiguar que correos estan
registrados.

Si las variables `VITE_FIREBASE_*` estan vacias, `firebaseConfigurado` es `false` y la pantalla
de login ofrece entrar en modo demostracion. Sin esa deteccion, Firebase lanzaria una excepcion
al inicializar y la pantalla quedaria en blanco.

---

## 9. Docker

Cinco servicios en `docker-compose.yml`: PostgreSQL, RabbitMQ, las dos APIs y el frontend.

**Dockerfiles de las APIs.** Multi-stage. Los `.csproj` se copian primero y por separado:
mientras no cambien las dependencias, Docker reutiliza la capa del `restore` y el build baja
de minutos a segundos. La imagen final es `aspnet:10.0`, corre con un usuario sin privilegios y
trae `curl` para el healthcheck.

El contexto de build es `./server` y no `./server/clientes`, porque el proyecto compartido
`shared/Devsu.Contracts` tiene que entrar en el contexto.

**`server/.dockerignore`.** Excluye `bin/` y `obj/`. Sin el, el `COPY` del codigo fuente
arrastra los artefactos de la maquina anfitriona y pisa el resultado del `restore` hecho dentro
de la imagen: el `project.assets.json` copiado apunta a rutas de Windows y el `publish` falla
con un error de NuGet que no dice nada del problema real. Es un fallo que se dio durante la
construccion de esta solucion.

**Dockerfile del frontend.** Compila con Node y sirve con nginx. Las variables `VITE_` entran
como `build args` porque Vite las sustituye en tiempo de compilacion, no de ejecucion.

**`nginx.conf`.** Sirve el bundle y reparte `/api/...` entre los dos microservicios. El
`try_files $uri $uri/ /index.html` es necesario porque React Router maneja el enrutado en el
cliente: sin el, recargar en `/clientes` daria 404.

**Orquestacion.** `depends_on` con `condition: service_healthy` para que las APIs no arranquen
antes que PostgreSQL y RabbitMQ. Los scripts de base de datos se montan en
`/docker-entrypoint-initdb.d` y la imagen los ejecuta una sola vez, cuando el volumen esta
vacio.

---

## 10. Rendimiento, escalabilidad y resiliencia

El enunciado pide contemplar estos factores, no necesariamente implementarlos. Lo que si esta
hecho y lo que quedaria por hacer:

### Implementado

| Factor | Medida |
|---|---|
| Rendimiento | `AsNoTracking` en todas las lecturas; paginacion obligatoria con tope de 100; totales de una pagina en una consulta; indices sobre documento, numero de cuenta y (cuenta, fecha) |
| Escalabilidad | Servicios sin estado, escalables horizontalmente; colas con nombre estable para que varias replicas compitan por los mensajes; bases separadas por servicio |
| Resiliencia | Transactional Outbox; consumo idempotente; reintentos de MassTransit; reintentos de EF ante fallos transitorios; cortacircuitos en el canal HTTP; health checks de liveness y readiness; espera activa de la base al arrancar |
| Correccion bajo concurrencia | `SELECT ... FOR UPDATE` sobre la cuenta al registrar un movimiento; secuencia de base de datos para el consecutivo de cliente |

### Pendiente

- **Cache de catalogos.** Se consultan en cada peticion. Son datos que cambian una vez al ano
  y deberian estar en memoria con invalidacion por evento.
- **Trazas distribuidas.** Se emite el `traceId` pero no hay un colector OpenTelemetry que
  permita seguir una peticion a traves de los dos servicios y el broker.
- **Cola de mensajes fallidos.** MassTransit reintenta, pero no hay un flujo definido para lo
  que cae en la dead letter queue.
- **Indice de saldos historicos.** El reporte recorre los movimientos del rango. Con millones
  de filas por cuenta convendria una tabla de saldos por corte diario.
- **Paginacion por cursor.** `OFFSET` degrada en paginas altas. Con volumenes grandes,
  paginacion basada en claves.

---

## 11. Deuda tecnica asumida

Decisiones conscientes que en un sistema real se resolverian de otra forma:

1. **`Devsu.Contracts` es un proyecto compartido.** Lo correcto seria publicarlo como paquete
   NuGet versionado para que cada servicio adopte la version nueva cuando le convenga. Como
   proyecto, un cambio de contrato obliga a recompilar los dos a la vez.

2. **Los catalogos estan duplicados en las dos bases con los mismos identificadores.** Es
   simple y funciona, pero mantener la sincronizacion es manual: el script tiene una
   comprobacion que aborta si el orden se rompe, y eso es una defensa, no una solucion.

3. **`SeedContrasenias` existe solo por los datos de prueba.** Un sistema real no tendria un
   componente que busca un literal magico en la columna de contrasenias.

4. **El modo demostracion del frontend salta la autenticacion.** Es deliberado para facilitar
   la revision, pero es codigo que nunca deberia llegar a produccion. Lo correcto seria
   eliminarlo en el build de produccion con una bandera de compilacion.

5. **Sin autorizacion por roles.** La autenticacion es binaria. Un sistema real distinguiria
   cajero, supervisor y auditor, y no todos podrian corregir un movimiento.

6. **El reverso de movimientos no esta implementado.** El catalogo contempla el estado
   `REVERSADO`, pero hoy un movimiento se corrige, no se reversa. Contablemente lo correcto es
   lo segundo: un asiento de correccion en lugar de modificar el original.

---

## 12. Verificacion realizada

La solucion se levanto completa con `docker compose up` y se verifico de extremo a extremo:

| Comprobacion | Resultado |
|---|---|
| 110 pruebas automatizadas | Todas en verde |
| Contenedores del compose | Los cinco healthy |
| Catalogos cargados con la convencion del modelo | 26 filas, ids 1 a 26 |
| Datos de prueba normalizados a mayusculas | JOSE LEMA, MARIANELA MONTALVO, JUAN OSORIO |
| Edad calculada por el trigger | 41, 35 y 48 anios |
| Caso de uso 4 completo | 478758 -> 1425, 225487 -> 700, 495878 -> 150, 496825 -> 0 |
| F3 sobre cuenta sin saldo | HTTP 400, title "Saldo no disponible" |
| F4 con el formato del enunciado | Las ocho claves exactas, incluidas las que llevan espacios |
| Alta de cliente replicada en Cuentas via RabbitMQ | ClientesRef actualizado |
| Evento de vuelta CuentaAperturada | Contador de cuentas actualizado en Clientes |
| Outbox tras la ejecucion | 0 pendientes, 0 con error |
| Proxy de nginx del frontend | Las tres rutas del API y el fallback de la SPA en 200 |
