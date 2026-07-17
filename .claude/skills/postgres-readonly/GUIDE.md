# postgres-readonly — Guia de Uso

Skill para inspecionar o banco PostgreSQL do quantix-admin diretamente no Claude Code.
Acesso somente-leitura via SSH tunnel + Docker. Nunca modifica dados.

---

## Pré-requisitos

### Ferramentas necessárias

| Ferramenta | Uso | Como instalar |
|---|---|---|
| `ssh` | Abrir tunnel até a VPS | Incluído no Windows 10/11 (OpenSSH) |
| `Docker Desktop` | Rodar `psql` sem instalar PostgreSQL | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop) |

> `psql` **não precisa ser instalado** localmente — a skill usa a imagem `postgres:16` do Docker automaticamente.

### Chave SSH

A autenticação na VPS é feita via chave SSH (não senha). Gere uma chave dedicada:

```powershell
ssh-keygen -t ed25519 -f $env:USERPROFILE\.ssh\id_ed25519_quantix_prod
```

Copie a chave pública para o servidor:

```powershell
# Copia o arquivo pub para a VPS (vai pedir senha uma vez)
scp -P <PORTA> $env:USERPROFILE\.ssh\id_ed25519_quantix_prod.pub usuario@servidor:/tmp/new_key.pub

# Adiciona ao authorized_keys
ssh -p <PORTA> usuario@servidor "cat /tmp/new_key.pub >> ~/.ssh/authorized_keys && rm /tmp/new_key.pub"
```

---

## Configuração das credenciais

Crie o arquivo de credenciais a partir do template:

```bash
cp .claude/db-readonly.env.example .claude/db-readonly.env
```

Edite `.claude/db-readonly.env` com as informações dos dois ambientes:

```
DEV_SSH_HOST=        # IP ou hostname da VPS de desenvolvimento
DEV_SSH_PORT=        # Porta SSH (padrão: 22)
DEV_SSH_USER=        # Usuário SSH
DEV_SSH_KEY=         # Caminho da chave privada (ex: ~/.ssh/id_ed25519_quantix_dev)
DEV_SSH_TUNNEL_PORT= # Porta local para o tunnel (ex: 5433)

DEV_DB_HOST=         # Endereço do PostgreSQL visto de dentro da VPS (normalmente 127.0.0.1)
DEV_DB_PORT=         # Porta do PostgreSQL (padrão: 5432)
DEV_DB_NAME=         # Nome do banco
DEV_DB_USER=         # Usuário readonly do banco
DEV_DB_PASS=         # Senha do usuário readonly

# Mesmo formato para PROD_, prefixo PROD_ em vez de DEV_
PROD_SSH_HOST=
...
PROD_SSH_TUNNEL_PORT= # Use porta diferente do DEV (ex: 5434)
```

> O arquivo `.claude/db-readonly.env` está no `.gitignore` — nunca é commitado.

---

## Como usar

Invoque a skill pelo Claude Code com o comando:

```
/postgres-readonly <sua pergunta ou instrução>
```

### Exemplos

```
/postgres-readonly amostre 10 registros da tabela users do schema admin
```

```
/postgres-readonly mostre a estrutura da tabela companies com colunas e tipos
```

```
/postgres-readonly quantos usuários existem por empresa no banco de produção
```

```
/postgres-readonly liste os indexes da tabela policies
```

```
/postgres-readonly debug: o usuário X existe no banco? mostre seus dados
```

---

## O que a skill faz (e não faz)

### Pode fazer

- `SELECT` em qualquer tabela dos schemas `public` e `admin`
- Inspecionar estrutura de tabelas (colunas, tipos, indexes, foreign keys)
- Contar registros, agrupar, filtrar por condições
- Amostrar dados para debug (máximo 50 linhas por query)
- Comparar estado real do banco com o que o código espera

### Não pode fazer

- `INSERT`, `UPDATE`, `DELETE`, `DROP`, `TRUNCATE`, `ALTER` — bloqueado por regra da skill
- Exibir valores de colunas sensíveis (`password`, `hash`, `secret`, `token`, `key`, `salt`, `cert`, `private`)
- Acessar produção sem confirmação explícita do usuário
- Expor credenciais, connection strings ou senhas no output

---

## Ambientes

| Ambiente | Quando usar |
|---|---|
| **Development** | Inspeção livre, debug do dia a dia |
| **Production** | Requer confirmação explícita — usar com moderação |

Se não especificar qual ambiente, a skill pergunta antes de executar.

---

## Fluxo interno

```
Claude recebe instrução
  → Carrega .claude/db-readonly.env
  → Abre SSH tunnel (localhost:TUNNEL_PORT → DB dentro da VPS)
  → Roda psql via Docker (host.docker.internal:TUNNEL_PORT)
  → Exibe resultado formatado como tabela Markdown
  → Fecha o tunnel
```

---

## Troubleshooting

| Problema | Causa provável | Solução |
|---|---|---|
| `Permission denied (publickey)` | Chave pública não está em `authorized_keys` | Re-copiar chave via `scp` e verificar conteúdo |
| `ExitOnForwardFailure` | Porta local já em uso | Trocar `TUNNEL_PORT` no `.env` |
| `psql: could not connect` | Tunnel não abriu ou DB_HOST errado | Confirmar que `DB_HOST` é o endereço do PostgreSQL visto de dentro da VPS |
| Chave truncada em `authorized_keys` | Append sem newline em linha existente | Sobrescrever com `scp pub_key usuario@servidor:~/.ssh/authorized_keys` |
| `docker: command not found` | Docker Desktop não instalado ou não iniciado | Instalar Docker Desktop e garantir que está rodando |
