-- ============================================================================
-- Proyecto  : Prueba tecnica Devsu - Arquitectura de microservicios
-- Motor     : PostgreSQL 16
-- Archivo   : BaseDatos.sql
-- Proposito : Crea las dos bases de datos del sistema (una por microservicio),
--             su esquema completo, los objetos de calculo de edad y los catalogos.
--
-- Ejecucion manual:
--     psql -h localhost -U devsu -d postgres -v ON_ERROR_STOP=1 -f BaseDatos.sql
--
-- En Docker este archivo se monta en /docker-entrypoint-initdb.d y la imagen lo
-- ejecuta una unica vez, cuando el volumen de datos esta vacio.
--
-- NOTA SOBRE LAS COMILLAS DOBLES
-- PostgreSQL pasa a minusculas todo identificador que no venga entrecomillado.
-- Entity Framework Core con el proveedor Npgsql si entrecomilla los nombres del
-- modelo, de modo que si aqui se escribiera CREATE TABLE Personas se crearia la
-- tabla "personas" y la aplicacion buscaria "Personas" sin encontrarla. Por eso
-- todas las tablas y columnas van entre comillas dobles. Los nombres de las
-- restricciones van en minusculas y sin comillas, siguiendo la convencion del
-- modelo original.
-- ============================================================================


-- ----------------------------------------------------------------------------
-- 1. Rol de aplicacion y bases de datos
-- ----------------------------------------------------------------------------

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'devsu') THEN
        CREATE ROLE devsu LOGIN PASSWORD 'devsu2024';
    END IF;
END
$$;

SELECT 'CREATE DATABASE devsu_clientes OWNER devsu'
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'devsu_clientes')
\gexec

SELECT 'CREATE DATABASE devsu_cuentas OWNER devsu'
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'devsu_cuentas')
\gexec


-- ============================================================================
-- 2. BASE DE DATOS: devsu_clientes   (microservicio de Clientes y Personas)
-- ============================================================================

\connect devsu_clientes

DROP TABLE IF EXISTS "MensajesProcesados" CASCADE;
DROP TABLE IF EXISTS "OutboxMensajes" CASCADE;
DROP TABLE IF EXISTS "ResumenCuentasCliente" CASCADE;
DROP TABLE IF EXISTS "Cliente" CASCADE;
DROP TABLE IF EXISTS "Personas" CASCADE;
DROP TABLE IF EXISTS "Catalogos" CASCADE;
DROP SEQUENCE IF EXISTS seq_cliente_consecutivo;
DROP PROCEDURE IF EXISTS sp_registrar_catalogo(VARCHAR, VARCHAR);
DROP PROCEDURE IF EXISTS sp_actualizar_edades();
DROP FUNCTION IF EXISTS trg_personas_calcular_edad() CASCADE;
DROP FUNCTION IF EXISTS fn_calcular_edad(DATE) CASCADE;


-- Catalogo auto referenciado. Una fila con "IdRaiz" nulo es la cabecera del
-- catalogo y lleva el nombre en "DetalleCatalogo"; sus items cuelgan de ella con
-- el valor en "Item" y "DetalleCatalogo" nulo. Es la misma convencion del
-- procedimiento de carga original y la que aplica sp_registrar_catalogo.
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

CREATE INDEX ix_Personas_NumeroDocumento ON "Personas"("NumeroDocumento");
CREATE INDEX ix_Personas_Apellidos ON "Personas"("PrimerApellido", "PrimerNombre");


