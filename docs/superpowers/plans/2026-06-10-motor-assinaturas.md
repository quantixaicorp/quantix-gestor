# Motor de Assinaturas — Sprint 8 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que qualquer empresa do GestorAI crie planos de assinatura para seus clientes, com checkout público white label, pagamento via PIX/boleto (Asaas), e controle via Contratos+Cobranças existentes.

**Architecture:** Remoção do código SaaS/White Label anterior → 4 novas entidades (PlanoAssinatura, PlanoAssinaturaItem, AssinaturaCliente, NichoTemplate) → services + endpoints autenticados e públicos → 5 telas frontend. O `AssinaturaService` define `tenantContext.EmpresaId` a partir do slug antes de delegar a `CobrancaService.EnviarAsaasAsync`.

**Tech Stack:** .NET 10 Minimal APIs, EF Core 10, PostgreSQL, React + TypeScript + Tailwind CSS, xUnit, InMemoryDatabase para testes

---

## File Map

**Backend — remover:**
- `backend/src/GestorAI.API/Domain/Entities/PlanoSaaS.cs`
- `backend/src/GestorAI.API/Domain/Entities/EmpresaPlano.cs`
- `backend/src/GestorAI.API/Services/PlanoService.cs`
- `backend/src/GestorAI.API/Services/FeatureService.cs`
- `backend/src/GestorAI.API/Services/BillingService.cs`
- `backend/src/GestorAI.API/Services/TenantResolutionService.cs`
- `backend/src/GestorAI.API/Endpoints/PlanosEndpoints.cs`
- `backend/src/GestorAI.API/Endpoints/BillingEndpoints.cs`

**Backend — criar:**
- `backend/src/GestorAI.API/Domain/Enums/TipoItemPlano.cs`
- `backend/src/GestorAI.API/Domain/Enums/AssinaturaStatus.cs`
- `backend/src/GestorAI.API/Domain/Entities/PlanoAssinatura.cs`
- `backend/src/GestorAI.API/Domain/Entities/PlanoAssinaturaItem.cs`
- `backend/src/GestorAI.API/Domain/Entities/AssinaturaCliente.cs`
- `backend/src/GestorAI.API/Domain/Entities/NichoTemplate.cs`
- `backend/src/GestorAI.API/DTOs/Assinaturas/PlanoAssinaturaDto.cs`
- `backend/src/GestorAI.API/DTOs/Assinaturas/AssinaturaDto.cs`
- `backend/src/GestorAI.API/Services/Assinaturas/PlanoAssinaturaService.cs`
- `backend/src/GestorAI.API/Services/Assinaturas/AssinaturaService.cs`
- `backend/src/GestorAI.API/Endpoints/PlanosAssinaturaEndpoints.cs`
- `backend/src/GestorAI.API/Endpoints/AssinaturasEndpoints.cs`
- `backend/src/GestorAI.API/Endpoints/AssinaturasPublicasEndpoints.cs`
- `backend/tests/GestorAI.Tests/Services/PlanoAssinaturaServiceTests.cs`
- `backend/tests/GestorAI.Tests/Services/AssinaturaServiceTests.cs`

**Backend — modificar:**
- `backend/src/GestorAI.API/Domain/Entities/Contrato.cs` — adicionar `AssinaturaClienteId?`
- `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs` — remover campos SaaS
- `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs` — atualizar DbSets
- `backend/src/GestorAI.API/Program.cs` — remover/adicionar services e endpoints
- `backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs` — remover feature gate
- `backend/src/GestorAI.API/Endpoints/CobrancasEndpoints.cs` — remover feature gate
- `backend/src/GestorAI.API/appsettings.json` — remover seção SaaS

**Frontend — remover:**
- `frontend/src/pages/configuracoes/PlanoAssinatura.tsx`
- `frontend/src/pages/configuracoes/WhiteLabel.tsx`

**Frontend — criar:**
- `frontend/src/types/assinaturas.ts`
- `frontend/src/pages/planos/PlanosList.tsx`
- `frontend/src/pages/planos/PlanoWizard.tsx`
- `frontend/src/pages/planos/PlanoDetalhe.tsx`
- `frontend/src/pages/publico/AssinarSlug.tsx`
- `frontend/src/pages/publico/AssinarPlano.tsx`

**Frontend — modificar:**
- `frontend/src/router/index.tsx`
- `frontend/src/components/layout/Sidebar.tsx`
- `frontend/src/components/layout/AppLayout.tsx` — remover branding effect

---

## Task 1: Limpeza — Remover Código SaaS/White Label

**Files:**
- Delete: todos os arquivos listados em "Backend — remover" e "Frontend — remover"
- Modify: `ContratosEndpoints.cs`, `CobrancasEndpoints.cs`, `ConfiguracaoEmpresa.cs`, `Program.cs`, `appsettings.json`, `router/index.tsx`, `Sidebar.tsx`, `AppLayout.tsx`

- [ ] **Step 1: Deletar arquivos de entities SaaS**

```bash
rm backend/src/GestorAI.API/Domain/Entities/PlanoSaaS.cs
rm backend/src/GestorAI.API/Domain/Entities/EmpresaPlano.cs
```

- [ ] **Step 2: Deletar services SaaS**

```bash
rm backend/src/GestorAI.API/Services/PlanoService.cs
rm backend/src/GestorAI.API/Services/FeatureService.cs
rm backend/src/GestorAI.API/Services/BillingService.cs
rm backend/src/GestorAI.API/Services/TenantResolutionService.cs
```

- [ ] **Step 3: Deletar endpoints SaaS**

```bash
rm backend/src/GestorAI.API/Endpoints/PlanosEndpoints.cs
rm backend/src/GestorAI.API/Endpoints/BillingEndpoints.cs
```

- [ ] **Step 4: Remover feature gate de ContratosEndpoints.cs**

Em `backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs`, substituir o bloco enviar-assinatura:

```csharp
        group.MapPost("/{id:guid}/enviar-assinatura", async (
            Guid id, EnviarAssinaturaRequest req,
            ContratoService svc, ClickSignService clickSign, CancellationToken ct) =>
            Results.Ok(await svc.EnviarAssinaturaAsync(id, req, clickSign, ct)));
```

Remover o `using GestorAI.API.Services;` se agora ficar sem uso.

- [ ] **Step 5: Remover feature gate de CobrancasEndpoints.cs**

Em `backend/src/GestorAI.API/Endpoints/CobrancasEndpoints.cs`, substituir o bloco enviar-asaas:

```csharp
        group.MapPost("/{id:guid}/enviar-asaas", async (
            Guid id, EnviarAsaasRequest req,
            CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.EnviarAsaasAsync(id, req, ct)));
```

- [ ] **Step 6: Remover campos SaaS de ConfiguracaoEmpresa.cs**

Remover as linhas:
```
    public string? DominioCustomizado { get; set; }
    // Billing SaaS — Asaas Marketplace
    public string? AsaasClienteIdSaaS { get; set; }
    public string? AssinaturaAsaasId { get; set; }
    public string? StatusAssinatura { get; set; }
    public DateTime? ProximaCobrancaEm { get; set; }
```

- [ ] **Step 7: Limpar Program.cs**

Remover as linhas de registro de services:
```csharp
builder.Services.AddScoped<FeatureService>();
builder.Services.AddScoped<PlanoService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<TenantResolutionService>();
```

Remover o bloco do middleware de domínio (linhas que contêm `// Resolve tenant by custom domain` até o `});` correspondente, ~8 linhas).

Remover os mapeamentos de endpoints:
```csharp
app.MapPlanos();
app.MapBilling();
```

- [ ] **Step 8: Remover seção SaaS de appsettings.json**

Remover o bloco:
```json
"SaaS": {
  "AsaasMarketplaceApiKey": "",
  "AsaasMarketplaceSandbox": "true"
},
```

- [ ] **Step 9: Remover DbSets de PlanoSaaS/EmpresaPlano de AppDbContext.cs**

Remover as linhas:
```csharp
    public DbSet<PlanoSaaS> PlanosSaaS => Set<PlanoSaaS>();
    public DbSet<EmpresaPlano> EmpresasPlano => Set<EmpresaPlano>();
```

- [ ] **Step 10: Deletar páginas frontend**

```bash
rm frontend/src/pages/configuracoes/PlanoAssinatura.tsx
rm frontend/src/pages/configuracoes/WhiteLabel.tsx
```

- [ ] **Step 11: Limpar router/index.tsx**

Remover imports:
```tsx
import PlanoAssinatura from '@/pages/configuracoes/PlanoAssinatura'
import WhiteLabel from '@/pages/configuracoes/WhiteLabel'
```

Remover rotas:
```tsx
      { path: '/configuracoes/plano', element: <PlanoAssinatura /> },
      { path: '/configuracoes/white-label', element: <WhiteLabel /> },
```

- [ ] **Step 12: Limpar Sidebar.tsx**

Remover imports não usados `CreditCard` e `Palette` do bloco lucide-react.

Remover itens do grupo Configurações:
```tsx
      { icon: CreditCard, label: 'Plano', path: '/configuracoes/plano' },
      { icon: Palette, label: 'White Label', path: '/configuracoes/white-label' },
```

- [ ] **Step 13: Limpar AppLayout.tsx**

Remover o `useEffect` que busca `/api/configuracao-empresa` e aplica CSS variables de cor primária (se existir).

- [ ] **Step 14: Gerar migration RemoveFeaturesSaaS**

```bash
cd backend && /usr/local/share/dotnet/dotnet ef migrations add RemoveFeaturesSaaS \
  --project src/GestorAI.API --startup-project src/GestorAI.API \
  --output-dir Infrastructure/Data/Migrations 2>&1
```

Esperado: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 15: Build e TypeScript check**

```bash
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj --no-restore -v quiet 2>&1 | tail -5
```
Esperado: `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`

```bash
cd ../frontend && PATH="/opt/homebrew/opt/node/bin:$PATH" ./node_modules/.bin/tsc --noEmit 2>&1
```
Esperado: sem output (zero erros)

- [ ] **Step 16: Commit**

```bash
cd ..
git add -A
git commit -m "chore: remove SaaS/WhiteLabel code, prepare for subscription engine"
```

---

## Task 2: Enums, Entidades e AssinaturaClienteId

**Files:**
- Create: `Domain/Enums/TipoItemPlano.cs`, `Domain/Enums/AssinaturaStatus.cs`
- Create: `Domain/Entities/PlanoAssinatura.cs`, `PlanoAssinaturaItem.cs`, `AssinaturaCliente.cs`, `NichoTemplate.cs`
- Modify: `Domain/Entities/Contrato.cs`

