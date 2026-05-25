# GestorAI — Plano 5/5: Dashboard + BI

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dashboard com KPIs do dia/mês e gráficos (Recharts), e página de Relatórios/BI com 4 abas completas (Visão Geral, Vendas, Financeiro, Estoque), filtros de período, drill-down e exportação PDF/Excel.

**Architecture:** `DashboardService` e `RelatorioService` no backend consultam os módulos existentes para calcular KPIs e séries temporais. Frontend usa Recharts para todos os gráficos. Exportação PDF via `window.print()` com CSS de impressão; Excel via geração de CSV (sem dependências extras).

**Tech Stack:** .NET 10, EF Core 9 | React 18, TypeScript, Recharts, shadcn/ui

**Pré-requisito:** Planos 1-4 concluídos — todos os módulos (Vendas, Estoque, Financeiro, Clientes) funcionais.

---

## Mapa de arquivos

### Backend

| Arquivo | Responsabilidade |
|---|---|
| `src/GestorAI.API/DTOs/Dashboard/DashboardDto.cs` | Records de KPIs e séries do dashboard |
| `src/GestorAI.API/DTOs/Relatorios/RelatorioDto.cs` | Records de relatórios por módulo |
| `src/GestorAI.API/Services/Dashboard/DashboardService.cs` | Calcula KPIs do dia/mês e séries |
| `src/GestorAI.API/Services/Relatorios/RelatorioService.cs` | Relatórios de Vendas, Financeiro, Estoque, Clientes |
| `src/GestorAI.API/Endpoints/DashboardEndpoints.cs` | Minimal API routes |
| `tests/GestorAI.Tests/Services/DashboardServiceTests.cs` | Testes do DashboardService |

### Frontend

| Arquivo | Responsabilidade |
|---|---|
| `src/types/dashboard.ts` | Types TypeScript de dashboard |
| `src/types/relatorios.ts` | Types TypeScript de relatórios |
| `src/hooks/useDashboard.ts` | Estado + carregamento do dashboard |
| `src/hooks/useRelatorios.ts` | Estado + carregamento de relatórios |
| `src/pages/Dashboard.tsx` | Dashboard completo com KPIs e gráficos |
| `src/pages/relatorios/Relatorios.tsx` | Página BI com abas, filtros e exportação |
| `src/components/dashboard/KpiCard.tsx` | Card de indicador com variação |
| `src/components/dashboard/GraficoVendas.tsx` | Recharts — vendas 7 dias |
| `src/components/dashboard/GraficoFluxo.tsx` | Recharts — entradas vs saídas |
| `src/components/dashboard/TopProdutos.tsx` | Recharts — top 5 produtos |
| `src/components/relatorios/FiltrosPeriodo.tsx` | Seletor de período reutilizável |
| `src/components/relatorios/AbaVisaoGeral.tsx` | KPIs consolidados |
| `src/components/relatorios/AbaVendas.tsx` | Tendência + rankings |
| `src/components/relatorios/AbaFinanceiro.tsx` | Fluxo de caixa + categorias |
| `src/components/relatorios/AbaEstoque.tsx` | Giro + produtos sem mov. |

---

## Task 1: DTOs de Dashboard e Relatórios

**Files:**
- Create: `src/GestorAI.API/DTOs/Dashboard/DashboardDto.cs`
- Create: `src/GestorAI.API/DTOs/Relatorios/RelatorioDto.cs`

- [ ] **Step 1: Criar diretórios**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
mkdir -p src/GestorAI.API/DTOs/Dashboard src/GestorAI.API/DTOs/Relatorios
```

- [ ] **Step 2: Criar DashboardDto.cs**

`src/GestorAI.API/DTOs/Dashboard/DashboardDto.cs`:
```csharp
namespace GestorAI.API.DTOs.Dashboard;

public record KpiResponse(
    decimal TotalVendidoHoje,
    decimal TotalVendidoMes,
    decimal LucroEstimadoMes,
    decimal ContasPagarVencidas,
    decimal ContasPagarProximas7Dias,
    decimal ContasReceberPendentes,
    int ProdutosEstoqueBaixo);

public record VendasDiaResponse(DateTime Data, decimal Total, int Quantidade);

public record FluxoDiaResponse(DateTime Data, decimal Receitas, decimal Despesas);

public record TopProdutoResponse(string Nome, decimal QuantidadeVendida, decimal TotalFaturado);

public record DashboardResponse(
    KpiResponse Kpis,
    List<VendasDiaResponse> VendasUltimos7Dias,
    List<FluxoDiaResponse> FluxoMes,
    List<TopProdutoResponse> TopProdutos);
```

- [ ] **Step 3: Criar RelatorioDto.cs**

`src/GestorAI.API/DTOs/Relatorios/RelatorioDto.cs`:
```csharp
namespace GestorAI.API.DTOs.Relatorios;

// Visão Geral
public record KpisGeralResponse(
    decimal Faturamento,
    decimal TicketMedio,
    decimal MargemEstimada,
    decimal Inadimplencia,
    int TotalVendas);

// Vendas
public record TendenciaVendasResponse(DateTime Data, decimal Total, int Quantidade);
public record RankingProdutoResponse(string Nome, decimal Quantidade, decimal Total);
public record RankingClienteResponse(string Nome, int Compras, decimal Total);
public record VendasPorPagamentoResponse(string FormaPagamento, int Quantidade, decimal Total);

public record RelatorioVendasResponse(
    List<TendenciaVendasResponse> Tendencia,
    List<RankingProdutoResponse> TopProdutos,
    List<RankingClienteResponse> TopClientes,
    List<VendasPorPagamentoResponse> PorFormaPagamento);

// Financeiro
public record FluxoCaixaDiaResponse(DateTime Data, decimal Receitas, decimal Despesas, decimal Saldo);
public record CategoriaDespesaResponse(string Categoria, decimal Total);

public record RelatorioFinanceiroResponse(
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal Saldo,
    List<FluxoCaixaDiaResponse> FluxoPorDia,
    List<CategoriaDespesaResponse> CategoriasDespesas);

// Estoque
public record GiroProdutoResponse(string Nome, decimal Entradas, decimal Saidas, decimal GiroLiquido);
public record ProdutoSemMovimentacaoResponse(string Nome, decimal EstoqueAtual, decimal ValorEmEstoque);

public record RelatorioEstoqueResponse(
    decimal ValorTotalEstoque,
    int ProdutosAtivos,
    int ProdutosEstoqueBaixo,
    List<GiroProdutoResponse> GiroProdutos,
    List<ProdutoSemMovimentacaoResponse> SemMovimentacao);

// Clientes
public record ClienteRankingResponse(string Nome, string Whatsapp, int Compras, decimal TotalGasto);

public record RelatorioClientesResponse(
    int TotalClientes,
    int ClientesCompraram,
    decimal TicketMedioCliente,
    List<ClienteRankingResponse> TopClientes);