-- El enunciado pide que Cliente herede de Persona. La forma canonica de mapear
-- herencia en un modelo relacional con EF Core es Table-Per-Type: la tabla hija
-- comparte la clave primaria con la tabla base en lugar de tener su propio
-- identificador mas una FK. Por eso "Id" es a la vez PK y FK hacia "Personas".
-- La clave unica de cliente que pide el enunciado se conserva como columna de
-- negocio "ClienteId", con formato CLI-000001.
CREATE TABLE "Cliente"(
    "Id" INT PRIMARY KEY,
    "ClienteId" VARCHAR(20) NOT NULL,
    "Contrasenia" VARCHAR(256) NOT NULL,
    "FechaCreacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "FechaModificacion" TIMESTAMP NULL,
    "IdEstadoCliente" INT NOT NULL,
    CONSTRAINT uq_Cliente_ClienteId UNIQUE ("ClienteId"),
    CONSTRAINT fk_IdPersona_Personas FOREIGN KEY ("Id") REFERENCES "Personas"("Id") ON DELETE CASCADE,
    CONSTRAINT fk_IdEstadoCliente_Catalogos FOREIGN KEY ("IdEstadoCliente") REFERENCES "Catalogos"("Id")
);

CREATE INDEX ix_Cliente_IdEstadoCliente ON "Cliente"("IdEstadoCliente");


-- Consecutivo del identificador de negocio "ClienteId" (CLI-000001).
-- Se usa una secuencia y no MAX("Id") + 1 porque dos altas simultaneas leerian el
-- mismo maximo y generarian el mismo codigo, violando uq_Cliente_ClienteId.
CREATE SEQUENCE seq_cliente_consecutivo START WITH 1 INCREMENT BY 1;


-- Read model alimentado por los eventos que publica el microservicio de Cuentas.
-- Permite exponer el numero de cuentas de un cliente sin llamar al otro servicio.
CREATE TABLE "ResumenCuentasCliente"(
    "IdCliente" INT PRIMARY KEY,
    "TotalCuentas" INT NOT NULL DEFAULT 0,
    "FechaActualizacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_IdCliente_Cliente FOREIGN KEY ("IdCliente") REFERENCES "Cliente"("Id") ON DELETE CASCADE
);


-- Patron Transactional Outbox. El evento se guarda en la misma transaccion que
-- el cambio de negocio y un BackgroundService lo publica despues. Sin esto, si
-- RabbitMQ esta caido en el instante del POST el evento se pierde en silencio y
-- las dos bases quedan desincronizadas de forma permanente.
CREATE TABLE "OutboxMensajes"(
    "Id" UUID PRIMARY KEY,
    "TipoMensaje" VARCHAR(256) NOT NULL,
    "Contenido" TEXT NOT NULL,
    "FechaCreacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "FechaProcesado" TIMESTAMP NULL,
    "Intentos" INT NOT NULL DEFAULT 0,
    "Error" TEXT NULL
);

CREATE INDEX ix_OutboxMensajes_Pendientes
    ON "OutboxMensajes"("FechaCreacion")
    WHERE "FechaProcesado" IS NULL;


-- Patron Inbox. RabbitMQ garantiza entrega al menos una vez, asi que un
-- reintento puede reentregar un evento ya aplicado. Esta tabla lo evita.
CREATE TABLE "MensajesProcesados"(
    "IdMensaje" UUID PRIMARY KEY,
    "TipoMensaje" VARCHAR(256) NOT NULL,
    "FechaProcesado" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);


-- ----------------------------------------------------------------------------
-- 2.1 Carga de catalogos
--
-- Traduccion a PostgreSQL del procedimiento de carga del modelo original: si la
-- cabecera no existe se crea, y si ya existe se le cuelga el item. Se anaden dos
-- cosas al original: los valores se normalizan a mayusculas de forma consistente
-- y la insercion del item comprueba que no exista ya, para que el script se pueda
-- volver a ejecutar sin duplicar filas.
-- ----------------------------------------------------------------------------

CREATE OR REPLACE PROCEDURE sp_registrar_catalogo(
    p_detalle_catalogo VARCHAR,
    p_item VARCHAR DEFAULT NULL)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id_raiz INT;
    v_detalle VARCHAR := UPPER(TRIM(p_detalle_catalogo));
    v_item VARCHAR := UPPER(TRIM(p_item));
