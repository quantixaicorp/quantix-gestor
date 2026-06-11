# Motor de Assinaturas — Design Spec

> Sprint 8 (Motor + Checkout) e Sprint 9 (Painel de Assinantes + Consumo)

---

## Objetivo

Permitir que qualquer empresa cadastrada no GestorAI crie planos de assinatura para fidelizar seus próprios clientes. O gestor configura planos com serviços, quantidades por ciclo e preço; o cliente assina por uma página pública white label e paga via PIX/boleto. O controle financeiro corre pelo fluxo já existente de Contratos → Cobranças.

---

## O que será removido antes da implementação

| Item | Local |
|---|---|
| `PlanoSaaS`, `EmpresaPlano` | Entities |
| `PlanoService`, `FeatureService`, `BillingService`, `TenantResolutionService` | Services |
| `PlanosEndpoints`, `BillingEndpoints` | Endpoints |
| Migrations `AddPlanosSaaS`, `AddBillingSaaS` | Migrations |
| Feature gates em ContratosEndpoints e CobrancasEndpoints | Endpoints |
| `PlanoAssinatura.tsx`, `WhiteLabel.tsx` | Frontend pages |
| Rotas `/configuracoes/plano` e `/configuracoes/white-label` | Router |
| Links Plano e White Label no Sidebar | Sidebar |
| Seção `SaaS` no appsettings.json | Config |
| Campos `DominioCustomizado`, `AsaasClienteIdSaaS`, `AssinaturaAsaasId`, `StatusAssinatura`, `ProximaCobrancaEm` | `ConfiguracaoEmpresa` |
| Middleware de resolução de domínio | `Program.cs` |

---

## Arquitetura

### Decisão de billing

O pagamento mensal segue o fluxo **Contrato → Cobrança** já existente:
- Na adesão: sistema cria `AssinaturaCliente` + `Contrato` automaticamente
- A primeira `Cobrança` é gerada imediatamente e enviada ao Asaas (PIX + boleto)
- Renovações mensais: gestor usa "Gerar Cobranças" no Contrato (Sprint 8) ou renovação automática via cron (Sprint 9)

### Checkout público

Página white label em `/assinar/:slug/:planoId` — sem referência ao GestorAI. Usa logo e nome da empresa do `ConfiguracaoEmpresa`.

---

## Modelo de Dados

### Sprint 8

#### `PlanoAssinatura` (tenant)
```csharp
public class PlanoAssinatura : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public string? Descricao { get; set; }
    public string Nicho { get; set; } = "Personalizado";   // string livre
    public decimal Preco { get; set; }
    public Periodicidade Periodicidade { get; set; }       // enum já existente
    public bool Ativo { get; set; } = true;
    public bool MaisVendido { get; set; } = false;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<PlanoAssinaturaItem> Itens { get; set; } = [];
    public ICollection<AssinaturaCliente> Assinantes { get; set; } = [];
}
```

#### `PlanoAssinaturaItem`
```csharp
public class PlanoAssinaturaItem
{
    public Guid Id { get; set; }
    public Guid PlanoAssinaturaId { get; set; }
    public required string Descricao { get; set; }
    public Guid? ServicoId { get; set; }            // link ao Produto/Serviço (para consumo Sprint 9)
    public int QuantidadePorCiclo { get; set; } = 1; // 0 = ilimitado
    public TipoItemPlano Tipo { get; set; }          // Servico | Desconto | Beneficio
    public decimal? PercentualDesconto { get; set; } // para Tipo=Desconto
}
```

#### `TipoItemPlano` (enum novo)
```csharp
public enum TipoItemPlano { Servico, Desconto, Beneficio }
```

#### `AssinaturaCliente` (tenant)
```csharp
public class AssinaturaCliente : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid PlanoAssinaturaId { get; set; }
    public Guid ContratoId { get; set; }            // contrato gerado na adesão
    public AssinaturaStatus Status { get; set; } = AssinaturaStatus.Ativa;
    public DateOnly DataInicio { get; set; }
    public DateOnly DataRenovacao { get; set; }     // próxima renovação
    public int CicloAtual { get; set; } = 1;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public PlanoAssinatura? Plano { get; set; }
    public Contrato? Contrato { get; set; }
    public ICollection<ConsumoAssinatura> Consumos { get; set; } = []; // Sprint 9
}
```

