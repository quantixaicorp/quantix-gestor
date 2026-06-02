# Página Pública de Agendamento — Design

**Goal:** Permitir que clientes agendem serviços via link público personalizado com a identidade visual do negócio, com confirmação interna obrigatória.

**Architecture:** Novos endpoints públicos no backend existente (`/public/{slug}/...`) sem autenticação, nova rota pública no frontend fora do `AppLayout`, e configuração de branding armazenada em `ConfiguracaoEmpresa`.

**Tech Stack:** .NET 10 Minimal APIs, EF Core + PostgreSQL, React + TypeScript + Vite, Tailwind CSS, react-hook-form + Zod.

---

## 1. Banco de Dados

### ConfiguracaoEmpresa — campos novos
```
Slug            string  unique, url-safe, ex: "barbearia-do-ze"
LogoUrl         string? caminho relativo, ex: "/uploads/logos/abc.png"
CorPrimaria     string? hex, ex: "#3B82F6"
DescricaoPublica string? texto curto exibido na página pública
```

### AgendamentoStatus — novo valor
```
AguardandoConfirmacao = 4  (além dos existentes: Agendado=0, Confirmado=1, Concluido=2, Cancelado=3)
```

Migration: `AddPublicBookingFields` — adiciona colunas e altera o enum.

---

## 2. Backend

### Arquivos criados/modificados

**Criar:** `Endpoints/PublicBookingEndpoints.cs`
Grupo `/public/{slug}`, sem `RequireAuthorization()`:
- `GET /public/{slug}/info` → `PublicEmpresaInfo` (nome, logoUrl, corPrimaria, descricao)
- `GET /public/{slug}/servicos` → lista de serviços ativos com id, nome, preco, duracaoMinutos
- `GET /public/{slug}/profissionais` → lista de profissionais ativos com id, nome
- `GET /public/{slug}/slots?profissionalId=&servicoId=&data=` → reutiliza lógica do `AgendaService.GetSlotsAsync`, sem filtro de tenant via `TenantContext` (usa slug para resolver EmpresaId)
- `POST /public/{slug}/agendamentos` → cria agendamento com `Status = AguardandoConfirmacao`

**Criar:** `Services/PublicBooking/PublicBookingService.cs`
- `ResolveEmpresaAsync(slug)` — busca `ConfiguracaoEmpresa` por `Slug`, retorna `EmpresaId` ou lança 404
- `GetInfoAsync(empresaId)` — retorna branding
- `GetServicosAsync(empresaId)` — filtra `Tipo == Servico && Ativo`
- `GetProfissionaisAsync(empresaId)` — filtra `Ativo`
- `GetSlotsAsync(empresaId, profissionalId, servicoId, data)` — delega para lógica existente de slots
- `CriarAgendamentoAsync(empresaId, req)` — insere com `Status = AguardandoConfirmacao`

**Modificar:** `Domain/Enums/AgendamentoStatus.cs`
Adicionar `AguardandoConfirmacao = 4`.

**Modificar:** `Services/Agendamentos/AgendaService.cs`
- `ConfirmarAsync`: aceitar `AguardandoConfirmacao` além de `Agendado`
- Adicionar `RecusarAsync(id)`: muda status para `Cancelado`

**Modificar:** `Endpoints/AgendamentosEndpoints.cs`
- Adicionar `POST /api/agendamentos/{id}/recusar` → `svc.RecusarAsync(id, ct)`

**Modificar:** `Domain/Entities/ConfiguracaoEmpresa.cs`
Adicionar propriedades: `Slug`, `LogoUrl`, `CorPrimaria`, `DescricaoPublica`.

**Modificar:** `Endpoints/ConfiguracaoEndpoints.cs`
- `POST /api/configuracoes/logo` — recebe `IFormFile`, valida extensão (jpg/png/webp, máx 2MB), salva em `wwwroot/uploads/logos/{empresaId}.{ext}`, atualiza `LogoUrl`
- `PUT /api/configuracoes/agendamento-publico` — salva `Slug`, `CorPrimaria`, `DescricaoPublica`

**Modificar:** `Program.cs`
- `app.UseStaticFiles()` para servir `wwwroot/uploads/`