```

- [ ] **Step 4: Build**

```bash
dotnet build
```

Expected: `Build succeeded.`

---

## Task 2: DashboardService com testes

**Files:**
- Create: `src/GestorAI.API/Services/Dashboard/DashboardService.cs`
- Test: `tests/GestorAI.Tests/Services/DashboardServiceTests.cs`

- [ ] **Step 1: Escrever testes (TDD — red)**

`tests/GestorAI.Tests/Services/DashboardServiceTests.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Dashboard;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class DashboardServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, DashboardService service) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new DashboardService(db));
    }

    [Fact]
    public async Task GetKpisAsync_TotalVendidoHoje_SomaVendasConcluidas()
    {
        var (db, service) = Setup();
        var hoje = DateTime.UtcNow;
        var ontem = hoje.AddDays(-1);

        db.Vendas.AddRange(
            new Venda
            {
                EmpresaId = _empresaId, DataHora = hoje,
                Status = StatusVenda.Concluida, Total = 300m,
                Subtotal = 300m, Desconto = 0m,
                FormaPagamento = FormaPagamento.Pix
            },
            new Venda
            {
                EmpresaId = _empresaId, DataHora = ontem,
                Status = StatusVenda.Concluida, Total = 500m,
                Subtotal = 500m, Desconto = 0m,
                FormaPagamento = FormaPagamento.Dinheiro
            },
            new Venda
            {
                EmpresaId = _empresaId, DataHora = hoje,
                Status = StatusVenda.Cancelada, Total = 100m,
                Subtotal = 100m, Desconto = 0m,
                FormaPagamento = FormaPagamento.Pix
            });
        await db.SaveChangesAsync();

        var result = await service.GetDashboardAsync(default);

        Assert.Equal(300m, result.Kpis.TotalVendidoHoje);
    }

    [Fact]
    public async Task GetKpisAsync_ProdutosEstoqueBaixo_ContaProdutosAbaixoMinimo()
    {
        var (db, service) = Setup();
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Cat" };
        db.Categorias.Add(cat);
        db.Produtos.AddRange(
            new Produto
            {
                EmpresaId = _empresaId, CategoriaId = cat.Id,
                Nome = "Baixo", PrecoVenda = 10m,
                EstoqueAtual = 1m, EstoqueMinimo = 5m
            },
            new Produto
            {
                EmpresaId = _empresaId, CategoriaId = cat.Id,
                Nome = "OK", PrecoVenda = 10m,
                EstoqueAtual = 10m, EstoqueMinimo = 2m
            });
        await db.SaveChangesAsync();

        var result = await service.GetDashboardAsync(default);

        Assert.Equal(1, result.Kpis.ProdutosEstoqueBaixo);
    }

    [Fact]
    public async Task GetDashboardAsync_TopProdutos_OrdenaPorQuantidadeVendida()
    {
        var (db, service) = Setup();
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Cat" };
        db.Categorias.Add(cat);
        var p1 = new Produto { EmpresaId = _empresaId, CategoriaId = cat.Id, Nome = "A", PrecoVenda = 10m, EstoqueAtual = 0m, EstoqueMinimo = 0m };
        var p2 = new Produto { EmpresaId = _empresaId, CategoriaId = cat.Id, Nome = "B", PrecoVenda = 20m, EstoqueAtual = 0m, EstoqueMinimo = 0m };
        db.Produtos.AddRange(p1, p2);

        var venda = new Venda
        {
            EmpresaId = _empresaId, DataHora = DateTime.UtcNow,
            Status = StatusVenda.Concluida, Subtotal = 50m,
            Desconto = 0m, Total = 50m, FormaPagamento = FormaPagamento.Pix
        };
        db.Vendas.Add(venda);
        await db.SaveChangesAsync();

        db.ItensVenda.AddRange(
            new ItemVenda { VendaId = venda.Id, ProdutoId = p1.Id, Quantidade = 3m, PrecoUnitario = 10m, Desconto = 0m, Total = 30m },
            new ItemVenda { VendaId = venda.Id, ProdutoId = p2.Id, Quantidade = 1m, PrecoUnitario = 20m, Desconto = 0m, Total = 20m });
        await db.SaveChangesAsync();

        var result = await service.GetDashboardAsync(default);

        Assert.Equal("A", result.TopProdutos[0].Nome);
        Assert.Equal(3m, result.TopProdutos[0].QuantidadeVendida);
    }
}
```

- [ ] **Step 2: Rodar — confirmar que falham**

```bash
dotnet test tests/GestorAI.Tests --filter "DashboardServiceTests"
```

Expected: Erro de compilação — `DashboardService` não existe.

- [ ] **Step 3: Criar DashboardService.cs**

```bash
mkdir -p src/GestorAI.API/Services/Dashboard src/GestorAI.API/Services/Relatorios
```

`src/GestorAI.API/Services/Dashboard/DashboardService.cs`:
```csharp
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Dashboard;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Dashboard;

public class DashboardService(AppDbContext db)
{
    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var em7Dias = hoje.AddDays(7);

        // KPIs
        var vendasHoje = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora.Date == hoje)
            .SumAsync(v => v.Total, ct);

        var vendasMes = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora >= inicioMes)
            .SumAsync(v => v.Total, ct);

        var lucroEstimado = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida
                && i.Venda.DataHora >= inicioMes)
            .Include(i => i.Produto)
            .SumAsync(i => (i.PrecoUnitario - i.Produto!.CustoMedio) * i.Quantidade, ct);

        var contasPagarVencidas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa
                && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date < hoje)
            .SumAsync(l => l.Valor, ct);

        var contasPagarProximas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa
                && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date >= hoje
                && l.DataVencimento.Date <= em7Dias)
            .SumAsync(l => l.Valor, ct);

        var contasReceberPendentes = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pendente)
            .SumAsync(l => l.Valor, ct);

        var estoqueBaixo = await db.Produtos
            .CountAsync(p => p.Ativo && p.EstoqueAtual <= p.EstoqueMinimo, ct);

        // Vendas últimos 7 dias
        var inicio7Dias = hoje.AddDays(-6);
        var vendasSerie = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora.Date >= inicio7Dias)
            .GroupBy(v => v.DataHora.Date)
            .Select(g => new VendasDiaResponse(g.Key, g.Sum(v => v.Total), g.Count()))
            .ToListAsync(ct);

        // Preenche dias sem vendas
        var vendas7Dias = Enumerable.Range(0, 7)
            .Select(i => inicio7Dias.AddDays(i))
            .Select(d => vendasSerie.FirstOrDefault(v => v.Data == d)
                ?? new VendasDiaResponse(d, 0m, 0))
            .ToList();

        // Fluxo do mês
        var fluxoSerie = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                && l.DataPagamento!.Value.Date >= inicioMes)
            .GroupBy(l => l.DataPagamento!.Value.Date)
            .Select(g => new FluxoDiaResponse(
                g.Key,
                g.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor),
                g.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor)))
            .OrderBy(f => f.Data)
            .ToListAsync(ct);

        // Top 5 produtos do mês
        var topProdutos = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida
                && i.Venda.DataHora >= inicioMes)
            .Include(i => i.Produto)
            .GroupBy(i => new { i.ProdutoId, i.Produto!.Nome })
            .Select(g => new TopProdutoResponse(
                g.Key.Nome,
                g.Sum(i => i.Quantidade),
                g.Sum(i => i.Total)))
            .OrderByDescending(p => p.QuantidadeVendida)
            .Take(5)
            .ToListAsync(ct);

        return new DashboardResponse(
            new KpiResponse(vendasHoje, vendasMes, lucroEstimado,
                contasPagarVencidas, contasPagarProximas, contasReceberPendentes, estoqueBaixo),
            vendas7Dias,
            fluxoSerie,
            topProdutos);
    }
}
```

- [ ] **Step 4: Rodar testes — confirmar que passam**

```bash
dotnet test tests/GestorAI.Tests --filter "DashboardServiceTests"
```

Expected: `Passed! - Failed: 0, Passed: 3`

---

## Task 3: RelatorioService

**Files:**
- Create: `src/GestorAI.API/Services/Relatorios/RelatorioService.cs`

- [ ] **Step 1: Criar RelatorioService.cs**

`src/GestorAI.API/Services/Relatorios/RelatorioService.cs`:
```csharp
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Relatorios;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Relatorios;