- [ ] **Step 1: Criar TipoItemPlano.cs**

```csharp
// backend/src/GestorAI.API/Domain/Enums/TipoItemPlano.cs
public enum TipoItemPlano { Servico, Desconto, Beneficio }
```

- [ ] **Step 2: Criar AssinaturaStatus.cs**

```csharp
// backend/src/GestorAI.API/Domain/Enums/AssinaturaStatus.cs
public enum AssinaturaStatus { Ativa, Cancelada, Inadimplente, Expirada }
```

- [ ] **Step 3: Criar PlanoAssinatura.cs**

```csharp
// backend/src/GestorAI.API/Domain/Entities/PlanoAssinatura.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class PlanoAssinatura : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public string? Descricao { get; set; }
    public string Nicho { get; set; } = "Personalizado";
    public decimal Preco { get; set; }
    public Periodicidade Periodicidade { get; set; }
    public bool Ativo { get; set; } = true;
    public bool MaisVendido { get; set; } = false;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<PlanoAssinaturaItem> Itens { get; set; } = [];
    public ICollection<AssinaturaCliente> Assinantes { get; set; } = [];
}
```

- [ ] **Step 4: Criar PlanoAssinaturaItem.cs**

```csharp
// backend/src/GestorAI.API/Domain/Entities/PlanoAssinaturaItem.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class PlanoAssinaturaItem
{
    public Guid Id { get; set; }
    public Guid PlanoAssinaturaId { get; set; }
    public required string Descricao { get; set; }
    public Guid? ServicoId { get; set; }
    public int QuantidadePorCiclo { get; set; } = 1;
    public TipoItemPlano Tipo { get; set; }
    public decimal? PercentualDesconto { get; set; }
    public PlanoAssinatura? Plano { get; set; }
}
```

- [ ] **Step 5: Criar AssinaturaCliente.cs**

```csharp
// backend/src/GestorAI.API/Domain/Entities/AssinaturaCliente.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class AssinaturaCliente : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid PlanoAssinaturaId { get; set; }
    public Guid ContratoId { get; set; }
    public AssinaturaStatus Status { get; set; } = AssinaturaStatus.Ativa;
    public DateOnly DataInicio { get; set; }
    public DateOnly DataRenovacao { get; set; }
    public int CicloAtual { get; set; } = 1;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public PlanoAssinatura? Plano { get; set; }
    public Contrato? Contrato { get; set; }
}
```

- [ ] **Step 6: Criar NichoTemplate.cs**

```csharp
// backend/src/GestorAI.API/Domain/Entities/NichoTemplate.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class NichoTemplate
{
    public Guid Id { get; set; }
    public required string Nicho { get; set; }
    public required string NomePlano { get; set; }
    public string? Descricao { get; set; }
    public decimal PrecoSugerido { get; set; }
    public bool MaisVendido { get; set; }
    public Periodicidade Periodicidade { get; set; }
    public ICollection<NichoTemplateItem> Itens { get; set; } = [];
}

public class NichoTemplateItem
{
    public Guid Id { get; set; }
    public Guid NichoTemplateId { get; set; }
    public required string Descricao { get; set; }
    public int QuantidadePorCiclo { get; set; }
    public TipoItemPlano Tipo { get; set; }
    public decimal? PercentualDesconto { get; set; }
    public NichoTemplate? Template { get; set; }
}
```

- [ ] **Step 7: Adicionar AssinaturaClienteId em Contrato.cs**

No arquivo `backend/src/GestorAI.API/Domain/Entities/Contrato.cs`, adicionar após `public string? Observacao { get; set; }`:

```csharp
    public Guid? AssinaturaClienteId { get; set; }
```

- [ ] **Step 8: Build**

```bash
cd backend && /usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj --no-restore -v quiet 2>&1 | tail -5
```
Esperado: `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`

- [ ] **Step 9: Commit**

```bash
git add backend/src/GestorAI.API/Domain/
git commit -m "feat: add PlanoAssinatura, AssinaturaCliente, NichoTemplate entities and enums"
```

---

## Task 3: AppDbContext + Migration AddMotorAssinaturas

**Files:**
- Modify: `Infrastructure/Data/AppDbContext.cs`
- New migration files (geradas automaticamente)

- [ ] **Step 1: Adicionar DbSets em AppDbContext.cs**

Adicionar após a linha `public DbSet<AutomacaoLog> AutomacaoLogs => Set<AutomacaoLog>();`:

```csharp
    public DbSet<PlanoAssinatura> PlanosAssinatura => Set<PlanoAssinatura>();
    public DbSet<PlanoAssinaturaItem> PlanosAssinaturaItens => Set<PlanoAssinaturaItem>();
    public DbSet<AssinaturaCliente> AssinaturasCliente => Set<AssinaturaCliente>();
    public DbSet<NichoTemplate> NichoTemplates => Set<NichoTemplate>();
    public DbSet<NichoTemplateItem> NichoTemplateItens => Set<NichoTemplateItem>();
```

- [ ] **Step 2: Adicionar query filters e relacionamentos em OnModelCreating**

Adicionar ao final do método `OnModelCreating`, antes do `}` de fechamento:

```csharp
        modelBuilder.Entity<PlanoAssinatura>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<AssinaturaCliente>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);

        modelBuilder.Entity<PlanoAssinaturaItem>().ToTable("PlanoAssinaturaItens");
        modelBuilder.Entity<NichoTemplateItem>().ToTable("NichoTemplateItens");

        modelBuilder.Entity<PlanoAssinaturaItem>()
            .HasOne(i => i.Plano)
            .WithMany(p => p.Itens)
            .HasForeignKey(i => i.PlanoAssinaturaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssinaturaCliente>()
            .HasOne(a => a.Cliente)
            .WithMany()
            .HasForeignKey(a => a.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssinaturaCliente>()
            .HasOne(a => a.Plano)
            .WithMany(p => p.Assinantes)
            .HasForeignKey(a => a.PlanoAssinaturaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssinaturaCliente>()
            .HasOne(a => a.Contrato)
            .WithMany()
            .HasForeignKey(a => a.ContratoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NichoTemplateItem>()
            .HasOne(i => i.Template)
            .WithMany(t => t.Itens)
            .HasForeignKey(i => i.NichoTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
```

- [ ] **Step 3: Gerar migration**

```bash
/usr/local/share/dotnet/dotnet ef migrations add AddMotorAssinaturas \
  --project src/GestorAI.API --startup-project src/GestorAI.API \
  --output-dir Infrastructure/Data/Migrations 2>&1
```
Esperado: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 4: Adicionar seed de NichoTemplates na migration**

Abrir o arquivo de migration gerado `Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_AddMotorAssinaturas.cs` e adicionar ao final do método `Up()`, antes do `}`:

```csharp
            // Seed NichoTemplates
            var barbearia1 = Guid.Parse("a1000001-0000-0000-0000-000000000001");
            var barbearia2 = Guid.Parse("a1000001-0000-0000-0000-000000000002");
            var barbearia3 = Guid.Parse("a1000001-0000-0000-0000-000000000003");
            var salao1 = Guid.Parse("a2000001-0000-0000-0000-000000000001");
            var salao2 = Guid.Parse("a2000001-0000-0000-0000-000000000002");
            var salao3 = Guid.Parse("a2000001-0000-0000-0000-000000000003");
            var estetica1 = Guid.Parse("a3000001-0000-0000-0000-000000000001");
            var estetica2 = Guid.Parse("a3000001-0000-0000-0000-000000000002");
            var estetica3 = Guid.Parse("a3000001-0000-0000-0000-000000000003");
            var pet1 = Guid.Parse("a4000001-0000-0000-0000-000000000001");
            var pet2 = Guid.Parse("a4000001-0000-0000-0000-000000000002");
            var pet3 = Guid.Parse("a4000001-0000-0000-0000-000000000003");
            var pt1 = Guid.Parse("a5000001-0000-0000-0000-000000000001");
            var pt2 = Guid.Parse("a5000001-0000-0000-0000-000000000002");
            var pt3 = Guid.Parse("a5000001-0000-0000-0000-000000000003");

            migrationBuilder.InsertData("NichoTemplates", ["Id","Nicho","NomePlano","Descricao","PrecoSugerido","MaisVendido","Periodicidade"],
            [
                [barbearia1,"Barbearia","Básico","2 cortes por mês",79m,false,0],
                [barbearia2,"Barbearia","Premium","4 cortes + 1 barba/mês + 10% desc. produtos",129m,true,0],
                [barbearia3,"Barbearia","VIP","Cortes ilimitados + 2 barbas + 1 hidratação + horário exclusivo",199m,false,0],
                [salao1,"Salão","Mãos & Pés","2 manicures + 2 pedicures/mês",99m,false,0],
                [salao2,"Salão","Beleza Completa","4 manicures + 4 pedicures + 1 escova/mês",189m,true,0],
                [salao3,"Salão","Noiva & Premium","Ilimitado mãos e pés + 2 escovas + 1 hidratação/mês",299m,false,0],
                [estetica1,"Estética","Pele","2 limpezas de pele/mês",149m,false,0],
                [estetica2,"Estética","Corpo & Rosto","4 procedimentos variados/mês",249m,true,0],
                [estetica3,"Estética","Premium Spa","8 procedimentos + drenagem + massagem/mês",399m,false,0],
                [pet1,"Pet Shop","Porte Pequeno","2 banhos + 1 tosa/mês",89m,false,0],
                [pet2,"Pet Shop","Porte Médio","2 banhos + 1 tosa/mês",139m,true,0],
                [pet3,"Pet Shop","Porte Grande","2 banhos + 1 tosa/mês",199m,false,0],
                [pt1,"Personal Trainer","2x/semana","8 sessões presenciais/mês",299m,false,0],
                [pt2,"Personal Trainer","3x/semana","12 sessões presenciais/mês",419m,true,0],
                [pt3,"Personal Trainer","Diário + Online","20 sessões + acompanhamento online ilimitado/mês",599m,false,0],
            ]);

            migrationBuilder.InsertData("NichoTemplateItens", ["Id","NichoTemplateId","Descricao","QuantidadePorCiclo","Tipo","PercentualDesconto"],
            [
                [Guid.NewGuid(),barbearia1,"Corte de cabelo",2,0,null],
                [Guid.NewGuid(),barbearia2,"Corte de cabelo",4,0,null],
                [Guid.NewGuid(),barbearia2,"Barba",1,0,null],
                [Guid.NewGuid(),barbearia2,"Desconto em produtos",0,1,10m],
                [Guid.NewGuid(),barbearia3,"Corte de cabelo",0,0,null],
                [Guid.NewGuid(),barbearia3,"Barba",2,0,null],
                [Guid.NewGuid(),barbearia3,"Hidratação capilar",1,0,null],
                [Guid.NewGuid(),barbearia3,"Horário exclusivo",0,2,null],
                [Guid.NewGuid(),salao1,"Manicure",2,0,null],
                [Guid.NewGuid(),salao1,"Pedicure",2,0,null],
                [Guid.NewGuid(),salao2,"Manicure",4,0,null],
                [Guid.NewGuid(),salao2,"Pedicure",4,0,null],
                [Guid.NewGuid(),salao2,"Escova",1,0,null],
                [Guid.NewGuid(),salao3,"Manicure",0,0,null],
                [Guid.NewGuid(),salao3,"Pedicure",0,0,null],
                [Guid.NewGuid(),salao3,"Escova",2,0,null],
                [Guid.NewGuid(),salao3,"Hidratação capilar",1,0,null],
                [Guid.NewGuid(),estetica1,"Limpeza de pele",2,0,null],
                [Guid.NewGuid(),estetica2,"Procedimentos estéticos",4,0,null],
                [Guid.NewGuid(),estetica3,"Procedimentos premium",8,0,null],
                [Guid.NewGuid(),estetica3,"Drenagem linfática",1,0,null],
                [Guid.NewGuid(),estetica3,"Massagem relaxante",1,0,null],
                [Guid.NewGuid(),pet1,"Banho",2,0,null],
                [Guid.NewGuid(),pet1,"Tosa",1,0,null],
                [Guid.NewGuid(),pet2,"Banho",2,0,null],
                [Guid.NewGuid(),pet2,"Tosa",1,0,null],
                [Guid.NewGuid(),pet3,"Banho",2,0,null],
                [Guid.NewGuid(),pet3,"Tosa",1,0,null],
                [Guid.NewGuid(),pt1,"Sessão presencial",8,0,null],
                [Guid.NewGuid(),pt2,"Sessão presencial",12,0,null],
                [Guid.NewGuid(),pt3,"Sessão presencial",20,0,null],
                [Guid.NewGuid(),pt3,"Acompanhamento online",0,2,null],
            ]);
```

