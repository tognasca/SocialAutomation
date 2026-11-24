ALTER TABLE campaigns ADD COLUMN instagram_media_id TEXT, ADD COLUMN comment_keyword TEXT;

CREATE UNIQUE INDEX uq_comment_campaign ON campaigns (instagram_account_id, instagram_media_id, comment_keyword)
    WHERE instagram_media_id IS NOT NULL AND status = 'Ativa';

CREATE TABLE instagram_comment_replies (
    instagram_account_id UUID NOT NULL REFERENCES instagram_accounts(id) ON DELETE CASCADE,
    comment_id TEXT NOT NULL,
    campaign_id UUID NOT NULL REFERENCES campaigns(id) ON DELETE CASCADE,
    status TEXT NOT NULL DEFAULT 'Processando',
    sent_message_id TEXT,
    recipient_id TEXT,
    replied_at TIMESTAMPTZ,
    lead_id UUID REFERENCES leads(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (instagram_account_id, comment_id)
);

CREATE INDEX idx_comment_replies_recipient ON instagram_comment_replies (instagram_account_id, recipient_id, created_at DESC);