public class RelatorioService(AppDbContext db)
{
    public async Task<KpisGeralResponse> GetKpisGeralAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var vendasPeriodo = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .ToListAsync(ct);

        var faturamento = vendasPeriodo.Sum(v => v.Total);
        var totalVendas = vendasPeriodo.Count;
        var ticketMedio = totalVendas > 0 ? faturamento / totalVendas : 0m;

        var lucro = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida
                && i.Venda.DataHora.Date >= de.Date && i.Venda.DataHora.Date <= ate.Date)
            .Include(i => i.Produto)
            .SumAsync(i => (i.PrecoUnitario - i.Produto!.CustoMedio) * i.Quantidade, ct);

        var margem = faturamento > 0 ? lucro / faturamento * 100m : 0m;

        var totalReceberPeriodo = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.DataVencimento.Date >= de.Date && l.DataVencimento.Date <= ate.Date)
            .SumAsync(l => l.Valor, ct);

        var vencidas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date < DateTime.UtcNow.Date)
            .SumAsync(l => l.Valor, ct);

        var inadimplencia = totalReceberPeriodo > 0 ? vencidas / totalReceberPeriodo * 100m : 0m;

        return new KpisGeralResponse(faturamento, ticketMedio, margem, inadimplencia, totalVendas);
    }

    public async Task<RelatorioVendasResponse> GetVendasAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var tendencia = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .GroupBy(v => v.DataHora.Date)
            .Select(g => new TendenciaVendasResponse(g.Key, g.Sum(v => v.Total), g.Count()))
            .OrderBy(t => t.Data)
            .ToListAsync(ct);

        var topProdutos = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida
                && i.Venda.DataHora.Date >= de.Date && i.Venda.DataHora.Date <= ate.Date)
            .Include(i => i.Produto)
            .GroupBy(i => new { i.ProdutoId, i.Produto!.Nome })
            .Select(g => new RankingProdutoResponse(g.Key.Nome, g.Sum(i => i.Quantidade), g.Sum(i => i.Total)))
            .OrderByDescending(p => p.Total)
            .Take(10)
            .ToListAsync(ct);

        var topClientes = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.ClienteId != null
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .Include(v => v.Cliente)
            .GroupBy(v => new { v.ClienteId, v.Cliente!.Nome })
            .Select(g => new RankingClienteResponse(g.Key.Nome, g.Count(), g.Sum(v => v.Total)))
            .OrderByDescending(c => c.Total)
            .Take(10)
            .ToListAsync(ct);

        var porPagamento = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .GroupBy(v => v.FormaPagamento)
            .Select(g => new VendasPorPagamentoResponse(
                g.Key.ToString(), g.Count(), g.Sum(v => v.Total)))
            .ToListAsync(ct);

        return new RelatorioVendasResponse(tendencia, topProdutos, topClientes, porPagamento);
    }

    public async Task<RelatorioFinanceiroResponse> GetFinanceiroAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var lancamentos = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                && l.DataPagamento!.Value.Date >= de.Date
                && l.DataPagamento.Value.Date <= ate.Date)
            .ToListAsync(ct);

        var fluxoPorDia = lancamentos
            .GroupBy(l => l.DataPagamento!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var receitas = g.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
                var despesas = g.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);
                return new FluxoCaixaDiaResponse(g.Key, receitas, despesas, receitas - despesas);
            })
            .ToList();

        var categoriasDespesas = lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa)
            .GroupBy(l => l.Categoria)
            .Select(g => new CategoriaDespesaResponse(g.Key, g.Sum(l => l.Valor)))
            .OrderByDescending(c => c.Total)
            .ToList();

        var totalReceitas = lancamentos.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
        var totalDespesas = lancamentos.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);

        return new RelatorioFinanceiroResponse(
            totalReceitas, totalDespesas, totalReceitas - totalDespesas,
            fluxoPorDia, categoriasDespesas);
    }

    public async Task<RelatorioEstoqueResponse> GetEstoqueAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var produtos = await db.Produtos.Where(p => p.Ativo).ToListAsync(ct);
        var valorTotal = produtos.Sum(p => p.EstoqueAtual * p.CustoMedio);
        var estoqueBaixo = produtos.Count(p => p.EstoqueAtual <= p.EstoqueMinimo);

        var movimentos = await db.MovimentacoesEstoque
            .Include(m => m.Produto)
            .Where(m => m.DataHora.Date >= de.Date && m.DataHora.Date <= ate.Date)
            .ToListAsync(ct);

        var giro = movimentos
            .GroupBy(m => new { m.ProdutoId, m.Produto!.Nome })
            .Select(g =>
            {
                var entradas = g.Where(m => m.Tipo == TipoMovimentacao.Entrada).Sum(m => m.Quantidade);
                var saidas = g.Where(m => m.Tipo == TipoMovimentacao.Saida).Sum(m => m.Quantidade);
                return new GiroProdutoResponse(g.Key.Nome, entradas, saidas, saidas - entradas);
            })
            .OrderByDescending(g => g.Saidas)
            .Take(20)
            .ToList();

        var produtosMovimentadosIds = movimentos.Select(m => m.ProdutoId).Distinct().ToHashSet();
        var semMovimentacao = produtos
            .Where(p => !produtosMovimentadosIds.Contains(p.Id))
            .Select(p => new ProdutoSemMovimentacaoResponse(
                p.Nome, p.EstoqueAtual, p.EstoqueAtual * p.CustoMedio))
            .OrderByDescending(p => p.ValorEmEstoque)
            .Take(20)
            .ToList();

        return new RelatorioEstoqueResponse(
            valorTotal, produtos.Count, estoqueBaixo, giro, semMovimentacao);
    }

    public async Task<RelatorioClientesResponse> GetClientesAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var totalClientes = await db.Clientes.CountAsync(ct);

        var vendas = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.ClienteId != null
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .Include(v => v.Cliente)
            .ToListAsync(ct);

        var clientesCompraram = vendas.Select(v => v.ClienteId).Distinct().Count();
        var totalFaturado = vendas.Sum(v => v.Total);
        var ticketMedio = clientesCompraram > 0 ? totalFaturado / clientesCompraram : 0m;

        var topClientes = vendas
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente?.Nome ?? "", v.Cliente?.Whatsapp })
            .Select(g => new ClienteRankingResponse(
                g.Key.Nome, g.Key.Whatsapp ?? "", g.Count(), g.Sum(v => v.Total)))
            .OrderByDescending(c => c.TotalGasto)
            .Take(10)
            .ToList();

        return new RelatorioClientesResponse(
            totalClientes, clientesCompraram, ticketMedio, topClientes);
    }
}
```

- [ ] **Step 2: Build e testes**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet build && dotnet test tests/GestorAI.Tests
```

