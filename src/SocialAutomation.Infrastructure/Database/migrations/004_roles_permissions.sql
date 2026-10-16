-- database/migrations/004_roles_permissions.sql

CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID REFERENCES tenants(id),
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    is_system BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(500),
    module VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_user_roles UNIQUE (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS role_permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    CONSTRAINT uq_role_permissions UNIQUE (role_id, permission_id)
);

CREATE INDEX IF NOT EXISTS idx_roles_tenant_id ON roles(tenant_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_role_id ON user_roles(role_id);
CREATE INDEX IF NOT EXISTS idx_role_permissions_role_id ON role_permissions(role_id);
CREATE INDEX IF NOT EXISTS idx_permissions_code ON permissions(code);
CREATE INDEX IF NOT EXISTS idx_permissions_module ON permissions(module);

-- System roles
INSERT INTO roles (id, tenant_id, name, description, is_system) VALUES
    ('a0000000-0000-0000-0000-000000000001', NULL, 'SuperAdmin', 'Super administrador do sistema', TRUE),
    ('a0000000-0000-0000-0000-000000000002', NULL, 'Admin', 'Administrador do tenant', TRUE),
    ('a0000000-0000-0000-0000-000000000003', NULL, 'Manager', 'Gerente', TRUE),
    ('a0000000-0000-0000-0000-000000000004', NULL, 'Sales', 'Vendedor', TRUE),
    ('a0000000-0000-0000-0000-000000000005', NULL, 'Attendant', 'Atendente', TRUE),
    ('a0000000-0000-0000-0000-000000000006', NULL, 'Editor', 'Editor de conteúdo', TRUE)
ON CONFLICT (id) DO NOTHING;

-- Permissions
INSERT INTO permissions (code, name, description, module) VALUES
    ('leads.read', 'Visualizar leads', 'Permite visualizar leads', 'leads'),
    ('leads.create', 'Criar leads', 'Permite criar leads', 'leads'),
    ('leads.update', 'Atualizar leads', 'Permite atualizar leads', 'leads'),
    ('leads.delete', 'Excluir leads', 'Permite excluir leads', 'leads'),
    ('conversations.read', 'Visualizar conversas', 'Permite visualizar conversas', 'conversations'),
    ('conversations.reply', 'Responder conversas', 'Permite responder conversas', 'conversations'),
    ('campaigns.read', 'Visualizar campanhas', 'Permite visualizar campanhas', 'campaigns'),
    ('campaigns.create', 'Criar campanhas', 'Permite criar campanhas', 'campaigns'),
    ('campaigns.execute', 'Executar campanhas', 'Permite executar campanhas', 'campaigns'),
    ('content.read', 'Visualizar conteúdo', 'Permite visualizar conteúdo', 'content'),
    ('content.create', 'Criar conteúdo', 'Permite criar conteúdo', 'content'),
    ('content.publish', 'Publicar conteúdo', 'Permite publicar conteúdo', 'content'),
    ('instagram.connect', 'Conectar Instagram', 'Permite conectar conta Instagram', 'instagram'),
    ('instagram.manage', 'Gerenciar Instagram', 'Permite gerenciar conta Instagram', 'instagram'),
    ('analytics.read', 'Visualizar analytics', 'Permite visualizar analytics', 'analytics'),
    ('users.manage', 'Gerenciar usuários', 'Permite gerenciar usuários', 'users'),
    ('settings.manage', 'Gerenciar configurações', 'Permite gerenciar configurações', 'settings'),
    ('ai.manage', 'Gerenciar IA', 'Permite gerenciar configurações de IA', 'ai'),
    ('automations.read', 'Visualizar automações', 'Permite visualizar automações', 'automations'),
    ('automations.create', 'Criar automações', 'Permite criar automações', 'automations'),
    ('automations.execute', 'Executar automações', 'Permite executar automações', 'automations')
ON CONFLICT (code) DO NOTHING;

-- Admin and SuperAdmin get all permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'a0000000-0000-0000-0000-000000000001', id FROM permissions
ON CONFLICT (role_id, permission_id) DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT 'a0000000-0000-0000-0000-000000000002', id FROM permissions
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Manager permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'a0000000-0000-0000-0000-000000000003', id FROM permissions
WHERE code IN ('leads.read','leads.create','leads.update','conversations.read','conversations.reply',
               'campaigns.read','campaigns.create','content.read','content.create',
               'analytics.read','users.manage')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Sales permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'a0000000-0000-0000-0000-000000000004', id FROM permissions
WHERE code IN ('leads.read','leads.create','leads.update','conversations.read','conversations.reply',
               'campaigns.read','analytics.read')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Attendant permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'a0000000-0000-0000-0000-000000000005', id FROM permissions
WHERE code IN ('leads.read','conversations.read','conversations.reply')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Editor permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'a0000000-0000-0000-0000-000000000006', id FROM permissions
WHERE code IN ('content.read','content.create','content.publish','analytics.read')
ON CONFLICT (role_id, permission_id) DO NOTHING;