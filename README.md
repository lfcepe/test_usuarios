# Devsu - Prueba tecnica de arquitectura de microservicios

Solucion a la prueba tecnica: dos microservicios en .NET 10 con PostgreSQL, comunicacion
asincrona mediante RabbitMQ, un cliente web en React y todo el conjunto desplegable con
un solo `docker compose up`.

- **Clientes** agrupa Persona y Cliente.
- **Cuentas** agrupa Cuenta y Movimiento.
- Los dos se comunican por eventos de integracion. No comparten base de datos.

El detalle de cada clase, metodo y decision esta en [DOCUMENTACION.md](DOCUMENTACION.md).
Este archivo se limita a lo necesario para levantar y revisar la solucion.

---

## 1. Puesta en marcha

### Requisitos

Docker Desktop con Docker Compose v2. Nada mas: el resto (SDK de .NET, Node, PostgreSQL)
vive dentro de las imagenes.

Para trabajar fuera de contenedores hacen falta .NET SDK 8.0 y Node 20 o superior.

### Levantar todo

```bash
cd devsu_test
docker compose up --build
```

La primera vez tarda varios minutos porque compila las tres imagenes. Cuando termine:

| Servicio | Direccion | Notas |
|---|---|---|
| Frontend | http://localhost:3000 | React servido por nginx |
| API Clientes | http://localhost:8081/swagger | Swagger UI |
| API Cuentas | http://localhost:8082/swagger | Swagger UI |
| RabbitMQ | http://localhost:15672 | usuario `devsu`, clave `devsu2024` |
| PostgreSQL | localhost:5432 | usuario `devsu`, clave `devsu2024` |

Comprobacion rapida de que todo respondio bien:

```bash
curl http://localhost:8081/health
curl http://localhost:8082/health
curl "http://localhost:8081/api/clientes?pagina=1&tamanio=5"
```

### Detener

```bash
docker compose down       # conserva los datos
docker compose down -v    # borra el volumen y fuerza a recrear la base al siguiente arranque
```

El segundo es el que hay que usar cuando se cambia `database/BaseDatos.sql`: la imagen de
PostgreSQL solo ejecuta los scripts de inicializacion si el volumen de datos esta vacio.

---

## 2. Estructura del repositorio

```
devsu_test/
  client/                     React 18 + Vite + TypeScript + Firebase Auth
  database/
    BaseDatos.sql             Entregable exigido: esquema completo de las dos bases
    datos-prueba.sql          Casos de uso 1, 2 y 3 del enunciado
  server/
    Devsu.sln
    Directory.Build.props     Propiedades comunes a todos los proyectos
    Directory.Packages.props  Versiones centralizadas de NuGet
    shared/Devsu.Contracts/   Contratos de los eventos de integracion
    clientes/
      src/                    Domain, Application, Infrastructure, Api
      tests/                  Unitarias e integracion
      Dockerfile
    cuentas/
      src/                    Domain, Application, Infrastructure, Api
      tests/                  Unitarias e integracion
      Dockerfile
  postman/
    Devsu.postman_collection.json
    Devsu.postman_environment.json
  docs/CONTRATO-TECNICO.md    Contrato interno seguido durante el desarrollo
  docker-compose.yml
  DOCUMENTACION.md            Documentacion tecnica detallada
```

Cada microservicio sigue Clean Architecture con cuatro proyectos y la regla de dependencia
apuntando siempre hacia el dominio:

```
Api  ->  Application  ->  Domain
 |            |
 +----> Infrastructure ---+
```

`Domain` no tiene ni una sola referencia de NuGet. Si alguna vez hiciera falta agregar un
paquete ahi, seria senal de que la logica esta en la capa equivocada.

---

## 3. Cumplimiento del enunciado

| Requisito | Donde se resuelve |
|---|---|
| F1 CRUD de Cliente | `ClientesController`, carpeta `Application/Clientes` |
| F1 CRU de Cuenta y Movimiento | `CuentasController`, `MovimientosController` |
| F2 Registro de movimientos y saldo | `Cuenta.RegistrarMovimiento` en el dominio de Cuentas |
| F3 "Saldo no disponible" | `SaldoNoDisponibleException` y `MiddlewareExcepciones` |
| F4 Reporte por fechas y cliente | `GET /api/reportes`, `GenerarReporteQueryHandler` |
| F5 Prueba unitaria de Cliente | `Devsu.Clientes.UnitTests/Dominio/ClienteTests.cs` |
| F6 Prueba de integracion | `ClientesEndpointsTests`, `CuentasEndpointsTests` |
| F7 Despliegue en contenedores | `docker-compose.yml` y los tres `Dockerfile` |
| Comunicacion asincrona | RabbitMQ + MassTransit, patron Outbox e Inbox |
| Script de base de datos | `database/BaseDatos.sql` |
| Coleccion de Postman | `postman/Devsu.postman_collection.json` |

