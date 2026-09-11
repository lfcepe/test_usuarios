-- ============================================================================
-- Archivo   : datos-prueba.sql
-- Proposito : Carga los casos de uso 1, 2 y 3 del enunciado (creacion de
--             usuarios y de cuentas). Se ejecuta despues de BaseDatos.sql.
--
-- Los movimientos del caso de uso 4 NO se insertan aqui de forma deliberada.
-- Registrarlos por SQL saltaria la logica de negocio y dejaria la demostracion
-- sin valor: se ejecutan desde la coleccion de Postman contra
-- POST /api/movimientos, que es lo que acredita F2 y F3.
--
-- Ejecucion manual:
--     psql -h localhost -U devsu -d postgres -v ON_ERROR_STOP=1 -f datos-prueba.sql
-- ============================================================================


\connect devsu_clientes

DELETE FROM "ResumenCuentasCliente";
DELETE FROM "Cliente";
DELETE FROM "Personas";


-- El enunciado solo da nombre, direccion, telefono y contrasenia. El resto de
-- campos obligatorios (documento, genero, correo, fecha de nacimiento) se
-- completa con valores verosimiles para que las validaciones del API pasen.
--
-- Todo el texto va en mayusculas: es la misma normalizacion que aplica la clase
-- Normalizador del dominio, de modo que una fila cargada por script y otra creada
-- desde el API quedan indistinguibles.
INSERT INTO "Personas"(
    "Id", "PrimerNombre", "SegundoNombre", "PrimerApellido", "SegundoApellido",
    "IdTipoDocumento", "NumeroDocumento", "IdGenero", "DireccionDomicilio",
    "NumeroCelular", "Email", "FechaNacimiento", "IdEstadoPersona") VALUES
    (1, 'JOSE', NULL, 'LEMA', NULL, 2, '1712345678', 6, 'OTAVALO SN Y PRINCIPAL', '0982547850', 'JOSE.LEMA@DEVSU.COM', DATE '1985-03-14', 10),
    (2, 'MARIANELA', NULL, 'MONTALVO', NULL, 2, '1709876543', 7, 'AMAZONAS Y NNUU', '0975489650', 'MARIANELA.MONTALVO@DEVSU.COM', DATE '1990-11-02', 10),
    (3, 'JUAN', NULL, 'OSORIO', NULL, 2, '1723456789', 6, '13 JUNIO Y EQUINOCCIAL', '0988745870', 'JUAN.OSORIO@DEVSU.COM', DATE '1978-07-21', 10);


-- La contrasenia se guarda como hash PBKDF2 y SQL no puede calcularlo. Se deja
-- el literal __PENDIENTE_HASH__ y el microservicio de Clientes lo sustituye al
-- arrancar por el hash real de la clave del enunciado (ver SeedContrasenias en
-- la capa de infraestructura). El acuerdo entre el script y la aplicacion es
-- justamente ese literal: si se cambia aqui, hay que cambiarlo alla.
INSERT INTO "Cliente"("Id", "ClienteId", "Contrasenia", "IdEstadoCliente") VALUES
    (1, 'CLI-000001', '__PENDIENTE_HASH__', 13),
    (2, 'CLI-000002', '__PENDIENTE_HASH__', 13),
    (3, 'CLI-000003', '__PENDIENTE_HASH__', 13);


-- Contador de cuentas activas por cliente. En operacion normal lo mantienen los
-- eventos CuentaAperturada; aqui se siembra coherente con las cuentas de abajo.
INSERT INTO "ResumenCuentasCliente"("IdCliente", "TotalCuentas") VALUES
    (1, 2),
    (2, 2),
    (3, 1);

SELECT setval(pg_get_serial_sequence('"Personas"', 'Id'), (SELECT MAX("Id") FROM "Personas"));
SELECT setval('seq_cliente_consecutivo', 3);


\connect devsu_cuentas

DELETE FROM "Movimientos";
DELETE FROM "CuentasPersona";
DELETE FROM "ClientesRef";


-- Espejo de los tres clientes. En operacion normal lo escribe el consumidor de
-- ClienteCreado; se siembra para que los datos de prueba esten completos aunque
-- se levante la base sin el broker.
INSERT INTO "ClientesRef"(
    "IdCliente", "ClienteId", "NombreCompleto", "NumeroDocumento", "IdEstadoCliente", "Activo") VALUES
    (1, 'CLI-000001', 'JOSE LEMA', '1712345678', 13, TRUE),
    (2, 'CLI-000002', 'MARIANELA MONTALVO', '1709876543', 13, TRUE),
    (3, 'CLI-000003', 'JUAN OSORIO', '1723456789', 13, TRUE);


-- Casos de uso 2 y 3 del enunciado. SaldoDisponible arranca igual a SaldoInicial
-- porque todavia no hay movimientos.
INSERT INTO "CuentasPersona"(
    "Id", "IdCliente", "NumeroCuenta", "IdTipoCuenta", "SaldoInicial", "SaldoDisponible", "IdEstadoCuenta") VALUES
    (1, 1, '478758', 16, 2000.00, 2000.00, 19),
    (2, 2, '225487', 17, 100.00, 100.00, 19),
    (3, 3, '495878', 16, 0.00, 0.00, 19),
    (4, 2, '496825', 16, 540.00, 540.00, 19),
    (5, 1, '585545', 17, 1000.00, 1000.00, 19);

SELECT setval(pg_get_serial_sequence('"CuentasPersona"', 'Id'), (SELECT MAX("Id") FROM "CuentasPersona"));
SELECT setval(pg_get_serial_sequence('"Movimientos"', 'Id'), 1, FALSE);

-- Fin de datos-prueba.sql