#### `AssinaturaStatus` (enum novo)
```csharp
public enum AssinaturaStatus { Ativa, Cancelada, Inadimplente, Expirada }
```

#### `NichoTemplate` (global, sem EmpresaId — dados de seed)
```csharp
public class NichoTemplate
{
    public Guid Id { get; set; }
    public required string Nicho { get; set; }    // "Barbearia", "Salao", etc.
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
}
```

### Alterações em entidades existentes

**`Contrato`**: adicionar campo `AssinaturaClienteId? (Guid?)` — identifica contratos originados por assinatura.

### Sprint 9

#### `ConsumoAssinatura` (tenant)
```csharp
public class ConsumoAssinatura : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid AssinaturaClienteId { get; set; }
    public Guid PlanoAssinaturaItemId { get; set; }
    public Guid? AgendamentoId { get; set; }        // se consumido via agendamento
    public string Ciclo { get; set; } = "";         // "2026-06" (ano-mês)
    public int Quantidade { get; set; } = 1;
    public DateTime DataConsumo { get; set; } = DateTime.UtcNow;
    public AssinaturaCliente? Assinatura { get; set; }
    public PlanoAssinaturaItem? Item { get; set; }
}
```

**`Agendamento`**: adicionar `AssinaturaClienteId? (Guid?)` e `ConsumoAssinaturaId? (Guid?)`.

---

## Seed de Templates (NichoTemplate)

### Barbearia
| Plano | Preço | Itens | Mais vendido |
|---|---|---|---|
| Básico | R$ 79 | 2 cortes/mês | não |
| Premium | R$ 129 | 4 cortes + 1 barba/mês + 10% desc. produtos | **sim** |
| VIP | R$ 199 | Cortes ilimitados + 2 barbas + 1 hidratação + horário exclusivo | não |

### Salão de Beleza
| Plano | Preço | Itens |
|---|---|---|
| Mãos & Pés | R$ 99 | 2 manicures + 2 pedicures/mês |
| Beleza Completa | R$ 189 | 4 manicures + 4 pedicures + 1 escova/mês |
| Noiva & Premium | R$ 299 | Ilimitado mãos e pés + 2 escovas + 1 hidratação/mês |

### Estética
| Plano | Preço | Itens |
|---|---|---|
| Pele | R$ 149 | 2 limpezas de pele/mês |
| Corpo & Rosto | R$ 249 | 4 procedimentos variados/mês |
| Premium Spa | R$ 399 | 8 procedimentos + drenagem + massagem/mês |

### Pet Shop
| Plano | Preço | Itens |
|---|---|---|
| Porte Pequeno | R$ 89 | 2 banhos + 1 tosa/mês |
| Porte Médio | R$ 139 | 2 banhos + 1 tosa/mês |
| Porte Grande | R$ 199 | 2 banhos + 1 tosa/mês |

### Personal Trainer
| Plano | Preço | Itens |
|---|---|---|
| 2x/semana | R$ 299 | 8 sessões presenciais/mês |
| 3x/semana | R$ 419 | 12 sessões presenciais/mês |
| Diário + Online | R$ 599 | 20 sessões + acompanhamento online ilimitado/mês |

---

## Endpoints

### Autenticados (gestor)

```
GET    /api/planos-assinatura                    listar planos da empresa
POST   /api/planos-assinatura                    criar plano
GET    /api/planos-assinatura/:id                detalhe do plano
PUT    /api/planos-assinatura/:id                editar plano
DELETE /api/planos-assinatura/:id                desativar plano

GET    /api/nicho-templates                      listar todos os templates
GET    /api/nicho-templates?nicho=Barbearia      filtrar por nicho

GET    /api/assinaturas                          listar assinantes (filtros: planoId, status)
GET    /api/assinaturas/:id                      detalhe + consumo do ciclo atual
POST   /api/assinaturas/:id/cancelar             cancelar assinatura

// Sprint 9
GET    /api/assinaturas/mrr                      MRR + taxa de renovação
POST   /api/assinaturas/:id/consumir             registrar consumo manual de benefício
```

