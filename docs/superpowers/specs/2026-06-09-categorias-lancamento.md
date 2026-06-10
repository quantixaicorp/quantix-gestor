# Categorias de Lançamentos — CRUD Gerenciável

## Goal

Substituir as listas de categorias hardcoded no frontend por uma tabela persistida no banco, permitindo que cada empresa gerencie suas próprias categorias de receita e despesa.

## Architecture

Nenhuma mudança estrutural nos lançamentos existentes — `Lancamento.Categoria` permanece `string`. Nova entidade `CategoriaLancamento` armazena os valores permitidos por tipo (Receita/Despesa) e empresa. Criação/edição de lançamentos valida que a categoria informada existe nessa tabela. Exclusão de categoria bloqueada se algum lançamento a referenciar pelo nome.

## Tech Stack

.NET 10 Minimal APIs, EF Core 10, PostgreSQL, React + TypeScript + Tailwind CSS, xUnit

## Dependências

Este spec pressupõe que o `UpdateLancamentoRequest` e a lógica de edição de lançamentos definidos em `docs/superpowers/specs/2026-06-09-melhorias-ux.md` (Task 2) já estejam implementados antes de validar o campo `categoria` no update.

---

## 1. Backend — Entidade e Serviço

### Entidade

`CategoriaLancamento` em `Domain/Entities/CategoriaLancamento.cs`:
```csharp
public class CategoriaLancamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public TipoLancamento Tipo { get; set; }
}
```

`AppDbContext` — adicionar `DbSet<CategoriaLancamento> CategoriasLancamento` com `HasQueryFilter(c => c.EmpresaId == tenantContext.EmpresaId)`.

### Migration

Nova migration `AddCategoriaLancamento`:
- Cria tabela `categorias_lancamento` com índice único em `(empresa_id, tipo, nome)`
- Faz seed das categorias padrão para cada empresa existente:
  - **Receita:** Venda, Serviço, Outros
  - **Despesa:** Aluguel, Fornecedor, Utilidades, Salários, Marketing, Outros

### DTOs

Em `DTOs/Financeiro/CategoriaLancamentoDto.cs`:
```csharp
public record CategoriaLancamentoResponse(Guid Id, string Nome, string Tipo);
public record CreateCategoriaLancamentoRequest(string Nome, string Tipo);
public record UpdateCategoriaLancamentoRequest(string Nome);
```

### Serviço

`CategoriaLancamentoService` em `Services/Financeiro/CategoriaLancamentoService.cs`:

- `ListAsync(string? tipo, CancellationToken ct)` — retorna lista filtrada por tipo (ou tudo)
- `CreateAsync(CreateCategoriaLancamentoRequest req, CancellationToken ct)` — valida nome único por tipo+empresa; lança `AppException(400)` se duplicado
- `UpdateAsync(Guid id, UpdateCategoriaLancamentoRequest req, CancellationToken ct)` — valida unicidade do novo nome; lança `AppException(404)` se não encontrado; ao renomear, executa `UPDATE lancamentos SET categoria = @novoNome WHERE empresa_id = @empresaId AND categoria = @nomeAntigo` via `ExecuteUpdateAsync` para manter consistência dos lançamentos existentes
- `DeleteAsync(Guid id, CancellationToken ct)` — bloqueia com `AppException(400)` se algum `Lancamento.Categoria == categoria.Nome` para aquela empresa; remove e salva

### Validação em Lançamentos

`CreateLancamentoValidator` — regra adicional: a `Categoria` informada deve existir em `CategoriaLancamento` para o `Tipo` do lançamento e a empresa atual. Usar `MustAsync`.

`UpdateLancamentoValidator` (novo, criado no plano melhorias-ux) — mesma regra.

### Endpoints

`CategoriasLancamentoEndpoints.cs` registrado em `WebApplicationExtensions`:
```
GET    /api/categorias-lancamento          ?tipo=Receita (opcional)
POST   /api/categorias-lancamento
PUT    /api/categorias-lancamento/{id:guid}
DELETE /api/categorias-lancamento/{id:guid}
```

---

## 2. Frontend

### Hook

`useCategoriasLancamento.ts`:
- `list(tipo?: string): Promise<CategoriaLancamentoResponse[]>`
- `create(req): Promise<CategoriaLancamentoResponse>`
- `update(id, req): Promise<CategoriaLancamentoResponse>`
- `remove(id): Promise<void>`

### LancamentoForm.tsx — campo categoria dinâmico

Remove as listas hardcoded `categoriasDespesa` e `categoriasReceita`. Substitui por `useCategoriasLancamento`:
- Ao montar o formulário, chama `list(tipo)` com o tipo atual
- Ao trocar o tipo no formulário, recarrega as categorias e limpa a seleção

```typescript
const [categorias, setCategorias] = useState<CategoriaLancamentoResponse[]>([])
useEffect(() => {
  void listCategorias(tipoWatched).then(setCategorias)
}, [tipoWatched, listCategorias])
```

### Página Categorias.tsx

Rota: `/financeiro/categorias`

Layout:
- Header com título "Categorias de Lançamentos" e botão "Nova Categoria"
- Duas abas: **Receita** | **Despesa** (tab ativo filtra a tabela)
- Tabela com colunas: Nome | Ações (editar inline / excluir)
- Editar: botão lápis abre modal mínimo — campo Nome (texto) + botões Salvar/Cancelar
- Excluir: `useConfirm` + `toast.error` se backend retornar 400 (vinculado)
- Nova categoria: modal com dois campos — Nome (text) + Tipo (select Receita/Despesa, pré-selecionado pela aba ativa)

### Rota e Menu

`App.tsx` — adicionar rota `/financeiro/categorias` com `<Categorias />`.

Menu lateral — adicionar link "Categorias" abaixo de "Lançamentos" na seção Financeiro.

### Tipos

`types/financeiro.ts` — adicionar:
```typescript
export interface CategoriaLancamentoResponse {
  id: string
  nome: string
  tipo: 'Receita' | 'Despesa'
}
export interface CreateCategoriaLancamentoRequest {
  nome: string
  tipo: 'Receita' | 'Despesa'
}
export interface UpdateCategoriaLancamentoRequest {
  nome: string
}
```

---

## 3. Testes

`backend/tests/GestorAI.Tests/Services/CategoriaLancamentoServiceTests.cs`:
- `CreateAsync_CriaCategoria_ComNomeUnico`
- `CreateAsync_LancaExcecao_QuandoNomeDuplicadoNoMesmoTipo`
- `UpdateAsync_RenomeiaCom_NomeValido_EAtualizaLancamentosExistentes`
- `DeleteAsync_RemoveCategoria_QuandoSemLancamentosVinculados`
- `DeleteAsync_LancaExcecao_QuandoLancamentoUsaCategoria`

---

## Checklist

- [ ] Entidade `CategoriaLancamento` + `AppDbContext`
- [ ] Migration com seed das categorias padrão
- [ ] `CategoriaLancamentoService` com CRUD completo
- [ ] Validação de categoria em `CreateLancamentoValidator`
- [ ] Validação de categoria em `UpdateLancamentoValidator`
- [ ] Endpoints `GET/POST/PUT/DELETE /api/categorias-lancamento`
- [ ] `useCategoriasLancamento.ts`
- [ ] `LancamentoForm.tsx` com categorias dinâmicas
- [ ] Página `Categorias.tsx` com abas Receita/Despesa
- [ ] Rota e link no menu
- [ ] `dotnet test` full suite passando
- [ ] `npx tsc --noEmit` sem erros
