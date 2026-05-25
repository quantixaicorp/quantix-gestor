# Agendamentos — Design Spec

**Data:** 2026-05-24

---

## Contexto

O GestorAI precisa de um módulo de agendamentos para negócios de serviços (salões, clínicas, estúdios). O módulo tem duas frentes:

- **Sprint 1 — ERP interno:** gestão de agenda pelos operadores
- **Sprint 2 — Portal público:** clientes agendam sozinhos via link slug

---

## Escopo — Sprint 1 (ERP Interno)

### Entidades

#### Profissional

```csharp
Id          Guid
EmpresaId   Guid           // tenant
Nome        string
Telefone    string?
Ativo       bool
CriadoEm   DateTime
```

#### DisponibilidadeSemanal

Horários semanais recorrentes por profissional.

```csharp
Id              Guid
ProfissionalId  Guid
DiaSemana       DayOfWeek   // 0=Dom … 6=Sab
HoraInicio      TimeSpan
HoraFim         TimeSpan
```

Regra: sem sobreposição de faixas para o mesmo profissional + dia.

#### BloqueioAgenda

Exceções pontuais (folgas, reuniões, feriados).

```csharp
Id              Guid
EmpresaId       Guid
ProfissionalId  Guid?       // null = bloqueia todos
DataInicio      DateTime
DataFim         DateTime
Motivo          string?
```

#### Agendamento

```csharp
Id              Guid
EmpresaId       Guid
ProfissionalId  Guid
ClienteNome     string
ClienteTelefone string
ClienteId       Guid?       // link opcional para Clientes
ServicoId       Guid        // FK para Produtos (DuracaoMinutos > 0)
DataHoraInicio  DateTime
DataHoraFim     DateTime    // calculado: DataHoraInicio + DuracaoMinutos
Status          AgendamentoStatus
Observacao      string?
VendaId         Guid?       // preenchido após Concluir
CriadoEm       DateTime
```

Enum `AgendamentoStatus`: `Agendado | Confirmado | Concluido | Cancelado`

#### Modificação em Produto

Adicionar campo opcional:

```csharp
DuracaoMinutos  int?    // null = não é serviço
```

---

### Fluxo de Status

```
Agendado → Confirmado → Concluido
         ↘            ↘
          Cancelado    Cancelado
```

Ao transitar para **Concluido**, o backend cria automaticamente uma `Venda` no status `Aberta` com o serviço como item, preenche `VendaId` no agendamento e retorna `vendaId` para o frontend navegar até `/vendas/nova?vendaId=<id>`.

---

### Validações de negócio (backend)

1. `DataHoraInicio` deve estar dentro de uma `DisponibilidadeSemanal` do profissional
2. Nenhum `BloqueioAgenda` ativo deve cobrir o horário
3. Nenhum outro `Agendamento` (não-Cancelado) do mesmo profissional sobrepõe o intervalo
4. `ServicoId` deve referenciar um Produto com `DuracaoMinutos > 0`
5. Todas as validações em `AgendamentoService`, nunca no endpoint

---

### Backend — Endpoints Sprint 1

**Profissionais**

| Método | Rota | Policy |
|--------|------|--------|
| GET | `/api/profissionais` | Bearer |
| POST | `/api/profissionais` | `AdminOnly` |
| PUT | `/api/profissionais/{id}` | `AdminOnly` |
| DELETE | `/api/profissionais/{id}` | `AdminOnly` |

**Disponibilidade**

| Método | Rota | Policy |
|--------|------|--------|
| GET | `/api/profissionais/{id}/disponibilidade` | Bearer |
| PUT | `/api/profissionais/{id}/disponibilidade` | `AdminOnly` |

Recebe array completo de faixas; substitui todas as existentes atomicamente.

**Bloqueios**

| Método | Rota | Policy |
|--------|------|--------|
| GET | `/api/agenda/bloqueios?de=&ate=` | Bearer |
| POST | `/api/agenda/bloqueios` | Bearer |
| DELETE | `/api/agenda/bloqueios/{id}` | Bearer |

**Agendamentos**

| Método | Rota | Policy |
|--------|------|--------|
| GET | `/api/agendamentos?data=YYYY-MM-DD` | Bearer |
| GET | `/api/agendamentos/{id}` | Bearer |
| POST | `/api/agendamentos` | Bearer |
| PUT | `/api/agendamentos/{id}` | Bearer |
| POST | `/api/agendamentos/{id}/confirmar` | Bearer |
| POST | `/api/agendamentos/{id}/concluir` | Bearer — retorna `{ vendaId }` |
| POST | `/api/agendamentos/{id}/cancelar` | Bearer |

**Horários livres (helper)**

```
GET /api/agendamentos/slots?profissionalId=&data=YYYY-MM-DD&servicoId=
```

Retorna array de `DateTime` disponíveis de 30 em 30 min dentro das faixas semanais, descontando bloqueios e agendamentos existentes.

---

### DTOs principais

```csharp
// Request
record CriarAgendamentoRequest(
    Guid ProfissionalId,
    string ClienteNome,
    string ClienteTelefone,
    Guid? ClienteId,
    Guid ServicoId,
    DateTime DataHoraInicio,
    string? Observacao
);

// Response
record AgendamentoResponse(
    Guid Id,
    string ProfissionalNome,
    string ClienteNome,
    string ClienteTelefone,
    string ServicoNome,
    int DuracaoMinutos,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    AgendamentoStatus Status,
    string? Observacao,
    Guid? VendaId
);

record ConcluirResponse(Guid VendaId);
```

---

### Frontend — Telas Sprint 1

#### Rotas novas