### Públicos (sem auth)

```
GET    /public/:slug/planos                      listar planos ativos da empresa
GET    /public/:slug/planos/:id                  detalhe do plano
POST   /public/:slug/planos/:id/assinar          assinar + gerar cobrança Asaas
```

#### Request `POST /public/:slug/planos/:id/assinar`
```json
{
  "nome": "João Silva",
  "whatsapp": "11999999999",
  "email": "joao@email.com"
}
```

#### Response
```json
{
  "assinaturaId": "uuid",
  "contratoId": "uuid",
  "cobrancaId": "uuid",
  "pixQrCode": "base64...",
  "pixCopiaCola": "00020126...",
  "boletoUrl": "https://...",
  "valor": 129.00,
  "vencimento": "2026-06-13"
}
```

---

## Lógica de Negócio — `AssinaturaService`

### `AssinarAsync`
1. Busca `PlanoAssinatura` pelo slug da empresa + planoId (sem auth)
2. Busca ou cria `Cliente` pelo WhatsApp (sem criar duplicata)
3. Cria `AssinaturaCliente` (Status: Ativa, DataInicio: hoje, DataRenovacao: hoje + 1 mês)
4. Cria `Contrato` automaticamente:
   - Titulo: `"Assinatura — {PlanoNome}"`
   - Objeto: itens do plano formatados
   - Valor: `PlanoAssinatura.Preco`
   - Periodicidade: do plano
   - DiaVencimento: dia atual
   - Status: `Ativo` (direto, sem passar por Rascunho)
   - `AssinaturaClienteId`: vinculado
5. Gera primeira `Cobrança` (vencimento: hoje + 3 dias)
6. Chama `CobrancaService.EnviarAsaasAsync` (já existente) → obtém PIX QR Code + boleto
7. Retorna dados de pagamento

### `CancelarAsync`
1. `AssinaturaCliente.Status = Cancelada`
2. Encerra o `Contrato` vinculado (`Status = Encerrado`)
3. Cobranças pendentes futuras: cancela automaticamente

### Sprint 9 — `RegistrarConsumoAsync`
1. Verifica se `AssinaturaCliente` está Ativa
2. Verifica se item do plano tem `ServicoId` correspondente ao agendamento
3. Conta consumos do ciclo atual (`Ciclo = "2026-06"`)
4. Se `QuantidadePorCiclo > 0` e já atingiu o limite → retorna false (cobrar normalmente)
5. Se dentro do limite → cria `ConsumoAssinatura`, vincula ao `Agendamento`

### Integração com Agendamentos (Sprint 9)
Em `AgendamentoService.ConfirmarAsync`:
1. Se `Agendamento.ClienteId` não nulo → busca `AssinaturaCliente` ativa com o serviço no plano
2. Se encontrar e tiver saldo → chama `RegistrarConsumoAsync`
3. Atualiza `Agendamento.AssinaturaClienteId` e `ConsumoAssinaturaId`

---

## Telas — Frontend

### `/planos` — Lista de planos (gestor)
- Grid responsivo: 1 col mobile / 2 col tablet / 3 col desktop
- Card por plano: nome, nicho badge, preço/mês, qtd assinantes ativos, link copiável
- Botão "Novo Plano"

### `/planos/novo` — Wizard 3 steps
**Step 1 — Nicho:**
- Grid 2×3 de cards de nicho (Barbearia, Salão, Estética, Pet Shop, Personal Trainer, Outro)
- Ao selecionar nicho com template → exibe 3 cards de planos sugeridos com preços
- Selecionar template pré-preenche Step 2; "Outro" → campo texto livre + Step 2 em branco
- Campos: nome do plano, descrição, preço, periodicidade

**Step 2 — Serviços:**
- Lista de itens adicionáveis
- Por item: descrição (ou busca por serviço cadastrado), tipo, quantidade por ciclo
- Cálculo de economia em tempo real: `(PreçoUnitárioServico × QtdPorCiclo) - PreçoPlano`
- Mobile: cada item em card expansível

**Step 3 — Revisão:**
- Preview do card como cliente verá
- Badge "Mais vendido" configurável
- Botão "Ativar Plano"

