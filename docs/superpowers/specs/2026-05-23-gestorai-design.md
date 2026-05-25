# GestorAI — Design Spec v1

**Data:** 2026-05-23
**Produto:** GestorAI
**Ecossistema:** QuantixAI
**Status:** Aprovado para implementação

---

## 1. Visão Geral

GestorAI é um SaaS para micro e pequenas empresas controlarem vendas, estoque, financeiro e clientes em um único lugar, substituindo planilhas e cadernos. Não tem foco em restaurantes (nicho já coberto pelo Quantix ERP). Público-alvo: lojas de roupas, salões, esmalterias, papelarias, assistências técnicas, prestadores de serviço e pequenos comércios.

Produto independente do ecossistema QuantixAI — compartilha apenas o **Quantix Admin** como emissor de JWT (OAuth2 PKCE). Banco, API e frontend são totalmente separados.

---

## 2. Decisões de Produto

| Decisão | Escolha |
|---|---|
| MVP | Dashboard + Vendas + Estoque + Financeiro + Clientes |
| Módulos opcionais (v2) | Orçamentos + Agendamentos |
| BI / Relatórios | Completo no v1: página dedicada, múltiplos relatórios, drill-down, exportação PDF/Excel |
| Auth | Quantix Admin (compartilhado) |
| Banco de dados | PostgreSQL separado (gestorai) |
| Repositório | `/ERP/gestorai-erp/` dentro do monorepo existente |
| Abordagem | Projeto limpo com os mesmos padrões do Quantix ERP (sem copiar código) |
| Layout / Design | Mesmo visual do Quantix ERP (shadcn/ui + Tailwind) |

---

## 3. Arquitetura

### Ecossistema

```
[Quantix Admin :5001]  ← IdP OAuth2 PKCE / JWT issuer (compartilhado)
        ↕
[GestorAI Frontend :5174]  ←→  [GestorAI API :5002]  ←→  [PostgreSQL gestorai :5433]
```

### Estrutura no monorepo

```
/ERP/
  quantix-erp/        ← produto restaurante (existente)
  quantix-admin/      ← IdP compartilhado (existente)
  gestorai-erp/       ← este produto
    frontend/
    backend/
    docker-compose.yml
```

### Padrões herdados do Quantix ERP

| Camada | Padrão |
|---|---|
| Multi-tenancy | `TenantMiddleware` → `TenantContext` → `AppDbContext` QueryFilter por `EmpresaId` |
| Auth | JWT via Quantix Admin; claims: `empresa_id`, `roles` |
| Backend | .NET 10 Minimal API + Services + DTOs + FluentValidation |
| Frontend | Custom Hooks + shadcn/ui + Tailwind + React Hook Form + Zod + Recharts |
| Transações | `BeginTransactionAsync` obrigatório em Vendas e Financeiro |
| Respostas API | Sempre DTOs (nunca entidades EF) |

### Políticas de autorização

| Policy | Roles |
|---|---|
| `AdminOnly` | admin |
| `FinanceAccess` | admin, financeiro |
| `EstoqueAccess` | admin, estoque |
| `VendasAccess` | admin, vendas, estoque |

### Claims JWT (Quantix Admin)

```
empresa_id  → GUID do tenant
roles       → ["admin", "vendas", "estoque", "financeiro"]
```

---

## 4. Modelo de Dados

### Produtos & Estoque

```
Produto
  Id (GUID), EmpresaId (GUID), CategoriaId (GUID)
  Nome (string), Descricao (string, nullable)
  PrecoVenda (decimal), CustoMedio (decimal)
  EstoqueAtual (decimal), EstoqueMinimo (decimal)
  CodigoBarras (string, nullable), Ativo (bool)
  CriadoEm, AtualizadoEm

Categoria
  Id (GUID), EmpresaId (GUID), Nome (string)

MovimentacaoEstoque
  Id (GUID), EmpresaId (GUID), ProdutoId (GUID)
  Tipo (enum: Entrada | Saida | Ajuste)
  Quantidade (decimal)
  Origem (enum: Venda | Compra | Manual)
  ReferenciaId (GUID, nullable)
  DataHora, Observacao (string, nullable)
```

### Vendas

```
Venda
  Id (GUID), EmpresaId (GUID), ClienteId (GUID, nullable)
  DataHora
  Status (enum: Aberta | Concluida | Cancelada)
  Subtotal (decimal), Desconto (decimal), Total (decimal)
  FormaPagamento (enum: Dinheiro | Pix | Cartao | Outro)
  Parcelas (int, nullable), Observacao (string, nullable)

ItemVenda
  Id (GUID), VendaId (GUID), ProdutoId (GUID)
  Quantidade (decimal), PrecoUnitario (decimal)
  Desconto (decimal), Total (decimal)
```