BEGIN
    SELECT "Id" INTO v_id_raiz
      FROM "Catalogos"
     WHERE "DetalleCatalogo" = v_detalle
       AND "IdRaiz" IS NULL;

    IF v_id_raiz IS NULL THEN
        INSERT INTO "Catalogos"("DetalleCatalogo")
        VALUES (v_detalle)
        RETURNING "Id" INTO v_id_raiz;

        RAISE NOTICE 'CATALOGO REGISTRADO: %', v_detalle;
    END IF;

    IF v_item IS NULL THEN
        RETURN;
    END IF;

    IF EXISTS (SELECT 1 FROM "Catalogos" WHERE "IdRaiz" = v_id_raiz AND "Item" = v_item) THEN
        RAISE NOTICE 'ITEM YA EXISTENTE: % / %', v_detalle, v_item;
        RETURN;
    END IF;

    INSERT INTO "Catalogos"("Item", "IdRaiz")
    VALUES (v_item, v_id_raiz);

    RAISE NOTICE 'ITEM REGISTRADO: % / %', v_detalle, v_item;
END;
$$;


-- El orden de estas llamadas fija los identificadores del 1 al 26. No es un
-- detalle cosmetico: esos numeros viajan dentro de los eventos de integracion y
-- las dos bases tienen que interpretarlos igual. La clase CatalogoIds de cada
-- microservicio es el espejo en codigo de esta lista.
CALL sp_registrar_catalogo('TIPO_DOCUMENTO');
CALL sp_registrar_catalogo('TIPO_DOCUMENTO', 'Cedula');
CALL sp_registrar_catalogo('TIPO_DOCUMENTO', 'Pasaporte');
CALL sp_registrar_catalogo('TIPO_DOCUMENTO', 'RUC');

CALL sp_registrar_catalogo('GENERO');
CALL sp_registrar_catalogo('GENERO', 'Masculino');
CALL sp_registrar_catalogo('GENERO', 'Femenino');
CALL sp_registrar_catalogo('GENERO', 'Otro');

CALL sp_registrar_catalogo('ESTADO_PERSONA');
CALL sp_registrar_catalogo('ESTADO_PERSONA', 'Activo');
CALL sp_registrar_catalogo('ESTADO_PERSONA', 'Inactivo');

CALL sp_registrar_catalogo('ESTADO_CLIENTE');
CALL sp_registrar_catalogo('ESTADO_CLIENTE', 'Activo');
CALL sp_registrar_catalogo('ESTADO_CLIENTE', 'Inactivo');

CALL sp_registrar_catalogo('TIPO_CUENTA');
CALL sp_registrar_catalogo('TIPO_CUENTA', 'Ahorros');
CALL sp_registrar_catalogo('TIPO_CUENTA', 'Corriente');

CALL sp_registrar_catalogo('ESTADO_CUENTA');
CALL sp_registrar_catalogo('ESTADO_CUENTA', 'Activa');
CALL sp_registrar_catalogo('ESTADO_CUENTA', 'Inactiva');

CALL sp_registrar_catalogo('TIPO_MOVIMIENTO');
CALL sp_registrar_catalogo('TIPO_MOVIMIENTO', 'Deposito');
CALL sp_registrar_catalogo('TIPO_MOVIMIENTO', 'Retiro');

CALL sp_registrar_catalogo('ESTADO_MOVIMIENTO');
CALL sp_registrar_catalogo('ESTADO_MOVIMIENTO', 'Aplicado');
CALL sp_registrar_catalogo('ESTADO_MOVIMIENTO', 'Reversado');


-- Salvaguarda: si alguien anade una llamada en medio de la lista, los
-- identificadores se desplazan y el sistema queda inconsistente sin dar ningun
-- error visible hasta mucho despues. Mejor detenerse aqui.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Catalogos" WHERE "Id" = 13 AND "Item" = 'ACTIVO') THEN
        RAISE EXCEPTION 'Los identificadores de catalogo no coinciden con CatalogoIds. Revise el orden de las llamadas a sp_registrar_catalogo.';
    END IF;
