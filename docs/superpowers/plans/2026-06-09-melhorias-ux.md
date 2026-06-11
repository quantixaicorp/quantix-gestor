# Melhorias UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dashboard em painéis organizados, CRUD completo de clientes, lançamentos financeiros editáveis com KPIs, histórico de vendas editável, e criação inline de cliente na tela de venda.

**Architecture:** Oito tarefas incrementais — quatro de backend (novos endpoints + services) e quatro de frontend (reorganização de layout + modais de edição). Nenhuma entidade nova. Todas as mudanças seguem os padrões existentes: `Service(db, tenantContext)`, `SumAsync` server-side, `Setup()` em testes, modais com componentes reutilizáveis.

**Tech Stack:** .NET 10 Minimal APIs, EF Core 10, PostgreSQL, React + TypeScript + Tailwind CSS, xUnit

---

## File Map

**Backend — modificar:**
- `backend/src/GestorAI.API/Services/Clientes/ClienteService.cs` — adicionar `DeleteAsync`
- `backend/src/GestorAI.API/Endpoints/ClientesEndpoints.cs` — adicionar `DELETE /{id:guid}`
- `backend/src/GestorAI.API/DTOs/Financeiro/LancamentoDto.cs` — adicionar `LancamentoResumo`, `UpdateLancamentoRequest`
- `backend/src/GestorAI.API/Services/Financeiro/LancamentoService.cs` — adicionar `GetResumoAsync`, `UpdateAsync`
- `backend/src/GestorAI.API/Endpoints/FinanceiroEndpoints.cs` — adicionar `GET /lancamentos/resumo`, `PUT /lancamentos/{id}`
- `backend/src/GestorAI.API/DTOs/Vendas/VendaDto.cs` — adicionar `ClienteId` em `VendaResponse` e `VendaListItem`, adicionar `UpdateVendaRequest`
- `backend/src/GestorAI.API/Services/Vendas/VendaService.cs` — adicionar `ClienteId` ao `GetAsync`/`ListAsync`, adicionar `UpdateAsync`
- `backend/src/GestorAI.API/Endpoints/VendasEndpoints.cs` — adicionar `PUT /{id:guid}`

**Backend — criar (testes):**
- `backend/tests/GestorAI.Tests/Services/ClienteDeleteServiceTests.cs`
- `backend/tests/GestorAI.Tests/Services/LancamentoResumoServiceTests.cs`
- `backend/tests/GestorAI.Tests/Services/LancamentoUpdateServiceTests.cs`
- `backend/tests/GestorAI.Tests/Services/VendaUpdateServiceTests.cs`

**Frontend — modificar:**
- `frontend/src/pages/Dashboard.tsx` — reorganizar em painéis
- `frontend/src/hooks/useClientes.ts` — adicionar `remove`
- `frontend/src/pages/clientes/Clientes.tsx` — adicionar editar/excluir
- `frontend/src/types/financeiro.ts` — adicionar `UpdateLancamentoRequest`, `LancamentoResumo`
- `frontend/src/hooks/useFinanceiro.ts` — adicionar `update`, `fetchResumo`
- `frontend/src/components/financeiro/LancamentoForm.tsx` — adicionar `defaultValues` para edição
- `frontend/src/pages/financeiro/Lancamentos.tsx` — adicionar KPIs + botão editar
- `frontend/src/types/vendas.ts` — adicionar `clienteId?` em `VendaResponse`/`VendaListItem`, adicionar `UpdateVendaRequest`
- `frontend/src/hooks/useVendas.ts` — adicionar `update`
- `frontend/src/pages/vendas/Historico.tsx` — adicionar botão editar + modal
- `frontend/src/pages/vendas/NovaVenda.tsx` — adicionar botão `+` e modal inline de cliente

---

### Task 1: Backend — `DELETE /api/clientes/{id}`

**Files:**
- Modify: `backend/src/GestorAI.API/Services/Clientes/ClienteService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/ClientesEndpoints.cs`
- Create: `backend/tests/GestorAI.Tests/Services/ClienteDeleteServiceTests.cs`

- [ ] **Step 1: Escrever teste falhando**

Criar `backend/tests/GestorAI.Tests/Services/ClienteDeleteServiceTests.cs`:

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ClienteDeleteServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ClienteService service) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new ClienteService(db, tc));
    }

    [Fact]
    public async Task DeleteAsync_RemoveCliente_QuandoSemVinculos()
    {
        var (db, svc) = Setup();
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Ana", Whatsapp = "11999990000" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        await svc.DeleteAsync(cliente.Id, default);

        var encontrado = await db.Clientes.IgnoreQueryFilters().FindAsync(cliente.Id);
        Assert.Null(encontrado);
    }

    [Fact]
    public async Task DeleteAsync_LancaExcecao_QuandoTemVenda()
    {
        var (db, svc) = Setup();
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Bob", Whatsapp = "11888880000" };
        db.Clientes.Add(cliente);
        db.Vendas.Add(new Venda
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            Status = StatusVenda.Concluida,
            FormaPagamento = FormaPagamento.Pix,
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.DeleteAsync(cliente.Id, default));
    }
}
```

- [ ] **Step 2: Rodar para confirmar falha**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test tests/GestorAI.Tests --filter "ClienteDeleteServiceTests" --no-build 2>&1 | tail -15
```

Esperado: erro de compilação — `ClienteService` não tem `DeleteAsync`.

- [ ] **Step 3: Adicionar `DeleteAsync` em `ClienteService.cs`**

Adicionar ao final da classe, antes do último `}`:

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

- [ ] **Step 4: Adicionar endpoint em `ClientesEndpoints.cs`**

Adicionar ao final do método `MapClientes`, dentro de `var group = ...`:

```csharp
group.MapDelete("/{id:guid}", async (Guid id, ClienteService svc, CancellationToken ct) =>
{
    await svc.DeleteAsync(id, ct);
    return Results.NoContent();
});
```

