# Contrato técnico interno

Este documento es la fuente de verdad del proyecto. Cualquier persona (o proceso) que
escriba código en este repositorio debe respetarlo al pie de la letra: nombres de tablas,
columnas, rutas, DTOs, eventos y convenciones. Si algo no está aquí, se decide con criterio
y se documenta después en `DOCUMENTACION.md`.

No es el README. El README se escribe al final y va dirigido al evaluador.

---

## 1. Estructura de carpetas

```
devsu_test/
  client/                         React 18 + Vite + TypeScript + Firebase Auth
  database/
    BaseDatos.sql                 Script único de creación (entregable exigido)
    datos-prueba.sql              Casos de uso del enunciado (Jose Lema, etc.)
  server/
    Devsu.sln
    Directory.Build.props
    Directory.Packages.props      Central Package Management
    shared/
      Devsu.Contracts/            Contratos de eventos de integración (único proyecto compartido)
    clientes/
      Dockerfile
      src/Devsu.Clientes.Domain/
      src/Devsu.Clientes.Application/
      src/Devsu.Clientes.Infrastructure/
      src/Devsu.Clientes.Api/
      tests/Devsu.Clientes.UnitTests/
      tests/Devsu.Clientes.IntegrationTests/
    cuentas/
      Dockerfile
      src/Devsu.Cuentas.Domain/
      src/Devsu.Cuentas.Application/
      src/Devsu.Cuentas.Infrastructure/
      src/Devsu.Cuentas.Api/
      tests/Devsu.Cuentas.UnitTests/
      tests/Devsu.Cuentas.IntegrationTests/
  postman/
    Devsu.postman_collection.json
    Devsu.postman_environment.json
  docs/
    CONTRATO-TECNICO.md           (este archivo)
  docker-compose.yml
  .env.example
  README.md
  DOCUMENTACION.md
```

El contexto de build de ambos Dockerfile es `./server`, de modo que el proyecto compartido
`shared/Devsu.Contracts` entre en el contexto. En `docker-compose.yml` eso se expresa como
`context: ./server` + `dockerfile: clientes/Dockerfile`.

---

## 2. Convenciones transversales

- **.NET 10** (`net10.0`), C# 14, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=false`.
  La version del SDK queda fijada en `global.json` con `rollForward: latestFeature`.
- Idioma del código: **inglés para palabras clave del framework, español para el dominio**.
  Las entidades, propiedades y rutas van en español porque el dominio y el enunciado están
  en español (`Cliente`, `Movimiento`, `SaldoDisponible`). Los sufijos técnicos van en inglés
  (`Repository`, `Handler`, `Dto`, `Request`).
- Sin emojis en código, comentarios, commits o documentación.
- Comentarios: solo donde el *por qué* no sea evidente. Nada de comentarios que repitan el
  nombre del método. Los XML-doc (`///`) se usan en interfaces públicas de Application y Domain.
- Nada de `AutoMapper` ni `MediatR` v13+: el mapeo es manual mediante métodos de extensión
  (`ClienteMappings.ToDto()`), y MediatR queda fijado en 12.4.1 (Apache-2.0).
- Excepciones de dominio heredan de una base propia por servicio, nunca de `Exception` directo.
- Todas las fechas que se persisten son UTC (`DateTime.UtcNow`). La conversión a hora local
  es responsabilidad del cliente.

### Identificadores en PostgreSQL

PostgreSQL pasa a minúsculas todo identificador sin comillas. EF Core con Npgsql **sí** entrecomilla.
Por lo tanto **todas las tablas y columnas del script se declaran entrecomilladas** (`"Personas"`,
`"PrimerNombre"`) para que coincidan con el modelo de EF. Esto no es opcional: sin comillas el
script crea `personas` y EF busca `"Personas"`, y falla en tiempo de ejecución.

Los nombres de restricciones (`fk_...`, `uq_...`) van en minúsculas y sin comillas, siguiendo la
convención del diseño original.

---

## 3. Base de datos

Una instancia de PostgreSQL 16, **dos bases de datos independientes** (una por microservicio):
`devsu_clientes` y `devsu_cuentas`. No hay JOINs entre microservicios; la información que
Cuentas necesita de Clientes llega por eventos y se materializa en una tabla local de solo
lectura (`ClientesRef`).

Usuario: `devsu` / contraseña: `devsu2024` (parametrizable por variables de entorno).

### 3.1 Desviaciones respecto al diseño original (documentar en el README)

