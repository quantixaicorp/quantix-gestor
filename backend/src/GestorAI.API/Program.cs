using FluentValidation;
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.DTOs.Clientes;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.DTOs.Vendas;
using GestorAI.API.Endpoints;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Agendamentos;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Services.Dashboard;
using GestorAI.API.Services.Estoque;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Services.Orcamentos;
using GestorAI.API.Services.Relatorios;
using GestorAI.API.Services.Vendas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<TenantContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.Authority = builder.Configuration["Jwt:Authority"];
        opt.Audience = builder.Configuration["Jwt:Audience"];
        opt.RequireHttpsMetadata = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
    opt.AddPolicy("FinanceAccess", p => p.RequireRole("admin", "financeiro"));
    opt.AddPolicy("EstoqueAccess", p => p.RequireRole("admin", "estoque"));
    opt.AddPolicy("VendasAccess", p => p.RequireRole("admin", "vendas", "estoque"));
});

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Services — Estoque
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<ProdutoService>();
// Services — Clientes
builder.Services.AddScoped<ClienteService>();
// Services — Vendas
builder.Services.AddScoped<VendaService>();
// Services — Financeiro
builder.Services.AddScoped<LancamentoService>();
// Services — Dashboard
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<RelatorioService>();
builder.Services.AddScoped<OrcamentoService>();
// Services — Agendamentos
builder.Services.AddScoped<ProfissionalService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<IValidator<CreateLancamentoRequest>, CreateLancamentoValidator>();
builder.Services.AddScoped<IValidator<CriarAgendamentoRequest>, CriarAgendamentoValidator>();
// Validators
builder.Services.AddScoped<IValidator<CreateProdutoRequest>, CreateProdutoValidator>();
builder.Services.AddScoped<IValidator<EntradaEstoqueRequest>, EntradaEstoqueValidator>();
builder.Services.AddScoped<IValidator<CreateClienteRequest>, CreateClienteValidator>();
builder.Services.AddScoped<IValidator<CreateVendaRequest>, CreateVendaValidator>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapEstoque();
app.MapClientes();
app.MapVendas();
app.MapFinanceiro();
app.MapDashboard();
app.MapOrcamentos();
app.MapProfissionais();
app.MapAgendamentos();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .AllowAnonymous();

app.Run();

public partial class Program { }