- [ ] **Step 5: Build e testes**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet build src/GestorAI.API --no-restore 2>&1 | tail -10
dotnet test tests/GestorAI.Tests --filter "ClienteDeleteServiceTests" 2>&1 | tail -15
```

Esperado: 2 testes PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Services/Clientes/ClienteService.cs \
        backend/src/GestorAI.API/Endpoints/ClientesEndpoints.cs \
        backend/tests/GestorAI.Tests/Services/ClienteDeleteServiceTests.cs
git commit -m "feat: add DELETE /api/clientes/{id} with vinculos guard"
```

---

### Task 2: Backend — `GET /api/lancamentos/resumo` + `PUT /api/lancamentos/{id}`

**Files:**
- Modify: `backend/src/GestorAI.API/DTOs/Financeiro/LancamentoDto.cs`
- Modify: `backend/src/GestorAI.API/Services/Financeiro/LancamentoService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/FinanceiroEndpoints.cs`
- Create: `backend/tests/GestorAI.Tests/Services/LancamentoResumoServiceTests.cs`
- Create: `backend/tests/GestorAI.Tests/Services/LancamentoUpdateServiceTests.cs`

- [ ] **Step 1: Escrever testes falhando**

Criar `backend/tests/GestorAI.Tests/Services/LancamentoResumoServiceTests.cs`:

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class LancamentoResumoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, LancamentoService svc) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new LancamentoService(db, tc));
    }

    [Fact]
    public async Task GetResumoAsync_ContabilizaTotaisCorretamente()
    {
        var (db, svc) = Setup();
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Receita paga no mês → TotalReceitasMes
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Receita,
            Descricao = "Receita", Valor = 500m,
            DataVencimento = hoje, Status = StatusLancamento.Pago,
            DataPagamento = inicioMes.AddDays(2), Categoria = "Serviço",
        });
        // Despesa paga no mês → TotalDespesasMes
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "Despesa", Valor = 200m,
            DataVencimento = hoje, Status = StatusLancamento.Pago,
            DataPagamento = inicioMes.AddDays(1), Categoria = "Aluguel",
        });
        // Pendente com vencimento futuro → TotalPendente
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "A pagar", Valor = 100m,
            DataVencimento = hoje.AddDays(5), Status = StatusLancamento.Pendente,
            Categoria = "Fornecedor",
        });
        // Cancelado — não entra em nada
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Receita,
            Descricao = "Cancelado", Valor = 999m,
            DataVencimento = hoje, Status = StatusLancamento.Cancelado,
            Categoria = "Outros",
        });
        await db.SaveChangesAsync();

        var resumo = await svc.GetResumoAsync(default);

        Assert.Equal(500m, resumo.TotalReceitasMes);
        Assert.Equal(200m, resumo.TotalDespesasMes);
        Assert.Equal(300m, resumo.SaldoMes);
        Assert.Equal(100m, resumo.TotalPendente);
    }
}
```

Criar `backend/tests/GestorAI.Tests/Services/LancamentoUpdateServiceTests.cs`:

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class LancamentoUpdateServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, LancamentoService svc) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new LancamentoService(db, tc));
    }

    private async Task<Lancamento> SeedAsync(AppDbContext db, StatusLancamento status, Guid? vendaId = null)
    {
        var l = new Lancamento
        {
            EmpresaId = _empresaId,
            Tipo = TipoLancamento.Despesa,
            Descricao = "Original",
            Valor = 100m,
            DataVencimento = DateTime.UtcNow.AddDays(5),
            Status = status,
            Categoria = "Aluguel",
            VendaId = vendaId,
        };
        db.Lancamentos.Add(l);
        await db.SaveChangesAsync();
        return l;
    }

    [Fact]
    public async Task UpdateAsync_AtualizaLancamento_QuandoPendente()
    {
        var (db, svc) = Setup();
        var l = await SeedAsync(db, StatusLancamento.Pendente);
        var req = new UpdateLancamentoRequest(
            "Receita", "Novo nome", 250m,
            DateTime.UtcNow.AddDays(10), "Serviço", "Obs");

        var result = await svc.UpdateAsync(l.Id, req, default);

        Assert.Equal("Receita", result.Tipo);
        Assert.Equal("Novo nome", result.Descricao);
        Assert.Equal(250m, result.Valor);
        Assert.Equal("Obs", result.Observacao);
    }

    [Fact]
    public async Task UpdateAsync_LancaExcecao_QuandoPago()
    {
        var (db, svc) = Setup();
        var l = await SeedAsync(db, StatusLancamento.Pago);
        var req = new UpdateLancamentoRequest("Despesa", "Qualquer", 50m,
            DateTime.UtcNow, "Outros", null);

        await Assert.ThrowsAsync<AppException>(() => svc.UpdateAsync(l.Id, req, default));
    }

    [Fact]
    public async Task UpdateAsync_LancaExcecao_QuandoVinculadoAVenda()
    {
        var (db, svc) = Setup();
        var l = await SeedAsync(db, StatusLancamento.Pendente, vendaId: Guid.NewGuid());
        var req = new UpdateLancamentoRequest("Despesa", "Qualquer", 50m,
            DateTime.UtcNow, "Outros", null);

        await Assert.ThrowsAsync<AppException>(() => svc.UpdateAsync(l.Id, req, default));
    }
}
```

- [ ] **Step 2: Rodar para confirmar falha**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test tests/GestorAI.Tests --filter "LancamentoResumoServiceTests|LancamentoUpdateServiceTests" --no-build 2>&1 | tail -15
```

Esperado: erro de compilação.

- [ ] **Step 3: Adicionar DTOs em `LancamentoDto.cs`**

Adicionar ao final do arquivo `backend/src/GestorAI.API/DTOs/Financeiro/LancamentoDto.cs`:

```csharp
public record LancamentoResumo(
    decimal TotalReceitasMes,
    decimal TotalDespesasMes,
    decimal SaldoMes,
    decimal TotalPendente);

public record UpdateLancamentoRequest(
    string Tipo,
    string Descricao,
    decimal Valor,
    DateTime DataVencimento,
    string Categoria,
    string? Observacao);