| Original | Final | Motivo |
|---|---|---|
| `Contrasenia VARCHAR(15) UNIQUE NOT NULL` | `Contrasenia VARCHAR(256) NOT NULL` | Un UNIQUE sobre la contraseña impide que dos clientes usen la misma clave y permite deducir por descarte si una clave existe. Además se almacena hash PBKDF2, no texto plano. |
| `SegundoNombre`, `SegundoApellido` NOT NULL | NULL | Los datos del enunciado ("Jose Lema") no tienen segundo nombre ni segundo apellido. |
| `FechaCreación` (con tilde) | `FechaCreacion` | Los identificadores acentuados obligan a entrecomillar siempre y dan problemas de codificación entre drivers. |
| `NumeroCuenta UNIQUEIDENTIFIER` | `NumeroCuenta VARCHAR(20) UNIQUE` | `UNIQUEIDENTIFIER` es de SQL Server. Además el enunciado usa números de cuenta como `478758`. |
| `Cliente.Id SERIAL PK` + `IdPersona FK` | `Cliente.Id INT PK` que **es** FK a `Personas.Id` | El enunciado exige que `Cliente` herede de `Persona`. La forma canónica de mapear herencia en EF Core relacional es Table-Per-Type, donde la tabla hija comparte la clave con la tabla base. Se conserva el nombre de la restricción `fk_IdPersona_Personas`. La "clave única de cliente" del enunciado se conserva como columna de negocio `ClienteId`. |
| `FechaCreacion TIMESTAMP DEFAULT` (incompleto) | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | El original quedó truncado. |
| — | `SaldoDisponible` en `CuentasPersona` | El enunciado pide "actualizar el saldo disponible" al registrar un movimiento; `SaldoInicial` no puede mutar o se pierde la trazabilidad. |

### 3.2 Esquema de `devsu_clientes`

```sql
CREATE TABLE "Catalogos"(
  "Id" SERIAL PRIMARY KEY,
  "DetalleCatalogo" VARCHAR(128) NULL,
  "Item" VARCHAR(128) NULL,
  "IdRaiz" INT NULL,
  CONSTRAINT fk_IdRaiz_Catalogos FOREIGN KEY ("IdRaiz") REFERENCES "Catalogos"("Id")
);

CREATE TABLE "Personas"(
  "Id" SERIAL PRIMARY KEY,
  "PrimerNombre" VARCHAR(64) NOT NULL,
  "SegundoNombre" VARCHAR(64) NULL,
  "PrimerApellido" VARCHAR(64) NOT NULL,
  "SegundoApellido" VARCHAR(64) NULL,
  "IdTipoDocumento" INT NOT NULL,
  "NumeroDocumento" VARCHAR(20) NOT NULL,
  "IdGenero" INT NOT NULL,
  "DireccionDomicilio" VARCHAR(1000) NOT NULL,
  "NumeroCelular" VARCHAR(10) NOT NULL,
  "Email" VARCHAR(500) NOT NULL,
  "FechaNacimiento" DATE NOT NULL,
  "Edad" INT NULL,
  "FechaCreacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "FechaModificacion" TIMESTAMP NULL,
  "IdEstadoPersona" INT NOT NULL,
  CONSTRAINT uq_Personas_Documento UNIQUE ("IdTipoDocumento", "NumeroDocumento"),
  CONSTRAINT fk_IdTipoDocumento_Catalogos FOREIGN KEY ("IdTipoDocumento") REFERENCES "Catalogos"("Id"),
  CONSTRAINT fk_IdGenero_Catalogos FOREIGN KEY ("IdGenero") REFERENCES "Catalogos"("Id"),
  CONSTRAINT fk_IdEstadoPersona_Catalogos FOREIGN KEY ("IdEstadoPersona") REFERENCES "Catalogos"("Id")
);

CREATE TABLE "Cliente"(
  "Id" INT PRIMARY KEY,                      -- comparte clave con "Personas" (herencia TPT)
  "ClienteId" VARCHAR(20) NOT NULL,          -- clave única de negocio: CLI-000001
  "Contrasenia" VARCHAR(256) NOT NULL,       -- hash PBKDF2 en formato iteraciones.salt.hash
  "FechaCreacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "FechaModificacion" TIMESTAMP NULL,
  "IdEstadoCliente" INT NOT NULL,
  CONSTRAINT uq_Cliente_ClienteId UNIQUE ("ClienteId"),
  CONSTRAINT fk_IdPersona_Personas FOREIGN KEY ("Id") REFERENCES "Personas"("Id") ON DELETE CASCADE,
  CONSTRAINT fk_IdEstadoCliente_Catalogos FOREIGN KEY ("IdEstadoCliente") REFERENCES "Catalogos"("Id")
);

-- Read model alimentado por eventos publicados por el microservicio de Cuentas.
CREATE TABLE "ResumenCuentasCliente"(
  "IdCliente" INT PRIMARY KEY,
  "TotalCuentas" INT NOT NULL DEFAULT 0,
  "FechaActualizacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_IdCliente_Cliente FOREIGN KEY ("IdCliente") REFERENCES "Cliente"("Id") ON DELETE CASCADE
);

-- Patrón Transactional Outbox: los eventos se guardan en la misma transacción que el cambio
-- de negocio y un HostedService los publica después. Evita perder eventos si RabbitMQ está caído.
CREATE TABLE "OutboxMensajes"(
  "Id" UUID PRIMARY KEY,
  "TipoMensaje" VARCHAR(256) NOT NULL,
  "Contenido" TEXT NOT NULL,
  "FechaCreacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "FechaProcesado" TIMESTAMP NULL,
  "Intentos" INT NOT NULL DEFAULT 0,
  "Error" TEXT NULL
);
CREATE INDEX ix_OutboxMensajes_Pendientes ON "OutboxMensajes"("FechaCreacion") WHERE "FechaProcesado" IS NULL;

-- Patrón Inbox: idempotencia en el consumo. Un evento reentregado no se aplica dos veces.
CREATE TABLE "MensajesProcesados"(
  "IdMensaje" UUID PRIMARY KEY,
  "TipoMensaje" VARCHAR(256) NOT NULL,
  "FechaProcesado" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

Índices adicionales: `ix_Personas_NumeroDocumento`, `ix_Cliente_IdEstadoCliente`.

### 3.3 Cálculo de la edad (trigger + función + procedimiento)

Tres piezas, cada una con su razón de ser:

1. `fn_calcular_edad(p_fecha DATE) RETURNS INT` — función pura reutilizable.
   `EXTRACT(YEAR FROM AGE(CURRENT_DATE, p_fecha))::INT`.
2. `trg_personas_calcular_edad()` + trigger `tr_personas_edad`
   `BEFORE INSERT OR UPDATE ON "Personas" FOR EACH ROW`: mantiene `Edad` coherente en cada
   escritura. Se valida además que `FechaNacimiento` no sea futura (`RAISE EXCEPTION`).
3. `sp_actualizar_edades()` — procedimiento almacenado que recalcula todas las edades.
   Necesario porque la edad cambia con el paso del tiempo sin que la fila se toque; se
   ejecutaría desde un job nocturno. Devuelve el número de filas afectadas por `RAISE NOTICE`.

En EF Core, `Persona.Edad` se mapea con `.ValueGeneratedOnAddOrUpdate()` para que Npgsql la
recupere con `RETURNING` y nunca la escriba. La entidad la expone como propiedad de solo lectura
desde el punto de vista de Application.

### 3.4 Esquema de `devsu_cuentas`

```sql
CREATE TABLE "Catalogos"( ... idéntico a devsu_clientes, mismos Id ... );

