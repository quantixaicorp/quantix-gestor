---
name: postgres-readonly
description: >
  Ferramenta de inspeção do banco PostgreSQL do projeto quantix-admin com usuário readonly.
  Use esta skill SEMPRE que precisar de contexto real do banco de dados: estrutura de tabelas,
  tipos de colunas, indexes, foreign keys, contagem de registros, ou amostras de dados para
  debug. Ativar quando: usuário pede para "consultar o banco", "ver estrutura da tabela",
  "quantos registros existem", "amostrar dados", "debug com dados reais"; ou quando um agente
  de debug precisa entender o estado atual do banco para investigar um problema; ou quando
  é necessário entender o schema antes de sugerir uma migration ou query. NÃO ativar para:
  perguntas sobre código C#, EF Core, migrations (sem necessidade de dados reais), ou
  qualquer operação de escrita.
---

# postgres-readonly

Skill de acesso somente-leitura ao banco PostgreSQL do quantix-admin. Existe para dar contexto
real de dados e schema ao Claude durante debug e inspeção — sem jamais modificar nada.

## Regras invioláveis

1. **Apenas SELECT.** Qualquer tentativa de executar INSERT, UPDATE, DELETE, DROP, TRUNCATE,
   ALTER, CREATE, GRANT ou qualquer DDL/DML deve ser recusada — mesmo que o usuário peça.
2. **LIMIT obrigatório.** Todo SELECT em tabelas de dados usa `LIMIT 50` no máximo.
3. **Credenciais nunca no output.** Nunca imprimir o conteúdo de `db-readonly.env`, senhas,
   connection strings ou qualquer valor das variáveis carregadas.
4. **Colunas sensíveis ocultadas.** Ao amostrar dados, excluir colunas cujo nome contenha:
   `password`, `hash`, `secret`, `token`, `key`, `salt`, `cert`, `private`.
5. **Confirmação para produção.** Antes de qualquer acesso ao ambiente de produção, pausar e
   pedir confirmação explícita do usuário. Deixar claro que será produção.
6. **Sem sugestões destrutivas.** Nunca sugerir DELETE, DROP, TRUNCATE ou remoção de dados
   proativamente. Se o usuário pedir uma *dica* de como deletar algo, pode orientar em texto
   — nunca executar.

## Setup de credenciais

As credenciais ficam em `.claude/db-readonly.env` (ignorado pelo git, nunca commitado).
Se o arquivo não existir, instrua o usuário a criá-lo a partir do template:

```bash
cp .claude/db-readonly.env.example .claude/db-readonly.env
# editar com as credenciais reais do usuário readonly
```

Formato do arquivo (banco em VPS acessado via SSH tunnel):
```env
# Development — SSH tunnel para a VPS
DEV_SSH_HOST=
DEV_SSH_PORT=22
DEV_SSH_USER=
DEV_SSH_KEY=~/.ssh/id_rsa
DEV_SSH_TUNNEL_PORT=5433

DEV_DB_HOST=127.0.0.1
DEV_DB_PORT=5432
DEV_DB_NAME=quantix_db
DEV_DB_USER=quantix_readonly
DEV_DB_PASS=

# Production — SSH tunnel para a VPS de produção
PROD_SSH_HOST=
PROD_SSH_PORT=22
PROD_SSH_USER=
PROD_SSH_KEY=~/.ssh/id_rsa
PROD_SSH_TUNNEL_PORT=5434

PROD_DB_HOST=127.0.0.1
PROD_DB_PORT=5432
PROD_DB_NAME=quantix_db
PROD_DB_USER=quantix_readonly
PROD_DB_PASS=
```

`DEV_DB_HOST` e `PROD_DB_HOST` são o endereço do PostgreSQL **visto de dentro da VPS**
(normalmente `127.0.0.1`). O tunnel mapeia `localhost:DEV_SSH_TUNNEL_PORT` na máquina
local para `DEV_DB_HOST:DEV_DB_PORT` dentro da VPS.

## Seleção de ambiente

Se o contexto não tornar óbvio qual ambiente usar, perguntar ao usuário antes de executar:
- **Development** — banco de dev via SSH tunnel, acesso livre para inspeção
- **Production** — requer confirmação explícita do usuário, usar com moderação

## Como executar queries

O banco fica em VPS remota. O fluxo é: **abrir SSH tunnel → rodar psql via tunnel → fechar tunnel**.

```bash
# 1. Carregar credenciais (senha nunca aparece em ps aux)
set -a
source .claude/db-readonly.env
set +a

# 2. Abrir SSH tunnel em background (exemplo: Development)
ssh -f -N \
  -L "${DEV_SSH_TUNNEL_PORT}:${DEV_DB_HOST}:${DEV_DB_PORT}" \
  -i "${DEV_SSH_KEY}" \
  -p "${DEV_SSH_PORT}" \
  -o StrictHostKeyChecking=no \
  -o ExitOnForwardFailure=yes \
  "${DEV_SSH_USER}@${DEV_SSH_HOST}"

# Guardar PID do tunnel para fechar depois
TUNNEL_PID=$(pgrep -f "ssh.*${DEV_SSH_TUNNEL_PORT}:${DEV_DB_HOST}:${DEV_DB_PORT}")

# 3. Executar query (conecta em localhost via tunnel)
PGPASSWORD="$DEV_DB_PASS" psql \
  -h 127.0.0.1 \
  -p "$DEV_SSH_TUNNEL_PORT" \
  -U "$DEV_DB_USER" \
  -d "$DEV_DB_NAME" \
  --no-align -t \
  -c "<query aqui>"

# 4. Fechar tunnel
kill "$TUNNEL_PID" 2>/dev/null
```

Para **Production**, substituir prefixo `DEV_` por `PROD_` em todas as variáveis.

**Checklist antes de executar:**
- Confirmar que `ssh` e `psql` estão disponíveis no PATH
- Confirmar que a chave SSH (`DEV_SSH_KEY`) existe e tem permissão correta (`chmod 600`)
- Se `ssh` pedir confirmação de host fingerprint, o `-o StrictHostKeyChecking=no` evita bloqueio (para ambientes conhecidos e confiáveis)
- Se a porta local já estiver em uso, incrementar `DEV_SSH_TUNNEL_PORT` no `.env`

Se `psql` não estiver no PATH, informar o usuário e sugerir instalação do PostgreSQL client.

## O que consultar

O banco usa dois schemas: `public` e `admin`. Consultar ambos conforme necessário.

Para queries prontas de inspeção de schema e amostragem de dados, ler:
`references/safe-queries.md`

Usar essas queries como ponto de partida. Adaptar conforme o contexto do debug — por exemplo,
adicionar WHERE clause com IDs específicos que o usuário mencionou, ou focar em uma tabela
específica.

## Apresentando resultados

- Formatar output como tabela Markdown quando possível
- Indicar claramente qual ambiente foi consultado (DEV / PROD)
- Indicar o schema e tabela de origem dos dados
- Se resultado estiver vazio, dizer explicitamente — não é erro
- Nunca repetir os valores de credenciais no texto explicativo
