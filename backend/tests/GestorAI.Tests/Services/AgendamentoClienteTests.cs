using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Agendamentos;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Services.PublicBooking;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

/// <summary>Agendar um cliente que ainda não está na base deve cadastrá-lo.</summary>
public class AgendamentoClienteTests
{
    private readonly Guid _empresaId = Guid.NewGuid();
    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _servicoId = Guid.NewGuid();
    private static readonly DateTime Inicio = new(2027, 6, 10, 9, 0, 0, DateTimeKind.Unspecified);

    private AppDbContext CreateDb()
    {
        var tc = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);

        db.CompanySettings.Add(new CompanySettings { CompanyId = _empresaId, Slug = "empresa" });
        db.Professionals.Add(new Professional
        {
            Id = _profissionalId, CompanyId = _empresaId, Name = "Dr. Carlos", IsActive = true,
        });
        db.Products.Add(new Product
        {
            Id = _servicoId, CompanyId = _empresaId, Name = "Consulta",
            Type = TipoProduto.Servico, SalePrice = 100m, DurationMinutes = 30, IsActive = true,
        });
        db.WeeklyAvailabilities.Add(new WeeklyAvailability
        {
            ProfessionalId = _profissionalId,
            WeekDay = (int)Inicio.DayOfWeek,
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(18),
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2030, 1, 1),
        });
        db.SaveChanges();
        return db;
    }

    private AgendamentoService Interno(AppDbContext db) =>
        new(db, new TenantContext { CompanyId = _empresaId }, new ClienteLookup(db));

    private PublicBookingService Publico(AppDbContext db) =>
        new(db, null!, new ClienteLookup(db));

    private CriarAgendamentoRequest ReqInterno(string nome, string telefone, Guid? clienteId = null) =>
        new(_profissionalId, nome, telefone, clienteId, _servicoId, Inicio, null);

    private PublicCriarAgendamentoRequest ReqPublico(string nome, string telefone) =>
        new(_servicoId, _profissionalId, Inicio, nome, telefone);

    // ---------- tela interna ----------

    [Fact]
    public async Task Interno_CadastraClienteNovoEVinculaAoAgendamento()
    {
        var db = CreateDb();

        var result = await Interno(db).CriarAsync(ReqInterno("Maria", "11999990001"), default);

        var cliente = Assert.Single(db.Customers.ToList());
        Assert.Equal("Maria", cliente.Name);
        Assert.Equal("11999990001", cliente.WhatsApp);

        var agendamento = await db.Appointments.FindAsync(result.Id);
        Assert.Equal(cliente.Id, agendamento!.CustomerId);
    }

    [Fact]
    public async Task Interno_ReusaClienteExistentePeloTelefone()
    {
        var db = CreateDb();
        var existente = new Customer { CompanyId = _empresaId, Name = "Maria", WhatsApp = "11999990001" };
        db.Customers.Add(existente);
        await db.SaveChangesAsync();

        var result = await Interno(db).CriarAsync(ReqInterno("Maria Souza", "11999990001"), default);

        Assert.Single(db.Customers.ToList());
        var agendamento = await db.Appointments.FindAsync(result.Id);
        Assert.Equal(existente.Id, agendamento!.CustomerId);
    }

    [Fact]
    public async Task Interno_ReusaClienteExistenteMesmoComMascaraDiferente()
    {
        var db = CreateDb();
        var existente = new Customer { CompanyId = _empresaId, Name = "Maria", WhatsApp = "(11) 99999-0001" };
        db.Customers.Add(existente);
        await db.SaveChangesAsync();

        var result = await Interno(db).CriarAsync(ReqInterno("Maria", "11999990001"), default);

        Assert.Single(db.Customers.ToList());
        Assert.Equal("(11) 99999-0001", db.Customers.Single().WhatsApp);
        var agendamento = await db.Appointments.FindAsync(result.Id);
        Assert.Equal(existente.Id, agendamento!.CustomerId);
    }

    [Fact]
    public async Task Interno_RespeitaClienteJaEscolhidoPeloOperador()
    {
        var db = CreateDb();
        var escolhido = new Customer { CompanyId = _empresaId, Name = "Maria", WhatsApp = "11888880002" };
        db.Customers.Add(escolhido);
        await db.SaveChangesAsync();

        var result = await Interno(db).CriarAsync(
            ReqInterno("Maria", "11999990001", clienteId: escolhido.Id), default);

        Assert.Single(db.Customers.ToList());
        var agendamento = await db.Appointments.FindAsync(result.Id);
        Assert.Equal(escolhido.Id, agendamento!.CustomerId);
    }

    [Fact]
    public async Task Interno_NaoCadastraClienteQuandoAgendamentoEhRejeitado()
    {
        var db = CreateDb();
        var foraDoHorario = Inicio.Date.AddHours(23);

        await Assert.ThrowsAsync<GestorAI.API.Shared.Exceptions.AppException>(() =>
            Interno(db).CriarAsync(
                new CriarAgendamentoRequest(
                    _profissionalId, "Maria", "11999990001", null, _servicoId, foraDoHorario, null),
                default));

        Assert.Empty(db.Customers.ToList());
    }

    // ---------- agendamento público ----------

    [Fact]
    public async Task Publico_CadastraClienteNovoEVinculaAoAgendamento()
    {
        var db = CreateDb();

        var result = await Publico(db).CriarAgendamentoAsync(
            _empresaId, ReqPublico("João", "11977770003"), default);

        var cliente = Assert.Single(db.Customers.ToList());
        Assert.Equal("João", cliente.Name);

        var agendamento = await db.Appointments.FindAsync(result.Id);
        Assert.Equal(cliente.Id, agendamento!.CustomerId);
    }

    [Fact]
    public async Task Publico_ReusaClienteExistentePeloTelefone()
    {
        var db = CreateDb();
        var existente = new Customer { CompanyId = _empresaId, Name = "João", WhatsApp = "11977770003" };
        db.Customers.Add(existente);
        await db.SaveChangesAsync();

        var result = await Publico(db).CriarAgendamentoAsync(
            _empresaId, ReqPublico("João", "11977770003"), default);

        Assert.Single(db.Customers.ToList());
        var agendamento = await db.Appointments.FindAsync(result.Id);
        Assert.Equal(existente.Id, agendamento!.CustomerId);
    }

    [Fact]
    public async Task Publico_NaoVazaClienteDeOutraEmpresa()
    {
        var db = CreateDb();
        var outraEmpresa = new Customer
        {
            CompanyId = Guid.NewGuid(), Name = "João", WhatsApp = "11977770003",
        };
        db.Customers.Add(outraEmpresa);
        await db.SaveChangesAsync();

        var result = await Publico(db).CriarAgendamentoAsync(
            _empresaId, ReqPublico("João", "11977770003"), default);

        var agendamento = await db.Appointments.FindAsync(result.Id);
        Assert.NotEqual(outraEmpresa.Id, agendamento!.CustomerId);
        Assert.Equal(2, db.Customers.IgnoreQueryFilters().Count());
    }
}
