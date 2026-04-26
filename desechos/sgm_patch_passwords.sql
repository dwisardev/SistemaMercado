-- =============================================================
--  PATCH: Agregar columna password_hash a sgm.usuarios
--  Ejecutar en pgAdmin DESPUÉS del script principal
-- =============================================================

-- 1. Agregar la columna (permite vacío temporalmente)
ALTER TABLE sgm.usuarios
    ADD COLUMN IF NOT EXISTS password_hash VARCHAR(200) NOT NULL DEFAULT '';

-- 2. Verificar que se creó
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'sgm' AND table_name = 'usuarios' AND column_name = 'password_hash';

-- =============================================================
--  NOTA: Los hashes de contraseña se insertan automáticamente
--  al arrancar el backend por primera vez (DataSeeder.cs).
--
--  Credenciales de prueba:
--    admin@mercado.com   → Admin123!
--    cajero1@mercado.com → Cajero123!
--    cajera2@mercado.com → Cajero123!
-- =============================================================