END
$$;


-- ----------------------------------------------------------------------------
-- 2.2 Calculo de la edad
--
-- Se resuelve con tres objetos porque cubren dos problemas distintos:
--   - el trigger mantiene "Edad" coherente en cada escritura;
--   - el procedimiento la recalcula en masa, porque la edad cambia con el paso
--     del tiempo sin que la fila se modifique (se ejecuta desde un job diario).
-- Una columna GENERATED ALWAYS AS no sirve aqui: PostgreSQL exige una expresion
-- inmutable y CURRENT_DATE no lo es.
-- ----------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_calcular_edad(p_fecha_nacimiento DATE)
RETURNS INT
LANGUAGE sql
STABLE
AS $$
    SELECT EXTRACT(YEAR FROM AGE(CURRENT_DATE, p_fecha_nacimiento))::INT;
$$;


CREATE OR REPLACE FUNCTION trg_personas_calcular_edad()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW."FechaNacimiento" > CURRENT_DATE THEN
        RAISE EXCEPTION 'La fecha de nacimiento no puede ser posterior a la fecha actual (%).',
            NEW."FechaNacimiento"
            USING ERRCODE = '23514';
    END IF;

    NEW."Edad" := fn_calcular_edad(NEW."FechaNacimiento");
    RETURN NEW;
END;
$$;


CREATE TRIGGER tr_personas_edad
    BEFORE INSERT OR UPDATE ON "Personas"
    FOR EACH ROW
    EXECUTE FUNCTION trg_personas_calcular_edad();


CREATE OR REPLACE PROCEDURE sp_actualizar_edades()
LANGUAGE plpgsql
AS $$
DECLARE
    v_filas INT;
BEGIN
    UPDATE "Personas"
       SET "Edad" = fn_calcular_edad("FechaNacimiento")
     WHERE "Edad" IS DISTINCT FROM fn_calcular_edad("FechaNacimiento");

    GET DIAGNOSTICS v_filas = ROW_COUNT;
    RAISE NOTICE 'sp_actualizar_edades: % fila(s) actualizada(s).', v_filas;
END;
$$;


COMMENT ON TABLE "Cliente" IS 'Hereda de "Personas" mediante Table-Per-Type: "Id" es a la vez PK y FK.';
COMMENT ON COLUMN "Cliente"."Contrasenia" IS 'Hash PBKDF2 en formato iteraciones.saltBase64.hashBase64. Nunca texto plano.';
COMMENT ON COLUMN "Personas"."Edad" IS 'La mantiene el trigger tr_personas_edad. La aplicacion no la escribe.';
COMMENT ON TABLE "Catalogos" IS 'Cabeceras con "IdRaiz" nulo y "DetalleCatalogo" informado; items con "Item" e "IdRaiz".';
COMMENT ON TABLE "ResumenCuentasCliente" IS 'Read model alimentado por eventos del microservicio de Cuentas.';
COMMENT ON COLUMN "OutboxMensajes"."Contenido" IS 'Evento de integracion serializado en JSON.';
COMMENT ON TABLE "MensajesProcesados" IS 'Claves de idempotencia de los eventos ya consumidos.';

GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO devsu;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO devsu;
GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public TO devsu;


-- ============================================================================
-- 3. BASE DE DATOS: devsu_cuentas   (microservicio de Cuentas y Movimientos)
-- ============================================================================

\connect devsu_cuentas

DROP TABLE IF EXISTS "MensajesProcesados" CASCADE;
DROP TABLE IF EXISTS "OutboxMensajes" CASCADE;
DROP TABLE IF EXISTS "Movimientos" CASCADE;
DROP TABLE IF EXISTS "CuentasPersona" CASCADE;
DROP TABLE IF EXISTS "ClientesRef" CASCADE;
DROP TABLE IF EXISTS "Catalogos" CASCADE;
DROP PROCEDURE IF EXISTS sp_registrar_catalogo(VARCHAR, VARCHAR);


