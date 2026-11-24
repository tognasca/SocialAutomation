ALTER TABLE campaigns ADD COLUMN proposal_plans jsonb NOT NULL DEFAULT '[]';

CREATE TABLE campaign_proposals (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    campaign_id uuid NOT NULL REFERENCES campaigns(id) ON DELETE CASCADE,
    lead_id uuid NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    company_name text NOT NULL,
    plan_name text NOT NULL,
    price numeric(12,2) NOT NULL CHECK (price > 0),
    terms text NOT NULL,
    created_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    accepted_at timestamptz,
    UNIQUE (campaign_id, lead_id)
);