-- Réplica local de solo lectura. NO es la fuente de verdad: la fuente es el ms de Clientes.
CREATE TABLE "ClientesRef"(
  "IdCliente" INT PRIMARY KEY,
  "ClienteId" VARCHAR(20) NOT NULL,
  "NombreCompleto" VARCHAR(256) NOT NULL,
  "NumeroDocumento" VARCHAR(20) NOT NULL,
  "IdEstadoCliente" INT NOT NULL,
  "Activo" BOOLEAN NOT NULL DEFAULT TRUE,
  "FechaSincronizacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "CuentasPersona"(
  "Id" SERIAL PRIMARY KEY,
  "IdCliente" INT NOT NULL,
  "NumeroCuenta" VARCHAR(20) NOT NULL,
  "IdTipoCuenta" INT NOT NULL,
  "SaldoInicial" DECIMAL(18,2) NOT NULL,
  "SaldoDisponible" DECIMAL(18,2) NOT NULL,
  "FechaCreacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "FechaModificacion" TIMESTAMP NULL,
  "IdEstadoCuenta" INT NOT NULL,
  CONSTRAINT uq_CuentasPersona_NumeroCuenta UNIQUE ("NumeroCuenta"),
  CONSTRAINT fk_IdCliente_CuentasPersona FOREIGN KEY ("IdCliente") REFERENCES "ClientesRef"("IdCliente"),
  CONSTRAINT fk_IdTipoCuenta_Catalogos FOREIGN KEY ("IdTipoCuenta") REFERENCES "Catalogos"("Id"),
  CONSTRAINT fk_IdEstadoCuenta_Catalogos FOREIGN KEY ("IdEstadoCuenta") REFERENCES "Catalogos"("Id")
);

CREATE TABLE "Movimientos"(
  "Id" SERIAL PRIMARY KEY,
  "IdCuentaPersona" INT NOT NULL,
  "Fecha" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "IdTipoMovimiento" INT NOT NULL,
  "Valor" DECIMAL(18,2) NOT NULL,
  "Saldo" DECIMAL(18,2) NOT NULL,             -- saldo resultante DESPUÉS del movimiento
  "Descripcion" VARCHAR(256) NULL,
  "FechaCreacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "FechaModificacion" TIMESTAMP NULL,
  "IdEstadoMovimiento" INT NOT NULL,
  CONSTRAINT fk_IdCuentaPersona_Movimientos FOREIGN KEY ("IdCuentaPersona") REFERENCES "CuentasPersona"("Id") ON DELETE CASCADE,
  CONSTRAINT fk_IdTipoMovimiento_Catalogos FOREIGN KEY ("IdTipoMovimiento") REFERENCES "Catalogos"("Id"),
  CONSTRAINT fk_IdEstadoMovimiento_Catalogos FOREIGN KEY ("IdEstadoMovimiento") REFERENCES "Catalogos"("Id")
);
CREATE INDEX ix_Movimientos_Cuenta_Fecha ON "Movimientos"("IdCuentaPersona", "Fecha" DESC);

