using FluentValidation;
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.DTOs.Clientes;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.DTOs.Fornecedores;
using GestorAI.API.DTOs.Vendas;
using GestorAI.API.Endpoints;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Agendamentos;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Services.Dashboard;
using GestorAI.API.Services.Fornecedores;
using GestorAI.API.Services.Fiscal;
using GestorAI.API.Services.Estoque;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Services.Contratos;
using GestorAI.API.Services.Asaas;
using GestorAI.API.Services.Automacao;
using GestorAI.API.Services;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Services.Orcamentos;
using GestorAI.API.Services.PublicBooking;
using GestorAI.API.Services.Relatorios;
using GestorAI.API.Services.Vendas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt =>
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
       .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<TenantContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.Authority = builder.Configuration["Jwt:Authority"];
        opt.Audience = builder.Configuration["Jwt:Audience"];
        opt.RequireHttpsMetadata = false;
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            RoleClaimType = "roles",
        };
    });

builder.Services.AddAuthorization(opt =>
{
    static bool superAdmin(System.Security.Claims.ClaimsPrincipal u) =>
        u.HasClaim("is_superadmin", "true");

    opt.AddPolicy("AdminOnly", p => p.RequireAssertion(ctx =>
        superAdmin(ctx.User) || ctx.User.IsInRole("admin")));
    opt.AddPolicy("FinanceAccess", p => p.RequireAssertion(ctx =>
        superAdmin(ctx.User) || ctx.User.IsInRole("admin") || ctx.User.IsInRole("financeiro")));
    opt.AddPolicy("EstoqueAccess", p => p.RequireAssertion(ctx =>
        superAdmin(ctx.User) || ctx.User.IsInRole("admin") || ctx.User.IsInRole("estoque")));
    opt.AddPolicy("VendasAccess", p => p.RequireAssertion(ctx =>
        superAdmin(ctx.User) || ctx.User.IsInRole("admin") ||
        ctx.User.IsInRole("vendas") || ctx.User.IsInRole("estoque")));
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
// Services — Fornecedores
builder.Services.AddScoped<FornecedorService>();
// Services — Vendas
builder.Services.AddScoped<VendaService>();
// Services — Financeiro
builder.Services.AddScoped<LancamentoService>();
builder.Services.AddScoped<CategoriaLancamentoService>();
// Services — Dashboard
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<RelatorioService>();
builder.Services.AddScoped<OrcamentoService>();
// Services — Agendamentos
builder.Services.AddScoped<ProfissionalService>();
builder.Services.AddScoped<AgendamentoService>();
// Services — Fiscal
builder.Services.AddScoped<NotaFiscalService>();
builder.Services.AddScoped<ConfiguracaoEmpresaService>();
builder.Services.AddScoped<ContratoService>();
builder.Services.AddScoped<ContratoTemplateService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<AsaasService>();
builder.Services.AddScoped<ClickSignService>();
builder.Services.AddScoped<IEvolutionApiService, EvolutionApiService>();
builder.Services.AddScoped<LembreteCobrancaService>();
builder.Services.AddScoped<GeracaoCobrancaService>();
builder.Services.AddHostedService<AutomacaoHostedService>();
builder.Services.AddScoped<CobrancaService>();
builder.Services.AddScoped<FeatureService>();
builder.Services.AddScoped<PlanoService>();
builder.Services.AddScoped<PublicBookingService>();
builder.Services.AddScoped<IValidator<CreateLancamentoRequest>, CreateLancamentoValidator>();
builder.Services.AddScoped<IValidator<UpdateLancamentoRequest>, UpdateLancamentoValidator>();
builder.Services.AddScoped<IValidator<CriarAgendamentoRequest>, CriarAgendamentoValidator>();
// Validators
builder.Services.AddScoped<IValidator<CreateProdutoRequest>, CreateProdutoValidator>();
builder.Services.AddScoped<IValidator<EntradaEstoqueRequest>, EntradaEstoqueValidator>();
builder.Services.AddScoped<IValidator<CreateClienteRequest>, CreateClienteValidator>();
builder.Services.AddScoped<IValidator<CreateFornecedorRequest>, CreateFornecedorValidator>();
builder.Services.AddScoped<IValidator<CreateVendaRequest>, CreateVendaValidator>();

var app = builder.Build();

// Garante que wwwroot existe e configura static files com PhysicalFileProvider explícito
var webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRoot);

app.UseMiddleware<ExceptionMiddleware>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot),
    RequestPath = "",
});
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapEstoque();
app.MapClientes();
app.MapFornecedores();
app.MapVendas();
app.MapFinanceiro();
app.MapCategoriasLancamento();
app.MapDashboard();
app.MapOrcamentos();
app.MapOrcamentosPublicos();
app.MapProfissionais();
app.MapAgendamentos();
app.MapFiscal();
app.MapContratos();
app.MapContratoTemplates();
app.MapCobrancas();
app.MapAutomacao();
app.MapWebhooks();
app.MapPublicBooking();
app.MapAdmin();
app.MapPlanos();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .AllowAnonymous();

app.Run();

public partial class Program { }