### Clientes

```
Cliente
  Id (GUID), EmpresaId (GUID)
  Nome (string), Whatsapp (string)
  Email (string, nullable), Observacoes (string, nullable)
  DataCadastro
```

Índice: `UNIQUE (EmpresaId, Whatsapp)`

### Financeiro

```
Lancamento
  Id (GUID), EmpresaId (GUID)
  Tipo (enum: Receita | Despesa)
  Descricao (string), Valor (decimal)
  DataVencimento, DataPagamento (nullable)
  Status (enum: Pendente | Pago | Cancelado)
  Categoria (string), VendaId (GUID, nullable)
  Observacao (string, nullable)
```

> Contas a pagar = `Lancamento` onde `Tipo = Despesa`
> Contas a receber = `Lancamento` onde `Tipo = Receita` e `Status = Pendente`
> Lançamentos vencidos = `DataVencimento < hoje` e `Status = Pendente` (calculado em query, sem campo extra)

---

## 5. Módulos e Telas

### Navegação (Sidebar fixa)

```
├── Dashboard
├── Vendas
│   ├── Nova Venda
│   └── Histórico
├── Estoque
│   ├── Produtos
│   └── Movimentações
├── Financeiro
│   ├── Lançamentos
│   ├── Contas a Pagar
│   └── Contas a Receber
├── Clientes
└── Relatórios
```

### Dashboard

Cards:
- Total vendido hoje / no mês
- Lucro estimado do mês (soma de `(PrecoUnitario - CustoMedio) * Quantidade` nos itens de vendas concluídas no mês; usa CustoMedio atual como aproximação)
- Contas a pagar (vencidas + próximas 7 dias)
- Contas a receber pendentes
- Produtos com estoque abaixo do mínimo (badge de alerta)

Gráficos (Recharts):
- Vendas dos últimos 7 dias (barra)
- Entradas vs Saídas do mês (linha)
- Top 5 produtos mais vendidos (barra horizontal)

### Vendas

**Nova Venda** — fluxo 3 etapas:
1. Busca e adiciona produtos (por nome ou código de barras)
2. Define cliente (opcional), desconto e forma de pagamento
3. Confirma e emite comprovante (impressão ou PDF)

**Histórico** — tabela com filtros por período, status e forma de pagamento. Detalhe da venda com opção de cancelamento.

### Estoque

**Produtos** — listagem com filtros por categoria, badge visual para estoque baixo. CRUD completo. Entrada manual de estoque na tela do produto.

**Movimentações** — log completo de entradas/saídas com origem (venda, ajuste manual).

### Financeiro

**Lançamentos** — visão unificada com filtros por tipo, status e período. Botão rápido para registrar despesa.

**Contas a Pagar** — despesas pendentes ordenadas por vencimento. Alerta visual para vencidas.

**Contas a Receber** — receitas pendentes (incluindo as geradas automaticamente por vendas).

### Clientes

Listagem com busca por nome/WhatsApp. Cadastro simples. Tela de detalhe com histórico de compras e total gasto.

### Relatórios / BI

Página dedicada com abas:

| Aba | Conteúdo |
|---|---|
| Visão Geral | KPIs: faturamento total, ticket médio (Total / qtd vendas), margem estimada (lucro estimado / faturamento %), inadimplência (receitas vencidas / total a receber %) |
| Vendas | Tendência, ranking de produtos, ranking de clientes, vendas por forma de pagamento |
| Financeiro | Fluxo de caixa por período, receitas vs despesas, categorias de despesas |
| Estoque | Giro de estoque, produtos sem movimentação, valor total em estoque |

Filtros globais: período (hoje / semana / mês / personalizado). Drill-down: clicar em um elemento do gráfico (ex: barra de produto) aplica aquela dimensão como filtro nos demais cards da aba.
Exportação: PDF e Excel por aba.

---

## 6. Estrutura Técnica

### Backend — `gestorai-erp/backend/src/GestorAI.API/`

