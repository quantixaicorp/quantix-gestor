# Módulo de Compras — GestorAI

**Data:** 2026-06-14
**Status:** Aprovado para implementação

---

## Contexto

Módulo de Compras para MEIs, microempresas e pequenas empresas. Permite gerenciar fornecedores, registrar pedidos e compras, controlar entradas de estoque, gerar contas a pagar com agrupamento de parcelas e visualizar dashboards executivos.

**Fora do escopo desta versão:** recomendações inteligentes (IA), módulo patrimonial completo, refatoração da UI de lançamentos manuais existentes para o novo modelo de Parcelamento.

---

## Abordagem Escolhida

**Abordagem C:** Implementar Compras com o modelo correto de Parcelamento desde o início. A fundação de dados (entidade `Parcelamento` + FK em `Lancamento`) é criada agora. A refatoração da UI do financeiro para lançamentos manuais fica para o próximo sprint.

---

## 1. Modelo de Dados

### 1.1 Entidades Novas

#### `PedidoCompra`
| Campo | Tipo | Descrição |
|---|---|---|
| Id | Guid | PK |
| EmpresaId | Guid | Multi-tenant |
| Numero | int | Auto-incremento por empresa |
| Data | DateTime | Data do pedido |
| FornecedorId | Guid | FK → Fornecedor |
| Status | enum | Rascunho \| AguardandoAprovacao \| Aprovado \| RecebidoParcialmente \| RecebidoTotalmente \| Cancelado |
| ValorEstimado | decimal | Soma estimada dos itens |
| Observacoes | string? | Campo livre |

#### `ItemPedidoCompra`
| Campo | Tipo | Descrição |
|---|---|---|
| Id | Guid | PK |
| PedidoCompraId | Guid | FK → PedidoCompra |
| ProdutoId | Guid? | FK → Produto (nullable) |
| Descricao | string | Nome do item |
| Quantidade | decimal | Qtd solicitada |
| ValorEstimado | decimal | Valor unitário estimado |

#### `Compra`
| Campo | Tipo | Descrição |
|---|---|---|
| Id | Guid | PK |
| EmpresaId | Guid | Multi-tenant |
| Numero | int | Auto-incremento por empresa |
| Data | DateTime | Data da compra |
| FornecedorId | Guid | FK → Fornecedor |
| PedidoCompraId | Guid? | FK opcional → PedidoCompra |
| TipoCompra | string | Ex: "Mercadoria", "Serviço", "Ativo" |
| NumeroNota | string? | Número da NF |
| CondicaoPagamento | string | AVista \| 30d \| 30_60_90d \| Parcelado \| Personalizado |
| FormaPagamento | string | Dinheiro \| PIX \| Boleto \| CartaoCredito \| CartaoDebito \| Transferencia |
| Status | enum | Rascunho \| Confirmada \| Cancelada |
| ValorTotal | decimal | Soma dos itens |
| Observacoes | string? | Campo livre |

#### `ItemCompra`
| Campo | Tipo | Descrição |
|---|---|---|
| Id | Guid | PK |
| CompraId | Guid | FK → Compra |
| ProdutoId | Guid? | FK → Produto (nullable) |
| Descricao | string | Nome do item |
| DestinoCompra | enum | EstoqueParaVenda \| ConsumoInterno \| AtivoImobilizado |
| Quantidade | decimal | Quantidade |
| ValorUnitario | decimal | Valor unitário |
| Desconto | decimal | Desconto em R$ |
| FreteRateado | decimal | Frete rateado proporcional |
| Impostos | decimal | Impostos incidentes |
| ValorTotal | decimal | Calculado: (qty × unit - desconto + frete + impostos) |
| CategoriaFinanceira | string? | Categoria para lançamento financeiro |
| CentroCusto | string? | Centro de custo |

#### `Parcelamento`
| Campo | Tipo | Descrição |
|---|---|---|
| Id | Guid | PK |
| EmpresaId | Guid | Multi-tenant |
| CompraId | Guid? | FK → Compra (nullable — extensível para outras origens) |
| Descricao | string | Ex: "Compra #42 — 3x" |
| ValorTotal | decimal | Valor total do parcelamento |
| QtdParcelas | int | Número de parcelas |
| Status | enum | EmAberto \| PagoParcialmente \| PagoTotal \| Cancelado |

---

### 1.2 Entidades Modificadas

#### `Fornecedor` — novos campos
| Campo | Tipo |
|---|---|
| RazaoSocial | string? |
| NomeFantasia | string? |
| InscricaoEstadual | string? |
| Whatsapp | string? |
| Status | enum: Ativo \| Inativo (default Ativo) |