```

- [ ] **Step 4: Adicionar `GetResumoAsync` e `UpdateAsync` em `LancamentoService.cs`**

Adicionar antes do método `private static LancamentoResponse ToResponse(...)` ao final da classe:

```csharp
public async Task<LancamentoResumo> GetResumoAsync(CancellationToken ct)
{
    var hoje = DateTime.UtcNow.Date;
    var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    var totalReceitas = await db.Lancamentos
        .Where(l => l.Status == StatusLancamento.Pago
                 && l.Tipo == TipoLancamento.Receita
                 && l.DataPagamento.HasValue
                 && l.DataPagamento.Value >= inicioMes)
        .SumAsync(l => (decimal?)l.Valor, ct) ?? 0m;

    var totalDespesas = await db.Lancamentos
        .Where(l => l.Status == StatusLancamento.Pago
                 && l.Tipo == TipoLancamento.Despesa
                 && l.DataPagamento.HasValue
                 && l.DataPagamento.Value >= inicioMes)
        .SumAsync(l => (decimal?)l.Valor, ct) ?? 0m;

    var totalPendente = await db.Lancamentos
        .Where(l => l.Status == StatusLancamento.Pendente
                 && l.DataVencimento.Date >= hoje)
        .SumAsync(l => (decimal?)l.Valor, ct) ?? 0m;

    return new LancamentoResumo(totalReceitas, totalDespesas, totalReceitas - totalDespesas, totalPendente);
}

public async Task<LancamentoResponse> UpdateAsync(Guid id, UpdateLancamentoRequest req, CancellationToken ct)
{
    var l = await db.Lancamentos.FindAsync([id], ct)
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

- [ ] **Step 5: Adicionar endpoints em `FinanceiroEndpoints.cs`**

No método `MapFinanceiro`, adicionar após o `group.MapGet("/lancamentos/{id:guid}", ...)`:

```csharp
group.MapGet("/lancamentos/resumo", async (
    LancamentoService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetResumoAsync(ct)));

group.MapPut("/lancamentos/{id:guid}", async (
    Guid id, UpdateLancamentoRequest req, LancamentoService svc, CancellationToken ct) =>
    Results.Ok(await svc.UpdateAsync(id, req, ct)));
```

**Atenção:** `GET /lancamentos/resumo` deve ser registrado **antes** de `GET /lancamentos/{id:guid}` para não conflitar. Verificar a ordem — mover `/resumo` para antes do `/{id:guid}` se necessário.

- [ ] **Step 6: Build e testes**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet build src/GestorAI.API --no-restore 2>&1 | tail -10
dotnet test tests/GestorAI.Tests --filter "LancamentoResumoServiceTests|LancamentoUpdateServiceTests" 2>&1 | tail -20
```

Esperado: 4 testes PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Financeiro/LancamentoDto.cs \
        backend/src/GestorAI.API/Services/Financeiro/LancamentoService.cs \
        backend/src/GestorAI.API/Endpoints/FinanceiroEndpoints.cs \
        backend/tests/GestorAI.Tests/Services/LancamentoResumoServiceTests.cs \
        backend/tests/GestorAI.Tests/Services/LancamentoUpdateServiceTests.cs
git commit -m "feat: add GET /lancamentos/resumo and PUT /lancamentos/{id}"
```

---

### Task 3: Backend — `ClienteId` em `VendaResponse` + `PUT /api/vendas/{id}`

**Files:**
- Modify: `backend/src/GestorAI.API/DTOs/Vendas/VendaDto.cs`
- Modify: `backend/src/GestorAI.API/Services/Vendas/VendaService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/VendasEndpoints.cs`
- Create: `backend/tests/GestorAI.Tests/Services/VendaUpdateServiceTests.cs`

- [ ] **Step 1: Escrever teste falhando**

Criar `backend/tests/GestorAI.Tests/Services/VendaUpdateServiceTests.cs`:

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Vendas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Vendas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class VendaUpdateServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, VendaService svc) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new VendaService(db, tc));
    }

    private async Task<(Venda venda, Lancamento lancamento, Cliente cliente)> SeedAsync(AppDbContext db)
    {
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Carlos", Whatsapp = "11999990001" };
        db.Clientes.Add(cliente);

        var venda = new Venda
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            Status = StatusVenda.Concluida,
            FormaPagamento = FormaPagamento.Pix,
            DataHora = DateTime.UtcNow.AddDays(-1),
            Total = 100m,
        };
        db.Vendas.Add(venda);
        await db.SaveChangesAsync();

        var lancamento = new Lancamento
        {
            EmpresaId = _empresaId,
            Tipo = TipoLancamento.Receita,
            Descricao = $"Venda — {cliente.Nome}",
            Valor = 100m,
            DataVencimento = venda.DataHora,
            DataPagamento = venda.DataHora,
            Status = StatusLancamento.Pago,
            Categoria = "Venda",
            VendaId = venda.Id,
        };
        db.Lancamentos.Add(lancamento);
        await db.SaveChangesAsync();

        return (venda, lancamento, cliente);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaVenda_QuandoConcluida()
    {
        var (db, svc) = Setup();
        var (venda, _, _) = await SeedAsync(db);
        var novaData = DateTime.UtcNow.AddDays(-2);
        var req = new UpdateVendaRequest(null, "Dinheiro", novaData);

        var result = await svc.UpdateAsync(venda.Id, req, default);

        Assert.Null(result.ClienteId);
        Assert.Equal("Dinheiro", result.FormaPagamento);
        Assert.Equal(novaData.Date, result.DataHora.Date);
    }

    [Fact]
    public async Task UpdateAsync_LancaExcecao_QuandoCancelada()
    {
        var (db, svc) = Setup();
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "X", Whatsapp = "1" };
        db.Clientes.Add(cliente);
        var venda = new Venda
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Status = StatusVenda.Cancelada, FormaPagamento = FormaPagamento.Pix,
        };
        db.Vendas.Add(venda);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(
            () => svc.UpdateAsync(venda.Id, new UpdateVendaRequest(null, "Pix", DateTime.UtcNow), default));
    }

    [Fact]
    public async Task UpdateAsync_AtualizaLancamentoVinculado()
    {
        var (db, svc) = Setup();
        var (venda, lancamento, _) = await SeedAsync(db);
        var novaData = DateTime.UtcNow.AddDays(-3);
        var req = new UpdateVendaRequest(null, "Dinheiro", novaData);

        await svc.UpdateAsync(venda.Id, req, default);

        var lancAtualizado = await db.Lancamentos.IgnoreQueryFilters().FirstAsync(l => l.Id == lancamento.Id);
        Assert.Equal(novaData.Date, lancAtualizado.DataVencimento.Date);
        Assert.Equal("Venda balcão", lancAtualizado.Descricao);
    }
}
```

- [ ] **Step 2: Rodar para confirmar falha**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test tests/GestorAI.Tests --filter "VendaUpdateServiceTests" --no-build 2>&1 | tail -15
```

