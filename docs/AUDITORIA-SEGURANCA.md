# Auditoria de Segurança — Quantix Gestor

**Data:** 2026-06-11
**Escopo:** Varredura de ponta a ponta (backend .NET `GestorAI.API`, frontend React, scripts de deploy/VPS, configuração de infra).
**Metodologia:** Análise estática de código, fluxo de dados de entradas não confiáveis até operações sensíveis, revisão de autenticação/autorização multi-tenant, gestão de segredos e endpoints públicos.

---

## Resumo executivo

| # | Severidade | Categoria | Local | Status |
|---|-----------|-----------|-------|--------|
| 1 | **CRÍTICO** | Fraude financeira / Auth | `WebhooksEndpoints.cs` — webhook Asaas sem verificação de assinatura | Aberto |
| 2 | **ALTO** | Takeover de tenant / Auth | `AdminEndpoints.cs` — `/admin/fix-tenant` anônimo com chave fraca | Aberto |
| 3 | **ALTO** | Segredo exposto | `scripts/vps/webhook.py` — token de deploy hardcoded no repositório | Aberto |
| 4 | **MÉDIO** | Segredos em config | `appsettings.json` — `AdminFixKey`, senha do banco em texto plano | Aberto |
| 5 | **MÉDIO** | SSRF | `AutomacaoEndpoints.cs` / `EvolutionApiService.cs` — URL controlada pelo usuário | Aberto |
| 6 | **MÉDIO** | Config TLS | `Program.cs` — `RequireHttpsMetadata = false` | Aberto |
| 7 | **BAIXO** | Exposição de token | Frontend — JWT/refresh token em `localStorage` | Aberto |

---

## 1. CRÍTICO — Webhook Asaas sem verificação de assinatura (fraude de pagamento)

**Arquivo:** `backend/src/GestorAI.API/Endpoints/WebhooksEndpoints.cs:9-24`

**Descrição:** O endpoint `POST /api/webhooks/asaas` é `AllowAnonymous()` e **não valida nenhuma assinatura, token ou segredo** que comprove que a requisição realmente veio do Asaas. Ele simplesmente confia no corpo JSON: se `Event` for `PAYMENT_RECEIVED`/`PAYMENT_CONFIRMED` e existir um `Payment.Id`, marca a cobrança correspondente como **Paga** e gera um lançamento de receita.

```csharp
app.MapPost("/api/webhooks/asaas", async (AsaasWebhookPayload payload, ...) =>
{
    if (payload.Event is not ("PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED")) return Results.Ok();
    var asaasId = payload.Payment?.Id;
    ...
    await cobrancaService.ConfirmarPagamentoAsaasAsync(asaasId, payload.Payment?.BillingType, ct);
}).AllowAnonymous();
```

`ConfirmarPagamentoAsaasAsync` (`CobrancaService.cs:223`) busca a cobrança por `AsaasId` ignorando o filtro de tenant e altera o status para `Pago`, registrando receita no financeiro.

**Cenário de exploração:** Um atacante envia
`POST /api/webhooks/asaas` com `{"event":"PAYMENT_RECEIVED","payment":{"id":"pay_xxxxx"}}`.
Se acertar/enumerar um `AsaasId` válido (identificadores Asaas seguem um padrão previsível `pay_########`), a cobrança é marcada como paga sem que nenhum pagamento real tenha ocorrido — fraude financeira direta, com impacto em conciliação e faturamento de **todos os tenants** (o webhook ignora o filtro de empresa).

**Recomendação:**
- Validar a autenticidade da requisição. O Asaas suporta um **token de autenticação de webhook** (cabeçalho `asaas-access-token`); configure um segredo por ambiente e rejeite requisições sem o cabeçalho correto.
- Como reforço, considerar consultar a API do Asaas para confirmar o status real do pagamento antes de marcar como pago, em vez de confiar apenas no payload.

---

## 2. ALTO — Endpoint `/admin/fix-tenant` anônimo com chave fraca e versionada

**Arquivo:** `backend/src/GestorAI.API/Endpoints/AdminEndpoints.cs:20-48` + `appsettings.json:10`

**Descrição:** O endpoint `POST /admin/fix-tenant?companyId=...&key=...` é `AllowAnonymous()` e protegido apenas por uma comparação com `config["AdminFixKey"]`, cujo valor estava **hardcoded no `appsettings.json` versionado** (chave curta e previsível, já removida). O endpoint reatribui **todos** os registros com `EmpresaId = Guid.Empty` (em 19 tabelas) para o `companyId` informado.

**Cenário de exploração:** Qualquer pessoa que tenha acesso ao repositório (ou adivinhe a chave previsível) pode chamar o endpoint passando o `companyId` da própria empresa e **absorver para o seu tenant todos os dados órfãos** (produtos, vendas, clientes, lançamentos, notas fiscais, contratos, cobranças etc.) de qualquer registro com tenant vazio. A chave também trafega na query string (fica em logs/histórico).

**Recomendação:**
- Remover o endpoint do código após a migração única (o próprio comentário já indica isso) ou protegê-lo com `RequireAuthorization("AdminOnly")` de superadmin.
- Nunca versionar a chave; movê-la para variável de ambiente/secret.
- Receber a chave via cabeçalho, não query string.

---

## 3. ALTO — Token de deploy hardcoded e versionado

**Arquivo:** `scripts/vps/webhook.py:4`

**Descrição:** O token que protege o webhook de deploy de produção está fixo no código versionado:

```python
TOKEN = "<token de 64 hex hardcoded no arquivo — já removido/rotacionado>"
```