O campo `Nome` existente é mantido como display name. Sem breaking change.

#### `Lancamento` — novos campos
| Campo | Tipo | Descrição |
|---|---|---|
| ParcelamentoId | Guid? | FK → Parcelamento (nullable — lançamentos antigos ficam null) |
| NumeroParcela | int? | Ex: 1, 2, 3 de N parcelas |

---

### 1.3 Relacionamentos

```
PedidoCompra ──< ItemPedidoCompra
PedidoCompra ──> Compra         (1:0..1, opcional)
Compra       ──< ItemCompra
Compra       ──> Parcelamento   (1:1, criado ao confirmar)
Parcelamento ──< Lancamento     (via ParcelamentoId em Lancamento)
```

---

## 2. Fluxos de Comportamento

### 2.1 Confirmar uma Compra

Ao mudar `Status: Rascunho → Confirmada`, executado em **transação única**:

**Passo 1 — Atualização de Estoque** (para itens com `DestinoCompra = EstoqueParaVenda`):
- Chama `ProdutoService.EntradaEstoqueAsync` por item
- Atualiza `Produto.EstoqueAtual` e `Produto.CustoMedio`
- Cria registro em `MovimentacaoEstoque` com tipo "Compra"
- Fórmula custo médio ponderado:
  ```
  novo_custo = (estoque_atual × custo_atual + qty × valor_unitario) / (estoque_atual + qty)
  ```

**Passo 2 — Geração de Parcelamento e Parcelas** (para todos os itens):
- Cria `Parcelamento` vinculado à compra
- Cria N `Lancamento` (tipo `Despesa`, status `Pendente`), cada um com:
  - `ParcelamentoId` apontando para o parcelamento
  - `NumeroParcela = X` (1 a N)
  - `Descricao = "Compra #123 - Parcela X/N"`
  - `DataVencimento` calculada pela condição de pagamento
  - `Categoria` = categoria financeira do item (ou "Compras" como default)

**Regras:**
- Compra confirmada não pode ser editada
- Cancelar compra: parcelas `Pendente` viram `Cancelado`; movimentações de estoque são revertidas (entrada negativa)
- `Numero` da compra e do pedido: `MAX(Numero) + 1 WHERE EmpresaId = X`, calculado dentro da transação de criação

### 2.4 Atualização do Status do Parcelamento

Quando um `Lancamento` com `ParcelamentoId` é pago (via `POST /lancamentos/:id/pagar`), o `LancamentoService` deve chamar `ParcelamentoService.RecalcularStatus(parcelamentoId)`:

| Condição | Status resultante |
|---|---|
| Nenhuma parcela paga | EmAberto |
| Algumas pagas, outras pendentes | PagoParcialmente |
| Todas pagas | PagoTotal |
| Parcelamento cancelado | Cancelado |

---

### 2.2 Cálculo de Vencimentos por Condição de Pagamento

| Condição | Comportamento |
|---|---|
| À Vista | 1 parcela, vencimento = data da compra |
| 30 dias | 1 parcela, vencimento = data + 30d |
| 30/60/90 dias | 3 parcelas, vencimentos +30d, +60d, +90d |
| Parcelado (Nx) | N parcelas mensais a partir da data |
| Personalizado | Usuário informa cada data manualmente na tela |

---

### 2.3 Pedido de Compra → Compra

Ao converter um pedido (`POST /api/pedidos-compra/:id/converter`):
- Cria nova `Compra` com `PedidoCompraId` preenchido
- Pré-popula itens da compra a partir dos itens do pedido
- Frontend abre o formulário de Compra pré-preenchido para revisão antes de confirmar
- Ao confirmar a compra, pedido atualiza status: `RecebidoParcialmente` ou `RecebidoTotalmente` conforme quantidades

---

## 3. Frontend

### 3.1 Rotas

```
/compras                    Lista de compras com KPIs e tabela
/compras/nova               Formulário nova compra
/compras/:id                Detalhe/edição da compra
/compras/pedidos            Lista de pedidos de compra
/compras/pedidos/novo       Formulário novo pedido
/compras/pedidos/:id        Detalhe/edição do pedido
/compras/dashboard          Dashboard executivo de compras
```

### 3.2 Sidebar — grupo "Compras"

```
Compras (moduleSlug: 'compras')
  ├── Fornecedores     /fornecedores       (já existe)
  ├── Pedidos          /compras/pedidos    (novo)
  ├── Compras          /compras            (novo)
  └── Dashboard        /compras/dashboard  (novo)
```

