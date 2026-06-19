# Venda Lojista vs Prestador de Serviço — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Parametrizar a tela de vendas para que empresas do tipo "Prestador de Serviço" vejam uma tela de Ordem de Serviço (OS) com cliente obrigatório, profissional responsável e itens filtrados por serviço — em vez do PDV padrão de lojista.

**Architecture:** Dispatcher em `/vendas/nova` lê `ConfiguracaoEmpresa.tipoNegocio` e renderiza `<NovaVendaPDV>` (atual) ou `<NovaVendaOS>` (nova). Backend recebe `ProfissionalId` e `ObservacaoOS` opcionais no `CreateVendaRequest` e os persiste na `Venda`. Migration EF Core adiciona os campos ao banco.

**Tech Stack:** ASP.NET Core Minimal API, Entity Framework Core (PostgreSQL schema `gestor`), React + TypeScript + Vite, Tailwind CSS, react-hook-form (não usado nas telas de venda — estado manual).

---

## Mapa de arquivos

### Backend — criar/modificar
| Arquivo | Ação |
|---|---|
| `Domain/Entities/ConfiguracaoEmpresa.cs` | Adicionar `TipoNegocio string` |
| `Domain/Entities/Venda.cs` | Adicionar `ProfissionalId Guid?`, `ProfissionalNome string?`, `ObservacaoOS string?` |
| `DTOs/Fiscal/ConfiguracaoEmpresaDto.cs` | Adicionar `TipoNegocio` em request e response |
| `DTOs/Vendas/VendaDto.cs` | Adicionar `ProfissionalId?`, `ObservacaoOS?` em `CreateVendaRequest`; `ProfissionalNome?`, `ObservacaoOS?` em `VendaResponse` e `VendaListItem` |
| `Services/Fiscal/ConfiguracaoEmpresaService.cs` | Mapear `TipoNegocio` em `AtualizarAsync` e `ToResponse` |
| `Services/Vendas/VendaService.cs` | Resolver `ProfissionalNome` a partir de `ProfissionalId` em `CreateAsync`; expor em `GetAsync` e `ListAsync` |
| `Services/Agendamentos/AgendamentoService.cs` | Em `ConcluirAsync`, preencher `ProfissionalId` e `ProfissionalNome` na `Venda` gerada |
| `Infrastructure/Data/Migrations/` | Nova migration `AddTipoNegocioEVendaOS` |

### Frontend — criar/modificar
| Arquivo | Ação |
|---|---|
| `types/fiscal.ts` | Adicionar `tipoNegocio` em `ConfiguracaoEmpresaResponse` e `AtualizarConfiguracaoEmpresaRequest` |
| `types/vendas.ts` | Adicionar `profissionalId?`, `observacaoOS?` em `CreateVendaRequest`; `profissionalNome?`, `observacaoOS?` em `VendaResponse` e `VendaListItem` |
| `pages/vendas/NovaVenda.tsx` | Substituir por dispatcher (lê config, renderiza PDV ou OS) |
| `pages/vendas/NovaVendaPDV.tsx` | Criar — conteúdo atual de `NovaVenda.tsx` sem alterações |
| `pages/vendas/NovaVendaOS.tsx` | Criar — tela de OS (novo) |
| `pages/vendas/Historico.tsx` | Adicionar coluna Profissional quando `tipoNegocio === "Prestador"` |
| `pages/configuracoes/ConfiguracaoEmpresa.tsx` | Adicionar campo "Tipo de negócio" na seção de identificação |

---

## Task 1: Migration — novos campos no banco

**Files:**
- Create: `backend/src/GestorAI.API/Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_AddTipoNegocioEVendaOS.cs`
- Modify: `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs`
- Modify: `backend/src/GestorAI.API/Domain/Entities/Venda.cs`

- [ ] **Step 1: Adicionar campo `TipoNegocio` à entidade `ConfiguracaoEmpresa`**

Em `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs`, adicionar antes de `}` final:

```csharp
    public string TipoNegocio { get; set; } = "Lojista";
```

- [ ] **Step 2: Adicionar campos `ProfissionalId`, `ProfissionalNome`, `ObservacaoOS` à entidade `Venda`**

Em `backend/src/GestorAI.API/Domain/Entities/Venda.cs`, adicionar antes de `}` final:

```csharp
    public Guid?   ProfissionalId   { get; set; }
    public string? ProfissionalNome { get; set; }
    public string? ObservacaoOS     { get; set; }
```