Esperado: erro de compilação — `UpdateVendaRequest` não existe, `VendaResponse` não tem `ClienteId`.

- [ ] **Step 3: Atualizar `VendaDto.cs`**

Substituir o conteúdo de `backend/src/GestorAI.API/DTOs/Vendas/VendaDto.cs` por (adicionar `Guid? ClienteId` como 2º parâmetro em `VendaResponse` e `VendaListItem`, adicionar `UpdateVendaRequest`):

```csharp
namespace GestorAI.API.DTOs.Vendas;

public record ItemVendaRequest(Guid ProdutoId, decimal Quantidade, decimal Desconto);

public record CreateVendaRequest(
    Guid? ClienteId,
    List<ItemVendaRequest> Itens,
    decimal Desconto,
    string FormaPagamento,
    int? Parcelas,
    string? Observacao,
    DateTime? DataHora = null);

public record ItemVendaResponse(
    Guid ProdutoId,
    string ProdutoNome,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal Desconto,
    decimal Total);

public record VendaResponse(
    Guid Id,
    Guid? ClienteId,
    string? ClienteNome,
    DateTime DataHora,
    string Status,
    decimal Subtotal,
    decimal Desconto,
    decimal Total,
    string FormaPagamento,
    int? Parcelas,
    string? Observacao,
    List<ItemVendaResponse> Itens);

public record VendaListItem(
    Guid Id,
    Guid? ClienteId,
    string? ClienteNome,
    DateTime DataHora,
    string Status,
    decimal Total,
    string FormaPagamento);

public record FecharVendaRequest(string FormaPagamento, int? Parcelas, string? Observacao);

public record UpdateVendaRequest(
    Guid? ClienteId,
    string FormaPagamento,
    DateTime DataHora);
```

- [ ] **Step 4: Atualizar `VendaService.cs` — adicionar `ClienteId` ao `GetAsync` e `ListAsync`**

Em `GetAsync`, substituir o `return new VendaResponse(...)`:

```csharp
return new VendaResponse(
    venda.Id,
    venda.ClienteId,
    venda.Cliente?.Nome,
    venda.DataHora,
    venda.Status.ToString(),
    venda.Subtotal,
    venda.Desconto,
    venda.Total,
    venda.FormaPagamento.ToString(),
    venda.Parcelas,
    venda.Observacao,
    venda.Itens.Select(i => new ItemVendaResponse(
        i.ProdutoId, i.Produto?.Nome ?? "",
        i.Quantidade, i.PrecoUnitario,
        i.Desconto, i.Total)).ToList());
```

Em `ListAsync`, substituir o `.Select(v => new VendaListItem(...))`:

```csharp
.Select(v => new VendaListItem(
    v.Id, v.ClienteId,
    v.Cliente != null ? v.Cliente.Nome : null,
    v.DataHora, v.Status.ToString(),
    v.Total, v.FormaPagamento.ToString()))
```

- [ ] **Step 5: Adicionar `UpdateAsync` em `VendaService.cs`**

Adicionar antes do método `GetAsync`:

```csharp
public async Task<VendaResponse> UpdateAsync(Guid id, UpdateVendaRequest req, CancellationToken ct)
{
    var venda = await db.Vendas
        .FirstOrDefaultAsync(v => v.Id == id, ct)
        ?? throw new AppException("Venda não encontrada.", 404);

    if (venda.Status != StatusVenda.Concluida)
        throw new AppException("Apenas vendas concluídas podem ser editadas.", 400);

    if (!Enum.TryParse<FormaPagamento>(req.FormaPagamento, out var forma))
        throw new AppException($"FormaPagamento inválida: {req.FormaPagamento}.", 400);

    venda.ClienteId = req.ClienteId;
    venda.FormaPagamento = forma;
    venda.DataHora = req.DataHora;

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

- [ ] **Step 6: Adicionar endpoint em `VendasEndpoints.cs`**

Adicionar após `group.MapPost("/{id:guid}/fechar", ...)`:

```csharp
group.MapPut("/{id:guid}", async (
    Guid id, UpdateVendaRequest req, VendaService svc, CancellationToken ct) =>
    Results.Ok(await svc.UpdateAsync(id, req, ct)));
```

- [ ] **Step 7: Build e testes**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet build src/GestorAI.API --no-restore 2>&1 | tail -10
dotnet test tests/GestorAI.Tests --filter "VendaUpdateServiceTests" 2>&1 | tail -20
dotnet test tests/GestorAI.Tests 2>&1 | tail -10
```

Esperado: 3 novos testes PASS + suite completa OK.

- [ ] **Step 8: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Vendas/VendaDto.cs \
        backend/src/GestorAI.API/Services/Vendas/VendaService.cs \
        backend/src/GestorAI.API/Endpoints/VendasEndpoints.cs \
        backend/tests/GestorAI.Tests/Services/VendaUpdateServiceTests.cs