CREATE TABLE "Catalogos"(
    "Id" SERIAL PRIMARY KEY,
    "DetalleCatalogo" VARCHAR(128) NULL,
    "Item" VARCHAR(128) NULL,
    "IdRaiz" INT NULL,
    CONSTRAINT fk_IdRaiz_Catalogos FOREIGN KEY ("IdRaiz") REFERENCES "Catalogos"("Id")
);


-- Replica local de solo lectura del cliente. No es la fuente de verdad: la
-- mantiene el consumidor de eventos. Existe para que el reporte de estado de
-- cuenta pueda mostrar el nombre del titular sin una llamada sincrona, y para
-- validar la existencia del cliente aunque el otro microservicio este caido.
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

CREATE INDEX ix_CuentasPersona_IdCliente ON "CuentasPersona"("IdCliente");


CREATE TABLE "Movimientos"(
    "Id" SERIAL PRIMARY KEY,
    "IdCuentaPersona" INT NOT NULL,
    "Fecha" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IdTipoMovimiento" INT NOT NULL,
    "Valor" DECIMAL(18,2) NOT NULL,
    "Saldo" DECIMAL(18,2) NOT NULL,
    "Descripcion" VARCHAR(256) NULL,
    "FechaCreacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "FechaModificacion" TIMESTAMP NULL,
    "IdEstadoMovimiento" INT NOT NULL,
    CONSTRAINT fk_IdCuentaPersona_Movimientos FOREIGN KEY ("IdCuentaPersona") REFERENCES "CuentasPersona"("Id") ON DELETE CASCADE,
    CONSTRAINT fk_IdTipoMovimiento_Catalogos FOREIGN KEY ("IdTipoMovimiento") REFERENCES "Catalogos"("Id"),
    CONSTRAINT fk_IdEstadoMovimiento_Catalogos FOREIGN KEY ("IdEstadoMovimiento") REFERENCES "Catalogos"("Id")
);

CREATE INDEX ix_Movimientos_Cuenta_Fecha ON "Movimientos"("IdCuentaPersona", "Fecha" DESC);


CREATE TABLE "OutboxMensajes"(
    "Id" UUID PRIMARY KEY,
    "TipoMensaje" VARCHAR(256) NOT NULL,
    "Contenido" TEXT NOT NULL,
    "FechaCreacion" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "FechaProcesado" TIMESTAMP NULL,
    "Intentos" INT NOT NULL DEFAULT 0,
    "Error" TEXT NULL
);

CREATE INDEX ix_OutboxMensajes_Pendientes
    ON "OutboxMensajes"("FechaCreacion")
    WHERE "FechaProcesado" IS NULL;


CREATE TABLE "MensajesProcesados"(
    "IdMensaje" UUID PRIMARY KEY,
    "TipoMensaje" VARCHAR(256) NOT NULL,
    "FechaProcesado" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);


CREATE OR REPLACE PROCEDURE sp_registrar_catalogo(
    p_detalle_catalogo VARCHAR,
    p_item VARCHAR DEFAULT NULL)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id_raiz INT;
    v_detalle VARCHAR := UPPER(TRIM(p_detalle_catalogo));
    v_item VARCHAR := UPPER(TRIM(p_item));
BEGIN
    SELECT "Id" INTO v_id_raiz
      FROM "Catalogos"
     WHERE "DetalleCatalogo" = v_detalle
       AND "IdRaiz" IS NULL;

    IF v_id_raiz IS NULL THEN
        INSERT INTO "Catalogos"("DetalleCatalogo")
        VALUES (v_detalle)
        RETURNING "Id" INTO v_id_raiz;

        RAISE NOTICE 'CATALOGO REGISTRADO: %', v_detalle;
    END IF;

    IF v_item IS NULL THEN
        RETURN;
    END IF;

    IF EXISTS (SELECT 1 FROM "Catalogos" WHERE "IdRaiz" = v_id_raiz AND "Item" = v_item) THEN
        RAISE NOTICE 'ITEM YA EXISTENTE: % / %', v_detalle, v_item;
        RETURN;
    END IF;

    INSERT INTO "Catalogos"("Item", "IdRaiz")
    VALUES (v_item, v_id_raiz);

    RAISE NOTICE 'ITEM REGISTRADO: % / %', v_detalle, v_item;