- [ ] **Step 3: Gerar migration via dotnet ef**

```bash
cd backend/src/GestorAI.API
dotnet ef migrations add AddTipoNegocioEVendaOS
```

Confirmar que o arquivo `.cs` gerado contém `AddColumn` para `tipo_negocio` em `configuracoes_empresa` e `profissional_id`, `profissional_nome`, `observacao_os` em `vendas` (schema `gestor`).

> **Atenção:** EF Core com Npgsql converte `ObservacaoOS` para `observacao_o_s` pelo padrão snake_case. Se isso ocorrer, adicionar data annotation em `Venda.cs` antes de rodar a migration:
> ```csharp
> [Column("observacao_os")]
> public string? ObservacaoOS { get; set; }
> ```
> E adicionar `using System.ComponentModel.DataAnnotations.Schema;` no topo do arquivo.

- [ ] **Step 4: Aplicar migration**

```bash
dotnet ef database update
```

Esperado: `Done.` sem erros.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs \
        backend/src/GestorAI.API/Domain/Entities/Venda.cs \
        backend/src/GestorAI.API/Infrastructure/Data/Migrations/
git commit -m "feat: migration AddTipoNegocioEVendaOS — campos de OS em Venda e tipo em ConfiguracaoEmpresa"
```

---

## Task 2: Backend — DTOs e serviço de configuração

**Files:**
- Modify: `backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs`
- Modify: `backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs`

- [ ] **Step 1: Adicionar `TipoNegocio` ao `AtualizarConfiguracaoEmpresaRequest`**

Em `ConfiguracaoEmpresaDto.cs`, no record `AtualizarConfiguracaoEmpresaRequest`, adicionar o parâmetro após `string? Email = null`:

```csharp
    string? TipoNegocio = null);
```

- [ ] **Step 2: Adicionar `TipoNegocio` ao `ConfiguracaoEmpresaResponse`**

No record `ConfiguracaoEmpresaResponse`, adicionar ao final dos parâmetros:

```csharp
    string TipoNegocio = "Lojista");
```

- [ ] **Step 3: Mapear `TipoNegocio` em `ConfiguracaoEmpresaService`**

Em `AtualizarAsync`, após o bloco `if (req.Email is not null)`, adicionar:

```csharp
        if (req.TipoNegocio is not null) config.TipoNegocio = req.TipoNegocio;
```

No método `ToResponse`, adicionar `c.TipoNegocio` ao final da chamada do construtor `ConfiguracaoEmpresaResponse(...)`. A última linha do construtor (antes do `;`) fica:

```csharp
        c.HorasLimiteCancelamento,
        c.TipoNegocio);
```

- [ ] **Step 4: Build para verificar compilação**

```bash
cd backend/src/GestorAI.API
dotnet build
```

Esperado: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs \
        backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs
git commit -m "feat: expõe TipoNegocio em ConfiguracaoEmpresa — request, response e service"
```

---

## Task 3: Backend — DTOs e serviço de venda

**Files:**
- Modify: `backend/src/GestorAI.API/DTOs/Vendas/VendaDto.cs`
- Modify: `backend/src/GestorAI.API/Services/Vendas/VendaService.cs`
- Modify: `backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs`

- [ ] **Step 1: Atualizar `CreateVendaRequest` com campos de OS**

Em `VendaDto.cs`, substituir o record `CreateVendaRequest` por:

```csharp
public record CreateVendaRequest(
    Guid? ClienteId,
    List<ItemVendaRequest> Itens,
    decimal Desconto,
    string FormaPagamento,
    int? Parcelas,
    string? Observacao,
    DateTime? DataHora = null,
    Guid? ProfissionalId = null,
    string? ObservacaoOS = null);
```

- [ ] **Step 2: Atualizar `VendaResponse` e `VendaListItem` com campos de OS**

Substituir os dois records:

```csharp
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
    List<ItemVendaResponse> Itens,
    string? ProfissionalNome = null,
    string? ObservacaoOS = null);

public record VendaListItem(
    Guid Id,
    Guid? ClienteId,
    string? ClienteNome,
    DateTime DataHora,
    string Status,
    decimal Total,
    string FormaPagamento,
    string? ProfissionalNome = null);
```

- [ ] **Step 3: Resolver `ProfissionalNome` em `VendaService.CreateAsync`**