CREATE TABLE "OutboxMensajes"( ... idéntico ... );
CREATE TABLE "MensajesProcesados"( ... idéntico ... );
```

### 3.5 Catálogos (idénticos en ambas bases, mismos Id)

Los Id son fijos y se insertan explícitamente, porque viajan dentro de los eventos y las dos
bases deben interpretarlos igual. Al final se reposiciona la secuencia con `setval`.

| Id | DetalleCatalogo | Item | IdRaiz |
|----|-----------------|------|--------|
| 1  | RAIZ | TIPO_DOCUMENTO | NULL |
| 2  | TIPO_DOCUMENTO | Cedula | 1 |
| 3  | TIPO_DOCUMENTO | Pasaporte | 1 |
| 4  | TIPO_DOCUMENTO | RUC | 1 |
| 5  | RAIZ | GENERO | NULL |
| 6  | GENERO | Masculino | 5 |
| 7  | GENERO | Femenino | 5 |
| 8  | GENERO | Otro | 5 |
| 9  | RAIZ | ESTADO_PERSONA | NULL |
| 10 | ESTADO_PERSONA | Activo | 9 |
| 11 | ESTADO_PERSONA | Inactivo | 9 |
| 12 | RAIZ | ESTADO_CLIENTE | NULL |
| 13 | ESTADO_CLIENTE | Activo | 12 |
| 14 | ESTADO_CLIENTE | Inactivo | 12 |
| 15 | RAIZ | TIPO_CUENTA | NULL |
| 16 | TIPO_CUENTA | Ahorros | 15 |
| 17 | TIPO_CUENTA | Corriente | 15 |
| 18 | RAIZ | ESTADO_CUENTA | NULL |
| 19 | ESTADO_CUENTA | Activa | 18 |
| 20 | ESTADO_CUENTA | Inactiva | 18 |
| 21 | RAIZ | TIPO_MOVIMIENTO | NULL |
| 22 | TIPO_MOVIMIENTO | Deposito | 21 |
| 23 | TIPO_MOVIMIENTO | Retiro | 21 |
| 24 | RAIZ | ESTADO_MOVIMIENTO | NULL |
| 25 | ESTADO_MOVIMIENTO | Aplicado | 24 |
| 26 | ESTADO_MOVIMIENTO | Reversado | 24 |

Constantes espejo en C#: `Devsu.<Servicio>.Domain.Catalogos.CatalogoIds`.

### 3.6 Datos de prueba (`datos-prueba.sql`)

Los del enunciado, con los campos que faltan completados de forma verosímil:

Personas/Clientes (estado true en todos):

| Nombre | Direccion | Telefono | Contrasenia |
|---|---|---|---|
| Jose Lema | Otavalo sn y principal | 098254785 | 1234 |
| Marianela Montalvo | Amazonas y NNUU | 097548965 | 5678 |
| Juan Osorio | 13 junio y Equinoccial | 098874587 | 1245 |

Cuentas: 478758 Ahorros 2000 (Jose Lema), 225487 Corriente 100 (Marianela),
495878 Ahorros 0 (Juan Osorio), 496825 Ahorros 540 (Marianela), 585545 Corriente 1000 (Jose Lema).

Los movimientos del punto 4 del enunciado **no** se insertan por SQL: se ejecutan desde la
colección de Postman contra `POST /api/movimientos`, que es lo que demuestra F2/F3.
El script sí deja las cuentas en su saldo inicial para que la secuencia sea reproducible.

---

## 4. Arquitectura de cada microservicio

Cuatro proyectos, dependencias hacia adentro (regla de dependencia de Clean Architecture):

```
Api  ->  Application  ->  Domain
 |            |
 +----> Infrastructure ---+