END;
$$;


-- Se cargan los ocho catalogos completos y no solo los cuatro que usa este
-- servicio. Cuesta doce filas de mas y garantiza que un identificador signifique
-- lo mismo en las dos bases, que es lo que permite que los eventos de
-- integracion lleven numeros en lugar de textos.
CALL sp_registrar_catalogo('TIPO_DOCUMENTO');
CALL sp_registrar_catalogo('TIPO_DOCUMENTO', 'Cedula');
CALL sp_registrar_catalogo('TIPO_DOCUMENTO', 'Pasaporte');
CALL sp_registrar_catalogo('TIPO_DOCUMENTO', 'RUC');

CALL sp_registrar_catalogo('GENERO');
CALL sp_registrar_catalogo('GENERO', 'Masculino');
CALL sp_registrar_catalogo('GENERO', 'Femenino');
CALL sp_registrar_catalogo('GENERO', 'Otro');

CALL sp_registrar_catalogo('ESTADO_PERSONA');
CALL sp_registrar_catalogo('ESTADO_PERSONA', 'Activo');
CALL sp_registrar_catalogo('ESTADO_PERSONA', 'Inactivo');

CALL sp_registrar_catalogo('ESTADO_CLIENTE');
CALL sp_registrar_catalogo('ESTADO_CLIENTE', 'Activo');
CALL sp_registrar_catalogo('ESTADO_CLIENTE', 'Inactivo');

CALL sp_registrar_catalogo('TIPO_CUENTA');
CALL sp_registrar_catalogo('TIPO_CUENTA', 'Ahorros');
CALL sp_registrar_catalogo('TIPO_CUENTA', 'Corriente');

CALL sp_registrar_catalogo('ESTADO_CUENTA');
CALL sp_registrar_catalogo('ESTADO_CUENTA', 'Activa');
CALL sp_registrar_catalogo('ESTADO_CUENTA', 'Inactiva');

CALL sp_registrar_catalogo('TIPO_MOVIMIENTO');
CALL sp_registrar_catalogo('TIPO_MOVIMIENTO', 'Deposito');
CALL sp_registrar_catalogo('TIPO_MOVIMIENTO', 'Retiro');

CALL sp_registrar_catalogo('ESTADO_MOVIMIENTO');
CALL sp_registrar_catalogo('ESTADO_MOVIMIENTO', 'Aplicado');
CALL sp_registrar_catalogo('ESTADO_MOVIMIENTO', 'Reversado');


DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Catalogos" WHERE "Id" = 16 AND "Item" = 'AHORROS') THEN
        RAISE EXCEPTION 'Los identificadores de catalogo no coinciden con CatalogoIds. Revise el orden de las llamadas a sp_registrar_catalogo.';
    END IF;
END
$$;


COMMENT ON TABLE "ClientesRef" IS 'Replica de solo lectura del cliente, mantenida por eventos. No es la fuente de verdad.';
COMMENT ON COLUMN "CuentasPersona"."SaldoInicial" IS 'Saldo de apertura. No cambia nunca; conservarlo es lo que permite auditar la cuenta.';
COMMENT ON COLUMN "CuentasPersona"."SaldoDisponible" IS 'Saldo vigente. Lo actualiza cada movimiento dentro de la misma transaccion.';
COMMENT ON COLUMN "Movimientos"."Valor" IS 'Positivo para deposito, negativo para retiro.';
COMMENT ON COLUMN "Movimientos"."Saldo" IS 'Saldo de la cuenta despues de aplicar este movimiento.';

GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO devsu;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO devsu;
GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public TO devsu;

-- Fin de BaseDatos.sql