Expected: Todos os testes passando.

- [ ] **Step 3: Commit**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add backend/
git commit -m "feat: DashboardService e RelatorioService com testes"
```

---

## Task 4: Endpoints Dashboard e Relatórios

**Files:**
- Create: `src/GestorAI.API/Endpoints/DashboardEndpoints.cs`
- Modify: `src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar DashboardEndpoints.cs**

`src/GestorAI.API/Endpoints/DashboardEndpoints.cs`:
```csharp
using GestorAI.API.Services.Dashboard;
using GestorAI.API.Services.Relatorios;

namespace GestorAI.API.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboard(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        group.MapGet("/dashboard", async (DashboardService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetDashboardAsync(ct)));

        group.MapGet("/relatorios/kpis", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetKpisGeralAsync(de, ate, ct)));

        group.MapGet("/relatorios/vendas", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetVendasAsync(de, ate, ct)));

        group.MapGet("/relatorios/financeiro", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetFinanceiroAsync(de, ate, ct)));

        group.MapGet("/relatorios/estoque", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetEstoqueAsync(de, ate, ct)));

        group.MapGet("/relatorios/clientes", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetClientesAsync(de, ate, ct)));
    }
}
```

- [ ] **Step 2: Registrar em Program.cs**

Adicionar antes de `var app = builder.Build();`:
```csharp
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<RelatorioService>();
```

Adicionar usings:
```csharp
using GestorAI.API.Services.Dashboard;
using GestorAI.API.Services.Relatorios;
```

Adicionar após `app.MapFinanceiro();`:
```csharp
app.MapDashboard();
```

- [ ] **Step 3: Build e testes**

```bash
dotnet build && dotnet test tests/GestorAI.Tests
```

Expected: Tudo passando.

- [ ] **Step 4: Commit**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add backend/
git commit -m "feat: endpoints Dashboard e Relatórios"
```

---

## Task 5: Types e hooks frontend

**Files:**
- Create: `src/types/dashboard.ts`
- Create: `src/types/relatorios.ts`
- Create: `src/hooks/useDashboard.ts`
- Create: `src/hooks/useRelatorios.ts`

- [ ] **Step 1: Criar src/types/dashboard.ts**

`src/types/dashboard.ts`:
```ts
export interface KpiResponse {
  totalVendidoHoje: number
  totalVendidoMes: number
  lucroEstimadoMes: number
  contasPagarVencidas: number
  contasPagarProximas7Dias: number
  contasReceberPendentes: number
  produtosEstoqueBaixo: number
}

export interface VendasDiaResponse { data: string; total: number; quantidade: number }
export interface FluxoDiaResponse { data: string; receitas: number; despesas: number }
export interface TopProdutoResponse { nome: string; quantidadeVendida: number; totalFaturado: number }

export interface DashboardResponse {
  kpis: KpiResponse
  vendasUltimos7Dias: VendasDiaResponse[]
  fluxoMes: FluxoDiaResponse[]
  topProdutos: TopProdutoResponse[]
}
```

- [ ] **Step 2: Criar src/types/relatorios.ts**

`src/types/relatorios.ts`:
```ts
export interface KpisGeralResponse {
  faturamento: number
  ticketMedio: number
  margemEstimada: number
  inadimplencia: number
  totalVendas: number
}

export interface TendenciaVendasResponse { data: string; total: number; quantidade: number }
export interface RankingProdutoResponse { nome: string; quantidade: number; total: number }
export interface RankingClienteResponse { nome: string; compras: number; total: number }
export interface VendasPorPagamentoResponse { formaPagamento: string; quantidade: number; total: number }
export interface RelatorioVendasResponse {
  tendencia: TendenciaVendasResponse[]
  topProdutos: RankingProdutoResponse[]
  topClientes: RankingClienteResponse[]
  porFormaPagamento: VendasPorPagamentoResponse[]
}

export interface FluxoCaixaDiaResponse { data: string; receitas: number; despesas: number; saldo: number }
export interface CategoriaDespesaResponse { categoria: string; total: number }
export interface RelatorioFinanceiroResponse {
  totalReceitas: number
  totalDespesas: number
  saldo: number
  fluxoPorDia: FluxoCaixaDiaResponse[]
  categoriasDespesas: CategoriaDespesaResponse[]
}

export interface GiroProdutoResponse { nome: string; entradas: number; saidas: number; giroLiquido: number }
export interface ProdutoSemMovimentacaoResponse { nome: string; estoqueAtual: number; valorEmEstoque: number }
export interface RelatorioEstoqueResponse {
  valorTotalEstoque: number
  produtosAtivos: number
  produtosEstoqueBaixo: number
  giroProdutos: GiroProdutoResponse[]
  semMovimentacao: ProdutoSemMovimentacaoResponse[]
}

export interface ClienteRankingResponse { nome: string; whatsapp: string; compras: number; totalGasto: number }
export interface RelatorioClientesResponse {
  totalClientes: number
  clientesCompraram: number
  ticketMedioCliente: number
  topClientes: ClienteRankingResponse[]
}
```

- [ ] **Step 3: Criar src/hooks/useDashboard.ts**

`src/hooks/useDashboard.ts`:
```ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { DashboardResponse } from '@/types/dashboard'

export function useDashboard() {
  const [data, setData] = useState<DashboardResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await api.get<DashboardResponse>('/api/dashboard')
      setData(result)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar dashboard')
    } finally {
      setLoading(false)
    }
  }, [])

  return { data, loading, error, load }
}
```

- [ ] **Step 4: Criar src/hooks/useRelatorios.ts**

`src/hooks/useRelatorios.ts`:
```ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  KpisGeralResponse, RelatorioVendasResponse,
  RelatorioFinanceiroResponse, RelatorioEstoqueResponse, RelatorioClientesResponse,
} from '@/types/relatorios'

export function useRelatorios() {
  const [kpis, setKpis] = useState<KpisGeralResponse | null>(null)
  const [vendas, setVendas] = useState<RelatorioVendasResponse | null>(null)
  const [financeiro, setFinanceiro] = useState<RelatorioFinanceiroResponse | null>(null)
  const [estoque, setEstoque] = useState<RelatorioEstoqueResponse | null>(null)
  const [clientes, setClientes] = useState<RelatorioClientesResponse | null>(null)
  const [loading, setLoading] = useState(false)

  function buildQs(de: string, ate: string) {
    return `?de=${encodeURIComponent(de)}&ate=${encodeURIComponent(ate)}`
  }

  const loadKpis = useCallback(async (de: string, ate: string) => {
    setLoading(true)
    try {
      const [k, v, f, e, c] = await Promise.all([
        api.get<KpisGeralResponse>(`/api/relatorios/kpis${buildQs(de, ate)}`),
        api.get<RelatorioVendasResponse>(`/api/relatorios/vendas${buildQs(de, ate)}`),
        api.get<RelatorioFinanceiroResponse>(`/api/relatorios/financeiro${buildQs(de, ate)}`),
        api.get<RelatorioEstoqueResponse>(`/api/relatorios/estoque${buildQs(de, ate)}`),
        api.get<RelatorioClientesResponse>(`/api/relatorios/clientes${buildQs(de, ate)}`),
      ])
      setKpis(k); setVendas(v); setFinanceiro(f); setEstoque(e); setClientes(c)
    } finally {
      setLoading(false)
    }
  }, [])

  return { kpis, vendas, financeiro, estoque, clientes, loading, loadKpis }
}
```

- [ ] **Step 5: Build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend && npm run build
```