- [ ] **Step 5: Build**

```bash
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj --no-restore -v quiet 2>&1 | tail -5
```
Esperado: `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Infrastructure/ backend/src/GestorAI.API/Domain/
git commit -m "feat: AppDbContext with subscription entities, AddMotorAssinaturas migration with nicho seed"
```

---

## Task 4: PlanoAssinaturaService + DTOs + Testes

**Files:**
- Create: `DTOs/Assinaturas/PlanoAssinaturaDto.cs`
- Create: `Services/Assinaturas/PlanoAssinaturaService.cs`
- Create: `tests/GestorAI.Tests/Services/PlanoAssinaturaServiceTests.cs`

- [ ] **Step 1: Criar DTOs**

```csharp
// backend/src/GestorAI.API/DTOs/Assinaturas/PlanoAssinaturaDto.cs
namespace GestorAI.API.DTOs.Assinaturas;

public record PlanoItemRequest(
    string Descricao,
    Guid? ServicoId,
    int QuantidadePorCiclo,
    string Tipo,
    decimal? PercentualDesconto);

public record CreatePlanoAssinaturaRequest(
    string Nome,
    string? Descricao,
    string Nicho,
    decimal Preco,
    string Periodicidade,
    bool MaisVendido,
    List<PlanoItemRequest> Itens);

public record UpdatePlanoAssinaturaRequest(
    string Nome,
    string? Descricao,
    string Nicho,
    decimal Preco,
    string Periodicidade,
    bool MaisVendido,
    bool Ativo,
    List<PlanoItemRequest> Itens);

public record PlanoItemResponse(
    Guid Id,
    string Descricao,
    Guid? ServicoId,
    int QuantidadePorCiclo,
    string Tipo,
    decimal? PercentualDesconto);

public record PlanoAssinaturaResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    string Nicho,
    decimal Preco,
    string Periodicidade,
    bool Ativo,
    bool MaisVendido,
    int TotalAssinantes,
    List<PlanoItemResponse> Itens,
    DateTime CriadoEm);

public record PlanoAssinaturaListItem(
    Guid Id,
    string Nome,
    string Nicho,
    decimal Preco,
    string Periodicidade,
    bool Ativo,
    bool MaisVendido,
    int TotalAssinantes);

public record NichoTemplateItemResponse(
    Guid Id,
    string Descricao,
    int QuantidadePorCiclo,
    string Tipo,
    decimal? PercentualDesconto);

public record NichoTemplateResponse(
    Guid Id,
    string Nicho,
    string NomePlano,
    string? Descricao,
    decimal PrecoSugerido,
    bool MaisVendido,
    string Periodicidade,
    List<NichoTemplateItemResponse> Itens);
```

- [ ] **Step 2: Escrever testes falhando**

```csharp
// backend/tests/GestorAI.Tests/Services/PlanoAssinaturaServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Assinaturas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class PlanoAssinaturaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, PlanoAssinaturaService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        return (db, new PlanoAssinaturaService(db, tenant));
    }

    [Fact]
    public async Task CreateAsync_PersistsPlanoWithItens()
    {
        var (db, svc) = Setup();
        var req = new CreatePlanoAssinaturaRequest(
            "Básico", "Plano simples", "Barbearia", 79m, "Mensal", false,
            [new PlanoItemRequest("Corte", null, 2, "Servico", null)]);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal("Básico", result.Nome);
        Assert.Equal(79m, result.Preco);
        Assert.Single(result.Itens);
        Assert.Equal("Corte", result.Itens[0].Descricao);
    }

    [Fact]
    public async Task CreateAsync_PeriodicidadeInvalida_LancaExcecao()
    {
        var (_, svc) = Setup();
        var req = new CreatePlanoAssinaturaRequest("X", null, "X", 99m, "Invalido", false, []);

        await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(req, default));
    }

    [Fact]
    public async Task UpdateAsync_AlteraAtivo()
    {
        var (db, svc) = Setup();
        var plano = new PlanoAssinatura
        {
            EmpresaId = _empresaId, Nome = "Original", Preco = 50m,
            Periodicidade = Periodicidade.Mensal
        };
        db.PlanosAssinatura.Add(plano);
        await db.SaveChangesAsync();

        var req = new UpdatePlanoAssinaturaRequest("Atualizado", null, "X", 60m, "Mensal", false, false, []);
        var result = await svc.UpdateAsync(plano.Id, req, default);

        Assert.Equal("Atualizado", result.Nome);
        Assert.False(result.Ativo);
    }

    [Fact]
    public async Task DeleteAsync_PlanoComAssinantes_LancaExcecao()
    {
        var (db, svc) = Setup();
        var plano = new PlanoAssinatura { EmpresaId = _empresaId, Nome = "P", Preco = 99m, Periodicidade = Periodicidade.Mensal };
        db.PlanosAssinatura.Add(plano);
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "C", Whatsapp = "11999990000" };
        db.Clientes.Add(cliente);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id, Titulo = "T", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 99m, Numero = 1,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 1, Status = ContratoStatus.Ativo
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();
        db.AssinaturasCliente.Add(new AssinaturaCliente
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            PlanoAssinaturaId = plano.Id, ContratoId = contrato.Id,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            DataRenovacao = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.DeleteAsync(plano.Id, default));
    }
}
```

- [ ] **Step 3: Rodar testes — verificar que falham**

```bash
cd backend && /usr/local/share/dotnet/dotnet test tests/GestorAI.Tests \
  --filter "PlanoAssinaturaServiceTests" --no-build 2>&1 | tail -10
```
Esperado: erro de compilação (service não existe ainda)

- [ ] **Step 4: Criar PlanoAssinaturaService**

```csharp
// backend/src/GestorAI.API/Services/Assinaturas/PlanoAssinaturaService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Assinaturas;

public class PlanoAssinaturaService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<PlanoAssinaturaListItem>> ListAsync(CancellationToken ct) =>
        await db.PlanosAssinatura
            .Select(p => new PlanoAssinaturaListItem(
                p.Id, p.Nome, p.Nicho, p.Preco, p.Periodicidade.ToString(),
                p.Ativo, p.MaisVendido,
                p.Assinantes.Count(a => a.Status == AssinaturaStatus.Ativa)))
            .ToListAsync(ct);

    public async Task<PlanoAssinaturaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.PlanosAssinatura
            .Include(x => x.Itens)
            .Include(x => x.Assinantes)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Plano não encontrado.", 404);
        return ToResponse(p);
    }

    public async Task<PlanoAssinaturaResponse> CreateAsync(CreatePlanoAssinaturaRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<Periodicidade>(req.Periodicidade, out var per))
            throw new AppException($"Periodicidade inválida: {req.Periodicidade}.", 400);

        var plano = new PlanoAssinatura
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            Descricao = req.Descricao,
            Nicho = req.Nicho,
            Preco = req.Preco,
            Periodicidade = per,
            MaisVendido = req.MaisVendido,
        };

        foreach (var item in req.Itens)
            plano.Itens.Add(MapItem(item));

        db.PlanosAssinatura.Add(plano);
        await db.SaveChangesAsync(ct);
        return await GetAsync(plano.Id, ct);
    }

    public async Task<PlanoAssinaturaResponse> UpdateAsync(Guid id, UpdatePlanoAssinaturaRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<Periodicidade>(req.Periodicidade, out var per))
            throw new AppException($"Periodicidade inválida: {req.Periodicidade}.", 400);

        var plano = await db.PlanosAssinatura.Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Plano não encontrado.", 404);

        plano.Nome = req.Nome;
        plano.Descricao = req.Descricao;
        plano.Nicho = req.Nicho;
        plano.Preco = req.Preco;
        plano.Periodicidade = per;
        plano.MaisVendido = req.MaisVendido;
        plano.Ativo = req.Ativo;

        plano.Itens.Clear();
        foreach (var item in req.Itens)
            plano.Itens.Add(MapItem(item));

        await db.SaveChangesAsync(ct);
        return await GetAsync(plano.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var plano = await db.PlanosAssinatura.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Plano não encontrado.", 404);

        var temAssinantes = await db.AssinaturasCliente
            .AnyAsync(a => a.PlanoAssinaturaId == id && a.Status == AssinaturaStatus.Ativa, ct);
        if (temAssinantes)
            throw new AppException("Plano possui assinantes ativos e não pode ser removido.", 400);

        db.PlanosAssinatura.Remove(plano);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<NichoTemplateResponse>> ListTemplatesAsync(string? nicho, CancellationToken ct)
    {
        var query = db.NichoTemplates.Include(t => t.Itens).AsQueryable();
        if (!string.IsNullOrWhiteSpace(nicho))
            query = query.Where(t => t.Nicho == nicho);
        var templates = await query.ToListAsync(ct);
        return templates.Select(t => new NichoTemplateResponse(
            t.Id, t.Nicho, t.NomePlano, t.Descricao, t.PrecoSugerido, t.MaisVendido,
            t.Periodicidade.ToString(),
            t.Itens.Select(i => new NichoTemplateItemResponse(
                i.Id, i.Descricao, i.QuantidadePorCiclo, i.Tipo.ToString(), i.PercentualDesconto
            )).ToList()
        )).ToList();
    }

    private static PlanoAssinaturaItem MapItem(PlanoItemRequest item)
    {
        if (!Enum.TryParse<TipoItemPlano>(item.Tipo, out var tipo))
            throw new AppException($"TipoItemPlano inválido: {item.Tipo}.", 400);
        return new PlanoAssinaturaItem
        {
            Descricao = item.Descricao,
            ServicoId = item.ServicoId,
            QuantidadePorCiclo = item.QuantidadePorCiclo,
            Tipo = tipo,
            PercentualDesconto = item.PercentualDesconto,
        };
    }

    private static PlanoAssinaturaResponse ToResponse(PlanoAssinatura p) =>
        new(p.Id, p.Nome, p.Descricao, p.Nicho, p.Preco, p.Periodicidade.ToString(),
            p.Ativo, p.MaisVendido,
            p.Assinantes.Count(a => a.Status == AssinaturaStatus.Ativa),
            p.Itens.Select(i => new PlanoItemResponse(
                i.Id, i.Descricao, i.ServicoId, i.QuantidadePorCiclo,
                i.Tipo.ToString(), i.PercentualDesconto)).ToList(),
            p.CriadoEm);
}
```

