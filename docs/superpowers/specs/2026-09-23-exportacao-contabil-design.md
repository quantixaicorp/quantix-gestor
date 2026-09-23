# Exportação Contábil — Design Spec

## Visão Geral

Módulo de exportação contábil no GestorAI ERP que permite ao cliente, junto com seu contador externo, mapear as categorias financeiras do sistema para contas de um Plano de Contas e gerar arquivos de importação para os sistemas **Domínio (Thomson Reuters)** e **Fortes (Fortes Tecnologia)**.

---

## Escopo

**Fase 1 (este spec):**
- Plano de Contas hierárquico com modelo padrão NBC TG pré-preenchido
- Mapeamento categoria → conta contábil
- Export para Domínio (TXT) e Fortes (CSV)

**Fora de escopo (Fase 2):**
- Outros sistemas contábeis (Alterdata, Sage, Totvs)
- SPED Contábil (ECD)
- Apuração de impostos

---

## Modelo de Dados

### `ChartOfAccount` (Plano de Contas)

| Campo | Tipo | Descrição |
|---|---|---|
| Id | Guid | PK |
| CompanyId | Guid | FK multi-tenant |
| Code | string | Ex: `3.1.1.02` |
| Name | string | Ex: `Receita de Vendas` |
| Type | enum | `Ativo` \| `Passivo` \| `PatrimonioLiquido` \| `Receita` \| `Despesa` |
| ParentId | Guid? | Auto-referência para hierarquia |
| IsActive | bool | Soft delete |

**Índice único:** `(CompanyId, Code)` — cada empresa tem seus próprios códigos.

### `AccountMapping` (De-para categoria → conta)

| Campo | Tipo | Descrição |
|---|---|---|
| Id | Guid | PK |
| CompanyId | Guid | FK multi-tenant |
| CategoryName | string | Igual ao `Transaction.Category` |
| AccountId | Guid | FK → ChartOfAccount |

**Índice único:** `(CompanyId, CategoryName)`

### Adições em `CompanySettings`

| Campo | Tipo | Descrição |
|---|---|---|
| DefaultCashAccountId | Guid? | FK → ChartOfAccount — conta de caixa/banco padrão usada como contrapartida |
| PreferredAccountingSystem | enum? | `Dominio` \| `Fortes` — preferência para pré-selecionar no export |

---

## Lógica de Lançamento Contábil

Para cada `Transaction` com `Status = Pago` no período:

| Tipo | Débito | Crédito |
|---|---|---|
| Receita | Conta padrão de caixa | Conta mapeada da categoria |
| Despesa | Conta mapeada da categoria | Conta padrão de caixa |

O campo `Transaction.PaymentDate` é usado como data do lançamento contábil (data de competência efetiva).

---

## Backend

### Novos serviços

| Serviço | Responsabilidade |
|---|---|
| `ChartOfAccountService` | CRUD + carga do template NBC TG |
| `AccountMappingService` | CRUD bulk do de-para |
| `AccountingExportService` | Monta `AccountingEntry` list + delega ao exporter |
| `DominioExporter` | Converte para TXT layout Domínio |
| `FortesExporter` | Converte para CSV layout Fortes |

### Interface de exporter

```csharp
public interface IAccountingExporter
{
    string FileExtension { get; }       // ".txt" ou ".csv"
    string ContentType { get; }
    string FileName(DateOnly from, DateOnly to);
    byte[] Export(IEnumerable<AccountingEntry> entries);
}

public record AccountingEntry(
    DateOnly Date,
    string Description,
    string DebitCode,
    string CreditCode,
    decimal Amount);
```

### Endpoints

```
# Plano de Contas
GET    /api/chart-of-accounts                   lista hierárquica (árvore)
POST   /api/chart-of-accounts                   criar conta
PUT    /api/chart-of-accounts/{id}              editar código/nome
DELETE /api/chart-of-accounts/{id}              soft delete
POST   /api/chart-of-accounts/load-template     carrega modelo padrão NBC TG
                                                (só executa se plano estiver vazio)

# Mapeamento
GET    /api/account-mappings                    lista todos (categoria + conta mapeada)
PUT    /api/account-mappings                    bulk upsert — salva todos de uma vez

# Configurações contábeis
GET    /api/accounting-settings
PUT    /api/accounting-settings                 { defaultCashAccountId, preferredAccountingSystem }

# Export
POST   /api/accounting-export/download
       Body: { system: "Dominio"|"Fortes", months: ["2025-01","2025-02"], includePending: false }
       Response: arquivo para download (Content-Disposition: attachment)
```

### Validação no export

Antes de gerar o arquivo, o serviço verifica:
1. `DefaultCashAccountId` está configurado — erro se não estiver
2. Todas as categorias presentes nas transações do período têm mapeamento — erro listando as categorias faltantes

### Formato Domínio (TXT)

