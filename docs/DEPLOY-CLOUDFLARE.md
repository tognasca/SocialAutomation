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

## Campanhas de comentário para Direct

1. No app da Meta, assine o campo `comments` do objeto Instagram no mesmo webhook que já recebe DMs. Confirme que a conta profissional e a Página conectada concederam as permissões exigidas para comentários e mensagens. O app precisa estar autorizado para a conta usada no teste.
2. Em **Automação → Campanhas**, selecione a conta e uma publicação própria, preencha a palavra-chave e a mensagem. Use um convite para responder e avançar com a demonstração. A campanha é criada pausada. Ative-a quando quiser começar a receber comentários novos.
3. Com outra conta, comente a palavra-chave em um post próprio e confira a mensagem privada no Instagram. Teste o webhook com o mesmo ID de comentário duas vezes e confirme que existe apenas um envio. Verifique o contador na campanha.
4. Responda à mensagem privada para abrir a conversa no Inbox e criar ou localizar o lead. Os contadores registram envios confirmados, respostas atribuídas e falhas. A conversa usa o agente e o CRM existentes da empresa.

Resultados da busca de perfis podem ser salvos no CRM. O link para o perfil permite abordagem manual. Para ofertas e fechamento use o histórico, as etapas e as tarefas do CRM. A implementação não faz disparos para perfis encontrados na pesquisa.

### Qualificação e agenda da campanha

Em **Automação → Campanhas → Configurar qualificação e agenda**, cadastre até cinco perguntas e os horários de demonstração no horário de Brasília. Cada horário representa um bloco de 30 minutos e só pode ser cadastrado uma vez por empresa. Deixe as perguntas vazias para desligar o fluxo da campanha. A primeira mensagem privada deve convidar a pessoa a responder para começar.

Depois da resposta do lead, uma conversa em **IA ativa** segue as perguntas da campanha em ordem. O CRM grava as respostas em **Atividades** e move os leads nas etapas iniciais para **Interessado**. Ao terminar, o Direct oferece até três horários; a pessoa escolhe com `AGENDAR <código>`. A reserva aparece na campanha, e o CRM cria uma tarefa no horário reservado. Se não houver vaga, o CRM registra uma tarefa para combinar o horário. O fluxo respeita o modo **Humano** e a janela de envio que o Inbox já aplica.

A agenda é interna. O teste com Instagram deve confirmar a chegada da pergunta, o registro das respostas, a escolha do horário e a existência de uma única tarefa de demonstração.

### Proposta comercial da campanha

Abra **Automação → Campanhas → Configurar qualificação e agenda**. Use a quantidade de veículos como primeira pergunta de qualificação. Cadastre em **Proposta comercial** as faixas de veículos, o preço oficial e as condições, incluindo a periodicidade. O sistema escolhe a menor faixa que comporta a quantidade informada. Se a resposta estiver fora das regras, cria uma tarefa de revisão e transfere o atendimento.

Após a reserva da demonstração, o lead pode responder `PROPOSTA` no Direct. O link abre os dados comerciais salvos no momento da emissão e permite registrar um aceite em até sete dias. O aceite é registrado nas atividades e gera uma tarefa no CRM. Configure `PUBLIC_API_URL` no Compose se a API utilizar outro domínio público. O aceite operacional não executa cobrança.

### Campanha de captura ESTOQUE

Em **Automação → Campanhas**, selecione **Usar modelo de diagnóstico de estoque**. Adapte o roteiro e a legenda, publique o Reel e associe o ID da publicação e a conta do Instagram. Salve a campanha pausada, configure os horários em **Configurar qualificação e agenda** e ative a campanha.

Para homologar com outra conta do Instagram, comente `ESTOQUE`, responda `QUERO` ao Direct e responda às duas perguntas. Confira o checklist na conversa e as respostas no CRM. Envie `DEMO` para consultar horários, escolha `AGENDAR <código>` e valide a reserva. Se houver planos cadastrados, envie `PROPOSTA` após a reserva e confirme que a aceitação aparece no funil. A conversa precisa estar em **IA ativa** para avançar automaticamente.

A API aplica a migration `022_campaign_capture.sql` na inicialização. Atualize API, worker e frontend para disponibilizar a campanha.

### Página pública de captura

Depois de atualizar a API, o worker e o frontend, abra uma campanha com material em **Automação → Campanhas → Página pública de captura**. Personalize a oferta e clique em **Publicar página**. A página tem publicação própria e pode ser despublicada pelo painel. **Configurar qualificação e agenda** controla as perguntas e as vagas compartilhadas com o Direct.

Selecione a origem antes de copiar o link para cada canal. O painel usa a primeira origem registrada na sessão do visitante. O visitante recebe o checklist sem informar contato. Para solicitar retorno e reservar uma demonstração, ele informa e-mail ou WhatsApp e autoriza o contato. O CRM registra as respostas, a origem e o agendamento. Se a campanha tiver planos cadastrados, após a reserva o visitante pode consultar e aceitar sua proposta.

A API aplica a migration `023_public_campaign_capture.sql` na inicialização. Para homologar, abra um link de Stories em uma sessão anônima, complete o diagnóstico, autorize um contato de teste e reserve uma vaga. Confira a atividade e a tarefa no CRM, e atualize o funil por origem para conferir a visita, o diagnóstico, a demonstração e o aceite.
