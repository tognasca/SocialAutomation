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
docker compose --env-file .env -f docker-compose.prod.yml config --quiet
docker compose --env-file .env -f docker-compose.prod.yml up -d --build
docker compose --env-file .env -f docker-compose.prod.yml ps
docker compose --env-file .env -f docker-compose.prod.yml logs --tail=100 api worker frontend
```

Compare os volumes ativos antes de migrar um deploy existente: os nomes dos volumes do Compose de desenvolvimento e de produção diferem. Faça backup e mantenha o PostgreSQL existente ou restaure o backup no volume de produção. Nunca use `down -v` para essa troca.

Crie um `.env` local a partir de `.env.example`. Preencha `JWT_SECRET`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `REDIS_PASSWORD`, `INSTAGRAM_APP_ID`, `INSTAGRAM_APP_SECRET`, `INSTAGRAM_WEBHOOK_VERIFY_TOKEN` e `INSTAGRAM_TOKEN_ENCRYPTION_KEY`. Use a mesma chave aleatória de pelo menos 32 caracteres na API e no worker e mantenha-a estável para preservar a leitura dos tokens. Configure `OPENAI_API_KEY` se usar respostas ou geração de conteúdo com IA. O App Secret e o token de verificação anteriores precisam ser rotacionados na Meta porque já foram versionados.

## Instagram

1. No app da Meta para Facebook Login, cadastre o retorno `https://socialautomation.ademartognasca.com.br/instagram/callback`. Use uma conta profissional conectada a uma Página do Facebook. Confira as permissões de leitura, mensagens e publicação, bem como o acesso a conteúdo público para pesquisa por hashtag.
2. Configure o webhook `https://api-socialautomation.ademartognasca.com.br/api/webhooks/instagram` com a assinatura do mesmo app e a inscrição de mensagens. O token de verificação do GET e o App Secret do POST são credenciais distintas.
3. Reconecte as contas existentes pelo painel para obter o token de usuário usado em pesquisa e publicação. O token da Página continua sendo usado para DMs. Confira os estados da conta e do webhook no painel.
4. Em **Instagram**, consulte uma hashtag ou um perfil profissional. No **Inbox**, responda a alguém que tenha iniciado uma conversa nas últimas 24 horas. Para resposta automática, ative o agente padrão da empresa e coloque a conversa em modo **IA_ATIVA**.
5. Em **Conteúdo**, selecione a conta e forneça uma URL HTTPS pública da mídia. Use imagem JPEG para Post ou Story e vídeo compatível para Reel. Selecione **Publicar agora** ou uma data de agendamento. Confira o estado no painel e verifique a publicação na conta de teste.

O worker usa a fila persistente do PostgreSQL. Se uma execução for interrompida depois de iniciar uma chamada externa, confira a conta e o inbox antes de repetir o envio. Publicações históricas com ID do antigo simulador aparecem como `Simulado`.

## Testes automatizados

Execute `dotnet test SocialAutomation.slnx` com o SDK .NET 10. Para incluir o teste de concorrência e migrations, defina `SOCIALAUTOMATION_TEST_POSTGRES` com uma conexão a um PostgreSQL de testes que permita criar e excluir um banco temporário. No frontend, execute `npm ci`, `npm run lint` e `npm run build`.

## Teste de CORS preflight

```powershell
curl.exe -i -X OPTIONS "http://localhost:5010/api/auth/login" -H "Origin: https://socialautomation.ademartognasca.com.br" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: content-type"
curl.exe -i -X OPTIONS "https://api-socialautomation.ademartognasca.com.br/api/auth/login" -H "Origin: http://localhost:3101" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: content-type"
```

Esperado: status 204 e Access-Control-Allow-Origin com a origem enviada. Se o teste local funcionar mas o público falhar, verifique túnel, DNS e regras Cloudflare, incluindo Access/WAF.

**Segurança:** troque as credenciais de desenvolvimento e revogue quaisquer tokens publicados no histórico do Git. Retirar o token do commit mais recente não o apaga de commits anteriores.

Na homologação, confira a saída das migrations no log da API e valide o fluxo com uma conta de teste da Meta.
