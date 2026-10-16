
CREATE TABLE IF NOT EXISTS instagram_webhook_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    instagram_account_id UUID REFERENCES instagram_accounts(id) ON DELETE SET NULL,
    event_id VARCHAR(150) NOT NULL UNIQUE,
    object_type VARCHAR(50) NOT NULL,
    entry_id VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    status SMALLINT NOT NULL DEFAULT 1, -- 1: Received/Pending, 2: Processing, 3: Processed, 4: Failed, 5: Ignored
    error_message TEXT,
    processed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_webhook_events_tenant_id ON instagram_webhook_events(tenant_id);
CREATE INDEX IF NOT EXISTS idx_webhook_events_event_id ON instagram_webhook_events(event_id);
CREATE INDEX IF NOT EXISTS idx_webhook_events_status ON instagram_webhook_events(status);
CREATE INDEX IF NOT EXISTS idx_webhook_events_created_at ON instagram_webhook_events(created_at);