git commit -m "feat: add ClienteId to VendaResponse and PUT /api/vendas/{id}"
```

---

### Task 4: Frontend — Dashboard reorganizado em painéis

**Files:**
- Modify: `frontend/src/pages/Dashboard.tsx`

- [ ] **Step 1: Substituir `Dashboard.tsx`**

Substituir o conteúdo do `return (...)` em `frontend/src/pages/Dashboard.tsx` pelo seguinte (manter imports e lógica de loading/error inalterados; remover imports de ícones não mais usados: `Clock, DollarSign, ArrowDownCircle, ArrowUpCircle`; adicionar `Wallet`):

Atualizar a linha de imports:
```typescript
import {
  TrendingUp, ShoppingCart, PackageX,
  AlertTriangle, ArrowUpCircle, Wallet,
} from 'lucide-react'
```

Substituir o `return (...)` inteiro:

```tsx
return (
  <div className="space-y-6">
    <div className="flex flex-wrap items-center justify-between gap-2">
      <h1 className="text-2xl font-bold">Dashboard</h1>
      <p className="text-sm text-muted-foreground">
        {new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })}
      </p>
    </div>

    <div className="rounded-xl border bg-card p-4 space-y-3">
      <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">Vendas</p>
      <div className="grid grid-cols-2 gap-4">
        <KpiCard titulo="Vendido hoje" valor={fmt(kpis.totalVendidoHoje)} icon={ShoppingCart} cor="green" />
        <KpiCard titulo="Vendido no mês" valor={fmt(kpis.totalVendidoMes)} icon={TrendingUp} />
      </div>
    </div>

    <div className="rounded-xl border bg-card p-4 space-y-3">
      <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">Financeiro — mês atual</p>
      <div className="grid grid-cols-3 gap-4">
        <KpiCard titulo="Saldo do mês" valor={fmt(saldoMes)} icon={Wallet}
          cor={saldoMes >= 0 ? 'green' : 'red'} />
        <KpiCard titulo="A receber (pendente)" valor={fmt(kpis.contasReceberPendentes)} icon={ArrowUpCircle} cor="green" />
        <KpiCard titulo="Contas vencidas" valor={fmt(kpis.contasPagarVencidas)} icon={AlertTriangle}
          cor={kpis.contasPagarVencidas > 0 ? 'red' : 'default'} />
      </div>
    </div>

    {kpis.produtosEstoqueBaixo > 0 && (
      <div className="flex items-center gap-2 rounded-md border border-yellow-500/50 bg-yellow-50 p-3 dark:bg-yellow-950/20">
        <PackageX size={18} className="text-yellow-600 shrink-0" />
        <p className="text-sm text-yellow-700 dark:text-yellow-400">
          <strong>{kpis.produtosEstoqueBaixo} produto(s)</strong> com estoque abaixo do mínimo.{' '}
          <a href="/estoque" className="underline font-medium">Ver estoque →</a>
        </p>
      </div>
    )}

    <div className="grid gap-4 lg:grid-cols-2">
      <GraficoVendas dados={data.vendasUltimos7Dias ?? []} />
      <GraficoFluxo dados={data.fluxoMes ?? []} />
    </div>

    <TopProdutos dados={data.topProdutos ?? []} />
  </div>
)
```

- [ ] **Step 2: Verificar build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npx tsc --noEmit 2>&1 | tail -20
```

Esperado: sem erros.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/Dashboard.tsx
git commit -m "feat: reorganize dashboard into Vendas and Financeiro panels"
```

---

### Task 5: Frontend — Clientes editar + excluir

**Files:**
- Modify: `frontend/src/hooks/useClientes.ts`
- Modify: `frontend/src/pages/clientes/Clientes.tsx`

- [ ] **Step 1: Adicionar `remove` em `useClientes.ts`**

Adicionar antes do `return`:

```typescript
const remove = useCallback(async (id: string) => {
  await api.delete(`/api/clientes/${id}`)
  setClientes(prev => prev.filter(c => c.id !== id))
}, [])
```

Adicionar `remove` ao `return`:

```typescript
return { clientes, loading, error, list, create, update, remove }
```

- [ ] **Step 2: Atualizar `Clientes.tsx`**

Atualizar imports para incluir `Pencil, Trash2`:
```typescript
import { Plus, Search, Pencil, Trash2 } from 'lucide-react'
```

Atualizar imports de tipos para incluir `UpdateClienteRequest`:
```typescript
import type { CreateClienteRequest, UpdateClienteRequest, ClienteResponse } from '@/types/clientes'
```

Atualizar hook destructuring:
```typescript
const { clientes, loading, list, create, update, remove } = useClientes()
```

Adicionar estado de edição após `const [modalAberto, setModalAberto] = useState(false)`:
```typescript
const [editando, setEditando] = useState<ClienteResponse | null>(null)
const { confirm, ConfirmDialogNode } = useConfirm()
```

Adicionar imports de `useConfirm` e `toast`:
```typescript
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
```

Adicionar handlers após `handleCreate`:
```typescript
async function handleEdit(data: UpdateClienteRequest) {
  if (!editando) return
  await update(editando.id, data)
  setEditando(null)
}

async function handleRemove(c: ClienteResponse) {
  const ok = await confirm({
    title: 'Excluir cliente?',
    description: `${c.nome} será removido permanentemente.`,
    variant: 'destructive',
  })
  if (!ok) return
  try {
    await remove(c.id)
    toast.success('Cliente excluído')
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Erro ao excluir')
  }
}
```

Atualizar o cabeçalho da tabela — adicionar 5ª coluna vazia para ações:
```tsx
<tr className="border-b bg-muted/50">
  <th className="px-4 py-3 text-left font-medium">Nome</th>
  <th className="px-4 py-3 text-left font-medium">WhatsApp</th>
  <th className="px-4 py-3 text-left font-medium">E-mail</th>
  <th className="px-4 py-3 text-left font-medium">Cadastrado em</th>
  <th className="px-4 py-3" />