- [ ] **Step 5: Rodar testes — verificar que passam**

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests \
  --filter "PlanoAssinaturaServiceTests" 2>&1 | tail -10
```
Esperado: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Assinaturas/ backend/src/GestorAI.API/Services/Assinaturas/PlanoAssinaturaService.cs backend/tests/
git commit -m "feat: PlanoAssinaturaService CRUD with tests"
```

---

## Task 5: PlanosAssinaturaEndpoints + Program.cs

**Files:**
- Create: `Endpoints/PlanosAssinaturaEndpoints.cs`
- Modify: `Program.cs`

- [ ] **Step 1: Criar PlanosAssinaturaEndpoints.cs**

```csharp
// backend/src/GestorAI.API/Endpoints/PlanosAssinaturaEndpoints.cs
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.Services.Assinaturas;

namespace GestorAI.API.Endpoints;

public static class PlanosAssinaturaEndpoints
{
    public static void MapPlanosAssinatura(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/planos-assinatura").RequireAuthorization();

        group.MapGet("/", async (PlanoAssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, PlanoAssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreatePlanoAssinaturaRequest req, PlanoAssinaturaService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/planos-assinatura/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdatePlanoAssinaturaRequest req, PlanoAssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, PlanoAssinaturaService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        app.MapGet("/api/nicho-templates", async (string? nicho, PlanoAssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListTemplatesAsync(nicho, ct))).AllowAnonymous();
    }
}
```

- [ ] **Step 2: Registrar em Program.cs**

Adicionar após `builder.Services.AddScoped<PublicBookingService>();`:

```csharp
builder.Services.AddScoped<PlanoAssinaturaService>();
builder.Services.AddScoped<AssinaturaService>();
```

Adicionar após `app.MapPublicBooking();`:

```csharp
app.MapPlanosAssinatura();
app.MapAssinaturas();
app.MapAssinaturasPublicas();
```

Adicionar using no topo do arquivo se necessário:
```csharp
using GestorAI.API.Services.Assinaturas;
```

- [ ] **Step 3: Build**

```bash
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj --no-restore -v quiet 2>&1 | tail -5
```
Esperado: `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`

(Vai falhar por `AssinaturaService` e endpoints não existirem ainda — criar stubs vazios se necessário, ou pular build até Task 7)

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/PlanosAssinaturaEndpoints.cs backend/src/GestorAI.API/Program.cs
git commit -m "feat: PlanosAssinaturaEndpoints and nicho-templates endpoint"
```

---

## Task 6: AssinaturaService + Testes

**Files:**
- Create: `DTOs/Assinaturas/AssinaturaDto.cs`
- Create: `Services/Assinaturas/AssinaturaService.cs`
- Create: `tests/GestorAI.Tests/Services/AssinaturaServiceTests.cs`

- [ ] **Step 1: Criar AssinaturaDto.cs**

```csharp
// backend/src/GestorAI.API/DTOs/Assinaturas/AssinaturaDto.cs
namespace GestorAI.API.DTOs.Assinaturas;

public record AssinarRequest(string Nome, string Whatsapp, string? Email);

public record AssinarResponse(
    Guid AssinaturaId,
    Guid ContratoId,
    Guid CobrancaId,
    string? PixQrCode,
    string? BoletoUrl,
    decimal Valor,
    DateOnly Vencimento);

public record AssinaturaListItem(
    Guid Id,
    string ClienteNome,
    string PlanNome,
    string Status,
    DateOnly DataRenovacao,
    int CicloAtual);

public record AssinaturaResponse(
    Guid Id,
    string ClienteNome,
    string ClienteWhatsapp,
    Guid PlanoId,
    string PlanoNome,
    decimal PlanPreco,
    string Status,
    DateOnly DataInicio,
    DateOnly DataRenovacao,
    int CicloAtual,
    Guid ContratoId);
```

- [ ] **Step 2: Escrever testes falhando**

```csharp
// backend/tests/GestorAI.Tests/Services/AssinaturaServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Assinaturas;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Services.Asaas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestorAI.Tests.Services;

public class AssinaturaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, TenantContext tenant, AssinaturaService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

        // Seed ConfiguracaoEmpresa com Asaas configurado
        db.ConfiguracoesEmpresa.Add(new ConfiguracaoEmpresa
        {
            EmpresaId = _empresaId,
            Slug = "minha-empresa",
            AsaasApiKey = "test_key",
            AsaasSandbox = true,
        });
        db.SaveChanges();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // AsaasService não pode ser instanciado sem IHttpClientFactory real,
        // mas nos testes que não chamam Asaas diretamente isso não importa
        var httpFactory = new FakeHttpClientFactory();
        var asaasService = new AsaasService(httpFactory);
        var cobrancaService = new CobrancaService(db, tenant, asaasService);
        var svc = new AssinaturaService(db, tenant, cobrancaService);
        return (db, tenant, svc);
    }

    private PlanoAssinatura CriarPlano(AppDbContext db)
    {
        var plano = new PlanoAssinatura
        {
            EmpresaId = _empresaId, Nome = "Premium", Preco = 129m,
            Periodicidade = Periodicidade.Mensal, Nicho = "Barbearia"
        };
        plano.Itens.Add(new PlanoAssinaturaItem { Descricao = "Corte", QuantidadePorCiclo = 4, Tipo = TipoItemPlano.Servico });
        db.PlanosAssinatura.Add(plano);
        db.SaveChanges();
        return plano;
    }

    [Fact]
    public async Task AssinarAsync_CriaClienteContratoCobranca()
    {
        var (db, tenant, svc) = Setup();
        var plano = CriarPlano(db);

        // Não chama EnviarAsaasAsync — só verifica criação dos registros
        var req = new AssinarRequest("João Silva", "11999990000", "joao@test.com");
        var assinatura = await svc.AssinarSemAsaasAsync(_empresaId, plano.Id, req, default);

        Assert.Equal(_empresaId, tenant.EmpresaId);

        var clienteExiste = await db.Clientes.IgnoreQueryFilters()
            .AnyAsync(c => c.Whatsapp == "11999990000");
        Assert.True(clienteExiste);

        var contratoExiste = await db.Contratos.IgnoreQueryFilters()
            .AnyAsync(c => c.AssinaturaClienteId == assinatura.AssinaturaId);
        Assert.True(contratoExiste);

        var cobrancaExiste = await db.Cobrancas.IgnoreQueryFilters()
            .AnyAsync(c => c.ContratoId == assinatura.ContratoId);
        Assert.True(cobrancaExiste);
    }

    [Fact]
    public async Task AssinarAsync_ClienteJaExiste_ReusaCliente()
    {
        var (db, _, svc) = Setup();
        var plano = CriarPlano(db);
        db.Clientes.Add(new Cliente { EmpresaId = _empresaId, Nome = "João", Whatsapp = "11999990000" });
        await db.SaveChangesAsync();

        var req = new AssinarRequest("João Silva", "11999990000", null);
        await svc.AssinarSemAsaasAsync(_empresaId, plano.Id, req, default);

        var count = await db.Clientes.IgnoreQueryFilters()
            .CountAsync(c => c.Whatsapp == "11999990000");
        Assert.Equal(1, count); // não criou duplicata
    }

    [Fact]
    public async Task CancelarAsync_MarcaCanceladaEEncerraContrato()
    {
        var (db, _, svc) = Setup();
        var plano = CriarPlano(db);
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Maria", Whatsapp = "11888880000" };
        db.Clientes.Add(cliente);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id, Titulo = "T", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 129m, Numero = 1,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 1, Status = ContratoStatus.Ativo
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var assinatura = new AssinaturaCliente
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            PlanoAssinaturaId = plano.Id, ContratoId = contrato.Id,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            DataRenovacao = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        };
        db.AssinaturasCliente.Add(assinatura);
        await db.SaveChangesAsync();

        await svc.CancelarAsync(assinatura.Id, default);

        var a = await db.AssinaturasCliente.IgnoreQueryFilters().FindAsync(assinatura.Id);
        Assert.Equal(AssinaturaStatus.Cancelada, a!.Status);

        var c = await db.Contratos.IgnoreQueryFilters().FindAsync(contrato.Id);
        Assert.Equal(ContratoStatus.Encerrado, c!.Status);
    }
}