---

## 4. Endpoints

Prefijo `/api` en los dos servicios. Todos los errores se devuelven como
`application/problem+json` con un campo adicional `codigo` para que el consumidor reaccione
al codigo y no al texto del mensaje.

### Microservicio de Clientes (puerto 8081)

| Verbo | Ruta | Descripcion |
|---|---|---|
| GET | `/api/clientes?pagina=1&tamanio=10&busqueda=&estado=` | Listado paginado |
| GET | `/api/clientes/{id}` | Detalle |
| GET | `/api/clientes/por-identificacion/{numeroDocumento}` | Busqueda por documento |
| POST | `/api/clientes` | Alta. 201 con cabecera `Location` |
| PUT | `/api/clientes/{id}` | Reemplazo completo |
| PATCH | `/api/clientes/{id}/estado` | Activa o desactiva |
| DELETE | `/api/clientes/{id}?definitivo=false` | Baja logica; con `true`, fisica |
| GET | `/api/personas?pagina=&tamanio=&busqueda=` | Consulta de personas |
| GET | `/api/personas/{id}` | Detalle de persona |
| GET | `/api/catalogos?raiz=TIPO_DOCUMENTO` | Items de un catalogo |
| GET | `/health` y `/health/ready` | Liveness y readiness |

### Microservicio de Cuentas (puerto 8082)

| Verbo | Ruta | Descripcion |
|---|---|---|
| GET | `/api/cuentas?clienteId=&estado=&pagina=&tamanio=` | Listado paginado |
| GET | `/api/cuentas/{id}` | Detalle |
| GET | `/api/cuentas/por-numero/{numeroCuenta}` | Busqueda por numero |
| POST | `/api/cuentas` | Apertura |
| PUT | `/api/cuentas/{id}` | Actualiza tipo y estado |
| PATCH | `/api/cuentas/{id}/estado` | Activa o desactiva |
| GET | `/api/movimientos?cuentaId=&numeroCuenta=&desde=&hasta=` | Listado paginado |
| GET | `/api/movimientos/{id}` | Detalle |
| POST | `/api/movimientos` | Registro (F2 y F3) |
| PUT | `/api/movimientos/{id}` | Correccion con recalculo de saldos |
| GET | `/api/reportes?fecha=2022-01-01,2022-02-28&cliente=1` | Reporte plano (F4) |
| GET | `/api/reportes/estado-cuenta?clienteId=&fechaInicio=&fechaFin=` | Reporte agrupado |
| GET | `/health` y `/health/ready` | Liveness y readiness |

### Ejemplos

Alta de cliente:

```bash
curl -X POST http://localhost:8081/api/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "primerNombre": "Ana",
    "primerApellido": "Paredes",
    "idTipoDocumento": 2,
    "numeroDocumento": "1714785236",
    "idGenero": 7,
    "direccionDomicilio": "Av. Republica y Pradera",
    "numeroCelular": "0991234567",
    "email": "ana.paredes@devsu.com",
    "fechaNacimiento": "1993-06-21",
    "contrasenia": "9012",
    "estado": true
  }'
```

Retiro que deja la cuenta en descubierto (F3):