```

- **Domain**: entidades, objetos de valor, excepciones de dominio, interfaces de repositorio,
  constantes de catálogo. Sin dependencias de NuGet salvo ninguna. Ni EF, ni MediatR.
- **Application**: casos de uso (MediatR `IRequest` + `IRequestHandler`), DTOs, validadores
  FluentValidation, interfaces de servicios de infraestructura (`IUnitOfWork`, `IPublicadorEventos`,
  `IServicioHashContrasenia`), behaviors del pipeline. Depende solo de Domain.
- **Infrastructure**: `DbContext`, `IEntityTypeConfiguration<>`, implementación de repositorios,
  `UnitOfWork`, publicador de eventos (outbox + MassTransit), consumidores, cliente HTTP con
  resiliencia. Depende de Application y Domain.
- **Api**: controladores, middleware de excepciones, configuración de DI, Swagger, health checks,
  autenticación Firebase opcional. Depende de Application e Infrastructure.

### 4.1 Patrones exigidos/aplicados

| Patrón | Dónde |
|---|---|
| Repository | `IRepositorioCliente`, `IRepositorioCuenta`, `IRepositorioMovimiento` (Domain) → `Infrastructure/Persistencia/Repositorios` |
| Unit of Work | `IUnitOfWork.GuardarCambiosAsync()`; los handlers nunca llaman a `SaveChanges` del contexto |
| CQRS ligero | Commands y Queries separados con MediatR |
| Mediator | MediatR 12.4.1 |
| Pipeline / Decorator | `ValidacionBehavior`, `RegistroBehavior` (logging), `TransaccionBehavior` |
| Specification (ligero) | filtros de consulta encapsulados en `Especificaciones/` |
| Transactional Outbox | `OutboxMensajes` + `PublicadorOutboxHostedService` |
| Inbox / Idempotent consumer | `MensajesProcesados` + `ConsumidorIdempotente<T>` |
| Circuit breaker + retry | `Microsoft.Extensions.Http.Resilience` sobre el cliente HTTP de respaldo |
| Result / excepciones tipadas | jerarquía `ExcepcionDominio` + middleware a ProblemDetails |

### 4.2 Paquetes (Central Package Management, `Directory.Packages.props`)

```
Microsoft.EntityFrameworkCore                      10.0.12
Microsoft.EntityFrameworkCore.Design               10.0.12
Microsoft.EntityFrameworkCore.Relational           10.0.12
Microsoft.EntityFrameworkCore.Sqlite               10.0.12  (solo pruebas de integracion)
Npgsql.EntityFrameworkCore.PostgreSQL              10.0.3
MediatR                                            12.4.1   (ultima Apache-2.0)
FluentValidation                                   11.12.0
FluentValidation.DependencyInjectionExtensions     11.12.0
MassTransit                                        8.5.10
MassTransit.RabbitMQ                               8.5.10
Serilog.AspNetCore                                 9.0.0
Serilog.Sinks.Console                              6.1.1
Swashbuckle.AspNetCore                             9.0.6
Microsoft.AspNetCore.Authentication.JwtBearer      10.0.12
Microsoft.Extensions.Http.Resilience               9.10.0
Microsoft.AspNetCore.Mvc.Testing                   10.0.12
Microsoft.NET.Test.Sdk                             17.14.1
xunit                                              2.9.3
xunit.runner.visualstudio                          2.8.2
FluentAssertions                                   6.12.2   (la rama 8 dejo de ser libre)
NSubstitute                                        5.3.0
coverlet.collector                                 6.0.4
```

No usar versiones distintas sin actualizar este documento.

---

## 5. Comunicación asíncrona

Broker: **RabbitMQ 3.13** (imagen `rabbitmq:3.13-management`), transporte **MassTransit 8**.

Flujo principal (Clientes → Cuentas):

```
POST /api/clientes
  -> CrearClienteCommandHandler
     -> repositorio.Agregar(cliente)
     -> outbox.Encolar(ClienteCreado)         MISMA transacción
     -> unitOfWork.GuardarCambiosAsync()
  -> PublicadorOutboxHostedService (cada 5 s)
     -> IPublishEndpoint.Publish(ClienteCreado)
        -> RabbitMQ exchange Devsu.Contracts.Eventos:ClienteCreado
           -> ConsumidorClienteCreado (ms Cuentas)
              -> upsert en "ClientesRef"    (idempotente vía "MensajesProcesados")
```

Flujo inverso (Cuentas → Clientes): `CuentaAperturada` / `CuentaEstadoCambiado` actualizan
`ResumenCuentasCliente` en la base de Clientes. Sirve para exponer `totalCuentas` en el DTO de
cliente sin acoplar los servicios de forma síncrona.

Respaldo síncrono: si Cuentas recibe una petición para un cliente que todavía no está en
`ClientesRef` (arranque en frío, evento en vuelo), consulta `GET /api/clientes/{id}` del ms de
Clientes a través de `IClientesApiClient`, protegido con retry exponencial + circuit breaker.
Si eso también falla, responde 409 con `CLIENTE_NO_SINCRONIZADO`. Nunca se inventa el dato.

### 5.1 Contratos de eventos (`Devsu.Contracts`)

Namespace `Devsu.Contracts.Eventos`. Todos los records implementan `IEventoIntegracion`:

```csharp
namespace Devsu.Contracts.Eventos;

public interface IEventoIntegracion
{
    Guid IdMensaje { get; }
    DateTime OcurridoEn { get; }
}