Em `VendaService.cs`, após declarar `var venda = new Venda { ... }`, adicionar resolução do profissional. No bloco de inicialização do objeto `Venda`, adicionar os novos campos logo após `Observacao = req.Observacao`:

```csharp
                ProfissionalId   = req.ProfissionalId,
                ObservacaoOS     = req.ObservacaoOS,
```

Após o bloco `var nomeCliente = ...`, adicionar:

```csharp
            string? nomeProfissional = null;
            if (req.ProfissionalId.HasValue)
                nomeProfissional = (await db.Profissionais.FindAsync([req.ProfissionalId.Value], ct))?.Nome;
            venda.ProfissionalNome = nomeProfissional;
```

- [ ] **Step 4: Expor campos de OS em `VendaService.GetAsync`**

Substituir a construção do `VendaResponse` em `GetAsync`:

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
                i.Desconto, i.Total)).ToList(),
            venda.ProfissionalNome,
            venda.ObservacaoOS);
```

- [ ] **Step 5: Expor `ProfissionalNome` em `VendaService.ListAsync`**

Substituir o `.Select(...)` em `ListAsync`:

```csharp
            .Select(v => new VendaListItem(
                v.Id, v.ClienteId, v.Cliente != null ? v.Cliente.Nome : null,
                v.DataHora, v.Status.ToString(),
                v.Total, v.FormaPagamento.ToString(),
                v.ProfissionalNome))
```

- [ ] **Step 6: Preencher `ProfissionalId` e `ProfissionalNome` ao concluir agendamento**

Em `AgendamentoService.ConcluirAsync`, no bloco de inicialização `var venda = new Venda { ... }`, adicionar os campos (o profissional já está carregado no agendamento via `a.ProfissionalId`):

Primeiro, carregar o nome do profissional antes do bloco da venda:

```csharp
        var profissional = await db.Profissionais.FindAsync([a.ProfissionalId], ct);
```

Depois, no objeto `Venda`, adicionar após `Observacao`:

```csharp
            ProfissionalId   = a.ProfissionalId,
            ProfissionalNome = profissional?.Nome,
            ObservacaoOS     = $"Agendamento de {a.ClienteNome}",
```

- [ ] **Step 7: Build para verificar compilação**

```bash
cd backend/src/GestorAI.API
dotnet build
```

Esperado: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Vendas/VendaDto.cs \
        backend/src/GestorAI.API/Services/Vendas/VendaService.cs \
        backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs
git commit -m "feat: VendaService e AgendamentoService — campos ProfissionalId, ProfissionalNome, ObservacaoOS"
```

---

## Task 4: Frontend — tipos TypeScript

**Files:**
- Modify: `frontend/src/types/fiscal.ts`
- Modify: `frontend/src/types/vendas.ts`

- [ ] **Step 1: Adicionar `tipoNegocio` em `ConfiguracaoEmpresaResponse`**

Em `types/fiscal.ts`, adicionar ao final do interface `ConfiguracaoEmpresaResponse`:

```typescript
  tipoNegocio: string
```

- [ ] **Step 2: Adicionar `tipoNegocio` em `AtualizarConfiguracaoEmpresaRequest`**

Adicionar ao final do interface `AtualizarConfiguracaoEmpresaRequest`:

```typescript
  tipoNegocio?: string
```

- [ ] **Step 3: Adicionar campos de OS em `CreateVendaRequest`**

Em `types/vendas.ts`, adicionar ao final do interface `CreateVendaRequest`:

```typescript
  profissionalId?: string
  observacaoOS?: string
```

- [ ] **Step 4: Adicionar `profissionalNome` e `observacaoOS` em `VendaResponse`**

Adicionar ao final do interface `VendaResponse`:

```typescript
  profissionalNome?: string | null
  observacaoOS?: string | null
```

- [ ] **Step 5: Adicionar `profissionalNome` em `VendaListItem`**

Adicionar ao final do interface `VendaListItem`:

```typescript
  profissionalNome?: string | null
```

- [ ] **Step 6: Verificar tipos com tsc**

```bash
cd frontend
npx tsc --noEmit 2>&1 | head -20
```