| Rota | Componente |
|------|-----------|
| `/agendamentos` | `Agendamentos.tsx` |
| `/agendamentos/novo` | `NovoAgendamento.tsx` |
| `/agendamentos/:id` | `DetalheAgendamento.tsx` |
| `/profissionais` | `Profissionais.tsx` |
| `/profissionais/:id/disponibilidade` | `DisponibilidadeProfissional.tsx` |

#### Sidebar

Adicionar após "Orçamentos":

```
📅 Agendamentos   → /agendamentos
👤 Profissionais  → /profissionais
```

#### `Agendamentos.tsx` — Agenda diária

- Seletor de data (default = hoje) com botões ← →
- **Grade de colunas** — uma coluna por profissional ativo
- Cada coluna lista agendamentos do dia com horário, nome do cliente, serviço e badge de status
- Cores de badge: `Agendado`=azul, `Confirmado`=verde, `Concluido`=roxo, `Cancelado`=vermelho
- Botão "Novo Agendamento" no topo direito
- Clicar no card navega para `DetalheAgendamento`

#### `NovoAgendamento.tsx` — Formulário

Campos:
1. Profissional (select — `GET /api/profissionais`)
2. Serviço (select — Produtos com `DuracaoMinutos > 0`)
3. Data
4. Horário (select — `GET /api/agendamentos/slots?...` após escolher 1+2+3)
5. Cliente: campo telefone primeiro → se existir, preenche nome automaticamente; se não, texto livre
6. Observação (textarea)
7. Botão "Agendar"

#### `DetalheAgendamento.tsx`

- Exibe todos os campos
- Botões condicionais por status:
  - `Agendado` → Confirmar + Cancelar + WhatsApp
  - `Confirmado` → Concluir + Cancelar + WhatsApp
  - `Concluido` → "Ver Venda" (navega para `/vendas`)
  - `Cancelado` — sem ações
- Botão WhatsApp monta link `wa.me/{tel}?text=...` com data/hora/serviço
- Concluir → chama `concluir(id)` → navega para `/vendas/nova?vendaId=<id>`

#### `Profissionais.tsx`

- Tabela com Nome, Telefone, Status (Ativo/Inativo)
- Botão "Novo Profissional" → modal inline (nome + telefone + ativo)
- Editar / Excluir por linha

#### `DisponibilidadeProfissional.tsx`

- Grade 7 dias × campos HoraInicio / HoraFim
- Salvar substitui todas as faixas atomicamente via `PUT /api/profissionais/:id/disponibilidade`
- Botão "+ faixa" por dia para múltiplas faixas (ex: 8h-12h e 14h-18h)

#### Hook `useAgendamentos`

```ts
const {
  agendamentos,   // AgendamentoResponse[]
  agendamento,    // AgendamentoResponse | null
  loading,
  error,
  list,           // (data: string) => Promise<void>
  get,            // (id: string) => Promise<void>
  create,         // (req) => Promise<AgendamentoResponse>
  confirmar,      // (id: string) => Promise<void>
  concluir,       // (id: string) => Promise<ConcluirResponse>
  cancelar,       // (id: string) => Promise<void>
  slots,          // (profId, data, servicoId) => Promise<string[]>
} = useAgendamentos()
```

#### Hook `useProfissionais`

```ts
interface DisponibilidadeItem {
  diaSemana: number    // 0–6
  horaInicio: string  // "HH:mm"
  horaFim: string     // "HH:mm"
}

const {
  profissionais,
  loading,
  error,
  list,
  create,
  update,
  remove,
  getDisponibilidade,  // (id: string) => Promise<DisponibilidadeItem[]>
  saveDisponibilidade, // (id: string, items: DisponibilidadeItem[]) => Promise<void>
} = useProfissionais()
```

---

## Escopo — Sprint 2 (Portal Público) — Outline

> Implementar após Sprint 1 estar estável.

- Empresa tem campo `SlugPublico` (único, ex: `salao-da-maria`)
- Rota pública: `/agendar/:slug` — sem autenticação JWT
- Endpoint público: `GET /api/public/agenda/:slug/slots?servicoId=&data=`
- Endpoint público: `POST /api/public/agenda/:slug/agendar`
- Frontend: página standalone fora do layout com Sidebar
- Fluxo: escolher serviço → escolher profissional → escolher data/hora → preencher nome+telefone → confirmar
- Lookup de cliente por telefone: se existir no sistema, pré-preenche nome
- Empresa configura `SlugPublico` nas configurações

---

## Decisões técnicas

| Decisão | Escolha | Motivo |
|---------|---------|--------|
| Disponibilidade | Template semanal + bloqueios | Flexível sem complexidade de calendário recorrente |
| Cálculo de slots | Backend (`/slots` endpoint) | Evita lógica de negócio no frontend |
| DataHoraFim | Calculada no backend | Consistência; frontend não precisa conhecer duração |
| Conversão em Venda | Automática no Concluir | Garante integridade; venda nasce do agendamento |
| Portal público | Sprint 2 separado | Não bloqueia valor para operadores internos |
| Grade de agenda | Colunas por profissional | Visão do dia de trabalho, comparável a Google Calendar |

---

## Padrões a seguir (do projeto)

- Multi-tenancy via `TenantContext.EmpresaId` + `HasQueryFilter` — **nunca filtrar manualmente**
- Transação obrigatória em `Concluir` (cria Venda + atualiza Agendamento atomicamente)
- DTOs em todas as respostas — nunca retornar entidade EF diretamente
- FluentValidation + `ValidationFilter` para validação de request
- `CancellationToken` em todos os métodos de serviço
- Hook interface estável — páginas não mudam quando a implementação muda
