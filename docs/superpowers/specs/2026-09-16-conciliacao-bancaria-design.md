# Conciliação Bancária — Design Spec

## Visão Geral

Módulo de conciliação bancária no GestorAI ERP que permite importar extratos bancários (OFX e CSV), cruzar automaticamente com lançamentos financeiros existentes, e gerar lançamentos para transações sem correspondência.

---

## Escopo

**Fase 1 (este spec):** Importação de OFX e CSV, conciliação automática e manual, geração de lançamentos a partir de transações não encontradas.

**Fase 2 (fora deste spec):** Importação de CNAB 240/400.

---

## Modelo de Dados

### `BankAccount` (conta bancária)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| Id | Guid | PK |
| CompanyId | Guid | FK multi-tenant |
| Name | string | Ex: "Conta Corrente BB" |
| BankName | string | Ex: "Banco do Brasil" |
| AccountNumber | string | Número da conta |
| Agency | string | Agência |
| IsActive | bool | Soft delete |
| CreatedAt | DateTime | |

### `BankStatement` (extrato importado)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| Id | Guid | PK |
| CompanyId | Guid | FK multi-tenant |
| BankAccountId | Guid | FK → BankAccount |
| FileName | string | Nome do arquivo original |
| Format | enum | OFX \| CSV |
| PeriodStart | DateOnly | Início do período do extrato |
| PeriodEnd | DateOnly | Fim do período |
| ImportedAt | DateTime | Data/hora do upload |
| ItemCount | int | Total de linhas importadas |

### `BankStatementItem` (transação do extrato)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| Id | Guid | PK |
| BankStatementId | Guid | FK → BankStatement |
| Date | DateOnly | Data da transação |
| Amount | decimal | Positivo = crédito, negativo = débito |
| Description | string | Descrição vinda do banco |
| BankTransactionId | string? | ID único do OFX (FITID), se disponível |
| Status | enum | AutoConciliated \| PendingReview \| ManuallyIgnored \| Unmatched |

### `BankReconciliation` (ligação item ↔ lançamento)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| Id | Guid | PK |
| BankStatementItemId | Guid | FK → BankStatementItem |
| TransactionId | Guid | FK → Transaction |
| ConfidenceScore | int | 0–100 |
| MatchType | enum | Auto \| Manual |
| ReconciledAt | DateTime | |
| CreatedByImport | bool | `true` quando Transaction foi gerada pelo import |

**Status de ciclo do BankStatementItem:**
```
Upload → [algoritmo] → AutoConciliated  (BankReconciliation criado, score=100)
                     → PendingReview    (BankReconciliation criado tentativo, score<100)
                     → Unmatched        → Transaction criada automaticamente (Source="BankImport")

Ações do usuário:
  PendingReview + Confirmar  → item permanece PendingReview, BankReconciliation confirmado
  PendingReview + Rejeitar   → BankReconciliation deletado, item → Unmatched
                               → Transaction criada automaticamente (mesmo fluxo do Unmatched)
  PendingReview + Ignorar    → ManuallyIgnored (sem Transaction, sem reconciliação)
  AutoConciliated + Desfazer → BankReconciliation deletado, item → PendingReview
  Unmatched + Excluir lançamento → Transaction deletada, item permanece Unmatched
```

---

## Backend

### Serviços

| Serviço | Responsabilidade |
|---------|-----------------|
| `BankAccountService` | CRUD de contas bancárias |
| `BankStatementService` | Upload, parsing, disparo do algoritmo de matching |
| `BankReconciliationService` | Algoritmo de match, confirmar/desfazer/ignorar |

### Endpoints

```
# Contas bancárias
GET    /api/bank-accounts
POST   /api/bank-accounts
DELETE /api/bank-accounts/{id}

# Extratos
POST   /api/bank-statements/import          multipart/form-data (arquivo + bankAccountId)
GET    /api/bank-statements                 lista com contadores de status
GET    /api/bank-statements/{id}/items      itens com status e reconciliação
DELETE /api/bank-statements/{id}

# Conciliação
POST   /api/bank-reconciliation/match       { itemId, transactionId }  ← match manual
DELETE /api/bank-reconciliation/{id}        ← desfazer match
POST   /api/bank-reconciliation/{itemId}/ignore  ← marcar como ignorado
```

### Algoritmo de Matching

Executado sincronicamente no momento do import, para cada `BankStatementItem`:

