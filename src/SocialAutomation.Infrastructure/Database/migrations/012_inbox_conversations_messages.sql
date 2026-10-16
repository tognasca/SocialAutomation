CREATE TABLE IF NOT EXISTS conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    lead_id UUID NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    instagram_account_id UUID REFERENCES instagram_accounts(id) ON DELETE SET NULL,
    assigned_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    mode VARCHAR(30) NOT NULL DEFAULT 'HUMANO', -- 'IA_ATIVA', 'IA_PAUSADA', 'HUMANO'
    status VARCHAR(30) NOT NULL DEFAULT 'Aberto', -- 'Aberto', 'Fechado', 'Aguardando'
    unread_count INT NOT NULL DEFAULT 0,
    last_message_preview VARCHAR(500),
    last_message_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_tenant_lead_conversation UNIQUE (tenant_id, lead_id)
);

CREATE INDEX IF NOT EXISTS idx_conversations_tenant_id ON conversations(tenant_id);
CREATE INDEX IF NOT EXISTS idx_conversations_assigned_user ON conversations(assigned_user_id);
CREATE INDEX IF NOT EXISTS idx_conversations_status ON conversations(status);
CREATE INDEX IF NOT EXISTS idx_conversations_last_message ON conversations(last_message_at DESC);

CREATE TABLE IF NOT EXISTS messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    conversation_id UUID NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    sender_type VARCHAR(20) NOT NULL, -- 'Lead', 'User', 'AI', 'System'
    sender_id UUID, -- NULL se for Lead (já tem a conversation) ou IA
    instagram_message_id VARCHAR(150),
    content TEXT NOT NULL,
    media_url VARCHAR(1000),
    status VARCHAR(20) NOT NULL DEFAULT 'Sent', -- 'Sent', 'Delivered', 'Read', 'Failed'
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_messages_conversation_id ON messages(conversation_id);
CREATE INDEX IF NOT EXISTS idx_messages_created_at ON messages(created_at);
CREATE INDEX IF NOT EXISTS idx_messages_ig_msg_id ON messages(instagram_message_id);