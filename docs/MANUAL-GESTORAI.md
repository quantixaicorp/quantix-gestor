# Manual do Usuário — GestorAI ERP

> Versão 1.0 — Junho 2026

---

## Sumário

1. [Visão Geral](#1-visão-geral)
2. [Acesso ao Sistema](#2-acesso-ao-sistema)
3. [Dashboard](#3-dashboard)
4. [Vendas](#4-vendas)
5. [Orçamentos](#5-orçamentos)
6. [Agendamentos](#6-agendamentos)
7. [Agenda Profissional](#7-agenda-profissional)
8. [Profissionais](#8-profissionais)
9. [Serviços](#9-serviços)
10. [Estoque](#10-estoque)
11. [Financeiro](#11-financeiro)
12. [Clientes](#12-clientes)
13. [Fornecedores](#13-fornecedores)
14. [Relatórios](#14-relatórios)
15. [Fiscal (NF-e)](#15-fiscal-nf-e)
16. [Contratos](#16-contratos)
17. [Cobranças](#17-cobranças)
18. [Agendamento Online (Público)](#18-agendamento-online-público)
19. [Configurações](#19-configurações)
20. [Automação](#20-automação)
21. [Plano de Assinatura](#21-plano-de-assinatura)
22. [Integrações](#22-integrações)

---

## 1. Visão Geral

O **GestorAI** é um ERP multi-módulo desenvolvido para negócios de serviços e varejo, como barbearias, salões, clínicas de estética, consultórios e prestadores de serviço em geral. O sistema centraliza vendas, agendamentos, financeiro, estoque, contratos, cobranças e notas fiscais em uma única plataforma.

### Principais recursos

- Vendas com controle de estoque automático
- Orçamentos com link de aprovação para o cliente
- Agenda de profissionais com página pública de agendamento
- Controle financeiro completo (contas a pagar, a receber, fluxo de caixa)
- Contratos recorrentes com geração automática de cobranças
- Cobranças via PIX/boleto integradas ao Asaas
- Assinatura digital de contratos via ClickSign
- Emissão de NF-e/NFS-e via Focus.nfe
- Automação de mensagens via WhatsApp (Evolution API)
- Página pública de agendamento online com link personalizável

---

## 2. Acesso ao Sistema

### Login

Acesse o sistema pela URL fornecida pela QuantixAI. Na tela de login, informe seu **e-mail** e **senha** cadastrados. Após autenticação, você será redirecionado ao Dashboard.

### Navegação

O menu lateral (sidebar) organiza todos os módulos em grupos:

| Grupo | Módulos disponíveis |
|---|---|
| Geral | Dashboard |
| Vendas | Nova Venda, Histórico, Orçamentos |
| Agenda | Agenda Geral, Agendamentos, Profissionais, Serviços |
| Estoque | Produtos, Movimentações |
| Financeiro | Lançamentos, Contas a Pagar, Contas a Receber, Categorias |
| Clientes / Relatórios | Clientes, Relatórios |
| Compras | Fornecedores |
| Fiscal | Notas Fiscais |
| Contratos | Contratos, Templates, Cobranças |
| Configurações | Agendamento Online, Integrações, Automação, Plano |
| Automação | Configurações, Log de Envios |

O menu pode ser **recolhido** clicando no ícone de painel no topo da sidebar (apenas em desktop). No celular, o menu abre como gaveta lateral pelo ícone de hambúrguer no topo da tela.

### Tema claro / escuro

Clique no ícone de sol/lua no topo da sidebar para alternar entre os temas.

---

## 3. Dashboard

O Dashboard é a tela inicial do sistema. Exibe um resumo em tempo real do negócio.

### Indicadores exibidos

- **Receitas do mês** — total de receitas pagas no mês corrente
- **Despesas do mês** — total de despesas pagas no mês corrente
- **Saldo** — diferença entre receitas e despesas
- **Vendas do dia** — quantidade e valor das vendas do dia
- **Agendamentos do dia** — lista dos agendamentos do dia atual com horário, cliente e profissional
- **Estoque baixo** — produtos com estoque abaixo do mínimo definido
- **Cobranças vencidas** — cobranças em atraso

O Dashboard é atualizado automaticamente ao carregar a página.

---

## 4. Vendas

### 4.1 Nova Venda

Caminho: **Vendas → Nova Venda**

Para registrar uma venda:

1. Clique em **Nova Venda**
2. Selecione o **cliente** (opcional — pode deixar em branco para venda avulsa)
3. Adicione os itens: busque um produto ou serviço pelo nome
   - Para **produtos físicos**, o sistema verifica se há estoque disponível antes de permitir a venda
   - Para **serviços**, não há controle de estoque
4. Ajuste as **quantidades** e aplique **desconto** se necessário
5. Selecione a **forma de pagamento**: Dinheiro, PIX, Cartão ou Outro
6. Se desejar, informe o número de **parcelas**
7. Clique em **Finalizar Venda**

Ao finalizar uma venda:
- O estoque dos produtos é **baixado automaticamente**
- Um **lançamento financeiro** (receita) é criado automaticamente
- A venda fica com status **Concluída**

### 4.2 Histórico de Vendas

Caminho: **Vendas → Histórico**

Lista todas as vendas com filtros por:
- **Período** (data de início e fim)
- **Status** (Aberta, Concluída, Cancelada)

Clique em uma venda para ver o detalhamento completo.

### Cancelamento de Venda

No detalhe da venda, clique em **Cancelar**. O estoque dos itens é **devolvido automaticamente**.

---

## 5. Orçamentos

O módulo de orçamentos permite criar propostas comerciais e enviá-las para aprovação do cliente via link.

### 5.1 Criar Orçamento

Caminho: **Vendas → Orçamentos → Novo Orçamento**

1. Selecione o **cliente**
2. Defina a **data de validade**
3. Adicione itens (produtos ou serviços com quantidade e valor unitário)
4. Adicione **observações** se necessário
5. Clique em **Salvar**

O orçamento é criado com status **Rascunho**.

### 5.2 Ciclo de vida do Orçamento

| Status | Descrição |
|---|---|
| Rascunho | Criado, ainda não enviado |
| Enviado | Link público gerado e enviado ao cliente |
| Aprovado | Cliente aprovou via link ou internamente |
| Rejeitado | Cliente rejeitou |
| Convertido | Convertido em venda |
| Cancelado | Cancelado manualmente |
| Expirado | Data de validade passou sem aprovação |

### 5.3 Enviar para o Cliente

No detalhe do orçamento, clique em **Enviar**. O sistema gera um **link público único** que pode ser copiado e enviado ao cliente por WhatsApp ou e-mail. O cliente acessa o link e pode **aprovar ou rejeitar** o orçamento sem precisar de login.

### 5.4 Converter em Venda

Após o orçamento ser aprovado, clique em **Converter em Venda**. O sistema cria a venda automaticamente com todos os itens do orçamento, baixa o estoque e gera o lançamento financeiro.

### 5.5 Gerar Cobrança

No detalhe do orçamento aprovado, clique em **Gerar Cobrança** para criar uma cobrança vinculada ao orçamento (útil quando a venda é por serviço e não produto).

### 5.6 PDF

Clique em **PDF** para baixar o orçamento em formato imprimível.

---

## 6. Agendamentos

### 6.1 Lista de Agendamentos

Caminho: **Agenda → Agendamentos**

Exibe os agendamentos do dia selecionado. Use as setas para navegar entre os dias.

### 6.2 Novo Agendamento

Caminho: **Agenda → Agendamentos → Novo Agendamento**

1. Selecione o **profissional**
2. Selecione o **serviço**
3. Selecione a **data e horário** disponível
4. Informe o **nome e telefone do cliente** (ou selecione um cliente cadastrado)
5. Adicione **observações** se necessário
6. Clique em **Salvar**

### 6.3 Ciclo de vida do Agendamento

| Status | Descrição |
|---|---|
| Aguardando Confirmação | Criado pelo cliente online, aguarda confirmação do estabelecimento |
| Agendado | Confirmado pelo sistema |
| Confirmado | Confirmado explicitamente pelo profissional/atendente |
| Concluído | Atendimento realizado |
| Cancelado | Cancelado pelo cliente ou estabelecimento |
| Recusado | Recusado pelo estabelecimento |

### 6.4 Ações no Detalhe do Agendamento

- **Confirmar** — muda status para Confirmado
- **Concluir** — muda status para Concluído
- **Cancelar** — cancela o agendamento
- **Recusar** — recusa o agendamento (para os criados online)

### 6.5 Sinal de Reserva

Se a configuração de sinal de reserva estiver ativa (plano Profissional), o agendamento pode exigir um **pagamento de sinal via PIX** no momento do agendamento online. O valor do sinal é configurado em **Configurações → Agendamento Online**.

---

## 7. Agenda Profissional

Caminho: **Agenda → Agenda Geral**

Exibe uma **visualização semanal** dos agendamentos por profissional. Permite:

- Filtrar por **profissional**
- Navegar pelas semanas
- Ver os slots ocupados e disponíveis em formato de calendário

---

## 8. Profissionais

Caminho: **Agenda → Profissionais**

Cadastro dos profissionais que realizam atendimentos.

### Campos

- **Nome** (obrigatório)
- **Telefone**
- **Ativo** — se desmarcado, o profissional não aparece para agendamentos

### Disponibilidade

Clique no ícone de calendário de um profissional para acessar sua **tela de disponibilidade**.

Na tela de disponibilidade:

1. Defina **períodos de disponibilidade** por faixa de datas, com dias da semana e horários de início/fim
2. Crie **bloqueios de agenda** para datas específicas (folgas, férias, feriados)

O sistema usa essas configurações para calcular os **slots disponíveis** exibidos na página pública de agendamento e na agenda interna.

---

## 9. Serviços

Caminho: **Agenda → Serviços**

Cadastro dos serviços oferecidos pelo estabelecimento.

### Campos

- **Nome** (obrigatório)
- **Preço de venda**
- **Duração em minutos** — usado para calcular os slots na agenda
- **Categoria**
- **Ativo**

Serviços são do tipo "produto sem estoque" no sistema. Eles aparecem tanto na venda quanto no agendamento.

---

## 10. Estoque

### 10.1 Produtos

Caminho: **Estoque → Produtos**

Cadastro dos produtos físicos comercializados.

#### Campos principais

- **Nome** e **código de barras** (opcional)
- **Preço de venda** e **custo médio**
- **Estoque atual** e **estoque mínimo** (alertas no dashboard quando abaixo do mínimo)
- **Categoria**
- **Ativo**

#### Filtros disponíveis

- Busca por nome
- Filtro por categoria
- **"Estoque baixo"** — exibe apenas produtos abaixo do estoque mínimo

### 10.2 Movimentações

Caminho: **Estoque → Movimentações**

Registra entradas e saídas de estoque manualmente.

Origens de movimentação:
- **Manual** — ajuste de inventário
- **Venda** — saída automática ao fechar uma venda
- **Compra** — entrada ao registrar compra de fornecedor

Para registrar uma entrada manual:
1. Selecione o produto
2. Informe a quantidade
3. Selecione o tipo (Entrada ou Saída)
4. Adicione observação se necessário
5. Clique em **Registrar**

---

## 11. Financeiro

### 11.1 Lançamentos

Caminho: **Financeiro → Lançamentos**

Visão completa de todas as movimentações financeiras: receitas e despesas.

#### Filtros

- **Tipo**: Receita ou Despesa
- **Status**: Pendente, Pago, Cancelado
- **Vencimento até**: data limite de vencimento

#### Criar Lançamento Manual

1. Clique em **Novo Lançamento**
2. Informe: tipo (Receita/Despesa), descrição, valor, data de vencimento, categoria
3. Salve

Lançamentos também são **criados automaticamente** pelo sistema ao fechar vendas, gerar cobranças ou registrar compras.

#### Marcar como Pago

Clique em **Pagar** no lançamento. Informe a data de pagamento se necessário.

### 11.2 Contas a Pagar

Caminho: **Financeiro → Contas a Pagar**

Visão filtrada apenas das **despesas** (pagas e pendentes). Mesma funcionalidade dos lançamentos, com foco no controle de pagamentos a fornecedores e despesas operacionais.

### 11.3 Contas a Receber

Caminho: **Financeiro → Contas a Receber**

Visão filtrada apenas das **receitas** (pagas e pendentes). Ideal para acompanhar recebimentos de clientes.

### 11.4 Categorias

Caminho: **Financeiro → Categorias**

Permite criar categorias para organizar os lançamentos financeiros.

Exemplos de categorias:
- Receita: Serviços, Produtos, Mensalidades
- Despesa: Aluguel, Fornecedores, Salários, Manutenção

As categorias aparecem como campo obrigatório ao criar um lançamento.

---

## 12. Clientes

Caminho: **Clientes**

Cadastro centralizado de clientes. Um cliente pode estar vinculado a vendas, agendamentos, cobranças, orçamentos e contratos.

### Campos

- **Nome** (obrigatório)
- **WhatsApp** (obrigatório — usado para automação de mensagens)
- **E-mail**
- **Observações**

### Histórico do Cliente

No detalhe do cliente é possível visualizar todo o histórico de atendimentos, vendas e cobranças relacionadas.

---

## 13. Fornecedores

Caminho: **Compras → Fornecedores**

Cadastro dos fornecedores de produtos e serviços.

### Campos

- **Nome** (obrigatório)
- **CNPJ/CPF**
- **Telefone** e **e-mail**
- **Endereço completo** (logradouro, cidade, UF, CEP)
- **Nome do contato**
- **Observações**

---

## 14. Relatórios

Caminho: **Relatórios**

Painel analítico com visões por período. Selecione o **intervalo de datas** no topo da página.

### Abas disponíveis

#### Visão Geral (KPIs)
- Receita total, despesa total, saldo do período
- Quantidade de vendas, ticket médio
- Novos clientes no período

#### Vendas
- Evolução diária de vendas
- Produtos/serviços mais vendidos
- Formas de pagamento utilizadas

#### Financeiro
- Fluxo de caixa no período
- Receitas x Despesas por categoria

#### Estoque
- Produtos com maior saída
- Produtos com estoque crítico

#### Exportação CSV
Em cada aba, clique em **Exportar CSV** para baixar os dados em planilha.

---

## 15. Fiscal (NF-e)

Caminho: **Fiscal → Notas Fiscais**

Emissão de NF-e e NFS-e integrada ao **Focus.nfe**. Requer configuração prévia em **Configurações → Integrações**.

### Pré-requisitos

Antes de emitir notas fiscais, configure em **Configurações → Empresa**:
- Razão Social, CNPJ, Inscrição Estadual/Municipal
- Endereço completo
- Regime tributário
- CSC ID e CSC Token (para NFC-e)
- Token Focus.nfe
- Ambiente (Homologação para testes, Produção para emissão real)

### Emitir Nota Fiscal

1. Acesse **Fiscal → Notas Fiscais**
2. Clique em **Emitir Nota**
3. Vincule a uma venda existente ou preencha os dados manualmente
4. Clique em **Emitir**

### Status da Nota

| Status | Descrição |
|---|---|
| Pendente | Enviada para processamento |
| Autorizada | Aprovada pela SEFAZ, chave de acesso gerada |
| Cancelada | Cancelada após autorização |

### Cancelamento

Acesse o detalhe da nota e clique em **Cancelar**. Informe a justificativa (obrigatório pela legislação).

---

## 16. Contratos

O módulo de contratos é voltado para negócios que têm acordos recorrentes com clientes (mensalidades, planos, pacotes).

### 16.1 Lista de Contratos

Caminho: **Contratos → Contratos**

Filtre por status: Rascunho, Ativo, Encerrado, Cancelado.

O sistema também exibe contratos **próximos do vencimento**.

### 16.2 Criar Contrato

Caminho: **Contratos → Contratos → Novo Contrato**

1. Selecione o **cliente**
2. Informe o **título** e o **objeto** (descrição do que está sendo contratado)
3. Defina o **valor** e a **periodicidade** (mensal, trimestral, anual etc.)
4. Informe o **dia de vencimento** das cobranças
5. Defina a **data de início** e, opcionalmente, a **data de término**
6. Adicione **itens** se desejar detalhar os serviços incluídos
7. Salve

O contrato é criado como **Rascunho**.

### 16.3 Ativar Contrato

No detalhe do contrato em status Rascunho, clique em **Ativar**. O contrato passa para status **Ativo** e está pronto para gerar cobranças.

### 16.4 Gerar Cobranças

Com o contrato **Ativo**, clique em **Gerar Cobranças**:
1. Informe o **período** (data de início e fim)
2. Clique em **Gerar**

O sistema cria uma cobrança para cada período dentro do intervalo informado, respeitando a periodicidade e o dia de vencimento definidos no contrato. Cobranças já existentes para o mesmo período **não são duplicadas**.

As cobranças geradas aparecem no módulo **Cobranças** vinculadas ao contrato.

### 16.5 Assinatura Digital

Se integrado ao ClickSign (plano Profissional), clique em **Enviar para Assinatura**:
1. Informe o **e-mail do signatário**
2. Clique em **Enviar**

O sistema envia o contrato ao ClickSign e retorna um link de assinatura. O status de assinatura é atualizado automaticamente.

### 16.6 Renovar Contrato

Para contratos com data de término, clique em **Renovar Contrato**. O sistema cria um novo contrato com os mesmos dados, com data de início igual ao término do contrato anterior.

### 16.7 Encerrar ou Cancelar

- **Encerrar** — encerramento normal do contrato
- **Cancelar** — cancelamento antes do término previsto

### 16.8 PDF

Clique em **PDF** para baixar o contrato formatado para impressão.

### 16.9 Templates de Contrato

Caminho: **Contratos → Templates**

Crie modelos de contrato reutilizáveis com texto padrão e itens pré-definidos. Ao criar um novo contrato, é possível selecionar um template como base.

---

## 17. Cobranças

O módulo de cobranças centraliza as cobranças avulsas e as geradas por contratos e orçamentos. Permite envio de PIX/boleto via Asaas.

### 17.1 Lista de Cobranças

Caminho: **Contratos → Cobranças**

Filtre por:
- **Status**: Pendente, Pago, Cancelado
- **Cliente**
- **Mês de vencimento**

O resumo no topo exibe o total em aberto, total recebido e análise de inadimplência (aging).

### 17.2 Nova Cobrança Manual

Caminho: **Cobranças → Nova Cobrança**

1. Selecione o **cliente**
2. Informe a **descrição**, **valor** e **data de vencimento**
3. Opcionalmente, vincule a um **contrato**
4. Salve

### 17.3 Detalhe da Cobrança

#### Marcar como Pago
Clique em **Pagar** e informe a forma de pagamento e data.

#### Enviar via Asaas (PIX/Boleto)
Disponível para clientes com **plano Profissional**. Clique em **Enviar para Asaas**:
- O sistema cria ou reutiliza o cliente no Asaas
- Gera o PIX (QR Code) e/ou boleto
- O link de pagamento fica disponível para envio ao cliente

#### Compartilhar via WhatsApp
Clique em **WhatsApp** para abrir o link de compartilhamento da cobrança diretamente no WhatsApp do cliente.

#### Cancelar
Clique em **Cancelar** para anular a cobrança.

---

## 18. Agendamento Online (Público)

O GestorAI permite criar uma **página pública de agendamento** com link personalizado para o seu negócio. Clientes podem se agendar sem precisar de login ou contato direto.

### 18.1 Configurar a Página Pública

Caminho: **Configurações → Agendamento Online**

Configure:
- **Slug** — identificador da URL pública (ex: `barbearia-do-joao`)
- **Aprovação automática** — se ativado, agendamentos online são confirmados automaticamente; se desativado, ficam como "Aguardando Confirmação"
- **Valor do sinal** — se preenchido, o cliente paga um sinal via PIX ao agendar (requer Asaas configurado)
- **Horas limite para cancelamento** — mínimo de antecedência para o cliente cancelar

### 18.2 Link da Página Pública

A URL da página pública segue o padrão:

```
https://[seu-dominio]/agendar/[slug]
```

Compartilhe esse link com seus clientes por WhatsApp, Instagram, Google Meu Negócio etc.

### 18.3 Fluxo do Agendamento Online

1. O cliente acessa o link público
2. Escolhe o **serviço**
3. Escolhe o **profissional** (se houver mais de um)
4. Seleciona a **data e horário** disponível
5. Informa **nome e WhatsApp**
6. Se houver sinal configurado, realiza o **pagamento PIX** antes de confirmar
7. O agendamento é criado no sistema

Se a aprovação não for automática, o agendamento aparece na lista com status **Aguardando Confirmação** e deve ser confirmado ou recusado pelo atendente.

### 18.4 Disponibilidade

A disponibilidade exibida na página pública é calculada com base nas configurações de disponibilidade semanal de cada profissional e nos bloqueios de agenda cadastrados. Configure em **Agenda → Profissionais → [Profissional] → Disponibilidade**.

---

## 19. Configurações

### 19.1 Dados da Empresa

Caminho: **Configurações → Empresa** (acessível via menu de configurações)

Configure os dados gerais da empresa:
- Razão Social, Nome Fantasia, CNPJ
- Endereço completo
- Dados fiscais (Regime Tributário, Inscrição Estadual/Municipal)
- Série da NF-e e NFC-e

### 19.2 Integrações

Caminho: **Configurações → Integrações**

Configure as integrações com serviços externos:

#### Asaas (PIX e Boleto)
- **API Key**: chave de API da sua conta Asaas
- **Sandbox**: marque durante os testes; desmarque para produção

#### ClickSign (Assinatura Digital)
- **API Key**: chave da sua conta ClickSign
- **Sandbox**: marque durante os testes

#### Evolution API (WhatsApp)
- **URL**: endereço da sua instância Evolution API
- **API Key**: chave de autenticação
- **Instância**: nome da instância WhatsApp conectada

#### Focus.nfe (Notas Fiscais)
- **Token**: token da sua conta Focus.nfe
- **Ambiente**: Homologação (testes) ou Produção

Após preencher, clique em **Salvar Integrações**.

### 19.3 Automação

Caminho: **Configurações → Automação**

Configure os lembretes automáticos de cobrança via WhatsApp:

- **3 dias antes do vencimento**
- **1 dia antes do vencimento**
- **No dia do vencimento**
- **1 dia após o vencimento**
- **3 dias após o vencimento**
- **7 dias após o vencimento**

Os lembretes são disparados automaticamente para cobranças com status **Pendente** nos dias configurados.

### 19.4 White Label

Caminho: **Configurações → White Label** (visível no menu quando disponível)

Personalize a identidade visual do sistema:

- **Slug** — identificador público da empresa (usado na URL de agendamento)
- **URL do Logo** — link para a imagem do logotipo
- **Cor primária** — cor principal do sistema (código hexadecimal); afeta botões e destaques
- **Descrição pública** — texto exibido na página de agendamento público
- **Domínio customizado** — domínio próprio para acesso ao sistema (ex: `erp.meunegoico.com.br`)

---

## 20. Automação

### 20.1 Log de Envios

Caminho: **Automação → Log de Envios**

Exibe os últimos 100 registros de envios automáticos de mensagens WhatsApp.

Colunas:
- **Data/Hora** do envio
- **Evento** (tipo de lembrete)
- **Cliente** relacionado
- **Cobrança** relacionada
- **Status** (Sucesso / Falha)
- **Mensagem de erro** (quando falha)

Use o filtro **"Apenas erros"** para identificar problemas na configuração da Evolution API.

### 20.2 Testar Conexão

Clique em **Testar Conexão** para verificar se a instância WhatsApp está conectada corretamente. Se retornar erro, verifique os dados em **Configurações → Integrações**.

---

## 21. Plano de Assinatura

Caminho: **Configurações → Plano**

Exibe os planos disponíveis do GestorAI e o plano atual da empresa.

### Planos disponíveis

| Plano | Preço | Principais recursos |
|---|---|---|
| **Básico** | R$ 97/mês | Vendas, agendamentos, financeiro, estoque, clientes |
| **Profissional** | R$ 197/mês | + Cobranças Asaas (PIX/boleto), automação WhatsApp, assinatura digital ClickSign, sinal de reserva |
| **Enterprise** | R$ 397/mês | + Relatórios avançados, emissão NF-e/NFS-e, múltiplos profissionais ilimitados |

### Recursos controlados por plano

Ao tentar usar um recurso não disponível no plano atual, o sistema exibe uma mensagem informando que a funcionalidade não está disponível no plano contratado.

### Alterar Plano

Selecione o plano desejado e clique em **Escolher Plano**. Entre em contato com o suporte da QuantixAI para formalizar a mudança de plano e processar o pagamento.

---

## 22. Integrações

Resumo de todas as integrações disponíveis no GestorAI:

### Asaas

O Asaas é utilizado em dois contextos:

1. **Cobranças para seus clientes** — envio de PIX e boleto para clientes do seu negócio pelo módulo Cobranças e Agendamentos (sinal de reserva)
2. **Cobrança do GestorAI** — processamento do pagamento da assinatura do ERP (gerenciado pela QuantixAI)

Para configurar: **Configurações → Integrações → Asaas**

### ClickSign

Utilizado para envio de contratos para **assinatura digital** dos clientes. O contrato é enviado por e-mail ao signatário, que assina eletronicamente pelo link gerado.

Para configurar: **Configurações → Integrações → ClickSign**

### Evolution API (WhatsApp)

Utilizada para envio automático de **lembretes de cobrança** via WhatsApp. Requer uma instância Evolution API própria com WhatsApp conectado.

Para configurar: **Configurações → Integrações → Evolution API**

### Focus.nfe

Utilizado para emissão de **NF-e, NFC-e e NFS-e**. A Focus.nfe é um intermediador que se comunica com a SEFAZ.

Para configurar: **Configurações → Integrações → Focus.nfe**

---

## Suporte

Para dúvidas, problemas ou solicitações de novas funcionalidades, entre em contato com a QuantixAI:

- **E-mail**: suporte@quantixai.com.br
- **WhatsApp**: disponível no site da QuantixAI

---

*Manual gerado em 10/06/2026 — GestorAI v1.0 by QuantixAI*
