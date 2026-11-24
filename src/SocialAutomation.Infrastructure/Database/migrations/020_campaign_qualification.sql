ALTER TABLE campaigns ADD COLUMN qualification_questions text[];

CREATE TABLE campaign_demo_slots (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    campaign_id uuid NOT NULL REFERENCES campaigns(id) ON DELETE CASCADE,
    starts_at timestamptz NOT NULL,
    booked_lead_id uuid REFERENCES leads(id) ON DELETE SET NULL,
    booked_at timestamptz,
    UNIQUE (tenant_id, starts_at)
);

CREATE TABLE campaign_qualifications (
    campaign_id uuid NOT NULL REFERENCES campaigns(id) ON DELETE CASCADE,
    lead_id uuid NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    questions text[] NOT NULL,
    answers text[] NOT NULL DEFAULT '{}',
    offered_slot_ids bigint[] NOT NULL DEFAULT '{}',
    booked_slot_id bigint REFERENCES campaign_demo_slots(id),
    PRIMARY KEY (campaign_id, lead_id)
);

CREATE INDEX idx_campaign_reply_lead ON instagram_comment_replies (instagram_account_id, lead_id, replied_at DESC) WHERE replied_at IS NOT NULL;
