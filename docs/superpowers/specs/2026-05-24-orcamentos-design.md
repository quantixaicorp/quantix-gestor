# Orçamentos — Design Spec

## Visão Geral

Módulo de orçamentos para o GestorAI ERP. Permite ao vendedor criar orçamentos com prazo de validade, enviá-los ao cliente via PDF ou WhatsApp, aprovar ou rejeitar conforme resposta do cliente, e converter orçamentos aprovados em vendas abertas com itens pré-preenchidos.

---

## Ciclo de Vida e Status

```
Rascunho → Enviado → Aprovado → Convertido
                   ↘ Rejeitado
                   ↘ Expirado  (lazy: DataValidade < hoje)
Rascunho → Cancelado
```

| De | Para | Gatilho |
|---|---|---|
| Rascunho | Enviado | Vendedor clica "Enviar" |
| Enviado | Aprovado | Vendedor registra aprovação do cliente |
| Enviado | Rejeitado | Vendedor registra rejeição |
| Enviado | Expirado | Sistema (lazy, em ListAsync/GetAsync) |
| Aprovado | Convertido | Vendedor clica "Converter em Venda" |
| Aprovado | Expirado | Sistema (lazy) |
| Rascunho | Cancelado | Vendedor cancela rascunho |

Status terminais: `Convertido`, `Rejeitado`, `Cancelado`, `Expirado`.

A expiração é **lazy** — não há job agendado. O status é atualizado no banco ao listar ou buscar um orçamento quando `DataValidade < DateTime.UtcNow.Date` e o status ainda permite expiração.

---

## Modelo de Dados

### Entidade `Orcamento`

| Campo | Tipo | Notas |
|---|---|---|
| Id | Guid | PK |
| EmpresaId | Guid | Multi-tenancy (HasQueryFilter) |
| ClienteId | Guid? | Opcional |
| Numero | int | Sequencial por empresa: MAX+1 no Create |
| Titulo | string | Ex: "Orçamento - Corte e Escova" |
| DataValidade | DateTime | Definida pelo vendedor |
| Status | OrcamentoStatus | Enum |
| Observacao | string? | Texto livre |
| VendaId | Guid? | Preenchido após conversão |
| CreatedAt | DateTime | UTC, setado no Create |

### Entidade `OrcamentoItem`

| Campo | Tipo | Notas |
|---|---|---|
| Id | Guid | PK |
| OrcamentoId | Guid | FK → Orcamento |
| Tipo | OrcamentoItemTipo | Enum: Produto \| Livre |
| ProdutoId | Guid? | Null quando Tipo == Livre |
| Descricao | string | Nome do produto ou descrição livre |
| Quantidade | decimal | |
| ValorUnitario | decimal | |

### Enums

```csharp
// OrcamentoStatus
Rascunho, Enviado, Aprovado, Convertido, Rejeitado, Cancelado, Expirado

// OrcamentoItemTipo
Produto, Livre
```

---

## Arquitetura Backend

### Novos arquivos

| Arquivo | Responsabilidade |
|---|---|
| `Domain/Entities/Orcamento.cs` | Entidade + propriedade calculada `Total` |
| `Domain/Entities/OrcamentoItem.cs` | Item de orçamento |
| `Domain/Enums/OrcamentoStatus.cs` | Enum de status |
| `Domain/Enums/OrcamentoItemTipo.cs` | Enum de tipo de item |
| `DTOs/Orcamentos/OrcamentoDto.cs` | Request/Response records |
| `Services/Orcamentos/OrcamentoService.cs` | Lógica de negócio |
| `Endpoints/OrcamentosEndpoints.cs` | Minimal API endpoints |

### Modificações

| Arquivo | Mudança |
|---|---|
| `Infrastructure/Data/AppDbContext.cs` | Adiciona `DbSet<Orcamento>` + `DbSet<OrcamentoItem>` + HasQueryFilter |
| `Program.cs` | Registra `OrcamentoService`, `IValidator<CreateOrcamentoRequest>`, `app.MapOrcamentos()` |

### Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | /orcamentos | Lista com filtro `?status=` e lazy-expire |
| GET | /orcamentos/{id} | Detalhe com lazy-expire |
| POST | /orcamentos | Cria como Rascunho |
| POST | /orcamentos/{id}/enviar | Rascunho → Enviado |
| POST | /orcamentos/{id}/aprovar | Enviado → Aprovado |
| POST | /orcamentos/{id}/rejeitar | Enviado → Rejeitado |
| POST | /orcamentos/{id}/cancelar | Rascunho → Cancelado |
| POST | /orcamentos/{id}/converter | Aprovado → Convertido + cria Venda |
| GET | /orcamentos/{id}/pdf | Retorna HTML para impressão |

### OrcamentoService — métodos principais

**`ConvertAsync`:**
1. Valida `Status == Aprovado`
2. Cria `Venda` com `Status = Aberta`, `ClienteId` copiado
3. Para cada item com `Tipo == Produto`: cria `ItemVenda` (sem baixar estoque — ocorre ao fechar a venda)
4. Itens `Livre` são ignorados na venda
5. Seta `Orcamento.VendaId` e `Status = Convertido`
6. Retorna a venda criada

