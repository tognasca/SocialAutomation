# Deployment and security notes

## Required configuration

Do not commit production credentials. Supply these values through the deployment environment or a secret manager:

- `Jwt__Secret`: at least 32 characters of high-entropy secret material.
- `ConnectionStrings__PostgreSQL`: database connection string.
- `Redis__ConnectionString`: Redis connection string (include its password in production).
- `Instagram__AppSecret`: Meta app secret used to validate webhook signatures.
- `Instagram__WebhookVerifyToken`: private token used during Meta webhook verification.
- `AI__ApiKey`: provider API key when AI features are enabled.
- `Instagram__AppId`: Meta app ID.
- `App__FrontendUrl`: exact frontend origin allowed by CORS.

For local development, use .NET User Secrets or environment variables. The committed `appsettings.json` intentionally contains no JWT or Meta app secret. If any credential previously committed was real, rotate it: deleting it from the current file does not erase Git history.

## Production deployment checklist

1. Replace `app.seudominio.com` and `api.seudominio.com` in `docker-compose.prod.yml` and `nginx.conf` with the actual domains before deployment.
2. Provision TLS certificates at the paths configured in `nginx.conf`; the repository does not provide production certificates.
3. Set all required environment variables before running Compose.
4. Configure `MIGRATIONS_PATH` only when needed; the API image includes SQL migrations at `/app/migrations`.
5. Verify database backups and perform a restore test before production use.
6. Confirm the Meta app permissions, OAuth flow, webhook subscription, and real publishing API flow in Meta's developer environment.

## Current functional limitation

The scheduled content worker intentionally marks pending items as failed with an explicit message instead of inventing a successful Instagram publication ID. Real publishing must be implemented against the Meta Graph API before enabling that feature for customers.
