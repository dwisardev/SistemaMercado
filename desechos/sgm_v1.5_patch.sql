-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  SGM — PATCH v1.5: Refresh Tokens                              ║
-- ║  Ejecutar en bases de datos existentes (v1.0 – v1.4)          ║
-- ║  Para entornos nuevos ya está incluido en                      ║
-- ║  sgm_postgresql17_local.sql (re-ejecutar es seguro, usa IF NOT EXISTS) ║
-- ╚══════════════════════════════════════════════════════════════════╝

CREATE TABLE IF NOT EXISTS sgm.refresh_tokens (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id  UUID        NOT NULL REFERENCES sgm.usuarios(id) ON DELETE CASCADE,
    token       VARCHAR(128) NOT NULL UNIQUE,
    expires_at  TIMESTAMPTZ NOT NULL,
    revocado    BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_usuario ON sgm.refresh_tokens(usuario_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token   ON sgm.refresh_tokens(token);
