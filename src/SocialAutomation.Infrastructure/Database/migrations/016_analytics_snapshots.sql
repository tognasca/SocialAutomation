CREATE TABLE IF NOT EXISTS analytics_daily_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    snapshot_date DATE NOT NULL,
    leads_created INT NOT NULL DEFAULT 0,
    leads_qualified INT NOT NULL DEFAULT 0,
    clients_won INT NOT NULL DEFAULT 0,
    conversations_started INT NOT NULL DEFAULT 0,
    messages_received INT NOT NULL DEFAULT 0,
    messages_sent INT NOT NULL DEFAULT 0,
    ai_replies INT NOT NULL DEFAULT 0,
    human_handoffs INT NOT NULL DEFAULT 0,
    campaigns_sent INT NOT NULL DEFAULT 0,
    content_published INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_analytics_tenant_date UNIQUE (tenant_id, snapshot_date)
);

CREATE INDEX IF NOT EXISTS idx_analytics_daily_tenant_date
    ON analytics_daily_snapshots(tenant_id, snapshot_date DESC);