O handler executa `subprocess.run(["bash", DEPLOY, sha])` para qualquer requisição que apresente esse token no cabeçalho `X-Deploy-Token`. Embora o socket escute apenas em `127.0.0.1:9989`, o token é o único fator que separa um trigger de deploy legítimo de um malicioso, e está exposto a qualquer um com acesso de leitura ao repositório.

**Cenário de exploração:** Quem tiver o token e conseguir alcançar o webhook (ex.: via outra vulnerabilidade que permita requisições à loopback, ou um proxy mal configurado) dispara deploys arbitrários de qualquer SHA presente nos artifacts do GitHub Actions.

**Recomendação:** Ler o token de variável de ambiente (`os.environ`), rotacionar o token atual (considerá-lo comprometido por já estar no histórico do git) e remover o valor do versionamento.

---

## 4. MÉDIO — Segredos em texto plano no `appsettings.json`

**Arquivo:** `backend/src/GestorAI.API/appsettings.json`

**Descrição:** Além do `AdminFixKey` (item 2), a connection string contém usuário/senha do banco em texto plano (`Username=gestorai;Password=gestorai`). Em produção esses valores devem vir de variáveis de ambiente/secret manager. Senhas idênticas a nome de usuário também aparecem no `docker-compose.yml`.

**Observação:** Aceitável para ambiente de desenvolvimento local, mas confirme que **produção** sobrescreve tudo via env vars e que `appsettings.Production.json` não contém segredos reais. Credenciais armazenadas em disco são, em geral, tratadas fora deste escopo, mas o `AdminFixKey` é um caso de segurança ativa.

**Recomendação:** Mover segredos para variáveis de ambiente / secret manager e garantir que nenhum segredo de produção esteja versionado.

---

## 5. MÉDIO — SSRF via URL controlada pelo usuário (Evolution API)

**Arquivos:** `backend/src/GestorAI.API/Endpoints/AutomacaoEndpoints.cs:42-47`, `Services/Automacao/EvolutionApiService.cs`

**Descrição:** O endpoint autenticado `POST /api/automacao/testar-conexao` recebe `req.ApiUrl` (host **e** protocolo controlados pelo usuário) e o servidor faz uma requisição HTTP a essa URL:

```csharp
var ok = await evolutionSvc.TestarConexaoAsync(req.ApiUrl, req.ApiKey, ct);
// -> client.GetAsync($"{apiUrl.TrimEnd('/')}/instance/fetchInstances")
```

O mesmo vetor existe de forma persistente em `EvolutionApiService.EnviarMensagemAsync`, que usa a `EvolutionApiUrl` salva na configuração da empresa.

**Cenário de exploração:** Um usuário autenticado (de qualquer tenant) aponta `ApiUrl` para endereços internos — por exemplo, o endpoint de metadados da nuvem (`http://169.254.169.254/...`) ou serviços internos da rede — fazendo o servidor emitir requisições em seu nome (port scanning interno, possível leitura de metadados se a resposta for refletida).

**Recomendação:** Validar e restringir a URL: exigir `https`, resolver o host e bloquear faixas privadas/loopback/link-local (RFC 1918, `169.254.0.0/16`, `127.0.0.0/8`), idealmente com uma allowlist de domínios. Considerar timeout curto e desabilitar redirecionamentos.

---

## 6. MÉDIO — Metadados JWT sem exigência de HTTPS

**Arquivo:** `backend/src/GestorAI.API/Program.cs:52`

**Descrição:** `opt.RequireHttpsMetadata = false` permite que a obtenção das chaves de assinatura do IdP (Authority) ocorra sobre HTTP. Em produção, isso abre espaço para MITM na recuperação das chaves de validação do token.

**Recomendação:** Definir `RequireHttpsMetadata = true` em produção (pode ser condicional ao ambiente) e garantir que `Jwt:Authority` use HTTPS no ambiente produtivo.

---

## 7. BAIXO — Tokens JWT em `localStorage`

**Arquivo:** `frontend/src/contexts/AuthContext.tsx:99-100`

**Descrição:** `access_token` e `refresh_token` são guardados em `localStorage`, acessível via JavaScript. Em caso de XSS, os tokens (incluindo o refresh de longa duração) podem ser exfiltrados. O fluxo PKCE está corretamente implementado, o que é positivo.

**Recomendação:** Preferir cookies `HttpOnly`/`Secure`/`SameSite` para o refresh token, ou ao menos manter o refresh token fora do `localStorage`. Manter políticas de CSP no frontend para reduzir superfície de XSS.

---

## Pontos positivos observados

- **Isolamento multi-tenant** consistente via global query filters por `EmpresaId` no `AppDbContext`, com uso controlado e justificado de `IgnoreQueryFilters()` nos fluxos públicos (sempre com filtro explícito por `EmpresaId`).
- **SQL parametrizado** — o único `ExecuteSqlRaw` usa parâmetros (`@p0`, `@p1`) e nomes de tabela vêm de constante; sem injeção de SQL.
- **PKCE** corretamente implementado no fluxo OAuth do frontend.
- **CORS** restrito a origens configuradas (não usa `AllowAnyOrigin`).
- **Upload de logo** valida extensão e tamanho, e usa o `EmpresaId` (GUID do servidor) como nome de arquivo — sem path traversal a partir do nome enviado.
- **Middleware de exceção** não vaza stack traces ao cliente (retorna mensagem genérica em 500).
- React/TSX sem uso de `dangerouslySetInnerHTML` — sem XSS evidente no frontend.

---

## Prioridade de remediação sugerida

1. **Imediato:** Itens 1 e 2 (fraude de pagamento e takeover de tenant) — ambos exploráveis sem autenticação.
2. **Curto prazo:** Itens 3 e 4 (rotacionar e desversionar segredos) e item 5 (SSRF).
3. **Médio prazo:** Itens 6 e 7 (hardening de TLS e armazenamento de token).