public sealed record ClienteCreado(
    Guid IdMensaje, DateTime OcurridoEn,
    int IdCliente, string ClienteId, string NombreCompleto,
    string NumeroDocumento, int IdEstadoCliente, bool Activo) : IEventoIntegracion;

public sealed record ClienteActualizado(
    Guid IdMensaje, DateTime OcurridoEn,
    int IdCliente, string ClienteId, string NombreCompleto,
    string NumeroDocumento, int IdEstadoCliente, bool Activo) : IEventoIntegracion;

public sealed record ClienteEstadoCambiado(
    Guid IdMensaje, DateTime OcurridoEn,
    int IdCliente, int IdEstadoCliente, bool Activo) : IEventoIntegracion;

public sealed record ClienteEliminado(
    Guid IdMensaje, DateTime OcurridoEn, int IdCliente) : IEventoIntegracion;

public sealed record CuentaAperturada(
    Guid IdMensaje, DateTime OcurridoEn,
    int IdCuenta, string NumeroCuenta, int IdCliente, decimal SaldoInicial) : IEventoIntegracion;

public sealed record CuentaEstadoCambiado(
    Guid IdMensaje, DateTime OcurridoEn,
    int IdCuenta, string NumeroCuenta, int IdCliente, bool Activa) : IEventoIntegracion;
```

`Devsu.Contracts` no referencia nada más que el framework base.

---

## 6. API REST

Prefijo `/api`. Versionado por cabecera no se implementa (se documenta como mejora).
Respuestas de error siempre en `application/problem+json`.

### 6.1 Microservicio Clientes — `http://localhost:8081`

| Verbo | Ruta | Descripción |
|---|---|---|
| GET | `/api/clientes?pagina=1&tamanio=10&busqueda=&estado=` | Listado paginado |
| GET | `/api/clientes/{id}` | Detalle |
| GET | `/api/clientes/por-identificacion/{numeroDocumento}` | Búsqueda por documento |
| POST | `/api/clientes` | Alta. 201 + `Location` |
| PUT | `/api/clientes/{id}` | Reemplazo completo |
| PATCH | `/api/clientes/{id}/estado` | Cambio de estado `{ "estado": false }` |
| DELETE | `/api/clientes/{id}` | Baja lógica por defecto; `?definitivo=true` borra físicamente |
| GET | `/api/personas?pagina=&tamanio=` | Consulta de personas |
| GET | `/api/personas/{id}` | Detalle de persona |
| GET | `/api/catalogos?raiz=TIPO_DOCUMENTO` | Ítems de un catálogo |
| GET | `/health` / `/health/ready` | Health checks |

`ClienteDto`:

```json
{
  "id": 1,
  "clienteId": "CLI-000001",
  "primerNombre": "Jose",
  "segundoNombre": null,
  "primerApellido": "Lema",
  "segundoApellido": null,
  "nombreCompleto": "Jose Lema",
  "idTipoDocumento": 2,
  "tipoDocumento": "Cedula",
  "numeroDocumento": "1712345678",
  "idGenero": 6,
  "genero": "Masculino",
  "direccionDomicilio": "Otavalo sn y principal",
  "numeroCelular": "098254785",
  "email": "jose.lema@devsu.com",
  "fechaNacimiento": "1990-05-12",
  "edad": 35,
  "estado": true,
  "idEstadoCliente": 13,
  "estadoDescripcion": "Activo",
  "totalCuentas": 2,
  "fechaCreacion": "2026-09-10T14:03:11.123Z",
  "fechaModificacion": null
}
```

`CrearClienteRequest`: `primerNombre`, `segundoNombre?`, `primerApellido`, `segundoApellido?`,
`idTipoDocumento`, `numeroDocumento`, `idGenero`, `direccionDomicilio`, `numeroCelular`,
`email`, `fechaNacimiento`, `contrasenia`, `estado`.
`ActualizarClienteRequest`: lo mismo salvo `contrasenia` (opcional; si viene vacío no se toca).

Validaciones (FluentValidation): nombres 2..64, documento 5..20 numérico según tipo, celular
exactamente 10 dígitos, email formato válido, `fechaNacimiento` en el pasado y edad >= 18,
`contrasenia` mínimo 4 caracteres (el enunciado usa "1234"), catálogos existentes.

### 6.2 Microservicio Cuentas — `http://localhost:8082`