| Condição | Status resultante | Score |
|----------|------------------|-------|
| Valor exato + data ≤ ±3 dias | `AutoConciliated` | 100 |
| Valor ≤ 1% de diferença + data ≤ ±7 dias | `PendingReview` | 70–99 |
| Valor exato, data > ±7 dias | `PendingReview` | 50 |
| Nenhum match encontrado | `Unmatched` → cria Transaction | — |

**Regras adicionais:**
- Um `Transaction` só pode ser vinculado a um `BankStatementItem` por vez (evitar double-match)
- `Transaction` com `Status = Pago` têm prioridade de match sobre `Pendente`
- `Amount` positivo do extrato → busca `Transaction.Type = Receita`; negativo → `Despesa`

### Geração automática de Transaction para Unmatched

Quando nenhum lançamento corresponde ao item do extrato:

```csharp
new Transaction {
    CompanyId = ...,
    Type = amount > 0 ? Receita : Despesa,
    Description = item.Description,
    Amount = Math.Abs(item.Amount),
    DueDate = item.Date.ToDateTime(TimeOnly.MinValue),
    PaymentDate = item.Date.ToDateTime(TimeOnly.MinValue),
    Status = StatusLancamento.Pago,
    Category = "Importado",
    Notes = $"Gerado automaticamente via import de extrato bancário",
}
```

### Formato CSV esperado

```csv
Data,Descrição,Valor
2025-08-01,PIX RECEBIDO JOAO,250.00
2025-08-02,PAGTO FORNECEDOR,-1500.00
```

- `Data`: `yyyy-MM-dd` ou `dd/MM/yyyy`
- `Valor`: positivo = crédito, negativo = débito
- Encoding: UTF-8

---

## Frontend

### Rotas

```
/financeiro/contas-bancarias      ← CRUD de contas bancárias
/financeiro/conciliacao           ← tela principal (lista de extratos)
/financeiro/conciliacao/:id       ← revisão de um extrato importado
```

### Tela Principal (`/financeiro/conciliacao`)

- Seletor de conta bancária (dropdown)
- Botão "Importar Extrato" → abre modal com:
  - Upload de arquivo (`.ofx` ou `.csv`)
  - Seleção de conta bancária
  - Botão Importar
- Lista de extratos já importados:
  - Nome do arquivo, período (de/até), data de importação
  - Badges: `X auto-conciliados` / `Y pendentes` / `Z novos lançamentos`
  - Botão Revisar → navega para `/conciliacao/:id`

### Tela de Revisão (`/financeiro/conciliacao/:id`)

Três abas:

**Pendentes** — itens com match incerto:
- Coluna esquerda: data, valor, descrição do extrato
- Coluna direita: lançamento sugerido (descrição, data, score de confiança)
- Ações: Confirmar match | Rejeitar sugestão | Ignorar item

**Conciliados** — itens auto ou manualmente conciliados:
- Mostra par item↔lançamento
- Botão Desfazer (remove `BankReconciliation`, volta status para `PendingReview`)

**Novos Lançamentos** — itens `Unmatched` que geraram Transaction automática:
- Lista com data, valor, descrição gerada
- Campos editáveis inline: categoria, descrição
- Botão Excluir lançamento (volta item para `Unmatched` sem reconciliação)

### Fluxo do Usuário

```
1. Configurações → Financeiro → Contas Bancárias → cadastrar conta (uma vez)
2. Financeiro → Conciliação → seleciona conta → Importar Extrato
3. Upload do arquivo OFX ou CSV
4. Sistema processa → redireciona para tela de revisão
5. Aba "Pendentes": confirmar ou rejeitar sugestões do algoritmo
6. Aba "Novos Lançamentos": ajustar categoria/descrição dos lançamentos gerados
7. Extrato 100% tratado
```

---

## Integrações com Módulo Financeiro Existente

- `BankReconciliation.TransactionId` → FK para `Transaction` (tabela já existente)
- Lançamentos gerados via import aparecem normalmente em Lançamentos > lista, com `Category = "Importado"`
- O campo `Source` será adicionado à entidade `Transaction` para rastrear origem (`Manual` | `Sale` | `BankImport`)

---

## Fora de Escopo (Fase 1)

- CNAB 240/400 (Fase 2)
- Conciliação automática de transferências entre contas
- Saldo reconciliado por conta (dashboard de saldo)
- Regras de categorização automática por descrição
