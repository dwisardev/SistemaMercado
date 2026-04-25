-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  SISTEMA DE GESTIÓN DE MERCADO (SGM)                           ║
-- ║  PostgreSQL 17 LOCAL (sin Supabase)                            ║
-- ║  Para ejecutar en pgAdmin: Tools > Query Tool > Pegar > F5     ║
-- ╚══════════════════════════════════════════════════════════════════╝

-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 0: PREPARACIÓN                      │
-- └─────────────────────────────────────────────┘

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE SCHEMA IF NOT EXISTS sgm;

-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 1: ENUMS                            │
-- └─────────────────────────────────────────────┘

DO $$ BEGIN
    CREATE TYPE sgm.rol_usuario AS ENUM ('admin', 'cajero', 'dueno');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE sgm.estado_puesto AS ENUM ('activo', 'inactivo', 'en_mantenimiento');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE sgm.estado_deuda AS ENUM ('pendiente', 'pagada', 'anulada');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE sgm.estado_pago AS ENUM ('activo', 'anulado');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE sgm.metodo_pago AS ENUM ('efectivo', 'transferencia', 'otro');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;


-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 2: TABLAS PRINCIPALES               │
-- └─────────────────────────────────────────────┘

-- 2.1 USUARIOS
CREATE TABLE IF NOT EXISTS sgm.usuarios (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre_completo VARCHAR(200) NOT NULL,
    email           VARCHAR(150) NOT NULL,
    telefono        VARCHAR(20),
    dni             VARCHAR(15)  NOT NULL,
    rol             sgm.rol_usuario NOT NULL DEFAULT 'dueno',
    activo          BOOLEAN NOT NULL DEFAULT TRUE,
    auth_user_id    UUID,
    metadata        JSONB DEFAULT '{}',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_usuarios_email UNIQUE (email),
    CONSTRAINT uq_usuarios_dni   UNIQUE (dni)
);

-- 2.2 PUESTOS
CREATE TABLE IF NOT EXISTS sgm.puestos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    codigo          VARCHAR(20) NOT NULL,
    descripcion     VARCHAR(300),
    ubicacion       VARCHAR(100),
    area_m2         DECIMAL(8,2),
    dueno_id        UUID REFERENCES sgm.usuarios(id) ON DELETE SET NULL,
    estado          sgm.estado_puesto NOT NULL DEFAULT 'activo',
    fecha_asignacion DATE,
    metadata        JSONB DEFAULT '{}',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_puestos_codigo UNIQUE (codigo)
);