Expected: Sem erros.

---

## Task 6: Componentes do Dashboard

**Files:**
- Create: `src/components/dashboard/KpiCard.tsx`
- Create: `src/components/dashboard/GraficoVendas.tsx`
- Create: `src/components/dashboard/GraficoFluxo.tsx`
- Create: `src/components/dashboard/TopProdutos.tsx`

- [ ] **Step 1: Criar diretório**

```bash
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/components/dashboard
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/components/relatorios
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/pages/relatorios
```

- [ ] **Step 2: Criar KpiCard.tsx**

`src/components/dashboard/KpiCard.tsx`:
```tsx
import { type LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

interface Props {
  titulo: string
  valor: string
  icon: LucideIcon
  cor?: 'default' | 'green' | 'red' | 'yellow'
  detalhe?: string
}

const corClasses = {
  default: 'text-primary',
  green: 'text-green-600',
  red: 'text-red-600',
  yellow: 'text-yellow-600',
}

export default function KpiCard({ titulo, valor, icon: Icon, cor = 'default', detalhe }: Props) {
  return (
    <div className="rounded-lg border bg-card p-4 space-y-2">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">{titulo}</p>
        <Icon size={18} className="text-muted-foreground" />
      </div>
      <p className={cn('text-2xl font-bold', corClasses[cor])}>{valor}</p>
      {detalhe && <p className="text-xs text-muted-foreground">{detalhe}</p>}
    </div>
  )
}
```

- [ ] **Step 3: Criar GraficoVendas.tsx**

`src/components/dashboard/GraficoVendas.tsx`:
```tsx
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer
} from 'recharts'
import type { VendasDiaResponse } from '@/types/dashboard'

interface Props { dados: VendasDiaResponse[] }

export default function GraficoVendas({ dados }: Props) {
  const data = dados.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    total: d.total,
    qtd: d.quantidade,
  }))

  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-4">Vendas — últimos 7 dias</p>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={data}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis dataKey="dia" tick={{ fontSize: 12 }} />
          <YAxis tick={{ fontSize: 12 }}
            tickFormatter={v => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 })} />
          <Tooltip
            formatter={(v: number) => [v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }), 'Total']} />
          <Bar dataKey="total" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
```

- [ ] **Step 4: Criar GraficoFluxo.tsx**

`src/components/dashboard/GraficoFluxo.tsx`:
```tsx
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer
} from 'recharts'
import type { FluxoDiaResponse } from '@/types/dashboard'

interface Props { dados: FluxoDiaResponse[] }

export default function GraficoFluxo({ dados }: Props) {
  const data = dados.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    receitas: d.receitas,
    despesas: d.despesas,
  }))

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-4">Entradas vs Saídas — mês atual</p>
      <ResponsiveContainer width="100%" height={200}>
        <LineChart data={data}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis dataKey="dia" tick={{ fontSize: 12 }} />
          <YAxis tick={{ fontSize: 12 }} tickFormatter={v => fmt(v)} />
          <Tooltip formatter={(v: number) => [fmt(v)]} />
          <Legend />
          <Line type="monotone" dataKey="receitas" name="Entradas"
            stroke="hsl(142 76% 36%)" strokeWidth={2} dot={false} />
          <Line type="monotone" dataKey="despesas" name="Saídas"
            stroke="hsl(0 72% 51%)" strokeWidth={2} dot={false} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}
```

- [ ] **Step 5: Criar TopProdutos.tsx**

`src/components/dashboard/TopProdutos.tsx`:
```tsx
import {
  BarChart, Bar, XAxis, YAxis, Tooltip,
  ResponsiveContainer, CartesianGrid
} from 'recharts'
import type { TopProdutoResponse } from '@/types/dashboard'

interface Props { dados: TopProdutoResponse[] }

export default function TopProdutos({ dados }: Props) {
  const data = dados.map(d => ({
    nome: d.nome.length > 15 ? d.nome.slice(0, 15) + '…' : d.nome,
    qtd: d.quantidadeVendida,
    total: d.totalFaturado,
  }))

  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-4">Top 5 produtos mais vendidos</p>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={data} layout="vertical">
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis type="number" tick={{ fontSize: 12 }} />
          <YAxis dataKey="nome" type="category" tick={{ fontSize: 11 }} width={120} />
          <Tooltip
            formatter={(v: number) => [v, 'Unidades vendidas']} />
          <Bar dataKey="qtd" fill="hsl(var(--primary))" radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
```

---

## Task 7: Dashboard completo

**Files:**
- Modify: `src/pages/Dashboard.tsx`

- [ ] **Step 1: Substituir stub pelo Dashboard completo**

`src/pages/Dashboard.tsx`:
```tsx
import { useEffect } from 'react'
import {
  TrendingUp, ShoppingCart, PackageX,
  AlertTriangle, Clock, DollarSign
} from 'lucide-react'
import { useDashboard } from '@/hooks/useDashboard'
import KpiCard from '@/components/dashboard/KpiCard'
import GraficoVendas from '@/components/dashboard/GraficoVendas'
import GraficoFluxo from '@/components/dashboard/GraficoFluxo'
import TopProdutos from '@/components/dashboard/TopProdutos'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function Dashboard() {
  const { data, loading, load } = useDashboard()

  useEffect(() => { load() }, [])

  if (loading || !data) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-muted-foreground">Carregando dashboard...</p>
      </div>
    )
  }

  const { kpis } = data

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <p className="text-sm text-muted-foreground">
          {new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })}
        </p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-3">
        <KpiCard
          titulo="Vendido hoje"
          valor={fmt(kpis.totalVendidoHoje)}
          icon={ShoppingCart}
          cor="green"
        />
        <KpiCard
          titulo="Vendido no mês"
          valor={fmt(kpis.totalVendidoMes)}
          icon={TrendingUp}
        />
        <KpiCard
          titulo="Lucro estimado (mês)"
          valor={fmt(kpis.lucroEstimadoMes)}
          icon={DollarSign}
          cor={kpis.lucroEstimadoMes >= 0 ? 'green' : 'red'}
        />
        <KpiCard
          titulo="Contas a pagar vencidas"
          valor={fmt(kpis.contasPagarVencidas)}
          icon={AlertTriangle}
          cor={kpis.contasPagarVencidas > 0 ? 'red' : 'default'}
        />
        <KpiCard
          titulo="A pagar próx. 7 dias"
          valor={fmt(kpis.contasPagarProximas7Dias)}
          icon={Clock}
          cor={kpis.contasPagarProximas7Dias > 0 ? 'yellow' : 'default'}
        />
        <KpiCard
          titulo="A receber (pendente)"
          valor={fmt(kpis.contasReceberPendentes)}
          icon={TrendingUp}
          cor="green"
        />
      </div>

      {/* Alerta estoque baixo */}
      {kpis.produtosEstoqueBaixo > 0 && (
        <div className="flex items-center gap-2 rounded-md border border-yellow-500/50 bg-yellow-50 p-3 dark:bg-yellow-950/20">
          <PackageX size={18} className="text-yellow-600 shrink-0" />
          <p className="text-sm text-yellow-700 dark:text-yellow-400">
            <strong>{kpis.produtosEstoqueBaixo} produto(s)</strong> com estoque abaixo do mínimo.{' '}
            <a href="/estoque" className="underline font-medium">Ver estoque →</a>
          </p>
        </div>
      )}

      {/* Gráficos */}
      <div className="grid gap-4 lg:grid-cols-2">
        <GraficoVendas dados={data.vendasUltimos7Dias} />
        <GraficoFluxo dados={data.fluxoMes} />
      </div>

      <TopProdutos dados={data.topProdutos} />
    </div>
  )
}
```