```
Domain/
  Entities/     ← Produto, Categoria, Venda, ItemVenda,
                   MovimentacaoEstoque, Cliente, Lancamento
  Enums/        ← TipoLancamento, StatusVenda, FormaPagamento,
                   TipoMovimentacao, OrigemMovimentacao, StatusLancamento
Infrastructure/
  Data/
    AppDbContext.cs   ← QueryFilters por EmpresaId
  Repositories/
    Repository.cs     ← Genérico
Services/
  Vendas/
  Estoque/
  Financeiro/
  Clientes/
  Relatorios/
Endpoints/            ← Minimal API, um arquivo por módulo
DTOs/                 ← Request/Response por módulo
Shared/
  MultiTenancy/       ← TenantMiddleware + TenantContext
  Exceptions/         ← AppException + ExceptionMiddleware
  Filters/            ← ValidationFilter (FluentValidation)
```

### Endpoints

| Módulo | Endpoints |
|---|---|
| Vendas | `POST /api/vendas`, `GET /api/vendas`, `GET /api/vendas/{id}`, `POST /api/vendas/{id}/cancelar` |
| Estoque | `GET/POST/PUT/DELETE /api/produtos`, `GET /api/categorias`, `POST /api/estoque/movimentar`, `GET /api/estoque/movimentacoes` |
| Financeiro | `GET/POST/PUT /api/lancamentos`, `POST /api/lancamentos/{id}/pagar`, `GET /api/financeiro/fluxo-caixa` |
| Clientes | `GET/POST/PUT/DELETE /api/clientes`, `GET /api/clientes/{id}/historico` |
| Relatórios | `GET /api/relatorios/kpis`, `/vendas`, `/financeiro`, `/estoque`, `/clientes` |

### Frontend — `gestorai-erp/frontend/src/`

```
contexts/
  AuthContext.tsx       ← OAuth2 PKCE (mesmo fluxo Quantix ERP)
services/
  api.ts                ← HTTP client com Bearer JWT + silent refresh
hooks/
  useVendas.ts
  useEstoque.ts
  useFinanceiro.ts
  useClientes.ts
  useRelatorios.ts
pages/
  Dashboard.tsx
  vendas/
  estoque/
  financeiro/
  clientes/
  relatorios/
components/
  ui/                  ← shadcn/ui primitivos
  vendas/
  estoque/
  financeiro/
  clientes/
  relatorios/          ← Recharts wrappers para BI
```

**Regras de frontend:**
- Páginas sem lógica — delegam tudo para hooks
- Hooks são a camada de dados — chamam `api.ts`
- Zero lógica de negócio no frontend
- React Hook Form + Zod em todos os formulários

---

## 7. Implantação

### docker-compose.yml

```yaml
services:
  gestorai-api:
    build: ./backend
    ports: ["5002:5002"]
    depends_on: [gestorai-db]

  gestorai-db:
    image: postgres:16
    ports: ["5433:5432"]
    environment:
      POSTGRES_DB: gestorai
      POSTGRES_USER: gestorai
      POSTGRES_PASSWORD: gestorai
```

Sem conflito com Quantix ERP (API `:5000`, DB `:5432`) nem com Quantix Admin (`:5001`).

---

## 8. Regras de Negócio Críticas

### Fluxo de Venda (transação atômica)

```
1. Validar estoque disponível de cada ItemVenda
2. INSERT Venda + ItensVenda
3. Deduzir EstoqueAtual de cada Produto
4. INSERT MovimentacaoEstoque por item (Tipo=Saida, Origem=Venda)
5. INSERT Lancamento (Tipo=Receita, vinculado à Venda)
```

Rollback completo se qualquer etapa falhar.

### Cancelamento de Venda

```
1. UPDATE Venda.Status = Cancelada
2. INSERT MovimentacaoEstoque por item (Tipo=Entrada, Origem=Venda) ← estorno
3. UPDATE Lancamento.Status = Cancelado
```

### Estoque

- `EstoqueAtual` nunca abaixo de zero — validado no Service, não só na UI
- Toda alteração de estoque gera `MovimentacaoEstoque` obrigatoriamente
- `CustoMedio` recalculado por média ponderada a cada entrada

### Financeiro

- Venda concluída → `Lancamento` de Receita criado na mesma transação
- Lançamento com `Status = Pago` não pode ser editado, apenas cancelado
- Desconto em venda não pode exceder 100% do subtotal

### Clientes

- `Whatsapp` é único por empresa (`UNIQUE (EmpresaId, Whatsapp)`)
- Histórico de compras = `Vendas` onde `ClienteId = id` e `Status = Concluida`

---

## 9. Fora do Escopo v1

| Funcionalidade | Versão prevista |
|---|---|
| Orçamentos | v2 |
| Agendamentos | v2 |
| Lembretes via WhatsApp | v2 |
| Emissão de NFe | v3 |
| App mobile | v3 |
| Core compartilhado entre produtos | v3 |