// Helper mínimo para testes que não fazem chamadas HTTP
public class FakeHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}
```

- [ ] **Step 3: Rodar testes — verificar que falham**

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests \
  --filter "AssinaturaServiceTests" --no-build 2>&1 | tail -5
```
Esperado: erro de compilação

- [ ] **Step 4: Criar AssinaturaService**

```csharp
// backend/src/GestorAI.API/Services/Assinaturas/AssinaturaService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Assinaturas;

public class AssinaturaService(
    AppDbContext db,
    TenantContext tenantContext,
    CobrancaService cobrancaService)
{
    public async Task<List<AssinaturaListItem>> ListAsync(Guid? planoId, string? status, CancellationToken ct)
    {
        var query = db.AssinaturasCliente
            .Include(a => a.Cliente)
            .Include(a => a.Plano)
            .AsQueryable();

        if (planoId.HasValue)
            query = query.Where(a => a.PlanoAssinaturaId == planoId.Value);

        if (status != null && Enum.TryParse<AssinaturaStatus>(status, out var s))
            query = query.Where(a => a.Status == s);

        return await query
            .OrderByDescending(a => a.CriadoEm)
            .Select(a => new AssinaturaListItem(
                a.Id, a.Cliente!.Nome, a.Plano!.Nome,
                a.Status.ToString(), a.DataRenovacao, a.CicloAtual))
            .ToListAsync(ct);
    }

    public async Task<AssinaturaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var a = await db.AssinaturasCliente
            .Include(x => x.Cliente)
            .Include(x => x.Plano)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Assinatura não encontrada.", 404);

        return new AssinaturaResponse(
            a.Id, a.Cliente!.Nome, a.Cliente.Whatsapp,
            a.PlanoAssinaturaId, a.Plano!.Nome, a.Plano.Preco,
            a.Status.ToString(), a.DataInicio, a.DataRenovacao,
            a.CicloAtual, a.ContratoId);
    }

    // Usado no checkout público — slug resolve empresaId antes de chamar
    public async Task<AssinarResponse> AssinarAsync(
        Guid empresaId, Guid planoId, AssinarRequest req, CancellationToken ct)
    {
        tenantContext.EmpresaId = empresaId;

        var (assinaturaId, contratoId, cobrancaId) = await AssinarSemAsaasAsync(empresaId, planoId, req, ct);

        var asaasResult = await cobrancaService.EnviarAsaasAsync(
            cobrancaId, new EnviarAsaasRequest("PIX"), ct);

        return new AssinarResponse(
            assinaturaId, contratoId, cobrancaId,
            asaasResult.PixQrCode, asaasResult.BoletoUrl,
            (await db.Cobrancas.FindAsync([cobrancaId], ct))!.Valor,
            (await db.Cobrancas.FindAsync([cobrancaId], ct))!.DataVencimento);
    }

    // Separado para permitir testes sem chamar Asaas
    public async Task<(Guid AssinaturaId, Guid ContratoId, Guid CobrancaId)> AssinarSemAsaasAsync(
        Guid empresaId, Guid planoId, AssinarRequest req, CancellationToken ct)
    {
        tenantContext.EmpresaId = empresaId;

        var plano = await db.PlanosAssinatura.Include(p => p.Itens)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == planoId && p.EmpresaId == empresaId && p.Ativo, ct)
            ?? throw new AppException("Plano não encontrado.", 404);

        // Busca ou cria cliente pelo WhatsApp
        var cliente = await db.Clientes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Whatsapp == req.Whatsapp, ct);
        if (cliente is null)
        {
            cliente = new Cliente { EmpresaId = empresaId, Nome = req.Nome, Whatsapp = req.Whatsapp, Email = req.Email };
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(ct);
        }

        var numero = (await db.Contratos.IgnoreQueryFilters()
            .Where(c => c.EmpresaId == empresaId)
            .MaxAsync(c => (int?)c.Numero, ct) ?? 0) + 1;

        var objeto = string.Join(", ", plano.Itens.Select(i =>
            i.QuantidadePorCiclo == 0 ? $"{i.Descricao} (ilimitado)" : $"{i.QuantidadePorCiclo}x {i.Descricao}"));

        var contrato = new Contrato
        {
            EmpresaId = empresaId,
            Numero = numero,
            ClienteId = cliente.Id,
            Titulo = $"Assinatura — {plano.Nome}",
            Objeto = objeto,
            TipoCobranca = TipoCobranca.Recorrente,
            Valor = plano.Preco,
            DataInicio = DateOnly.FromDateTime(DateTime.UtcNow),
            Periodicidade = plano.Periodicidade,
            DiaVencimento = DateTime.UtcNow.Day,
            Status = ContratoStatus.Ativo,
        };
        contrato.Itens.Add(new ContratoItem
        {
            Descricao = plano.Nome,
            Quantidade = 1,
            ValorUnitario = plano.Preco,
        });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync(ct);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var assinatura = new AssinaturaCliente
        {
            EmpresaId = empresaId,
            ClienteId = cliente.Id,
            PlanoAssinaturaId = plano.Id,
            ContratoId = contrato.Id,
            DataInicio = hoje,
            DataRenovacao = hoje.AddMonths(1),
        };
        db.AssinaturasCliente.Add(assinatura);

        // Vincula o contrato à assinatura
        contrato.AssinaturaClienteId = assinatura.Id;

        var cobranca = new Cobranca
        {
            EmpresaId = empresaId,
            ClienteId = cliente.Id,
            ContratoId = contrato.Id,
            Referencia = $"Assinatura {plano.Nome} — {hoje:MMMM/yyyy}",
            Valor = plano.Preco,
            DataVencimento = hoje.AddDays(3),
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync(ct);

        return (assinatura.Id, contrato.Id, cobranca.Id);
    }

    public async Task CancelarAsync(Guid id, CancellationToken ct)
    {
        var assinatura = await db.AssinaturasCliente
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Assinatura não encontrada.", 404);

        assinatura.Status = AssinaturaStatus.Cancelada;

        var contrato = await db.Contratos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == assinatura.ContratoId, ct);
        if (contrato != null && contrato.Status == ContratoStatus.Ativo)
            contrato.Status = ContratoStatus.Encerrado;

        await db.Cobrancas.IgnoreQueryFilters()
            .Where(c => c.ContratoId == assinatura.ContratoId && c.Status == CobrancaStatus.Pendente)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, CobrancaStatus.Cancelado), ct);

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Rodar testes — verificar que passam**

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests \
  --filter "AssinaturaServiceTests" 2>&1 | tail -10
```
Esperado: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Assinaturas/AssinaturaDto.cs \
  backend/src/GestorAI.API/Services/Assinaturas/AssinaturaService.cs \
  backend/tests/
git commit -m "feat: AssinaturaService with AssinarAsync, CancelarAsync and tests"
```

---

## Task 7: AssinaturasEndpoints + Endpoints Públicos

**Files:**
- Create: `Endpoints/AssinaturasEndpoints.cs`
- Create: `Endpoints/AssinaturasPublicasEndpoints.cs`

- [ ] **Step 1: Criar AssinaturasEndpoints.cs**

```csharp
// backend/src/GestorAI.API/Endpoints/AssinaturasEndpoints.cs
using GestorAI.API.Services.Assinaturas;

namespace GestorAI.API.Endpoints;

public static class AssinaturasEndpoints
{
    public static void MapAssinaturas(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assinaturas").RequireAuthorization();

        group.MapGet("/", async (Guid? planoId, string? status, AssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(planoId, status, ct)));

        group.MapGet("/{id:guid}", async (Guid id, AssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, AssinaturaService svc, CancellationToken ct) =>
        {
            await svc.CancelarAsync(id, ct);
            return Results.Ok();
        });
    }
}
```

- [ ] **Step 2: Criar AssinaturasPublicasEndpoints.cs**

```csharp
// backend/src/GestorAI.API/Endpoints/AssinaturasPublicasEndpoints.cs
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.Services.Assinaturas;
using GestorAI.API.Services.PublicBooking;

namespace GestorAI.API.Endpoints;

public static class AssinaturasPublicasEndpoints
{
    public static void MapAssinaturasPublicas(this IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup("/public/{slug}").AllowAnonymous();

        publicGroup.MapGet("/planos", async (
            string slug, PlanoAssinaturaService svc,
            PublicBookingService publicSvc, CancellationToken ct) =>
        {
            var empresaId = await publicSvc.ResolveEmpresaAsync(slug, ct);
            var planos = await svc.ListPublicAsync(empresaId, ct);
            return Results.Ok(planos);
        });

        publicGroup.MapGet("/planos/{planoId:guid}", async (
            string slug, Guid planoId,
            PlanoAssinaturaService svc, PublicBookingService publicSvc, CancellationToken ct) =>
        {
            var empresaId = await publicSvc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetPublicAsync(empresaId, planoId, ct));
        });

        publicGroup.MapPost("/planos/{planoId:guid}/assinar", async (
            string slug, Guid planoId,
            AssinarRequest req,
            AssinaturaService svc, PublicBookingService publicSvc, CancellationToken ct) =>
        {
            var empresaId = await publicSvc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.AssinarAsync(empresaId, planoId, req, ct));
        });
    }
}
```

- [ ] **Step 3: Adicionar ListPublicAsync e GetPublicAsync em PlanoAssinaturaService**

Adicionar ao final de `PlanoAssinaturaService.cs`, antes do último `}`:

```csharp
    public async Task<List<PlanoAssinaturaListItem>> ListPublicAsync(Guid empresaId, CancellationToken ct) =>
        await db.PlanosAssinatura
            .IgnoreQueryFilters()
            .Where(p => p.EmpresaId == empresaId && p.Ativo)
            .Select(p => new PlanoAssinaturaListItem(
                p.Id, p.Nome, p.Nicho, p.Preco, p.Periodicidade.ToString(),
                p.Ativo, p.MaisVendido,
                p.Assinantes.Count(a => a.Status == AssinaturaStatus.Ativa)))
            .ToListAsync(ct);

    public async Task<PlanoAssinaturaResponse> GetPublicAsync(Guid empresaId, Guid planoId, CancellationToken ct)
    {
        var p = await db.PlanosAssinatura
            .IgnoreQueryFilters()
            .Include(x => x.Itens)
            .Include(x => x.Assinantes)
            .FirstOrDefaultAsync(x => x.Id == planoId && x.EmpresaId == empresaId && x.Ativo, ct)
            ?? throw new AppException("Plano não encontrado.", 404);
        return ToResponse(p);
    }