```bash
curl -X POST http://localhost:8082/api/movimientos \
  -H "Content-Type: application/json" \
  -d '{"numeroCuenta": "495878", "valor": -150}'
```

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Saldo no disponible",
  "status": 400,
  "detail": "La cuenta 495878 no cuenta con saldo suficiente. Saldo disponible: 0.00, valor solicitado: -150.00.",
  "codigo": "SALDO_NO_DISPONIBLE",
  "numeroCuenta": "495878",
  "saldoDisponible": 0,
  "valorSolicitado": -150,
  "traceId": "00-..."
}
```

Reporte de estado de cuenta (F4), con el formato literal del enunciado:

```bash
curl "http://localhost:8082/api/reportes?fecha=2022-01-01,2030-12-31&cliente=2"
```

```json
[
  {
    "Fecha": "10/2/2022",
    "Cliente": "MARIANELA MONTALVO",
    "Numero Cuenta": "225487",
    "Tipo": "CORRIENTE",
    "Saldo Inicial": 100,
    "Estado": true,
    "Movimiento": 600,
    "Saldo Disponible": 700
  }
]
```

---

## 5. Postman

Importar los dos archivos de la carpeta `postman/`: la coleccion y el entorno "Devsu local".

La coleccion trae 49 peticiones repartidas en ocho carpetas y cada una lleva sus
aserciones. La carpeta **06 Casos de uso del enunciado** reproduce en orden los cinco casos
de uso del documento y esta pensada para ejecutarse entera con el Collection Runner sobre
una base recien levantada.

Si se activa la validacion de Firebase en el backend, hay que rellenar la variable `token`
del entorno con un JWT valido; la coleccion ya lo envia en la cabecera `Authorization`.

---

## 6. Pruebas

```bash
cd server
dotnet test Devsu.sln
```

Resultado esperado: 110 pruebas, todas en verde.

| Proyecto | Pruebas | Cubre |
|---|---|---|
| `Devsu.Clientes.UnitTests` | 52 | Entidad Cliente (F5), Persona, handlers y validadores |
| `Devsu.Clientes.IntegrationTests` | 7 | Pila HTTP completa sobre SQLite en memoria (F6) |
| `Devsu.Cuentas.UnitTests` | 40 | Reglas de saldo, recalculo en cadena, parser de fechas |
| `Devsu.Cuentas.IntegrationTests` | 11 | Apertura, movimientos, F3 y reporte sobre HTTP |

Las pruebas de integracion levantan la API real con `WebApplicationFactory` y la apuntan a
SQLite en memoria. Se eligio SQLite y no el proveedor `InMemory` de EF porque SQLite es
relacional de verdad: respeta claves foraneas, restricciones unicas y transacciones, que es
justo lo que hay que ejercitar. No hace falta Docker ni base de datos externa para
ejecutarlas.

---

## 7. Base de datos

Un unico servidor PostgreSQL 16 con **dos bases independientes**, una por microservicio:
`devsu_clientes` y `devsu_cuentas`. No hay consultas cruzadas entre ellas; lo que Cuentas
necesita de Clientes llega por eventos y se materializa en una tabla local `ClientesRef`.

El script `database/BaseDatos.sql` crea las dos bases, sus tablas, restricciones, indices,
el trigger de la edad y los catalogos. En Docker se ejecuta solo, montado en
`/docker-entrypoint-initdb.d`. A mano:

```bash
psql -h localhost -U devsu -d postgres -v ON_ERROR_STOP=1 -f database/BaseDatos.sql
psql -h localhost -U devsu -d postgres -v ON_ERROR_STOP=1 -f database/datos-prueba.sql
```

### Catalogos

`Catalogos` es una tabla auto referenciada. Las cabeceras llevan el nombre en
`DetalleCatalogo` con `IdRaiz` nulo, y sus items llevan el valor en `Item` apuntando a la
cabecera. La carga se hace con el procedimiento `sp_registrar_catalogo`, que es la version
en PostgreSQL del procedimiento de carga del modelo original.

```sql
CALL sp_registrar_catalogo('ESTADO_CLIENTE');
CALL sp_registrar_catalogo('ESTADO_CLIENTE', 'Activo');
CALL sp_registrar_catalogo('ESTADO_CLIENTE', 'Inactivo');
```

Los identificadores del 1 al 26 son fijos porque viajan dentro de los eventos de
integracion: las dos bases tienen que interpretar el mismo numero de la misma manera. La
clase `CatalogoIds` de cada microservicio es el espejo en codigo de esa lista, y el script
termina con una comprobacion que aborta si el orden se rompe.

### Normalizacion del texto

Todo el texto de negocio se almacena **en mayusculas y sin espacios sobrantes**, tanto si
entra por el API como si lo carga un script. La regla vive en `Normalizador` (capa de
dominio) y es la misma que aplica `sp_registrar_catalogo`. El objetivo es que "Jose Lema",
"JOSE LEMA" y "jose  lema" no convivan como si fueran tres personas distintas.

La unica excepcion es el hash de la contrasenia, que va en Base64 y quedaria inservible al
pasarlo a mayusculas. Para eso existe `Guardas.TextoSensible`.

### Calculo de la edad

Se resuelve en la base con tres objetos, porque son dos problemas distintos:

- `fn_calcular_edad(DATE)` es la funcion reutilizable.
- El trigger `tr_personas_edad` mantiene la columna al dia en cada escritura.
- El procedimiento `sp_actualizar_edades()` la recalcula en masa, porque la edad cambia con
  el paso del tiempo sin que la fila se modifique. Esta pensado para un job diario.

Una columna `GENERATED ALWAYS AS` no sirve: PostgreSQL exige una expresion inmutable y
`CURRENT_DATE` no lo es. EF Core mapea `Edad` como generada por el almacen, de modo que la
aplicacion la lee pero nunca la escribe.

### Cambios respecto al modelo original

El modelo de partida estaba pensado para SQL Server y en un solo esquema. Estos son los
cambios y el motivo de cada uno:

| Original | Final | Motivo |
|---|---|---|
| `Contrasenia VARCHAR(15) UNIQUE NOT NULL` | `VARCHAR(256) NOT NULL` con hash PBKDF2 | Un UNIQUE sobre la contrasenia impide que dos clientes usen la misma clave y permite deducir por descarte si una clave existe. Guardar la clave en claro es peor todavia. |
| `SegundoNombre` y `SegundoApellido` NOT NULL | NULL | Los datos del propio enunciado ("Jose Lema") no tienen segundo nombre ni apellido. |
| `FechaCreación` con tilde | `FechaCreacion` | Los identificadores acentuados obligan a entrecomillar siempre y dan problemas de codificacion entre drivers. |
| `NumeroCuenta UNIQUEIDENTIFIER` | `VARCHAR(20) UNIQUE` | `UNIQUEIDENTIFIER` es de SQL Server, y el enunciado usa numeros de cuenta como `478758`. |
| `Cliente.Id SERIAL` + `IdPersona` FK | `Cliente.Id INT` que **es** la FK a `Personas.Id` | El enunciado pide que Cliente herede de Persona. La forma canonica de mapear herencia en EF Core relacional es Table-Per-Type, donde la tabla hija comparte la clave con la base. Se conserva el nombre de la restriccion `fk_IdPersona_Personas`. La clave unica de cliente queda como columna de negocio `ClienteId` (`CLI-000001`). |
| `FechaCreacion TIMESTAMP DEFAULT` (truncado) | `NOT NULL DEFAULT CURRENT_TIMESTAMP` | El original quedo incompleto. |
| Una sola base | `devsu_clientes` y `devsu_cuentas` | Cada microservicio es dueno de sus datos. Compartir base seria un monolito con dos procesos. |
| - | `SaldoDisponible` en `CuentasPersona` | El enunciado pide actualizar el saldo disponible al registrar un movimiento. Mutar `SaldoInicial` destruiria la trazabilidad contable. |
| - | `OutboxMensajes` y `MensajesProcesados` | Publicacion fiable de eventos y consumo idempotente. |

Todos los nombres de tabla y columna van **entrecomillados** en el script. PostgreSQL pasa
a minusculas los identificadores sin comillas y EF Core con Npgsql si los entrecomilla: sin
las comillas el script crearia `personas` y la aplicacion buscaria `"Personas"` sin
encontrarla.

---

## 8. Comunicacion entre microservicios

Broker RabbitMQ 3.13, transporte MassTransit 8.

```
POST /api/clientes
  -> CrearClienteCommandHandler
     -> repositorio.Agregar(cliente)
     -> outbox.Encolar(ClienteCreado)        misma transaccion
     -> unitOfWork.GuardarCambios()
  -> PublicadorOutboxHostedService (cada 5 s)
     -> IPublishEndpoint.Publish(ClienteCreado)
        -> RabbitMQ
           -> ConsumidorClienteCreado (ms Cuentas)
              -> upsert en "ClientesRef", con control de idempotencia