### `/planos/:id` — Detalhe do plano
- Edição dos campos
- Link público do checkout (botão copiar)
- Lista de assinantes ativos

### Página pública `/assinar/:slug`
- Logo + nome da empresa (sem referência ao GestorAI)
- Cards de planos: benefícios com checkmarks, preço em destaque, badge "mais vendido"
- Mobile: 1 col; Tablet: 2 col; Desktop: 3 col
- Botão "Assinar" por plano

### Página pública `/assinar/:slug/:planoId`
- Resumo do plano selecionado
- Formulário: Nome, WhatsApp, E-mail (inputs ≥ 16px font-size, touch targets ≥ 48px)
- Botão "Confirmar Assinatura" → chama API → exibe seção de pagamento
- **Mobile**: pagamento como bottom sheet (desliza de baixo)
- **Desktop**: seção abaixo do formulário
- QR Code PIX em tamanho adequado + botão "Copiar código"
- Link alternativo "Pagar com Boleto"
- Mensagem final: "Pagamento confirmado via webhook → WhatsApp notificado"

### `/assinaturas` — Painel de assinantes (Sprint 9)
- KPIs: MRR, ativos, renovando em 7 dias, inadimplentes
- Tabela → cards mobile
- Filtros: plano, status
- Por assinante: nome, plano, status badge, barra resumo de consumo, data renovação

### `/assinaturas/:id` — Detalhe do assinante (Sprint 9)
- Status + próxima renovação
- Barras de consumo por serviço (ex: "Cortes: 1 / 2 utilizados no ciclo")
- Histórico de cobranças
- Botão cancelar assinatura

---

## Responsividade

Todas as telas seguem os padrões definidos no documento de responsividade:
- Mobile-first, testado em 375px, 390px, 360px
- Touch targets ≥ 44px em todos os botões e actions
- Inputs com `font-size: 16px` mínimo (evita zoom no iOS)
- Modais → bottom sheet no mobile
- Tabelas → cards empilhados no mobile
- Sem scroll horizontal na página principal
- Wizard steps: cada step cabe na viewport sem scroll

---

## Migrations necessárias

1. `AddMotorAssinaturas` — cria tabelas `PlanoAssinaturas`, `PlanoAssinaturaItens`, `AssinaturasCliente`, `NichoTemplates`, `NichoTemplateItens`; adiciona `AssinaturaClienteId` em `Contratos`
2. `RemoveFeaturesSaaS` — remove colunas de white label e billing de `ConfiguracaoEmpresa`; remove tabelas `PlanosSaaS` e `EmpresasPlano`
3. `AddConsumoAssinatura` (Sprint 9) — cria `ConsumoAssinaturas`; adiciona `AssinaturaClienteId` e `ConsumoAssinaturaId` em `Agendamentos`

---

## Decomposição em Tarefas (referência para plano de implementação)

### Sprint 8

- [ ] **Limpeza**: remover código SaaS/White Label (entities, services, endpoints, migrations, frontend, sidebar)
- [ ] **Migration 1**: `RemoveFeaturesSaaS`
- [ ] **Migration 2**: `AddMotorAssinaturas` + seed NichoTemplates
- [ ] **Backend**: `PlanoAssinaturaService` + endpoints CRUD
- [ ] **Backend**: `AssinaturaService.AssinarAsync` (fluxo completo: cliente → contrato → cobrança → Asaas)
- [ ] **Backend**: endpoints públicos `/public/:slug/planos`
- [ ] **Frontend**: telas internas `/planos` e `/planos/novo` (wizard)
- [ ] **Frontend**: páginas públicas `/assinar/:slug` e `/assinar/:slug/:planoId`

### Sprint 9

- [ ] **Migration 3**: `AddConsumoAssinatura`
- [ ] **Backend**: `AssinaturaService.RegistrarConsumoAsync`
- [ ] **Backend**: integração em `AgendamentoService.ConfirmarAsync`
- [ ] **Backend**: endpoint `GET /api/assinaturas/mrr`
- [ ] **Frontend**: `/assinaturas` (painel MRR + lista)
- [ ] **Frontend**: `/assinaturas/:id` (detalhe + consumo)