### DTOs novos
```
PublicEmpresaInfo(string Nome, string? LogoUrl, string? CorPrimaria, string? Descricao)
PublicServicoResponse(Guid Id, string Nome, decimal Preco, int? DuracaoMinutos)
PublicProfissionalResponse(Guid Id, string Nome)
PublicCriarAgendamentoRequest(Guid ServicoId, Guid ProfissionalId, DateTime DataHoraInicio, string ClienteNome, string ClienteTelefone)
ConfigurarAgendamentoPublicoRequest(string Slug, string? CorPrimaria, string? DescricaoPublica)
```

---

## 3. Frontend

### Arquivos criados/modificados

**Criar:** `src/pages/agendamento-publico/AgendamentoPublico.tsx`
Rota `/agendar/:slug`, fora do `AppLayout`, sem autenticação.

Fluxo em 5 etapas com barra de progresso e branding dinâmico (cor primária via CSS variable, logo no topo):

1. **Serviço** — cards clicáveis com nome, duração e preço
2. **Profissional** — cards clicáveis com nome
3. **Data** — calendário mensal, desabilita dias sem disponibilidade
4. **Horário** — grade de slots disponíveis
5. **Seus dados** — campos Nome completo e Telefone + botão Confirmar

Tela final de confirmação: resumo do agendamento (serviço, profissional, data, hora) e mensagem "Aguardando confirmação da empresa".

**Criar:** `src/pages/agendamento-publico/usePublicBooking.ts`
Hook com estado do wizard: etapa atual, seleções, loading, chamadas aos endpoints `/public/{slug}/...`.

**Criar:** `src/pages/configuracoes/AgendamentoPublicoConfig.tsx`
Rota `/configuracoes/agendamento-publico` (autenticada):
- Campo slug (URL-safe, validado)
- Color picker para cor primária
- Campo descrição pública
- Upload de logo: input file → `POST /api/configuracoes/logo`
- Preview da logo atual
- Seção "Seu link" com link copiável

**Modificar:** `src/router/index.tsx`
- Adicionar `/agendar/:slug` fora do `AppLayout`
- Adicionar `/configuracoes/agendamento-publico` dentro do `AppLayout`

**Modificar:** `src/components/layout/Sidebar.tsx`
- Adicionar item "Configurações" com subrotas ou link direto para `/configuracoes/agendamento-publico`

**Modificar:** `src/pages/agendamentos/Agendamentos.tsx`
- Seção destacada no topo para agendamentos com status `AguardandoConfirmacao`
- Cada item tem botões **Confirmar** e **Recusar**
- Ao confirmar/recusar, rebusca a lista

**Criar:** `src/services/publicBookingApi.ts`
Funções de acesso aos endpoints públicos (sem token de autenticação).

---

## 4. Fluxo completo

```
Cliente acessa /agendar/barbearia-do-ze
  → frontend busca /public/barbearia-do-ze/info (branding)
  → exibe wizard com cores e logo da empresa
  → cliente seleciona serviço, profissional, data, hora, nome, telefone
  → POST /public/barbearia-do-ze/agendamentos
  → agendamento criado com Status=AguardandoConfirmacao (slot bloqueado)
  → cliente vê tela "Aguardando confirmação"

Empresário vê notificação na tela de Agendamentos
  → clica Confirmar → POST /api/agendamentos/{id}/confirmar → Status=Confirmado
  → ou clica Recusar → POST /api/agendamentos/{id}/recusar → Status=Cancelado (slot liberado)
```

---

## 5. Regras de negócio

- Slug deve ser único globalmente, apenas letras minúsculas, números e hífens; conflito retorna 400 com mensagem "Este slug já está em uso"
- Slot bloqueado imediatamente ao criar com `AguardandoConfirmacao` — não aparece em `/slots`
- `RecusarAsync` só aceita status `AguardandoConfirmacao` (não cancela agendamentos já confirmados via este endpoint)
- Logo: apenas jpg/png/webp, máximo 2MB
- Endpoints `/public/...` não têm `RequireAuthorization` mas retornam 404 se slug não existir
- Slots públicos usam o mesmo algoritmo de disponibilidade dos slots internos

---

## 6. Fora do escopo

- Notificações WhatsApp (implementar em sprint futura)
- Foto do profissional
- Múltiplos serviços por agendamento
- Cancelamento pelo cliente
