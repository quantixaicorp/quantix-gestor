# Contratos e Cobranças — Design Spec

**Goal:** Adicionar dois módulos ao GestorAI ERP: Contratos (gestão de documentos de acordo com clientes) e Cobranças (gestão de recebíveis), desacoplados mas integráveis via "Gerar Cobranças".

**Architecture:** Opção B — módulos independentes com integração opcional. Um contrato ativo pode gerar cobranças automaticamente (um registro por ciclo/parcela), mas cobranças também existem de forma avulsa sem contrato. Segue o padrão ContaAzul.

**Tech Stack:** .NET 10 Minimal APIs, EF Core + PostgreSQL, QuestPDF (geração de PDF server-side), React + TailwindCSS, padrão existente do projeto (ITenantEntity, AppDbContext, Service + Endpoints + DTOs, hooks React).

---

## Módulo 1: Contratos

### Entidades

```csharp
// Domain/Entities/Contrato.cs
public class Contrato : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public int Numero { get; set; }               // auto-incrementado por empresa
    public Guid ClienteId { get; set; }
    public required string Titulo { get; set; }
    public required string Objeto { get; set; }   // descrição do que o contrato cobre
    public TipoCobranca TipoCobranca { get; set; }
    public decimal Valor { get; set; }            // mensal se Recorrente, total se ParceladoPrazoFixo
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }        // null = indefinido (válido para recorrente sem prazo)
    public Periodicidade Periodicidade { get; set; }
    public int DiaVencimento { get; set; }        // 1–28, dia do mês das cobranças
    public ContratoStatus Status { get; set; } = ContratoStatus.Rascunho;
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public ICollection<ContratoItem> Itens { get; set; } = [];
}

// Domain/Entities/ContratoItem.cs
public class ContratoItem
{
    public Guid Id { get; set; }
    public Guid ContratoId { get; set; }
    public required string Descricao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
}
```

### Enums

```csharp
public enum ContratoStatus  { Rascunho, Ativo, Encerrado, Cancelado }
public enum TipoCobranca    { Recorrente, ParceladoPrazoFixo }
public enum Periodicidade   { Mensal, Trimestral, Semestral, Anual }
```

### Endpoints `GET|POST /api/contratos`

| Método | Rota                          | Descrição                                                  |
|--------|-------------------------------|------------------------------------------------------------|
| GET    | `/`                           | Lista contratos (query: `status`, `clienteId`)             |
| GET    | `/{id}`                       | Detalhe com itens e cobranças geradas                      |
| POST   | `/`                           | Criar contrato (status inicial: Rascunho)                  |
| PUT    | `/{id}`                       | Editar contrato (apenas em Rascunho)                       |
| POST   | `/{id}/ativar`                | Rascunho → Ativo                                           |
| POST   | `/{id}/encerrar`              | Ativo → Encerrado                                          |
| POST   | `/{id}/cancelar`              | Qualquer status → Cancelado                                |
| GET    | `/{id}/pdf`                   | Retorna PDF do contrato (application/pdf)                  |
| POST   | `/{id}/gerar-cobranças`       | Body: `{ de, ate }` — gera cobranças no período            |

### Regras de negócio

- **Numero**: sequencial por empresa, gerado no `CriarAsync` com `MAX(Numero) + 1` com lock.
- **Edição**: apenas contratos em status `Rascunho` podem ser editados ou ter itens alterados.
- **Ativar**: requer `ClienteId` preenchido, pelo menos 1 item, e `DataInicio` válida.
- **Encerrar**: apenas contratos `Ativo`. Cobranças `Pendente` existentes não são canceladas automaticamente (usuário decide).
- **Gerar Cobranças** (`POST /{id}/gerar-cobranças`):
  - Contrato deve estar `Ativo`.
  - Recebe `{ de: DateOnly, ate: DateOnly }`.
  - Calcula as datas de vencimento dentro do período com base em `Periodicidade` e `DiaVencimento`.
  - Não duplica: verifica se já existe `Cobranca` com `ContratoId == id` e `DataVencimento == dataCalculada` antes de criar.
  - Para `ParceladoPrazoFixo`: referência gerada como `"Parcela X/N"`.
  - Para `Recorrente`: referência gerada como `"Mensalidade Jan/2026"` (ou Trim./Sem./Ano conforme periodicidade).
  - Para `ParceladoPrazoFixo`: `DataFim` é obrigatória (validada no `AtivarAsync`).
  - Número de parcelas para `ParceladoPrazoFixo`: derivado de `DataInicio`, `DataFim` e `Periodicidade` (ex: Jan/2026 a Dez/2026 Mensal = 12 parcelas). Não é armazenado.
  - Valor da cobrança:
    - `Recorrente` → `Contrato.Valor` (valor mensal/periódico).
    - `ParceladoPrazoFixo` → `Contrato.Valor / nParcelas` (calculado na hora da geração).
- **PDF**: gerado com QuestPDF, contém cabeçalho com dados da empresa (`ConfiguracaoEmpresa`), dados do cliente, tabela de itens, valor total, datas, e área de assinatura manual ao final.

---

## Módulo 2: Cobranças

### Entidade

```csharp
// Domain/Entities/Cobranca.cs
public class Cobranca : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid? ContratoId { get; set; }         // null = cobrança avulsa
    public required string Referencia { get; set; } // ex: "Mensalidade Mai/2026"
    public decimal Valor { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public CobrancaStatus Status { get; set; } = CobrancaStatus.Pendente;
    public FormaPagamento? FormaPagamento { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public Contrato? Contrato { get; set; }
}
```

