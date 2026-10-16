CREATE TABLE IF NOT EXISTS content_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    instagram_account_id UUID REFERENCES instagram_accounts(id) ON DELETE SET NULL,
    type VARCHAR(30) NOT NULL DEFAULT 'Post', -- 'Post', 'Story', 'Reel'
    title VARCHAR(255) NOT NULL,
    caption TEXT,
    hashtags TEXT[],
    cta VARCHAR(255),
    media_urls TEXT[],
    media_format VARCHAR(20) NOT NULL DEFAULT '1:1', -- '1:1', '4:5', '9:16', '16:9'
    status VARCHAR(30) NOT NULL DEFAULT 'Rascunho', -- 'Rascunho', 'Agendado', 'Processando', 'Publicado', 'Erro'
    scheduled_at TIMESTAMPTZ,
    published_at TIMESTAMPTZ,
    instagram_publication_id VARCHAR(100),
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_content_items_tenant_id ON content_items(tenant_id);
CREATE INDEX IF NOT EXISTS idx_content_items_status ON content_items(status, scheduled_at);
CREATE INDEX IF NOT EXISTS idx_content_items_type ON content_items(type);

-- Tabela de Cenas do Roteiro do Reel
CREATE TABLE IF NOT EXISTS reel_scenes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    content_item_id UUID NOT NULL REFERENCES content_items(id) ON DELETE CASCADE,
    scene_order INT NOT NULL DEFAULT 1,
    script_text TEXT NOT NULL,
    visual_description TEXT,
    duration_seconds INT NOT NULL DEFAULT 5,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_reel_scenes_content_id ON reel_scenes(content_item_id, scene_order);