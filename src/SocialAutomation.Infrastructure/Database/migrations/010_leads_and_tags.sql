CREATE TABLE IF NOT EXISTS leads (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    instagram_account_id UUID REFERENCES instagram_accounts(id) ON DELETE SET NULL,
    name VARCHAR(255) NOT NULL,
    instagram_username VARCHAR(150),
    instagram_user_id VARCHAR(100),
    profile_image VARCHAR(1000),
    phone VARCHAR(30),
    email VARCHAR(255),
    company VARCHAR(255),
    source VARCHAR(100) NOT NULL DEFAULT 'instagram_dm',
    status VARCHAR(50) NOT NULL DEFAULT 'Novo',
    pipeline_stage VARCHAR(50) NOT NULL DEFAULT 'Novo',
    pipeline_order INT NOT NULL DEFAULT 0,
    deal_value NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    owner_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    score INT NOT NULL DEFAULT 0,
    score_classification VARCHAR(30) NOT NULL DEFAULT 'Frio',
    notes TEXT,
    last_interaction_at TIMESTAMPTZ,
    next_follow_up_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_leads_tenant_id ON leads(tenant_id);
CREATE INDEX IF NOT EXISTS idx_leads_instagram_username ON leads(instagram_username);
CREATE INDEX IF NOT EXISTS idx_leads_status ON leads(status);
CREATE INDEX IF NOT EXISTS idx_leads_pipeline_stage ON leads(pipeline_stage);
CREATE INDEX IF NOT EXISTS idx_leads_owner_user_id ON leads(owner_user_id);
CREATE INDEX IF NOT EXISTS idx_leads_score ON leads(score);

CREATE TABLE IF NOT EXISTS tags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    color VARCHAR(20) NOT NULL DEFAULT '#6366F1',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_tenant_tag_name UNIQUE (tenant_id, name)
);

CREATE INDEX IF NOT EXISTS idx_tags_tenant_id ON tags(tenant_id);

CREATE TABLE IF NOT EXISTS lead_tags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lead_id UUID NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_lead_tag UNIQUE (lead_id, tag_id)
);

CREATE INDEX IF NOT EXISTS idx_lead_tags_lead_id ON lead_tags(lead_id);
CREATE INDEX IF NOT EXISTS idx_lead_tags_tag_id ON lead_tags(tag_id);