</tr>
```

Atualizar cada linha `<tr>` para adicionar célula de ações:
```tsx
<tr key={c.id} className="border-b hover:bg-muted/30">
  <td className="px-4 py-3 font-medium">{c.nome}</td>
  <td className="px-4 py-3 text-muted-foreground">{c.whatsapp}</td>
  <td className="px-4 py-3 text-muted-foreground">{c.email ?? '—'}</td>
  <td className="px-4 py-3 text-muted-foreground">
    {new Date(c.dataCadastro).toLocaleDateString('pt-BR')}
  </td>
  <td className="px-4 py-3">
    <div className="flex gap-1">
      <Button size="sm" variant="ghost"
        onClick={e => { e.stopPropagation(); setEditando(c) }}>
        <Pencil size={14} />
      </Button>
      <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive hover:bg-destructive/10"
        onClick={e => { e.stopPropagation(); void handleRemove(c) }}>
        <Trash2 size={14} />
      </Button>
    </div>
  </td>
</tr>
```

Adicionar Dialog de edição antes do `{ConfirmDialogNode}` e após o Dialog de criação:
```tsx
<Dialog open={!!editando} onOpenChange={open => { if (!open) setEditando(null) }}>
  <DialogContent>
    <DialogHeader><DialogTitle>Editar Cliente</DialogTitle></DialogHeader>
    {editando && (
      <ClienteForm
        key={editando.id}
        defaultValues={{
          nome: editando.nome,
          whatsapp: editando.whatsapp,
          email: editando.email ?? '',
          observacoes: editando.observacoes ?? '',
        }}
        onSubmit={handleEdit}
        onCancel={() => setEditando(null)}
      />
    )}
  </DialogContent>
</Dialog>
{ConfirmDialogNode}
```

- [ ] **Step 3: Verificar build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npx tsc --noEmit 2>&1 | tail -20
```

Esperado: sem erros.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/hooks/useClientes.ts \
        frontend/src/pages/clientes/Clientes.tsx
git commit -m "feat: add edit and delete actions to Clientes page"
```

---

### Task 6: Frontend — Lançamentos KPIs + editar

**Files:**
- Modify: `frontend/src/types/financeiro.ts`
- Modify: `frontend/src/hooks/useFinanceiro.ts`
- Modify: `frontend/src/components/financeiro/LancamentoForm.tsx`
- Modify: `frontend/src/pages/financeiro/Lancamentos.tsx`

- [ ] **Step 1: Adicionar tipos em `frontend/src/types/financeiro.ts`**

Adicionar ao final do arquivo:

```typescript
export interface LancamentoResumo {
  totalReceitasMes: number
  totalDespesasMes: number
  saldoMes: number
  totalPendente: number
}

export interface UpdateLancamentoRequest {
  tipo: 'Receita' | 'Despesa'
  descricao: string
  valor: number
  dataVencimento: string
  categoria: string
  observacao?: string
}
```

- [ ] **Step 2: Adicionar `update` e `fetchResumo` em `useFinanceiro.ts`**

Adicionar antes do `return`:

```typescript
const update = useCallback(async (id: string, req: UpdateLancamentoRequest) => {
  const result = await api.put<LancamentoResponse>(`/api/lancamentos/${id}`, req)
  setLancamentos(prev => prev.map(l => l.id === id ? result : l))
  return result
}, [])

const fetchResumo = useCallback(async (): Promise<LancamentoResumo> => {
  return api.get<LancamentoResumo>('/api/lancamentos/resumo')
}, [])
```

Atualizar import de tipos no topo do arquivo:
```typescript
import type {
  LancamentoResponse, CreateLancamentoRequest,
  PagarLancamentoRequest, FluxoCaixaResponse,
  LancamentoResumo, UpdateLancamentoRequest,
} from '@/types/financeiro'
```

Adicionar `update` e `fetchResumo` ao `return`:
```typescript
return { lancamentos, fluxo, loading, error, list, create, pagar, cancelar, remove, getFluxoCaixa, update, fetchResumo }
```

- [ ] **Step 3: Adicionar `defaultValues` ao `LancamentoForm.tsx`**

Atualizar a interface `Props` em `frontend/src/components/financeiro/LancamentoForm.tsx`:

```typescript
interface Props {
  defaultValues?: Partial<FormValues>
  defaultTipo?: 'Receita' | 'Despesa'
  onSubmit: (data: CreateLancamentoRequest) => Promise<void>
  onCancel: () => void
}
```

Atualizar a chamada `useForm` dentro do componente:

```typescript
const { register, watch, handleSubmit, formState: { errors, isSubmitting } } =
  useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { tipo: defaultTipo, ...defaultValues },
  })
