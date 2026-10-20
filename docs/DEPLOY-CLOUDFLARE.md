# Deploy SocialAutomation com Docker + Cloudflare Tunnel

## Portas (quando o cloudflared roda no host Windows)

- Frontend: `http://localhost:3100`
- API: `http://localhost:5010`
- Banco: `postgres:5432` (somente rede Docker)
- Redis: `redis:6379` (somente rede Docker)

Rotas públicas no `C:\Cloudflared\config\config.yml` (integrar às rotas existentes, sem substituir seus outros serviços):

```yaml
  - hostname: socialautomation.ademartognasca.com.br
    service: http://localhost:3100
  - hostname: api-socialautomation.ademartognasca.com.br
    service: http://localhost:5010
```

Mantenha uma regra final `- service: http_status:404`.
Se o tunnel estiver em container, `localhost` aponta para o container e não para o host. Adapte o endereço conforme a rede real.

## Deploy

```powershell
docker compose -f docker-compose.yml config
docker compose -f docker-compose.yml up -d --build
docker compose -f docker-compose.yml ps
docker compose -f docker-compose.yml logs --tail=100 api frontend postgres
```

A versão de produção exige um arquivo `.env` local com JWT_SECRET, POSTGRES_USER, POSTGRES_PASSWORD e REDIS_PASSWORD. Não versionar segredos. Para produção, use `docker-compose.prod.yml` depois de configurar essas variáveis.

## Teste de CORS preflight

```powershell
curl.exe -i -X OPTIONS "http://localhost:5010/api/auth/login" -H "Origin: https://socialautomation.ademartognasca.com.br" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: content-type"
curl.exe -i -X OPTIONS "https://api-socialautomation.ademartognasca.com.br/api/auth/login" -H "Origin: http://localhost:3101" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: content-type"
```

Esperado: status 204 e Access-Control-Allow-Origin com a origem enviada. Se o teste local funcionar mas o público falhar, verifique túnel, DNS e regras Cloudflare, incluindo Access/WAF.

**Segurança:** troque as credenciais de desenvolvimento e revogue quaisquer tokens publicados no histórico do Git. Retirar o token do commit mais recente não o apaga de commits anteriores.

**Limite da auditoria:** estas alterações são estáticas no GitHub, não foram executadas no host Docker/Cloudflare. Antes de declarar o deploy corrigido, executar build, migrations e testes end-to-end.
