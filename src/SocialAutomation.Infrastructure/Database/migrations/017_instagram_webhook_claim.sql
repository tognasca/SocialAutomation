ALTER TABLE instagram_webhook_events
    ADD COLUMN IF NOT EXISTS processing_started_at TIMESTAMPTZ;

ALTER TABLE instagram_accounts
    ADD COLUMN IF NOT EXISTS user_access_token TEXT;

CREATE TABLE IF NOT EXISTS instagram_message_receipts (
    account_id UUID NOT NULL REFERENCES instagram_accounts(id) ON DELETE CASCADE,
    message_id VARCHAR(150) NOT NULL,
    PRIMARY KEY (account_id, message_id)
);