```

Eventos publicados por **Clientes**: `ClienteCreado`, `ClienteActualizado`,
`ClienteEstadoCambiado`, `ClienteEliminado`.
Eventos publicados por **Cuentas**: `CuentaAperturada`, `CuentaEstadoCambiado`, que Clientes
consume para mantener el contador de cuentas activas.

Tres piezas sostienen la fiabilidad del canal:

- **Transactional Outbox**: el evento se guarda en la misma transaccion que el cambio de
  negocio. Si RabbitMQ esta caido, el evento espera en la tabla y se publica despues. Sin
  esto se perderia en silencio y las bases quedarian desincronizadas para siempre.
- **Inbox**: cada evento lleva un `IdMensaje` que el consumidor registra. RabbitMQ garantiza
  entrega "al menos una vez", asi que un reintento puede reentregar un evento ya aplicado.
- **Respaldo sincrono**: si Cuentas recibe una peticion para un cliente que todavia no esta
  replicado, consulta `GET /api/clientes/{id}` con reintentos y cortacircuitos. Si tampoco
  responde, devuelve 409 `CLIENTE_NO_SINCRONIZADO`. Nunca se inventa el dato del titular.

---

## 9. Autenticacion con Firebase

El frontend usa Firebase Authentication (correo y contrasenia, o Google) y envia el token en
la cabecera `Authorization: Bearer`. El backend puede validar ese token contra el emisor de
Google, pero viene **desactivado por defecto** para que la prueba se pueda recorrer con
Postman sin montar un proyecto de Firebase.

Para activarlo:

```bash
# en .env, en la raiz del proyecto
FIREBASE_HABILITADO=true
FIREBASE_PROJECT_ID=mi-proyecto-firebase
```

Configuracion del cliente:

1. Crear un proyecto en https://console.firebase.google.com.
2. Activar Authentication y los proveedores "Correo electronico/contrasenia" y "Google".
3. Registrar una app web y copiar las credenciales.
4. `cp client/.env.example client/.env` y rellenar las variables `VITE_FIREBASE_*`.

Si esas variables estan vacias, la pantalla de login lo detecta y ofrece entrar en **modo
demostracion**: se navega por toda la aplicacion sin token. Es deliberado, para que la
revision no dependa de tener credenciales de Firebase a mano.

Vite sustituye las variables `VITE_` en tiempo de compilacion, no de ejecucion. Por eso en
`docker-compose.yml` entran como `build args` y no como variables de entorno del contenedor.

---

## 10. Desarrollo fuera de Docker

Hace falta PostgreSQL y RabbitMQ accesibles. Lo comodo es levantar solo esos dos:

```bash
docker compose up devsu-postgres devsu-rabbitmq
```

Y despues, en tres terminales:

```bash
cd server/clientes/src/Devsu.Clientes.Api && dotnet run    # http://localhost:8081
cd server/cuentas/src/Devsu.Cuentas.Api   && dotnet run    # http://localhost:8082
cd client && npm install && npm run dev                    # http://localhost:3000
```

En desarrollo el proxy de Vite reparte las llamadas entre los dos microservicios; en
produccion ese papel lo hace nginx. El codigo del frontend siempre llama a rutas relativas
`/api/...` y no sabe en que puerto vive cada servicio.

---

## 11. Problemas frecuentes

**Los cambios en `BaseDatos.sql` no se aplican.** La imagen de PostgreSQL solo ejecuta los
scripts de inicializacion cuando el volumen esta vacio. `docker compose down -v` y volver a
levantar.

**Las APIs arrancan antes que PostgreSQL.** Estan preparadas: `ArranqueBaseDatos` reintenta
la conexion quince veces con tres segundos de espera, y el `depends_on` del compose usa
`condition: service_healthy`. Si aun asi falla, el log dice exactamente que revisar.

**Un cliente creado no aparece en Cuentas.** La replicacion es asincrona y el publicador del
outbox corre cada cinco segundos. Se puede seguir el rastro en
http://localhost:15672 o consultando la tabla `OutboxMensajes` de `devsu_clientes`: si hay
filas con `FechaProcesado` nulo e `Intentos` creciendo, la columna `Error` dice por que.

**El puerto 5432 esta ocupado** por otra instalacion de PostgreSQL. Cambiar
`POSTGRES_PORT` en el archivo `.env`.

---

## 12. Lo que queda fuera

Consciente y documentado, porque excede el alcance de la prueba:

- **Autorizacion por roles.** Hoy la autenticacion es binaria: se pasa o no se pasa. Un
  sistema real distinguiria cajero, supervisor y auditor.
- **Versionado del API.** Los contratos estan fijos en `/api/...`. Con consumidores externos
  haria falta `/api/v1/...` y una politica de deprecacion.
- **Trazas distribuidas.** Se emite el `traceId` en cada error, pero no hay un colector tipo
  OpenTelemetry que permita seguir una peticion a traves de los dos servicios y el broker.
- **Cache de lecturas.** Los catalogos se consultan en cada peticion. Son datos que cambian
  una vez al ano y deberian estar en memoria con invalidacion por evento.
- **Reverso de movimientos.** El catalogo ya contempla el estado `REVERSADO`, pero el caso de
  uso no esta implementado: hoy un movimiento se corrige, no se reversa.
