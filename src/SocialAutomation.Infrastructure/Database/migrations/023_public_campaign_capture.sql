ALTER TABLE campaigns ADD COLUMN public_capture_enabled boolean NOT NULL DEFAULT false,
    ADD COLUMN public_capture_title text,
    ADD COLUMN public_capture_intro text;

CREATE TABLE campaign_web_visits (
    campaign_id uuid NOT NULL REFERENCES campaigns(id) ON DELETE CASCADE,
    visitor_id uuid NOT NULL,
    channel varchar(80) NOT NULL,
    diagnosed_at timestamptz,
    answers text[],
    lead_id uuid REFERENCES leads(id) ON DELETE SET NULL,
    contact_consented_at timestamptz,
    booked_slot_id bigint REFERENCES campaign_demo_slots(id) ON DELETE SET NULL,
    PRIMARY KEY (campaign_id, visitor_id)
);

CREATE INDEX idx_campaign_web_visits_lead ON campaign_web_visits(campaign_id, lead_id);

ALTER TABLE campaign_proposals ADD COLUMN web_visitor_id uuid;