-- 2.3 HISTORIAL DE DUEÑOS
CREATE TABLE IF NOT EXISTS sgm.historial_duenos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    puesto_id       UUID NOT NULL REFERENCES sgm.puestos(id) ON DELETE CASCADE,
    dueno_id        UUID REFERENCES sgm.usuarios(id) ON DELETE SET NULL,
    fecha_inicio    DATE NOT NULL DEFAULT CURRENT_DATE,
    fecha_fin       DATE,
    motivo          VARCHAR(200),
    registrado_por  UUID REFERENCES sgm.usuarios(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 2.4 CONCEPTOS DE COBRO
CREATE TABLE IF NOT EXISTS sgm.conceptos_cobro (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre          VARCHAR(100) NOT NULL,
    descripcion     VARCHAR(300),
    monto_default   DECIMAL(10,2) NOT NULL,
    es_recurrente   BOOLEAN NOT NULL DEFAULT TRUE,
    dia_emision     SMALLINT DEFAULT 1,
    activo          BOOLEAN NOT NULL DEFAULT TRUE,
    metadata        JSONB DEFAULT '{}',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_conceptos_nombre UNIQUE (nombre),
    CONSTRAINT ck_conceptos_monto  CHECK (monto_default >= 0),
    CONSTRAINT ck_conceptos_dia    CHECK (dia_emision BETWEEN 1 AND 28)
);

-- 2.5 TARIFAS POR PUESTO
CREATE TABLE IF NOT EXISTS sgm.tarifas_puesto (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    puesto_id       UUID NOT NULL REFERENCES sgm.puestos(id) ON DELETE CASCADE,
    concepto_id     UUID NOT NULL REFERENCES sgm.conceptos_cobro(id) ON DELETE CASCADE,
    monto           DECIMAL(10,2) NOT NULL,
    vigente_desde   DATE NOT NULL DEFAULT CURRENT_DATE,
    vigente_hasta   DATE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_tarifas_monto  CHECK (monto >= 0),
    CONSTRAINT uq_tarifas_puesto_concepto_vigente UNIQUE (puesto_id, concepto_id, vigente_desde)
);

-- 2.6 DEUDAS
CREATE TABLE IF NOT EXISTS sgm.deudas (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    puesto_id         UUID NOT NULL REFERENCES sgm.puestos(id) ON DELETE RESTRICT,
    concepto_id       UUID NOT NULL REFERENCES sgm.conceptos_cobro(id) ON DELETE RESTRICT,
    monto             DECIMAL(10,2) NOT NULL,
    periodo           VARCHAR(7) NOT NULL,
    fecha_emision     DATE NOT NULL DEFAULT CURRENT_DATE,
    fecha_vencimiento DATE,
    estado            sgm.estado_deuda NOT NULL DEFAULT 'pendiente',
    lote_carga_id     UUID,
    generado_por      UUID REFERENCES sgm.usuarios(id),
    anulado_por       UUID REFERENCES sgm.usuarios(id),
    motivo_anulacion  VARCHAR(300),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_deudas_monto    CHECK (monto > 0),
    CONSTRAINT ck_deudas_periodo  CHECK (periodo ~ '^\d{4}-(0[1-9]|1[0-2])$'),
    CONSTRAINT uq_deudas_puesto_concepto_periodo UNIQUE (puesto_id, concepto_id, periodo)
);

-- 2.7 PAGOS
CREATE TABLE IF NOT EXISTS sgm.pagos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    deuda_id        UUID NOT NULL REFERENCES sgm.deudas(id) ON DELETE RESTRICT,
    monto_pagado    DECIMAL(10,2) NOT NULL,
    fecha_pago      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    cajero_id       UUID NOT NULL REFERENCES sgm.usuarios(id) ON DELETE RESTRICT,
    nro_comprobante VARCHAR(30) NOT NULL,
    metodo_pago     sgm.metodo_pago NOT NULL DEFAULT 'efectivo',
    estado          sgm.estado_pago NOT NULL DEFAULT 'activo',
    referencia_pago VARCHAR(100),
    observaciones   TEXT,
    anulado_por     UUID REFERENCES sgm.usuarios(id),
    motivo_anulacion VARCHAR(300),
    fecha_anulacion TIMESTAMPTZ,
    comprobante_url VARCHAR(500),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_pagos_comprobante UNIQUE (nro_comprobante),
    CONSTRAINT uq_pagos_deuda       UNIQUE (deuda_id),
    CONSTRAINT ck_pagos_monto       CHECK (monto_pagado > 0)
);


-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 3: TABLAS JSONB (NoSQL)             │
-- └─────────────────────────────────────────────┘

CREATE TABLE IF NOT EXISTS sgm.configuracion (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clave           VARCHAR(50) NOT NULL,
    categoria       VARCHAR(50) NOT NULL DEFAULT 'general',
    valor           JSONB NOT NULL DEFAULT '{}',
    descripcion     VARCHAR(300),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by      UUID REFERENCES sgm.usuarios(id),

    CONSTRAINT uq_configuracion_clave UNIQUE (clave)
);

