# Melhorias UX — Dashboard, Clientes CRUD, Lançamentos Editáveis, Inline Cliente em Venda

## Goal

Quatro melhorias incrementais de UX/funcionalidade: reorganizar o dashboard com painéis visuais, completar o CRUD de clientes (editar + excluir), tornar lançamentos financeiros e histórico de vendas editáveis, e permitir criar um novo cliente diretamente na tela de nova venda.

## Architecture

Nenhuma entidade nova. Todas as mudanças são incrementais sobre os serviços e endpoints existentes. Backend: novos endpoints PUT/DELETE onde faltam, um novo endpoint de resumo para lançamentos. Frontend: reorganização de layout no Dashboard e adição de botões de ação em tabelas existentes com modais de edição.

## Tech Stack

.NET 10 Minimal APIs, EF Core 10, PostgreSQL, React + TypeScript + Tailwind CSS, xUnit

---

## 1. Dashboard Reorganizado

### O que muda

Apenas `frontend/src/pages/Dashboard.tsx` — zero mudanças de backend.

### Layout alvo

**Painel "Vendas"** — `rounded-xl border bg-card p-4` com título "Vendas", grid 2-col interno:
- Vendido hoje
- Vendido no mês

**Painel "Financeiro"** — mesmo estilo, grid 3-col interno:
- Saldo do mês (Receitas − Despesas pagas)
- A receber (cobranças pendentes)
- Contas vencidas (vermelho se > 0)

**Removidos** (eram cards soltos sem painel): Lucro estimado, A pagar próx. 7 dias, Entradas separado, Saídas separado — informação redundante com os painéis acima.

**Zona de gráficos** — dois gráficos lado a lado (`grid grid-cols-2 gap-4`):
- Esquerda: `GraficoVendas` (já existe)
- Direita: `GraficoFluxo` (já existe)

**Alerta de estoque** — mantido, repositicionado acima dos gráficos.

**Top Produtos** — mantido abaixo dos gráficos.

### KpiCard atual

`KpiCard` já aceita `value`, `label`, `icon`, `color`. Nenhuma mudança no componente — apenas reorganização de uso em `Dashboard.tsx`.

---

## 2. Clientes CRUD — Editar + Excluir

### Backend

**`PUT /api/clientes/{id}`** — já existe em `ClientesEndpoints.cs:25` e `ClienteService.UpdateAsync`. Nenhuma mudança necessária.

**`DELETE /api/clientes/{id}`** — novo endpoint + novo método de serviço.

`ClienteService.DeleteAsync(Guid id, CancellationToken ct)`:
```csharp
public async Task DeleteAsync(Guid id, CancellationToken ct)
{
    var cliente = await db.Clientes
        .FirstOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new AppException("Cliente não encontrado.", 404);

    var temVinculos =
        await db.Contratos.AnyAsync(c => c.ClienteId == id, ct) ||
        await db.Cobrancas.AnyAsync(c => c.ClienteId == id, ct) ||
        await db.Orcamentos.AnyAsync(o => o.ClienteId == id, ct) ||
        await db.Vendas.AnyAsync(v => v.ClienteId == id, ct);

    if (temVinculos)
        throw new AppException(
            "Este cliente possui dados vinculados (contratos, cobranças, orçamentos ou vendas) e não pode ser excluído.", 400);

    db.Clientes.Remove(cliente);
    await db.SaveChangesAsync(ct);
}
```

Endpoint em `ClientesEndpoints.cs`:
```csharp
group.MapDelete("/{id:guid}", async (Guid id, ClienteService svc, CancellationToken ct) =>
{
    await svc.DeleteAsync(id, ct);
    return Results.NoContent();
});
```

### Frontend

**`useClientes.ts`** — adicionar `remove`:
```typescript
const remove = useCallback(async (id: string) => {
  await api.delete(`/api/clientes/${id}`)
}, [])
// adicionar ao return: remove
```

**`Clientes.tsx`** — cada linha da tabela ganha dois ícones de ação:
- Lápis (`Pencil` de lucide-react) — abre modal com `ClienteForm` preenchido, chama `update(id, req)`, recarrega lista
- Lixeira (`Trash2`) — abre `useConfirm`, chama `remove(id)`, recarrega lista; toast.error se backend retornar 400 (vínculos)

```tsx
<td className="px-4 py-3">
  <div className="flex gap-1">
    <Button size="sm" variant="ghost" onClick={e => { e.stopPropagation(); setEditando(c) }}>
      <Pencil size={14} />
    </Button>
    <Button size="sm" variant="ghost" className="text-destructive" onClick={e => { e.stopPropagation(); handleRemove(c.id) }}>
      <Trash2 size={14} />
    </Button>
  </div>
</td>
```

Modal de edição — reaproveita `ClienteForm` existente, pré-preenchido com dados do cliente selecionado (`editando`).

