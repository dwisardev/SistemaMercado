-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  SGM — PATCH v1.6: JWT Blacklist persistente                   ║
-- ║  Ejecutar en bases de datos existentes (v1.0 – v1.5)          ║
-- ║  Para entornos nuevos ya está incluido en                      ║
-- ║  sgm_postgresql17_local.sql (re-ejecutar es seguro, usa IF NOT EXISTS) ║
-- ╚══════════════════════════════════════════════════════════════════╝

CREATE TABLE IF NOT EXISTS sgm.tokens_revocados (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    jti         VARCHAR(36) NOT NULL UNIQUE,
    expires_at  TIMESTAMPTZ NOT NULL,
    revoked_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_tokens_revocados_jti        ON sgm.tokens_revocados(jti);
CREATE INDEX IF NOT EXISTS idx_tokens_revocados_expires_at ON sgm.tokens_revocados(expires_at);
