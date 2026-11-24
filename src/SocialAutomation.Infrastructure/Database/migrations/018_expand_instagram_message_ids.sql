BEGIN;

ALTER TABLE instagram_message_receipts
    ALTER COLUMN message_id TYPE TEXT;

ALTER TABLE messages
    ALTER COLUMN instagram_message_id TYPE TEXT;

COMMIT;
