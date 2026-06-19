# Design: Parametrização de Vendas — Lojista vs Prestador de Serviço

**Data:** 2026-06-19
**Status:** Aprovado

---

## Problema

A tela de vendas do GestorAI (`/vendas/nova`) é um PDV orientado a produto (lojista). Clientes prestadores de serviço (salões, clínicas, oficinas, etc.) precisam de um fluxo de **Ordem de Serviço (OS)** com campos específicos: cliente obrigatório, profissional responsável, observação do serviço prestado e itens filtrados apenas por serviços do catálogo.

---

## Decisões de design

| Pergunta | Decisão |
|---|---|
| Como configurar o tipo de negócio? | Campo em `ConfiguracaoEmpresa` (admin configura uma vez) |
| Como é o fluxo do prestador? | Ordem de Serviço com cliente obrigatório, profissional e observação |
| Quem preenche o campo profissional? | Lista de `Profissional` do módulo de agendamentos |
| Cliente é obrigatório? | Sim, com criação rápida embutida (botão "+") |
| E o fluxo de agendamento? | OS substitui a tela de finalização — profissional e cliente pré-preenchidos |
| Histórico muda? | Sim — colunas extras (Profissional) para prestadores |
| Arquitetura frontend? | Dispatcher em `/vendas/nova` — renderiza PDV ou OS conforme tipo |

---

## Modelo de dados

### `ConfiguracaoEmpresa` (nova coluna)

```csharp
public string TipoNegocio { get; set; } = "Lojista"; // "Lojista" | "Prestador"
```

Migration EF Core. Valor default `"Lojista"` — nenhuma empresa existente quebra.

### `Venda` (três novas colunas opcionais)

```csharp
public Guid?   ProfissionalId   { get; set; }  // FK → Profissional (nullable)
public string? ProfissionalNome { get; set; }  // desnormalizado para histórico
public string? ObservacaoOS     { get; set; }  // descrição do serviço prestado
```

Todas nullable — vendas PDV existentes não são afetadas.

---

## Backend API

### `ConfiguracaoEmpresa`
- `AtualizarConfiguracaoEmpresaRequest` recebe `string? TipoNegocio`
- `ConfiguracaoEmpresaResponse` expõe `string TipoNegocio`
- Endpoint existente `PUT /api/configuracao-empresa` — sem endpoint novo

### `Venda`
- `CreateVendaRequest` recebe `Guid? ProfissionalId` e `string? ObservacaoOS`
- `VendaService.CreateAsync` resolve `ProfissionalNome` a partir de `ProfissionalId`
- `VendaResponse` expõe `string? ProfissionalNome` e `string? ObservacaoOS`
- Endpoint existente `POST /api/vendas` — sem endpoint novo

---

## Arquitetura frontend

### Estrutura de arquivos

```
pages/vendas/
  NovaVenda.tsx       ← dispatcher (substitui atual)
  NovaVendaPDV.tsx    ← atual NovaVenda.tsx renomeado (sem mudanças)
  NovaVendaOS.tsx     ← nova tela de Ordem de Serviço
  Historico.tsx       ← estende com coluna Profissional para prestadores
```

### Dispatcher (`NovaVenda.tsx`)

```
1. Chama GET /api/configuracao-empresa
2. Se tipoNegocio === "Prestador" → renderiza <NovaVendaOS />
3. Se tipoNegocio === "Lojista"   → renderiza <NovaVendaPDV />
```

`DetalheAgendamento.tsx` **não muda** — já navega para `/vendas/nova?vendaId=…&origem=agendamento`. O dispatcher passa os search params para o componente correto.

---

## Tela `NovaVendaOS`

Layout em duas colunas (igual ao PDV):

**Coluna esquerda — Serviços:**
- Seletor de itens filtrado por `tipo === "Servico"`
- Carrinho com quantidade, remoção e desconto

**Coluna direita — OS + Pagamento:**

| Campo | Detalhe |
|---|---|
| Cliente * | Select com busca + botão "+" para criação rápida |
| Profissional * | Select com profissionais ativos |
| Data de execução | Date picker, default hoje |
| Observação | Textarea opcional (diagnóstico, procedimento) |
| Forma de pagamento | Pix / Dinheiro / Cartão / Outro |
| Parcelas | Só exibido quando forma = Cartão |
| Total | Valor em destaque |
| Botão | "✓ Finalizar OS" |

**Quando vem de agendamento** (`?vendaId=…&origem=agendamento`):
- Itens pré-carregados (igual ao PDV atual via `GET /api/vendas/:id`)
- Cliente pré-selecionado a partir do payload da venda
- Profissional pré-selecionado a partir do agendamento
- Banner azul: "Venda gerada a partir de agendamento — confirme o pagamento para concluir."

**Tela de sucesso:** "OS finalizada!", total, botões "Nova OS" e "Ver histórico".

---

## Histórico de Vendas (`Historico.tsx`)

O histórico detecta `tipoNegocio` via hook de configuração:

**Para `Prestador`:**
- Desktop: colunas `Data | Cliente | Profissional | Pagamento | Total | Status | Ações`
- Mobile: linha extra "Profissional: João Silva" nos cards (quando preenchido)

**Para `Lojista`:**
- Layout atual inalterado

---

## Configurações da empresa

A tela `ConfiguracaoEmpresa.tsx` ganha um campo select na seção de dados gerais:

```
Tipo de negócio:  [ Lojista ▾ ]   /   [ Prestador de Serviço ▾ ]
```

Salvo no mesmo request de atualização da empresa.

---

## Escopo fora deste design

- Relatórios por profissional (próxima iteração)
- Dashboard diferenciado por tipo de negócio
- Múltiplos profissionais por OS
- Assinatura digital da OS