Esperado: sem erros.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/types/fiscal.ts frontend/src/types/vendas.ts
git commit -m "feat: tipos TS — tipoNegocio em ConfiguracaoEmpresa e campos OS em Venda"
```

---

## Task 5: Frontend — campo "Tipo de negócio" nas Configurações

**Files:**
- Modify: `frontend/src/pages/configuracoes/ConfiguracaoEmpresa.tsx`

- [ ] **Step 1: Adicionar `tipoNegocio` ao estado `ident`**

No componente `ConfiguracaoEmpresa`, o estado `ident` é inicializado com campos como `razaoSocial`, `cnpj`, etc. Adicionar `tipoNegocio`:

```typescript
const [ident, setIdent] = useState({
  razaoSocial: '', nomeFantasia: '', cnpj: '',
  inscricaoEstadual: '', inscricaoMunicipal: '',
  telefone: '', email: '',
  tipoNegocio: 'Lojista',
})
```

- [ ] **Step 2: Carregar `tipoNegocio` ao obter configuração**

No `useEffect` (ou `onLoad`) onde o config é carregado para o estado `ident`, adicionar:

```typescript
tipoNegocio: c.tipoNegocio ?? 'Lojista',
```

- [ ] **Step 3: Incluir `tipoNegocio` no request de save**

Na chamada `atualizar({ ... })`, adicionar:

```typescript
tipoNegocio: ident.tipoNegocio,
```

- [ ] **Step 4: Renderizar o select na seção de identificação**

Logo após o campo "Email" na seção de identificação, adicionar:

```tsx
<Field label="Tipo de negócio">
  <select
    value={ident.tipoNegocio}
    onChange={e => setIdent(v => ({ ...v, tipoNegocio: e.target.value }))}
    className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
  >
    <option value="Lojista">Lojista (varejo / produtos)</option>
    <option value="Prestador">Prestador de Serviço</option>
  </select>
</Field>
```

- [ ] **Step 5: Verificar tipos**

```bash
cd frontend
npx tsc --noEmit 2>&1 | grep "ConfiguracaoEmpresa"
```

Esperado: sem erros.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/pages/configuracoes/ConfiguracaoEmpresa.tsx
git commit -m "feat: campo Tipo de Negócio nas configurações da empresa"
```

---

## Task 6: Frontend — `NovaVendaPDV` (renomear) e dispatcher

**Files:**
- Create: `frontend/src/pages/vendas/NovaVendaPDV.tsx`
- Modify: `frontend/src/pages/vendas/NovaVenda.tsx` (substituir por dispatcher)

- [ ] **Step 1: Criar `NovaVendaPDV.tsx` com o conteúdo atual de `NovaVenda.tsx`**

Copiar todo o conteúdo de `NovaVenda.tsx` para o novo arquivo `NovaVendaPDV.tsx`, alterando apenas o nome do componente exportado:

```tsx
// Tudo igual ao NovaVenda.tsx atual, mas a função exportada se chama:
export default function NovaVendaPDV() {
  // ... conteúdo inalterado ...
}
```

- [ ] **Step 2: Substituir `NovaVenda.tsx` pelo dispatcher**

```tsx
import { useEffect, useState } from 'react'
import { useConfiguracaoEmpresa } from '@/hooks/useConfiguracaoEmpresa'
import NovaVendaPDV from './NovaVendaPDV'
import NovaVendaOS from './NovaVendaOS'

export default function NovaVenda() {
  const { obter } = useConfiguracaoEmpresa()
  const [tipoNegocio, setTipoNegocio] = useState<string | null>(null)

  useEffect(() => {
    void obter().then(c => setTipoNegocio(c?.tipoNegocio ?? 'Lojista'))
  }, [obter])

  if (tipoNegocio === null)
    return <div className="flex h-64 items-center justify-center text-muted-foreground">Carregando...</div>

  if (tipoNegocio === 'Prestador')
    return <NovaVendaOS />

  return <NovaVendaPDV />
}
```

- [ ] **Step 3: Verificar tipos**

```bash
cd frontend
npx tsc --noEmit 2>&1 | grep -E "NovaVenda|NovaVendaPDV|NovaVendaOS" | head -10
```

Esperado: apenas erros de `NovaVendaOS` não encontrada (será criada na Task 7).

- [ ] **Step 4: Commit**

```bash
git add frontend/src/pages/vendas/NovaVendaPDV.tsx \
        frontend/src/pages/vendas/NovaVenda.tsx
git commit -m "feat: dispatcher NovaVenda — renderiza PDV ou OS conforme TipoNegocio"
```

---

## Task 7: Frontend — `NovaVendaOS` (tela de Ordem de Serviço)

