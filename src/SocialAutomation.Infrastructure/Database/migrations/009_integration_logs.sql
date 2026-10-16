
CREATE TABLE IF NOT EXISTS integration_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    provider VARCHAR(50) NOT NULL, -- 'instagram', 'meta', 'openai', etc.
    operation VARCHAR(100) NOT NULL,
    endpoint VARCHAR(500) NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    status_code INT,
    request_payload JSONB,
    response_payload JSONB,
    success BOOLEAN NOT NULL DEFAULT TRUE,
    error_message TEXT,
    duration_ms BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_integration_logs_tenant_id ON integration_logs(tenant_id);
CREATE INDEX IF NOT EXISTS idx_integration_logs_provider ON integration_logs(provider);
CREATE INDEX IF NOT EXISTS idx_integration_logs_created_at ON integration_logs(created_at);