```
I|01/01/2025|DESCRICAO DO LANCAMENTO|3.1.1.01|1.1.1.02|1500,00|
```

Campos separados por `|`:
- `I` = identificador de lançamento
- Data no formato `dd/MM/yyyy`
- Histórico (descrição truncada em 40 caracteres)
- Conta débito (código)
- Conta crédito (código)
- Valor com vírgula decimal, sem separador de milhar
- Pipe final obrigatório

### Formato Fortes (CSV)

```
Data;Historico;Debito;Credito;Valor
01/01/2025;DESCRICAO DO LANCAMENTO;3.1.1.01;1.1.1.02;1500,00
```

Separador `;`, primeira linha é cabeçalho, encoding UTF-8.

---

## Template Padrão NBC TG

Carregado via `POST /api/chart-of-accounts/load-template`. Estrutura mínima:

```
1       Ativo
1.1     Ativo Circulante
1.1.1   Caixa e Equivalentes de Caixa
1.1.1.01  Caixa
1.1.1.02  Banco Conta Corrente
1.1.2   Contas a Receber
1.1.2.01  Clientes
1.1.3   Estoques
1.2     Ativo Não Circulante
1.2.1   Imobilizado
2       Passivo
2.1     Passivo Circulante
2.1.1   Fornecedores
2.1.2   Obrigações Fiscais
2.1.3   Obrigações Trabalhistas
2.2     Passivo Não Circulante
3       Patrimônio Líquido
3.1     Capital Social
3.2     Lucros/Prejuízos Acumulados
4       Receitas
4.1     Receitas Operacionais
4.1.1   Receita de Vendas
4.1.2   Receita de Serviços
4.2     Outras Receitas
5       Despesas
5.1     Despesas Operacionais
5.1.1   Despesas Administrativas
5.1.1.01  Aluguel
5.1.1.02  Água e Energia Elétrica
5.1.1.03  Material de Escritório
5.1.1.04  Telefone e Internet
5.1.2   Despesas com Pessoal
5.1.2.01  Salários e Ordenados
5.1.2.02  Encargos Sociais
5.1.2.03  Pró-Labore
5.1.3   Despesas Tributárias
5.1.3.01  Impostos e Taxas
5.1.3.02  Simples Nacional
5.1.4   Despesas Financeiras
5.1.4.01  Juros e Encargos Bancários
5.1.4.02  Tarifas Bancárias
5.2     Custo das Mercadorias/Serviços Vendidos
5.2.1   CMV/CSV
```

---

## Frontend

### Novas rotas

```
/configuracoes/contabilidade/plano-de-contas
/configuracoes/contabilidade/mapeamento
/financeiro/exportar-contador
```

### Tela: Plano de Contas

- Árvore hierárquica com grupos expansíveis por tipo (Ativo, Passivo, PL, Receitas, Despesas)
- Botão **"Carregar Modelo Padrão"** visível apenas se o plano estiver vazio
- Cada linha: código + nome + botões editar / adicionar subconta / desativar
- Contas desativadas aparecem em cinza (não somem, para preservar histórico de mapeamentos)

### Tela: Mapeamento de Categorias

- Tabela: coluna Categoria (nome) + coluna Tipo (Receita/Despesa) + coluna Conta Contábil (dropdown)
- Categorias sem mapeamento destacadas com badge amarelo "Sem mapeamento"
- Campo no topo: **Conta Padrão de Caixa** (dropdown — pré-selecionada se o plano tem `1.1.1.02`)
- Botão **Salvar Mapeamento** (bulk save)

### Tela: Exportar para Contador

- Seletor de sistema: **Domínio** / **Fortes** (radio buttons, pré-selecionado por `PreferredAccountingSystem`)
- Seletor de período: grid de meses (jan–dez) com seleção múltipla + seletor de ano
- Checkbox **Incluir lançamentos pendentes** (desmarcado por padrão)
- Se houver categorias sem mapeamento no período selecionado: banner de aviso listando as categorias antes do botão exportar
- Botão **Exportar** → dispara download do arquivo

### Navegação

- Dois novos itens em **Configurações → Contabilidade**: "Plano de Contas" e "Mapeamento"
- Novo item em **Financeiro**: "Exportar para Contador" (ícone FileDown)

---

## Integrações com Módulos Existentes

- `Transaction.Category` → chave de busca no `AccountMapping`
- `Transaction.PaymentDate` → data do lançamento contábil
- `Transaction.Type` → determina qual lado é débito/crédito
- `TransactionCategory` → fonte das categorias disponíveis para mapeamento

---

## Fora de Escopo (Fase 1)

- Outros sistemas contábeis
- SPED Contábil / ECD
- Apuração de impostos (DAS, DARF)
- Razão contábil, balancete, balanço patrimonial
- Lançamentos de abertura/encerramento de exercício