### Testes

`backend/tests/GestorAI.Tests/Services/ClienteDeleteServiceTests.cs`:
- `DeleteAsync_RemoveCliente_QuandoSemVinculos`
- `DeleteAsync_LancaExcecao_QuandoTemVinculos`

---

## 3. Lançamentos Financeiros — KPIs + Editável

### Backend — KPIs

Novo DTO em `LancamentoDto.cs`:
```csharp
public record LancamentoResumo(
    decimal TotalReceitasMes,
    decimal TotalDespesasMes,
    decimal SaldoMes,
    decimal TotalPendente);
```

`LancamentoService.GetResumoAsync(CancellationToken ct)`:
- `TotalReceitasMes` — `SumAsync` de lançamentos `Pago`, `Tipo == Receita`, `DataPagamento >= inicioMes` (mesmo padrão de `CobrancaService.GetResumoAsync`)
- `TotalDespesasMes` — idem com `Tipo == Despesa`
- `SaldoMes` — `TotalReceitasMes - TotalDespesasMes`
- `TotalPendente` — `SumAsync` de lançamentos `Pendente` com `DataVencimento >= hoje`

Endpoint: `GET /api/lancamentos/resumo` em `FinanceiroEndpoints.cs`.

### Backend — Editar

Novo DTO:
```csharp
public record UpdateLancamentoRequest(
    string Tipo,
    string Descricao,
    decimal Valor,
    DateTime DataVencimento,
    string Categoria,
    string? Observacao);
```

`LancamentoService.UpdateAsync(Guid id, UpdateLancamentoRequest req, CancellationToken ct)`:
```csharp
public async Task<LancamentoResponse> UpdateAsync(Guid id, UpdateLancamentoRequest req, CancellationToken ct)
{
    var l = await db.Lancamentos
        .FirstOrDefaultAsync(x => x.Id == id, ct)
        ?? throw new AppException("Lançamento não encontrado.", 404);

    if (l.Status != StatusLancamento.Pendente)
        throw new AppException("Apenas lançamentos pendentes podem ser editados.", 400);

    if (l.VendaId.HasValue)
        throw new AppException("Lançamentos gerados por vendas não podem ser editados.", 400);

    if (!Enum.TryParse<TipoLancamento>(req.Tipo, out var tipo))
        throw new AppException($"Tipo inválido: {req.Tipo}.", 400);

    l.Tipo = tipo;
    l.Descricao = req.Descricao;
    l.Valor = req.Valor;
    l.DataVencimento = req.DataVencimento;
    l.Categoria = req.Categoria;
    l.Observacao = req.Observacao;
    await db.SaveChangesAsync(ct);

    return await GetAsync(id, ct);
}
```

Endpoint: `PUT /api/lancamentos/{id:guid}` em `FinanceiroEndpoints.cs`.

### Frontend

**`useFinanceiro.ts`** — adicionar `update` e `fetchResumo`:
```typescript
const update = useCallback(async (id: string, req: UpdateLancamentoRequest) => {
  const result = await api.put<LancamentoResponse>(`/api/lancamentos/${id}`, req)
  return result
}, [])

const fetchResumo = useCallback(async (): Promise<LancamentoResumo> => {
  return api.get<LancamentoResumo>('/api/lancamentos/resumo')
}, [])
```

**`Lancamentos.tsx`**:
- Painel de KPIs no topo (mesmo estilo de `ResumoCards`) com 4 cards: Receitas do mês, Despesas do mês, Saldo, Pendentes
- Botão de editar (ícone `Pencil`) por linha quando `status === 'Pendente'` e `vendaId == null`
- Modal de edição com `LancamentoForm` existente pré-preenchido

### Testes

`backend/tests/GestorAI.Tests/Services/LancamentoUpdateServiceTests.cs`:
- `UpdateAsync_AtualizaLancamento_QuandoPendente`
- `UpdateAsync_LancaExcecao_QuandoPago`
- `UpdateAsync_LancaExcecao_QuandoVinculadoAVenda`

`backend/tests/GestorAI.Tests/Services/LancamentoResumoServiceTests.cs`:
- `GetResumoAsync_ContabilizaTotaisCorretamente`

---

## 4. Vendas Histórico — Editável

### Backend

**`VendaResponse` e `VendaListItem`** — adicionar `ClienteId?` aos dois records:
```csharp
public record VendaResponse(
    Guid Id,
    Guid? ClienteId,   // novo
    string? ClienteNome,
    // ... restante igual
);

public record VendaListItem(
    Guid Id,
    Guid? ClienteId,   // novo
    string? ClienteNome,
    // ... restante igual
);
```

`VendaService.GetAsync` — incluir `venda.ClienteId` no `new VendaResponse(...)`.
`VendaService.ListAsync` — incluir `v.ClienteId` no `new VendaListItem(...)`.