| Verbo | Ruta | Descripción |
|---|---|---|
| GET | `/api/cuentas?clienteId=&estado=&pagina=&tamanio=` | Listado paginado |
| GET | `/api/cuentas/{id}` | Detalle |
| GET | `/api/cuentas/por-numero/{numeroCuenta}` | Búsqueda por número |
| POST | `/api/cuentas` | Alta |
| PUT | `/api/cuentas/{id}` | Actualización (tipo y estado) |
| PATCH | `/api/cuentas/{id}/estado` | Cambio de estado |
| GET | `/api/movimientos?cuentaId=&numeroCuenta=&desde=&hasta=&pagina=&tamanio=` | Listado |
| GET | `/api/movimientos/{id}` | Detalle |
| POST | `/api/movimientos` | Registro de movimiento (F2/F3) |
| PUT | `/api/movimientos/{id}` | Corrección de valor con recálculo en cadena |
| GET | `/api/reportes?fecha=2022-02-01,2022-02-28&cliente=1` | Reporte plano del enunciado (F4) |
| GET | `/api/reportes/estado-cuenta?fechaInicio=&fechaFin=&clienteId=` | Reporte enriquecido |
| GET | `/health` / `/health/ready` | Health checks |

`POST /api/movimientos` acepta `{ "numeroCuenta": "478758", "valor": -575, "descripcion": "Retiro cajero" }`.
El tipo de movimiento se deduce del signo (`valor < 0` → Retiro, `valor > 0` → Deposito) y puede
forzarse con `idTipoMovimiento`. `valor = 0` se rechaza.

Reglas de negocio (Domain, no en el controlador):
- El saldo resultante nunca puede ser negativo → `SaldoNoDisponibleException("Saldo no disponible")`.
- La cuenta debe estar activa → `CuentaInactivaException`.
- El cliente debe existir en `ClientesRef` y estar activo.
- `Movimiento.Saldo` guarda el saldo posterior; `CuentasPersona.SaldoDisponible` se actualiza en
  la misma transacción con bloqueo pesimista (`SELECT ... FOR UPDATE`) para evitar carreras.

`PUT /api/movimientos/{id}` recalcula `Saldo` de ese movimiento y de todos los posteriores de la
misma cuenta, y rechaza el cambio si en algún punto de la cadena el saldo quedaría negativo.

Respuesta de F3 (400):

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Saldo no disponible",
  "status": 400,
  "detail": "La cuenta 478758 no cuenta con saldo suficiente. Saldo disponible: 1425.00, valor solicitado: -2000.00.",
  "codigo": "SALDO_NO_DISPONIBLE",
  "traceId": "00-...."
}
```

Reporte plano (F4) — el formato del enunciado, tal cual, incluidas las claves con espacio:

```json
[
  {
    "Fecha": "10/2/2022",
    "Cliente": "Marianela Montalvo",
    "Numero Cuenta": "225487",
    "Tipo": "Corriente",
    "Saldo Inicial": 100,
    "Estado": true,
    "Movimiento": 600,
    "Saldo Disponible": 700
  }
]
```

El parámetro `fecha` admite `2022-02-01,2022-02-28` y también `01/02/2022-28/02/2022`.
`cliente` admite el `Id` numérico o el `ClienteId` de negocio.

---

## 7. Manejo de errores

`MiddlewareExcepciones` traduce a ProblemDetails:

| Excepción | HTTP | codigo |
|---|---|---|
| `RecursoNoEncontradoException` | 404 | `RECURSO_NO_ENCONTRADO` |
| `ReglaNegocioException` | 400 | `REGLA_NEGOCIO` |
| `SaldoNoDisponibleException` | 400 | `SALDO_NO_DISPONIBLE` |
| `CuentaInactivaException` | 409 | `CUENTA_INACTIVA` |
| `ClienteNoSincronizadoException` | 409 | `CLIENTE_NO_SINCRONIZADO` |
| `ConflictoConcurrenciaException` | 409 | `CONFLICTO_CONCURRENCIA` |
| `FluentValidation.ValidationException` | 422 | `VALIDACION` + diccionario `errores` |
| Cualquier otra | 500 | `ERROR_INTERNO` (sin filtrar el stack trace) |

El `traceId` siempre se incluye para poder cruzar con los logs de Serilog.

---

## 8. Pruebas

- **F5 — unitaria de dominio (Cliente)**: `ClienteTests` en `Devsu.Clientes.UnitTests`.
  Cubre creación válida, nombre completo, cambio de estado, verificación de contraseña y
  reglas que lanzan excepción. Como mínimo 8 casos.
- Unitarias adicionales: handlers con repositorios sustituidos por NSubstitute, y el dominio de
  `Cuenta`/`Movimiento` (saldo insuficiente, depósito, retiro, recálculo en cadena).
- **F6 — integración**: `WebApplicationFactory<Program>` con SQLite en memoria, broker en modo
  in-memory de MassTransit y publicador de eventos falso. Escenario mínimo:
  `POST /api/clientes` → 201 → `GET /api/clientes/{id}` → 200 con los mismos datos.
  En Cuentas: crear cuenta, registrar depósito, registrar retiro que excede el saldo → 400
  "Saldo no disponible", y consultar el reporte.
- `Program` debe ser accesible desde los tests: añadir `public partial class Program { }` al final
  de `Program.cs` de cada API.

---

## 9. Docker

`docker-compose.yml` en la raíz de `devsu_test/`. Servicios:

| Servicio | Imagen / build | Puerto host | Depende de |
|---|---|---|---|
| `devsu-postgres` | `postgres:16-alpine` | 5432 | — |
| `devsu-rabbitmq` | `rabbitmq:3.13-management-alpine` | 5672, 15672 | — |
| `devsu-clientes-api` | build `./server`, `clientes/Dockerfile` | 8081 → 8080 | postgres healthy, rabbitmq healthy |
| `devsu-cuentas-api` | build `./server`, `cuentas/Dockerfile` | 8082 → 8080 | postgres healthy, rabbitmq healthy |
| `devsu-client` | build `./client` | 3000 → 80 | — |

- El script `database/BaseDatos.sql` se monta en `/docker-entrypoint-initdb.d/01-BaseDatos.sql`
  y `datos-prueba.sql` como `02-datos-prueba.sql`. Se ejecutan en orden alfabético, una sola vez,
  cuando el volumen está vacío.
- `BaseDatos.sql` crea ambas bases con `CREATE DATABASE` y cambia entre ellas con `\connect`
  (meta-comando de psql, que es lo que usa el entrypoint de la imagen).
- Healthchecks: `pg_isready` para Postgres, `rabbitmq-diagnostics -q ping` para RabbitMQ,
  `curl -f http://localhost:8080/health` para las APIs.