```

Atualizar a assinatura da função do componente:
```typescript
export default function LancamentoForm({ defaultValues, defaultTipo = 'Despesa', onSubmit, onCancel }: Props) {
```

- [ ] **Step 4: Atualizar `Lancamentos.tsx`**

Adicionar import de `Pencil` e tipos novos:
```typescript
import { Plus, Trash2, Pencil } from 'lucide-react'
import type { CreateLancamentoRequest, LancamentoResponse, LancamentoResumo, UpdateLancamentoRequest } from '@/types/financeiro'
```

Atualizar hook destructuring:
```typescript
const { lancamentos, loading, list, create, pagar, remove, update, fetchResumo } = useFinanceiro()
```

Adicionar estados após os estados existentes:
```typescript
const [resumo, setResumo] = useState<LancamentoResumo | null>(null)
const [editandoLanc, setEditandoLanc] = useState<LancamentoResponse | null>(null)
```

Atualizar `useEffect` para também buscar resumo:
```typescript
useEffect(() => {
  void list()
  void fetchResumo().then(setResumo).catch(() => {})
}, [list, fetchResumo])
```

Adicionar handler de edição após `handlePagar`:
```typescript
async function handleEditLanc(data: CreateLancamentoRequest) {
  if (!editandoLanc) return
  await update(editandoLanc.id, data as UpdateLancamentoRequest)
  setEditandoLanc(null)
}
```

Adicionar painel de KPIs logo após o `<div className="flex flex-wrap ...">` do header:
```tsx
{resumo && (
  <div className="rounded-xl border bg-card p-4 space-y-3">
    <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">Resumo do mês</p>
    <div className="grid grid-cols-4 gap-4">
      {[
        { label: 'Receitas pagas', value: resumo.totalReceitasMes, color: 'text-green-600 dark:text-green-400' },
        { label: 'Despesas pagas', value: resumo.totalDespesasMes, color: 'text-red-600 dark:text-red-400' },
        { label: 'Saldo', value: resumo.saldoMes, color: resumo.saldoMes >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400' },
        { label: 'Pendentes', value: resumo.totalPendente, color: 'text-yellow-600 dark:text-yellow-400' },
      ].map(k => (
        <div key={k.label} className="rounded-lg border bg-background p-3">
          <p className="text-xs text-muted-foreground">{k.label}</p>
          <p className={`text-xl font-bold mt-1 ${k.color}`}>
            {k.value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </p>
        </div>
      ))}
    </div>
  </div>
)}
```

Adicionar botão de editar na coluna de ações — dentro do `<div className="flex items-center gap-2">`, após o botão Pagar/Receber e antes do lixão:
```tsx
{l.status === 'Pendente' && !l.vendaId && (
  <Button size="sm" variant="ghost"
    onClick={() => setEditandoLanc(l)}>
    <Pencil size={14} />
  </Button>
)}
```

Adicionar Dialog de edição antes do `{ConfirmDialogNode}`:
```tsx
<Dialog open={!!editandoLanc} onOpenChange={open => { if (!open) setEditandoLanc(null) }}>
  <DialogContent className="max-w-lg">
    <DialogHeader><DialogTitle>Editar Lançamento</DialogTitle></DialogHeader>
    {editandoLanc && (
      <LancamentoForm
        key={editandoLanc.id}
        defaultValues={{
          tipo: editandoLanc.tipo,
          descricao: editandoLanc.descricao,
          valor: editandoLanc.valor.toString(),
          dataVencimento: editandoLanc.dataVencimento.slice(0, 10),
          categoria: editandoLanc.categoria,
          observacao: editandoLanc.observacao ?? undefined,
        }}
        onSubmit={handleEditLanc}
        onCancel={() => setEditandoLanc(null)}
      />
    )}
  </DialogContent>
</Dialog>
```

- [ ] **Step 5: Verificar build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npx tsc --noEmit 2>&1 | tail -20
```

Esperado: sem erros.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/types/financeiro.ts \
        frontend/src/hooks/useFinanceiro.ts \
        frontend/src/components/financeiro/LancamentoForm.tsx \
        frontend/src/pages/financeiro/Lancamentos.tsx
git commit -m "feat: add KPI panel and edit button to Lancamentos page"
```

---

### Task 7: Frontend — Histórico de Vendas editável

**Files:**
- Modify: `frontend/src/types/vendas.ts`
- Modify: `frontend/src/hooks/useVendas.ts`
- Modify: `frontend/src/pages/vendas/Historico.tsx`

- [ ] **Step 1: Atualizar `frontend/src/types/vendas.ts`**

Adicionar `clienteId?: string` como segundo campo em `VendaResponse` e `VendaListItem`, e adicionar `UpdateVendaRequest`:

```typescript
export interface VendaResponse {
  id: string
  clienteId: string | null
  clienteNome: string | null
  dataHora: string
  status: string
  subtotal: number
  desconto: number
  total: number
  formaPagamento: string
  parcelas: number | null
  observacao: string | null
  itens: ItemVendaResponse[]
}

export interface VendaListItem {
  id: string
  clienteId: string | null
  clienteNome: string | null
  dataHora: string
  status: string
  total: number
  formaPagamento: string
}

export interface UpdateVendaRequest {
  clienteId?: string | null
  formaPagamento: string
  dataHora: string
}
```

(Manter os outros tipos existentes: `ItemVendaRequest`, `CreateVendaRequest`, `ItemVendaResponse`, `ItemCarrinho`, `FecharVendaRequest`.)

- [ ] **Step 2: Adicionar `update` em `useVendas.ts`**

Adicionar antes do `return`:

```typescript
const update = useCallback(async (id: string, req: UpdateVendaRequest) => {
  const result = await api.put<VendaResponse>(`/api/vendas/${id}`, req)
  setVendas(prev => prev.map(v => v.id === id
    ? { ...v, clienteId: result.clienteId, clienteNome: result.clienteNome, formaPagamento: result.formaPagamento, dataHora: result.dataHora }
    : v))
  return result
}, [])
```

Adicionar ao import de tipos: `UpdateVendaRequest`.

Adicionar `update` ao `return`.

- [ ] **Step 3: Atualizar `Historico.tsx`**

Adicionar imports:
```typescript
import { Trash2, Pencil } from 'lucide-react'
import { useClientes } from '@/hooks/useClientes'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { UpdateVendaRequest } from '@/types/vendas'
```

Adicionar `useClientes` e `update` ao topo do componente:
```typescript
const { vendas, loading, list, cancelar, remove, update } = useVendas()
const { clientes, list: listClientes } = useClientes()
```

Adicionar `useEffect` para carregar clientes:
```typescript
useEffect(() => { void listClientes() }, [listClientes])
```

Adicionar estados para o modal de edição após os estados existentes:
```typescript
const [editandoVenda, setEditandoVenda] = useState<{ id: string; clienteId: string | null; formaPagamento: string; dataHora: string } | null>(null)
const [salvandoEdit, setSalvandoEdit] = useState(false)
```

Adicionar handler:
```typescript
async function handleSalvarEdit(req: UpdateVendaRequest) {
  if (!editandoVenda) return
  setSalvandoEdit(true)
  try {
    await update(editandoVenda.id, req)
    setEditandoVenda(null)
    toast.success('Venda atualizada')
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Erro ao atualizar')
  } finally { setSalvandoEdit(false) }
}
```

Adicionar botão de editar na coluna de ações — dentro do `<div className="flex items-center gap-2">`, antes do botão Cancelar:
```tsx
{v.status === 'Concluida' && (
  <Button size="sm" variant="ghost"
    onClick={() => setEditandoVenda({ id: v.id, clienteId: v.clienteId, formaPagamento: v.formaPagamento, dataHora: v.dataHora })}>
    <Pencil size={14} />
  </Button>
)}
```

Adicionar modal de edição antes do `{ConfirmDialogNode}`:
```tsx
{editandoVenda && (
  <Dialog open onOpenChange={open => { if (!open) setEditandoVenda(null) }}>
    <DialogContent className="max-w-sm">
      <DialogHeader><DialogTitle>Editar Venda</DialogTitle></DialogHeader>
      <div className="space-y-4">
        <div className="space-y-1.5">
          <Label>Cliente</Label>
          <select
            defaultValue={editandoVenda.clienteId ?? ''}
            onChange={e => setEditandoVenda(prev => prev ? { ...prev, clienteId: e.target.value || null } : prev)}
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
            <option value="">Balcão (sem cliente)</option>
            {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label>Forma de pagamento</Label>
          <select
            defaultValue={editandoVenda.formaPagamento}
            onChange={e => setEditandoVenda(prev => prev ? { ...prev, formaPagamento: e.target.value } : prev)}
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
            {['Pix', 'Dinheiro', 'Cartao', 'Outro'].map(f => <option key={f} value={f}>{f}</option>)}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label>Data da venda</Label>
          <Input type="date"
            defaultValue={editandoVenda.dataHora.slice(0, 10)}
            onChange={e => setEditandoVenda(prev => prev ? { ...prev, dataHora: e.target.value + 'T12:00:00Z' } : prev)}
            className="h-9" />
        </div>
        <div className="flex gap-2 pt-2">
          <Button disabled={salvandoEdit}
            onClick={() => void handleSalvarEdit({ clienteId: editandoVenda.clienteId, formaPagamento: editandoVenda.formaPagamento, dataHora: editandoVenda.dataHora })}>
            {salvandoEdit ? 'Salvando...' : 'Salvar'}
          </Button>
          <Button variant="outline" onClick={() => setEditandoVenda(null)}>Cancelar</Button>
        </div>
      </div>
    </DialogContent>
  </Dialog>
)}
```

- [ ] **Step 4: Verificar build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npx tsc --noEmit 2>&1 | tail -20
```

Esperado: sem erros.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/types/vendas.ts \
        frontend/src/hooks/useVendas.ts \
        frontend/src/pages/vendas/Historico.tsx
git commit -m "feat: add edit button to Historico de Vendas"
```

---

### Task 8: Frontend — Inline criação de cliente em NovaVenda

**Files:**
- Modify: `frontend/src/pages/vendas/NovaVenda.tsx`

- [ ] **Step 1: Atualizar `NovaVenda.tsx`**

Adicionar imports:
```typescript
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ClienteForm from '@/components/clientes/ClienteForm'
import type { CreateClienteRequest } from '@/types/clientes'
```

Adicionar import de `Plus` — o arquivo já tem `CheckCircle2, ArrowLeft`, acrescentar:
```typescript
import { CheckCircle2, ArrowLeft, Plus } from 'lucide-react'
```

Adicionar estado após os estados existentes:
```typescript
const [modalNovoCliente, setModalNovoCliente] = useState(false)
```

Adicionar handler de criação inline:
```typescript
async function handleCriarCliente(data: CreateClienteRequest) {
  const { create: createCliente } = useClientes() // já está disponível no escopo
  // Usar diretamente a função create disponível via useClientes já importado
}
```

Na verdade, `useClientes` já está importado e `create` está disponível. Atualizar a linha de destructuring existente:
```typescript
const { clientes, list: listClientes, create: createCliente } = useClientes()
```

Adicionar handler:
```typescript
async function handleCriarCliente(data: CreateClienteRequest) {
  const novoCliente = await createCliente(data)
  await listClientes()
  setClienteId(novoCliente.id)
  setModalNovoCliente(false)
}
```

Localizar o bloco do seletor de cliente (linha ~200, dentro de `{!vendaIdParam && (...)}`) e substituir:

```tsx
{!vendaIdParam && (
  <div className="space-y-1.5">
    <Label className="text-xs text-muted-foreground">Cliente (opcional)</Label>
    <div className="flex gap-2">
      <select value={clienteId} onChange={e => setClienteId(e.target.value)}
        className="flex h-9 flex-1 rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
        <option value="">Balcão (sem cliente)</option>
        {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
      </select>
      <Button type="button" variant="outline" size="icon"
        title="Criar novo cliente"
        onClick={() => setModalNovoCliente(true)}>
        <Plus size={16} />
      </Button>
    </div>
  </div>
)}
```

Adicionar modal logo antes do `</div>` final do componente:
```tsx
<Dialog open={modalNovoCliente} onOpenChange={setModalNovoCliente}>
  <DialogContent>
    <DialogHeader><DialogTitle>Novo Cliente</DialogTitle></DialogHeader>
    <ClienteForm
      onSubmit={handleCriarCliente}
      onCancel={() => setModalNovoCliente(false)}
    />
  </DialogContent>
</Dialog>
```

- [ ] **Step 2: Verificar build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npx tsc --noEmit 2>&1 | tail -20
```

Esperado: sem erros.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/vendas/NovaVenda.tsx
git commit -m "feat: add inline new client creation in NovaVenda"
```

---

## Checklist final

- [ ] `DELETE /api/clientes/{id}` — bloqueia se tiver vínculos, testado
- [ ] `GET /api/lancamentos/resumo` — 4 KPIs server-side, testado
- [ ] `PUT /api/lancamentos/{id}` — só Pendente sem VendaId, testado
- [ ] `ClienteId` em `VendaResponse` + `VendaListItem`
- [ ] `PUT /api/vendas/{id}` — atualiza venda + lançamento vinculado, testado
- [ ] Dashboard com Painel Vendas + Painel Financeiro
- [ ] Clientes: editar (modal) + excluir (confirmação + toast.error se vínculos)
- [ ] Lançamentos: painel KPIs + botão editar (só Pendente sem VendaId)
- [ ] Histórico de Vendas: botão editar (modal 3 campos: cliente, forma, data)
- [ ] NovaVenda: botão `+` abre modal ClienteForm, seleciona cliente criado
- [ ] `dotnet test` full suite passando (92+ pass)
- [ ] `npx tsc --noEmit` sem erros
