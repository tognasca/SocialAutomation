CREATE TABLE IF NOT EXISTS instagram_accounts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    instagram_business_id VARCHAR(100) NOT NULL,
    instagram_user_id VARCHAR(100),
    page_id VARCHAR(100) NOT NULL,
    page_name VARCHAR(255) NOT NULL,
    username VARCHAR(150) NOT NULL,
    name VARCHAR(255),
    profile_picture_url VARCHAR(1000),
    access_token TEXT NOT NULL,
    token_expires_at TIMESTAMPTZ,
    status SMALLINT NOT NULL DEFAULT 1, -- 1: Active, 2: TokenExpired, 3: Disconnected, 4: Error
    webhook_subscribed BOOLEAN NOT NULL DEFAULT FALSE,
    scopes TEXT[],
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_tenant_instagram_business_id UNIQUE (tenant_id, instagram_business_id)
);

CREATE INDEX IF NOT EXISTS idx_instagram_accounts_tenant_id ON instagram_accounts(tenant_id);
CREATE INDEX IF NOT EXISTS idx_instagram_accounts_business_id ON instagram_accounts(instagram_business_id);
CREATE INDEX IF NOT EXISTS idx_instagram_accounts_page_id ON instagram_accounts(page_id);
CREATE INDEX IF NOT EXISTS idx_instagram_accounts_status ON instagram_accounts(status);