**Files:**
- Create: `frontend/src/pages/vendas/NovaVendaOS.tsx`

- [ ] **Step 1: Criar `NovaVendaOS.tsx`**

```tsx
import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { CheckCircle2, ArrowLeft, Plus } from 'lucide-react'
import { useVendas } from '@/hooks/useVendas'
import { useClientes } from '@/hooks/useClientes'
import { useProfissionais } from '@/hooks/useProfissionais'
import { useEstoque } from '@/hooks/useEstoque'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ClienteForm from '@/components/clientes/ClienteForm'
import ResumoPedido from '@/components/vendas/ResumoPedido'
import { toast } from '@/hooks/useToast'
import type { ItemCarrinho, CreateVendaRequest, FecharVendaRequest } from '@/types/vendas'
import type { CreateClienteRequest } from '@/types/clientes'

const FORMAS = [
  { value: 'Pix',      label: 'Pix',     emoji: '⚡' },
  { value: 'Dinheiro', label: 'Dinheiro', emoji: '💵' },
  { value: 'Cartao',   label: 'Cartão',   emoji: '💳' },
  { value: 'Outro',    label: 'Outro',    emoji: '🔖' },
] as const

type FormaPagamento = typeof FORMAS[number]['value']

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function NovaVendaOS() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const vendaIdParam = searchParams.get('vendaId')
  const origemParam = searchParams.get('origem')

  const { create, fechar, get } = useVendas()
  const { clientes, list: listClientes, create: createCliente } = useClientes()
  const { profissionais, list: listProfissionais } = useProfissionais()
  const { produtos, listProdutos } = useEstoque()

  const [itens, setItens] = useState<ItemCarrinho[]>([])
  const [desconto, setDesconto] = useState(0)
  const [clienteId, setClienteId] = useState('')
  const [profissionalId, setProfissionalId] = useState('')
  const [observacaoOS, setObservacaoOS] = useState('')
  const [formaPagamento, setFormaPagamento] = useState<FormaPagamento>('Pix')
  const [parcelas, setParcelas] = useState(1)
  const [salvando, setSalvando] = useState(false)
  const [vendaFinalizada, setVendaFinalizada] = useState<{ id: string; total: number } | null>(null)
  const [carregando, setCarregando] = useState(false)
  const [modalNovoCliente, setModalNovoCliente] = useState(false)
  const [busca, setBusca] = useState('')
  const hoje = new Date().toISOString().slice(0, 10)
  const [dataExecucao, setDataExecucao] = useState(hoje)

  // Filtra somente itens do tipo Servico
  const servicos = produtos.filter(p => p.ativo && p.tipo === 'Servico')
  const servicosFiltrados = servicos.filter(p =>
    !busca || p.nome.toLowerCase().includes(busca.toLowerCase()))

  useEffect(() => {
    void listClientes()
    void listProfissionais()
    void listProdutos()
    if (!vendaIdParam) return
    setCarregando(true)
    void get(vendaIdParam).then(venda => {
      setItens(venda.itens.map(i => ({
        produtoId: i.produtoId,
        produtoNome: i.produtoNome,
        precoUnitario: i.precoUnitario,
        quantidade: i.quantidade,
        desconto: 0,
        total: i.precoUnitario * i.quantidade,
      })))
      if (venda.clienteId) setClienteId(venda.clienteId)
      // profissionalNome → pré-seleciona pelo nome se vier da agenda
      // (aguarda profissionais carregarem — veja efeito abaixo)
    }).finally(() => setCarregando(false))
  }, [vendaIdParam, get, listClientes, listProfissionais, listProdutos])

  // Pré-seleciona profissional quando vem de agendamento
  useEffect(() => {
    if (!vendaIdParam || profissionais.length === 0) return
    void get(vendaIdParam).then(venda => {
      if (venda.profissionalNome) {
        const prof = profissionais.find(p => p.nome === venda.profissionalNome)
        if (prof) setProfissionalId(prof.id)
      }
    })
  }, [profissionais, vendaIdParam, get])

  function adicionarItem(produtoId: string) {
    const p = produtos.find(x => x.id === produtoId)
    if (!p) return
    setItens(prev => {
      const existente = prev.find(i => i.produtoId === p.id)
      if (existente)
        return prev.map(i => i.produtoId === p.id
          ? { ...i, quantidade: i.quantidade + 1, total: i.precoUnitario * (i.quantidade + 1) }
          : i)
      return [...prev, {
        produtoId: p.id, produtoNome: p.nome,
        precoUnitario: p.precoVenda, quantidade: 1,
        desconto: 0, total: p.precoVenda,
      }]
    })
    setBusca('')
  }

  async function handleCriarCliente(data: CreateClienteRequest) {
    try {
      const novoCliente = await createCliente(data)
      setClienteId(novoCliente.id)
      setModalNovoCliente(false)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao criar cliente')
    }
  }

  async function confirmarOS() {
    if (itens.length === 0) { toast.error('Adicione ao menos um serviço'); return }
    if (!clienteId) { toast.error('Selecione um cliente'); return }
    setSalvando(true)
    try {
      if (vendaIdParam) {
        const req: FecharVendaRequest = {
          formaPagamento,
          parcelas: formaPagamento === 'Cartao' ? parcelas : undefined,
          observacao: observacaoOS || undefined,
        }
        const result = await fechar(vendaIdParam, req)
        setVendaFinalizada({ id: result.id, total: result.total })
      } else {
        const req: CreateVendaRequest = {
          clienteId,
          itens: itens.map(i => ({ produtoId: i.produtoId, quantidade: i.quantidade, desconto: 0 })),
          desconto,
          formaPagamento,
          parcelas: formaPagamento === 'Cartao' ? parcelas : undefined,
          observacaoOS: observacaoOS || undefined,
          profissionalId: profissionalId || undefined,
          dataHora: dataExecucao !== hoje
            ? new Date(dataExecucao + 'T12:00:00').toISOString()
            : undefined,
        }
        const result = await create(req)
        setVendaFinalizada({ id: result.id, total: result.total })
      }
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao finalizar OS')
    } finally {
      setSalvando(false)
    }
  }

  const subtotal = itens.reduce((s, i) => s + i.precoUnitario * i.quantidade, 0)
  const total = Math.max(0, subtotal - desconto)

  if (carregando)
    return <div className="flex h-64 items-center justify-center text-muted-foreground">Carregando...</div>

  if (vendaFinalizada) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="text-center space-y-5 max-w-sm">
          <CheckCircle2 size={64} className="mx-auto text-green-500" />
          <div>
            <h2 className="text-2xl font-bold">OS finalizada!</h2>
            <p className="text-muted-foreground mt-1">
              Total cobrado: <span className="font-semibold text-foreground">{fmt(vendaFinalizada.total)}</span>
            </p>
          </div>
          <div className="flex flex-col gap-2">
            <Button onClick={() => {
              setItens([]); setDesconto(0); setClienteId('')
              setProfissionalId(''); setObservacaoOS(''); setVendaFinalizada(null)
            }}>
              Nova OS
            </Button>
            <Button variant="outline" onClick={() => navigate('/vendas')}>
              Ver histórico
            </Button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-4 h-full">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate('/vendas')}
          className="text-muted-foreground hover:text-foreground transition-colors">
          <ArrowLeft size={20} />
        </button>
        <h1 className="text-xl font-bold">
          {!vendaIdParam ? 'Nova Ordem de Serviço'
            : origemParam === 'agendamento' ? 'Finalizar Agendamento'
            : 'Finalizar Orçamento'}
        </h1>
      </div>

      {vendaIdParam && (
        <div className="rounded-lg border border-blue-200 bg-blue-50 dark:bg-blue-950/30 dark:border-blue-800 px-4 py-2.5 text-sm text-blue-700 dark:text-blue-300">
          {origemParam === 'agendamento'
            ? 'Venda gerada a partir de agendamento — confirme o pagamento para concluir.'
            : 'Venda gerada a partir de orçamento — confirme o pagamento para concluir.'}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-[1fr_380px] gap-5 items-start">

        {/* Coluna esquerda: seletor de serviços + carrinho */}
        <div className="space-y-4">
          {!vendaIdParam && (
            <div className="rounded-xl border bg-card shadow-sm p-4 space-y-3">
              <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
                Adicionar serviço
              </p>
              <input
                type="text"
                placeholder="Buscar serviço..."
                value={busca}
                onChange={e => setBusca(e.target.value)}
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 text-sm outline-none placeholder:text-muted-foreground"
              />
              {busca && (
                <div className="rounded-md border divide-y max-h-48 overflow-y-auto">
                  {servicosFiltrados.length === 0
                    ? <p className="px-4 py-3 text-sm text-muted-foreground">Nenhum serviço encontrado</p>
                    : servicosFiltrados.map(p => (
                        <button key={p.id} type="button"
                          className="w-full flex items-center justify-between px-4 py-2 text-left hover:bg-accent text-sm"
                          onClick={() => adicionarItem(p.id)}>
                          <span>{p.nome}</span>
                          <span className="text-muted-foreground">{fmt(p.precoVenda)}</span>
                        </button>
                      ))}
                </div>
              )}
            </div>
          )}

          <div className="rounded-xl border bg-card shadow-sm p-4">
            <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-3">
              Serviços
            </p>
            <ResumoPedido
              itens={itens}
              desconto={desconto}
              onChangeQuantidade={(produtoId, quantidade) => {
                if (quantidade <= 0) return
                setItens(prev => prev.map(i => i.produtoId === produtoId
                  ? { ...i, quantidade, total: i.precoUnitario * quantidade }
                  : i))
              }}
              onRemover={id => setItens(prev => prev.filter(i => i.produtoId !== id))}
              onChangeDesconto={setDesconto}
            />
          </div>
        </div>

        {/* Coluna direita: OS + pagamento */}
        <div className="rounded-xl border bg-card shadow-sm p-5 space-y-5 lg:sticky lg:top-4">
          <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
            Ordem de Serviço
          </p>

          {/* Cliente obrigatório */}
          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">Cliente *</Label>
            <div className="flex items-center gap-2">
              <select value={clienteId} onChange={e => setClienteId(e.target.value)}
                className="flex h-9 flex-1 rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
                <option value="">Selecione o cliente...</option>
                {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
              <Button type="button" variant="outline" size="icon"
                title="Criar novo cliente"
                onClick={() => setModalNovoCliente(true)}>
                <Plus size={16} />
              </Button>
            </div>
            {!clienteId && (
              <p className="text-xs text-destructive">Cliente obrigatório</p>
            )}
          </div>

          {/* Profissional */}
          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">Profissional responsável</Label>
            <select value={profissionalId} onChange={e => setProfissionalId(e.target.value)}
              className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
              <option value="">Selecione o profissional...</option>
              {profissionais.filter(p => p.ativo).map(p =>
                <option key={p.id} value={p.id}>{p.nome}</option>
              )}
            </select>
          </div>

          {/* Data de execução */}
          {!vendaIdParam && (
            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground">Data de execução</Label>
              <input
                type="date"
                value={dataExecucao}
                max={hoje}
                onChange={e => setDataExecucao(e.target.value)}
                className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm"
              />
            </div>
          )}

          {/* Observação */}
          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">Observação do serviço (opcional)</Label>
            <textarea
              value={observacaoOS}
              onChange={e => setObservacaoOS(e.target.value)}
              placeholder="Diagnóstico, procedimento realizado..."
              rows={2}
              className="flex w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm resize-none placeholder:text-muted-foreground outline-none focus:ring-1 focus:ring-ring"
            />
          </div>

          <hr className="border-border" />

          {/* Forma de pagamento */}
          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">Forma de pagamento</Label>
            <div className="grid grid-cols-2 gap-2">
              {FORMAS.map(f => (
                <button key={f.value} type="button"
                  onClick={() => setFormaPagamento(f.value)}
                  className={`flex items-center gap-2 rounded-lg border px-3 py-2.5 text-sm font-medium transition-all ${
                    formaPagamento === f.value
                      ? 'border-primary bg-primary/10 text-primary shadow-sm'
                      : 'border-border text-muted-foreground hover:border-muted-foreground hover:text-foreground'
                  }`}>
                  <span>{f.emoji}</span>
                  {f.label}
                </button>
              ))}
            </div>
          </div>

          {formaPagamento === 'Cartao' && (
            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground">Parcelas</Label>
              <select value={parcelas} onChange={e => setParcelas(Number(e.target.value))}
                className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
                {Array.from({ length: 12 }, (_, i) => i + 1).map(n => (
                  <option key={n} value={n}>{n}x {n > 1 ? fmt(total / n) : ''}</option>
                ))}
              </select>
            </div>
          )}

          <div className="rounded-lg bg-primary/5 border border-primary/20 px-4 py-3 flex justify-between items-center">
            <span className="text-sm font-medium text-muted-foreground">Total</span>
            <span className="text-3xl font-bold text-primary tabular-nums">{fmt(total)}</span>
          </div>

          <Button
            className="w-full h-12 text-base font-semibold"
            disabled={itens.length === 0 || !clienteId || salvando}
            onClick={confirmarOS}>
            {salvando ? 'Finalizando...' : '✓ Finalizar OS'}
          </Button>

          {itens.length === 0 && (
            <p className="text-xs text-center text-muted-foreground">
              Adicione ao menos um serviço para finalizar
            </p>
          )}
        </div>
      </div>

      <Dialog open={modalNovoCliente} onOpenChange={setModalNovoCliente}>
        <DialogContent>
          <DialogHeader><DialogTitle>Novo Cliente</DialogTitle></DialogHeader>
          <ClienteForm
            onSubmit={handleCriarCliente}
            onCancel={() => setModalNovoCliente(false)}
          />
        </DialogContent>
      </Dialog>
    </div>
  )
}
```