```

- [ ] **Step 4: Build**

```bash
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj --no-restore -v quiet 2>&1 | tail -5
```
Esperado: `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`

- [ ] **Step 5: Rodar todos os testes**

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests 2>&1 | tail -5
```
Esperado: `Passed! - Failed: 0`

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/ backend/src/GestorAI.API/Services/Assinaturas/
git commit -m "feat: AssinaturasEndpoints and public checkout endpoints"
```

---

## Task 8: Frontend — Tipos e Telas Internas

**Files:**
- Create: `frontend/src/types/assinaturas.ts`
- Create: `frontend/src/pages/planos/PlanosList.tsx`
- Create: `frontend/src/pages/planos/PlanoWizard.tsx`
- Create: `frontend/src/pages/planos/PlanoDetalhe.tsx`
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Criar types/assinaturas.ts**

```typescript
// frontend/src/types/assinaturas.ts
export interface PlanoItem {
  id: string
  descricao: string
  servicoId: string | null
  quantidadePorCiclo: number
  tipo: 'Servico' | 'Desconto' | 'Beneficio'
  percentualDesconto: number | null
}

export interface PlanoAssinaturaListItem {
  id: string
  nome: string
  nicho: string
  preco: number
  periodicidade: string
  ativo: boolean
  maisVendido: boolean
  totalAssinantes: number
}

export interface PlanoAssinaturaResponse extends PlanoAssinaturaListItem {
  descricao: string | null
  itens: PlanoItem[]
  criadoEm: string
}

export interface NichoTemplateItem {
  id: string
  descricao: string
  quantidadePorCiclo: number
  tipo: string
  percentualDesconto: number | null
}

export interface NichoTemplate {
  id: string
  nicho: string
  nomePlano: string
  descricao: string | null
  precoSugerido: number
  maisVendido: boolean
  periodicidade: string
  itens: NichoTemplateItem[]
}

export interface AssinaturaListItem {
  id: string
  clienteNome: string
  planNome: string
  status: string
  dataRenovacao: string
  cicloAtual: number
}
```

- [ ] **Step 2: Criar PlanosList.tsx**

```tsx
// frontend/src/pages/planos/PlanosList.tsx
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import type { PlanoAssinaturaListItem } from '@/types/assinaturas'

