# Safe Queries — postgres-readonly

Queries de referência para inspeção do banco quantix-admin. Todas são SELECT puro.
O banco usa dois schemas: `public` e `admin`. Substituir `<schema>` conforme necessário.

---

## Schema inspection

### Listar todas as tabelas (ambos os schemas)

```sql
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema IN ('public', 'admin')
  AND table_type = 'BASE TABLE'
ORDER BY table_schema, table_name;
```

### Estrutura de uma tabela (colunas, tipos, nullable, default)

```sql
SELECT
  column_name,
  data_type,
  character_maximum_length,
  is_nullable,
  column_default
FROM information_schema.columns
WHERE table_schema = '<schema>'
  AND table_name   = '<tabela>'
ORDER BY ordinal_position;
```

### Estrutura de todas as tabelas (ambos os schemas)

```sql
SELECT
  table_schema,
  table_name,
  column_name,
  data_type,
  is_nullable,
  column_default
FROM information_schema.columns
WHERE table_schema IN ('public', 'admin')
ORDER BY table_schema, table_name, ordinal_position;
```

### Indexes de uma tabela

```sql
SELECT
  indexname,
  indexdef
FROM pg_indexes
WHERE schemaname = '<schema>'
  AND tablename  = '<tabela>'
ORDER BY indexname;
```

### Todos os indexes (ambos os schemas)

```sql
SELECT schemaname, tablename, indexname, indexdef
FROM pg_indexes
WHERE schemaname IN ('public', 'admin')
ORDER BY schemaname, tablename, indexname;
```

### Foreign keys

```sql
SELECT
  tc.table_schema,
  tc.table_name,
  kcu.column_name,
  ccu.table_schema AS foreign_schema,
  ccu.table_name   AS foreign_table,
  ccu.column_name  AS foreign_column,
  tc.constraint_name
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu
  ON tc.constraint_name = kcu.constraint_name
 AND tc.table_schema    = kcu.table_schema
JOIN information_schema.constraint_column_usage ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema IN ('public', 'admin')
ORDER BY tc.table_schema, tc.table_name;
```

### Constraints de uma tabela

```sql
SELECT
  constraint_name,
  constraint_type
FROM information_schema.table_constraints
WHERE table_schema = '<schema>'
  AND table_name   = '<tabela>'
ORDER BY constraint_type, constraint_name;
```

---

## Row counts

### Contagem de todas as tabelas (ambos os schemas)

```sql
SELECT
  schemaname,
  relname AS table_name,
  n_live_tup AS estimated_rows
FROM pg_stat_user_tables
WHERE schemaname IN ('public', 'admin')
ORDER BY schemaname, n_live_tup DESC;
```

### Contagem exata de uma tabela específica

```sql
SELECT COUNT(*) FROM <schema>.<tabela>;
```

---

## Data sampling

### Amostra de dados (substituir colunas sensíveis por '[hidden]' ou excluí-las)

```sql
SELECT *
FROM <schema>.<tabela>
LIMIT 20;
```

> Antes de executar, verificar quais colunas existem (via estrutura acima) e excluir
> aquelas com nomes contendo: password, hash, secret, token, key, salt, cert, private.

### Amostra com filtro por ID

```sql
SELECT *
FROM <schema>.<tabela>
WHERE id = '<uuid>'
LIMIT 1;
```

### Amostra com filtro por company (multi-tenant)

```sql
SELECT *
FROM <schema>.<tabela>
WHERE company_id = '<uuid>'
LIMIT 20;
```

### Amostra recente (por timestamptz)

```sql
SELECT *
FROM <schema>.<tabela>
ORDER BY created_at DESC
LIMIT 20;
```

---

## Diagnóstico

### Tamanho das tabelas

```sql
SELECT
  schemaname,
  tablename,
  pg_size_pretty(pg_total_relation_size(schemaname || '.' || tablename)) AS total_size,
  pg_size_pretty(pg_relation_size(schemaname || '.' || tablename))        AS table_size,
  pg_size_pretty(
    pg_total_relation_size(schemaname || '.' || tablename) -
    pg_relation_size(schemaname || '.' || tablename)
  ) AS index_size
FROM pg_tables
WHERE schemaname IN ('public', 'admin')
ORDER BY pg_total_relation_size(schemaname || '.' || tablename) DESC;
```

### Conexões ativas (sem credenciais)

```sql
SELECT
  datname,
  usename,
  application_name,
  client_addr,
  state,
  wait_event_type,
  wait_event,
  LEFT(query, 100) AS query_snippet
FROM pg_stat_activity
WHERE datname = current_database()
ORDER BY state, usename;
```

### Versão do PostgreSQL

```sql
SELECT version();
```

### Schemas disponíveis

```sql
SELECT schema_name
FROM information_schema.schemata
ORDER BY schema_name;
```
