using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.PublicBooking;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class PublicBookingServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, PublicBookingService service) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new PublicBookingService(db, null!));
    }

    [Fact]
    public async Task ResolveEmpresaAsync_RetornaEmpresaId_QuandoSlugExiste()
    {
        var (db, svc) = Setup();
        db.ConfiguracoesEmpresa.Add(new ConfiguracaoEmpresa
        {
            EmpresaId = _empresaId,
            Slug = "minha-empresa",
        });
        await db.SaveChangesAsync();

        var result = await svc.ResolveEmpresaAsync("minha-empresa", default);

        Assert.Equal(_empresaId, result);
    }

    [Fact]
    public async Task ResolveEmpresaAsync_Lanca404_QuandoSlugNaoExiste()
    {
        var (_, svc) = Setup();

        await Assert.ThrowsAsync<AppException>(() =>
            svc.ResolveEmpresaAsync("nao-existe", default));
    }

    [Fact]
    public async Task GetServicosAsync_RetornaApenasServicosAtivos()
    {
        var (db, svc) = Setup();

        var categoriaId = Guid.NewGuid();
        db.Categorias.Add(new Categoria
        {
            Id = categoriaId,
            EmpresaId = _empresaId,
            Nome = "Categoria Teste",
        });

        db.Produtos.AddRange(
            // Servico ativo com duracao — deve aparecer
            new Produto
            {
                EmpresaId = _empresaId,
                CategoriaId = categoriaId,
                Nome = "Corte de Cabelo",
                Tipo = TipoProduto.Servico,
                Ativo = true,
                DuracaoMinutos = 30,
            },
            // Servico inativo — não deve aparecer
            new Produto
            {
                EmpresaId = _empresaId,
                CategoriaId = categoriaId,
                Nome = "Escova Inativa",
                Tipo = TipoProduto.Servico,
                Ativo = false,
                DuracaoMinutos = 45,
            },
            // Produto (não é serviço) — não deve aparecer
            new Produto
            {
                EmpresaId = _empresaId,
                CategoriaId = categoriaId,
                Nome = "Shampoo",
                Tipo = TipoProduto.Produto,
                Ativo = true,
                DuracaoMinutos = null,
            }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetServicosAsync(_empresaId, default);

        Assert.Single(result);
        Assert.Equal("Corte de Cabelo", result[0].Nome);
    }

    [Fact]
    public async Task CriarAgendamentoAsync_CriaComStatusAguardandoConfirmacao()
    {
        var (db, svc) = Setup();

        var categoriaId = Guid.NewGuid();
        var profissionalId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();

        db.Categorias.Add(new Categoria
        {
            Id = categoriaId,
            EmpresaId = _empresaId,
            Nome = "Categoria",
        });

        db.Profissionais.Add(new Profissional
        {
            Id = profissionalId,
            EmpresaId = _empresaId,
            Nome = "João",
            Ativo = true,
        });

        db.Produtos.Add(new Produto
        {
            Id = servicoId,
            EmpresaId = _empresaId,
            CategoriaId = categoriaId,
            Nome = "Corte",
            Tipo = TipoProduto.Servico,
            Ativo = true,
            DuracaoMinutos = 60,
        });

        await db.SaveChangesAsync();

        var dataHoraInicio = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        var req = new PublicCriarAgendamentoRequest(
            ServicoId: servicoId,
            ProfissionalId: profissionalId,
            DataHoraInicio: dataHoraInicio,
            ClienteNome: "Maria",
            ClienteTelefone: "11999990001");

        var result = await svc.CriarAgendamentoAsync(_empresaId, req, default);

        Assert.NotEqual(Guid.Empty, result.Id);

        var agendamento = await db.Agendamentos.FindAsync(result.Id);
        Assert.NotNull(agendamento);
        Assert.Equal(AgendamentoStatus.AguardandoConfirmacao, agendamento.Status);
    }

    [Fact]
    public async Task CriarAgendamentoAsync_LancaConflito_QuandoHorarioOcupado()
    {
        var (db, svc) = Setup();

        var categoriaId = Guid.NewGuid();
        var profissionalId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();

        db.Categorias.Add(new Categoria
        {
            Id = categoriaId,
            EmpresaId = _empresaId,
            Nome = "Categoria",
        });

        db.Profissionais.Add(new Profissional
        {
            Id = profissionalId,
            EmpresaId = _empresaId,
            Nome = "Ana",
            Ativo = true,
        });

        db.Produtos.Add(new Produto
        {
            Id = servicoId,
            EmpresaId = _empresaId,
            CategoriaId = categoriaId,
            Nome = "Massagem",
            Tipo = TipoProduto.Servico,
            Ativo = true,
            DuracaoMinutos = 60,
        });

        var dataHoraInicio = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);

        // Agendamento existente ocupando das 09:00 às 10:00
        db.Agendamentos.Add(new Agendamento
        {
            EmpresaId = _empresaId,
            ProfissionalId = profissionalId,
            ServicoId = servicoId,
            ClienteNome = "Cliente Anterior",
            ClienteTelefone = "11999990000",
            DataHoraInicio = dataHoraInicio,
            DataHoraFim = dataHoraInicio.AddMinutes(60),
            Status = AgendamentoStatus.Agendado,
        });

        await db.SaveChangesAsync();

        var req = new PublicCriarAgendamentoRequest(
            ServicoId: servicoId,
            ProfissionalId: profissionalId,
            DataHoraInicio: dataHoraInicio,
            ClienteNome: "Novo Cliente",
            ClienteTelefone: "11999990002");

        await Assert.ThrowsAsync<AppException>(() =>
            svc.CriarAgendamentoAsync(_empresaId, req, default));
    }
}