### Enums

```csharp
public enum CobrancaStatus  { Pendente, Pago, Cancelado }
// FormaPagamento já existe em Venda: Dinheiro | Pix | Cartao | Outro
// reutilizar o enum existente
```

> **Vencido** é derivado: `Status == Pendente && DataVencimento < DateOnly.FromDateTime(DateTime.UtcNow)`. Não é armazenado.

### Endpoints `GET|POST /api/cobrancas`

| Método | Rota                  | Descrição                                                        |
|--------|-----------------------|------------------------------------------------------------------|
| GET    | `/`                   | Lista cobranças (query: `status`, `clienteId`, `mes` YYYY-MM)    |
| GET    | `/{id}`               | Detalhe                                                          |
| POST   | `/`                   | Criar cobrança avulsa                                            |
| PUT    | `/{id}`               | Editar (apenas `Pendente`)                                       |
| POST   | `/{id}/pagar`         | Body: `{ dataPagamento, formaPagamento }` → Status Pago          |
| POST   | `/{id}/cancelar`      | Cancela cobrança                                                 |
| GET    | `/{id}/whatsapp`      | Retorna `{ url: "https://wa.me/55..." }` com mensagem pronta     |

### Regras de negócio

- **Edição**: apenas cobranças `Pendente`.
- **Pagar**: define `DataPagamento = DateTime.UtcNow` (ou a data informada), `FormaPagamento`, `Status = Pago`.
- **WhatsApp URL**: monta mensagem:
  ```
  "Olá {ClienteNome}, segue cobrança referente a {Referencia}: R$ {Valor:N2}
  com vencimento em {DataVencimento:dd/MM/yyyy}.
  Em caso de dúvidas, entre em contato."
  ```
  Retorna `https://wa.me/55{WhatsappSemMascara}?text={urlEncoded}`.
- **Filtro por `mes`**: retorna cobranças com `DataVencimento` dentro do mês informado (YYYY-MM).

---

## Frontend

### Estrutura de arquivos novos

```
frontend/src/
├── types/
│   ├── contrato.ts      — Contrato, ContratoItem, ContratoStatus, TipoCobranca, Periodicidade
│   └── cobranca.ts      — Cobranca, CobrancaStatus
├── hooks/
│   ├── useContratos.ts  — list, get, create, update, ativar, encerrar, cancelar, gerarCobranças, downloadPdf
│   └── useCobrancas.ts  — list, get, create, update, pagar, cancelar, getWhatsappUrl
└── pages/
    ├── contratos/
    │   ├── Contratos.tsx       — lista com filtro por status
    │   ├── NovoContrato.tsx    — formulário de criação
    │   └── DetalheContrato.tsx — detalhe + ações + cobranças geradas
    └── cobrancas/
        ├── Cobrancas.tsx       — lista com filtro status + mês
        ├── NovaCobranca.tsx    — formulário avulso
        └── DetalheCobranca.tsx — detalhe + registrar pagamento + WhatsApp
```

### Telas

**`/contratos`** — tabela: Nº · Cliente · Título · Tipo · Valor · Status (badge) · Data Início. Botão "Novo Contrato". Filtro por status.

**`/contratos/novo`** — campos: cliente (select), título, objeto (textarea), tipo de cobrança (toggle), valor, data início, data fim (opcional), periodicidade, dia de vencimento, itens (tabela editável com descrição/qtd/valor unitário), observação.

**`/contratos/:id`** — exibe todos os dados + lista de cobranças já geradas. Botões conforme status:
- Rascunho: Ativar, Editar, Cancelar
- Ativo: Encerrar, Cancelar, **Gerar Cobranças** (abre modal com seleção de período), **Baixar PDF**
- Encerrado/Cancelado: apenas Baixar PDF

**`/cobrancas`** — tabela: Referência · Cliente · Contrato · Valor · Vencimento · Status (badge: verde=Pago, amarelo=Pendente, vermelho=Vencido). Filtros: status + mês.

**`/cobrancas/nova`** — campos: cliente, referência, valor, vencimento, observação.

**`/cobrancas/:id`** — exibe dados + botões conforme status:
- Pendente: **Registrar Pagamento** (modal: data + forma pagamento), Cancelar, **Enviar via WhatsApp** (abre `wa.me` em nova aba)
- Pago/Cancelado: somente leitura

### Sidebar — novo grupo

```
Contratos
  ├── FileText   "Contratos"   → /contratos
  └── DollarSign "Cobranças"   → /cobrancas
```

### Rotas novas (`router/index.tsx`)

```
/contratos            → Contratos
/contratos/novo       → NovoContrato
/contratos/:id        → DetalheContrato
/cobrancas            → Cobrancas
/cobrancas/nova       → NovaCobranca
/cobrancas/:id        → DetalheCobranca
```

---

## Migrações EF Core

Duas novas migrations:
1. `AddContratos` — tabelas `Contratos` e `ContratoItens`
2. `AddCobrancas` — tabela `Cobrancas`

Query filters no `AppDbContext`:
```csharp
modelBuilder.Entity<Contrato>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
modelBuilder.Entity<Cobranca>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
```

---

## Dependências externas

- **QuestPDF** (`QuestPDF` NuGet) — geração de PDF server-side. Licença Community gratuita para receita < $1M/ano.
- Nenhuma outra dependência nova.

---

## Fora de escopo (v1)

- Assinatura digital (ClickSign, DocuSign)
- Envio automático via WhatsApp API
- Gateway de pagamento (Pix cobranças, boleto)
- Régua de cobrança automática (lembretes automáticos de vencimento)