**Lazy expire (aplicado em `ListAsync` e `GetAsync`):**
```csharp
var hoje = DateTime.UtcNow.Date;
var expirados = orcamentos
    .Where(o => o.DataValidade.Date < hoje
        && o.Status is OrcamentoStatus.Enviado or OrcamentoStatus.Aprovado)
    .ToList();
foreach (var o in expirados) o.Status = OrcamentoStatus.Expirado;
if (expirados.Count > 0) await db.SaveChangesAsync(ct);
```

**Tratamento de erros:**
- Converter não-Aprovado → `AppException("Orçamento não está aprovado.", 400)`
- Aprovar expirado → `AppException("Orçamento expirado não pode ser aprovado.", 400)`
- Transição inválida → `AppException("Transição de status inválida.", 400)`

---

## Arquitetura Frontend

### Novas rotas

| Rota | Componente | Descrição |
|---|---|---|
| `/orcamentos` | `Orcamentos.tsx` | Lista com filtro de status |
| `/orcamentos/novo` | `NovoOrcamento.tsx` | Formulário de criação |
| `/orcamentos/:id` | `DetalheOrcamento.tsx` | Detalhe + ações |

### Novos arquivos frontend

| Arquivo | Responsabilidade |
|---|---|
| `src/types/orcamento.ts` | Tipos TypeScript |
| `src/hooks/useOrcamentos.ts` | Listagem com filtro |
| `src/hooks/useOrcamento.ts` | Detalhe + mutações |
| `src/pages/orcamentos/Orcamentos.tsx` | Página lista |
| `src/pages/orcamentos/NovoOrcamento.tsx` | Página formulário |
| `src/pages/orcamentos/DetalheOrcamento.tsx` | Página detalhe |

### Sidebar

Adicionar entre "Histórico" e "Produtos":
```ts
{ to: '/orcamentos', icon: FileText, label: 'Orçamentos' }
```

### Tela Lista

- Tabela: Nº, Título, Cliente, Validade, Total, Status (badge colorido)
- Chips de filtro por status no topo
- Botão de ação contextual por linha: "Enviar" (Rascunho), "Converter" (Aprovado), "Ver" (demais)
- Botão "Novo Orçamento" no header

**Cores dos badges:**

| Status | Cor |
|---|---|
| Rascunho | cinza |
| Enviado | azul |
| Aprovado | verde |
| Convertido | roxo |
| Rejeitado | vermelho |
| Cancelado | vermelho claro |
| Expirado | laranja |

### Tela Formulário (Novo Orçamento)

- Título (obrigatório)
- Seletor de cliente (opcional, combobox)
- Data de validade (date picker)
- Observação (textarea)
- Tabela de itens:
  - Linha "Produto do estoque": busca produto, auto-preenche descrição + valor, quantidade editável
  - Linha "Item livre": descrição, quantidade, valor unitário — todos manuais
  - Botão remover por linha
- Totalizador em tempo real no rodapé
- Salva como Rascunho

### Tela Detalhe

Barra de ações conforme status:

| Status | Ações disponíveis |
|---|---|
| Rascunho | Enviar, Cancelar |
| Enviado | Aprovar, Rejeitar, WhatsApp (se cliente tem telefone), PDF |
| Aprovado | Converter em Venda, PDF |
| Convertido | PDF, link para a Venda |
| Demais | PDF |

- Banner vermelho se `Expirado`
- Após "Converter": redireciona para `/vendas/nova?vendaId={id}` (venda pré-preenchida)

### WhatsApp

Gerado no frontend, sem chamada ao backend:

```ts
const msg = encodeURIComponent(
  `Olá ${cliente.nome}! Segue o Orçamento ORC-${String(numero).padStart(3,'0')}: "${titulo}"\n` +
  `Total: R$ ${total} | Válido até: ${dataValidade}`
)
const url = `https://wa.me/${cliente.telefone.replace(/\D/g,'')}?text=${msg}`
```

Botão desabilitado com tooltip se cliente não tem telefone.

### PDF

- `GET /orcamentos/{id}/pdf` retorna HTML
- Frontend abre em nova aba → `window.print()`
- Aproveita o `@media print` já configurado em `index.css`

---

## Testes

### Backend (xUnit + InMemory)

- `OrcamentoServiceTests`: 6 testes
  1. `CreateAsync_DeveRetornarRascunho`
  2. `ListAsync_DeveExpirarOrcamentosVencidos`
  3. `ConvertAsync_DevecriarVendaComItens`
  4. `ConvertAsync_DeveIgnorarItensLivres`
  5. `ConvertAsync_QuandoNaoAprovado_DeveLancarExcecao`
  6. `AprovarAsync_QuandoExpirado_DeveLancarExcecao`

### Frontend (Vitest)

- `NovoOrcamento.test.tsx`: renderiza formulário, adiciona item produto + item livre, verifica totalizador
- `DetalheOrcamento.test.tsx`: botões corretos por status (Rascunho mostra Enviar, Aprovado mostra Converter)