- Dockerfiles multi-stage: `sdk:10.0` para restore/publish, `aspnet:10.0` para runtime,
  usuario no root, `ASPNETCORE_URLS=http://+:8080`.
- El front se sirve con `nginx:alpine` y un `nginx.conf` que hace proxy de `/api/clientes`,
  `/api/personas`, `/api/catalogos` al ms de Clientes y de `/api/cuentas`, `/api/movimientos`,
  `/api/reportes` al ms de Cuentas. Así el navegador no sufre CORS ni necesita dos hosts.

Variables de entorno de las APIs:

```
ConnectionStrings__Postgres=Host=devsu-postgres;Port=5432;Database=devsu_clientes;Username=devsu;Password=devsu2024
RabbitMq__Host=devsu-rabbitmq
RabbitMq__Usuario=devsu
RabbitMq__Contrasenia=devsu2024
Firebase__Habilitado=false
Firebase__ProjectId=
ServiciosExternos__ClientesApi=http://devsu-clientes-api:8080
```

---

## 10. Frontend

React 18 + Vite 5 + TypeScript. Sin librería de componentes pesada: CSS propio con variables,
diseño sobrio, responsive. Estructura:

```
client/src/
  main.tsx  App.tsx
  firebase/config.ts            initializeApp con variables VITE_*
  auth/ContextoAuth.tsx         Provider con onAuthStateChanged
  auth/RutaProtegida.tsx
  api/clienteHttp.ts            instancia fetch/axios con baseURL e interceptor de token
  api/clientesApi.ts  cuentasApi.ts  movimientosApi.ts  reportesApi.ts
  paginas/Login.tsx  Clientes.tsx  Cuentas.tsx  Movimientos.tsx  Reportes.tsx
  componentes/  Tabla.tsx  Modal.tsx  Formulario*.tsx  Alerta.tsx  Cargando.tsx
  tipos/index.ts
```

Login con Firebase Authentication (email/contraseña + Google). El token se envía en
`Authorization: Bearer`. Como el backend trae la validación desactivada por defecto
(`Firebase__Habilitado=false`), el evaluador puede probar con Postman sin token; al activarla,
las APIs validan el JWT contra `https://securetoken.google.com/{projectId}`.

Variables: `VITE_FIREBASE_API_KEY`, `VITE_FIREBASE_AUTH_DOMAIN`, `VITE_FIREBASE_PROJECT_ID`,
`VITE_FIREBASE_STORAGE_BUCKET`, `VITE_FIREBASE_MESSAGING_SENDER_ID`, `VITE_FIREBASE_APP_ID`,
`VITE_API_URL` (por defecto `/api`, resuelto por el proxy de nginx o el de Vite en desarrollo).

Debe existir `client/.env.example` y el README debe explicar cómo crear el proyecto en Firebase.
Si las variables no están configuradas, la pantalla de login muestra un aviso claro y permite
entrar en "modo demostración" (sin token) para no bloquear la revisión.

---

## 11. Entregables exigidos por el enunciado

- [x] `database/BaseDatos.sql`
- [x] Colección Postman con todos los endpoints y los casos de uso 1..5 del enunciado
- [x] F1 CRUD Cliente / CRU Cuenta y Movimiento
- [x] F2 registro de movimientos con actualización de saldo
- [x] F3 "Saldo no disponible"
- [x] F4 reporte por rango de fechas y cliente en JSON
- [x] F5 prueba unitaria de la entidad Cliente
- [x] F6 prueba de integración
- [x] F7 despliegue en contenedores
- [x] README con instrucciones de despliegue