CREATE TABLE IF NOT EXISTS sgm.audit_logs (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    accion          VARCHAR(50) NOT NULL,
    tabla_afectada  VARCHAR(50),
    registro_id     UUID,
    usuario_id      UUID,
    usuario_nombre  VARCHAR(200),
    ip_address      INET,
    user_agent      VARCHAR(500),
    detalle         JSONB DEFAULT '{}',
    datos_anteriores JSONB,
    datos_nuevos    JSONB,
    timestamp       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sgm.notificaciones (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tipo            VARCHAR(30) NOT NULL,
    destinatario_id UUID REFERENCES sgm.usuarios(id),
    titulo          VARCHAR(200) NOT NULL,
    mensaje         TEXT NOT NULL,
    canal           VARCHAR(20) NOT NULL DEFAULT 'sistema',
    leida           BOOLEAN NOT NULL DEFAULT FALSE,
    datos           JSONB DEFAULT '{}',
    enviada_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);


-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 4: ÍNDICES                          │
-- └─────────────────────────────────────────────┘

CREATE INDEX IF NOT EXISTS idx_usuarios_rol ON sgm.usuarios(rol);
CREATE INDEX IF NOT EXISTS idx_usuarios_activo ON sgm.usuarios(activo) WHERE activo = TRUE;

CREATE INDEX IF NOT EXISTS idx_puestos_dueno ON sgm.puestos(dueno_id);
CREATE INDEX IF NOT EXISTS idx_puestos_estado ON sgm.puestos(estado);

CREATE INDEX IF NOT EXISTS idx_historial_puesto ON sgm.historial_duenos(puesto_id);
CREATE INDEX IF NOT EXISTS idx_historial_activo ON sgm.historial_duenos(puesto_id) WHERE fecha_fin IS NULL;

CREATE INDEX IF NOT EXISTS idx_tarifas_puesto ON sgm.tarifas_puesto(puesto_id);

CREATE INDEX IF NOT EXISTS idx_deudas_puesto ON sgm.deudas(puesto_id);
CREATE INDEX IF NOT EXISTS idx_deudas_concepto ON sgm.deudas(concepto_id);
CREATE INDEX IF NOT EXISTS idx_deudas_estado ON sgm.deudas(estado);
CREATE INDEX IF NOT EXISTS idx_deudas_periodo ON sgm.deudas(periodo);
CREATE INDEX IF NOT EXISTS idx_deudas_pendientes ON sgm.deudas(puesto_id, estado) WHERE estado = 'pendiente';
CREATE INDEX IF NOT EXISTS idx_deudas_vencidas ON sgm.deudas(fecha_vencimiento, estado)
    WHERE estado = 'pendiente' AND fecha_vencimiento IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_pagos_cajero ON sgm.pagos(cajero_id);
CREATE INDEX IF NOT EXISTS idx_pagos_fecha ON sgm.pagos(fecha_pago);
CREATE INDEX IF NOT EXISTS idx_pagos_fecha_activo ON sgm.pagos(fecha_pago, estado) WHERE estado = 'activo';

CREATE INDEX IF NOT EXISTS idx_audit_timestamp ON sgm.audit_logs(timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_accion ON sgm.audit_logs(accion);

CREATE INDEX IF NOT EXISTS idx_notif_destinatario ON sgm.notificaciones(destinatario_id, leida) WHERE leida = FALSE;

-- Índices GIN para JSONB
CREATE INDEX IF NOT EXISTS idx_configuracion_valor ON sgm.configuracion USING GIN (valor);
CREATE INDEX IF NOT EXISTS idx_audit_detalle ON sgm.audit_logs USING GIN (detalle);


-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 5: FUNCIONES Y TRIGGERS             │
-- └─────────────────────────────────────────────┘

-- 5.1 Auto-actualizar updated_at
CREATE OR REPLACE FUNCTION sgm.fn_update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_usuarios_updated ON sgm.usuarios;
CREATE TRIGGER trg_usuarios_updated BEFORE UPDATE ON sgm.usuarios
    FOR EACH ROW EXECUTE FUNCTION sgm.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_puestos_updated ON sgm.puestos;
CREATE TRIGGER trg_puestos_updated BEFORE UPDATE ON sgm.puestos
    FOR EACH ROW EXECUTE FUNCTION sgm.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_conceptos_updated ON sgm.conceptos_cobro;
CREATE TRIGGER trg_conceptos_updated BEFORE UPDATE ON sgm.conceptos_cobro
    FOR EACH ROW EXECUTE FUNCTION sgm.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_deudas_updated ON sgm.deudas;
CREATE TRIGGER trg_deudas_updated BEFORE UPDATE ON sgm.deudas
    FOR EACH ROW EXECUTE FUNCTION sgm.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_pagos_updated ON sgm.pagos;
CREATE TRIGGER trg_pagos_updated BEFORE UPDATE ON sgm.pagos
    FOR EACH ROW EXECUTE FUNCTION sgm.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_config_updated ON sgm.configuracion;
CREATE TRIGGER trg_config_updated BEFORE UPDATE ON sgm.configuracion
    FOR EACH ROW EXECUTE FUNCTION sgm.fn_update_timestamp();


-- 5.2 Secuencia para comprobantes
CREATE SEQUENCE IF NOT EXISTS sgm.seq_comprobante START 1;

CREATE OR REPLACE FUNCTION sgm.fn_generar_nro_comprobante()
RETURNS VARCHAR(30) AS $$
DECLARE
    prefijo VARCHAR(2) := 'C-';
    anio VARCHAR(4) := EXTRACT(YEAR FROM NOW())::VARCHAR;
    correlativo INTEGER;
BEGIN
    correlativo := nextval('sgm.seq_comprobante');
    RETURN prefijo || anio || '-' || LPAD(correlativo::VARCHAR, 6, '0');
END;
$$ LANGUAGE plpgsql;


-- 5.3 Validar pago antes de insertar
CREATE OR REPLACE FUNCTION sgm.fn_validar_pago()
RETURNS TRIGGER AS $$
DECLARE
    v_estado sgm.estado_deuda;
    v_monto DECIMAL(10,2);
BEGIN
    SELECT estado, monto INTO v_estado, v_monto
    FROM sgm.deudas WHERE id = NEW.deuda_id;

    IF v_estado IS NULL THEN
        RAISE EXCEPTION 'La deuda con ID % no existe', NEW.deuda_id;
    END IF;

    IF v_estado != 'pendiente' THEN
        RAISE EXCEPTION 'La deuda ya fue % y no puede pagarse', v_estado;
    END IF;

    IF NEW.monto_pagado != v_monto THEN
        RAISE EXCEPTION 'Monto pagado (%) no coincide con deuda (%). No se permiten pagos parciales', NEW.monto_pagado, v_monto;
    END IF;

    IF NEW.nro_comprobante IS NULL OR NEW.nro_comprobante = '' THEN
        NEW.nro_comprobante := sgm.fn_generar_nro_comprobante();
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_validar_pago ON sgm.pagos;
CREATE TRIGGER trg_validar_pago BEFORE INSERT ON sgm.pagos
    FOR EACH ROW EXECUTE FUNCTION sgm.fn_validar_pago();


-- 5.4 Cambiar estado deuda automáticamente
CREATE OR REPLACE FUNCTION sgm.fn_actualizar_estado_deuda()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE sgm.deudas SET estado = 'pagada' WHERE id = NEW.deuda_id;
    ELSIF TG_OP = 'UPDATE' AND NEW.estado = 'anulado' AND OLD.estado = 'activo' THEN
        UPDATE sgm.deudas SET estado = 'pendiente' WHERE id = NEW.deuda_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_estado_deuda ON sgm.pagos;
CREATE TRIGGER trg_estado_deuda AFTER INSERT OR UPDATE ON sgm.pagos
    FOR EACH ROW EXECUTE FUNCTION sgm.fn_actualizar_estado_deuda();


-- 5.5 Historial de cambio de dueño
CREATE OR REPLACE FUNCTION sgm.fn_registrar_cambio_dueno()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.dueno_id IS DISTINCT FROM NEW.dueno_id THEN
        UPDATE sgm.historial_duenos
        SET fecha_fin = CURRENT_DATE
        WHERE puesto_id = NEW.id AND fecha_fin IS NULL;

        IF NEW.dueno_id IS NOT NULL THEN
            INSERT INTO sgm.historial_duenos (puesto_id, dueno_id, motivo)
            VALUES (NEW.id, NEW.dueno_id, 'transferencia');
        END IF;

        NEW.fecha_asignacion = CASE WHEN NEW.dueno_id IS NOT NULL THEN CURRENT_DATE ELSE NULL END;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_cambio_dueno ON sgm.puestos;
CREATE TRIGGER trg_cambio_dueno BEFORE UPDATE ON sgm.puestos
    FOR EACH ROW EXECUTE FUNCTION sgm.fn_registrar_cambio_dueno();


-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 6: VISTAS PARA REPORTES             │
-- └─────────────────────────────────────────────┘

CREATE OR REPLACE VIEW sgm.vw_deudas_pendientes AS
SELECT
    d.id AS deuda_id,
    p.codigo AS puesto_codigo,
    p.descripcion AS puesto_descripcion,
    u.nombre_completo AS dueno_nombre,
    c.nombre AS concepto,
    d.monto,
    d.periodo,
    d.fecha_emision,
    d.fecha_vencimiento,
    CASE
        WHEN d.fecha_vencimiento IS NOT NULL AND d.fecha_vencimiento < CURRENT_DATE
        THEN CURRENT_DATE - d.fecha_vencimiento
        ELSE 0
    END AS dias_mora
FROM sgm.deudas d
JOIN sgm.puestos p ON d.puesto_id = p.id
LEFT JOIN sgm.usuarios u ON p.dueno_id = u.id
JOIN sgm.conceptos_cobro c ON d.concepto_id = c.id
WHERE d.estado = 'pendiente';


CREATE OR REPLACE VIEW sgm.vw_caja_diaria AS
SELECT
    DATE(pg.fecha_pago) AS fecha,
    pg.nro_comprobante,
    p.codigo AS puesto_codigo,
    u_dueno.nombre_completo AS dueno_nombre,
    c.nombre AS concepto,
    d.periodo,
    pg.monto_pagado,
    pg.metodo_pago::TEXT AS metodo,
    u_cajero.nombre_completo AS cajero_nombre,
    pg.fecha_pago AS hora_pago
FROM sgm.pagos pg
JOIN sgm.deudas d ON pg.deuda_id = d.id
JOIN sgm.puestos p ON d.puesto_id = p.id
LEFT JOIN sgm.usuarios u_dueno ON p.dueno_id = u_dueno.id
JOIN sgm.conceptos_cobro c ON d.concepto_id = c.id
JOIN sgm.usuarios u_cajero ON pg.cajero_id = u_cajero.id
WHERE pg.estado = 'activo';


CREATE OR REPLACE VIEW sgm.vw_morosidad AS
SELECT
    p.codigo AS puesto_codigo,
    u.nombre_completo AS dueno_nombre,
    u.telefono AS dueno_telefono,
    COUNT(*) AS cantidad_deudas_vencidas,
    SUM(d.monto) AS monto_total_adeudado,
    MIN(d.fecha_vencimiento) AS deuda_mas_antigua,
    MAX(CURRENT_DATE - d.fecha_vencimiento) AS dias_mayor_atraso
FROM sgm.deudas d
JOIN sgm.puestos p ON d.puesto_id = p.id
LEFT JOIN sgm.usuarios u ON p.dueno_id = u.id
WHERE d.estado = 'pendiente'
  AND d.fecha_vencimiento IS NOT NULL
  AND d.fecha_vencimiento < CURRENT_DATE
GROUP BY p.codigo, u.nombre_completo, u.telefono
ORDER BY dias_mayor_atraso DESC;


CREATE OR REPLACE VIEW sgm.vw_resumen_puesto AS
SELECT
    p.id AS puesto_id,
    p.codigo,
    p.descripcion,
    p.estado::TEXT AS estado,
    p.ubicacion,
    u.nombre_completo AS dueno_nombre,
    COALESCE(dp.total, 0) AS deuda_pendiente_total,
    COALESCE(dp.cantidad, 0) AS deudas_pendientes_count
FROM sgm.puestos p
LEFT JOIN sgm.usuarios u ON p.dueno_id = u.id
LEFT JOIN LATERAL (
    SELECT SUM(monto) AS total, COUNT(*) AS cantidad
    FROM sgm.deudas WHERE puesto_id = p.id AND estado = 'pendiente'
) dp ON TRUE;


-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 7: FUNCIONES DE NEGOCIO             │
-- └─────────────────────────────────────────────┘

-- Carga masiva de deudas
CREATE OR REPLACE FUNCTION sgm.fn_carga_masiva_deudas(
    p_concepto_id UUID,
    p_periodo VARCHAR(7),
    p_monto DECIMAL(10,2),
    p_fecha_vencimiento DATE DEFAULT NULL,
    p_generado_por UUID DEFAULT NULL,
    p_incluir_sin_dueno BOOLEAN DEFAULT TRUE
)
RETURNS TABLE(puestos_afectados INTEGER, monto_total DECIMAL, lote_id UUID)
AS $$
DECLARE
    v_lote_id UUID := gen_random_uuid();
    v_count INTEGER;
BEGIN
    INSERT INTO sgm.deudas (puesto_id, concepto_id, monto, periodo, fecha_vencimiento, lote_carga_id, generado_por)
    SELECT
        p.id,
        p_concepto_id,
        COALESCE(tp.monto, p_monto),
        p_periodo,
        p_fecha_vencimiento,
        v_lote_id,
        p_generado_por
    FROM sgm.puestos p
    LEFT JOIN sgm.tarifas_puesto tp
        ON tp.puesto_id = p.id
        AND tp.concepto_id = p_concepto_id
        AND (tp.vigente_hasta IS NULL OR tp.vigente_hasta >= CURRENT_DATE)
    WHERE p.estado = 'activo'
      AND (p_incluir_sin_dueno OR p.dueno_id IS NOT NULL)
      AND NOT EXISTS (
          SELECT 1 FROM sgm.deudas d
          WHERE d.puesto_id = p.id
            AND d.concepto_id = p_concepto_id
            AND d.periodo = p_periodo
      );

    GET DIAGNOSTICS v_count = ROW_COUNT;

    RETURN QUERY SELECT
        v_count,
        (SELECT COALESCE(SUM(d.monto), 0) FROM sgm.deudas d WHERE d.lote_carga_id = v_lote_id),
        v_lote_id;
END;
$$ LANGUAGE plpgsql;


-- Resumen de caja
CREATE OR REPLACE FUNCTION sgm.fn_resumen_caja(p_fecha DATE DEFAULT CURRENT_DATE)
RETURNS TABLE(
    total_cobrado DECIMAL,
    total_operaciones BIGINT,
    total_efectivo DECIMAL,
    total_transferencia DECIMAL
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        COALESCE(SUM(pg.monto_pagado), 0),
        COUNT(*),
        COALESCE(SUM(pg.monto_pagado) FILTER (WHERE pg.metodo_pago = 'efectivo'), 0),
        COALESCE(SUM(pg.monto_pagado) FILTER (WHERE pg.metodo_pago = 'transferencia'), 0)
    FROM sgm.pagos pg
    WHERE DATE(pg.fecha_pago) = p_fecha
      AND pg.estado = 'activo';
END;
$$ LANGUAGE plpgsql;


-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 8: DATOS DE PRUEBA                  │
-- └─────────────────────────────────────────────┘

-- Configuración del mercado
INSERT INTO sgm.configuracion (clave, categoria, valor, descripcion) VALUES
('datos_mercado', 'general', '{
    "nombre": "Mercado Central de Chincha",
    "ruc": "20123456789",
    "direccion": "Av. Principal 123, Chincha, Ica",
    "telefono": "056-261234"
}', 'Datos generales del mercado'),
('politicas_cobro', 'negocio', '{
    "permitir_pagos_parciales": false,
    "dias_gracia": 5,
    "recargo_mora_porcentaje": 0
}', 'Políticas de cobro')
ON CONFLICT (clave) DO NOTHING;

-- Usuarios
INSERT INTO sgm.usuarios (id, nombre_completo, email, telefono, dni, rol) VALUES
('a0000000-0000-0000-0000-000000000001', 'Roberto Mendoza Pérez', 'admin@mercado.com', '956111222', '12345678', 'admin'),
('a0000000-0000-0000-0000-000000000002', 'Carlos Ruiz Díaz', 'cajero1@mercado.com', '956333444', '23456789', 'cajero'),
('a0000000-0000-0000-0000-000000000003', 'Ana Díaz Torres', 'cajera2@mercado.com', '956555666', '34567890', 'cajero'),
('a0000000-0000-0000-0000-000000000004', 'María García López', 'maria.garcia@email.com', '956777888', '45678912', 'dueno'),
('a0000000-0000-0000-0000-000000000005', 'Juan López Medina', 'juan.lopez@email.com', '956999000', '56789123', 'dueno'),
('a0000000-0000-0000-0000-000000000006', 'Rosa Torres Vega', 'rosa.torres@email.com', '957111222', '67891234', 'dueno'),
('a0000000-0000-0000-0000-000000000007', 'Pedro Sánchez Ruiz', 'pedro.sanchez@email.com', '957333444', '78912345', 'dueno'),
('a0000000-0000-0000-0000-000000000008', 'Lucía Vargas Flores', 'lucia.vargas@email.com', '957555666', '89123456', 'dueno')
ON CONFLICT (email) DO NOTHING;

-- Puestos
INSERT INTO sgm.puestos (codigo, descripcion, ubicacion, area_m2, dueno_id, estado) VALUES
('P-001', 'Verduras y hortalizas', 'Pasillo A, Local 1', 12.5, 'a0000000-0000-0000-0000-000000000004', 'activo'),
('P-002', 'Frutas tropicales', 'Pasillo A, Local 2', 10.0, 'a0000000-0000-0000-0000-000000000005', 'activo'),
('P-003', 'Abarrotes generales', 'Pasillo B, Local 1', 15.0, NULL, 'activo'),
('P-004', 'Carnicería El Buen Corte', 'Pasillo B, Local 2', 18.0, 'a0000000-0000-0000-0000-000000000006', 'activo'),
('P-005', 'Pollos y aves', 'Pasillo C, Local 1', 14.0, NULL, 'en_mantenimiento'),
('P-006', 'Pescados y mariscos', 'Pasillo C, Local 2', 16.0, 'a0000000-0000-0000-0000-000000000007', 'activo'),
('P-007', 'Especias y condimentos', 'Pasillo D, Local 1', 8.0, 'a0000000-0000-0000-0000-000000000004', 'activo'),
('P-008', 'Panadería artesanal', 'Pasillo D, Local 2', 20.0, 'a0000000-0000-0000-0000-000000000008', 'activo'),
('P-009', 'Jugos y bebidas', 'Pasillo E, Local 1', 9.0, 'a0000000-0000-0000-0000-000000000005', 'activo'),
('P-010', 'Ropa y textiles', 'Pasillo E, Local 2', 22.0, NULL, 'activo')
ON CONFLICT (codigo) DO NOTHING;

-- Conceptos de cobro
INSERT INTO sgm.conceptos_cobro (id, nombre, descripcion, monto_default, es_recurrente, dia_emision) VALUES
('b0000000-0000-0000-0000-000000000001', 'Luz / Electricidad', 'Servicio eléctrico del mercado', 85.00, TRUE, 1),
('b0000000-0000-0000-0000-000000000002', 'Limpieza', 'Servicio de limpieza de áreas comunes', 45.00, TRUE, 1),
('b0000000-0000-0000-0000-000000000003', 'Vigilancia / Seguridad', 'Servicio de vigilancia 24 horas', 60.00, TRUE, 1)
ON CONFLICT (nombre) DO NOTHING;

-- Tarifas especiales
INSERT INTO sgm.tarifas_puesto (puesto_id, concepto_id, monto)
SELECT p.id, 'b0000000-0000-0000-0000-000000000001', 120.00
FROM sgm.puestos p WHERE p.codigo = 'P-004'
ON CONFLICT DO NOTHING;

INSERT INTO sgm.tarifas_puesto (puesto_id, concepto_id, monto)
SELECT p.id, 'b0000000-0000-0000-0000-000000000001', 110.00
FROM sgm.puestos p WHERE p.codigo = 'P-008'
ON CONFLICT DO NOTHING;

-- Cargar deudas de Enero 2026 usando la función masiva
SELECT * FROM sgm.fn_carga_masiva_deudas(
    'b0000000-0000-0000-0000-000000000001', '2026-01', 85.00, '2026-01-31',
    'a0000000-0000-0000-0000-000000000001', TRUE
);
SELECT * FROM sgm.fn_carga_masiva_deudas(
    'b0000000-0000-0000-0000-000000000002', '2026-01', 45.00, '2026-01-31',
    'a0000000-0000-0000-0000-000000000001', TRUE
);
SELECT * FROM sgm.fn_carga_masiva_deudas(
    'b0000000-0000-0000-0000-000000000003', '2026-01', 60.00, '2026-01-31',
    'a0000000-0000-0000-0000-000000000001', TRUE
);

-- Cargar deudas de Febrero 2026
SELECT * FROM sgm.fn_carga_masiva_deudas(
    'b0000000-0000-0000-0000-000000000001', '2026-02', 85.00, '2026-02-28',
    'a0000000-0000-0000-0000-000000000001', TRUE
);

-- Pagos de prueba (los triggers auto-generan comprobante y cambian estado)
INSERT INTO sgm.pagos (deuda_id, monto_pagado, cajero_id, nro_comprobante, metodo_pago) VALUES
((SELECT id FROM sgm.deudas WHERE puesto_id = (SELECT id FROM sgm.puestos WHERE codigo = 'P-001')
  AND concepto_id = 'b0000000-0000-0000-0000-000000000001' AND periodo = '2026-01'),
 85.00, 'a0000000-0000-0000-0000-000000000002', '', 'efectivo');

INSERT INTO sgm.pagos (deuda_id, monto_pagado, cajero_id, nro_comprobante, metodo_pago) VALUES
((SELECT id FROM sgm.deudas WHERE puesto_id = (SELECT id FROM sgm.puestos WHERE codigo = 'P-001')
  AND concepto_id = 'b0000000-0000-0000-0000-000000000002' AND periodo = '2026-01'),
 45.00, 'a0000000-0000-0000-0000-000000000002', '', 'efectivo');

INSERT INTO sgm.pagos (deuda_id, monto_pagado, cajero_id, nro_comprobante, metodo_pago, referencia_pago) VALUES
((SELECT id FROM sgm.deudas WHERE puesto_id = (SELECT id FROM sgm.puestos WHERE codigo = 'P-002')
  AND concepto_id = 'b0000000-0000-0000-0000-000000000001' AND periodo = '2026-01'),
 85.00, 'a0000000-0000-0000-0000-000000000003', '', 'transferencia', 'TR-2026-00145');

-- Audit log inicial
INSERT INTO sgm.audit_logs (accion, usuario_id, usuario_nombre, detalle) VALUES
('SISTEMA_INICIALIZADO', 'a0000000-0000-0000-0000-000000000001', 'Roberto Mendoza Pérez',
 '{"version": "1.0", "puestos": 10, "usuarios": 8, "conceptos": 3}');


-- ┌─────────────────────────────────────────────┐
-- │  SECCIÓN 9: VERIFICACIÓN FINAL               │
-- │  Ejecuta estas queries para confirmar que    │
-- │  todo se creó correctamente.                 │
-- └─────────────────────────────────────────────┘

-- Ver resumen de puestos
SELECT * FROM sgm.vw_resumen_puesto ORDER BY codigo;

-- Ver deudas pendientes (enero vencido = morosos)
SELECT puesto_codigo, concepto, monto, periodo, dias_mora
FROM sgm.vw_deudas_pendientes
ORDER BY puesto_codigo, concepto;

-- Ver pagos registrados (comprobantes auto-generados)
SELECT nro_comprobante, monto_pagado, metodo_pago::TEXT, estado::TEXT
FROM sgm.pagos ORDER BY created_at;

-- Resumen de caja del día
SELECT * FROM sgm.fn_resumen_caja(CURRENT_DATE);

-- Contar registros
SELECT 'usuarios' AS tabla, COUNT(*) AS registros FROM sgm.usuarios
UNION ALL SELECT 'puestos', COUNT(*) FROM sgm.puestos
UNION ALL SELECT 'conceptos', COUNT(*) FROM sgm.conceptos_cobro
UNION ALL SELECT 'deudas', COUNT(*) FROM sgm.deudas
UNION ALL SELECT 'pagos', COUNT(*) FROM sgm.pagos
UNION ALL SELECT 'config', COUNT(*) FROM sgm.configuracion
ORDER BY tabla;