---

## Task 8: Componentes de Relatórios

**Files:**
- Create: `src/components/relatorios/FiltrosPeriodo.tsx`
- Create: `src/components/relatorios/AbaVisaoGeral.tsx`
- Create: `src/components/relatorios/AbaVendas.tsx`
- Create: `src/components/relatorios/AbaFinanceiro.tsx`
- Create: `src/components/relatorios/AbaEstoque.tsx`

- [ ] **Step 1: Criar FiltrosPeriodo.tsx**

`src/components/relatorios/FiltrosPeriodo.tsx`:
```tsx
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const PERIODOS = [
  { label: 'Hoje', dias: 0 },
  { label: '7 dias', dias: 7 },
  { label: '30 dias', dias: 30 },
  { label: 'Mês atual', dias: -1 },
]

function isoDate(d: Date) { return d.toISOString().split('T')[0] }

interface Props {
  onChange: (de: string, ate: string) => void
}

export default function FiltrosPeriodo({ onChange }: Props) {
  const [de, setDe] = useState(isoDate(new Date()))
  const [ate, setAte] = useState(isoDate(new Date()))

  function aplicar(preset?: number) {
    const hoje = new Date()
    let inicio = de
    let fim = isoDate(hoje)

    if (preset === 0) {
      inicio = isoDate(hoje)
    } else if (preset && preset > 0) {
      const d = new Date(hoje)
      d.setDate(d.getDate() - preset)
      inicio = isoDate(d)
    } else if (preset === -1) {
      inicio = isoDate(new Date(hoje.getFullYear(), hoje.getMonth(), 1))
    }

    setDe(inicio)
    setAte(fim)
    onChange(inicio, fim)
  }

  return (
    <div className="flex items-center gap-3 flex-wrap">
      {PERIODOS.map(p => (
        <Button key={p.label} size="sm" variant="outline" onClick={() => aplicar(p.dias)}>
          {p.label}
        </Button>
      ))}
      <div className="flex items-center gap-2 ml-2">
        <Input type="date" value={de} onChange={e => setDe(e.target.value)} className="h-8 w-36" />
        <span className="text-muted-foreground text-sm">até</span>
        <Input type="date" value={ate} onChange={e => setAte(e.target.value)} className="h-8 w-36" />
        <Button size="sm" onClick={() => onChange(de, ate)}>Filtrar</Button>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Criar AbaVisaoGeral.tsx**

`src/components/relatorios/AbaVisaoGeral.tsx`:
```tsx
import { TrendingUp, ShoppingCart, DollarSign, AlertTriangle } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { KpisGeralResponse } from '@/types/relatorios'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtPct = (v: number) => `${v.toFixed(1)}%`

interface Props { kpis: KpisGeralResponse }

export default function AbaVisaoGeral({ kpis }: Props) {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-3">
      <KpiCard titulo="Faturamento" valor={fmt(kpis.faturamento)} icon={TrendingUp} cor="green" />
      <KpiCard titulo="Ticket médio" valor={fmt(kpis.ticketMedio)} icon={DollarSign} />
      <KpiCard titulo="Margem estimada" valor={fmtPct(kpis.margemEstimada)} icon={TrendingUp}
        cor={kpis.margemEstimada >= 20 ? 'green' : kpis.margemEstimada >= 10 ? 'yellow' : 'red'} />
      <KpiCard titulo="Total de vendas" valor={kpis.totalVendas.toString()} icon={ShoppingCart} />
      <KpiCard titulo="Inadimplência" valor={fmtPct(kpis.inadimplencia)} icon={AlertTriangle}
        cor={kpis.inadimplencia > 0 ? 'red' : 'default'}
        detalhe="Receitas vencidas / total a receber" />
    </div>
  )
}
```

- [ ] **Step 3: Criar AbaVendas.tsx**

`src/components/relatorios/AbaVendas.tsx`:
```tsx
import {
  AreaChart, Area, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer
} from 'recharts'
import type { RelatorioVendasResponse } from '@/types/relatorios'

const CORES = ['hsl(var(--primary))', 'hsl(142 76% 36%)', 'hsl(38 92% 50%)', 'hsl(0 72% 51%)']
const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioVendasResponse }

