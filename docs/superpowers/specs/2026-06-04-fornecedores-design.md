# Cadastro de Fornecedores — Design

**Goal:** CRUD completo de fornecedores com lista buscável, cadastro/edição em modal e exclusão.

**Architecture:** Segue o mesmo padrão do módulo Clientes — entidade multi-tenant, Minimal API no backend, lista + modal no frontend. Nenhuma migração de schema quebra outros módulos.

**Tech Stack:** .NET 10 Minimal APIs, EF Core + PostgreSQL, React + TypeScript, React Hook Form + Zod, Tailwind CSS.

---

## Backend

### Entidade

`Domain/Entities/Fornecedor.cs` — implementa `ITenantEntity`:

| Campo | Tipo | Obrigatório |
|---|---|---|
| Id | Guid | sim (PK) |
| EmpresaId | Guid | sim (tenant) |
| Nome | string | sim |
| CnpjCpf | string? | não |
| Telefone | string? | não |
| Email | string? | não |
| Logradouro | string? | não |
| Cidade | string? | não |
| Uf | string? | não |
| Cep | string? | não |
| Contato | string? | não |
| Observacoes | string? | não |
| DataCadastro | DateTime | sim (auto UTC) |

### DTOs

`DTOs/Fornecedores/FornecedorDto.cs`:

- `FornecedorResponse` — todos os campos acima exceto EmpresaId
- `CreateFornecedorRequest` — todos exceto Id, EmpresaId, DataCadastro
- `UpdateFornecedorRequest` — mesmos campos do Create

### Validação

`Services/Fornecedores/CreateFornecedorValidator.cs` (FluentValidation):

- `Nome`: NotEmpty, MaximumLength(200)
- `CnpjCpf`: quando preenchido, 11 ou 14 dígitos (apenas números)
- `Email`: EmailAddress quando preenchido
- `Telefone`: MaximumLength(20)
- `Uf`: MaximumLength(2)
- `Cep`: MaximumLength(9)

### Service

`Services/Fornecedores/FornecedorService.cs`:

- `ListAsync(string? busca, ct)` — filtra por Nome ou CnpjCpf contendo busca, ordena por Nome
- `GetAsync(Guid id, ct)` — 404 se não encontrado
- `CreateAsync(CreateFornecedorRequest, ct)` — seta EmpresaId do TenantContext
- `UpdateAsync(Guid id, UpdateFornecedorRequest, ct)` — 404 se não encontrado
- `DeleteAsync(Guid id, ct)` — 404 se não encontrado; exclui fisicamente

### Endpoints

`Endpoints/FornecedoresEndpoints.cs` — grupo `/api/fornecedores`, `RequireAuthorization()`:

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/fornecedores?busca=` | Lista com busca opcional |
| GET | `/api/fornecedores/{id}` | Detalhe |
| POST | `/api/fornecedores` | Criar (ValidationFilter) |
| PUT | `/api/fornecedores/{id}` | Atualizar |
| DELETE | `/api/fornecedores/{id}` | Excluir |

### AppDbContext

- `DbSet<Fornecedor> Fornecedores`
- Query filter: `e.EmpresaId == tenantContext.EmpresaId`
- Índice único em `(EmpresaId, CnpjCpf)` com filtro `WHERE "CnpjCpf" IS NOT NULL`

### Program.cs

```csharp
builder.Services.AddScoped<FornecedorService>();
builder.Services.AddScoped<IValidator<CreateFornecedorRequest>, CreateFornecedorValidator>();
app.MapFornecedores();
```

---

## Frontend

### Tipos

`types/fornecedores.ts` — `FornecedorResponse`, `CreateFornecedorRequest`, `UpdateFornecedorRequest`

### Hook

`hooks/useFornecedores.ts`:

- `list(busca?)` — GET `/api/fornecedores?busca=`
- `create(req)` — POST, atualiza estado local
- `update(id, req)` — PUT, atualiza estado local
- `remove(id)` — DELETE, remove do estado local

### Formulário

`components/fornecedores/FornecedorForm.tsx` — React Hook Form + Zod:

- **Seção Dados básicos:** Nome*, CNPJ/CPF, Telefone, E-mail, Contato
- **Seção Endereço:** Logradouro, Cidade, UF, CEP
- Observações (textarea)
- Botões Cancelar / Salvar

### Página

`pages/fornecedores/Fornecedores.tsx`:

- Cabeçalho "Fornecedores" + botão "Novo Fornecedor"
- Busca client-side por Nome ou CNPJ/CPF
- Tabela: Nome | CNPJ/CPF | Telefone | Cidade/UF | Contato | (ações)
- Coluna de ações: botões Editar e Excluir (com confirmação)
- Modal de criação e modal de edição (reutiliza `FornecedorForm`)
- Estado vazio: "Nenhum fornecedor cadastrado"

### Roteamento

`router/index.tsx` — rota `/fornecedores` dentro de `<AppLayout>`

### Sidebar

Novo grupo **"Compras"** com item:

- Ícone `Truck` (Lucide)
- Label "Fornecedores"
- Path `/fornecedores`

---

## Testes

`backend/tests/GestorAI.Tests/Services/FornecedorServiceTests.cs`:

1. `ListAsync_RetornaApenasDoTenant`
2. `CreateAsync_SalvaComEmpresaId`
3. `UpdateAsync_Lanca404_QuandoNaoEncontrado`
4. `DeleteAsync_RemoveFornecedor`
5. `DeleteAsync_Lanca404_QuandoNaoEncontrado`

---

## Migrações

Uma migration EF Core: `AddFornecedores` — cria tabela `Fornecedores` com índice único condicional em CnpjCpf.
