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
        var tenantContext = new TenantContext { CompanyId = _empresaId };
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
        db.ConfiguracoesEmpresa.Add(new CompanySettings
        {
            CompanyId = _empresaId,
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
        db.Categorias.Add(new Category
        {
            Id = categoriaId,
            CompanyId = _empresaId,
            Name = "Category Teste",
        });

        db.Produtos.AddRange(
            // Servico ativo com duracao — deve aparecer
            new Product
            {
                CompanyId = _empresaId,
                CategoryId = categoriaId,
                Name = "Corte de Cabelo",
                Type = TipoProduto.Servico,
                IsActive = true,
                DurationMinutes = 30,
            },
            // Servico inativo — não deve aparecer
            new Product
            {
                CompanyId = _empresaId,
                CategoryId = categoriaId,
                Name = "Escova Inativa",
                Type = TipoProduto.Servico,
                IsActive = false,
                DurationMinutes = 45,
            },
            // Product (não é serviço) — não deve aparecer
            new Product
            {
                CompanyId = _empresaId,
                CategoryId = categoriaId,
                Name = "Shampoo",
                Type = TipoProduto.Product,
                IsActive = true,
                DurationMinutes = null,
            }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetServicosAsync(_empresaId, default);

        Assert.Single(result);
        Assert.Equal("Corte de Cabelo", result[0].Name);
    }

    [Fact]
    public async Task CriarAgendamentoAsync_CriaComStatusAguardandoConfirmacao()
    {
        var (db, svc) = Setup();

        var categoriaId = Guid.NewGuid();
        var profissionalId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();

        db.Categorias.Add(new Category
        {
            Id = categoriaId,
            CompanyId = _empresaId,
            Name = "Category",
        });

        db.Profissionais.Add(new Professional
        {
            Id = profissionalId,
            CompanyId = _empresaId,
            Name = "João",
            IsActive = true,
        });

        db.Produtos.Add(new Product
        {
            Id = servicoId,
            CompanyId = _empresaId,
            CategoryId = categoriaId,
            Name = "Corte",
            Type = TipoProduto.Servico,
            IsActive = true,
            DurationMinutes = 60,
        });

        await db.SaveChangesAsync();

        var dataHoraInicio = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        var req = new PublicCriarAgendamentoRequest(
            ServicoId: servicoId,
            ProfessionalId: profissionalId,
            StartAt: dataHoraInicio,
            CustomerName: "Maria",
            CustomerPhone: "11999990001");

        var result = await svc.CriarAgendamentoAsync(_empresaId, req, default);

        Assert.NotEqual(Guid.Empty, result.Id);

        var agendamento = await db.Appointments.FindAsync(result.Id);
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

        db.Categorias.Add(new Category
        {
            Id = categoriaId,
            CompanyId = _empresaId,
            Name = "Category",
        });

        db.Profissionais.Add(new Professional
        {
            Id = profissionalId,
            CompanyId = _empresaId,
            Name = "Ana",
            IsActive = true,
        });

        db.Produtos.Add(new Product
        {
            Id = servicoId,
            CompanyId = _empresaId,
            CategoryId = categoriaId,
            Name = "Massagem",
            Type = TipoProduto.Servico,
            IsActive = true,
            DurationMinutes = 60,
        });

        var dataHoraInicio = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);

        // Appointment existente ocupando das 09:00 às 10:00
        db.Appointments.Add(new Appointment
        {
            CompanyId = _empresaId,
            ProfessionalId = profissionalId,
            ServicoId = servicoId,
            CustomerName = "Customer Anterior",
            CustomerPhone = "11999990000",
            StartAt = dataHoraInicio,
            EndAt = dataHoraInicio.AddMinutes(60),
            Status = AgendamentoStatus.Agendado,
        });

        await db.SaveChangesAsync();

        var req = new PublicCriarAgendamentoRequest(
            ServicoId: servicoId,
            ProfessionalId: profissionalId,
            StartAt: dataHoraInicio,
            CustomerName: "Novo Customer",
            CustomerPhone: "11999990002");

        await Assert.ThrowsAsync<AppException>(() =>
            svc.CriarAgendamentoAsync(_empresaId, req, default));
    }
}