export default function AbaVendas({ dados }: Props) {
  const tendencia = dados.tendencia.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    total: d.total,
  }))

  return (
    <div className="space-y-6">
      {/* Tendência */}
      <div className="rounded-lg border bg-card p-4">
        <p className="text-sm font-medium mb-4">Tendência de vendas</p>
        <ResponsiveContainer width="100%" height={220}>
          <AreaChart data={tendencia}>
            <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
            <XAxis dataKey="dia" tick={{ fontSize: 12 }} />
            <YAxis tick={{ fontSize: 12 }} tickFormatter={v => fmt(v)} />
            <Tooltip formatter={(v: number) => [fmt(v), 'Total']} />
            <Area type="monotone" dataKey="total" fill="hsl(var(--primary) / 0.2)"
              stroke="hsl(var(--primary))" strokeWidth={2} />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* Top produtos */}
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Top produtos</p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="py-2 text-left font-medium text-muted-foreground">Produto</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Qtd</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Total</th>
              </tr>
            </thead>
            <tbody>
              {dados.topProdutos.map((p, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2">{p.nome}</td>
                  <td className="py-2 text-right">{p.quantidade}</td>
                  <td className="py-2 text-right font-medium">{fmt(p.total)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Por forma de pagamento */}
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Por forma de pagamento</p>
          {dados.porFormaPagamento.length > 0 ? (
            <ResponsiveContainer width="100%" height={200}>
              <PieChart>
                <Pie data={dados.porFormaPagamento} dataKey="total"
                  nameKey="formaPagamento" cx="50%" cy="50%" outerRadius={80}
                  label={({ formaPagamento, percent }) =>
                    `${formaPagamento} ${(percent * 100).toFixed(0)}%`}>
                  {dados.porFormaPagamento.map((_, i) => (
                    <Cell key={i} fill={CORES[i % CORES.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(v: number) => [fmt(v)]} />
              </PieChart>
            </ResponsiveContainer>
          ) : <p className="text-sm text-muted-foreground py-8 text-center">Sem dados</p>}
        </div>
      </div>

      {/* Top clientes */}
      {dados.topClientes.length > 0 && (
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Top clientes</p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="py-2 text-left font-medium text-muted-foreground">Cliente</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Compras</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Total</th>
              </tr>
            </thead>
            <tbody>
              {dados.topClientes.map((c, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2">{c.nome}</td>
                  <td className="py-2 text-right">{c.compras}</td>
                  <td className="py-2 text-right font-medium">{fmt(c.total)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 4: Criar AbaFinanceiro.tsx**

`src/components/relatorios/AbaFinanceiro.tsx`:
```tsx
import {
  ComposedChart, Bar, Line, XAxis, YAxis,
  CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  PieChart, Pie, Cell
} from 'recharts'
import type { RelatorioFinanceiroResponse } from '@/types/relatorios'

const CORES = ['hsl(0 72% 51%)', 'hsl(38 92% 50%)', 'hsl(262 80% 50%)', 'hsl(142 76% 36%)', 'hsl(200 98% 39%)']
const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioFinanceiroResponse }

export default function AbaFinanceiro({ dados }: Props) {
  const fluxo = dados.fluxoPorDia.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    receitas: d.receitas,
    despesas: d.despesas,
    saldo: d.saldo,
  }))

  return (
    <div className="space-y-6">
      {/* Resumo */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Total Receitas', valor: fmt(dados.totalReceitas), cor: 'text-green-600' },
          { label: 'Total Despesas', valor: fmt(dados.totalDespesas), cor: 'text-red-600' },
          { label: 'Saldo', valor: fmt(dados.saldo), cor: dados.saldo >= 0 ? 'text-green-600' : 'text-red-600' },
        ].map(item => (
          <div key={item.label} className="rounded-lg border bg-card p-4">
            <p className="text-sm text-muted-foreground">{item.label}</p>
            <p className={`text-xl font-bold ${item.cor}`}>{item.valor}</p>
          </div>
        ))}
      </div>

      {/* Fluxo de caixa */}
      <div className="rounded-lg border bg-card p-4">
        <p className="text-sm font-medium mb-4">Fluxo de caixa por dia</p>
        <ResponsiveContainer width="100%" height={240}>
          <ComposedChart data={fluxo}>
            <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
            <XAxis dataKey="dia" tick={{ fontSize: 12 }} />
            <YAxis tick={{ fontSize: 12 }} tickFormatter={v => fmt(v)} />
            <Tooltip formatter={(v: number) => [fmt(v)]} />
            <Legend />
            <Bar dataKey="receitas" name="Receitas" fill="hsl(142 76% 36%)" />
            <Bar dataKey="despesas" name="Despesas" fill="hsl(0 72% 51%)" />
            <Line type="monotone" dataKey="saldo" name="Saldo"
              stroke="hsl(var(--primary))" strokeWidth={2} dot={false} />
          </ComposedChart>
        </ResponsiveContainer>
      </div>

      {/* Categorias despesas */}
      {dados.categoriasDespesas.length > 0 && (
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-4">Despesas por categoria</p>
          <div className="grid lg:grid-cols-2 gap-4 items-center">
            <ResponsiveContainer width="100%" height={200}>
              <PieChart>
                <Pie data={dados.categoriasDespesas} dataKey="total"
                  nameKey="categoria" cx="50%" cy="50%" outerRadius={80}>
                  {dados.categoriasDespesas.map((_, i) => (
                    <Cell key={i} fill={CORES[i % CORES.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(v: number) => [fmt(v)]} />
              </PieChart>
            </ResponsiveContainer>
            <table className="text-sm w-full">
              <tbody>
                {dados.categoriasDespesas.map((c, i) => (
                  <tr key={i} className="border-b last:border-0">
                    <td className="py-1.5 flex items-center gap-2">
                      <span className="w-3 h-3 rounded-full shrink-0"
                        style={{ background: CORES[i % CORES.length] }} />
                      {c.categoria}
                    </td>
                    <td className="py-1.5 text-right font-medium">{fmt(c.total)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 5: Criar AbaEstoque.tsx**

`src/components/relatorios/AbaEstoque.tsx`:
```tsx
import type { RelatorioEstoqueResponse } from '@/types/relatorios'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioEstoqueResponse }

export default function AbaEstoque({ dados }: Props) {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Valor total em estoque', valor: fmt(dados.valorTotalEstoque) },
          { label: 'Produtos ativos', valor: dados.produtosAtivos.toString() },
          { label: 'Com estoque baixo', valor: dados.produtosEstoqueBaixo.toString(), destaque: dados.produtosEstoqueBaixo > 0 },
        ].map(item => (
          <div key={item.label} className="rounded-lg border bg-card p-4">
            <p className="text-sm text-muted-foreground">{item.label}</p>
            <p className={`text-xl font-bold ${item.destaque ? 'text-red-600' : ''}`}>{item.valor}</p>
          </div>
        ))}
      </div>

      <div className="grid lg:grid-cols-2 gap-4">
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Giro de estoque (período)</p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="py-2 text-left font-medium text-muted-foreground">Produto</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Saídas</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Entradas</th>
              </tr>
            </thead>
            <tbody>
              {dados.giroProdutos.map((p, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2">{p.nome}</td>
                  <td className="py-2 text-right">{p.saidas}</td>
                  <td className="py-2 text-right">{p.entradas}</td>
                </tr>
              ))}
              {dados.giroProdutos.length === 0 && (
                <tr><td colSpan={3} className="py-6 text-center text-muted-foreground">Sem movimentações no período</td></tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Produtos sem movimentação</p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="py-2 text-left font-medium text-muted-foreground">Produto</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Estoque</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Valor</th>
              </tr>
            </thead>
            <tbody>
              {dados.semMovimentacao.map((p, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2">{p.nome}</td>
                  <td className="py-2 text-right">{p.estoqueAtual}</td>
                  <td className="py-2 text-right">{fmt(p.valorEmEstoque)}</td>
                </tr>
              ))}
              {dados.semMovimentacao.length === 0 && (
                <tr><td colSpan={3} className="py-6 text-center text-muted-foreground">Todos os produtos tiveram movimentação</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
```

---

## Task 9: Página de Relatórios completa

**Files:**
- Create: `src/pages/relatorios/Relatorios.tsx`

- [ ] **Step 1: Criar Relatorios.tsx**

`src/pages/relatorios/Relatorios.tsx`:
```tsx
import { useState, useEffect } from 'react'
import { Download } from 'lucide-react'
import { useRelatorios } from '@/hooks/useRelatorios'
import { Button } from '@/components/ui/button'
import FiltrosPeriodo from '@/components/relatorios/FiltrosPeriodo'
import AbaVisaoGeral from '@/components/relatorios/AbaVisaoGeral'
import AbaVendas from '@/components/relatorios/AbaVendas'
import AbaFinanceiro from '@/components/relatorios/AbaFinanceiro'
import AbaEstoque from '@/components/relatorios/AbaEstoque'
import { cn } from '@/lib/utils'

type Aba = 'visao-geral' | 'vendas' | 'financeiro' | 'estoque'
const ABAS: { id: Aba; label: string }[] = [
  { id: 'visao-geral', label: 'Visão Geral' },
  { id: 'vendas', label: 'Vendas' },
  { id: 'financeiro', label: 'Financeiro' },
  { id: 'estoque', label: 'Estoque' },
]

function exportarCSV(nome: string, linhas: string[][]) {
  const csv = linhas.map(l => l.join(';')).join('\n')
  const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url; a.download = `${nome}.csv`; a.click()
  URL.revokeObjectURL(url)
}

export default function Relatorios() {
  const { kpis, vendas, financeiro, estoque, loading, loadKpis } = useRelatorios()
  const [aba, setAba] = useState<Aba>('visao-geral')
  const [periodo, setPeriodo] = useState({ de: '', ate: '' })

  const hoje = new Date().toISOString().split('T')[0]
  const inicioMes = `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-01`

  useEffect(() => {
    loadKpis(inicioMes, hoje)
    setPeriodo({ de: inicioMes, ate: hoje })
  }, [])

  function handlePeriodo(de: string, ate: string) {
    setPeriodo({ de, ate })
    loadKpis(de, ate)
  }

  function handleExportar() {
    if (aba === 'vendas' && vendas) {
      exportarCSV(`relatorio-vendas-${periodo.de}-${periodo.ate}`, [
        ['Data', 'Total', 'Quantidade'],
        ...vendas.tendencia.map(d => [d.data, d.total.toString(), d.quantidade.toString()]),
      ])
    } else if (aba === 'financeiro' && financeiro) {
      exportarCSV(`relatorio-financeiro-${periodo.de}-${periodo.ate}`, [
        ['Data', 'Receitas', 'Despesas', 'Saldo'],
        ...financeiro.fluxoPorDia.map(d => [d.data, d.receitas.toString(), d.despesas.toString(), d.saldo.toString()]),
      ])
    } else {
      window.print()
    }
  }

  return (
    <div className="space-y-4 print:space-y-2">
      <div className="flex items-center justify-between print:hidden">
        <h1 className="text-2xl font-bold">Relatórios</h1>
        <Button variant="outline" onClick={handleExportar}>
          <Download size={16} className="mr-2" />
          Exportar {aba === 'vendas' || aba === 'financeiro' ? 'CSV' : 'PDF'}
        </Button>
      </div>

      <div className="print:hidden">
        <FiltrosPeriodo onChange={handlePeriodo} />
      </div>

      {/* Abas */}
      <div className="flex gap-1 border-b print:hidden">
        {ABAS.map(a => (
          <button key={a.id}
            onClick={() => setAba(a.id)}
            className={cn(
              'px-4 py-2 text-sm font-medium border-b-2 transition-colors',
              aba === a.id
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            )}>
            {a.label}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="flex h-48 items-center justify-center">
          <p className="text-muted-foreground">Carregando relatórios...</p>
        </div>
      ) : (
        <>
          {aba === 'visao-geral' && kpis && <AbaVisaoGeral kpis={kpis} />}
          {aba === 'vendas' && vendas && <AbaVendas dados={vendas} />}
          {aba === 'financeiro' && financeiro && <AbaFinanceiro dados={financeiro} />}
          {aba === 'estoque' && estoque && <AbaEstoque dados={estoque} />}
        </>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Adicionar CSS de impressão em src/index.css**

Adicionar ao final de `src/index.css`:
```css
@media print {
  aside { display: none !important; }
  .print\:hidden { display: none !important; }
  body { background: white; }
}
```

---

## Task 10: Wiring final e verificação

**Files:**
- Modify: `src/router/index.tsx`

- [ ] **Step 1: Atualizar router com todas as rotas finais**

`src/router/index.tsx`:
```tsx
import { createBrowserRouter } from 'react-router-dom'
import AppLayout from '@/components/layout/AppLayout'
import Auth from '@/pages/Auth'
import AuthCallback from '@/pages/AuthCallback'
import Dashboard from '@/pages/Dashboard'
import Produtos from '@/pages/estoque/Produtos'
import Movimentacoes from '@/pages/estoque/Movimentacoes'
import Clientes from '@/pages/clientes/Clientes'
import NovaVenda from '@/pages/vendas/NovaVenda'
import Historico from '@/pages/vendas/Historico'
import Lancamentos from '@/pages/financeiro/Lancamentos'
import ContasPagar from '@/pages/financeiro/ContasPagar'
import ContasReceber from '@/pages/financeiro/ContasReceber'
import Relatorios from '@/pages/relatorios/Relatorios'

export const router = createBrowserRouter([
  { path: '/auth', element: <Auth /> },
  { path: '/auth/callback', element: <AuthCallback /> },
  {
    element: <AppLayout />,
    children: [
      { path: '/', element: <Dashboard /> },
      { path: '/vendas', element: <Historico /> },
      { path: '/vendas/nova', element: <NovaVenda /> },
      { path: '/estoque', element: <Produtos /> },
      { path: '/estoque/movimentacoes', element: <Movimentacoes /> },
      { path: '/financeiro', element: <Lancamentos /> },
      { path: '/financeiro/pagar', element: <ContasPagar /> },
      { path: '/financeiro/receber', element: <ContasReceber /> },
      { path: '/clientes', element: <Clientes /> },
      { path: '/relatorios', element: <Relatorios /> },
    ],
  },
])
```

- [ ] **Step 2: Verificar build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend && npm run build
```

Expected: Sem erros TypeScript.

- [ ] **Step 3: Rodar todos os testes**

```bash
npx vitest run
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend && dotnet test
```

Expected: Todos os testes passando nos dois projetos.

- [ ] **Step 4: Iniciar serviços e validar manualmente**

```bash
# Terminal 1 — Backend
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
docker compose up gestorai-db -d
cd backend && dotnet run --project src/GestorAI.API

# Terminal 2 — Frontend
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npm run dev
```

Validar em `http://localhost:5174`:
- [ ] Dashboard carrega com KPIs e gráficos
- [ ] Página Relatórios carrega com abas e filtro de período
- [ ] Botão Exportar gera CSV para Vendas e Financeiro
- [ ] `window.print()` formata corretamente para impressão (sidebar oculta)

- [ ] **Step 5: Commit final**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add .
git commit -m "feat: Plano 5 completo — Dashboard e BI funcionais"
```

---

## Checklist de entrega

- [ ] `GET /api/dashboard` retorna KPIs do dia/mês, série de 7 dias, fluxo do mês e top 5 produtos
- [ ] `GET /api/relatorios/kpis` calcula faturamento, ticket médio, margem e inadimplência
- [ ] `GET /api/relatorios/vendas` retorna tendência, ranking de produtos, ranking de clientes e por pagamento
- [ ] `GET /api/relatorios/financeiro` retorna fluxo de caixa e categorias de despesas
- [ ] `GET /api/relatorios/estoque` retorna giro e produtos sem movimentação
- [ ] Testes passando: DashboardServiceTests (3)
- [ ] Dashboard renderiza 6 KPI cards + 3 gráficos Recharts
- [ ] Alerta de estoque baixo aparece quando há produtos abaixo do mínimo
- [ ] Página Relatórios tem 4 abas com filtro de período funcional
- [ ] Exportação CSV funciona para abas Vendas e Financeiro
- [ ] Impressão PDF formata corretamente (sidebar oculta via CSS)
- [ ] Build TypeScript sem erros; todos os testes passando
