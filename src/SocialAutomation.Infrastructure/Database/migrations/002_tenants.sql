-- database/migrations/002_tenants.sql

CREATE TABLE IF NOT EXISTS tenants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(100) NOT NULL UNIQUE,
    document VARCHAR(20),
    email VARCHAR(255),
    phone VARCHAR(20),
    plan VARCHAR(50) NOT NULL DEFAULT 'free',
    status SMALLINT NOT NULL DEFAULT 1,
    settings JSONB DEFAULT '{}',
    max_users INT NOT NULL DEFAULT 5,
    max_instagram_accounts INT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_tenants_slug ON tenants(slug);
CREATE INDEX IF NOT EXISTS idx_tenants_status ON tenants(status);