### 3.3 Descrição das Páginas

**`/compras`**
- 3 cards KPI: Total Comprado no Mês / Qtd de Compras / Contas a Pagar Geradas
- Tabela: Nº, Data, Fornecedor, Valor Total, Status (badge colorido), Ações
- Botão "Nova Compra"
- Filtros: status, fornecedor, período

**`/compras/nova`** — formulário em 4 etapas:
1. **Cabeçalho:** Fornecedor, Data, Nº Nota, Tipo de Compra, Pedido Vinculado (opcional), Observações
2. **Itens:** tabela com Produto/Descrição, Destino, Qtd, Valor Unit., Desconto, Frete, Impostos, Total
3. **Pagamento:** Condição de Pagamento, Forma, preview das parcelas geradas com datas editáveis
4. **Revisão:** resumo completo antes de salvar como rascunho ou confirmar direto

**`/compras/:id`**
- Visualização do detalhe com timeline de status
- Seção de itens, seção de parcelamento com parcelas (pagas/abertas)
- Ações: Confirmar / Cancelar (conforme status)

**`/compras/pedidos`**
- Tabela: Nº, Data, Fornecedor, Valor Estimado, Status
- Botão "Novo Pedido"
- Ação "Converter em Compra" para pedidos aprovados

**`/compras/dashboard`**
- KPIs: Total Mês, Total Ano, Ticket Médio, Qtd Compras, Fornecedores Ativos
- Gráfico de evolução mensal (linha)
- Compras por fornecedor (donut)
- Top produtos comprados (barra horizontal)

**`/relatorios`** — nova aba "Compras"
- Filtros: período, fornecedor, destino da compra
- Tabelas: compras por período, por fornecedor, por produto

### 3.4 Novos Hooks

| Hook | Responsabilidade |
|---|---|
| `useCompras` | CRUD, confirmar, cancelar |
| `usePedidosCompra` | CRUD, converter em compra |
| `useComprasDashboard` | KPIs e séries para gráficos |
| `useParcelamentos` | listar parcelamentos e suas parcelas |

---

## 4. Backend

### 4.1 Endpoints

#### `/api/compras`
```
GET    /              listar (filtros: status, fornecedorId, de, ate)
GET    /resumo        KPIs do mês
GET    /:id           detalhe completo com itens e parcelamento
POST   /              criar rascunho
PUT    /:id           editar (só rascunho)
POST   /:id/confirmar confirma + dispara estoque + gera parcelas (transação)
POST   /:id/cancelar  cancela + reverte estoque + cancela parcelas pendentes
DELETE /:id           remover rascunho
```

#### `/api/pedidos-compra`
```
GET    /              listar (filtros: status, fornecedorId)
GET    /:id           detalhe com itens
POST   /              criar
PUT    /:id           editar
POST   /:id/converter converte em Compra (retorna Compra pré-preenchida)
POST   /:id/cancelar  cancelar
```

#### `/api/parcelamentos`
```
GET    /              listar (filtros: status, compraId)
GET    /:id           detalhe com lista de parcelas (Lancamentos vinculados)
```

#### `/api/compras/dashboard`
```
GET    /              KPIs + séries para gráficos (query: de, ate — padrão do projeto)
```

### 4.2 Serviços

| Serviço | Responsabilidade |
|---|---|
| `CompraService` | CRUD + orquestra confirmação (chama ProdutoService e ParcelamentoService) + cancelamento |
| `PedidoCompraService` | CRUD + conversão em Compra |
| `ParcelamentoService` | Criar parcelamento com N Lancamentos; listar com parcelas |
| `ComprasDashboardService` | Agregar KPIs e séries temporais |

### 4.3 Migration

Uma migration única `AddModuloCompras` com:
- Expand `Fornecedores`: RazaoSocial, NomeFantasia, InscricaoEstadual, Whatsapp, Status
- Alter `Lancamentos`: + ParcelamentoId (Guid?, FK), + NumeroParcela (int?)
- Create `Parcelamentos`
- Create `Compras`
- Create `ItensCompra`
- Create `PedidosCompra`
- Create `ItensPedidoCompra`

---

## 5. Fora do Escopo (próximas versões)

- Recomendações inteligentes via IA
- Módulo patrimonial completo para itens `AtivoImobilizado`
- Refatoração da UI de lançamentos manuais existentes para expor o agrupamento de Parcelamentos
- Relatórios avançados: Curva ABC de compras, histórico de preços por fornecedor
- Ranking e scoring de fornecedores