- [ ] **Step 2: Verificar tipos**

```bash
cd frontend
npx tsc --noEmit 2>&1 | grep "NovaVendaOS" | head -10
```

Esperado: sem erros.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/vendas/NovaVendaOS.tsx
git commit -m "feat: tela NovaVendaOS — Ordem de Serviço com cliente, profissional e serviços"
```

---

## Task 8: Frontend — histórico com coluna Profissional

**Files:**
- Modify: `frontend/src/pages/vendas/Historico.tsx`

- [ ] **Step 1: Importar `useConfiguracaoEmpresa` e adicionar estado**

No topo do arquivo, adicionar o import:

```typescript
import { useConfiguracaoEmpresa } from '@/hooks/useConfiguracaoEmpresa'
```

Dentro do componente `Historico`, adicionar:

```typescript
const { obter } = useConfiguracaoEmpresa()
const [tipoNegocio, setTipoNegocio] = useState('Lojista')

useEffect(() => {
  void obter().then(c => setTipoNegocio(c?.tipoNegocio ?? 'Lojista'))
}, [obter])

const isPrestador = tipoNegocio === 'Prestador'
```

- [ ] **Step 2: Adicionar coluna Profissional na tabela desktop**

No `<thead>`, após `<th ... >Cliente</th>`, adicionar condicionalmente:

```tsx
{isPrestador && <th className="px-4 py-3 text-left font-medium">Profissional</th>}
```

Nas linhas `<tbody>`, após `<td ... >{v.clienteNome ?? 'Balcão'}</td>`, adicionar:

```tsx
{isPrestador && (
  <td className="px-4 py-3 text-muted-foreground">
    {v.profissionalNome ?? '—'}
  </td>
)}
```

- [ ] **Step 3: Adicionar linha Profissional nos cards mobile**

Nos cards mobile (`<div className="md:hidden ..."`), após o elemento que exibe `clienteNome`, adicionar:

```tsx
{isPrestador && v.profissionalNome && (
  <p className="text-xs text-muted-foreground">
    Profissional: {v.profissionalNome}
  </p>
)}
```

- [ ] **Step 4: Verificar tipos**

```bash
cd frontend
npx tsc --noEmit 2>&1 | grep "Historico" | head -5
```

Esperado: sem erros.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/vendas/Historico.tsx
git commit -m "feat: histórico de vendas exibe coluna Profissional para prestadores de serviço"
```

---

## Task 9: Push e verificação final

- [ ] **Step 1: Verificar build completo do frontend**

```bash
cd frontend
npx tsc --noEmit 2>&1
```

Esperado: sem erros de tipo.

- [ ] **Step 2: Verificar build do backend**

```bash
cd backend/src/GestorAI.API
dotnet build
```

Esperado: `Build succeeded.`

- [ ] **Step 3: Push da branch**

```bash
git push origin feature/ajustes-ux-junho
```

- [ ] **Step 4: Testar manualmente o fluxo Prestador**

1. Ir em Configurações → mudar "Tipo de negócio" para "Prestador de Serviço" → Salvar
2. Navegar para `/vendas/nova` → deve renderizar "Nova Ordem de Serviço"
3. Tentar finalizar sem cliente → botão desabilitado + mensagem
4. Selecionar cliente + profissional + serviço → finalizar → tela de sucesso
5. Verificar Histórico → coluna "Profissional" visível com o nome correto
6. Concluir um agendamento → tela de OS com itens pré-carregados
7. Voltar às configurações → mudar para "Lojista" → `/vendas/nova` volta ao PDV