export default function PlanosList() {
  const [planos, setPlanos] = useState<PlanoAssinaturaListItem[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.get<PlanoAssinaturaListItem[]>('/api/planos-assinatura')
      .then(setPlanos)
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <p className="text-sm text-muted-foreground">Carregando...</p>

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Planos de Assinatura</h1>
        <Button asChild><Link to="/planos/novo">Novo Plano</Link></Button>
      </div>

      {planos.length === 0 && (
        <p className="text-muted-foreground text-sm">Nenhum plano criado ainda.</p>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {planos.map(p => (
          <Link key={p.id} to={`/planos/${p.id}`}
            className="rounded-lg border p-4 hover:shadow-md transition-shadow space-y-2">
            <div className="flex items-start justify-between">
              <div>
                <p className="font-semibold">{p.nome}</p>
                <span className="text-xs bg-muted px-2 py-0.5 rounded-full">{p.nicho}</span>
              </div>
              {p.maisVendido && (
                <span className="text-xs bg-amber-100 text-amber-800 px-2 py-0.5 rounded-full">Mais vendido</span>
              )}
            </div>
            <p className="text-2xl font-bold">
              R$ {p.preco.toFixed(2).replace('.', ',')}
              <span className="text-sm font-normal text-muted-foreground">/{p.periodicidade.toLowerCase()}</span>
            </p>
            <p className="text-sm text-muted-foreground">{p.totalAssinantes} assinante(s) ativo(s)</p>
            {!p.ativo && <span className="text-xs text-red-500">Inativo</span>}
          </Link>
        ))}
      </div>
    </div>
  )
}
```

- [ ] **Step 3: Criar PlanoWizard.tsx**

```tsx
// frontend/src/pages/planos/PlanoWizard.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { NichoTemplate } from '@/types/assinaturas'

const NICHOS = ['Barbearia', 'Salão', 'Estética', 'Pet Shop', 'Personal Trainer', 'Personalizado']

interface ItemForm {
  descricao: string
  quantidadePorCiclo: number
  tipo: string
  percentualDesconto: string
}

interface PlanoForm {
  nome: string
  descricao: string
  nicho: string
  preco: string
  periodicidade: string
  maisVendido: boolean
  itens: ItemForm[]
}

const BLANK_FORM: PlanoForm = {
  nome: '', descricao: '', nicho: 'Personalizado', preco: '',
  periodicidade: 'Mensal', maisVendido: false, itens: [],
}

export default function PlanoWizard() {
  const navigate = useNavigate()
  const [step, setStep] = useState(1)
  const [templates, setTemplates] = useState<NichoTemplate[]>([])
  const [selectedNicho, setSelectedNicho] = useState('')
  const [form, setForm] = useState<PlanoForm>(BLANK_FORM)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api.get<NichoTemplate[]>('/api/nicho-templates').then(setTemplates).catch(() => {})
  }, [])

  const templatesByNicho = templates.filter(t => t.nicho === selectedNicho)

  function applyTemplate(t: NichoTemplate) {
    setForm({
      nome: t.nomePlano,
      descricao: t.descricao ?? '',
      nicho: t.nicho,
      preco: String(t.precoSugerido),
      periodicidade: t.periodicidade,
      maisVendido: t.maisVendido,
      itens: t.itens.map(i => ({
        descricao: i.descricao,
        quantidadePorCiclo: i.quantidadePorCiclo,
        tipo: i.tipo,
        percentualDesconto: i.percentualDesconto ? String(i.percentualDesconto) : '',
      })),
    })
    setStep(2)
  }

  function addItem() {
    setForm(f => ({ ...f, itens: [...f.itens, { descricao: '', quantidadePorCiclo: 1, tipo: 'Servico', percentualDesconto: '' }] }))
  }

  function removeItem(idx: number) {
    setForm(f => ({ ...f, itens: f.itens.filter((_, i) => i !== idx) }))
  }

  async function handleSave() {
    setSaving(true)
    try {
      const result = await api.post<{ id: string }>('/api/planos-assinatura', {
        nome: form.nome,
        descricao: form.descricao || null,
        nicho: form.nicho,
        preco: parseFloat(form.preco),
        periodicidade: form.periodicidade,
        maisVendido: form.maisVendido,
        itens: form.itens.map(i => ({
          descricao: i.descricao,
          servicoId: null,
          quantidadePorCiclo: i.quantidadePorCiclo,
          tipo: i.tipo,
          percentualDesconto: i.percentualDesconto ? parseFloat(i.percentualDesconto) : null,
        })),
      })
      toast.success('Plano criado com sucesso!')
      navigate(`/planos/${result.id}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div className="flex items-center gap-2">
        {[1, 2, 3].map(n => (
          <div key={n} className={`h-2 flex-1 rounded-full ${step >= n ? 'bg-primary' : 'bg-muted'}`} />
        ))}
      </div>

      {step === 1 && (
        <div className="space-y-4">
          <h1 className="text-xl font-bold">Passo 1 — Escolha o Nicho</h1>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {NICHOS.map(n => (
              <button key={n} onClick={() => setSelectedNicho(n)}
                className={`rounded-lg border p-4 text-left transition-colors hover:border-primary ${selectedNicho === n ? 'border-primary bg-primary/5' : ''}`}>
                <p className="font-medium">{n}</p>
              </button>
            ))}
          </div>

          {selectedNicho && templatesByNicho.length > 0 && (
            <div className="space-y-2">
              <p className="text-sm font-medium">Modelos sugeridos para {selectedNicho}:</p>
              <div className="grid gap-3 sm:grid-cols-3">
                {templatesByNicho.map(t => (
                  <button key={t.id} onClick={() => applyTemplate(t)}
                    className="rounded-lg border p-3 text-left hover:border-primary transition-colors">
                    {t.maisVendido && <span className="text-xs text-amber-700 font-medium">⭐ Mais vendido</span>}
                    <p className="font-semibold">{t.nomePlano}</p>
                    <p className="text-primary font-bold">R$ {t.precoSugerido.toFixed(2).replace('.', ',')}/mês</p>
                    <ul className="text-xs text-muted-foreground mt-1 space-y-0.5">
                      {t.itens.slice(0, 3).map(i => <li key={i.id}>• {i.quantidadePorCiclo === 0 ? `${i.descricao} ilimitado` : `${i.quantidadePorCiclo}x ${i.descricao}`}</li>)}
                    </ul>
                  </button>
                ))}
              </div>
            </div>
          )}

          <div className="flex justify-between pt-2">
            <Button variant="outline" onClick={() => navigate('/planos')}>Cancelar</Button>
            <Button onClick={() => { setForm(f => ({ ...f, nicho: selectedNicho || 'Personalizado' })); setStep(2) }}
              disabled={!selectedNicho}>
              Próximo
            </Button>
          </div>
        </div>
      )}

      {step === 2 && (
        <div className="space-y-4">
          <h1 className="text-xl font-bold">Passo 2 — Serviços e Preço</h1>
          <div className="grid gap-3">
            <div className="grid gap-1.5">
              <Label>Nome do Plano</Label>
              <Input value={form.nome} onChange={e => setForm(f => ({ ...f, nome: e.target.value }))} />
            </div>
            <div className="grid gap-1.5">
              <Label>Descrição (opcional)</Label>
              <Input value={form.descricao} onChange={e => setForm(f => ({ ...f, descricao: e.target.value }))} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="grid gap-1.5">
                <Label>Preço (R$)</Label>
                <Input type="number" min="0" step="0.01" value={form.preco}
                  onChange={e => setForm(f => ({ ...f, preco: e.target.value }))} />
              </div>
              <div className="grid gap-1.5">
                <Label>Periodicidade</Label>
                <select value={form.periodicidade} onChange={e => setForm(f => ({ ...f, periodicidade: e.target.value }))}
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
                  {['Mensal', 'Trimestral', 'Semestral', 'Anual'].map(p => <option key={p}>{p}</option>)}
                </select>
              </div>
            </div>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <p className="font-medium text-sm">Itens do Plano</p>
              <Button size="sm" variant="outline" onClick={addItem}>+ Adicionar Item</Button>
            </div>
            {form.itens.map((item, idx) => (
              <div key={idx} className="rounded border p-3 grid gap-2 sm:grid-cols-3">
                <Input placeholder="Descrição" value={item.descricao}
                  onChange={e => setForm(f => ({ ...f, itens: f.itens.map((x, i) => i === idx ? { ...x, descricao: e.target.value } : x) }))} />
                <div className="flex gap-2">
                  <Input type="number" min="0" placeholder="Qtd (0=∞)" value={item.quantidadePorCiclo}
                    onChange={e => setForm(f => ({ ...f, itens: f.itens.map((x, i) => i === idx ? { ...x, quantidadePorCiclo: parseInt(e.target.value) || 0 } : x) }))} />
                  <select value={item.tipo}
                    onChange={e => setForm(f => ({ ...f, itens: f.itens.map((x, i) => i === idx ? { ...x, tipo: e.target.value } : x) }))}
                    className="flex h-9 rounded-md border border-input bg-transparent px-2 py-1 text-sm">
                    <option>Servico</option><option>Desconto</option><option>Beneficio</option>
                  </select>
                </div>
                <div className="flex gap-2 items-center">
                  {item.tipo === 'Desconto' && (
                    <Input type="number" placeholder="% desconto" value={item.percentualDesconto}
                      onChange={e => setForm(f => ({ ...f, itens: f.itens.map((x, i) => i === idx ? { ...x, percentualDesconto: e.target.value } : x) }))} />
                  )}
                  <Button size="sm" variant="ghost" className="text-red-500" onClick={() => removeItem(idx)}>✕</Button>
                </div>
              </div>
            ))}
          </div>

          <div className="flex justify-between pt-2">
            <Button variant="outline" onClick={() => setStep(1)}>Voltar</Button>
            <Button onClick={() => setStep(3)} disabled={!form.nome || !form.preco}>Próximo</Button>
          </div>
        </div>
      )}

      {step === 3 && (
        <div className="space-y-4">
          <h1 className="text-xl font-bold">Passo 3 — Revisão</h1>
          <div className="rounded-lg border p-5 space-y-3 max-w-xs">
            {form.maisVendido && <span className="text-xs bg-amber-100 text-amber-800 px-2 py-0.5 rounded-full">⭐ Mais vendido</span>}
            <h2 className="text-lg font-bold">{form.nome || 'Sem nome'}</h2>
            {form.descricao && <p className="text-sm text-muted-foreground">{form.descricao}</p>}
            <p className="text-3xl font-bold text-primary">
              R$ {(parseFloat(form.preco) || 0).toFixed(2).replace('.', ',')}
              <span className="text-sm font-normal text-muted-foreground">/{form.periodicidade.toLowerCase()}</span>
            </p>
            <ul className="text-sm space-y-1">
              {form.itens.map((i, idx) => (
                <li key={idx} className="flex items-center gap-1">
                  <span className="text-green-500">✓</span>
                  {i.quantidadePorCiclo === 0 ? `${i.descricao} (ilimitado)` : `${i.quantidadePorCiclo}x ${i.descricao}`}
                  {i.tipo === 'Desconto' && i.percentualDesconto && ` (${i.percentualDesconto}% desc.)`}
                </li>
              ))}
            </ul>
          </div>

          <div className="flex items-center gap-2">
            <input type="checkbox" id="maisVendido" checked={form.maisVendido}
              onChange={e => setForm(f => ({ ...f, maisVendido: e.target.checked }))} />
            <Label htmlFor="maisVendido">Marcar como "Mais vendido"</Label>
          </div>

          <div className="flex justify-between pt-2">
            <Button variant="outline" onClick={() => setStep(2)}>Voltar</Button>
            <Button onClick={() => void handleSave()} disabled={saving}>
              {saving ? 'Salvando...' : 'Ativar Plano'}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 4: Criar PlanoDetalhe.tsx**

```tsx
// frontend/src/pages/planos/PlanoDetalhe.tsx
import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { toast } from '@/hooks/useToast'
import type { PlanoAssinaturaResponse } from '@/types/assinaturas'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

export default function PlanoDetalhe() {
  const { id } = useParams<{ id: string }>()
  const [plano, setPlano] = useState<PlanoAssinaturaResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [slug, setSlug] = useState('')

  useEffect(() => {
    Promise.all([
      api.get<PlanoAssinaturaResponse>(`/api/planos-assinatura/${id}`),
      api.get<{ slug: string | null }>('/api/configuracao-empresa'),
    ]).then(([p, cfg]) => {
      setPlano(p)
      setSlug(cfg.slug ?? '')
    }).finally(() => setLoading(false))
  }, [id])

  if (loading) return <p className="text-sm text-muted-foreground">Carregando...</p>
  if (!plano) return <p className="text-red-500">Plano não encontrado.</p>

  const checkoutUrl = `${window.location.origin}/assinar/${slug}/${plano.id}`

  async function toggleAtivo() {
    try {
      await api.put(`/api/planos-assinatura/${plano!.id}`, { ...plano, ativo: !plano!.ativo })
      setPlano(p => p ? { ...p, ativo: !p.ativo } : p)
      toast.success(plano!.ativo ? 'Plano desativado' : 'Plano ativado')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    }
  }

  return (
    <div className="max-w-xl space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <Link to="/planos" className="text-sm text-muted-foreground hover:underline">← Planos</Link>
          <h1 className="text-2xl font-bold">{plano.nome}</h1>
          <span className="text-sm bg-muted px-2 py-0.5 rounded-full">{plano.nicho}</span>
        </div>
        <Button variant={plano.ativo ? 'outline' : 'default'} size="sm" onClick={() => void toggleAtivo()}>
          {plano.ativo ? 'Desativar' : 'Ativar'}
        </Button>
      </div>

      <div className="rounded-lg border p-4 space-y-2">
        <p className="text-3xl font-bold">
          R$ {plano.preco.toFixed(2).replace('.', ',')}
          <span className="text-sm font-normal text-muted-foreground">/{plano.periodicidade.toLowerCase()}</span>
        </p>
        {plano.descricao && <p className="text-sm text-muted-foreground">{plano.descricao}</p>}
        <p className="text-sm">{plano.totalAssinantes} assinante(s) ativo(s)</p>
      </div>

      {plano.itens.length > 0 && (
        <div className="space-y-2">
          <p className="font-medium">Itens do plano</p>
          <ul className="space-y-1">
            {plano.itens.map(i => (
              <li key={i.id} className="flex items-center gap-2 text-sm">
                <span className="text-green-500">✓</span>
                {i.quantidadePorCiclo === 0 ? `${i.descricao} (ilimitado)` : `${i.quantidadePorCiclo}x ${i.descricao}`}
                {i.tipo === 'Desconto' && i.percentualDesconto && ` — ${i.percentualDesconto}% desconto`}
              </li>
            ))}
          </ul>
        </div>
      )}

      {slug && (
        <div className="rounded-lg border p-4 space-y-2">
          <p className="font-medium text-sm">Link público de assinatura</p>
          <div className="flex items-center gap-2">
            <code className="text-xs bg-muted px-2 py-1 rounded flex-1 break-all">{checkoutUrl}</code>
            <Button size="sm" variant="outline"
              onClick={() => { void navigator.clipboard.writeText(checkoutUrl); toast.success('Copiado!') }}>
              Copiar
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 5: Atualizar router/index.tsx**

Adicionar imports:
```tsx
import PlanosList from '@/pages/planos/PlanosList'
import PlanoWizard from '@/pages/planos/PlanoWizard'
import PlanoDetalhe from '@/pages/planos/PlanoDetalhe'
```

Adicionar rotas no bloco autenticado (dentro de children do AppLayout):
```tsx
      { path: '/planos', element: <PlanosList /> },
      { path: '/planos/novo', element: <PlanoWizard /> },
      { path: '/planos/:id', element: <PlanoDetalhe /> },
```

- [ ] **Step 6: Atualizar Sidebar.tsx**

Adicionar import `Receipt` (já deve estar importado) ou usar `CreditCard` (remover se foi removido no Task 1, senão reaproveitar).

Adicionar grupo após Contratos:
```tsx
  {
    label: 'Assinaturas',
    items: [
      { icon: CreditCard, label: 'Planos', path: '/planos' },
    ],
  },
```

Garantir que `CreditCard` está nos imports lucide-react (adicionar se foi removido no Task 1).

- [ ] **Step 7: TypeScript check**

```bash
cd frontend && PATH="/opt/homebrew/opt/node/bin:$PATH" ./node_modules/.bin/tsc --noEmit 2>&1
```
Esperado: sem output

- [ ] **Step 8: Commit**

```bash
git add frontend/src/
git commit -m "feat: subscription plans UI — list, wizard (3 steps), and detail pages"
```

---

## Task 9: Frontend — Páginas Públicas de Assinatura

**Files:**
- Create: `frontend/src/pages/publico/AssinarSlug.tsx`
- Create: `frontend/src/pages/publico/AssinarPlano.tsx`
- Modify: `frontend/src/router/index.tsx`

- [ ] **Step 1: Criar AssinarSlug.tsx**

```tsx
// frontend/src/pages/publico/AssinarSlug.tsx
import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { PlanoAssinaturaListItem } from '@/types/assinaturas'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

interface EmpresaInfo {
  nomeFantasia: string | null
  logoUrl: string | null
  corPrimaria: string | null
  descricaoPublica: string | null
}

export default function AssinarSlug() {
  const { slug } = useParams<{ slug: string }>()
  const [info, setInfo] = useState<EmpresaInfo | null>(null)
  const [planos, setPlanos] = useState<PlanoAssinaturaListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState('')

  useEffect(() => {
    Promise.all([
      fetch(`${API_BASE}/public/${slug}/info`).then(r => r.ok ? r.json() as Promise<EmpresaInfo> : Promise.reject()),
      fetch(`${API_BASE}/public/${slug}/planos`).then(r => r.ok ? r.json() as Promise<PlanoAssinaturaListItem[]> : Promise.reject()),
    ]).then(([i, p]) => { setInfo(i); setPlanos(p) })
      .catch(() => setErro('Empresa não encontrada ou sem planos ativos.'))
      .finally(() => setLoading(false))
  }, [slug])

  const cor = info?.corPrimaria ?? '#3B82F6'

  if (loading) return <div className="flex h-screen items-center justify-center"><p className="text-gray-500">Carregando...</p></div>
  if (erro) return <div className="flex h-screen items-center justify-center"><p className="text-red-500">{erro}</p></div>

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="shadow-sm py-5" style={{ backgroundColor: cor }}>
        <div className="max-w-3xl mx-auto px-4 flex items-center gap-3">
          {info?.logoUrl && <img src={info.logoUrl} alt="Logo" className="h-10 object-contain" />}
          <div>
            <h1 className="text-white font-bold text-xl">{info?.nomeFantasia ?? 'Empresa'}</h1>
            {info?.descricaoPublica && <p className="text-white/80 text-sm">{info.descricaoPublica}</p>}
          </div>
        </div>
      </div>

      <div className="max-w-3xl mx-auto px-4 py-8 space-y-6">
        <h2 className="text-xl font-semibold text-center">Escolha seu plano</h2>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {planos.map(p => (
            <div key={p.id} className={`rounded-xl border-2 p-5 space-y-3 bg-white relative ${p.maisVendido ? 'border-primary shadow-md' : 'border-gray-200'}`}>
              {p.maisVendido && (
                <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                  <span className="text-xs bg-primary text-white px-3 py-0.5 rounded-full font-medium">⭐ Mais vendido</span>
                </div>
              )}
              <div>
                <p className="font-bold text-lg">{p.nome}</p>
                <span className="text-xs bg-gray-100 px-2 py-0.5 rounded-full">{p.nicho}</span>
              </div>
              <p className="text-3xl font-extrabold" style={{ color: cor }}>
                R$ {p.preco.toFixed(2).replace('.', ',')}
                <span className="text-sm font-normal text-gray-500">/{p.periodicidade.toLowerCase()}</span>
              </p>
              <Link to={`/assinar/${slug}/${p.id}`}>
                <button className="w-full py-2.5 rounded-lg text-white font-semibold text-sm transition-opacity hover:opacity-90"
                  style={{ backgroundColor: cor }}>
                  Assinar este plano
                </button>
              </Link>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Criar AssinarPlano.tsx**

```tsx
// frontend/src/pages/publico/AssinarPlano.tsx
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import type { PlanoAssinaturaResponse } from '@/types/assinaturas'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

interface EmpresaInfo { nomeFantasia: string | null; logoUrl: string | null; corPrimaria: string | null }
interface AssinarResponse { assinaturaId: string; pixQrCode: string | null; boletoUrl: string | null; valor: number; vencimento: string }

export default function AssinarPlano() {
  const { slug, planoId } = useParams<{ slug: string; planoId: string }>()
  const [info, setInfo] = useState<EmpresaInfo | null>(null)
  const [plano, setPlano] = useState<PlanoAssinaturaResponse | null>(null)
  const [form, setForm] = useState({ nome: '', whatsapp: '', email: '' })
  const [resultado, setResultado] = useState<AssinarResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [erro, setErro] = useState('')

  const cor = info?.corPrimaria ?? '#3B82F6'

  useEffect(() => {
    Promise.all([
      fetch(`${API_BASE}/public/${slug}/info`).then(r => r.json() as Promise<EmpresaInfo>),
      fetch(`${API_BASE}/public/${slug}/planos/${planoId}`).then(r => r.json() as Promise<PlanoAssinaturaResponse>),
    ]).then(([i, p]) => { setInfo(i); setPlano(p) })
      .catch(() => setErro('Plano não encontrado.'))
      .finally(() => setLoading(false))
  }, [slug, planoId])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setErro('')
    try {
      const res = await fetch(`${API_BASE}/public/${slug}/planos/${planoId}/assinar`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ nome: form.nome, whatsapp: form.whatsapp, email: form.email || null }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => null) as { error?: string } | null
        throw new Error(body?.error ?? 'Erro ao processar assinatura.')
      }
      setResultado(await res.json() as AssinarResponse)
    } catch (e) {
      setErro(e instanceof Error ? e.message : 'Erro')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <div className="flex h-screen items-center justify-center"><p>Carregando...</p></div>
  if (!plano) return <div className="flex h-screen items-center justify-center"><p className="text-red-500">{erro || 'Plano não encontrado.'}</p></div>

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="py-4 shadow-sm" style={{ backgroundColor: cor }}>
        <div className="max-w-lg mx-auto px-4 flex items-center gap-3">
          {info?.logoUrl && <img src={info.logoUrl} alt="Logo" className="h-8 object-contain" />}
          <p className="text-white font-semibold">{info?.nomeFantasia ?? ''}</p>
        </div>
      </div>

      <div className="max-w-lg mx-auto px-4 py-8 space-y-6">
        {/* Resumo do plano */}
        <div className="rounded-xl border bg-white p-5 space-y-2">
          <p className="font-bold text-lg">{plano.nome}</p>
          {plano.descricao && <p className="text-sm text-gray-500">{plano.descricao}</p>}
          <p className="text-2xl font-extrabold" style={{ color: cor }}>
            R$ {plano.preco.toFixed(2).replace('.', ',')}
            <span className="text-sm font-normal text-gray-500">/{plano.periodicidade.toLowerCase()}</span>
          </p>
          <ul className="text-sm space-y-1 pt-1">
            {plano.itens.map(i => (
              <li key={i.id} className="flex gap-1">
                <span style={{ color: cor }}>✓</span>
                {i.quantidadePorCiclo === 0 ? `${i.descricao} (ilimitado)` : `${i.quantidadePorCiclo}x ${i.descricao}`}
              </li>
            ))}
          </ul>
        </div>

        {!resultado ? (
          <form onSubmit={e => void handleSubmit(e)} className="bg-white rounded-xl border p-5 space-y-4">
            <h2 className="font-semibold">Seus dados</h2>
            <div className="space-y-3">
              <div>
                <label className="text-sm font-medium block mb-1">Nome completo</label>
                <input required value={form.nome} onChange={e => setForm(f => ({ ...f, nome: e.target.value }))}
                  className="w-full border rounded-lg px-3 py-2.5 text-base outline-none focus:ring-2" style={{ fontSize: '16px' }}
                  placeholder="João Silva" />
              </div>
              <div>
                <label className="text-sm font-medium block mb-1">WhatsApp</label>
                <input required value={form.whatsapp} onChange={e => setForm(f => ({ ...f, whatsapp: e.target.value }))}
                  className="w-full border rounded-lg px-3 py-2.5 text-base outline-none focus:ring-2" style={{ fontSize: '16px' }}
                  placeholder="11999990000" type="tel" />
              </div>
              <div>
                <label className="text-sm font-medium block mb-1">E-mail (opcional)</label>
                <input value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                  className="w-full border rounded-lg px-3 py-2.5 text-base outline-none focus:ring-2" style={{ fontSize: '16px' }}
                  placeholder="joao@email.com" type="email" />
              </div>
            </div>
            {erro && <p className="text-red-500 text-sm">{erro}</p>}
            <button type="submit" disabled={submitting}
              className="w-full py-3 rounded-lg text-white font-semibold text-base disabled:opacity-60"
              style={{ backgroundColor: cor, minHeight: '48px' }}>
              {submitting ? 'Processando...' : 'Confirmar Assinatura'}
            </button>
          </form>
        ) : (
          <div className="bg-white rounded-xl border p-5 space-y-4">
            <div className="text-center space-y-1">
              <p className="text-2xl">🎉</p>
              <h2 className="font-bold text-lg">Assinatura confirmada!</h2>
              <p className="text-sm text-gray-500">Realize o pagamento para ativar seu plano.</p>
            </div>

            {resultado.pixQrCode && (
              <div className="space-y-3">
                <p className="font-medium text-center text-sm">Pague via PIX</p>
                <div className="flex justify-center">
                  <img
                    src={`data:image/png;base64,${resultado.pixQrCode}`}
                    alt="QR Code PIX"
                    className="w-48 h-48 rounded border p-2"
                  />
                </div>
                <button onClick={() => void navigator.clipboard.writeText(resultado.pixQrCode ?? '')}
                  className="w-full border rounded-lg py-2.5 text-sm font-medium"
                  style={{ minHeight: '44px' }}>
                  Copiar código PIX
                </button>
              </div>
            )}

            {resultado.boletoUrl && (
              <a href={resultado.boletoUrl} target="_blank" rel="noopener noreferrer"
                className="block text-center text-sm underline text-gray-500">
                Pagar com Boleto
              </a>
            )}

            <p className="text-xs text-center text-gray-400">
              Vencimento: {new Date(resultado.vencimento).toLocaleDateString('pt-BR')} •
              R$ {resultado.valor.toFixed(2).replace('.', ',')}
            </p>
          </div>
        )}
      </div>
    </div>
  )
}
```

- [ ] **Step 3: Adicionar rotas públicas em router/index.tsx**

Adicionar imports:
```tsx
import AssinarSlug from '@/pages/publico/AssinarSlug'
import AssinarPlano from '@/pages/publico/AssinarPlano'
```

Adicionar rotas públicas no array principal (fora do AppLayout, igual ao padrão do `/agendar/:slug`):
```tsx
  { path: '/assinar/:slug', element: <AssinarSlug />, errorElement: <ErrorPage /> },
  { path: '/assinar/:slug/:planoId', element: <AssinarPlano />, errorElement: <ErrorPage /> },
```

- [ ] **Step 4: Adicionar endpoint GET /public/:slug/info em PublicBookingEndpoints.cs**

Verificar se já existe. Se não, adicionar em `PublicBookingEndpoints.cs`:

```csharp
        group.MapGet("/info", async (
            string slug, PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetInfoAsync(empresaId, ct));
        });
```

- [ ] **Step 5: TypeScript check final**

```bash
cd frontend && PATH="/opt/homebrew/opt/node/bin:$PATH" ./node_modules/.bin/tsc --noEmit 2>&1
```
Esperado: sem output

- [ ] **Step 6: Build backend final**

```bash
cd ../backend && /usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj --no-restore -v quiet 2>&1 | tail -5
```
Esperado: `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`

- [ ] **Step 7: Rodar todos os testes**

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests 2>&1 | tail -5
```
Esperado: `Passed! - Failed: 0`

- [ ] **Step 8: Commit final**

```bash
cd ..
git add frontend/src/ backend/src/GestorAI.API/Endpoints/
git commit -m "feat: public subscription checkout pages (/assinar/:slug and /assinar/:slug/:planoId)"
```

---

## Sprint 9 — Plano Subsequente

Sprint 9 (`ConsumoAssinatura` + painel MRR) deve ser planejada em separado após Sprint 8 estar em produção. Cobre:
- Entidade `ConsumoAssinatura` + campos em `Agendamento`
- `AssinaturaService.RegistrarConsumoAsync` + integração em `AgendamentoService.ConfirmarAsync`
- Endpoint `GET /api/assinaturas/mrr`
- Telas `/assinaturas` (painel MRR) e `/assinaturas/:id` (detalhe + barras de consumo)