**Novo DTO:**
```csharp
public record UpdateVendaRequest(
    Guid? ClienteId,
    string FormaPagamento,
    DateTime DataHora);
```

**`VendaService.UpdateAsync`:**
```csharp
public async Task<VendaResponse> UpdateAsync(Guid id, UpdateVendaRequest req, CancellationToken ct)
{
    var venda = await db.Vendas
        .Include(v => v.Cliente)
        .FirstOrDefaultAsync(v => v.Id == id, ct)
        ?? throw new AppException("Venda não encontrada.", 404);

    if (venda.Status != StatusVenda.Concluida)
        throw new AppException("Apenas vendas concluídas podem ser editadas.", 400);

    if (!Enum.TryParse<FormaPagamento>(req.FormaPagamento, out var forma))
        throw new AppException($"FormaPagamento inválida: {req.FormaPagamento}.", 400);

    venda.ClienteId = req.ClienteId;
    venda.FormaPagamento = forma;
    venda.DataHora = req.DataHora;

    // atualiza lançamento vinculado (descrição e data)
    var lancamento = await db.Lancamentos
        .FirstOrDefaultAsync(l => l.VendaId == id, ct);
    if (lancamento is not null)
    {
        var nomeCliente = req.ClienteId.HasValue
            ? (await db.Clientes.FindAsync([req.ClienteId.Value], ct))?.Nome ?? "Cliente"
            : "Venda balcão";
        lancamento.Descricao = $"Venda — {nomeCliente}";
        lancamento.DataVencimento = req.DataHora;
        lancamento.DataPagamento = req.DataHora;
    }

    await db.SaveChangesAsync(ct);
    return await GetAsync(id, ct);
}
```

Endpoint: `PUT /api/vendas/{id:guid}` em `VendasEndpoints.cs`.

### Frontend

**`types/venda.ts`** — adicionar `clienteId?: string` a `VendaResponse` e `VendaListItem`.

**`useVendas.ts`** — adicionar `update`:
```typescript
const update = useCallback(async (id: string, req: { clienteId?: string; formaPagamento: string; dataHora: string }) => {
  return api.put<VendaResponse>(`/api/vendas/${id}`, req)
}, [])
```

**`Historico.tsx`** — botão de editar por linha com status `Concluida`. Abre mini-modal com 3 campos:
- Seletor de cliente (opcional, lista existente de clientes)
- Forma de pagamento (select: Pix, Dinheiro, Cartão, Outro)
- Data da venda (date input)

Ao salvar, chama `update(id, req)` e recarrega a lista.

### Testes

`backend/tests/GestorAI.Tests/Services/VendaUpdateServiceTests.cs`:
- `UpdateAsync_AtualizaVenda_QuandoConcluida`
- `UpdateAsync_LancaExcecao_QuandoCancelada`
- `UpdateAsync_AtualizaLancamentoVinculado`

---

## 5. Inline Cliente em Venda

### Backend

Nenhuma mudança — reutiliza `POST /api/clientes` existente.

### Frontend

**`NovaVenda.tsx`** — ao lado do `<select>` de cliente, adicionar botão `+`:
```tsx
<div className="flex gap-2 items-end">
  <div className="flex-1">
    <label className={labelClass}>Cliente</label>
    <select value={clienteId} onChange={e => setClienteId(e.target.value)} className={inputClass}>
      <option value="">Balcão</option>
      {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
    </select>
  </div>
  <Button type="button" variant="outline" size="icon"
    onClick={() => setModalNovoCliente(true)}
    title="Criar novo cliente">
    <Plus size={16} />
  </Button>
</div>
```

Estado e handler:
```typescript
const [modalNovoCliente, setModalNovoCliente] = useState(false)
```

Modal com `ClienteForm` existente. Ao salvar:
1. Chama `create(req)` de `useClientes`
2. Recarrega lista de clientes (`listClientes()`)
3. Seleciona automaticamente o novo cliente (`setClienteId(novoCliente.id)`)
4. Fecha o modal

---

## Checklist final

- [ ] `DELETE /api/clientes/{id}` — bloqueia se tiver vínculos
- [ ] Editar/excluir na tela de Clientes
- [ ] `GET /api/lancamentos/resumo` — 4 KPIs
- [ ] `PUT /api/lancamentos/{id}` — só Pendente sem VendaId
- [ ] KPIs + botão editar em Lançamentos
- [ ] `ClienteId` em `VendaResponse` / `VendaListItem`
- [ ] `PUT /api/vendas/{id}` — atualiza venda + lançamento vinculado
- [ ] Botão editar em Histórico de Vendas (modal 3 campos)
- [ ] Dashboard reorganizado em painéis
- [ ] Botão `+` inline para criar cliente em NovaVenda
- [ ] `dotnet test` full suite passando
- [ ] `npx tsc --noEmit` sem erros
