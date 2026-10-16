-- Tabela Principal de Fluxos de Automação
CREATE TABLE IF NOT EXISTS automation_flows (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    trigger_type VARCHAR(100) NOT NULL, -- 'LeadCriado', 'MensagemRecebida', 'TagAdicionada', 'StatusAlterado', 'ScoreAlterado'
    trigger_config JSONB NOT NULL DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    execution_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_automation_flows_tenant_id ON automation_flows(tenant_id);
CREATE INDEX IF NOT EXISTS idx_automation_flows_trigger ON automation_flows(tenant_id, trigger_type, is_active);

-- Tabela de Nós (Passos) do Fluxo
CREATE TABLE IF NOT EXISTS automation_nodes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    flow_id UUID NOT NULL REFERENCES automation_flows(id) ON DELETE CASCADE,
    step_order INT NOT NULL DEFAULT 0,
    node_type VARCHAR(50) NOT NULL, -- 'Condition', 'Action', 'Delay'
    action_or_condition VARCHAR(100) NOT NULL, -- 'AdicionarTag', 'AlterarStatus', 'ScoreMaiorQue', 'AguardarTempo'
    config JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_automation_nodes_flow ON automation_nodes(flow_id, step_order);

-- Tabela de Execuções de Automação (Logs de Estado por Lead)
CREATE TABLE IF NOT EXISTS automation_executions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    flow_id UUID NOT NULL REFERENCES automation_flows(id) ON DELETE CASCADE,
    lead_id UUID NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    current_step INT NOT NULL DEFAULT 0,
    status VARCHAR(30) NOT NULL DEFAULT 'Running', -- 'Running', 'Completed', 'Failed', 'WaitingDelay'
    next_execution_at TIMESTAMPTZ,
    execution_log JSONB DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_automation_exec_tenant ON automation_executions(tenant_id);
CREATE INDEX IF NOT EXISTS idx_automation_exec_status ON automation_executions(status, next_execution_at);

-- Tabela de Campanhas Segmentadas
CREATE TABLE IF NOT EXISTS campaigns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    instagram_account_id UUID REFERENCES instagram_accounts(id) ON DELETE SET NULL,
    name VARCHAR(255) NOT NULL,
    message_template TEXT NOT NULL,
    segment_filter JSONB NOT NULL DEFAULT '{}', -- { tagIds: [], status: [], minScore: 0 }
    status VARCHAR(30) NOT NULL DEFAULT 'Rascunho', -- 'Rascunho', 'Agendada', 'EmExecucao', 'Concluida', 'Pausada'
    scheduled_at TIMESTAMPTZ,
    total_audience INT NOT NULL DEFAULT 0,
    sent_count INT NOT NULL DEFAULT 0,
    failed_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_campaigns_tenant ON campaigns(tenant_id);
CREATE INDEX IF NOT EXISTS idx_campaigns_status ON campaigns(status, scheduled_at);

-- Público Alvo / Execução da Campanha
CREATE TABLE IF NOT EXISTS campaign_recipients (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    campaign_id UUID NOT NULL REFERENCES campaigns(id) ON DELETE CASCADE,
    lead_id UUID NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    status VARCHAR(30) NOT NULL DEFAULT 'Pendente', -- 'Pendente', 'Enviado', 'Falhou', 'IgnoradoJanelaMeta'
    error_message TEXT,
    sent_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_campaign_recipients_camp ON campaign_recipients(campaign_id, status);