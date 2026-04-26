-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  SGM — CASTS DE ASIGNACIÓN PARA ENUMS PERSONALIZADOS           ║
-- ║  Ejecutar una sola vez por entorno (local, staging, prod)       ║
-- ║  Requerido para que EF Core + Npgsql pueda hacer INSERT/UPDATE  ║
-- ║  con columnas de tipo enum custom de PostgreSQL.                ║
-- ╚══════════════════════════════════════════════════════════════════╝

-- estado_deuda
CREATE CAST (varchar AS sgm.estado_deuda) WITH INOUT AS ASSIGNMENT;
CREATE CAST (text    AS sgm.estado_deuda) WITH INOUT AS ASSIGNMENT;

-- estado_pago
CREATE CAST (varchar AS sgm.estado_pago) WITH INOUT AS ASSIGNMENT;
CREATE CAST (text    AS sgm.estado_pago) WITH INOUT AS ASSIGNMENT;

-- metodo_pago
CREATE CAST (varchar AS sgm.metodo_pago) WITH INOUT AS ASSIGNMENT;
CREATE CAST (text    AS sgm.metodo_pago) WITH INOUT AS ASSIGNMENT;

-- estado_puesto
CREATE CAST (varchar AS sgm.estado_puesto) WITH INOUT AS ASSIGNMENT;
CREATE CAST (text    AS sgm.estado_puesto) WITH INOUT AS ASSIGNMENT;

-- rol_usuario
CREATE CAST (varchar AS sgm.rol_usuario) WITH INOUT AS ASSIGNMENT;
CREATE CAST (text    AS sgm.rol_usuario) WITH INOUT AS ASSIGNMENT;
